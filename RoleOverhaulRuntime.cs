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
    internal void Stop() { KingUpgradeAura.Stop(); _cargoPositions.Clear(); _nextTick = 0; }

    internal void Tick(IReadOnlyList<RoleAssignment> assignments, RoleNotifier notifier)
    {
        if (Time.time < _nextTick) return;
        _nextTick = Time.time + 0.25f;
        KingUpgradeAura.Tick(_config, assignments);
        PhysGrabObject[]? candidates = null;
        foreach (RoleAssignment assignment in assignments)
        {
            RoleOverhaulState state = assignment.Overhaul;
            if (!PlayerState.IsLiving(assignment.Player))
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
        // The contract is already consumed, so this full heal is sent once even
        // if another capped heal is pending. No retries or repeated rewards.
        try { RoleHealingRuntime.TryHealToFull(assignment.Player); }
        catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug($"Contract healing skipped: {error.Message}"); }
    }

}
