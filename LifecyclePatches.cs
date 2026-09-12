using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

[HarmonyAfter(StageRolesPlugin.StageFluxGuid)]
internal static class LifecyclePatches
{
    private static readonly FieldInfo? HurtColliderPlayerField =
        AccessTools.Field(typeof(HurtCollider), "playerCausingHurt");

    private sealed class EnemyHitOverrideState
    {
        internal EnemyHitOverrideState(float enemyStunTime, int enemyDamage)
        {
            EnemyStunTime = enemyStunTime;
            EnemyDamage = enemyDamage;
        }

        private float EnemyStunTime { get; }
        private int EnemyDamage { get; }

        internal void Restore(HurtCollider collider)
        {
            collider.enemyStunTime = EnemyStunTime;
            collider.enemyDamage = EnemyDamage;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(LevelGenerator), "GenerateDone")]
    private static void LevelGeneratorGenerateDonePostfix(LevelGenerator __instance)
    {
        StageRolesPlugin.Instance?.BaseUpgradeDraw?.LevelReady();
        StageRolesPlugin.Instance?.Controller?.StageReady(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RoundDirector), nameof(RoundDirector.ExtractionCompleted))]
    private static void RoundDirectorExtractionCompletedPostfix(RoundDirector __instance)
    {
        try
        {
            StageRolesPlugin.Instance?.Controller?.ExtractionCompleted(__instance);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogError(
                $"Role extraction recovery was skipped without interrupting " +
                $"the vanilla extraction: {exception}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    private static bool RunManagerChangeLevelPrefix(
        RunManager __instance,
        bool _levelFailed,
        RunManager.ChangeLevelType _changeLevelType,
        out bool __state)
    {
        __state = IsShopContext();
        if (__state &&
            _changeLevelType == RunManager.ChangeLevelType.Normal)
        {
            StageRolesPlugin.Instance?.BaseUpgradeDraw?
                .PrepareForTruckTransition();
        }
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        if (_levelFailed && controller != null && controller.TryPreventPhoenixRetake())
        {
            __instance.AllPlayersDeadSet(_set: false);
            StageRolesPlugin.ModLogger.LogInfo(
                "Phoenix prevented a failed-stage retake.");
            return false;
        }
        return true;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(RunManager), nameof(RunManager.ChangeLevel))]
    private static void RunManagerChangeLevelPostfix(
        bool __runOriginal,
        bool __state)
    {
        if (__runOriginal)
        {
            StageRolesPlugin.Instance?.Controller?.StageEnding();
            if (__state && IsTruckContext())
            {
                StageRolesPlugin.Instance?.BaseUpgradeDraw?.QueueForTruck();
            }
        }
    }

    private static bool IsShopContext()
    {
        try
        {
            return RunManager.instance != null && SemiFunc.RunIsShop();
        }
        catch
        {
            return false;
        }
    }

    private static bool IsTruckContext()
    {
        try
        {
            return RunManager.instance != null && SemiFunc.RunIsLobby();
        }
        catch
        {
            return false;
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EnemyHealth), nameof(EnemyHealth.DeathImpulseRPC))]
    private static void EnemyHealthDeathImpulseRpcPostfix(EnemyHealth __instance)
    {
        StageRolesPlugin.Instance?.Controller?.EnemyDied(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemySpinny), "Update")]
    private static void EnemySpinnyUpdatePrefix(EnemySpinny __instance)
    {
        StageRolesPlugin.Instance?.Controller?.ObserveGambitState(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPriority(Priority.First)]
    [HarmonyBefore(EliteEnemyVariantsCompatibility.PluginGuid)]
    [HarmonyPatch(typeof(EnemySpinny), "UpdateState")]
    private static void EnemySpinnyUpdateStatePrefix(
        EnemySpinny __instance,
        EnemySpinny.State _nextState)
    {
        if (_nextState == EnemySpinny.State.Roulette)
        {
            StageRolesPlugin.Instance?.Controller?.CaptureGambitTarget(__instance);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemySpinny), "StateRouletteEffect")]
    private static void EnemySpinnyStateRouletteEffectPrefix(
        EnemySpinny __instance,
        out GamblerRoleRuntime.GambitEffectContext? __state)
    {
        __state = StageRolesPlugin.Instance?.Controller?
            .BeginGambitEffect(__instance);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EnemySpinny), "StateRouletteEffect")]
    private static void EnemySpinnyStateRouletteEffectPostfix(
        EnemySpinny __instance,
        GamblerRoleRuntime.GambitEffectContext? __state)
    {
        if (__state != null)
        {
            StageRolesPlugin.Instance?.Controller?
                .CompleteGambitEffect(__state);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.Despawn))]
    private static void EnemyParentDespawnPrefix(
        EnemyParent __instance,
        out int __state)
    {
        __state = StageRolesPlugin.Instance?.Controller?
            .CaptureEnemyOrbSpawnCount(__instance) ?? -1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(EnemyParent), nameof(EnemyParent.Despawn))]
    private static void EnemyParentDespawnPostfix(
        EnemyParent __instance,
        int __state)
    {
        StageRolesPlugin.Instance?.Controller?
            .EnemyDespawned(__instance, __state);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HurtCollider), "EnemyHurt")]
    private static void HurtColliderEnemyHurtPrefix(
        HurtCollider __instance,
        Enemy __0,
        out EnemyHitOverrideState __state)
    {
        __state = new EnemyHitOverrideState(
            __instance.enemyStunTime,
            __instance.enemyDamage);
        PlayerAvatar? attacker =
            HurtColliderPlayerField?.GetValue(__instance) as PlayerAvatar;
        StageRolesPlugin.Instance?.Controller?.ApplyEnemyHitRoleOverrides(
            __instance,
            __0,
            attacker);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(HurtCollider), "EnemyHurt")]
    private static void HurtColliderEnemyHurtPostfix(
        HurtCollider __instance,
        Enemy __0,
        bool __result,
        EnemyHitOverrideState __state)
    {
        __state.Restore(__instance);
        if (!__result)
        {
            return;
        }
        PlayerAvatar? attacker =
            HurtColliderPlayerField?.GetValue(__instance) as PlayerAvatar;
        StageRolesPlugin.Instance?.Controller?
            .ApplyRammerSelfDamageAfterEnemyHit(
                __instance,
                attacker);
        StageRolesPlugin.Instance?.Controller?.RecordEnemyAttacker(__0, attacker);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
    private static void HurtColliderPlayerHurtPrefix(
        HurtCollider __instance,
        PlayerAvatar __0,
        out int __state)
    {
        __state = __instance.playerDamage;
        try
        {
            PlayerAvatar? attacker =
                HurtColliderPlayerField?.GetValue(__instance) as PlayerAvatar;
            StageRoleController? controller =
                StageRolesPlugin.Instance?.Controller;
            controller?.ApplyPlayerHitRoleOverrides(__instance, attacker);
            controller?.RegisterPlayerHit(__instance, __0, attacker);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Role player-hit observation was skipped without interrupting " +
                $"vanilla damage: {exception.Message}");
        }
    }

    [HarmonyFinalizer]
    [HarmonyPatch(typeof(HurtCollider), "PlayerHurt")]
    private static void HurtColliderPlayerHurtPostfix(
        HurtCollider __instance,
        PlayerAvatar __0,
        int __state)
    {
        __instance.playerDamage = __state;
        try
        {
            StageRolesPlugin.Instance?.Controller?.EndPlayerHit(__instance, __0);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug($"Role hit cleanup was skipped: {exception.Message}");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]
    private static void PlayerHealthUpdateHealthRpcPrefix(
        PlayerHealth __instance,
        out int __state)
    {
        PlayerAvatar? player = __instance.GetComponent<PlayerAvatar>();
        __state = PlayerState.TryGetCurrentHealth(player, out int health)
            ? health
            : -1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.UpdateHealthRPC))]
    private static void PlayerHealthUpdateHealthRpcPostfix(
        PlayerHealth __instance,
        int healthNew,
        bool effect,
        bool hurtByHeal,
        int __state)
    {
        if (__state < 0)
        {
            return;
        }
        PlayerAvatar? player = __instance.GetComponent<PlayerAvatar>();
        if (player != null && PlayerState.TryGetCurrentHealth(player, out int currentHealth))
        {
            StageRolesPlugin.Instance?.Controller?.ObserveHealthUpdate(
                player,
                __state,
                currentHealth,
                healingAcknowledgement: effect && !hurtByHeal);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    private static void PlayerHealthHurtPrefix(
        PlayerHealth __instance,
        out int __state)
    {
        PlayerAvatar? player = __instance.GetComponent<PlayerAvatar>();
        __state = PlayerState.TryGetCurrentHealth(player, out int health)
            ? health
            : -1;
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerHealth), nameof(PlayerHealth.Hurt))]
    private static void PlayerHealthHurtPostfix(
        PlayerHealth __instance,
        int __state)
    {
        if (__state < 0)
        {
            return;
        }
        PlayerAvatar? player = __instance.GetComponent<PlayerAvatar>();
        if (player != null &&
            PlayerState.TryGetCurrentHealth(player, out int currentHealth))
        {
            StageRolesPlugin.Instance?.Controller?.ObserveHealthUpdate(
                player,
                __state,
                currentHealth);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.PlayerDeathRPC))]
    private static void PlayerAvatarPlayerDeathRpcPostfix(
        PlayerAvatar __instance)
    {
        StageRolesPlugin.Instance?.Controller?.PlayerDied(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.TumbleSet))]
    private static bool PlayerTumbleTumbleSetPrefix(
        PlayerTumble __instance,
        bool _isTumbling) =>
        _isTumbling ||
        StageRolesPlugin.Instance?.Controller?
            .ShouldKeepDiverTumbling(__instance) != true;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.TumbleSet))]
    private static void PlayerTumbleTumbleSetPostfix(
        PlayerTumble __instance,
        bool _isTumbling,
        bool _playerInput)
    {
        if (_isTumbling && _playerInput)
        {
            StageRolesPlugin.Instance?.Controller?
                .DiverTumbleStarted(__instance);
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.HitEnemy))]
    private static bool PlayerTumbleHitEnemyPrefix(PlayerTumble __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressRammerVanillaSelfDamage(__instance) != true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerTumble), nameof(PlayerTumble.TumbleSetRPC))]
    private static bool PlayerTumbleTumbleSetRpcPrefix(
        PlayerTumble __instance,
        bool _isTumbling) =>
        _isTumbling ||
        StageRolesPlugin.Instance?.Controller?
            .ShouldKeepDiverTumbling(__instance) != true;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(MusicalValuableLogic), nameof(MusicalValuableLogic.MusicKeyPressedRPC))]
    private static void MusicalValuableLogicMusicKeyPressedRpcPostfix(int grabberID)
    {
        StageRolesPlugin.Instance?.Controller?.InstrumentNotePlayed(grabberID);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Trap), "TrapActivateSync")]
    private static bool TrapActivateSyncPrefix(Trap __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerTrap(__instance) != true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(Trap), nameof(Trap.TrapStart))]
    private static bool TrapStartPrefix(Trap __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerTrap(__instance) != true;

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ChatMessageSendRPC))]
    private static void PlayerAvatarChatMessageSendRpcPrefix(
        PlayerAvatar __instance,
        string _message,
        out bool __state)
    {
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        __state = controller != null && controller.IsRoleQueryRequest(_message);
        if (__state)
        {
            StageFluxCompatibility.ExpectNotification(__instance, _message);
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ChatMessageSendRPC))]
    private static void PlayerAvatarChatMessageSendRpcPostfix(
        PlayerAvatar __instance,
        string _message,
        bool __state)
    {
        if (__state)
        {
            StageRolesPlugin.Instance?.Controller?.RespondToRoleQuery(__instance);
        }
        StageRolesPlugin.Instance?.Controller?.TryHandleMageCast(
            __instance,
            _message);
        StageRolesPlugin.Instance?.Controller?.TryHandleTricksterDecoy(
            __instance,
            _message);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysGrabObject), "GrabStartedRPC")]
    private static bool PhysGrabObjectGrabStartedRpcPrefix(
        PhysGrabObject __instance,
        int playerPhotonID,
        PhotonMessageInfo _info)
    {
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        if (controller?.IsTricksterDecoy(__instance) == true)
        {
            return false;
        }
        bool allowed = controller == null || controller.AllowBomberGrenadeGrabStart(
            __instance,
            playerPhotonID,
            _info);
        if (!allowed)
        {
            return false;
        }
        if (controller?.TryRegisterEngineerHostOnlyGrab(
                __instance,
                playerPhotonID,
                _info) == true)
        {
            return false;
        }
        controller?.RecordGrabStarted(__instance, playerPhotonID);
        return true;
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(ScreamDollValuable), "StateActive")]
    private static void ScreamDollValuableStateActivePrefix(
        ScreamDollValuable __instance)
    {
        StageRolesPlugin.Instance?.Controller?
            .MaintainTricksterDecoyActiveState(__instance);
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(PhysGrabObject), "GrabEndedRPC")]
    private static bool PhysGrabObjectGrabEndedRpcPrefix(
        PhysGrabObject __instance,
        int playerPhotonID,
        PhotonMessageInfo _info)
    {
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        controller?.RecordGrabEnded(__instance, playerPhotonID);
        return controller == null || controller.AllowBomberGrenadeGrabEnd(
            __instance,
            playerPhotonID,
            _info);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(StaticGrabObject), "GrabStartedRPC")]
    private static void StaticGrabObjectGrabStartedRpcPostfix(
        StaticGrabObject __instance,
        int playerPhotonID)
    {
        StageRolesPlugin.Instance?.Controller?.TryCopyImitatorRole(
            __instance,
            playerPhotonID);
    }
}

