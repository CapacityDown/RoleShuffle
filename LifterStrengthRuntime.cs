using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal bool UsesFixedLifterStrength(PlayerAvatar player) =>
        RoleAssignmentsReady && _config.Enabled.Value &&
        PlayerState.IsLiving(player) && PlayerHasRole(player, StageRole.Lifter);
}

internal static class LifterStrengthRuntime
{
    private static readonly FieldInfo? Tumbling = AccessTools.Field(typeof(PlayerAvatar), "isTumbling");
    private static readonly FieldInfo? GrabberOverride = AccessTools.Field(typeof(PhysGrabber), "overrideGrabStrength");
    private static readonly FieldInfo? GrabDisabled = AccessTools.Field(typeof(PhysGrabObject), "overrideExtraGrabStrengthDisable");
    private static readonly FieldInfo? TorqueDisabled = AccessTools.Field(typeof(PhysGrabObject), "overrideExtraTorqueStrengthDisable");
    private static readonly FieldInfo? GrabTimer = AccessTools.Field(typeof(PhysGrabObject), "overrideGrabStrengthTimer");
    private static readonly FieldInfo? MinGrabTimer = AccessTools.Field(typeof(PhysGrabObject), "overrideMinGrabStrengthTimer");
    private static readonly FieldInfo? TorqueTimer = AccessTools.Field(typeof(PhysGrabObject), "overrideTorqueStrengthTimer");
    private static readonly FieldInfo? MinTorqueTimer = AccessTools.Field(typeof(PhysGrabObject), "overrideMinTorqueStrengthTimer");

    internal static bool FieldsAvailable =>
        Tumbling?.FieldType == typeof(bool) && GrabberOverride?.FieldType == typeof(float) &&
        GrabDisabled?.FieldType == typeof(bool) && TorqueDisabled?.FieldType == typeof(bool) &&
        GrabTimer?.FieldType == typeof(float) && MinGrabTimer?.FieldType == typeof(float) &&
        TorqueTimer?.FieldType == typeof(float) && MinTorqueTimer?.FieldType == typeof(float);

    // Replaces only the two vanilla penalty blends, inside the individual
    // grabber loop. Never changes the shared grabber state or other players.
    internal static float Blend(float raw, float reduced, float blend,
        PhysGrabObject owner, PhysGrabber grabber, bool rotation)
    {
        float vanilla = Mathf.Lerp(raw, reduced, blend);
        if (!RoleOverhaulRules.LifterPhysicsAvailable || owner == null || owner.rb == null ||
            grabber == null || grabber.playerAvatar == null ||
            StageRolesPlugin.Instance?.Controller?.UsesFixedLifterStrength(grabber.playerAvatar) != true)
            return vanilla;

        // Explicit vanilla/other-mod disables and temporary overrides retain
        // precedence. In particular, Lifter cannot cancel tumbling penalties.
        if (Tumbling!.GetValue(grabber.playerAvatar) is true ||
            (rotation ? TorqueDisabled : GrabDisabled)!.GetValue(owner) is true ||
            (float)(rotation ? TorqueTimer : GrabTimer)!.GetValue(owner)! > 0f ||
            (float)(rotation ? MinTorqueTimer : MinGrabTimer)!.GetValue(owner)! > 0f ||
            (float)GrabberOverride!.GetValue(grabber)! != -1f ||
            Math.Abs(raw - grabber.grabStrength) > 0.0001f)
            return vanilla;

        // Exact absolute coefficient, independent of Base/native level. The
        // rotation result is an input to vanilla torque, not final torque.
        return (float)RoleOverhaulRules.LifterEffectiveStrength(owner.rb.mass < 2f, rotation);
    }
}
