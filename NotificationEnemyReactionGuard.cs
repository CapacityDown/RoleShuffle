using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

internal static class NotificationEnemyReactionGuard
{
    private const float PlaybackStartGraceSeconds = 10f;
    private const float PlaybackSilenceGraceSeconds = 1f;
    private const float InvestigateCooldownSeconds = 1f;
    private static readonly FieldInfo? PlayerVoiceChatField =
        AccessTools.Field(typeof(PlayerAvatar), "voiceChat");
    private static readonly FieldInfo? VoiceChatPlayerAvatarField =
        AccessTools.Field(typeof(PlayerVoiceChat), "playerAvatar");
    private static readonly FieldInfo? InvestigateTimerField =
        AccessTools.Field(typeof(PlayerVoiceChat), "investigateTimer");
    private static readonly FieldInfo? ClipLoudnessNoTtsField =
        AccessTools.Field(typeof(PlayerVoiceChat), "clipLoudnessNoTTS");
    private static readonly FieldInfo? ClipLoudnessCrawlingCounterField =
        AccessTools.Field(typeof(PlayerVoiceChat), "clipLoudnessCrawlingCounter");
    private static readonly FieldInfo? PlayerDeathHeadField =
        AccessTools.Field(typeof(PlayerAvatar), "playerDeathHead");
    private static readonly FieldInfo? PlayerDisabledField =
        AccessTools.Field(typeof(PlayerAvatar), "isDisabled");
    private static readonly FieldInfo? PlayerCrawlingField =
        AccessTools.Field(typeof(PlayerAvatar), "isCrawling");
    private static readonly FieldInfo? PlayerCrouchingField =
        AccessTools.Field(typeof(PlayerAvatar), "isCrouching");
    private static readonly FieldInfo? DeathHeadSpectatedField =
        AccessTools.Field(typeof(PlayerDeathHead), "spectated");
    private static readonly FieldInfo? DeathHeadPhysGrabObjectField =
        AccessTools.Field(typeof(PlayerDeathHead), "physGrabObject");

    private sealed class ActiveNotification
    {
        internal ActiveNotification(PlayerVoiceChat voiceChat, string message)
        {
            VoiceChat = voiceChat;
            AwaitPlaybackUntil = Time.time + PlaybackStartGraceSeconds;
            HardExpiresAt = Time.time + Mathf.Clamp(
                PlaybackStartGraceSeconds + 2f + message.Length * 0.25f,
                12f,
                45f);
            NextMicrophoneInvestigateAt =
                Time.time + Mathf.Max(0f, ReadFloat(InvestigateTimerField, voiceChat));
            NoTtsCrawlingCounter = ReadFloat(ClipLoudnessNoTtsField, voiceChat) > 0.05f
                ? ReadInt(ClipLoudnessCrawlingCounterField, voiceChat)
                : 0;
        }

        internal PlayerVoiceChat VoiceChat { get; }
        internal float AwaitPlaybackUntil { get; }
        internal float HardExpiresAt { get; }
        internal bool PlaybackObserved { get; set; }
        internal float LastPlaybackAt { get; set; }
        internal float NextMicrophoneInvestigateAt { get; set; }
        internal int NoTtsCrawlingCounter { get; set; }
    }

    private static readonly Dictionary<int, ActiveNotification> ActiveByVoiceChat = new();

    internal static void Expect(PlayerAvatar player, string message)
    {
        PlayerVoiceChat? voiceChat = GetVoiceChat(player);
        if (player?.photonView == null || string.IsNullOrEmpty(message) || voiceChat == null)
        {
            return;
        }
        ActiveByVoiceChat[voiceChat.GetInstanceID()] =
            new ActiveNotification(voiceChat, message);
    }

    internal static void CancelExpected(PlayerAvatar player)
    {
        PlayerVoiceChat? voiceChat = GetVoiceChat(player);
        if (voiceChat != null)
        {
            ActiveByVoiceChat.Remove(voiceChat.GetInstanceID());
        }
    }

    internal static bool PrepareVoiceUpdate(PlayerVoiceChat voiceChat)
    {
        if (!SuppressionEnabled() || !TryGetActive(voiceChat, out _))
        {
            return false;
        }
        voiceChat.OverrideDisableEnemyInvestigate(0.25f);
        return true;
    }