[HarmonyPatch(typeof(EnemyVision), nameof(EnemyVision.VisionTrigger))]
internal static class NinjaVisionPatch
{
    [HarmonyPrefix]
    private static bool Prefix(
        EnemyVision __instance,
        int playerID,
        PlayerAvatar player,
        bool playerNear) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldDelayNinjaVision(
                __instance,
                playerID,
                player,
                playerNear) != true;
}

[HarmonyPatch(typeof(PlayerVoiceChat), "FixedUpdate")]
internal static class NinjaVoiceInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerVoiceChat __instance)
    {
        StageRoleController? controller = StageRolesPlugin.Instance?.Controller;
        PlayerAvatar? owner = UtilityRoleRuntime.ResolveVoiceOwner(__instance);
        if (controller?.PlayerHasRole(owner, StageRole.Ninja) == true)
        {
            __instance.OverrideDisableEnemyInvestigate(1f);
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar), "LandRPC")]
internal static class NinjaLandingInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerAvatar __instance)
    {
        if (StageRolesPlugin.Instance?.Controller?
                .PlayerHasRole(__instance, StageRole.Ninja) == true)
        {
            __instance.OverrideDisableEnemyInvestigate(1f);
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Footstep))]
internal static class NinjaFootstepInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerAvatar __instance)
    {
        if (StageRolesPlugin.Instance?.Controller?
                .PlayerHasRole(__instance, StageRole.Ninja) == true)
        {
            __instance.OverrideDisableEnemyInvestigate(1f);
        }
    }
}

