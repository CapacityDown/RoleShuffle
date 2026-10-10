using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class DualWielderRuntime(StageRolesConfig config)
{
    private readonly Dictionary<string, DualWielderEcho> _echoes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> _retryAt = new(StringComparer.Ordinal);

    internal void Tick(IReadOnlyList<RoleAssignment> assignments)
    {
        HashSet<string> active = new(StringComparer.Ordinal);
        foreach (RoleAssignment assignment in assignments)
        {
            if (!config.Enabled.Value || !PlayerState.IsLiving(assignment.Player) ||
                !RoleCatalog.HasCapability(assignment.Role, StageRole.DualWielder)) continue;
            string id = assignment.SteamId;
            ItemMelee? source = HeldMelee(assignment.Player);
            if (source == null) continue;
            active.Add(id);
            if (_echoes.TryGetValue(id, out DualWielderEcho? prior) &&
                (prior == null || prior.Source != source || prior.Owner != assignment.Player)) Release(id);
            if (_echoes.TryGetValue(id, out DualWielderEcho? echo) && echo != null) continue;
            if (_retryAt.TryGetValue(id, out float retry) && Time.time < retry) continue;
            _retryAt[id] = Time.time + 2f;
            try
            {
                ItemAttributes? item = source.GetComponent<ItemAttributes>();
                PrefabRef? prefab = item?.item?.prefab;
                if (prefab == null || !prefab.IsValid() || string.IsNullOrEmpty(prefab.ResourcePath) ||
                    prefab.Prefab == null || prefab.Prefab.GetComponent<ItemMelee>() == null) continue;
                Vector3 position = source.transform.position - Right(assignment.Player) * config.DualWielderLeftOffset.Value;
                GameObject instance = SemiFunc.IsMultiplayer()
                    ? PhotonNetwork.Instantiate(prefab.ResourcePath, position, source.transform.rotation)
                    : UnityEngine.Object.Instantiate(prefab.Prefab, position, source.transform.rotation);
                // Keep custody immediately, so even a partial initialization is cleaned up.
                echo = instance.AddComponent<DualWielderEcho>();
                _echoes[id] = echo;
                echo.Initialize(assignment.Player, source, config);
                _retryAt.Remove(id);
            }
            catch (Exception error)
            {
                Release(id);
                _retryAt[id] = Time.time + 2f;
                StageRolesPlugin.ModLogger.LogWarning($"DualWielder copy unavailable: {error.Message}");
            }
        }
        foreach (string id in new List<string>(_echoes.Keys))
            if (!active.Contains(id)) Release(id);
    }

    internal static ItemMelee? HeldMelee(PlayerAvatar player)
    {
        Transform? held = player.physGrabber?.grabbedObjectTransform;
        PhysGrabObject? physics = held != null ? held.GetComponent<PhysGrabObject>() : null;
        if (physics == null || physics.GetComponent<DualWielderEcho>() != null ||
            physics.playerGrabbing.Count != 1 || physics.playerGrabbing[0] != player.physGrabber) return null;
        ItemMelee? melee = physics.GetComponent<ItemMelee>();
        return melee != null && !melee.swingLogicOnly && physics.GetComponent<ItemAttributes>() != null ? melee : null;
    }

    internal static Vector3 Right(PlayerAvatar player)
    {
        Vector3 right = Vector3.Cross(Vector3.up, MageSpellAim.GetDirection(player));
        return right.sqrMagnitude > 0.001f ? right.normalized : player.transform.right;
    }

    internal void Release(string id)
    {
        if (!_echoes.TryGetValue(id, out DualWielderEcho? echo)) return;
        if (echo != null) echo.Close();
        _echoes.Remove(id);
    }

    internal void Stop()
    {
        foreach (string id in new List<string>(_echoes.Keys)) Release(id);
        _retryAt.Clear();
    }
}

[DefaultExecutionOrder(10000)]
internal sealed class DualWielderEcho : MonoBehaviour
{
    internal static readonly FieldInfo? Hurt = AccessTools.Field(typeof(ItemMelee), "hurtCollider");
    internal static readonly FieldInfo? Battery = AccessTools.Field(typeof(ItemMelee), "itemBattery");
    private static readonly FieldInfo? HitTimer = AccessTools.Field(typeof(ItemMelee), "hitTimer");
    private static readonly FieldInfo? Attacker = AccessTools.Field(typeof(HurtCollider), "playerCausingHurtOverride");
    private static readonly FieldInfo? InstanceName = AccessTools.Field(typeof(ItemAttributes), "instanceName");
    private static readonly MethodInfo? Timers = AccessTools.Method(typeof(ItemMelee), "TimersTick");
    internal PlayerAvatar Owner { get; private set; } = null!;
    internal ItemMelee Source { get; private set; } = null!;
    private StageRolesConfig _config = null!;
    private ItemMelee _copy = null!;
    private PhysGrabObject _physics = null!;
    private Rigidbody _body = null!;
    private HurtCollider? _hurt;
    private readonly List<Collider> _solidColliders = new();
    private readonly DualWielderHistory _history = new();
    private bool _ready, _closed;
    private float _delay, _offset, _destroyRetryAt;

