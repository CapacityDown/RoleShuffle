using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed partial class StageRoleController
{
    internal void OnSignalmanChat(PlayerAvatar sender, string message, PhotonMessageInfo info)
    {
        if (!_stageReady || !_assignmentsInitialized || !_config.Enabled.Value || !IsAuthority() ||
            PrivatePlayerSpeech.IsDispatching || !PlayerState.IsLiving(sender) || sender.photonView?.Owner == null ||
            info.Sender == null || info.Sender.ActorNumber != sender.photonView.Owner.ActorNumber ||
            (sender.photonView.Owner == PhotonNetwork.LocalPlayer && !InfluenzaChatPatches.LocalSubmission) ||
            !SignalmanRules.IsOrdinaryChat(message, _config.SignalmanExcludedCommands.Value, _config.SignalmanExcludedPrefixes.Value)) return;

        RoleAssignment? assignment = null;
        foreach (RoleAssignment candidate in _assignments)
            if (candidate.Player == sender && RoleCatalog.HasCapability(candidate.Role, StageRole.Signalman))
                { assignment = candidate; break; }
        if (assignment == null || Time.time < assignment.SignalmanNextTransmitAt) return;

        int generation = _signalmanGeneration;
        long speechVersion = PlayerMessageActivity.Version(sender);
        float expiresAt = Time.realtimeSinceStartup + _config.SignalmanMaximumDelaySeconds.Value;
        bool accepted = false;
        foreach (RoleAssignment target in _assignments)
        {
            PlayerAvatar receiver = target.Player;
            if (receiver == sender || !PlayerState.IsLiving(receiver) || receiver.photonView?.Owner == null) continue;
            accepted |= _notifier.NotifyPrivate(receiver, message,
                () => generation == _signalmanGeneration && _stageReady &&
                      _assignments.Contains(assignment) && assignment.Player == sender &&
                      _assignments.Contains(target) && target.Player == receiver &&
                      RoleCatalog.HasCapability(assignment.Role, StageRole.Signalman) &&
                      PlayerState.IsLiving(sender) && PlayerState.IsLiving(receiver) &&
                      Time.realtimeSinceStartup <= expiresAt,
                () => PrivatePlayerSpeech.Send(receiver, message, sender, speechVersion));
        }
        if (accepted)
            assignment.SignalmanNextTransmitAt = Time.time + _config.SignalmanCooldownSeconds.Value;
    }

    private int _signalmanGeneration;
    private readonly HashSet<PlayerAvatar> _signalmanReportedDeaths = new();

    private void TickSignalman() => _signalmanReportedDeaths.RemoveWhere(player => player == null || PlayerState.IsLiving(player));

    private void NotifySignalmanDeath(PlayerAvatar victim)
    {
        if (!_config.SignalmanDeathAlerts.Value || !_config.AnnouncementsEnabled.Value || !_signalmanReportedDeaths.Add(victim)) return;
        int generation = _signalmanGeneration;
        float expiresAt = Time.realtimeSinceStartup + _config.SignalmanMaximumDelaySeconds.Value;
        string name = string.Concat(PlayerIdentity.Name(victim).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        if (name.Length == 0) name = "Player";
        if (name.Length > 20) name = name.Substring(0, 20);
        string message = "Down:" + name;
        foreach (RoleAssignment assignment in _assignments)
        {
            PlayerAvatar receiver = assignment.Player;
            if (receiver == victim || !RoleCatalog.HasCapability(assignment.Role, StageRole.Signalman) || !PlayerState.IsLiving(receiver)) continue;
            _notifier.NotifyPrivate(receiver, message,
                () => generation == _signalmanGeneration && _stageReady && _assignments.Contains(assignment) &&
                      RoleCatalog.HasCapability(assignment.Role, StageRole.Signalman) &&
                      PlayerState.IsLiving(receiver) && victim != null && !PlayerState.IsLiving(victim) &&
                      Time.realtimeSinceStartup <= expiresAt,
                () => PrivatePlayerSpeech.Send(receiver, message));
        }
    }
}

internal static class PrivatePlayerSpeech
{
    internal static bool IsDispatching { get; private set; }

    internal static bool Send(PlayerAvatar receiver, string message, PlayerAvatar? originalSpeaker = null, long originalVersion = 0)
    {
        if (!RoleNotifier.CanNotify() || !PlayerState.IsLiving(receiver) || receiver.photonView?.Owner == null) return false;
        bool previous = IsDispatching;
        IsDispatching = true;
        try
        {
            // Stop only the original proximity utterance, never a later chat.
            // Target one listener, so nearby players do not hear a chorus of copies.
            if (originalSpeaker != null && originalSpeaker.photonView != null &&
                originalVersion == PlayerMessageActivity.Version(originalSpeaker))
                originalSpeaker.photonView.RPC(nameof(PlayerAvatar.ChatMessageSendRPC), receiver.photonView.Owner, new object[] { "", false });

            bool local = receiver.photonView.Owner == PhotonNetwork.LocalPlayer;
            if (local) StageFluxCompatibility.ExpectNotification(receiver, message);
            receiver.photonView.RPC(nameof(PlayerAvatar.ChatMessageSendRPC), receiver.photonView.Owner, new object[] { message, false });
            if (local) PlayerMessageActivity.Observe(receiver);
            else PlayerMessageActivity.ReserveRemote(receiver, SignalmanRules.PlaybackReservation(message));
            return true;
        }
        finally { IsDispatching = previous; }
    }
}
