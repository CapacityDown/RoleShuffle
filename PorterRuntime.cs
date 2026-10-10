using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal void PorterEnemyHit(PlayerAvatar victim, int damage)
    {
        if (_stageReady && _assignmentsInitialized && IsAuthority() && damage > 0)
            _porter.EnemyHit(victim, _notifier);
    }
}

// All cargo and value decisions run on authority. The ORIGINAL network object
// is parked outside the playable map; there is no despawn/respawn or value copy.
internal sealed class PorterRuntime(StageRolesConfig config)
{
    private static readonly FieldInfo? OriginalMass = AccessTools.Field(typeof(PhysGrabObject), "massOriginal");
    private static readonly FieldInfo? CurrentValue = AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
    private readonly Dictionary<string, Carrier> _carriers = new(StringComparer.Ordinal);
    private readonly Dictionary<int, Cargo> _stored = new();
    private readonly Dictionary<int, float> _storeBlockedUntil = new();
    private float _nextScan;

    private sealed class Carrier(PlayerAvatar player)
    {
        internal readonly string SteamId = PlayerIdentity.SteamId(player);
        internal readonly PlayerAvatar Player = player;
        internal readonly PorterLoad Load = new();
        internal readonly PorterHold Hold = new();
        internal readonly PorterUnload Unload = new();
        internal PhysGrabCart? UnloadCart;
        internal readonly List<Cargo> Cargo = new();
        internal Vector3 LastPosition = player.transform.position;
        internal int RejectedId;
        internal bool Retiring;
    }

    private sealed class Cargo(PhysGrabObject physics, float mass)
    {
        internal readonly PhysGrabObject Physics = physics;
        internal readonly int Id = physics.GetInstanceID();
        internal readonly float Mass = mass;
        internal readonly Quaternion Rotation = physics.transform.rotation;
        internal bool PendingReturn;
        private readonly List<Behaviour> _paused = new();

        internal void Store()
        {
            // Leave Photon and the base physics/value/room components running.
            // Native inventory deactivation supplies collision/position handling.
            foreach (MonoBehaviour behaviour in Physics.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour == null || !behaviour.enabled || behaviour is ValuableObject) continue;
                string name = behaviour.GetType().Name;
                if (behaviour is Trap || behaviour is HurtCollider ||
                    name.StartsWith("Valuable", StringComparison.Ordinal) || name == "ScreamDollValuable")
                { _paused.Add(behaviour); behaviour.enabled = false; }
            }
            PorterStorage.Begin(Physics);
            Maintain();
        }

        internal void Maintain()
        {
            if (Physics == null) return;
            Physics.OverrideIndestructible(1f);
            Physics.OverrideGrabDisable(1f);
            Physics.DisableDeathPitEffect(1f);
            PorterStorage.Maintain(Physics);
        }