    internal void Initialize(PlayerAvatar owner, ItemMelee source, StageRolesConfig config)
    {
        Owner = owner; Source = source; _config = config;
        _copy = GetComponent<ItemMelee>(); _physics = GetComponent<PhysGrabObject>(); _body = GetComponent<Rigidbody>();
        if (_copy == null || _physics == null || _body == null || Hurt?.FieldType != typeof(HurtCollider) ||
            Battery?.FieldType != typeof(ItemBattery) || HitTimer?.FieldType != typeof(float) ||
            Attacker?.FieldType != typeof(PlayerAvatar) || InstanceName?.FieldType != typeof(string) || Timers == null)
            throw new NotSupportedException("Native melee contract changed.");
        foreach (HurtCollider hurt in GetComponentsInChildren<HurtCollider>(true)) hurt.enabled = false;
        foreach (Collider collider in GetComponentsInChildren<Collider>(true))
            if (collider.GetComponentInParent<HurtCollider>(true) == null) _solidColliders.Add(collider);
        ItemBattery? duplicateBattery = GetComponent<ItemBattery>();
        if (duplicateBattery != null) duplicateBattery.enabled = false;
        _delay = config.DualWielderDelaySeconds.Value;
        _offset = config.DualWielderLeftOffset.Value;
        _ready = true;
        HoldPhysics();
    }

    internal bool BindSharedItemName(ItemAttributes item, int viewId)
    {
        if (Source == null || Source.GetComponent<ItemAttributes>() is not ItemAttributes original ||
            InstanceName?.GetValue(original) is not string name || string.IsNullOrEmpty(name)) return false;
        // Reuse the native name without ItemAdd/ItemRemove or changing purchased counts.
        PunManager.instance.SetItemName(name, item, viewId);
        return true;
    }

    internal void TickMeleeTimers()
    {
        if (_ready && !_closed && _copy != null) Timers?.Invoke(_copy, null);
    }

    private bool Held() => _ready && !_closed && Source != null && Owner != null &&
        PlayerState.IsLiving(Owner) && DualWielderRuntime.HeldMelee(Owner) == Source;

    private void LateUpdate()
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        if (!Held()) { Close(); return; }
        try
        {
            // Native Start initializes melee callbacks; never activate its hitbox before that.
            if (Hurt!.GetValue(Source) is not HurtCollider original || Hurt.GetValue(_copy) is not HurtCollider hurt) return;
            _hurt = hurt;
            ItemBattery? sourceBattery = Source.GetComponent<ItemBattery>();
            if (Source.usesBatteryLogic && sourceBattery == null) { Close(); return; }
            Battery!.SetValue(_copy, sourceBattery);
            _copy.usesBatteryLogic = Source.usesBatteryLogic;
            if (!_hurt.ignorePlayers.Contains(Owner)) _hurt.ignorePlayers.Add(Owner);
            PhysGrabObject originalPhysics = Source.GetComponent<PhysGrabObject>();
            if (!_hurt.ignoreObjects.Contains(originalPhysics)) _hurt.ignoreObjects.Add(originalPhysics);
            Attacker!.SetValue(_hurt, Owner);
            if (_delay != _config.DualWielderDelaySeconds.Value || _offset != _config.DualWielderLeftOffset.Value)
            {
                _history.Clear(); _delay = _config.DualWielderDelaySeconds.Value; _offset = _config.DualWielderLeftOffset.Value;
            }
            bool charged = !Source.usesBatteryLogic || sourceBattery!.batteryLife > 0;
            _history.Record(new DualWielderPose(Time.time,
                Source.transform.position - DualWielderRuntime.Right(Owner) * _offset, Source.transform.rotation,
                original.transform.rotation, charged && original.isActiveAndEnabled,
                original.enemyDamage, original.playerDamage));
            HoldPhysics();
        }
        catch (Exception error)
        {
            StageRolesPlugin.ModLogger.LogWarning($"DualWielder copy stopped: {error.Message}");
            Close();
        }
    }

    private void FixedUpdate()
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        if (!Held()) { Close(); return; }
        if (_hurt == null) return;
        if (!_history.Sample(Time.time - _delay, out DualWielderPose pose)) { _hurt.gameObject.SetActive(false); return; }
        HoldPhysics();
        _body.position = pose.Position; _body.rotation = pose.Rotation;
        transform.SetPositionAndRotation(pose.Position, pose.Rotation);
        _hurt.transform.rotation = pose.HitRotation;
        _hurt.enemyDamage = pose.EnemyDamage; _hurt.playerDamage = pose.PlayerDamage;
        ItemBattery? battery = Source.GetComponent<ItemBattery>();
        bool charged = !Source.usesBatteryLogic || (battery != null && battery.batteryLife > 0);
        bool active = pose.Attacking && charged && !(HitTimer!.GetValue(_copy) is float remaining && remaining > 0);
        _hurt.enabled = true;
        if (_hurt.gameObject.activeSelf != active) _hurt.gameObject.SetActive(active);
    }

    private void HoldPhysics()
    {
        foreach (Collider collider in _solidColliders) if (collider != null) collider.enabled = false;
        _body.useGravity = false;
        _physics.OverrideKinematic(1f);
        _physics.OverrideGrabDisable(1f);
        _physics.OverrideIndestructible(1f);
        _physics.DisableDeathPitEffect(1f);
    }

    internal void Close()
    {
        if (!_closed)
        {
            _closed = true;
            foreach (HurtCollider hurt in GetComponentsInChildren<HurtCollider>(true)) hurt.enabled = false;
            ItemAttributes? item = GetComponent<ItemAttributes>();
            if (item != null && ItemManager.instance != null) ItemManager.instance.spawnedItems.Remove(item);
            _history.Clear();
        }
        if (Time.time < _destroyRetryAt) return;
        try
        {
            PhotonView? view = GetComponent<PhotonView>();
            if (SemiFunc.IsMultiplayer() && PhotonNetwork.InRoom && view != null && view.ViewID != 0) PhotonNetwork.Destroy(gameObject);
            else UnityEngine.Object.Destroy(gameObject);
        }
        catch (Exception error)
        {
            // Disabled copies retry from their own updates, even after the role stops tracking them.
            _destroyRetryAt = Time.time + 1f;
            StageRolesPlugin.ModLogger.LogWarning($"DualWielder cleanup will retry: {error.Message}");
        }
    }
}

