using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class RoleNotifier
{
    private const float NotificationReadyTimeoutSeconds = 10f;
    private const float VoiceWaitTimeoutSeconds = 15f;
    private const float StageFluxQuietPeriodSeconds = 1.5f;
    private const float PollIntervalSeconds = 0.1f;
    private const float QueueHeadWaitTimeoutSeconds = 15f;
    private const float ReservationSeconds = 2f;
    private const float PlaybackStartGraceSeconds = 2f;
    private const float PlaybackQuietPeriodSeconds = 0.25f;
    private const int MaximumQueuedNotifications = 64;
    private const string NotificationOwner = "RoleShuffle.Notifications";
    private static readonly FieldInfo? VoiceChatField =
        AccessTools.Field(typeof(PlayerAvatar), "voiceChat");
    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly Queue<PendingNotification> _pendingNotifications = new();
    private int _generation;
    private int _queueRunnerGeneration = -1;

    private sealed class PendingNotification
    {
        internal PendingNotification(
            int generation,
            PlayerAvatar player,
            string message,
            string source,
            float reservationSeconds,
            Func<bool>? isValid = null,
            Action? onSent = null,
            Action? onFinished = null)
        {
            Generation = generation;
            Player = player;
            Message = message;
            Source = source;
            ReservationSeconds = reservationSeconds;
            IsValid = isValid;
            OnSent = onSent;
            OnFinished = onFinished;
        }

        internal int Generation { get; }
        internal PlayerAvatar Player { get; }
        internal string Message { get; }
        internal string Source { get; }
        internal float ReservationSeconds { get; }
        internal Func<bool>? IsValid { get; }
        internal Action? OnSent { get; }
        internal Action? OnFinished { get; }
    }

    internal RoleNotifier(MonoBehaviour coroutineOwner, StageRolesConfig config)
    {
        _coroutineOwner = coroutineOwner;
        _config = config;
    }

    internal void Begin(IReadOnlyList<RoleAssignment> assignments)
    {
        End();
        if (!_config.AnnouncementsEnabled.Value)
        {
            return;
        }
        int generation = _generation;
        _coroutineOwner.StartCoroutine(Announce(assignments, generation));
    }

    internal void End()
    {
        ClearPending(clearExternalState: true);
    }

    internal void ResetPendingLocally()
    {
        ClearPending(clearExternalState: false);
    }

    private void ClearPending(bool clearExternalState)
    {
        _generation++;
        foreach (PendingNotification pending in _pendingNotifications)
            InvokeCallback(pending.OnFinished);
        _pendingNotifications.Clear();
        _queueRunnerGeneration = -1;
        if (clearExternalState)
        {
            StageFluxCompatibility.ClearNotifications();
        }
    }

    internal void ResetPending() => End();

    internal void Notify(PlayerAvatar player, string message)
    {
        if (!_config.AnnouncementsEnabled.Value)
        {
            return;
        }
        Enqueue(player, message, "role effect");
    }

    internal void NotifyConditional(
        PlayerAvatar player,
        string message,
        Func<bool> isValid)
    {
        if (!_config.AnnouncementsEnabled.Value)
        {
            return;
        }
        Enqueue(player, message, "role effect", isValid);
    }

    internal bool CanQueueNotification => _pendingNotifications.Count < MaximumQueuedNotifications;

    internal bool NotifyExhaustion(PlayerAvatar player, string message,
        Func<bool> isValid, Action onSent, Action onFinished) =>
        _config.AnnouncementsEnabled.Value && CanQueueNotification &&
        Enqueue(player, message, "ability exhausted", isValid,
            ReservationSeconds, onSent, onFinished);

    internal bool NotifyCountdown(
        PlayerAvatar player,
        string message,
        Func<bool> isValid,
        Action? onSent = null)
    {
        if (!_config.AnnouncementsEnabled.Value)
        {
            return false;
        }
        if (player == null || !player.gameObject.activeInHierarchy ||
            player.photonView == null || string.IsNullOrEmpty(message))
        {
            return false;
        }
        try
        {
            if (!isValid())
            {
                return false;
            }
            StageFluxCompatibility.ExpectNotification(player, message);
            player.ChatMessageSend(message);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Sent immediate countdown through player view " +
                $"{player.photonView.ViewID}: {message}");
            onSent?.Invoke();
            return true;
        }
        catch (Exception exception)
        {
            StageFluxCompatibility.CancelExpectedNotification(player);
            StageRolesPlugin.ModLogger.LogWarning(
                $"Immediate countdown failed for player view " +
                $"{player.photonView?.ViewID ?? 0}: {exception.Message}");
            return false;
        }
    }

    internal void NotifyResponse(PlayerAvatar player, string message) =>
        Enqueue(player, message, "automatic response");

    internal bool HasPendingNotifications =>
        _pendingNotifications.Count > 0 ||
        _queueRunnerGeneration == _generation;

    private bool Enqueue(
        PlayerAvatar player,
        string message,
        string source,
        Func<bool>? isValid = null,
        float reservationSeconds = ReservationSeconds,
        Action? onSent = null,
        Action? onFinished = null)
    {
        if (player == null || !player.gameObject.activeInHierarchy ||
            player.photonView == null || string.IsNullOrEmpty(message))
        {
            return false;
        }
        if (_pendingNotifications.Count >= MaximumQueuedNotifications)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role notification queue is full; skipped {source} for " +
                $"player view {player.photonView.ViewID}: {message}");
            return false;
        }

        int generation = _generation;
        _pendingNotifications.Enqueue(new PendingNotification(
            generation,
            player,
            message,
            source,
            reservationSeconds,
            isValid,
            onSent,
            onFinished));
        if (_queueRunnerGeneration == generation)
        {
            return true;
        }
        _queueRunnerGeneration = generation;
        _coroutineOwner.StartCoroutine(ProcessQueue(generation));
        return true;
    }

    private IEnumerator Announce(
        IReadOnlyList<RoleAssignment> assignments,
        int generation)
    {
        bool stageFluxInstalled =
            Chainloader.PluginInfos.ContainsKey(StageRolesPlugin.StageFluxGuid);
        float delay = stageFluxInstalled
            ? _config.StageFluxAnnouncementDelaySeconds.Value
            : _config.AnnouncementDelaySeconds.Value;
        yield return new WaitForSeconds(Mathf.Clamp(delay, 0f, 30f));

        float readyDeadline = Time.time + NotificationReadyTimeoutSeconds;
        while (generation == _generation && !CanNotify() && Time.time < readyDeadline)
        {
            yield return new WaitForSeconds(PollIntervalSeconds);
        }
        if (generation != _generation || !CanNotify())
        {
            yield break;
        }

        float requiredQuietPeriod = stageFluxInstalled
            ? StageFluxQuietPeriodSeconds
            : 0f;
        float voiceDeadline = Time.time + VoiceWaitTimeoutSeconds;
        float quietSince = -1f;
        bool quietPeriodReached = false;
        while (generation == _generation && Time.time < voiceDeadline)
        {
            if (AnyNotificationBusy())
            {
                quietSince = -1f;
            }
            else
            {
                if (quietSince < 0f)
                {
                    quietSince = Time.time;
                }
                if (Time.time - quietSince >= requiredQuietPeriod)
                {
                    quietPeriodReached = true;
                    break;
                }
            }
            yield return new WaitForSeconds(PollIntervalSeconds);
        }

        if (generation != _generation || !CanNotify() || !quietPeriodReached)
        {
            yield break;
        }

        int queued = 0;
        foreach (RoleAssignment assignment in assignments)
        {
            PlayerAvatar player = assignment.Player;
            if (player == null || !player.gameObject.activeInHierarchy ||
                player.photonView == null)
            {
                continue;
            }
            Enqueue(
                player,
                RoleCatalog.AssignmentName(
                    assignment.AssignedRole,
                    assignment.Role),
                "role assignment");
            queued++;
        }

        StageRolesPlugin.ModLogger.LogInfo(
            $"Queued {queued}/{assignments.Count} assigned roles for sequential player chat.");
    }

    private IEnumerator ProcessQueue(int generation)
    {
        try
        {
            while (generation == _generation && _pendingNotifications.Count > 0)
            {
                PendingNotification pending = _pendingNotifications.Peek();
                if (pending.Generation != generation ||
                    pending.Player == null ||
                    !pending.Player.gameObject.activeInHierarchy ||
                    pending.Player.photonView == null ||
                    !IsStillValid(pending))
                {
                    _pendingNotifications.Dequeue();
                    InvokeCallback(pending.OnFinished);
                    continue;
                }

                float deadline = Time.realtimeSinceStartup +
                                 QueueHeadWaitTimeoutSeconds;
                bool reserved = false;
                while (generation == _generation &&
                       Time.realtimeSinceStartup < deadline)
                {
                    if (CanNotify() && !AnyNotificationBusy() &&
                        StageFluxCompatibility.TryReserveNotificationWindow(
                            NotificationOwner,
                            pending.ReservationSeconds))
                    {
                        reserved = true;
                        break;
                    }
                    yield return new WaitForSecondsRealtime(PollIntervalSeconds);
                }

                if (generation != _generation)
                {
                    yield break;
                }

                _pendingNotifications.Dequeue();
                if (!reserved || !IsStillValid(pending))
                {
                    InvokeCallback(pending.OnFinished);
                    if (!reserved)
                    {
                        StageRolesPlugin.ModLogger.LogWarning(
                            $"Role notification expired while waiting for a shared " +
                            $"notification window ({pending.Source}): {pending.Message}");
                    }
                    continue;
                }

                bool sent = Send(pending);
                if (sent) InvokeCallback(pending.OnSent);
                InvokeCallback(pending.OnFinished);
                if (sent)
                {
                    yield return WaitForNotificationCompletion(generation);
                }
            }
        }
        finally
        {
            if (_queueRunnerGeneration == generation)
            {
                _queueRunnerGeneration = -1;
            }
        }
    }

    private static bool IsStillValid(PendingNotification pending)
    {
        try
        {
            return pending.IsValid?.Invoke() != false;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Skipped invalidated {pending.Source}: {exception.Message}");
            return false;
        }
    }

    private static void InvokeCallback(Action? callback)
    {
        try { callback?.Invoke(); }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug($"Notification completion callback failed: {exception.Message}");
        }
    }

    private static bool Send(PendingNotification pending)
    {
        try
        {
            StageFluxCompatibility.ExpectNotification(
                pending.Player,
                pending.Message);
            pending.Player.ChatMessageSend(pending.Message);
            StageRolesPlugin.ModLogger.LogDebug(
                $"Sent queued {pending.Source} through player view " +
                $"{pending.Player.photonView.ViewID}: {pending.Message}");
            return true;
        }
        catch (Exception exception)
        {
            StageFluxCompatibility.CancelExpectedNotification(pending.Player);
            StageRolesPlugin.ModLogger.LogWarning(
                $"Queued {pending.Source} failed for player view " +
                $"{pending.Player.photonView?.ViewID ?? 0}: {exception.Message}");
            return false;
        }
    }

    private IEnumerator WaitForNotificationCompletion(int generation)
    {
        float playbackDeadline = Time.realtimeSinceStartup +
                                 PlaybackStartGraceSeconds;
        float quietSince = -1f;
        bool activityObserved = false;
        while (generation == _generation)
        {
            bool busy = AnyNotificationBusy();
            if (busy)
            {
                activityObserved = true;
                quietSince = -1f;
            }
            else if (activityObserved ||
                     Time.realtimeSinceStartup >= playbackDeadline)
            {
                if (quietSince < 0f)
                {
                    quietSince = Time.realtimeSinceStartup;
                }
                if (Time.realtimeSinceStartup - quietSince >=
                    PlaybackQuietPeriodSeconds)
                {
                    yield break;
                }
            }
            yield return new WaitForSecondsRealtime(PollIntervalSeconds);
        }
    }

    internal static bool CanNotify() =>
        SemiFunc.IsMasterClientOrSingleplayer() &&
        GameDirector.instance != null &&
        GameDirector.instance.currentState == GameDirector.gameState.Main;

    internal static bool AnyTtsPlaying()
    {
        List<PlayerAvatar>? players = SemiFunc.PlayerGetList();
        if (players == null)
        {
            return false;
        }
        foreach (PlayerAvatar player in players)
        {
            if (VoiceChatField?.GetValue(player) is PlayerVoiceChat voiceChat &&
                voiceChat.ttsAudioSource != null && voiceChat.ttsAudioSource.isPlaying)
            {
                return true;
            }
        }
        return false;
    }

    internal static bool AnyNotificationBusy()
    {
        bool ttsPlaying = AnyTtsPlaying();
        return StageFluxCompatibility.TryGetNotificationBusy(out bool busy)
            ? busy || ttsPlaying
            : NotificationEnemyReactionGuard.HasActiveNotifications() ||
              ttsPlaying;
    }
}
