using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal int LifterItemLevel => _config.LifterLightItemStrengthLevel.Value;
    internal bool UsesLifterHandling(PlayerAvatar player) =>
        RoleAssignmentsReady && _config.Enabled.Value &&
        PlayerState.IsLiving(player) && PlayerHasRole(player, StageRole.Lifter);
}

internal static class LifterStrengthRuntime
{
    private static readonly FieldInfo? Gun = AccessTools.Field(typeof(PhysGrabObject), "isGun");
    private static readonly FieldInfo? Melee = AccessTools.Field(typeof(PhysGrabObject), "isMelee");
    private static readonly FieldInfo? Cart = AccessTools.Field(typeof(PhysGrabObject), "isCart");

    internal static bool FieldsAvailable =>
        Gun?.FieldType == typeof(bool) && Melee?.FieldType == typeof(bool) && Cart?.FieldType == typeof(bool) &&
        LifterMeleeHoldingPatch.FieldsAvailable;

    private static bool IsHeavyEquipment(PhysGrabObject owner) =>
        Cart!.GetValue(owner) is true || owner.GetComponent<ItemVehicle>() != null ||
        owner.GetComponent<ValuableArcticSnowBike>() != null;

    private static bool IsLightHandlingItem(PhysGrabObject owner) =>
        !IsHeavyEquipment(owner) && ((owner.rb != null && owner.rb.mass < 2f) || Gun!.GetValue(owner) is true || Melee!.GetValue(owner) is true ||
        owner.GetComponent<ItemAttributes>() != null);

    // Light valuables and shop items other than carts/bikes use the configured
    // holding Strength (default level 1). Supply it
    // before vanilla applies those overrides, so their built-in behavior stays
    // intact without receiving the role's level-200 input or heavy-object boost.
    // This changes a local value, never the shared grabber or stored upgrade.
    internal static float NativeStrengthInput(float raw, PhysGrabObject owner, PhysGrabber grabber) =>
        RoleOverhaulRules.LifterPhysicsAvailable && owner != null &&
        grabber != null && grabber.playerAvatar != null &&
        StageRolesPlugin.Instance?.Controller?.UsesLifterHandling(grabber.playerAvatar) == true &&
        IsLightHandlingItem(owner)
            ? 1f + 0.2f * StageRolesPlugin.Instance.Controller.LifterItemLevel : raw;

}

[HarmonyPatch]
internal static class LifterMeleeHoldingPatch
{
    private static readonly FieldInfo? MeleeObject = AccessTools.Field(typeof(ItemMelee), "physGrabObject");
    [ThreadStatic] private static ItemMelee? _holdingMelee;
    internal static bool FieldsAvailable => MeleeObject?.FieldType == typeof(PhysGrabObject);

    // Only the native holding-override calculation gets the configured item bonus.
    // Swing forces and cooldowns use the same native helper outside this scope
    // and must continue to receive their original Strength bonus.
    [HarmonyPrefix, HarmonyPatch(typeof(ItemMelee), "GrabOverridesLogic")]
    internal static void BeginHolding(ItemMelee __instance, out ItemMelee? __state)
    {
        __state = _holdingMelee;
        _holdingMelee = __instance;
    }

    [HarmonyFinalizer, HarmonyPatch(typeof(ItemMelee), "GrabOverridesLogic")]
    internal static void EndHolding(ItemMelee? __state) => _holdingMelee = __state;

    [HarmonyPrefix, HarmonyPatch(typeof(ItemMelee), "MeleeStrengthBonus")]
    internal static bool HoldingBonus(ItemMelee __instance, float __0, ref float __result)
    {
        if (!RoleOverhaulRules.LifterPhysicsAvailable || !ReferenceEquals(_holdingMelee, __instance) ||
            MeleeObject!.GetValue(__instance) is not PhysGrabObject owner ||
            owner.playerGrabbing.Count == 0)
            return true;

        // Vanilla derives the object's override from its first grabber only.
        PhysGrabber grabber = owner.playerGrabbing[0];
        if (grabber == null || grabber.playerAvatar == null ||
            StageRolesPlugin.Instance?.Controller?.UsesLifterHandling(grabber.playerAvatar) != true)
            return true;

        float level = StageRolesPlugin.Instance.Controller.LifterItemLevel;
        __result = level / (level + __0);
        return false;
    }
}
