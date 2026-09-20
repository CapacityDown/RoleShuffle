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
                state.Start(Time.time, _config.JoblessInitialGrace.Value);
                candidates ??= UnityEngine.Object.FindObjectsOfType<PhysGrabObject>();
                TickContract(assignment, candidates, notifier);
            }
            else state.ResetCargo();
        }
    }

    private void TickContract(RoleAssignment assignment, PhysGrabObject[] candidates, RoleNotifier notifier)
    {
        RoleOverhaulState state = assignment.Overhaul;
        PhysGrabObject? cargo = null;
        ValuableObject? cargoValuable = null;
        foreach (PhysGrabObject item in candidates)
        {
            if (item == null || state.IsDelivered(item.GetInstanceID()) || !UtilityRoleRuntime.IsHeldBy(item, assignment)) continue;
            ValuableObject? valuable = Valuable(item);
            if (valuable == null || CurrentValue?.GetValue(valuable) is not float value || value <= 0) continue;
            if (item.GetInstanceID() == state.CargoId) { cargo = item; cargoValuable = valuable; break; }
            if (cargo == null || item.GetInstanceID() < cargo.GetInstanceID()) { cargo = item; cargoValuable = valuable; }
        }
        if (cargo == null)
        {
            state.ResetCargo();
            _cargoPositions.Remove(assignment.SteamId);
            return;
        }
        ObserveCargo(assignment, cargo, cargoValuable!, notifier, false);
    }

    private static ValuableObject? Valuable(PhysGrabObject item) => item.GetComponent<ValuableObject>() ??
        item.GetComponentInChildren<ValuableObject>() ?? item.GetComponentInParent<ValuableObject>();

    internal float DeliveryGrace(ValuableVolume.Type size) => size switch
    {
        ValuableVolume.Type.Tiny => _config.JoblessTinyGrace.Value,
        ValuableVolume.Type.Small => _config.JoblessSmallGrace.Value,
        ValuableVolume.Type.Medium => _config.JoblessMediumGrace.Value,
        ValuableVolume.Type.Big => _config.JoblessBigGrace.Value,
        ValuableVolume.Type.Wide => _config.JoblessWideGrace.Value,
        ValuableVolume.Type.Tall => _config.JoblessTallGrace.Value,
        ValuableVolume.Type.VeryTall => _config.JoblessVeryTallGrace.Value,
        _ => _config.JoblessSmallGrace.Value
    };

    internal int DeliveryHeal(ValuableVolume.Type size) => size switch
    {
        ValuableVolume.Type.Tiny => _config.JoblessTinyHeal.Value,
        ValuableVolume.Type.Small => _config.JoblessSmallHeal.Value,
        ValuableVolume.Type.Medium => _config.JoblessMediumHeal.Value,
        ValuableVolume.Type.Big => _config.JoblessBigHeal.Value,
        ValuableVolume.Type.Wide => _config.JoblessWideHeal.Value,
        ValuableVolume.Type.Tall => _config.JoblessTallHeal.Value,
        ValuableVolume.Type.VeryTall => _config.JoblessVeryTallHeal.Value,
        _ => _config.JoblessSmallHeal.Value
    };

    // Called before release clears carrying progress. Refresh the game's room
    // check so placing an item and releasing it between ticks still qualifies.
    internal void CargoReleased(RoleAssignment assignment, PhysGrabObject cargo, RoleNotifier notifier)
    {
        if (assignment.Role != StageRole.Jobless || !PlayerState.IsLiving(assignment.Player) ||
            assignment.Overhaul.CargoId != cargo.GetInstanceID()) return;
        try
        {
            ValuableObject? valuable = Valuable(cargo);
            if (valuable != null && CurrentValue?.GetValue(valuable) is float value && value > 0)
                ObserveCargo(assignment, cargo, valuable, notifier, true);
        }
        catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug($"Delivery on release skipped: {error.Message}"); }
    }

    private void ObserveCargo(RoleAssignment assignment, PhysGrabObject cargo, ValuableObject valuable,
        RoleNotifier notifier, bool refreshRoom)
    {
        RoomVolumeCheck? rooms = cargo.GetComponent<RoomVolumeCheck>() ??
            cargo.GetComponentInChildren<RoomVolumeCheck>() ?? cargo.GetComponentInParent<RoomVolumeCheck>();
        if (refreshRoom && rooms != null) rooms.CheckSet();
        bool inDeliveryArea = false;
        if (rooms?.CurrentRooms != null)
            foreach (RoomVolume room in rooms.CurrentRooms)
                if (room != null && (room.Truck || room.Extraction)) { inDeliveryArea = true; break; }

        Vector3 position = cargo.centerPoint;
        float distance = _cargoPositions.TryGetValue(assignment.SteamId, out Vector3 previous)
            ? Vector3.Distance(position, previous) : 0f;
        _cargoPositions[assignment.SteamId] = position;
        bool completed = assignment.Overhaul.Carry(cargo.GetInstanceID(), distance,
            inDeliveryArea, Time.time, _config.JoblessContractDistance.Value, DeliveryGrace(valuable.volumeType));
        if (!completed) return;
        assignment.JoblessDamageTimer = 0;
        notifier.NotifyResponse(assignment.Player, "ContractComplete");
        // The valuable is already recorded, so its reward is sent once even
        // if another capped heal is pending. No retries or repeated rewards.
        try { RoleHealingRuntime.TryHealReward(assignment.Player, DeliveryHeal(valuable.volumeType)); }
        catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug($"Contract healing skipped: {error.Message}"); }
    }

}