        internal void Restore(Vector3 position)
        {
            if (Physics == null) return;
            Physics.OverrideDeactivateReset();
            Physics.OverrideIndestructible(2f);
            Physics.OverrideBreakEffects(2f);
            Physics.OverrideGrabDisable(0.5f);
            Physics.Teleport(position, Rotation);
            if (Physics.rb != null && !Physics.rb.isKinematic)
            { Physics.rb.velocity = Vector3.zero; Physics.rb.angularVelocity = Vector3.zero; }
            foreach (Behaviour behaviour in _paused)
                if (behaviour != null) behaviour.enabled = true;
            _paused.Clear();
            Physics.GetComponent<RoomVolumeCheck>()?.CheckSet();
        }
    }

    internal float Weight(string steamId) => _carriers.TryGetValue(steamId, out Carrier? carrier) ? carrier.Load.Weight : 0;
    internal int Value(string steamId)
    {
        double total = 0;
        if (_carriers.TryGetValue(steamId, out Carrier? carrier))
            foreach (Cargo cargo in carrier.Cargo)
                if (cargo.Physics != null && cargo.Physics.GetComponent<ValuableObject>() is ValuableObject valuable &&
                    CurrentValue?.GetValue(valuable) is float value && PorterRules.Finite(value) && value > 0) total += value;
        return (int)Math.Min(int.MaxValue, Math.Floor(total));
    }
    internal float UnloadReadyAt(string steamId) => _carriers.TryGetValue(steamId, out Carrier? carrier) && carrier.Unload.Active
        ? carrier.Unload.ReadyAt : 0;

    internal void Tick(IReadOnlyList<RoleAssignment> assignments, RoleNotifier notifier)
    {
        HashSet<string> live = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in assignments)
        {
            if (assignment.Role != StageRole.Porter || !PlayerState.IsLiving(assignment.Player)) continue;
            live.Add(assignment.SteamId);
            if (_carriers.TryGetValue(assignment.SteamId, out Carrier? prior) && prior.Player != assignment.Player)
                ReleaseAll(assignment.SteamId);
            if (!_carriers.ContainsKey(assignment.SteamId)) _carriers[assignment.SteamId] = new Carrier(assignment.Player);
        }
        foreach (string id in new List<string>(_carriers.Keys))
            if (!live.Contains(id) || !config.Enabled.Value || _carriers[id].Retiring) ReleaseAll(id);
        foreach (Carrier carrier in _carriers.Values)
        {
            if (carrier.Retiring) continue;
            // Hold the last grounded position for disconnects or death in a pit.
            Vector3 position = carrier.Player.transform.position;
            if (Physics.Raycast(position + Vector3.up * 0.2f, Vector3.down, 3f,
                LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)) carrier.LastPosition = position;
            int dropIndex = 0;
            foreach (Cargo cargo in new List<Cargo>(carrier.Cargo))
            {
                if (cargo.Physics == null) Remove(carrier, cargo);
                else if (cargo.PendingReturn) Drop(carrier, cargo, dropIndex++);
                else cargo.Maintain();
            }
            UpdateSpeed(carrier);
        }
        if (Time.time < _nextScan || _carriers.Count == 0) return;
        _nextScan = Time.time + 0.1f;
        PhysGrabObject[] objects = UnityEngine.Object.FindObjectsOfType<PhysGrabObject>();
        foreach (Carrier carrier in _carriers.Values)
        {
            if (carrier.Retiring) continue;
            PhysGrabCart? cart = PorterCart.RidingCart(carrier.Player);
            bool delivery = IsDelivery(carrier.Player.RoomVolumeCheck);
            if (carrier.UnloadCart != cart) carrier.Unload.Reset();
            carrier.UnloadCart = cart;
            if (delivery || cart != null)
            {
                carrier.Hold.Reset();
                bool starting = !carrier.Unload.Active && carrier.Load.Count > 0;
                if (carrier.Unload.Tick(true, carrier.Load.Weight, config.PorterCapacity.Value,
                    config.PorterUnloadSeconds.Value, Time.time))
                {
                    DropAll(carrier, cart);
                    if (carrier.Cargo.Count == 0) notifier.NotifyResponse(carrier.Player, "Unloaded");
                }
                else if (starting) notifier.NotifyResponse(carrier.Player, "Unloading");
                continue;
            }
            carrier.Unload.Reset();
            PhysGrabObject? held = null;
            foreach (PhysGrabObject item in objects)
            {
                if (item == null || item.playerGrabbing.Count != 1 ||
                    item.playerGrabbing[0] != carrier.Player.physGrabber || _stored.ContainsKey(item.GetInstanceID())) continue;
                if (held == null || item.GetInstanceID() < held.GetInstanceID()) held = item;
            }
            if (held == null) { carrier.Hold.Reset(); carrier.RejectedId = 0; continue; }
            int itemId = held.GetInstanceID();
            if (_storeBlockedUntil.TryGetValue(itemId, out float until) && Time.time < until)
            { carrier.Hold.Reset(); continue; }
            ValuableObject? valuable = held.GetComponent<ValuableObject>();
            if (valuable == null || held.GetComponent<ItemAttributes>() != null ||
                CurrentValue?.GetValue(valuable) is not float value || value <= 0 ||
                IsDelivery(held.GetComponent<RoomVolumeCheck>())) { carrier.Hold.Reset(); continue; }
            float mass = OriginalMass?.GetValue(held) is float original ? original : float.NaN;
            string? rejection = !PorterRules.AllowsSize(config.PorterAllowedSizes.Value, valuable.volumeType.ToString())
                ? "TooLarge" : !PorterRules.Fits(carrier.Load.Weight, mass, config.PorterCapacity.Value) ? "TooHeavy" : null;
            if (rejection != null)
            {
                carrier.Hold.Reset();
                if (carrier.RejectedId != itemId) { carrier.RejectedId = itemId; notifier.NotifyResponse(carrier.Player, rejection); }
                continue;
            }
            carrier.RejectedId = 0;
            if (!carrier.Hold.Ready(itemId, Time.time, config.PorterHoldSeconds.Value)) continue;
            carrier.Hold.Reset();
            if (Store(carrier, held, mass)) notifier.NotifyResponse(carrier.Player,
                carrier.Load.Weight >= config.PorterCapacity.Value - 0.000001f ? "CargoFull" : "Stored");
        }
    }

    private bool Store(Carrier carrier, PhysGrabObject physics, float mass)
    {
        int id = physics.GetInstanceID();
        if (_stored.ContainsKey(id) || !carrier.Load.Add(id, mass, config.PorterCapacity.Value)) return false;
        Cargo cargo = new(physics, mass);
        Vector3 originalPosition = physics.transform.position;
        try
        {
            _stored.Add(id, cargo);
            carrier.Cargo.Add(cargo);
            // Release through the native owner RPC before moving the item away.
            Photon.Pun.PhotonView? view = physics.GetComponent<Photon.Pun.PhotonView>();
            carrier.Player.physGrabber.OverrideGrabRelease(view != null ? view.ViewID : -1, 0.5f);
            cargo.Store();
            UpdateSpeed(carrier);
            return true;
        }
        catch (Exception error)
        {
            StageRolesPlugin.ModLogger.LogWarning($"Porter storage was rolled back: {error.Message}");
            try { cargo.Restore(originalPosition); Remove(carrier, cargo); }
            catch (Exception restoreError)
            {
                cargo.PendingReturn = true;
                StageRolesPlugin.ModLogger.LogWarning($"Porter will retry storage rollback: {restoreError.Message}");
            }
            return false;
        }
    }

    internal void EnemyHit(PlayerAvatar player, RoleNotifier notifier)
    {
        if (!config.PorterDropOnEnemyHit.Value || !PlayerState.IsLiving(player) ||
            !_carriers.TryGetValue(PlayerIdentity.SteamId(player), out Carrier? carrier) || carrier.Cargo.Count == 0) return;
        // Scatter every original object; failed returns remain owned and are retried.
        carrier.Unload.Reset();
        carrier.Hold.Reset();
        foreach (Cargo cargo in new List<Cargo>(carrier.Cargo))
            if (cargo.Physics == null) Remove(carrier, cargo);
        if (carrier.Cargo.Count == 0) return;
        int count = carrier.Cargo.Count;
        foreach (Cargo cargo in carrier.Cargo) cargo.PendingReturn = true;
        DropAll(carrier);
        if (carrier.Cargo.Count < count) notifier.NotifyResponse(player, "CargoDropped");
    }

    internal void ReleaseAll(string steamId)
    {
        if (!_carriers.TryGetValue(steamId, out Carrier? carrier)) return;
        carrier.Retiring = true;
        PorterSpeed.Update(steamId, 0, config.PorterCapacity.Value);
        DropAll(carrier);
        if (carrier.Cargo.Count == 0) _carriers.Remove(steamId);
    }

    private void DropAll(Carrier carrier, PhysGrabCart? cart = null)
    {
        int index = 0;
        foreach (Cargo cargo in new List<Cargo>(carrier.Cargo)) Drop(carrier, cargo, index++, cart);
        UpdateSpeed(carrier);
        carrier.Hold.Reset();
        carrier.Unload.Reset();
    }

    private bool Drop(Carrier carrier, Cargo cargo, int index, PhysGrabCart? cart = null)
    {
        if (cargo.Physics == null) { Remove(carrier, cargo); return false; }
        try
        {
            Vector3 position = cart != null ? PorterCart.DropPosition(cart, index) : DropPosition(carrier, index);
            cargo.Restore(position);
            if (cart != null && cargo.Physics.rb != null && !cargo.Physics.rb.isKinematic)
                cargo.Physics.rb.velocity = PorterCart.Velocity(cart, position);
            _storeBlockedUntil[cargo.Id] = Time.time + 3f;
            Remove(carrier, cargo);
            return true;
        }
        catch (Exception error)
        {
            // Retain the reference and weight if returning the original failed.
            StageRolesPlugin.ModLogger.LogWarning($"Porter will retry returning cargo: {error.Message}");
            return false;
        }
    }

    private void Remove(Carrier carrier, Cargo cargo)
    {
        carrier.Load.Remove(cargo.Id); carrier.Cargo.Remove(cargo); _stored.Remove(cargo.Id);
    }

    private void UpdateSpeed(Carrier carrier) => PorterSpeed.Update(carrier.SteamId,
        carrier.Retiring ? 0 : carrier.Load.Weight, config.PorterCapacity.Value);

    private static Vector3 DropPosition(Carrier carrier, int index)
    {
        Vector3 origin = carrier.LastPosition + Vector3.up * 0.65f;
        float angle = index * 2.3999632f;
        float radius = 0.65f + Math.Min(1f, index * 0.08f);
        Vector3 offset = new(Mathf.Cos(angle) * radius, 0, Mathf.Sin(angle) * radius);
        if (Physics.Raycast(origin, offset.normalized, out RaycastHit hit, offset.magnitude,
            LayerMask.GetMask("Default"), QueryTriggerInteraction.Ignore)) offset = offset.normalized * Math.Max(0, hit.distance - 0.35f);
        return origin + offset;
    }

    private static bool IsDelivery(RoomVolumeCheck? check)
    {
        if (check?.CurrentRooms == null) return false;
        foreach (RoomVolume room in check.CurrentRooms)
            if (room != null && (room.Truck || room.Extraction)) return true;
        return false;
    }

    internal void Stop()
    {
        foreach (string id in new List<string>(_carriers.Keys)) ReleaseAll(id);
        _storeBlockedUntil.Clear();
        _nextScan = 0;
    }
}
