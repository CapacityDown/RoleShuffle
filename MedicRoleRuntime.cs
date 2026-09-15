using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class MedicRoleRuntime
{
    private static readonly FieldInfo? HealthField =
        AccessTools.Field(typeof(PlayerHealth), "health");
    private static readonly FieldInfo? MaxHealthField =
        AccessTools.Field(typeof(PlayerHealth), "maxHealth");

    private sealed class MedicState
    {
        internal MedicState(
            string ownerSteamId,
            PlayerAvatar player,
            float nextHealAt)
        {
            OwnerSteamId = ownerSteamId;
            Player = player;
            NextHealAt = nextHealAt;
        }

        internal string OwnerSteamId { get; }
        internal PlayerAvatar Player { get; }
        internal float NextHealAt { get; set; }
    }

    private readonly StageRolesConfig _config;
    private readonly Dictionary<string, MedicState> _medics =
        new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> _healingUsed = new(StringComparer.Ordinal);
    private readonly HashSet<string> _exhaustionLogged = new(StringComparer.Ordinal);
    private bool _active;

    internal int HealingUsed(string steamId) => _healingUsed.TryGetValue(steamId, out int used) ? used : 0;

    internal bool IsExhausted(string steamId) =>
        _healingUsed.TryGetValue(steamId, out int used) &&
        used >= Mathf.Clamp(_config.MedicTotalHealingLimit.Value, 1, 10000);

    internal MedicRoleRuntime(StageRolesConfig config)
    {
        _config = config;
    }

    internal void Begin(IEnumerable<RoleAssignment> assignments)
    {
        _medics.Clear();
        _active = true;
        HashSet<string> activeMedics = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Medic))
            {
                activeMedics.Add(assignment.SteamId);
                if (!_healingUsed.ContainsKey(assignment.SteamId))
                {
                    _healingUsed[assignment.SteamId] = 0;
                }
                EnsureMedic(assignment);
            }
        }
        List<string> removedMedics = new();
        foreach (string steamId in _healingUsed.Keys)
        {
            if (!activeMedics.Contains(steamId))
            {
                removedMedics.Add(steamId);
            }
        }
        foreach (string steamId in removedMedics)
        {
            _healingUsed.Remove(steamId);
            _exhaustionLogged.Remove(steamId);
        }
    }

    internal void Tick(IEnumerable<RoleAssignment> assignments)
    {
        if (!_active)
        {
            return;
        }

        IReadOnlyList<RoleAssignment> currentAssignments =
            assignments as IReadOnlyList<RoleAssignment> ??
            new List<RoleAssignment>(assignments);
        foreach (RoleAssignment assignment in currentAssignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Medic))
            {
                EnsureMedic(assignment);
            }
        }

        List<string> stale = new();
        foreach (KeyValuePair<string, MedicState> pair in _medics)
        {
            MedicState medic = pair.Value;
            if (medic.Player == null)
            {
                stale.Add(pair.Key);
                continue;
            }
            HealNearbyPlayers(medic, currentAssignments);
        }

        foreach (string key in stale)
        {
            _medics.Remove(key);
        }
    }

    internal void Stop()
    {
        _active = false;
        _medics.Clear();
        _healingUsed.Clear();
        _exhaustionLogged.Clear();
    }

    internal void AddPlayer(RoleAssignment assignment)
    {
        if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Medic))
        {
            return;
        }
        if (!_healingUsed.ContainsKey(assignment.SteamId))
        {
            _healingUsed[assignment.SteamId] = 0;
        }
        EnsureMedic(assignment);
    }

    internal void RemovePlayer(string steamId, bool preserveHealingState = false)
    {
        _medics.Remove(steamId);
        if (!preserveHealingState)
        {
            _healingUsed.Remove(steamId);
            _exhaustionLogged.Remove(steamId);
        }
    }

    private void EnsureMedic(RoleAssignment assignment)
    {
        if (_medics.ContainsKey(assignment.SteamId))
        {
            return;
        }
        if (assignment.Player == null)
        {
            return;
        }

        _medics[assignment.SteamId] = new MedicState(
            assignment.SteamId,
            assignment.Player,
            Time.time + Mathf.Clamp(
                _config.MedicHealIntervalSeconds.Value,
                0.1f,
                30f));
    }

    private void HealNearbyPlayers(
        MedicState follow,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (Time.time < follow.NextHealAt)
        {
            return;
        }
        follow.NextHealAt = Time.time + Mathf.Clamp(
            _config.MedicHealIntervalSeconds.Value,
            0.1f,
            30f);
        if (!PlayerState.IsLiving(follow.Player))
        {
            return;
        }

        float radius = Mathf.Clamp(_config.MedicHealRadius.Value, 1f, 30f);
        float radiusSquared = radius * radius;
        int healAmount = Mathf.Clamp(_config.MedicHealAmount.Value, 1, 100);
        int healingLimit = Mathf.Clamp(
            _config.MedicTotalHealingLimit.Value,
            1,
            10000);
        _healingUsed.TryGetValue(follow.OwnerSteamId, out int healingUsed);
        int healingRemaining = Mathf.Max(0, healingLimit - healingUsed);
        if (healingRemaining <= 0)
        {
            LogExhaustion(follow.OwnerSteamId, healingLimit);
            return;
        }
        _exhaustionLogged.Remove(follow.OwnerSteamId);
        Vector3 center = follow.Player.transform.position;
        foreach (RoleAssignment assignment in assignments)
        {
            if (string.Equals(
                    assignment.SteamId,
                    follow.OwnerSteamId,
                    StringComparison.Ordinal))
            {
                continue;
            }
            PlayerAvatar target = assignment.Player;
            if (!PlayerState.IsLiving(target) || target.playerHealth == null ||
                (target.transform.position - center).sqrMagnitude > radiusSquared)
            {
                continue;
            }
            try
            {
                int missingHealth = MissingHealth(target.playerHealth);
                int appliedHealing = Mathf.Min(
                    healAmount,
                    Mathf.Min(healingRemaining, missingHealth));
                if (appliedHealing <= 0)
                {
                    continue;
                }
                healingUsed += appliedHealing;
                healingRemaining -= appliedHealing;
                _healingUsed[follow.OwnerSteamId] = healingUsed;
                if (!RoleHealingRuntime.TryHeal(target, appliedHealing, restored =>
                    {
                        if (_active && _healingUsed.TryGetValue(follow.OwnerSteamId, out int used))
                            _healingUsed[follow.OwnerSteamId] = Math.Max(0, used - appliedHealing + restored);
                    }))
                {
                    _healingUsed[follow.OwnerSteamId] -= appliedHealing;
                }
                healingUsed = _healingUsed[follow.OwnerSteamId];
                healingRemaining = Math.Max(0, healingLimit - healingUsed);
                if (healingRemaining <= 0)
                {
                    LogExhaustion(follow.OwnerSteamId, healingLimit);
                    break;
                }
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Medic healing skipped for {assignment.SteamId}: " +
                    exception.Message);
            }
        }
    }

    private static int MissingHealth(PlayerHealth playerHealth)
    {
        try
        {
            if (HealthField?.GetValue(playerHealth) is int currentHealth &&
                MaxHealthField?.GetValue(playerHealth) is int maximumHealth)
            {
                return Mathf.Max(0, maximumHealth - currentHealth);
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Medic health read failed: {exception.Message}");
        }
        return int.MaxValue;
    }

    private void LogExhaustion(string ownerSteamId, int healingLimit)
    {
        if (_exhaustionLogged.Add(ownerSteamId))
        {
            StageRolesPlugin.ModLogger.LogInfo(
                $"Medic {ownerSteamId} exhausted its {healingLimit} HP stage healing limit.");
        }
    }

}
