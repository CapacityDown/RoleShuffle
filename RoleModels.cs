using System;
using System.Collections.Generic;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class RoleAssignment
{
    internal RoleAssignment(string steamId, PlayerAvatar player, StageRole role)
    {
        SteamId = steamId;
        Player = player;
        ActorNumber = PlayerIdentity.ActorNumber(player);
        AssignedRole = role;
        Role = role;
        PreviousPosition = player.transform.position;
        StinkerPreviousPosition = PreviousPosition;
        TunaPreviousPosition = PreviousPosition;
        StinkerTrailAnchor = player.transform.position;
        WasAlive = true;
    }

    internal string SteamId { get; }
    internal PlayerAvatar Player { get; set; }
    internal int ActorNumber { get; set; }
    internal AbilityExhaustionState ExhaustionNotifications { get; } = new();
    internal StageRole AssignedRole { get; set; }
    internal StageRole Role { get; set; }
    internal RoleOverhaulState Overhaul { get; } = new();
    internal Vector3 PreviousPosition { get; set; }
    internal Vector3 StinkerPreviousPosition { get; set; }
    internal Vector3 TunaPreviousPosition { get; set; }
    internal float StinkerTravelDistance { get; set; }
    internal Vector3 StinkerTrailAnchor { get; set; }
    internal float TravelDistance { get; set; }
    internal float JoblessDamageTimer { get; set; }
    internal float TunaStationaryTimer { get; set; }
    internal float TunaDamageTimer { get; set; }
    internal float MageNextCastAt { get; set; }
    // Stage-scoped: retained through revival, avatar replacement and role changes.
    internal int MageAutoRecoveryUsed { get; set; }
    internal bool WasAlive { get; set; }
    internal bool PhoenixUsed { get; set; }
    internal bool PhoenixRevivePending { get; set; }
    internal float PhoenixReadyAt { get; set; }
    internal int RescuerRevivesUsed { get; set; }
    internal int GamblerWagersUsed { get; set; }
    internal float MechanicRepairPercentUsed { get; set; }
    internal float ElectricianChargePercentUsed { get; set; }
    internal float InfluencerNextTtsAt { get; set; }
    internal float AvengerEmpoweredUntil { get; set; }
}

internal readonly struct UpgradeGrant
{
    internal UpgradeGrant(string commandName, string dictionaryName, int level)
    {
        CommandName = commandName;
        DictionaryName = dictionaryName;
        Level = level;
    }

    internal string CommandName { get; }
    internal string DictionaryName { get; }
    internal int Level { get; }
}

internal static class RoleCatalog
{
    private const int MaximumScalingRunLevel = 999999;
    private static readonly Dictionary<string, BaseScalingCache> BaseScalingCaches =
        new(StringComparer.Ordinal);

    internal static readonly IReadOnlyList<StageRole> AllRoles =
        (StageRole[])Enum.GetValues(typeof(StageRole));

    internal static readonly IReadOnlyList<UpgradeGrant> RammerLockedUpgrades =
        new[]
        {
            new UpgradeGrant("Launch", "playerUpgradeLaunch", 0),
            new UpgradeGrant("TumbleClimb", "playerUpgradeTumbleClimb", 0),
            new UpgradeGrant("TumbleWings", "playerUpgradeTumbleWings", 0)
        };

    internal static bool IsSecretRole(StageRole role) =>
        role is StageRole.Superbot or StageRole.Disaster;

    internal static string DisplayName(StageRole role) => role switch
    {
        StageRole.Superbot => "???1",
        StageRole.Disaster => "???2",
        _ => role.ToString()
    };

    internal static string AssignmentName(StageRole role) => role.ToString();

    internal static string AssignmentName(
        StageRole assignedRole,
        StageRole effectiveRole) =>
        assignedRole == StageRole.Imitator && effectiveRole != StageRole.Imitator
            ? $"Imitator ({AssignmentName(effectiveRole)})"
            : AssignmentName(assignedRole);

