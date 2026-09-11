using System;

namespace REPOJP.StageRoles;

internal static class PlayerIdentity
{
    internal static string SteamId(PlayerAvatar? player)
    {
        if (player == null)
        {
            return string.Empty;
        }

        // Direct access to PlayerAvatar.steamID is rejected by the unmodified
        // runtime assembly, even though the publicized reference permits it.
        try
        {
            string steamId = SemiFunc.PlayerGetSteamID(player);
            if (!string.IsNullOrEmpty(steamId))
            {
                return steamId;
            }
        }
        catch
        {
            // Photon supplies the same account identifier in multiplayer.
        }

        try
        {
            return player.photonView?.Owner?.UserId ?? string.Empty;
        }
        catch
        {
            return string.Empty;
        }
    }

    internal static string Name(PlayerAvatar? player)
    {
        if (player == null)
        {
            return string.Empty;
        }

        try
        {
            return SemiFunc.PlayerGetName(player) ?? string.Empty;
        }
        catch
        {
            try
            {
                return player.photonView?.Owner?.NickName ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }

    internal static int ActorNumber(PlayerAvatar? player)
    {
        try
        {
            return player?.photonView?.Owner?.ActorNumber ?? 0;
        }
        catch
        {
            return 0;
        }
    }
}
