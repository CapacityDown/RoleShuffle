using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

// Let native animation, expiry, destruction and puzzle bookkeeping keep running.
// Gate the actual activation paths separately from their cleanup paths.
[HarmonyPatch]
internal static class EngineerModernFramePatch
{
    private static IEnumerable<MethodBase> TargetMethods()
    {
        var names = new List<string>(EngineerEffectCatalog.ModernEffectTypeNames) { "EnemyBangFuse" };
        foreach (string name in names)
        {
            Type? type = AccessTools.TypeByName(name);
            if (type == null) continue;
            foreach (string frame in new[] { "Update", "FixedUpdate" })
            {
                MethodInfo? method = EngineerEffectCatalog.FindDeclaredInstanceMethod(type, frame, Type.EmptyTypes);
                if (method != null) yield return method;
            }
        }
    }

    [HarmonyPrefix]
    internal static bool Prefix(Component __instance, out bool __state)
    {
        __state = false;
        if (__instance is EnemyBangFuse { standalone: false }) return true;
        bool suppress = EngineerModernEffects.Suppressed(__instance);
        // Native detachment calls EnemyDirector directly instead of its own helper.
        __state = suppress && __instance is ValuableMusicInstrument;
        if (__state) EngineerModernEffects.QuietInstrumentDepth++;
        return EngineerModernEffects.BeforeFrame(__instance, suppress);
    }

    [HarmonyFinalizer]
    internal static void Finalizer(bool __state)
    {
        if (__state) EngineerModernEffects.QuietInstrumentDepth--;
    }
}

[HarmonyPatch]
internal static class EngineerModernActionPatch
{
    internal static readonly (string TypeName, string MethodName)[] Targets =
    {
        ("SnakeInACanValuable", "SnakeOut"),
        ("ValuableDesertBirdCage", "StateIdle"),
        ("ValuableDesertBirdCage", "StateDead"),
        ("ValuableDesertBirdCage", "ChirpSet"),
        ("ValuableDesertEnergyCrystalChunk", "StateIdle"),
        ("ValuableDesertEnergyCrystalChunk", "EnergizeLogic"),
        ("ValuableDesertEnergyCrystalChunk", "BreakImpulseChargeCloud"),
        ("ValuableDesertEnergyCrystalChunk", "ChargeItemsInRange"),
        ("ValuableDesertExplosiveTower", "StateTrigger"),
        ("ValuableDesertExplosiveTower", "StateExplode"),
        ("ValuableDesertExplosiveTowerDynamite", "Explode"),
        ("ValuableDesertGolfcart", "FixedUpdateActive"),
        ("ValuableDesertGolfcart", "NeighOnBreak"),
        ("ValuableDesertPopcornStand", "RevLogic"),
        ("ValuableDesertPopcornStand", "InvestigateLogic"),
        ("ValuableDesertZeroGravCrystal", "AffectSpawnLogic"),
        ("ValuableDesertZeroGravCrystal", "AffectRenewLogic"),
        ("ValuableDesertZeroGravCrystal", "PlayerAffectStart"),
        ("ValuableDesertZeroGravCrystal", "CrystalAffectStart"),
        ("ValuableDesertZeroGravCrystal", "GrabberBeamsLogic"),
        ("ValuableMusicInstrument", "EnemySetInvestigate"),
        ("ValuableWizardCrystal", "Destroyed"),
        ("ValuableCubeOfKnowledge", "TalkInvestigateLogic"),
        ("ValuableCubeOfKnowledge", "TalkPhysics"),
        ("ValuableCubeOfKnowledge", "TalkAudioLogic"),
        ("ValuableCubeOfKnowledge", "SoundLoopLogic"),
        ("ValuableCubeOfKnowledge", "ItemRejectPhysics"),
        ("ValuableCubeOfKnowledge", "ItemRejectExisting"),
        ("ValuableCubeOfKnowledge", "ConnectionImpactApply"),
        ("ValuableCubeOfKnowledge", "PlatformAlignmentFixedUpdate"),
        ("ValuableCubeOfKnowledge", "AwakenedFloatFixedUpdate"),
        ("ValuableCubeOfKnowledge", "ActivationHoldLogic"),
        ("ValuableCubeOfKnowledge", "FinalBuildShakeLogic")
    };

    internal static IEnumerable<MethodBase> Resolve((string TypeName, string MethodName)[] targets)
    {
        foreach ((string typeName, string methodName) in targets)
        {
            Type? type = AccessTools.TypeByName(typeName);
            MethodInfo? method = type == null ? null : EngineerEffectCatalog.FindDeclaredInstanceMethod(type, methodName);
            if (method == null) throw new MissingMethodException(typeName, methodName);
            yield return method;
        }
    }

    private static IEnumerable<MethodBase> TargetMethods() => Resolve(Targets);
    [HarmonyPrefix]
    internal static bool Prefix(Component __instance) => !EngineerModernEffects.Suppressed(__instance);
}

[HarmonyPatch]
internal static class EngineerModernConditionPatch
{
    internal static readonly (string TypeName, string MethodName)[] Targets =
    {
        ("ValuableDesertExplosiveTower", "GrabberTriggerCheck"),
        ("ValuableDesertExplosiveTower", "CollisionTriggerCheck"),
        ("ValuableMusicInstrument", "IsGrabbed"),
        ("ValuableCubeOfKnowledge", "SideActivate"),
        ("ValuableCubeOfKnowledge", "CanConnectGet"),
        ("ValuableCubeOfKnowledge", "ItemReject"),
        ("ValuableCubeOfKnowledge", "ItemRejectActive")
    };
    private static IEnumerable<MethodBase> TargetMethods() => EngineerModernActionPatch.Resolve(Targets);
    [HarmonyPrefix]
    internal static bool Prefix(Component __instance, ref bool __result)
    {
        if (!EngineerModernEffects.Suppressed(__instance)) return true;
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(ValuableDesertGolfcart), "Driver")]
internal static class EngineerGolfcartDriverPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(ValuableDesertGolfcart __instance, ref PlayerAvatar? __result)
    {
        if (!EngineerModernEffects.Suppressed(__instance)) return true;
        __result = null;
        return false;
    }
}

[HarmonyPatch(typeof(EnemyDirector), nameof(EnemyDirector.SetInvestigate))]
internal static class EngineerInstrumentNoisePatch
{
    [HarmonyPrefix]
    internal static bool Prefix() => EngineerModernEffects.QuietInstrumentDepth == 0;
}
