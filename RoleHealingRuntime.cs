using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

namespace REPOJP.StageRoles;

// One outstanding capped heal per recipient, shared by Medic and Mage.
// An unacknowledged request keeps its budget reservation: refunding it on a
// timer could exceed the cap if the owner's RPC arrives after the timeout.
internal static class RoleHealingRuntime
{
    private sealed class Request
    {
        internal int Amount;
        internal float Deadline;
        internal Action<int> Complete = null!;
    }

    private static readonly Dictionary<PlayerAvatar, Request> Pending = new();

    internal static void Clear() => Pending.Clear();
    internal static void Forget(PlayerAvatar player) => Pending.Remove(player);

    // One-shot full-health rewards have their own count limit. They must not
    // disappear behind a Medic/Mage recipient lock or refund that healer's
    // reservation. Vanilla clamps on the owning client. Send maximum HP rather
    // than the host's missing HP, which may be stale for an unmodded guest.
    internal static bool TryHealToFull(PlayerAvatar player)
    {
        if (player == null || player.playerHealth == null || !PlayerState.IsLiving(player) ||
            (SemiFunc.IsMultiplayer() && player.photonView == null) ||
            !PlayerState.TryGetMaximumHealth(player, out int maximum) || maximum <= 0)
            return false;
        player.playerHealth.HealOther(maximum, true);
        return true;
    }

    internal static bool TryHeal(PlayerAvatar player, int amount, Action<int> complete)
    {
        if (player == null || player.playerHealth == null || amount <= 0)
            return false;
        if (SemiFunc.IsMultiplayer() && player.photonView == null)
            return false;
        if (Pending.TryGetValue(player, out Request? previous))
        {
            if (Time.unscaledTime < previous.Deadline)
                return false;
            // Keep the previous reservation charged when its outcome is unknown.
            Pending.Remove(player);
        }
        if (!PlayerState.TryGetCurrentHealth(player, out int health) ||
            !PlayerState.TryGetMaximumHealth(player, out int maximum) || health >= maximum)
            return false;

        Request request = new()
        {
            Amount = Math.Min(amount, maximum - health),
            Deadline = Time.unscaledTime + 5f,
            Complete = complete
        };
        bool local = !SemiFunc.IsMultiplayer() || player.photonView.IsMine;
        Pending[player] = request;
        try
        {
            player.playerHealth.HealOther(request.Amount, true);
            if (local && Pending.Remove(player))
            {
                int restored = PlayerState.TryGetCurrentHealth(player, out int after)
                    ? Math.Max(0, after - health) : 0;
                complete(Math.Min(request.Amount, restored));
            }
            return true;
        }
        catch
        {
            // A remote send may have succeeded before throwing. Do not refund
            // an uncertain result or let the caller repeat an unbounded heal.
            if (local && Pending.Remove(player))
            {
                int restored = PlayerState.TryGetCurrentHealth(player, out int after)
                    ? Math.Max(0, after - health) : 0;
                complete(Math.Min(request.Amount, restored));
            }
            throw;
        }
    }

    internal static void Observe(PlayerAvatar player, int before, int after)
    {
        if (after <= before || !Pending.Remove(player, out Request? request))
            return;
        // Vanilla remote health updates do not identify the healing source.
        // A smaller increase may belong to another healer before our request
        // arrives. Refunding the difference would allow the cap to be exceeded.
        // Release the recipient lock, but keep the full reservation charged.
        request.Complete(request.Amount);
    }
}
