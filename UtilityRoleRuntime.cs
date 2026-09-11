using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class UtilityRoleRuntime
{
    private const float TickIntervalSeconds = 0.25f;
    private const float NinjaSuppressionSeconds = 0.5f;
    private const float NinjaVisionAttemptResetSeconds = 0.6f;
    private static readonly FieldInfo? ValuableOriginalValueField =
        AccessTools.Field(typeof(ValuableObject), "dollarValueOriginal");
    private static readonly FieldInfo? ValuableCurrentValueField =
        AccessTools.Field(typeof(ValuableObject), "dollarValueCurrent");
    private static readonly MethodInfo? ValuableHealLogicMethod =
        AccessTools.Method(
            typeof(PhysGrabObjectImpactDetector),
            "HealLogic",
            new[] { typeof(float), typeof(Vector3) });
    private static readonly FieldInfo? RunManagerVoiceChatsField =
        AccessTools.Field(typeof(RunManager), "voiceChats");
    private static readonly FieldInfo? VoiceChatPlayerAvatarField =
        AccessTools.Field(typeof(PlayerVoiceChat), "playerAvatar");
    private static readonly FieldInfo? PlayerCrawlingField =
        AccessTools.Field(typeof(PlayerAvatar), "isCrawling");
    private static readonly FieldInfo? PlayerCrouchingField =
        AccessTools.Field(typeof(PlayerAvatar), "isCrouching");
    private static readonly FieldInfo? PlayerTumblingField =
        AccessTools.Field(typeof(PlayerAvatar), "isTumbling");
    private readonly StageRolesConfig _config;
    private readonly GameObject _chargerObject;
    private readonly Dictionary<string, float> _mechanicPendingRepairPercent =
        new(StringComparer.Ordinal);
    private readonly Dictionary<long, NinjaVisionAttempt> _ninjaVisionAttempts = new();
    private float _lastTickAt;
    private float _nextTickAt;

    internal UtilityRoleRuntime(StageRolesConfig config, GameObject chargerObject)
    {
        _config = config;
        _chargerObject = chargerObject;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        foreach (RoleAssignment assignment in assignments)
        {
            assignment.MechanicRepairPercentUsed = 0f;
            assignment.ElectricianChargePercentUsed = 0f;
        }
        _lastTickAt = Time.time;
        _nextTickAt = Time.time;
        _mechanicPendingRepairPercent.Clear();
        _ninjaVisionAttempts.Clear();
    }

    internal void Stop()
    {
        _lastTickAt = 0f;
        _nextTickAt = 0f;
        _mechanicPendingRepairPercent.Clear();
        _ninjaVisionAttempts.Clear();
    }

    internal void Tick(IReadOnlyList<RoleAssignment> assignments)
    {
        if (Time.time < _nextTickAt)
        {
            return;
        }

        float elapsed = _lastTickAt > 0f
            ? Mathf.Clamp(Time.time - _lastTickAt, 0f, 1f)
            : TickIntervalSeconds;
        _lastTickAt = Time.time;
        _nextTickAt = Time.time + TickIntervalSeconds;

        PhysGrabObject[]? heldCandidates = null;
        foreach (RoleAssignment assignment in assignments)
        {
            if (!PlayerState.IsLiving(assignment.Player))
            {
                continue;
            }

            if (RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Mechanic) &&
                _config.MechanicRepairPercentPerSecond.Value > 0f &&
                assignment.MechanicRepairPercentUsed < Mathf.Clamp(
                    _config.MechanicMaximumRepairPercentPerStage.Value, 0f, 100f))
            {
                heldCandidates ??= UnityEngine.Object.FindObjectsOfType<PhysGrabObject>();
                TickMechanic(assignment, heldCandidates, elapsed);
            }
            if (RoleCatalog.HasCapability(
                    assignment.Role,
                    StageRole.Electrician) &&
                _config.ElectricianChargePercentPerSecond.Value > 0f &&
                assignment.ElectricianChargePercentUsed < Mathf.Clamp(
                    _config.ElectricianMaximumChargePercentPerStage.Value, 0f, 1000f))
            {
                heldCandidates ??= UnityEngine.Object.FindObjectsOfType<PhysGrabObject>();
                TickElectrician(assignment, heldCandidates, elapsed);
            }
            if (RoleCatalog.HasCapability(assignment.Role, StageRole.Ninja))
            {
                SuppressPlayerInvestigations(assignment.Player);
            }
        }
    }

    private void TickMechanic(
        RoleAssignment assignment,
        IReadOnlyList<PhysGrabObject> candidates,
        float elapsed)
    {
        float stageRemaining = Mathf.Clamp(
                _config.MechanicMaximumRepairPercentPerStage.Value,
                0f,
                100f) -
            assignment.MechanicRepairPercentUsed;
        float tickRemaining = Mathf.Min(
            stageRemaining,
            Mathf.Clamp(_config.MechanicRepairPercentPerSecond.Value, 0f, 100f) *
            elapsed);
        if (tickRemaining <= 0f)
        {
            return;
        }

        HashSet<int> repaired = new();
        foreach (PhysGrabObject physObject in candidates)
        {
            if (tickRemaining <= 0f || physObject == null ||
                !IsHeldBy(physObject, assignment))
            {
                continue;
            }

            PhysGrabObjectImpactDetector? detector =
                physObject.GetComponent<PhysGrabObjectImpactDetector>() ??
                physObject.GetComponentInChildren<PhysGrabObjectImpactDetector>(true) ??
                physObject.GetComponentInParent<PhysGrabObjectImpactDetector>();
            ValuableObject? valuable =
                physObject.GetComponent<ValuableObject>() ??
                physObject.GetComponentInChildren<ValuableObject>(true) ??
                physObject.GetComponentInParent<ValuableObject>();
            if (detector == null || valuable == null ||
                !repaired.Add(detector.GetInstanceID()))
            {
                continue;
            }

            float requestedPercent = tickRemaining;
            try
            {
                if (!TryRepairValuable(
                        assignment,
                        detector,
                        valuable,
                        requestedPercent,
                        stageRemaining,
                        physObject.centerPoint,
                        out float consumedPercent))
                {
                    continue;
                }
                assignment.MechanicRepairPercentUsed += consumedPercent;
                stageRemaining = Mathf.Max(0f, stageRemaining - consumedPercent);
                tickRemaining = Mathf.Max(0f, tickRemaining - consumedPercent);
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Mechanic repair skipped for {physObject.name}: " +
                    exception.Message);
            }
        }
    }

    private bool TryRepairValuable(
        RoleAssignment assignment,
        PhysGrabObjectImpactDetector detector,
        ValuableObject valuable,
        float requestedPercent,
        float stageRemainingPercent,
        Vector3 healingPoint,
        out float consumedPercent)
    {
        consumedPercent = 0f;
        string pendingKey = assignment.SteamId + ":" + detector.GetInstanceID();
        float accumulatedPercent = Mathf.Max(0f, requestedPercent);
        if (_mechanicPendingRepairPercent.TryGetValue(
                pendingKey,
                out float pendingPercent))
        {
            accumulatedPercent += pendingPercent;
        }

        accumulatedPercent = Mathf.Min(
            accumulatedPercent,
            Mathf.Max(0f, stageRemainingPercent));
        float displayedOriginalValue = ReadFloat(
            ValuableOriginalValueField,
            valuable);
        float displayedCurrentValue = ReadFloat(
            ValuableCurrentValueField,
            valuable);
        float eventMultiplier = Mathf.Clamp(
            StageFluxCompatibility.GetValuableValueMultiplier(valuable),
            0.01f,
            100f);
        float baseOriginalValue = displayedOriginalValue / eventMultiplier;
        float baseCurrentValue = displayedCurrentValue / eventMultiplier;
        if (displayedOriginalValue <= 0f || displayedCurrentValue < 0f ||
            baseOriginalValue <= 0f || baseCurrentValue >= baseOriginalValue ||
            ValuableHealLogicMethod == null)
        {
            _mechanicPendingRepairPercent.Remove(pendingKey);
            return false;
        }

        float missingBaseValue = baseOriginalValue - baseCurrentValue;
        float displayedRepairValue = MechanicRepairBudget.RepairValue(
            displayedOriginalValue, displayedCurrentValue,
            accumulatedPercent, stageRemainingPercent);
        if (displayedRepairValue < 1f)
        {
            _mechanicPendingRepairPercent[pendingKey] = Mathf.Min(
                accumulatedPercent,
                Mathf.Max(0f, stageRemainingPercent));
            return false;
        }

        if (!ApplyValuableRepair(detector, displayedRepairValue, healingPoint))
        {
            return false;
        }

        float repairedDisplayedValue = ReadFloat(
            ValuableCurrentValueField,
            valuable);
        float repairedBaseValue = repairedDisplayedValue / eventMultiplier;
        float actualRestoredBaseValue = Mathf.Clamp(
            repairedBaseValue - baseCurrentValue,
            0f,
            missingBaseValue);
        if (actualRestoredBaseValue <= 0f)
        {
            if (repairedBaseValue >= baseOriginalValue)
            {
                _mechanicPendingRepairPercent.Remove(pendingKey);
            }
            else
            {
                _mechanicPendingRepairPercent[pendingKey] = accumulatedPercent;
            }
            return false;
        }

        consumedPercent = Mathf.Min(
            accumulatedPercent,
            actualRestoredBaseValue / baseOriginalValue * 100f);
        float remainingAccumulation = Mathf.Max(
            0f,
            accumulatedPercent - consumedPercent);
        if (repairedBaseValue < baseOriginalValue && remainingAccumulation > 0f)
        {
            _mechanicPendingRepairPercent[pendingKey] = remainingAccumulation;
        }
        else
        {
            _mechanicPendingRepairPercent.Remove(pendingKey);
        }
        StageRolesPlugin.ModLogger.LogDebug(
            $"Mechanic restored {actualRestoredBaseValue:0.###} " +
            $"base value to {valuable.name} with active value multiplier " +
            $"{eventMultiplier:0.###}.");
        return true;
    }

    private static bool ApplyValuableRepair(
        PhysGrabObjectImpactDetector detector,
        float repairValue,
        Vector3 healingPoint)
    {
        try
        {
            PhotonView? view = null;
            if (SemiFunc.IsMultiplayer())
            {
                view = detector.GetComponent<PhotonView>() ??
                       detector.GetComponentInParent<PhotonView>();
                if (view == null || view.ViewID == 0)
                {
                    return false;
                }
            }
            ValuableHealLogicMethod!.Invoke(
                detector,
                new object[] { repairValue, healingPoint });
            if (view != null)
            {
                view.RPC(
                    "HealRPC",
                    RpcTarget.Others,
                    new object[] { repairValue, healingPoint });
            }
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                "Mechanic could not apply a valuable repair: " +
                exception.GetBaseException().Message);
            return false;
        }
    }

    private void TickElectrician(
        RoleAssignment assignment,
        IReadOnlyList<PhysGrabObject> candidates,
        float elapsed)
    {
        float stageRemaining = Mathf.Clamp(
                _config.ElectricianMaximumChargePercentPerStage.Value,
                0f,
                1000f) -
            assignment.ElectricianChargePercentUsed;
        float tickRemaining = Mathf.Min(
            stageRemaining,
            Mathf.Clamp(_config.ElectricianChargePercentPerSecond.Value, 0f, 100f) *
            elapsed);
        if (tickRemaining <= 0f)
        {
            return;
        }

        HashSet<int> charged = new();
        foreach (PhysGrabObject physObject in candidates)
        {
            if (tickRemaining <= 0f || physObject == null ||
                !IsHeldBy(physObject, assignment))
            {
                continue;
            }

            ItemBattery? battery =
                physObject.GetComponent<ItemBattery>() ??
                physObject.GetComponentInChildren<ItemBattery>(true) ??
                physObject.GetComponentInParent<ItemBattery>();
            if (battery == null || battery.isUnchargable ||
                (battery.batteryActive && HasUsableBatteryBar(battery)) ||
                battery.batteryLife >= 99.9f ||
                !charged.Add(battery.GetInstanceID()))
            {
                continue;
            }

            float restoredPercent = Mathf.Min(
                tickRemaining,
                100f - battery.batteryLife);
            if (restoredPercent <= 0f)
            {
                continue;
            }

            try
            {
                // ChargeBattery applies the supplied rate for 0.1 seconds.
                battery.ChargeBattery(_chargerObject, restoredPercent * 10f);
                assignment.ElectricianChargePercentUsed += restoredPercent;
                tickRemaining -= restoredPercent;
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogDebug(
                    $"Electrician charge skipped for {physObject.name}: " +
                    exception.Message);
            }
        }
    }

    private static bool HasUsableBatteryBar(ItemBattery battery)
    {
        int bars = Mathf.Max(1, battery.batteryBars);
        float firstBarThreshold = 50f / bars;
        return battery.batteryLife > firstBarThreshold;
    }

    private static bool IsHeldBy(
        PhysGrabObject physObject,
        RoleAssignment assignment)
    {
        if (physObject.playerGrabbing == null)
        {
            return false;
        }
        foreach (PhysGrabber grabber in physObject.playerGrabbing)
        {
            PlayerAvatar? player = grabber?.playerAvatar;
            if (player == null)
            {
                continue;
            }
            if (ReferenceEquals(player, assignment.Player))
            {
                return true;
            }
            string steamId = PlayerIdentity.SteamId(player);
            if (!string.IsNullOrEmpty(steamId) &&
                string.Equals(steamId, assignment.SteamId, StringComparison.Ordinal))
            {
                return true;
            }
        }
        return false;
    }

    private static void SuppressPlayerInvestigations(PlayerAvatar player)
    {
        try
        {
            player.OverrideDisableEnemyInvestigate(NinjaSuppressionSeconds);
            foreach (PlayerVoiceChat voiceChat in
                     player.GetComponentsInChildren<PlayerVoiceChat>(true))
            {
                voiceChat.OverrideDisableEnemyInvestigate(
                    NinjaSuppressionSeconds);
            }
            if (RunManager.instance != null &&
                RunManagerVoiceChatsField?.GetValue(RunManager.instance) is IEnumerable voiceChats)
            {
                string steamId = PlayerIdentity.SteamId(player);
                foreach (object? entry in voiceChats)
                {
                    PlayerVoiceChat? voiceChat = entry as PlayerVoiceChat;
                    PlayerAvatar? voiceOwner = ResolveVoiceOwner(voiceChat);
                    if (voiceChat == null || voiceOwner == null)
                    {
                        continue;
                    }
                    if (ReferenceEquals(voiceOwner, player) ||
                        (!string.IsNullOrEmpty(steamId) &&
                         string.Equals(
                             PlayerIdentity.SteamId(voiceOwner),
                             steamId,
                             StringComparison.Ordinal)))
                    {
                        voiceChat.OverrideDisableEnemyInvestigate(
                            NinjaSuppressionSeconds);
                    }
                }
            }
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Ninja noise suppression skipped: {exception.Message}");
        }
    }

    internal static PlayerAvatar? ResolveVoiceOwner(PlayerVoiceChat? voiceChat)
    {
        if (voiceChat == null)
        {
            return null;
        }
        if (VoiceChatPlayerAvatarField?.GetValue(voiceChat) is PlayerAvatar owner)
        {
            return owner;
        }

        PhotonView? voiceView = voiceChat.GetComponent<PhotonView>();
        if (voiceView?.Owner == null || GameDirector.instance?.PlayerList == null)
        {
            return null;
        }
        foreach (PlayerAvatar player in GameDirector.instance.PlayerList)
        {
            PhotonView? playerView = player?.GetComponent<PhotonView>();
            if (playerView?.Owner != null &&
                playerView.Owner.ActorNumber == voiceView.Owner.ActorNumber)
            {
                return player;
            }
        }
        return null;
    }

    private static float ReadFloat(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is float value ? value : -1f;

    private static bool ReadBool(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is bool value && value;

    internal bool ShouldDelayNinjaVision(
        EnemyVision vision,
        int playerId,
        PlayerAvatar player,
        bool playerNear,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (vision == null || player == null ||
            !HasLivingRole(player, StageRole.Ninja, assignments))
        {
            return false;
        }

        float multiplier = Mathf.Clamp(
            _config.NinjaVisionRecognitionMultiplier.Value,
            1f,
            10f);
        bool crawling = ReadBool(PlayerCrawlingField, player);
        bool crouching = ReadBool(PlayerCrouchingField, player);
        bool tumbling = ReadBool(PlayerTumblingField, player);
        int vanillaChecks = playerNear
            ? Mathf.Max(1, vision.VisionsToTrigger)
            : crawling
                ? Mathf.Max(1, vision.VisionsToTriggerCrawl)
                : crouching || tumbling
                    ? Mathf.Max(1, vision.VisionsToTriggerCrouch)
                    : Mathf.Max(1, vision.VisionsToTrigger);
        int additionalChecks = Mathf.CeilToInt(
            vanillaChecks * (multiplier - 1f));
        if (additionalChecks <= 0)
        {
            return false;
        }

        long key = ((long)(uint)vision.GetInstanceID() << 32) | (uint)playerId;
        if (!_ninjaVisionAttempts.TryGetValue(key, out NinjaVisionAttempt attempt) ||
            Time.time - attempt.LastAttemptAt > NinjaVisionAttemptResetSeconds)
        {
            attempt = new NinjaVisionAttempt();
            _ninjaVisionAttempts[key] = attempt;
        }
        attempt.LastAttemptAt = Time.time;
        if (attempt.Allowed)
        {
            return false;
        }

        attempt.Attempts++;
        if (attempt.Attempts > additionalChecks)
        {
            attempt.Allowed = true;
            return false;
        }
        if (vision.VisionTriggered.ContainsKey(playerId))
        {
            vision.VisionTriggered[playerId] = false;
        }
        return true;
    }

    private static bool HasLivingRole(
        PlayerAvatar player,
        StageRole role,
        IReadOnlyList<RoleAssignment> assignments)
    {
        if (!PlayerState.IsLiving(player))
        {
            return false;
        }
        string steamId = PlayerIdentity.SteamId(player);
        foreach (RoleAssignment assignment in assignments)
        {
            if (RoleCatalog.HasCapability(assignment.Role, role) &&
                (ReferenceEquals(assignment.Player, player) ||
                 (!string.IsNullOrEmpty(steamId) &&
                  string.Equals(
                      assignment.SteamId,
                      steamId,
                      StringComparison.Ordinal))))
            {
                return true;
            }
        }
        return false;
    }

    private sealed class NinjaVisionAttempt
    {
        internal int Attempts { get; set; }
        internal float LastAttemptAt { get; set; }
        internal bool Allowed { get; set; }
    }
}
