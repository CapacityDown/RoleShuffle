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
    private const float CountdownPrioritySeconds = 1.25f;
    private const int MaximumQueuedNotifications = 64;
    private const string NotificationOwner = "RoleShuffle.Notifications";
    private static readonly FieldInfo? VoiceChatField =
        AccessTools.Field(typeof(PlayerAvatar), "voiceChat");
    private readonly MonoBehaviour _coroutineOwner;
    private readonly StageRolesConfig _config;
    private readonly NotificationQueue _assignmentQueue = new();
    private readonly Dictionary<PlayerAvatar, NotificationQueue> _playerQueues = new();
    private int _generation;
    private int _announcingGeneration = -1;

    private sealed class NotificationQueue
    {
        internal readonly Queue<PendingNotification> Pending = new();
        internal int RunnerGeneration = -1;
    }

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
            Action? onFinished = null,
            Func<bool>? dispatch = null)
        {
            Generation = generation;
            Player = player;
            Message = message;
            Source = source;
            ReservationSeconds = reservationSeconds;
            IsValid = isValid;
            OnSent = onSent;
            OnFinished = onFinished;
            Dispatch = dispatch;
        }

        internal int Generation { get; }
        internal PlayerAvatar Player { get; }
        internal string Message { get; }
        internal string Source { get; }
        internal float ReservationSeconds { get; }
        internal Func<bool>? IsValid { get; }
        internal Action? OnSent { get; }
        internal Action? OnFinished { get; }
        internal Func<bool>? Dispatch { get; }
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
        _announcingGeneration = generation;
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
        ClearQueue(_assignmentQueue);
        foreach (NotificationQueue queue in _playerQueues.Values) ClearQueue(queue);
        _playerQueues.Clear();
        _announcingGeneration = -1;
        if (clearExternalState)
        {
            StageFluxCompatibility.ClearNotifications();
        }
    }

    private static void ClearQueue(NotificationQueue queue)
    {
        while (queue.Pending.Count > 0) InvokeCallback(queue.Pending.Dequeue().OnFinished);
        queue.RunnerGeneration = -1;
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

    internal bool CanQueueNotification(PlayerAvatar player) =>
        !_playerQueues.TryGetValue(player, out var queue) ||
        queue.Pending.Count < MaximumQueuedNotifications;

    internal bool NotifyExhaustion(PlayerAvatar player, string message,
        Func<bool> isValid, Action onSent, Action onFinished) =>
        _config.AnnouncementsEnabled.Value && CanQueueNotification(player) &&
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
        if (!CanNotify() || player == null || !player.gameObject.activeInHierarchy ||
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
            // Counts are time-critical: replace current speech, and keep normal
            // notices out of the gaps between the final one-second counts.
            PlayerMessageActivity.Observe(player, CountdownPrioritySeconds);
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

    internal bool NotifyPrivate(PlayerAvatar player, string message, Func<bool> isValid, Func<bool> dispatch) =>
        Enqueue(player, message, "private radio", isValid, dispatch: dispatch);

    private bool HasPendingAssignments =>
        _announcingGeneration == _generation || _assignmentQueue.Pending.Count > 0 ||
        _assignmentQueue.RunnerGeneration == _generation;

    internal bool HasPendingNotificationsFor(PlayerAvatar player) =>
        HasPendingAssignments ||
        (_playerQueues.TryGetValue(player, out var queue) &&
         (queue.Pending.Count > 0 || queue.RunnerGeneration == _generation));

    internal bool IsPlayerBusy(PlayerAvatar player) =>
        HasPendingNotificationsFor(player) || PlayerMessageActivity.IsBusy(player);

    // Ability speech remains audible to enemies and independent of notification settings.
    internal bool TrySendPlayerMessage(PlayerAvatar player, string message)
    {
        if (!CanNotify() || player == null || !player.gameObject.activeInHierarchy ||
            player.photonView == null || string.IsNullOrEmpty(message) || IsPlayerBusy(player)) return false;
        try
        {
            player.ChatMessageSend(message);
            PlayerMessageActivity.Observe(player);
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug($"Player message was delayed: {exception.Message}");
            return false;
        }
    }

    private bool Enqueue(
        PlayerAvatar player,
        string message,
        string source,
        Func<bool>? isValid = null,
        float reservationSeconds = ReservationSeconds,
        Action? onSent = null,
        Action? onFinished = null,
        bool shared = false,
        Func<bool>? dispatch = null)
    {
        if (player == null || !player.gameObject.activeInHierarchy ||
            player.photonView == null || string.IsNullOrEmpty(message))
        {
            return false;
        }
        NotificationQueue queue;
        if (shared) queue = _assignmentQueue;
        else if (_playerQueues.TryGetValue(player, out var existing)) queue = existing;
        else
            _playerQueues[player] = queue = new NotificationQueue();
        if (queue.Pending.Count >= MaximumQueuedNotifications)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Role notification queue is full; skipped {source} for " +
                $"player view {player.photonView.ViewID}: {message}");
            return false;
        }

        int generation = _generation;
        queue.Pending.Enqueue(new PendingNotification(
            generation,
            player,
            message,
            source,
            reservationSeconds,
            isValid,
            onSent,
            onFinished,
            dispatch));
        if (queue.RunnerGeneration == generation)
        {
            return true;
        }
        queue.RunnerGeneration = generation;
        _coroutineOwner.StartCoroutine(ProcessQueue(queue, shared ? null : player, generation));
        return true;
    }

    private IEnumerator Announce(
        IReadOnlyList<RoleAssignment> assignments,
        int generation)
    {
        try { yield return AnnounceRoles(assignments, generation); }
        finally { if (_announcingGeneration == generation) _announcingGeneration = -1; }
    }

    private IEnumerator AnnounceRoles(
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
                "role assignment", shared: true);
            queued++;
        }

        StageRolesPlugin.ModLogger.LogInfo(
            $"Queued {queued}/{assignments.Count} assigned roles for sequential player chat.");
    }

    private IEnumerator ProcessQueue(NotificationQueue queue, PlayerAvatar? player, int generation)
    {
        bool shared = ReferenceEquals(player, null);
        try
        {
            while (generation == _generation && queue.Pending.Count > 0)
            {
                PendingNotification pending = queue.Pending.Peek();
                if (pending.Generation != generation ||
                    pending.Player == null ||
                    !pending.Player.gameObject.activeInHierarchy ||
                    pending.Player.photonView == null ||
                    !IsStillValid(pending))
                {
                    queue.Pending.Dequeue();
                    InvokeCallback(pending.OnFinished);
                    continue;
                }

                // Initial roles own the shared channel; do not expire a player's
                // response simply because a large party is still being announced.
                while (generation == _generation && !shared && HasPendingAssignments)
                    yield return new WaitForSecondsRealtime(PollIntervalSeconds);

                float deadline = Time.realtimeSinceStartup +
                                 (pending.Dispatch != null
                                     ? _config.SignalmanMaximumDelaySeconds.Value
                                     : QueueHeadWaitTimeoutSeconds);
                bool reserved = false;
                while (generation == _generation &&
                       Time.realtimeSinceStartup < deadline)
                {
                    if (!IsStillValid(pending)) break;
                    bool available = shared
                        ? !AnyNotificationBusy() && StageFluxCompatibility.TryReserveNotificationWindow(
                            NotificationOwner, pending.ReservationSeconds)
                        : !HasPendingAssignments && !PlayerMessageActivity.IsBusy(player);
                    if (CanNotify() && available)
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

                queue.Pending.Dequeue();
                if (!reserved || !IsStillValid(pending))
                {
                    InvokeCallback(pending.OnFinished);
                    if (!reserved)
                    {
                        StageRolesPlugin.ModLogger.LogWarning(
                            $"Role notification expired while waiting for a " +
                            $"notification window ({pending.Source}): {pending.Message}");
                    }
                    continue;
                }

                bool sent = Send(pending);
                if (sent) InvokeCallback(pending.OnSent);
                InvokeCallback(pending.OnFinished);
                if (sent)
                {
                    yield return WaitForNotificationCompletion(player, generation);
                }
            }
        }
        finally
        {
            if (queue.RunnerGeneration == generation)
            {
                queue.RunnerGeneration = -1;
                if (!shared && _playerQueues.TryGetValue(player!, out var current) &&
                    ReferenceEquals(current, queue)) _playerQueues.Remove(player!);
            }
        }
    }

    private static bool IsStillValid(PendingNotification pending)
    {
        if (pending.Player == null || !pending.Player.gameObject.activeInHierarchy ||
            pending.Player.photonView == null) return false;
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
            if (pending.Dispatch != null) return pending.Dispatch();
            StageFluxCompatibility.ExpectNotification(
                pending.Player,
                pending.Message);
            pending.Player.ChatMessageSend(pending.Message);
            PlayerMessageActivity.Observe(pending.Player);
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

    private IEnumerator WaitForNotificationCompletion(PlayerAvatar? player, int generation)
    {
        float playbackDeadline = Time.realtimeSinceStartup +
                                 PlaybackStartGraceSeconds;
        float quietSince = -1f;
        bool activityObserved = false;
        while (generation == _generation)
        {
            bool busy = ReferenceEquals(player, null) ? AnyNotificationBusy() : PlayerMessageActivity.IsBusy(player);
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
        // Sample tracked players even when a native audio source is playing.
        bool trackedBusy = PlayerMessageActivity.AnyBusy();
        bool ttsPlaying = AnyTtsPlaying() || trackedBusy;
        return StageFluxCompatibility.TryGetNotificationBusy(out bool busy)
            ? busy || ttsPlaying
            : NotificationEnemyReactionGuard.HasActiveNotifications() ||
              ttsPlaying;
    }
}