internal static class InfluencerNoiseContext
{
    [ThreadStatic]
    private static int _depth;

    [ThreadStatic]
    private static float _multiplier;

    internal static bool Enter(PlayerAvatar? player)
    {
        float multiplier = StageRolesPlugin.Instance?.Controller?
            .InfluencerNoiseMultiplier(player) ?? 1f;
        if (multiplier <= 1f)
        {
            return false;
        }
        _depth++;
        _multiplier = Mathf.Max(_multiplier, multiplier);
        return true;
    }

    internal static void Exit(bool entered)
    {
        if (!entered)
        {
            return;
        }
        _depth = Mathf.Max(0, _depth - 1);
        if (_depth == 0)
        {
            _multiplier = 1f;
        }
    }

    internal static void Apply(ref float radius)
    {
        if (_depth > 0)
        {
            radius *= Mathf.Max(1f, _multiplier);
        }
    }
}

[HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.Footstep))]
internal static class InfluencerFootstepInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerAvatar __instance, out bool __state) =>
        __state = InfluencerNoiseContext.Enter(__instance);

    [HarmonyPostfix]
    private static void Postfix(bool __state) =>
        InfluencerNoiseContext.Exit(__state);
}

[HarmonyPatch(typeof(PlayerAvatar), "LandRPC")]
internal static class InfluencerLandingInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerAvatar __instance, out bool __state) =>
        __state = InfluencerNoiseContext.Enter(__instance);

    [HarmonyPostfix]
    private static void Postfix(bool __state) =>
        InfluencerNoiseContext.Exit(__state);
}