    internal static bool HasCapability(
        StageRole assignedRole,
        StageRole capability)
    {
        if (assignedRole == capability)
        {
            return true;
        }
        if (assignedRole == StageRole.Disaster)
        {
            return capability is StageRole.Bomber or StageRole.Stinker or StageRole.Tuna;
        }
        if (assignedRole != StageRole.Superbot)
        {
            return false;
        }
        return !IsSecretRole(capability) &&
               capability != StageRole.Bomber &&
               capability != StageRole.Stinker &&
               capability != StageRole.Werewolf &&
               capability != StageRole.Jobless &&
               capability != StageRole.Tuna &&
               capability != StageRole.King &&
               capability != StageRole.Diver &&
               capability != StageRole.Imitator &&
               capability != StageRole.Sniper &&
               capability != StageRole.Brawler;
    }

    internal static bool CanBeCopiedByImitator(StageRole role) =>
        role != StageRole.Imitator &&
        role != StageRole.King &&
        role != StageRole.Bomber &&
        role != StageRole.Stinker &&
        role != StageRole.Werewolf &&
        role != StageRole.Jobless &&
        role != StageRole.Tuna &&
        !IsSecretRole(role);

    internal static IReadOnlyList<UpgradeGrant> BaseUpgrades(
        StageRolesConfig config) =>
        BuildBaseUpgrades(config, includeTruckDrawBonus: true);

    internal static IReadOnlyList<UpgradeGrant> ConfiguredBaseUpgrades(
        StageRolesConfig config) =>
        BuildBaseUpgrades(config, includeTruckDrawBonus: false);

    private static IReadOnlyList<UpgradeGrant> BuildBaseUpgrades(
        StageRolesConfig config,
        bool includeTruckDrawBonus) =>
        new[]
        {
            BaseGrant(config, "Health", "playerUpgradeHealth", config.BaseHealthLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "Stamina", "playerUpgradeStamina", config.BaseStaminaLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "ExtraJump", "playerUpgradeExtraJump", config.BaseExtraJumpLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "Speed", "playerUpgradeSpeed", config.BaseSpeedLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "Strength", "playerUpgradeStrength", config.BaseStrengthLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "Range", "playerUpgradeRange", config.BaseRangeLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "Launch", "playerUpgradeLaunch", config.BaseLaunchLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "TumbleClimb", "playerUpgradeTumbleClimb", config.BaseTumbleClimbLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "TumbleWings", "playerUpgradeTumbleWings", config.BaseTumbleWingsLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "CrouchRest", "playerUpgradeCrouchRest", config.BaseCrouchRestLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus),
            BaseGrant(config, "MapPlayerCount", "playerUpgradeMapPlayerCount", config.BaseMapPlayerCountLevels.Value, 1, includeTruckDrawBonus),
            BaseGrant(config, "DeathHeadBattery", "playerUpgradeDeathHeadBattery", config.BaseDeathHeadBatteryLevels.Value, RoleUpgradeScaling.MaximumUpgradeLevel, includeTruckDrawBonus)
        };

