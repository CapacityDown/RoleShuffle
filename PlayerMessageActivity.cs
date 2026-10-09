using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace REPOJP.StageRoles;

// Observe the native speech path too, including vanilla guests and other mods.
// This tracks playback only; it does not suppress player input or enemy hearing.
internal static class PlayerMessageActivity
{
    private const float StartGraceSeconds = 10f;
    private const float QuietSeconds = 0.25f;
    private static readonly FieldInfo? VoiceChatField = AccessTools.Field(typeof(PlayerAvatar), "voiceChat");
    private static readonly Dictionary<PlayerAvatar, Playback> Active = new();

    private sealed class Playback
    {
        internal float AwaitUntil = Time.realtimeSinceStartup + StartGraceSeconds;
        internal bool Observed;
        internal float LastPlayingAt;
        internal float PriorityUntil;
    }

    internal static void Observe(PlayerAvatar player, float prioritySeconds = 0f)
    {
        AnyBusy(); // Prune expired or destroyed avatars across scene changes.
        if (player != null && player.gameObject.activeInHierarchy)
        {
            float previous = Active.TryGetValue(player, out var active) ? active.PriorityUntil : 0f;
            Active[player] = new Playback
            {
                PriorityUntil = System.Math.Max(previous, Time.realtimeSinceStartup + prioritySeconds)
            };
        }
    }

    internal static bool IsBusy(PlayerAvatar? player)
    {
        if (ReferenceEquals(player, null)) return false;
        if (player == null || !player.gameObject.activeInHierarchy)
        {
            Active.Remove(player!);
            return false;
        }
        bool playing = VoiceChatField?.GetValue(player) is PlayerVoiceChat voice &&
            voice.ttsAudioSource != null && voice.ttsAudioSource.isPlaying;
        if (!Active.TryGetValue(player, out var playback)) return playing;
        float now = Time.realtimeSinceStartup;
        if (playing)
        {
            playback.Observed = true;
            playback.LastPlayingAt = now;
            return true;
        }
        if (now < playback.PriorityUntil ||
            (playback.Observed ? now - playback.LastPlayingAt < QuietSeconds : now < playback.AwaitUntil))
            return true;
        Active.Remove(player);
        return false;
    }

    internal static bool AnyBusy()
    {
        bool busy = false;
        foreach (PlayerAvatar player in new List<PlayerAvatar>(Active.Keys)) busy |= IsBusy(player);
        return busy;
    }
}