[HarmonyPatch(typeof(PlayerVoiceChat), "FixedUpdate")]
internal static class InfluencerVoiceInvestigationPatch
{
    [HarmonyPrefix]
    private static void Prefix(PlayerVoiceChat __instance, out bool __state) =>
        __state = InfluencerNoiseContext.Enter(
            UtilityRoleRuntime.ResolveVoiceOwner(__instance));

    [HarmonyPostfix]
    private static void Postfix(bool __state) =>
        InfluencerNoiseContext.Exit(__state);
}

[HarmonyPatch(
    typeof(EnemyDirector),
    nameof(EnemyDirector.SetInvestigate),
    new[] { typeof(Vector3), typeof(float), typeof(bool) })]
internal static class InfluencerInvestigationRadiusPatch
{
    [HarmonyPrefix]
    private static void Prefix(ref float radius) =>
        InfluencerNoiseContext.Apply(ref radius);
}

[HarmonyPatch]
internal static class EngineerTrapUpdatePatches
{
    private static readonly string[] FrameMethodNames =
    {
        "Update",
        "FixedUpdate",
        "LateUpdate"
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> targets = new();
        foreach (Type type in typeof(Trap).Assembly.GetTypes())
        {
            if (!typeof(Trap).IsAssignableFrom(type))
            {
                continue;
            }
            AddFrameMethods(type, targets);
        }

        foreach (string typeName in EngineerEffectCatalog.FrameEffectTypeNames)
        {
            Type? type = AccessTools.TypeByName(typeName);
            if (type != null)
            {
                AddFrameMethods(type, targets);
            }
        }

        foreach (MethodBase target in targets)
        {
            yield return target;
        }
    }

