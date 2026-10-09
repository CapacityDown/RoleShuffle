using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyPatch]
internal static class TwinsPatches
{
    private static StageRoleController? Controller => StageRolesPlugin.Instance?.Controller;
    private static readonly FieldInfo? GrabTarget = AccessTools.Field(typeof(PlayerHealthGrab), "playerAvatar");
    private static readonly FieldInfo? StaticGrab = AccessTools.Field(typeof(PlayerHealthGrab), "staticGrabObject");
    [ThreadStatic] private static PhysGrabObjectImpactDetector? _collision;

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    private static void BeforeHurt(PlayerHealth __instance, out int __state) => BeforeLocalHealth(__instance, out __state);

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerHealth), "Heal")]
    private static void BeforeHeal(PlayerHealth __instance, out int __state) => BeforeLocalHealth(__instance, out __state);

    private static void BeforeLocalHealth(PlayerHealth __instance, out int __state)
    {
        __state = -1;
        PlayerAvatar player = __instance.GetComponent<PlayerAvatar>();
        if (Controller?.RoleAssignmentsReady != true || Controller.WritingTwinsHealth ||
            !Controller.IsTwin(player) || (SemiFunc.IsMultiplayer() && player.photonView?.IsMine != true)) return;
        if (PlayerState.TryGetCurrentHealth(player, out int health)) __state = health;
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    private static void AfterHurt(PlayerHealth __instance, int __state) => Observe(__instance, __state);

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerHealth), "Heal")]
    private static void AfterHeal(PlayerHealth __instance, int __state) => Observe(__instance, __state);

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]
    private static void BeforeRemoteHealth(PlayerHealth __instance, PhotonMessageInfo _info, out int __state) =>
        __state = TwinsHealthPackets.Previous(__instance, _info);

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]
    private static void AfterRemoteHealth(PlayerHealth __instance, int __state) => Observe(__instance, __state);

    private static void Observe(PlayerHealth health, int previous)
    {
        if (previous < 0) return;
        PlayerAvatar player = health.GetComponent<PlayerAvatar>();
        if (PlayerState.TryGetCurrentHealth(player, out int current) && PlayerState.TryGetMaximumHealth(player, out int maximum))
            Controller?.ObserveTwinsHealth(player, previous, current, maximum);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeathRPC))]
    private static void Death(PlayerAvatar __instance) => Controller?.TwinDied(__instance);

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ReviveRPC))]
    private static void Revive(PlayerAvatar __instance) => Controller?.TwinRevived(__instance);

    [HarmonyPrefix, HarmonyPatch(typeof(PlayerHealthGrab), "Update")]
    private static void BeforeTransfer(PlayerHealthGrab __instance, out List<PhysGrabber>? __state)
    {
        __state = null;
        if (Controller?.RoleAssignmentsReady != true || GrabTarget?.GetValue(__instance) is not PlayerAvatar target ||
            !Controller.IsTwin(target) || StaticGrab?.GetValue(__instance) is not StaticGrabObject grab) return;
        foreach (PhysGrabber holder in grab.playerGrabbing)
            if (holder != null && Controller.IsTwin(holder.playerAvatar)) (__state ??= new()).Add(holder);
        if (__state != null) foreach (PhysGrabber holder in __state) grab.playerGrabbing.Remove(holder);
    }

    [HarmonyFinalizer, HarmonyPatch(typeof(PlayerHealthGrab), "Update")]
    private static void AfterTransfer(PlayerHealthGrab __instance, List<PhysGrabber>? __state)
    {
        if (__state == null || StaticGrab?.GetValue(__instance) is not StaticGrabObject grab) return;
        foreach (PhysGrabber holder in __state)
            if (holder != null && !grab.playerGrabbing.Contains(holder)) grab.playerGrabbing.Add(holder);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnCollisionStay")]
    private static void BeforeCollision(PhysGrabObjectImpactDetector __instance, out PhysGrabObjectImpactDetector? __state)
    { __state = _collision; _collision = __instance; }

    [HarmonyFinalizer, HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "OnCollisionStay")]
    private static void AfterCollision(PhysGrabObjectImpactDetector? __state) => _collision = __state;

    [HarmonyPrefix, HarmonyPatch(typeof(PhysGrabObjectImpactDetector), "Break")]
    private static void CollisionLoss(PhysGrabObjectImpactDetector __instance, ref float __0)
    {
        if (_collision != __instance || Controller?.RoleAssignmentsReady != true) return;
        PhysGrabObject item = __instance.GetComponent<PhysGrabObject>();
        if (item != null) __0 *= Controller.TwinsCollisionMultiplier(item);
    }
}

// Outside the Harmony patch class: this is an ordinary value inspection helper.
internal static class TwinsHealthPackets
{
    internal static int Previous(PlayerHealth instance, PhotonMessageInfo info)
    {
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        PlayerAvatar player = instance.GetComponent<PlayerAvatar>();
        var sender = info.Sender;
        if (controller?.RoleAssignmentsReady != true || controller.WritingTwinsHealth ||
            !controller.IsTwin(player) || sender == null ||
            (sender != player.photonView?.Owner && !sender.IsMasterClient)) return -1;
        return PlayerState.TryGetCurrentHealth(player, out int health) ? health : -1;
    }
}