    internal static void RestoreMicrophoneInvestigation(PlayerVoiceChat voiceChat)
    {
        if (!SemiFunc.IsMultiplayer() || !SemiFunc.IsMasterClient() ||
            EnemyDirector.instance == null ||
            !TryGetActive(voiceChat, out ActiveNotification active))
        {
            return;
        }

        PlayerAvatar? player = ReadReference<PlayerAvatar>(
            VoiceChatPlayerAvatarField,
            voiceChat);
        if (player == null)
        {
            return;
        }

        PlayerDeathHead? deathHead = ReadReference<PlayerDeathHead>(
            PlayerDeathHeadField,
            player);
        bool spectated = deathHead != null && ReadBool(DeathHeadSpectatedField, deathHead);
        if (ReadBool(PlayerDisabledField, player) && !spectated)
        {
            active.NoTtsCrawlingCounter = 0;
            return;
        }

        bool microphoneTriggered;
        if (spectated)
        {
            active.NoTtsCrawlingCounter = 0;
            microphoneTriggered = ReadFloat(ClipLoudnessNoTtsField, voiceChat) > 0.05f;
        }
        else if (ReadBool(PlayerCrawlingField, player))
        {
            active.NoTtsCrawlingCounter =
                ReadFloat(ClipLoudnessNoTtsField, voiceChat) > 0.05f
                    ? active.NoTtsCrawlingCounter + 1
                    : 0;
            microphoneTriggered = active.NoTtsCrawlingCounter > 10;
        }
        else
        {
            active.NoTtsCrawlingCounter = 0;
            microphoneTriggered = ReadFloat(ClipLoudnessNoTtsField, voiceChat) >
                (ReadBool(PlayerCrouchingField, player) ? 0.05f : 0.025f);
        }

        if (!microphoneTriggered || Time.time < active.NextMicrophoneInvestigateAt)
        {
            return;
        }

        Vector3 position = player.PlayerVisionTarget.VisionTransform.position;
        PhysGrabObject? deathHeadObject = deathHead != null
            ? ReadReference<PhysGrabObject>(DeathHeadPhysGrabObjectField, deathHead)
            : null;
        if (spectated && deathHeadObject != null)
        {
            position = deathHeadObject.centerPoint;
        }
        active.NextMicrophoneInvestigateAt = Time.time + InvestigateCooldownSeconds;
        EnemyDirector.instance.SetInvestigate(position, 5f);
    }

    internal static void Clear() => ActiveByVoiceChat.Clear();

    internal static bool HasActiveNotifications()
    {
        bool found = false;
        foreach (int key in new List<int>(ActiveByVoiceChat.Keys))
        {
            if (!ActiveByVoiceChat.TryGetValue(
                    key,
                    out ActiveNotification active) ||
                active.VoiceChat == null)
            {
                ActiveByVoiceChat.Remove(key);
                continue;
            }
            if (TryGetActive(active.VoiceChat, out _))
            {
                found = true;
            }
        }
        return found;
    }

    private static bool TryGetActive(
        PlayerVoiceChat voiceChat,
        out ActiveNotification active)
    {
        int key = voiceChat.GetInstanceID();
        if (!ActiveByVoiceChat.TryGetValue(key, out active) || active.VoiceChat == null)
        {
            ActiveByVoiceChat.Remove(key);
            active = null!;
            return false;
        }

        float now = Time.time;
        if (now > active.HardExpiresAt)
        {
            ActiveByVoiceChat.Remove(key);
            active = null!;
            return false;
        }
        bool playing = voiceChat.ttsAudioSource != null && voiceChat.ttsAudioSource.isPlaying;
        if (playing)
        {
            active.PlaybackObserved = true;
            active.LastPlaybackAt = now;
            return true;
        }
        if (!active.PlaybackObserved && now <= active.AwaitPlaybackUntil)
        {
            return true;
        }
        if (active.PlaybackObserved && now - active.LastPlaybackAt <= PlaybackSilenceGraceSeconds)
        {
            return true;
        }

        ActiveByVoiceChat.Remove(key);
        active = null!;
        return false;
    }

    private static bool SuppressionEnabled() =>
        StageRolesPlugin.Instance != null &&
        StageRolesPlugin.Instance.Settings.Enabled.Value;

    private static PlayerVoiceChat? GetVoiceChat(PlayerAvatar? player) =>
        player == null
            ? null
            : ReadReference<PlayerVoiceChat>(PlayerVoiceChatField, player);

    private static T? ReadReference<T>(FieldInfo? field, object instance)
        where T : class => field?.GetValue(instance) as T;

    private static float ReadFloat(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is float value ? value : 0f;

    private static int ReadInt(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is int value ? value : 0;

    private static bool ReadBool(FieldInfo? field, object instance) =>
        field?.GetValue(instance) is bool value && value;
}