    private static void AddFrameMethods(
        Type type,
        HashSet<MethodBase> targets)
    {
        foreach (string methodName in FrameMethodNames)
        {
            MethodInfo? method = EngineerEffectCatalog.FindDeclaredInstanceMethod(
                type,
                methodName,
                Type.EmptyTypes);
            if (method != null && !method.IsStatic &&
                method.ReturnType == typeof(void))
            {
                targets.Add(method);
            }
        }
    }

    [HarmonyPrefix]
    private static bool EffectFramePrefix(Component __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerEffect(__instance) != true;
}

[HarmonyPatch]
internal static class EngineerEffectActivationPatches
{
    private static readonly (string TypeName, string MethodName)[] Targets =
    {
        ("FireExtinguisherValuable", "GrabTrigger"),
        ("FlamethrowerValuable", "GrabTrigger"),
        ("MusicalValuableLogic", "MusicKeyPressed"),
        ("PhoneValuable", "PickUp"),
        ("PowerCrystalValuable", "Explode"),
        ("ValuableCamera", "Explosion"),
        ("ValuableCauldronBox", "TrapActivate"),
        ("ValuableLevitationPotion", "ActivateSphere"),
        ("ValuableStarWand", "CastSpell"),
        ("ValuableWizardStaff", "StaffLaser")
    };

