using System;
using System.Collections.Generic;
using Photon.Pun;

namespace REPOJP.StageRoles;

// Preserve authoritative holders, but withhold the native client-side activation
// condition while any holder is an Engineer. Restore only holders still present.
internal sealed class EngineerGrabVisibility
{
    private readonly Dictionary<PhysGrabObject, HashSet<int>> _hidden = new();

    internal void Hide(PhysGrabObject valuable)
    {
        PhotonView? view = valuable.GetComponent<PhotonView>();
        if (!SemiFunc.IsMasterClientOrSingleplayer() || !SemiFunc.IsMultiplayer() || view == null) return;
        if (!_hidden.TryGetValue(valuable, out var ids))
            _hidden.Add(valuable, ids = new HashSet<int>());
        foreach (PhysGrabber holder in valuable.playerGrabbing)
        {
            if (holder == null || holder.photonView == null || holder.photonView.ViewID <= 0) continue;
            int id = holder.photonView.ViewID;
            if (ids.Add(id)) view.RPC("GrabPlayerRemoveRPC", RpcTarget.Others, id);
        }
    }

    // A fresh native grab after suppression already broadcasts its own Add RPC.
    internal void NativeGrabWillBeVisible(PhysGrabObject valuable, int playerId)
    {
        if (_hidden.TryGetValue(valuable, out var ids)) ids.Remove(playerId);
    }

    internal void Tick(Func<PhysGrabObject, bool> suppress)
    {
        foreach (PhysGrabObject valuable in new List<PhysGrabObject>(_hidden.Keys))
            if (valuable == null || !suppress(valuable)) Restore(valuable!);
    }

    private void Restore(PhysGrabObject valuable)
    {
        if (!_hidden.TryGetValue(valuable, out var ids)) return;
        PhotonView? view = valuable != null ? valuable.GetComponent<PhotonView>() : null;
        if (valuable != null && SemiFunc.IsMasterClientOrSingleplayer() && SemiFunc.IsMultiplayer() && view != null)
        {
            foreach (PhysGrabber holder in valuable.playerGrabbing)
                if (holder != null && holder.photonView != null && ids.Contains(holder.photonView.ViewID))
                    view.RPC("GrabPlayerAddRPC", RpcTarget.Others, holder.photonView.ViewID);
        }
        _hidden.Remove(valuable!);
    }

    internal void Clear()
    {
        foreach (PhysGrabObject valuable in new List<PhysGrabObject>(_hidden.Keys)) Restore(valuable);
        _hidden.Clear();
    }
}
