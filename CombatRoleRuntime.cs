using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    private static readonly FieldInfo? CombatEnemyHealthField = AccessTools.Field(typeof(EnemyHealth), "healthCurrent");

    internal static int ReadCombatEnemyHealth(Enemy enemy) =>
        enemy != null && enemy.GetComponent<EnemyHealth>() is EnemyHealth health &&
        CombatEnemyHealthField?.GetValue(health) is int value ? value : -1;

    internal void ConfirmEnemyDamage(HurtCollider collider, Enemy enemy, PlayerAvatar? attacker, int before)
    {
        if (!_stageReady || !IsAuthority() || before <= 0) return;
        int after = ReadCombatEnemyHealth(enemy);
        if (after < 0 || after >= before) return;
        RecordEnemyAttacker(enemy, attacker);
        RoleAssignment? assignment = attacker != null ? FindAssignment(attacker) : null;
        if (assignment?.Role == StageRole.Brawler && PlayerState.IsLiving(attacker) &&
            collider.GetComponentInParent<ItemMelee>() != null)
            assignment.BrawlerCombo.Confirm(enemy.GetInstanceID(), Time.time, Time.frameCount, _config.BrawlerComboTimeout.Value);
    }

    internal void NotifyAvengerAllyHit(PlayerAvatar victim, int damage)
    {
        if (!_stageReady || !IsAuthority() || damage < _config.AvengerHitDamageThreshold.Value) return;
        float range = _config.AvengerHitTriggerRadius.Value;
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Avenger) ||
                ReferenceEquals(assignment.Player, victim) || assignment.SteamId == PlayerIdentity.SteamId(victim) ||
                !PlayerState.IsLiving(assignment.Player) || Time.time < assignment.AvengerNextHitAt ||
                (assignment.Player.transform.position - victim.transform.position).sqrMagnitude > range * range) continue;
            assignment.AvengerHitUntil = Time.time + _config.AvengerHitDuration.Value;
            assignment.AvengerNextHitAt = Time.time + _config.AvengerHitCooldown.Value;
            if (Time.time >= assignment.AvengerEmpoweredUntil)
                _notifier.NotifyConditional(assignment.Player, "AvengerActive",
                    () => RoleCatalog.HasCapability(assignment.Role, StageRole.Avenger) &&
                        PlayerState.IsLiving(assignment.Player) && Time.time < assignment.AvengerHitUntil);
        }
    }

    private void TickGhostSupport()
    {
        float interval = _config.GhostHealInterval.Value;
        foreach (RoleAssignment ghost in _assignments)
        {
            if (!RoleCatalog.HasCapability(ghost.Role, StageRole.Ghost) || PlayerState.IsLiving(ghost.Player))
            { ghost.GhostNextHealAt = Time.time + interval; continue; }
            if (Time.time < ghost.GhostNextHealAt) continue;
            ghost.GhostNextHealAt = Time.time + interval;
            if (!PlayerState.TryGetActiveDeathHeadPosition(ghost.Player, out Vector3 position)) continue;
            float range = _config.GhostHealRadius.Value;
            foreach (RoleAssignment target in _assignments)
            {
                int amount = Math.Min(_config.GhostHealAmount.Value, _config.GhostTotalHealingLimit.Value - ghost.GhostHealingUsed);
                if (amount <= 0) break;
                if (!PlayerState.IsLiving(target.Player) ||
                    (target.Player.transform.position - position).sqrMagnitude > range * range ||
                    !PlayerState.TryGetCurrentHealth(target.Player, out int health) ||
                    !PlayerState.TryGetMaximumHealth(target.Player, out int maximum)) continue;
                amount = Math.Min(amount, Math.Max(0, maximum - health));
                if (amount <= 0) continue;
                int reserved = amount;
                ghost.GhostHealingUsed += reserved;
                try
                {
                    if (!RoleHealingRuntime.TryHeal(target.Player, reserved,
                        restored => ghost.GhostHealingUsed -= reserved - restored)) ghost.GhostHealingUsed -= reserved;
                }
                catch (Exception error)
                { StageRolesPlugin.ModLogger.LogDebug($"Ghost heal kept its reservation: {error.Message}"); }
            }
        }
    }
}