    private static IEnumerable<MethodBase> TargetMethods()
    {
        HashSet<MethodBase> methods = new();
        foreach (Type type in typeof(Trap).Assembly.GetTypes())
        {
            if (!typeof(Trap).IsAssignableFrom(type))
            {
                continue;
            }
            MethodInfo? trapActivate = EngineerEffectCatalog.FindDeclaredInstanceMethod(
                type,
                "TrapActivate",
                Type.EmptyTypes);
            if (trapActivate != null && !trapActivate.IsStatic)
            {
                methods.Add(trapActivate);
            }
        }

        foreach ((string typeName, string methodName) in Targets)
        {
            Type? type = AccessTools.TypeByName(typeName);
            MethodInfo? method = type != null
                ? EngineerEffectCatalog.FindDeclaredInstanceMethod(type, methodName)
                : null;
            if (method != null && !method.IsStatic)
            {
                methods.Add(method);
            }
        }

        foreach (MethodBase method in methods)
        {
            yield return method;
        }
    }

    [HarmonyPrefix]
    private static bool EffectActivationPrefix(Component __instance) =>
        StageRolesPlugin.Instance?.Controller?
            .ShouldSuppressEngineerEffect(__instance) != true;
}

internal static class EngineerEffectCatalog
{
    private static readonly HashSet<string> UnsupportedHostOnlyTypeNames = new(
        new[]
        {
            "ClownTrap",
            "PropaneTankTrap",
            "ValuableArcticSnowBike",
            "ValuableCamera",
            "ValuableFlashlight",
            "ValuableLovePotion"
        },
        StringComparer.Ordinal);

    internal static readonly string[] FrameEffectTypeNames =
    {
        "CrystalBallValuable",
        "FireExtinguisherValuable",
        "FlamethrowerValuable",
        "JackhammerValuable",
        "MapValuable",
        "MusicalValuableLogic",
        "PhoneValuable",
        "PowerCrystalValuable",
        "ScreamDollValuable",
        "UraniumScript",
        "ValuableArcticSnowBike",
        "ValuableCamera",
        "ValuableCauldronBox",
        "ValuableEyeOfOrpigox",
        "ValuableFlashlight",
        "ValuableLevitationPotion",
        "ValuableLovePotion",
        "ValuablePills",
        "ValuableSawBlade",
        "ValuableScale",
        "ValuableSpiderPotion",
        "ValuableStarWand",
        "ValuableTeethBot",
        "ValuableWizardStaff",
        "ValuableWizardTimeGlass"
    };

    private static readonly HashSet<string> EffectTypeNames =
        new(FrameEffectTypeNames, StringComparer.Ordinal);

    internal static bool IsEffectValuable(PhysGrabObject valuable)
    {
        Component[] components = valuable.GetComponentsInChildren<Component>(true);
        foreach (Component component in components)
        {
            if (component != null &&
                UnsupportedHostOnlyTypeNames.Contains(component.GetType().Name))
            {
                return false;
            }
        }

        if (valuable.GetComponent<Trap>() != null ||
            valuable.GetComponentInChildren<Trap>(true) != null)
        {
            return true;
        }

        foreach (Component component in components)
        {
            if (component != null &&
                EffectTypeNames.Contains(component.GetType().Name))
            {
                return true;
            }
        }
        return false;
    }

    internal static MethodInfo? FindDeclaredInstanceMethod(
        Type type,
        string methodName,
        Type[]? parameterTypes = null)
    {
        const BindingFlags flags = BindingFlags.Instance |
                                   BindingFlags.Public |
                                   BindingFlags.NonPublic |
                                   BindingFlags.DeclaredOnly;
        if (parameterTypes != null)
        {
            return type.GetMethod(
                methodName,
                flags,
                binder: null,
                types: parameterTypes,
                modifiers: null);
        }

        foreach (MethodInfo method in type.GetMethods(flags))
        {
            if (string.Equals(method.Name, methodName, StringComparison.Ordinal))
            {
                return method;
            }
        }
        return null;
    }
}
