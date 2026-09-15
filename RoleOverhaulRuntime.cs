using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class RoleOverhaulRuntime
{
    private static readonly FieldInfo? CurrentValue = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
    private readonly StageRolesConfig _config;
    private readonly Dictionary<string, Vector3> _cargoPositions = new(StringComparer.Ordinal);
    private float _nextTick;

    internal RoleOverhaulRuntime(StageRolesConfig config) => _config = config;
    internal void Stop() { _cargoPositions.Clear(); _nextTick = 0; }

    internal void Tick(IReadOnlyList<RoleAssignment> assignments, RoleNotifier notifier)
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + 0.25f;
        PhysGrabObject[]? candidates = null;
        foreach (RoleAssignment assignment in assignments)
        {
            RoleOverhaulState state = assignment.Overhaul;
            if (!_config.OverhaulEnabled.Value || !PlayerState.IsLiving(assignment.Player))
            {
                state.ResetCargo();
                _cargoPositions.Remove(assignment.SteamId);
                continue;
            }
            if (assignment.Role == StageRole.Jobless)
            {
                state.Start(Time.time, _config.JoblessContractGrace.Value);
                if (state.ContractsCompleted < _config.JoblessContractLimit.Value)
                {
                    candidates ??= UnityEngine.Object.FindObjectsOfType<PhysGrabObject>();
                    TickContract(assignment, candidates, notifier);
                }
            }
            else state.ResetCargo();
            if (assignment.Role == StageRole.King) TickKing(assignment, assignments);
        }
    }

    private void TickContract(RoleAssignment assignment, PhysGrabObject[] candidates, RoleNotifier notifier)
    {
        RoleOverhaulState state = assignment.Overhaul;
        PhysGrabObject? cargo = null;
        foreach (PhysGrabObject item in candidates)
        {
            if (item == null || state.IsDelivered(item.GetInstanceID()) || !UtilityRoleRuntime.IsHeldBy(item, assignment)) continue;
            ValuableObject? valuable = item.GetComponent<ValuableObject>() ??
                item.GetComponentInChildren<ValuableObject>() ?? item.GetComponentInParent<ValuableObject>();
            if (valuable == null || CurrentValue?.GetValue(valuable) is not float value || value <= 0) continue;
            if (item.GetInstanceID() == state.CargoId) { cargo = item; break; }
            if (cargo == null || item.GetInstanceID() < cargo.GetInstanceID()) cargo = item;
        }
        if (cargo == null)
        {
            state.ResetCargo();
            _cargoPositions.Remove(assignment.SteamId);
            return;
        }
        Vector3 position = cargo.centerPoint;
        float distance = _cargoPositions.TryGetValue(assignment.SteamId, out Vector3 previous)
            ? Vector3.Distance(position, previous) : 0f;
        _cargoPositions[assignment.SteamId] = position;
        bool completed = state.Carry(cargo.GetInstanceID(), distance,
            PlayerState.IsInTruck(assignment.Player), Time.time,
            _config.JoblessContractDistance.Value, _config.JoblessContractGrace.Value,
            _config.JoblessContractLimit.Value);
        if (!completed) return;
        assignment.JoblessDamageTimer = 0;
        notifier.NotifyResponse(assignment.Player, "ContractComplete");
        // One attempt per completed contract: it cannot repeat after a failed
        // or unacknowledged remote request, nor overlap another capped healer.
        try { RoleHealingRuntime.TryHeal(assignment.Player, _config.JoblessContractHeal.Value, _ => { }); }
        catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug($"Contract healing skipped: {error.Message}"); }
    }

    private void TickKing(RoleAssignment king, IReadOnlyList<RoleAssignment> assignments)
    {
        RoleOverhaulState state = king.Overhaul;
        if (Time.time < state.NextKingHealAt) return;
        state.NextKingHealAt = Time.time + _config.KingHealInterval.Value;
        float radiusSquared = _config.KingHealRadius.Value * _config.KingHealRadius.Value;
        int count = assignments.Count;
        int start = state.NextKingTarget;
        for (int offset = 0; offset < count; offset++)
        {
            int remaining = Math.Max(0, _config.KingHealLimit.Value - state.KingHealingUsed);
            if (remaining == 0) break;
            RoleAssignment target = assignments[(start + offset) % count];
            if (target.SteamId == king.SteamId || !PlayerState.IsLiving(target.Player) ||
                (target.Player.transform.position - king.Player.transform.position).sqrMagnitude > radiusSquared ||
                !PlayerState.TryGetCurrentHealth(target.Player, out int health) ||
                !PlayerState.TryGetMaximumHealth(target.Player, out int maximum)) continue;
            int amount = Math.Min(remaining, Math.Min(_config.KingHealAmount.Value, Math.Max(0, maximum - health)));
            if (amount <= 0) continue;
            // Reserve before invoking vanilla healing, including synchronous callbacks.
            state.KingHealingUsed += amount;
            try
            {
                if (!RoleHealingRuntime.TryHeal(target.Player, amount,
                    restored => state.KingHealingUsed = Math.Max(0, state.KingHealingUsed - amount + restored)))
                    state.KingHealingUsed -= amount;
            }
            catch (Exception error)
            { StageRolesPlugin.ModLogger.LogDebug($"King healing skipped: {error.Message}"); }
        }
        state.NextKingTarget = count > 0 ? (start + 1) % count : 0;
    }
}
