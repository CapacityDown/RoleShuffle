using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    private static readonly FieldInfo? InfluenzaVoice = AccessTools.Field(typeof(PlayerAvatar), "voiceChat");
    private static readonly FieldInfo? InfluenzaMicLoudness = AccessTools.Field(typeof(PlayerVoiceChat), "clipLoudnessNoTTS");
    private readonly Dictionary<string, InfluenzaState> _influenza = new(StringComparer.Ordinal);

    private void StartInfluenza(RoleAssignment assignment)
    {
        if (RoleCatalog.HasCapability(assignment.Role, StageRole.Influenza) &&
            !_influenza.ContainsKey(assignment.SteamId))
            _influenza.Add(assignment.SteamId, new InfluenzaState(Time.time));
    }

    private void TickInfluenza()
    {
        // Assignment objects may change roles during SpreadInfluenza, but the list is stable.
        foreach (RoleAssignment assignment in _assignments)
        {
            if (!RoleCatalog.HasCapability(assignment.Role, StageRole.Influenza)) continue;
            StartInfluenza(assignment);
            InfluenzaState state = _influenza[assignment.SteamId];
            PlayerAvatar player = assignment.Player;
            if (!PlayerState.IsLiving(player)) continue;

            bool speaking = InfluenzaVoice?.GetValue(player) is PlayerVoiceChat voice &&
                InfluenzaMicLoudness?.GetValue(voice) is float loudness && loudness > 0.05f;
            bool speechStarted = state.ObserveVoice(Time.time, speaking);
            if (!state.Symptomatic(Time.time)) continue;
            if (!state.SymptomsStarted)
            {
                state.SymptomsStarted = true;
                state.NextSneezeAt = Time.time + InfluenzaRules.SneezeInterval(UnityEngine.Random.value);
            }
            EnforceInfluenzaHealth(assignment, state);
            if (speechStarted) SpreadInfluenza(assignment, sneeze: false);
            if (Time.time < state.NextSneezeAt) continue;
            state.NextSneezeAt = Time.time + InfluenzaRules.SneezeInterval(UnityEngine.Random.value);
            // Standard TTS reaches unmodded guests. This is not ordinary chat input.
            try { player.ChatMessageSend("Achoo!"); }
            catch (Exception error) { StageRolesPlugin.ModLogger.LogDebug($"Sneeze voice unavailable: {error.Message}"); }
            SpreadInfluenza(assignment, sneeze: true);
        }
    }

    private static int HealthUpgrade(string steamId) =>
        UpgradeService.TryGetLevels(steamId, out var levels)
            ? levels.GetValueOrDefault("playerUpgradeHealth", 0) : 0;

    private static void EnforceInfluenzaHealth(RoleAssignment assignment, InfluenzaState state)
    {
        if (!PlayerState.TryGetMaximumHealth(assignment.Player, out int maximum)) return;
        state.CaptureHealth(maximum, HealthUpgrade(assignment.SteamId));
        if (maximum != InfluenzaRules.MaximumHealth ||
            (PlayerState.TryGetCurrentHealth(assignment.Player, out int current) && current > InfluenzaRules.MaximumHealth))
            PlayerState.SetMaximumHealthSynchronized(assignment.Player, InfluenzaRules.MaximumHealth);
    }

    private void EndInfluenza(RoleAssignment assignment)
    {
        if (!_influenza.Remove(assignment.SteamId, out InfluenzaState? state)) return;
        if (state.HealthCaptured && IsAuthority())
            PlayerState.SetMaximumHealthSynchronized(assignment.Player,
                state.RestoredMaximum(HealthUpgrade(assignment.SteamId)));
    }

    private void StopInfluenza()
    {
        foreach (RoleAssignment assignment in _assignments) EndInfluenza(assignment);
        foreach (RoleAssignment assignment in _departedAssignments.Values) EndInfluenza(assignment);
        _influenza.Clear();
    }

    internal void OnInfluenzaChat(PlayerAvatar player, string message, PhotonMessageInfo info)
    {
        if (!_stageReady || !_assignmentsInitialized || !_config.Enabled.Value || !IsAuthority() ||
            string.IsNullOrWhiteSpace(message) || message.TrimStart().StartsWith("/", StringComparison.Ordinal) ||
            !PlayerState.IsLiving(player)) return;
        // A sender may only infect through their own avatar. Host-generated role
        // announcements and sneeze TTS never count as ordinary text submissions.
        if (SemiFunc.IsMultiplayer() && (info.Sender == null || player.photonView == null ||
            info.Sender != player.photonView.Owner ||
            (info.Sender == PhotonNetwork.LocalPlayer && !InfluenzaChatPatches.LocalSubmission))) return;
        RoleAssignment? assignment = FindAssignment(player);
        if (assignment != null && RoleCatalog.HasCapability(assignment.Role, StageRole.Influenza) &&
            _influenza.TryGetValue(assignment.SteamId, out InfluenzaState? state) && state.Symptomatic(Time.time))
            SpreadInfluenza(assignment, sneeze: false);
    }

    private void SpreadInfluenza(RoleAssignment source, bool sneeze)
    {
        PlayerAvatar player = source.Player;
        Vector3 origin = InfluenzaHeadPosition(player);
        Transform facing = player.playerTransform != null ? player.playerTransform : player.transform;
        Vector3 forward = facing.forward;
        forward.y = 0;
        if (forward.sqrMagnitude < 0.0001f) return;
        forward.Normalize();
        foreach (RoleAssignment target in _assignments)
        {
            // Existing carriers include Disaster. Reinfection must not replace
            // its combined role or restart its incubation period.
            if (ReferenceEquals(source, target) || RoleCatalog.HasCapability(target.Role, StageRole.Influenza) ||
                !PlayerState.IsLiving(target.Player)) continue;
            Vector3 delta = InfluenzaHeadPosition(target.Player) - origin;
            float distanceSquared = delta.sqrMagnitude;
            delta.y = 0;
            float dot = delta.sqrMagnitude < 0.0001f ? 1f : Vector3.Dot(forward, delta.normalized);
            if (!InfluenzaRules.InRange(distanceSquared, dot, sneeze) ||
                !InfluenzaRules.Infects(UnityEngine.Random.value, sneeze)) continue;
            InfectWithInfluenza(target);
        }
    }

    private static Vector3 InfluenzaHeadPosition(PlayerAvatar player) =>
        player.PlayerVisionTarget != null && player.PlayerVisionTarget.VisionTransform != null
            ? player.PlayerVisionTarget.VisionTransform.position : player.transform.position + Vector3.up;

    private void InfectWithInfluenza(RoleAssignment assignment)
    {
        StageRole previous = assignment.Role;
        // Remove only this player's persistent effects; other players' budgets,
        // hazards and pending actions must retain their stage state.
        _bomber.RemovePlayer(assignment.SteamId);
        _medic.RemovePlayer(assignment.SteamId);
        _stinker.RemovePlayer(assignment.SteamId);
        _trickster.RemovePlayer(assignment.SteamId);
        _eventRoles.RemovePlayer(assignment.SteamId);
        _diver.RemovePlayer(assignment.SteamId);
        if (previous == StageRole.King) RestoreKingCrown();
        assignment.AssignedRole = StageRole.Influenza;
        assignment.Role = StageRole.Influenza;
        ResetAssignmentForRoleChange(assignment);
        UpgradeService.SetLevels(assignment.SteamId, RoleCatalog.TargetUpgrades(StageRole.Influenza, _config));
        KingUpgradeAura.Tick(_config, _assignments);
        StartInfluenza(assignment);
        RoleAssignmentSync.Publish(_assignments);
        _nextAbilityPublishAt = 0f;
        _notifier.NotifyConditional(assignment.Player, "Influenza",
            () => _stageReady && assignment.Role == StageRole.Influenza);
        StageRolesPlugin.ModLogger.LogInfo($"Influenza infection: {assignment.SteamId}, {previous} -> Influenza.");
    }
}

[HarmonyPatch]
internal static class InfluenzaChatPatches
{
    internal static bool LocalSubmission { get; private set; }

    [HarmonyPrefix, HarmonyPatch(typeof(ChatManager), "MessageSend")]
    private static void BeginSubmission(bool _possessed, out bool __state)
    {
        __state = LocalSubmission;
        LocalSubmission = !_possessed;
    }

    [HarmonyFinalizer, HarmonyPatch(typeof(ChatManager), "MessageSend")]
    private static void EndSubmission(bool __state) => LocalSubmission = __state;

    [HarmonyPostfix, HarmonyPatch(typeof(PlayerAvatar), nameof(PlayerAvatar.ChatMessageSendRPC))]
    private static void ReceiveChat(PlayerAvatar __instance, string _message, PhotonMessageInfo _info) =>
        StageRolesPlugin.Instance?.Controller?.OnInfluenzaChat(__instance, _message, _info);
}
