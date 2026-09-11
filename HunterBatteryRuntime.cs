using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal readonly struct HunterBatteryState
{
    internal HunterBatteryState(ItemBattery? battery, float batteryLife, bool active)
    {
        Battery = battery;
        BatteryLife = batteryLife;
        Active = active;
    }

    internal ItemBattery? Battery { get; }
    internal float BatteryLife { get; }
    internal bool Active { get; }
}

internal static class HunterBatteryRuntime
{
    private static readonly Dictionary<int, float> FullBarSavings = new();
    private static readonly FieldInfo? LastPlayerGrabbingField =
        AccessTools.Field(typeof(PhysGrabObject), "lastPlayerGrabbing");
    private static bool _failureLogged;

    internal static void Clear()
    {
        FullBarSavings.Clear();
        _failureLogged = false;
    }

    internal static bool TryScaleDrain(ItemBattery battery, ref float amount)
    {
        try
        {
            if (!IsHunterWeapon(battery))
            {
                return false;
            }
            amount *= ConsumptionFactor;
            return true;
        }
        catch (Exception exception)
        {
            LogFailure("battery drain", exception);
            return false;
        }
    }

    internal static void ScaleFullBarDrain(ItemBattery battery, ref int bars)
    {
        try
        {
            if (!IsHunterWeapon(battery) || bars <= 0)
            {
                return;
            }

            int instanceId = battery.GetInstanceID();
            float savedBars = FullBarSavings.GetValueOrDefault(instanceId, 0f) +
                bars * (1f - ConsumptionFactor);
            int wholeBarsSaved = Mathf.FloorToInt(savedBars + 0.0001f);
            if (wholeBarsSaved > 0)
            {
                bars = Mathf.Max(0, bars - wholeBarsSaved);
                savedBars -= wholeBarsSaved;
            }
            FullBarSavings[instanceId] = Mathf.Max(0f, savedBars);
        }
        catch (Exception exception)
        {
            LogFailure("full-bar drain", exception);
        }
    }

    internal static HunterBatteryState Capture(Component source)
    {
        try
        {
            ItemBattery? battery = FindBattery(source);
            return battery != null && IsHunterWeapon(battery)
                ? new HunterBatteryState(battery, battery.batteryLife, active: true)
                : default;
        }
        catch (Exception exception)
        {
            LogFailure("consumption capture", exception);
            return default;
        }
    }

    internal static void Refund(HunterBatteryState state)
    {
        try
        {
            ItemBattery? battery = state.Battery;
            if (!state.Active || battery == null)
            {
                return;
            }

            float consumed = state.BatteryLife - battery.batteryLife;
            if (consumed <= 0f)
            {
                return;
            }
            battery.batteryLife = Mathf.Clamp(
                battery.batteryLife + consumed * (1f - ConsumptionFactor),
                0f,
                100f);
        }
        catch (Exception exception)
        {
            LogFailure("consumption refund", exception);
        }
    }

    internal static bool IsHunterWeapon(ItemBattery? battery)
    {
        if (battery == null || !SemiFunc.IsMasterClientOrSingleplayer() ||
            !IsWeapon(battery))
        {
            return false;
        }

        PlayerAvatar? owner = ResolveOwner(battery);
        return owner != null &&
               StageRolesPlugin.Instance?.Controller?.PlayerHasRole(
                   owner,
                   StageRole.Hunter) == true;
    }

    private static float ConsumptionFactor =>
        StageRolesPlugin.Instance?.Settings?.ClampedHunterBatteryConsumption ?? 1f;

    private static ItemBattery? FindBattery(Component source) =>
        source.GetComponent<ItemBattery>() ??
        source.GetComponentInChildren<ItemBattery>() ??
        source.GetComponentInParent<ItemBattery>();

    private static PlayerAvatar? ResolveOwner(ItemBattery battery)
    {
        ItemEquippable? equippable = battery.GetComponentInParent<ItemEquippable>();
        PlayerAvatar? owner = equippable?.GetOwnerPlayerAvatar();
        if (owner != null)
        {
            return owner;
        }

        PhysGrabObject? physGrabObject = battery.GetComponentInParent<PhysGrabObject>();
        return physGrabObject != null
            ? LastPlayerGrabbingField?.GetValue(physGrabObject) as PlayerAvatar
            : null;
    }

    private static void LogFailure(string operation, Exception exception)
    {
        if (_failureLogged)
        {
            return;
        }
        _failureLogged = true;
        StageRolesPlugin.ModLogger.LogWarning(
            $"Hunter {operation} handling was disabled for this attempt so vanilla weapon behavior could continue: {exception.Message}");
    }

    private static bool IsWeapon(ItemBattery battery) =>
        battery.GetComponentInParent<ItemGun>() != null ||
        battery.GetComponentInParent<ItemMelee>() != null ||
        battery.GetComponentInParent<ItemLeafBlower>() != null ||
        battery.GetComponentInParent<ItemCartCannon>() != null ||
        battery.GetComponentInParent<ItemCartLaser>() != null ||
        battery.GetComponentInParent<ItemStaffTorque>() != null ||
        battery.GetComponentInParent<ItemStaffVoid>() != null ||
        battery.GetComponentInParent<ItemStaffZeroGravity>() != null;
}

[HarmonyPatch]
internal static class HunterBatteryPatches
{
    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemBattery), nameof(ItemBattery.Drain))]
    private static void ItemBatteryDrainPrefix(ItemBattery __instance, ref float __0)
    {
        HunterBatteryRuntime.TryScaleDrain(__instance, ref __0);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemBattery), nameof(ItemBattery.RemoveFullBar))]
    private static void ItemBatteryRemoveFullBarPrefix(ItemBattery __instance, ref int __0)
    {
        HunterBatteryRuntime.ScaleFullBarDrain(__instance, ref __0);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemGun), nameof(ItemGun.ShootRPC))]
    private static void ItemGunShootRpcPrefix(ItemGun __instance, out HunterBatteryState __state)
    {
        __state = __instance.batteryDrainFullBar
            ? default
            : HunterBatteryRuntime.Capture(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemGun), nameof(ItemGun.ShootRPC))]
    private static void ItemGunShootRpcPostfix(HunterBatteryState __state)
    {
        HunterBatteryRuntime.Refund(__state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.EnemyOrPVPSwingHitRPC))]
    private static void ItemMeleeEnemyHitPrefix(ItemMelee __instance, out HunterBatteryState __state)
    {
        __state = HunterBatteryRuntime.Capture(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.EnemyOrPVPSwingHitRPC))]
    private static void ItemMeleeEnemyHitPostfix(HunterBatteryState __state)
    {
        HunterBatteryRuntime.Refund(__state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.SwingHitRPC))]
    private static void ItemMeleeSwingHitPrefix(ItemMelee __instance, out HunterBatteryState __state)
    {
        __state = HunterBatteryRuntime.Capture(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(ItemMelee), nameof(ItemMelee.SwingHitRPC))]
    private static void ItemMeleeSwingHitPostfix(HunterBatteryState __state)
    {
        HunterBatteryRuntime.Refund(__state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ItemLeafBlower), nameof(ItemLeafBlower.BatteryDrain))]
    private static void ItemLeafBlowerBatteryDrainPrefix(
        ItemLeafBlower __instance,
        ref float __0)
    {
        ItemBattery? battery = __instance.GetComponent<ItemBattery>() ??
            __instance.GetComponentInChildren<ItemBattery>() ??
            __instance.GetComponentInParent<ItemBattery>();
        if (battery != null)
        {
            HunterBatteryRuntime.TryScaleDrain(battery, ref __0);
        }
    }
}
