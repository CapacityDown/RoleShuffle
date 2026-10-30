using System;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal static class PrivatePlayerSpeech
{
    internal static bool IsDispatching { get; private set; }

    internal static bool Send(PlayerAvatar receiver, string message)
    {
        if (!RoleNotifier.CanNotify() || !PlayerState.IsLiving(receiver) || receiver.photonView?.Owner == null) return false;
        bool previous = IsDispatching;
        IsDispatching = true;
        try
        {
            bool local = receiver.photonView.Owner == PhotonNetwork.LocalPlayer;
            if (local) StageFluxCompatibility.ExpectNotification(receiver, message);
            receiver.photonView.RPC(nameof(PlayerAvatar.ChatMessageSendRPC), receiver.photonView.Owner, new object[] { message, false, "en-US" });
            if (local) PlayerMessageActivity.Observe(receiver);
            else PlayerMessageActivity.ReserveRemote(receiver, Math.Clamp(3f + message.Length * 0.35f, 4f, 120f));
            return true;
        }
        finally { IsDispatching = previous; }
    }
}