    internal static bool BaseUpgradeMeetsOrExceedsRoleTarget(
        StageRole role,
        StageRolesConfig config)
    {
        if (role < StageRole.Tank || role > StageRole.Ghost)
        {
            return false;
        }

        IReadOnlyList<UpgradeGrant> baseUpgrades = BaseUpgrades(config);
        if (config.OverhaulEnabled.Value && RoleOverhaulRules.GrowsWithBase(role))
        {
            foreach (UpgradeGrant target in TargetUpgrades(role, config))
                foreach (UpgradeGrant baseline in baseUpgrades)
                    if (target.DictionaryName == baseline.DictionaryName && target.Level > baseline.Level)
                        return false;
            return true;
        }
        foreach (UpgradeGrant roleUpgrade in RoleUpgrades(role, config))
        {
            foreach (UpgradeGrant baseUpgrade in baseUpgrades)
            {
                if (string.Equals(
                        baseUpgrade.DictionaryName,
                        roleUpgrade.DictionaryName,
                        StringComparison.Ordinal) &&
                    baseUpgrade.Level >= roleUpgrade.Level)
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static bool InfluencerHasUpgradeBenefit(
        StageRolesConfig config,
        int playerCount)
    {
        int maximumNearbyPlayers = Math.Max(0, playerCount - 1);
        IReadOnlyList<UpgradeGrant> baseUpgrades = BaseUpgrades(config);
        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            if (!config.InfluencerUpgradeScaling.TryGetValue(
                    upgrade.Name,
                    out var entry))
            {
                continue;
            }

            RoleUpgradeScaling.TryParse(
                entry.Value,
                30,
                upgrade.MaximumLevel,
                out IReadOnlyList<UpgradeScalingRule> rules,
                out _);
            int baseLevel = 0;
            foreach (UpgradeGrant baseUpgrade in baseUpgrades)
            {
                if (string.Equals(
                        baseUpgrade.DictionaryName,
                        upgrade.DictionaryName,
                        StringComparison.Ordinal))
                {
                    baseLevel = baseUpgrade.Level;
                    break;
                }
            }

            foreach (UpgradeScalingRule rule in rules)
            {
                if (rule.Condition <= maximumNearbyPlayers &&
                    rule.Level > baseLevel)
                {
                    return true;
                }
            }
        }
        return false;
    }

    internal static IReadOnlyList<UpgradeGrant> TargetUpgrades(
        StageRole role,
        StageRolesConfig config)
    {
        List<UpgradeGrant> targets = new(BaseUpgrades(config));
        foreach (UpgradeGrant roleUpgrade in RoleUpgrades(role, config))
        {
            for (int index = 0; index < targets.Count; index++)
            {
                if (!string.Equals(
                        targets[index].DictionaryName,
                        roleUpgrade.DictionaryName,
                        StringComparison.Ordinal))
                {
                    continue;
                }
                int bonus = config.OverhaulEnabled.Value ? GrowthBonus(role, roleUpgrade.CommandName, config) : 0;
                targets[index] = config.OverhaulEnabled.Value &&
                    (RoleOverhaulRules.GrowsWithBase(role) || (role == StageRole.Superbot &&
                        roleUpgrade.CommandName is "Health" or "Speed" or "Stamina" or "Strength"))
                    ? new UpgradeGrant(roleUpgrade.CommandName, roleUpgrade.DictionaryName,
                        roleUpgrade.CommandName == "Strength"
                            ? RoleOverhaulRules.StrengthTarget(targets[index].Level, roleUpgrade.Level, bonus)
                            : RoleOverhaulRules.UpgradeTarget(targets[index].Level, roleUpgrade.Level, bonus))
                    : roleUpgrade;
                break;
            }
        }
        return targets;
    }

    private static int GrowthBonus(StageRole role, string command, StageRolesConfig config) => command switch
    {
        "Health" when role is StageRole.Tank or StageRole.Superbot => config.TankBaseBonus.Value,
        "Speed" when role is StageRole.Runner or StageRole.Superbot => config.RunnerSpeedBaseBonus.Value,
        "Stamina" when role is StageRole.Runner or StageRole.Superbot => config.RunnerStaminaBaseBonus.Value,
        "Strength" when role is StageRole.Lifter or StageRole.Superbot => config.LifterBaseBonus.Value,
        _ => 0
    };

    private static IReadOnlyList<UpgradeGrant> RoleUpgrades(
        StageRole role,
        StageRolesConfig config)
    {
        if (role == StageRole.Superbot)
        {
            return SuperbotUpgrades(config);
        }
        return role switch
        {
            StageRole.Tank => Grant("Health", "playerUpgradeHealth", config.TankHealthLevels.Value),
            StageRole.Runner => new[]
            {
                new UpgradeGrant("Speed", "playerUpgradeSpeed", config.RunnerSpeedLevels.Value),
                new UpgradeGrant("Stamina", "playerUpgradeStamina", config.RunnerStaminaLevels.Value)
            },
            StageRole.Jumper => Grant("ExtraJump", "playerUpgradeExtraJump", config.JumperExtraJumpLevels.Value),
            StageRole.Lifter => Grant("Strength", "playerUpgradeStrength", config.LifterStrengthLevels.Value),
            StageRole.Launcher => Grant("Launch", "playerUpgradeLaunch", config.LauncherLaunchLevels.Value),
            StageRole.Climber => new[]
            {
                new UpgradeGrant("TumbleClimb", "playerUpgradeTumbleClimb", config.ClimberClimbLevels.Value),
                new UpgradeGrant("Range", "playerUpgradeRange", config.ClimberRangeLevels.Value)
            },
            StageRole.Flyer => Grant("TumbleWings", "playerUpgradeTumbleWings", config.FlyerWingsLevels.Value),
            StageRole.Tracker => new[]
            {
                new UpgradeGrant("Health", "playerUpgradeHealth", config.TrackerHealthLevels.Value),
                new UpgradeGrant("MapPlayerCount", "playerUpgradeMapPlayerCount", 1)
            },
            StageRole.Ghost => Grant("DeathHeadBattery", "playerUpgradeDeathHeadBattery", config.GhostDeathHeadBatteryLevels.Value),
            StageRole.Bodyguard => Grant("Health", "playerUpgradeHealth", config.BodyguardHealthLevels.Value),
            StageRole.Rammer => RammerLockedUpgrades,
            _ => Array.Empty<UpgradeGrant>()
        };
    }

    private static IReadOnlyList<UpgradeGrant> SuperbotUpgrades(
        StageRolesConfig config)
    {
        Dictionary<string, UpgradeGrant> strongest = new(StringComparer.Ordinal);
        StageRole[] roles =
        {
            StageRole.Tank,
            StageRole.Runner,
            StageRole.Jumper,
            StageRole.Lifter,
            StageRole.Launcher,
            StageRole.Climber,
            StageRole.Flyer,
            StageRole.Tracker,
            StageRole.Ghost,
            StageRole.Bodyguard
        };
        foreach (StageRole role in roles)
        {
            foreach (UpgradeGrant grant in RoleUpgrades(role, config))
            {
                if (!strongest.TryGetValue(
                        grant.DictionaryName,
                        out UpgradeGrant current) ||
                    grant.Level > current.Level)
                {
                    strongest[grant.DictionaryName] = grant;
                }
            }
        }
        return new List<UpgradeGrant>(strongest.Values);
    }

    private static IReadOnlyList<UpgradeGrant> Grant(
        string commandName,
        string dictionaryName,
        int level) =>
        new[] { new UpgradeGrant(commandName, dictionaryName, level) };

    private static UpgradeGrant BaseGrant(
        StageRolesConfig config,
        string commandName,
        string dictionaryName,
        string source,
        int maximumLevel,
        bool includeTruckDrawBonus) =>
        new(
            commandName,
            dictionaryName,
            includeTruckDrawBonus
                ? BaseUpgradeManualStore.EffectiveLevel(dictionaryName,
                    ResolveBaseLevel(commandName, source, maximumLevel), maximumLevel,
                    config.BaseUpgradeManualAdjustmentEnabled.Value)
                : ResolveBaseLevel(commandName, source, maximumLevel));

    private static int ResolveBaseLevel(
        string upgradeName,
        string source,
        int maximumLevel)
    {
        if (!BaseScalingCaches.TryGetValue(
                upgradeName,
                out BaseScalingCache cache) ||
            !string.Equals(cache.Source, source, StringComparison.Ordinal))
        {
            cache = new BaseScalingCache { Source = source };
            RoleUpgradeScaling.TryParse(
                source,
                minimumCondition: 1,
                MaximumScalingRunLevel,
                maximumLevel,
                clampOutOfRange: true,
                out IReadOnlyList<UpgradeScalingRule> rules,
                out string error);
            cache.Rules = rules;
            BaseScalingCaches[upgradeName] = cache;
            if (!string.IsNullOrEmpty(error))
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"Ignored invalid Base Upgrades.{upgradeName}UpgradeLevels pair(s): {error}");
            }
        }

        int runLevel = (int)Math.Max(1L, Math.Min(MaximumScalingRunLevel,
            RunManager.instance != null ? (long)RunManager.instance.levelsCompleted + 1 : 1));
        int target = 0;
        foreach (UpgradeScalingRule rule in cache.Rules)
        {
            if (runLevel >= rule.Condition)
            {
                target = rule.Level;
            }
        }
        return target;
    }

    private sealed class BaseScalingCache
    {
        internal string Source { get; set; } = string.Empty;
        internal IReadOnlyList<UpgradeScalingRule> Rules { get; set; } =
            Array.Empty<UpgradeScalingRule>();
    }
}
