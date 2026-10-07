using System;
using Photon.Pun;

namespace REPOJP.StageRoles;

// Role/base resets reduce Health without vanilla's negative-upgrade Hurt call.
// Both RPCs used here exist on unmodded clients.
internal static class HealthUpgradeReset
{
    internal static int ScaleRemaining(int health, int maximum, int nextMaximum)
    {
        if (health <= 0) return 0;
        maximum = Math.Max(1, maximum);
        nextMaximum = Math.Max(1, nextMaximum);
        return Math.Max(1, (int)((long)Math.Min(health, maximum) * nextMaximum / maximum));
    }

    internal static void Apply(string steamId, int currentLevel, int targetLevel)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        PlayerAvatar? player = SemiFunc.PlayerAvatarGetFromSteamID(steamId);
        PlayerHealth? playerHealth = player?.playerHealth;
        int health;
        int maximum;
        PhotonView? view = null;
        if (playerHealth != null)
        {
            if (!PlayerState.TryGetCurrentHealth(player, out health) ||
                !PlayerState.TryGetMaximumHealth(player, out maximum))
                throw new InvalidOperationException("Cannot read HP before removing a Health upgrade.");
            if (SemiFunc.IsMultiplayer())
            {
                view = playerHealth.GetComponent<PhotonView>() ?? player!.photonView;
                if (view == null)
                    throw new InvalidOperationException("PlayerHealth has no PhotonView.");
            }
        }
        else
        {
            // Departed players still need their saved HP converted before rejoining.
            health = StatsManager.instance.GetPlayerHealth(steamId);
            maximum = (int)Math.Clamp(100L + 20L * currentLevel, 1L, int.MaxValue);
        }
        int nextMaximum = (int)Math.Clamp(
            (long)maximum + 20L * (targetLevel - (long)currentLevel), 1L, int.MaxValue);
        int remaining = ScaleRemaining(health, maximum, nextMaximum);

        // UpdateStat changes the level without Heal/Hurt. Persist HP explicitly:
        // UpdateHealthRPC's SetPlayerHealth is ignored while entering the shop.
        PunManager.instance.UpdateStat("playerUpgradeHealth", steamId, targetLevel);
        PunManager.instance.UpdateStat("playerHealth", steamId, remaining);
        if (playerHealth == null) return;
        if (view != null)
            view.RPC(nameof(PlayerHealth.UpdateHealthRPC), RpcTarget.All,
                remaining, nextMaximum, false, false);
        else
            playerHealth.UpdateHealthRPC(remaining, nextMaximum, false, false);
    }
}
