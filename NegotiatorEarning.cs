using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class NegotiatorRuntime
{
    private static readonly FieldInfo? ValuableValue = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
    private readonly Dictionary<string, Dictionary<PhysGrabObject, CargoProgress>> _cargo = new(StringComparer.Ordinal);
    private bool _stageActive;
    private float _nextCargoAt;

    private sealed class CargoProgress
    {
        internal float Distance;
        private Vector3 _lastPosition;
        private float _lastAt;
        private bool _outside;
        private bool _holding;
        internal void Observe(Vector3 position, bool outside, float requiredDistance)
        {
            float step = (position - _lastPosition).magnitude;
            // Ignore unheld movement and truck crossings; discard teleport progress.
            if (_holding && _outside && outside)
            {
                // Native sprint speed reaches 205 at level 200. Scale the jump
                // guard by elapsed time so legitimate upgraded movement still counts.
                float maximumStep = Math.Max(4f, (Time.time - _lastAt) * 250f);
                if (float.IsNaN(step) || float.IsInfinity(step) || step > maximumStep) Distance = 0;
                else Distance = Math.Min(requiredDistance, Distance + step);
            }
            _lastPosition = position; _lastAt = Time.time; _outside = outside; _holding = true;
        }
        internal void Released() => _holding = false;
    }

    private bool Eligible(RoleAssignment assignment, NegotiatorReward reward) => _stageActive &&
        _config.Enabled.Value && SemiFunc.IsMasterClientOrSingleplayer() && !SemiFunc.RunIsShop() &&
        RoleCatalog.HasCapability(assignment.Role, StageRole.Negotiator) &&
        PlayerState.IsLiving(assignment.Player) && _state.CanEarn(assignment.SteamId, reward);

    private void Earn(RoleAssignment assignment, NegotiatorReward reward)
    {
        if (!Eligible(assignment, reward) || !_state.TryEarn(assignment.SteamId, reward)) return;
        StageRolesPlugin.Instance.Controller.NotifyNegotiationEarned(assignment);
    }

    internal void EnemyDefeated(RoleAssignment assignment, int tier)
    {
        if (tier == _config.NegotiatorEnemyTier.Value) Earn(assignment, NegotiatorReward.EnemyKill);
    }

    private static ValuableObject? Valuable(PhysGrabObject item) =>
        item.GetComponent<ValuableObject>() ?? item.GetComponentInChildren<ValuableObject>() ?? item.GetComponentInParent<ValuableObject>();

    private bool EligibleCargo(PhysGrabObject item)
    {
        if (item == null) return false;
        ValuableObject? valuable = Valuable(item);
        return valuable != null && NegotiatorRules.AllowsSize(_config.NegotiatorDeliveryAllowedSizes.Value, valuable.volumeType.ToString()) &&
            ValuableValue?.GetValue(valuable) is float value && value > 0 && !float.IsInfinity(value);
    }

    private static void Location(PhysGrabObject item, bool refresh, out bool truck, out bool extraction)
    {
        truck = false; extraction = false;
        RoomVolumeCheck? rooms = item.GetComponent<RoomVolumeCheck>() ??
            item.GetComponentInChildren<RoomVolumeCheck>() ?? item.GetComponentInParent<RoomVolumeCheck>();
        if (refresh && rooms != null) rooms.CheckSet();
        if (rooms?.CurrentRooms == null) return;
        foreach (RoomVolume room in rooms.CurrentRooms)
        {
            if (room == null) continue;
            truck |= room.Truck; extraction |= room.Extraction;
        }
    }

    internal void TickStage(IReadOnlyList<RoleAssignment> assignments)
    {
        if (!_stageActive || Time.time < _nextCargoAt) return;
        _nextCargoAt = Time.time + 0.25f;
        PhysGrabObject[]? objects = null;
        foreach (RoleAssignment assignment in assignments)
        {
            if (!Eligible(assignment, NegotiatorReward.Delivery)) { _cargo.Remove(assignment.SteamId); continue; }
            objects ??= UnityEngine.Object.FindObjectsByType<PhysGrabObject>(FindObjectsSortMode.InstanceID);
            if (!_cargo.TryGetValue(assignment.SteamId, out var tracked))
                _cargo[assignment.SteamId] = tracked = new();
            HashSet<PhysGrabObject> held = new();
            foreach (PhysGrabObject item in objects)
            {
                if (!UtilityRoleRuntime.IsHeldBy(item, assignment) || !EligibleCargo(item)) continue;
                held.Add(item);
                if (!tracked.TryGetValue(item, out CargoProgress? progress)) tracked[item] = progress = new();
                Location(item, false, out bool truck, out _);
                progress.Observe(assignment.Player.transform.position, !truck && !PlayerState.IsInTruck(assignment.Player), _config.NegotiatorDeliveryCarryDistance.Value);
            }
            List<PhysGrabObject> removed = new();
            foreach (var pair in tracked)
            {
                if (pair.Key == null) removed.Add(pair.Key!);
                else if (!held.Contains(pair.Key)) pair.Value.Released();
            }
            foreach (PhysGrabObject item in removed) tracked.Remove(item);
        }
    }

    internal void CargoReleased(RoleAssignment assignment, PhysGrabObject item)
    {
        if (!Eligible(assignment, NegotiatorReward.Delivery) || !EligibleCargo(item) ||
            !_cargo.TryGetValue(assignment.SteamId, out var tracked) || !tracked.TryGetValue(item, out CargoProgress? progress)) return;
        // Release may occur between polling ticks. Refresh the object's room membership.
        Location(item, true, out bool truck, out bool extraction);
        progress.Observe(assignment.Player.transform.position, !truck && !PlayerState.IsInTruck(assignment.Player), _config.NegotiatorDeliveryCarryDistance.Value);
        progress.Released();
        if (progress.Distance >= _config.NegotiatorDeliveryCarryDistance.Value && extraction && !truck)
            Earn(assignment, NegotiatorReward.Delivery);
    }
}
