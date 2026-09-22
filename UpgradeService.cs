using System;
using System.Collections.Generic;
using Photon.Pun;

namespace REPOJP.StageRoles;

internal static class UpgradeService
{
    internal static bool Ready => PunManager.instance != null && StatsManager.instance != null;

    internal static void SetLevels(
        string steamId,
        IReadOnlyList<UpgradeGrant> targets)
    {
        ApplyLevels(steamId, targets, preserveHigherLevels: false);
    }

    internal static void EnsureAtLeastLevels(
        string steamId,
        IReadOnlyList<UpgradeGrant> targets)
    {
        ApplyLevels(steamId, targets, preserveHigherLevels: true);
    }

    internal static bool AddLevels(
        string steamId,
        string commandName,
        int levels)
    {
        if (!Ready || string.IsNullOrEmpty(steamId) || levels == 0)
        {
            return false;
        }
        try
        {
            SendUpgradeDelta(steamId, commandName, levels);
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not add {levels} {commandName} level(s) to player " +
                $"{steamId}: {exception.Message}");
            return false;
        }
    }

    internal static bool AddLevelsHostAuthoritative(
        string steamId,
        string commandName,
        int levels)
    {
        if (!Ready || string.IsNullOrEmpty(steamId) || levels == 0 ||
            !SemiFunc.IsMasterClientOrSingleplayer())
        {
            return false;
        }
        try
        {
            ApplyUpgradeDeltaLocally(steamId, commandName, levels);
            if (SemiFunc.IsMultiplayer())
            {
                PhotonView? view = PunManager.instance.GetComponent<PhotonView>();
                if (view == null)
                {
                    throw new InvalidOperationException(
                        "PunManager has no PhotonView.");
                }
                view.RPC(
                    nameof(PunManager.TesterUpgradeCommandRPC),
                    RpcTarget.Others,
                    steamId,
                    commandName,
                    levels);
            }
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not add {levels} {commandName} level(s) to player " +
                $"{steamId}: {exception.Message}");
            return false;
        }
    }

    internal static bool TryGetLevels(
        string steamId,
        out Dictionary<string, int> levels)
    {
        levels = new Dictionary<string, int>(StringComparer.Ordinal);
        if (!Ready || string.IsNullOrEmpty(steamId))
        {
            return false;
        }
        try
        {
            levels = StatsManager.instance.FetchPlayerUpgrades(steamId);
            return true;
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogDebug(
                $"Could not read dynamic upgrade levels for player {steamId}: " +
                exception.Message);
            return false;
        }
    }

    private static void ApplyLevels(
        string steamId,
        IReadOnlyList<UpgradeGrant> targets,
        bool preserveHigherLevels)
    {
        if (!Ready || string.IsNullOrEmpty(steamId))
        {
            return;
        }

        Dictionary<string, int> currentUpgrades;
        try
        {
            currentUpgrades = StatsManager.instance.FetchPlayerUpgrades(steamId);
        }
        catch (Exception exception)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Could not read upgrade levels for player {steamId}: {exception.Message}");
            return;
        }

        UpgradeItemRetentionPatch.ApplyPendingThrow(steamId);

        foreach (UpgradeGrant target in targets)
        {
            int targetLevel = Math.Max(0, target.Level);
            int currentLevel = currentUpgrades.GetValueOrDefault(target.DictionaryName, 0);
            if (preserveHigherLevels && KingUpgradeAura.WithoutBonus(steamId, target.DictionaryName, currentLevel) >= targetLevel)
            {
                continue;
            }
            int delta = targetLevel - currentLevel;
            try
            {
                if (delta != 0)
                    SendUpgradeDelta(steamId, target.CommandName, delta);
                KingUpgradeAura.Forget(steamId, target.DictionaryName);
                if (target.CommandName == "Strength")
                    ReconcileGrabStrength(steamId, targetLevel);
            }
            catch (Exception exception)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Could not set {target.CommandName} to {targetLevel} for player " +
                    $"{steamId}: {exception.Message}");
            }
        }
    }

    private static void ReconcileGrabStrength(string steamId, int targetLevel)
    {
        if (!SemiFunc.IsMasterClientOrSingleplayer()) return;
        PlayerAvatar? player = SemiFunc.PlayerAvatarGetFromSteamID(steamId);
        PhysGrabber? grabber = player != null ? player.physGrabber : null;
        if (grabber == null || !TryGetLevels(steamId, out var levels)) return;
        int actualLevel = levels.GetValueOrDefault("playerUpgradeStrength", 0);
        if (actualLevel != targetLevel)
        {
            StageRolesPlugin.ModLogger.LogWarning(
                $"Strength reset was not applied: requested {targetLevel}, actual {actualLevel}.");
            return;
        }

        // Vanilla applies upgrades by adding 0.2 * delta to a separate cache.
        // A stale cache (including a duplicated LateStart contribution) survives
        // that subtraction even after the level is back at Base. Absolute role/
        // stage resets own this value, so reconcile it even for a zero delta.
        // Host physics reads these replicas for both host and unmodded guests.
        // Temporary overrideGrabStrength and per-object overrides stay intact.
        float expected = 1f + 0.2f * actualLevel;
        float previous = grabber.grabStrength;
        if (Math.Abs(previous - expected) <= 0.0001f) return;
        grabber.grabStrength = expected;
        StageRolesPlugin.ModLogger.LogDebug(
            $"Reconciled grab Strength cache at level {actualLevel}: {previous:F4} -> {expected:F4}.");
    }

    private static void SendUpgradeDelta(
        string steamId,
        string commandName,
        int delta)
    {
        if (SemiFunc.IsMultiplayer())
        {
            PhotonView? view = PunManager.instance.GetComponent<PhotonView>();
            if (view == null)
            {
                throw new InvalidOperationException("PunManager has no PhotonView.");
            }
            view.RPC(
                nameof(PunManager.TesterUpgradeCommandRPC),
                RpcTarget.All,
                steamId,
                commandName,
                delta);
            return;
        }

        PunManager.instance.TesterUpgradeCommandRPC(
            steamId,
            commandName,
            delta);
    }

    private static void ApplyUpgradeDeltaLocally(
        string steamId,
        string commandName,
        int delta)
    {
        switch (commandName)
        {
            case "CrouchRest":
                PunManager.instance.UpgradePlayerCrouchRest(steamId, delta);
                break;
            case "ExtraJump":
                PunManager.instance.UpgradePlayerExtraJump(steamId, delta);
                break;
            case "Health":
                PunManager.instance.UpgradePlayerHealth(steamId, delta);
                break;
            case "Launch":
                PunManager.instance.UpgradePlayerTumbleLaunch(steamId, delta);
                break;
            case "MapPlayerCount":
                PunManager.instance.UpgradeMapPlayerCount(steamId, delta);
                break;
            case "Range":
                PunManager.instance.UpgradePlayerGrabRange(steamId, delta);
                break;
            case "Speed":
                PunManager.instance.UpgradePlayerSprintSpeed(steamId, delta);
                break;
            case "Stamina":
                PunManager.instance.UpgradePlayerEnergy(steamId, delta);
                break;
            case "Strength":
                PunManager.instance.UpgradePlayerGrabStrength(steamId, delta);
                break;
            case "Throw":
                PunManager.instance.UpgradePlayerThrowStrength(steamId, delta);
                break;
            case "TumbleWings":
                PunManager.instance.UpgradePlayerTumbleWings(steamId, delta);
                break;
            case "TumbleClimb":
                PunManager.instance.UpgradePlayerTumbleClimb(steamId, delta);
                break;
            case "DeathHeadBattery":
                PunManager.instance.UpgradeDeathHeadBattery(steamId, delta);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(commandName),
                    commandName,
                    "Unsupported player upgrade.");
        }
    }
}