// All hooks apply only to generated copies on the authority. Normal weapons are untouched.
[HarmonyPatch]
internal static class DualWielderPatches
{
    private static DualWielderEcho? Echo(Component instance) =>
        SemiFunc.IsMasterClientOrSingleplayer() ? instance.GetComponent<DualWielderEcho>() : null;

    [HarmonyPrefix, HarmonyPatch(typeof(ItemMelee), "Update")]
    private static bool MeleeUpdate(ItemMelee __instance)
    {
        DualWielderEcho? echo = Echo(__instance);
        if (echo == null) return true;
        echo.TickMeleeTimers(); return false;
    }
    [HarmonyPrefix, HarmonyPatch(typeof(ItemMelee), "FixedUpdate")]
    private static bool MeleeFixed(ItemMelee __instance) => Echo(__instance) == null;

    [HarmonyPrefix, HarmonyPatch(typeof(ItemManager), nameof(ItemManager.AddSpawnedItem))]
    private static bool SaveList(ItemAttributes __0) => Echo(__0) == null;

    [HarmonyPrefix, HarmonyPatch(typeof(StatsManager), nameof(StatsManager.ItemFetchName))]
    private static bool Name(ItemAttributes __1, int __2)
    {
        DualWielderEcho? echo = Echo(__1);
        if (echo == null) return true;
        if (!echo.BindSharedItemName(__1, __2)) echo.Close();
        return false;
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ItemEquippable), "RPC_RequestEquip")]
    private static bool Equip(ItemEquippable __instance) => Echo(__instance) == null;

    [HarmonyPrefix, HarmonyPatch(typeof(PhysGrabObject), "GrabLinkRPC")]
    private static bool GrabLink(PhysGrabObject __instance, int __0) => RejectGrab(__instance, __0);
    [HarmonyPrefix, HarmonyPatch(typeof(PhysGrabObject), "GrabStartedRPC")]
    private static bool GrabStart(PhysGrabObject __instance, int __0) => RejectGrab(__instance, __0);
    [HarmonyPrefix, HarmonyPatch(typeof(PhysGrabObject), "GrabPlayerAddRPC")]
    private static bool GrabAdd(PhysGrabObject __instance, int __0) => RejectGrab(__instance, __0);
    private static bool RejectGrab(PhysGrabObject __instance, int __0)
    {
        if (Echo(__instance) == null) return true;
        PhysGrabber? grabber = PhotonView.Find(__0)?.GetComponent<PhysGrabber>();
        if (grabber != null) grabber.OverrideGrabRelease(__instance.GetComponent<PhotonView>().ViewID, 0.5f);
        return false;
    }
}
