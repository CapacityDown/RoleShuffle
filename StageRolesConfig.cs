using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;

namespace REPOJP.StageRoles;

internal sealed class StageRolesConfig
{
    internal const int MaximumSupportedPlayers = 30;
    private readonly Dictionary<string, ConfigEntry<int>>
        _truckUpgradeDrawWeights = new(StringComparer.Ordinal);
    private readonly Dictionary<string, ConfigEntry<bool>>
        _baseUpgradeDrawEnabled = new(StringComparer.Ordinal);

    internal ConfigEntry<bool>? BaseUpgradeDrawEnabledEntry(string name) =>
        _baseUpgradeDrawEnabled.TryGetValue(name, out var entry) ? entry : null;
    internal bool BaseUpgradeDrawIsEnabled(string name) => BaseUpgradeDrawEnabledEntry(name)?.Value ?? false;

    internal StageRolesConfig(ConfigFile config)
    {
        RoleConfigMigration.Apply(config);

        Enabled = BindBool(config, "General", "Enabled", true, "Enables stage role assignment.");
        UniqueRoles = BindBool(config, "General", "UniqueRoles", true, "Avoids duplicate roles until every enabled role has been assigned once.");
        ShopUpgradeItemCount = BindInt(config, "General", "ShopUpgradeItemCount", 0, 0, 30, "Number of upgrade items requested for each shop.");

        // REPOConfig displays sections in registration order.
        GuideLanguage = config.Bind("UI", "GuideLanguage", "English",
            new ConfigDescription("Local language for the role guide and utility pages. Default: English. Language names are shown in their native spelling. Also changes the in-menu language toggle; both controls save the same setting.",
                new RoleLanguageValues()));

        EnhancedEnemyRewardsEnabled = BindBool(
            config,
            "Compatibility",
            "EnhancedEnemyRewardsEnabled",
            true,
            "Treats Elite Enemy Variants Enhanced enemies as one tier higher for Vampire healing and Hunter bonus-orb quality.");


        ShowcaseGuaranteesEnabled = BindBool(config, "Role Balance - Guarantees", "ShowcaseEnabled", true, "Guarantees event-focused roles according to the configured party-size tiers.");
        ShowcaseMinimumsByPlayerCount = PartySizeScaling(
            config,
            "Role Balance - Guarantees",
            "ShowcaseMinimums",
            "3:1,7:2,14:3,24:4",
            "Minimum Showcase roles by party size.");
        SupportGuaranteesEnabled = BindBool(config, "Role Balance - Guarantees", "SupportEnabled", true, "Guarantees teamwork-focused roles according to the configured party-size tiers.");
        SupportMinimumsByPlayerCount = PartySizeScaling(
            config,
            "Role Balance - Guarantees",
            "SupportMinimums",
            "4:1,8:2,14:3,24:4",
            "Minimum Support roles by party size.");

        RiskCombinationLimitsEnabled = BindBool(config, "Role Balance - Limits", "Enabled", true, "Limits simultaneous Danger and Hardship roles during normal assignment.");
        DangerRoleLimit = PartySizeScaling(config, "Role Balance - Limits", "DangerMaximums", "1:1,8:2,16:3", "Maximum simultaneous Bomber, Stinker, and Werewolf roles by party size.");
        HardshipRoleLimit = PartySizeScaling(config, "Role Balance - Limits", "HardshipMaximums", "1:1,12:2", "Maximum simultaneous Jobless and Tuna roles by party size.");
        SmallPartyCombinedRiskPlayerCount = BindInt(config, "Role Balance - Limits", "SmallPartyMaximumPlayers", 4, 1, MaximumSupportedPlayers, "Largest party size that uses the combined Danger and Hardship limit.");
        SmallPartyCombinedRiskRoleLimit = BindInt(config, "Role Balance - Limits", "SmallPartyCombinedMaximum", 1, 1, MaximumSupportedPlayers, "Maximum combined Danger and Hardship roles in a small party.");

        PreventConsecutiveSameRole = BindBool(config, "Role Balance - Variety", "PreventSameRole", true, "Prevents a player from receiving the same role in consecutive stages when another role is available.");
        HardshipPersonalCooldownStages = BindInt(config, "Role Balance - Variety", "HardshipCooldownStages", 2, 0, 10, "Stages after Jobless or Tuna during which that player cannot normally receive either Hardship role.");
        ExcludeUnavailableContextRoles = BindBool(config, "Role Balance - Variety", "ExcludeUnavailableRoles", true, "Excludes context-dependent roles when their required player, enemy, weapon, or vanilla object is not available.");

        AnnouncementsEnabled = BindBool(config, "Notifications", "Enabled", true, "Enables role-name and role-effect chat and TTS notifications.");
        AnnouncementDelaySeconds = BindFloat(config, "Notifications", "DelaySeconds", 2.5f, 0f, 30f, "Delay before role announcements.");
        StageFluxAnnouncementDelaySeconds = BindFloat(config, "Notifications", "StageFluxDelaySeconds", 5f, 0f, 30f, "Delay used when Stage Flux is installed.");

        HudFontSize = BindInt(config, "HUD", "FontSize", 28, 16, 48, "HUD text size before overall scaling.");
        HudEnabled = BindBool(config, "HUD", "Enabled", true, "Shows every player's assigned role during a stage.");
        HudRoleDisplay = config.Bind("HUD", "RoleDisplay", "NameOnly",
            new ConfigDescription("Role display style. Player names remain visible in every mode.",
                new AcceptableValueList<string>("IconAndName", "NameOnly", "IconOnly")));
        HudIconSize = BindInt(config, "HUD", "IconSize", 64, 32, 128,
            "Role icon size before HUD scaling. Rows and page capacity adjust to keep icons from overlapping.");
        HudAnchor = config.Bind(
            "HUD",
            "Anchor",
            "BottomLeft",
            new ConfigDescription(
                "Anchor point used to position the role HUD.",
                new AcceptableValueList<string>(
                    "TopLeft", "TopCenter", "TopRight",
                    "MiddleLeft", "MiddleCenter", "MiddleRight",
                    "BottomLeft", "BottomCenter", "BottomRight")));
        HudAlignment = config.Bind(
            "HUD",
            "Alignment",
            "Left",
            new ConfigDescription(
                "Horizontal alignment of the role HUD text.",
                new AcceptableValueList<string>("Left", "Center", "Right")));
        HudOffsetX = BindInt(config, "HUD", "OffsetX", 0, -3840, 3840, "Horizontal offset from the anchor in pixels.");
        HudOffsetY = BindInt(config, "HUD", "OffsetY", 80, -2160, 2160, "Vertical offset from the anchor in pixels.");
        HudScalePercent = BindInt(config, "HUD", "ScalePercent", 70, 50, 200, "Role HUD scale percentage.");
        HudPlayersPerPage = BindInt(config, "HUD", "PlayersPerPage", 8, 2, 20, "Maximum players shown at once, including the pinned local player. Fewer players are shown when needed to fit the icons and font height.");
        HudPageIntervalSeconds = BindFloat(config, "HUD", "PageIntervalSeconds", 5f, 1f, 30f, "Seconds each role page remains visible.");
        HudTransitionDurationSeconds = BindFloat(config, "HUD", "TransitionDurationSeconds", 0.2f, 0f, 1f, "Duration of the fade between role pages.");
        HudPinLocalPlayer = BindBool(config, "HUD", "PinLocalPlayer", true, "Keeps the local player's role visible on every page.");

        SetRoleCommandEnabled = BindBool(config, "Testing", "Enabled", false, "Enables the testing commands.");

        BaseHealthLevels = BaseUpgradeScaling(config, "HealthUpgradeLevels", "1:1", RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseStaminaLevels = BaseUpgradeScaling(config, "StaminaUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseExtraJumpLevels = BaseUpgradeScaling(config, "ExtraJumpUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseSpeedLevels = BaseUpgradeScaling(config, "SpeedUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseStrengthLevels = BaseUpgradeScaling(config, "StrengthUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseRangeLevels = BaseUpgradeScaling(config, "RangeUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseLaunchLevels = BaseUpgradeScaling(config, "LaunchUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseTumbleClimbLevels = BaseUpgradeScaling(config, "TumbleClimbUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseTumbleWingsLevels = BaseUpgradeScaling(config, "TumbleWingsUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseCrouchRestLevels = BaseUpgradeScaling(config, "CrouchRestUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        BaseMapPlayerCountLevels = BaseUpgradeScaling(config, "MapPlayerCountUpgradeLevels", string.Empty, 1);
        BaseDeathHeadBatteryLevels = BaseUpgradeScaling(config, "DeathHeadBatteryUpgradeLevels", string.Empty, RoleUpgradeScaling.MaximumUpgradeLevel);
        TruckUpgradeDrawEnabled = BindBool(
            config,
            "Base Upgrade Draw",
            "Enabled",
            true,
            "After leaving the shop, runs a weighted shared Base Upgrade draw during the truck preparation phase.");
        TruckUpgradeDrawMaximumLevel = BindInt(
            config,
            "Base Upgrade Draw",
            "MaximumLevel",
            RoleUpgradeScaling.MaximumUpgradeLevel,
            0,
            RoleUpgradeScaling.MaximumUpgradeLevel,
            "Maximum Base Upgrade target reachable through positive truck draws. Map Player Count remains limited to 1.");
        TruckUpgradeDrawCappedWeightMultiplier = BindFloat(
            config,
            "Base Upgrade Draw",
            "CappedUpgradeWeightMultiplier",
            0.25f,
            0f,
            1f,
            "Minimum selection weight multiplier reached at the truck draw maximum, including configured levels and truck bonuses. Weight decreases gradually toward this value. 0 excludes capped upgrades; 1 disables the decrease. Does not allow increases beyond the maximum or change All Upgrades weight.");
        TruckUpgradeDrawWeightFalloffExponent = BindFloat(
            config,
            "Base Upgrade Draw",
            "WeightFalloffExponent",
            2f,
            0.1f,
            10f,
            "Controls how upgrade selection weight decreases toward the draw maximum. 1 decreases evenly; larger values preserve more weight until near the maximum. All Upgrades weight is unchanged.");
        TruckUpgradeDrawDeltaWeights = config.Bind(
            "Base Upgrade Draw",
            "ChangeAmountWeights",
            "-1:10,0:15,1:60,2:15",
            "Comma-separated delta:weight pairs used by the truck Base Upgrade draw. Delta range: -200 to 200. Weight range: 0 to 1000. Out-of-range numeric values are clamped, malformed pairs are ignored, and duplicate deltas use the last value.");
        BindTruckUpgradeDrawWeight(config, "Health");
        BindTruckUpgradeDrawWeight(config, "Stamina");
        BindTruckUpgradeDrawWeight(config, "ExtraJump");
        BindTruckUpgradeDrawWeight(config, "Speed");
        BindTruckUpgradeDrawWeight(config, "Strength");
        BindTruckUpgradeDrawWeight(config, "Range");
        BindTruckUpgradeDrawWeight(config, "Launch");
        BindTruckUpgradeDrawWeight(config, "TumbleClimb");
        BindTruckUpgradeDrawWeight(config, "TumbleWings");
        BindTruckUpgradeDrawWeight(config, "CrouchRest");
        BindTruckUpgradeDrawWeight(config, "MapPlayerCount");
        BindTruckUpgradeDrawWeight(config, "DeathHeadBattery");
        BindTruckUpgradeDrawWeight(config, "AllUpgrades");

        TankEnabled = RoleEnabled(config, "Tank");
        TankWeight = RoleWeight(config, "Tank");
        TankHealthLevels = UpgradeLevel(config, "Tank", "HealthUpgradeLevels", 21);

        RunnerEnabled = RoleEnabled(config, "Runner");
        RunnerWeight = RoleWeight(config, "Runner");
        RunnerSpeedLevels = UpgradeLevel(config, "Runner", "SpeedUpgradeLevels", 6);
        RunnerStaminaLevels = UpgradeLevel(config, "Runner", "StaminaUpgradeLevels", 46);

        JumperEnabled = RoleEnabled(config, "Jumper");
        JumperWeight = RoleWeight(config, "Jumper");
        JumperExtraJumpLevels = UpgradeLevel(config, "Jumper", "ExtraJumpUpgradeLevels", 10);

        LifterEnabled = RoleEnabled(config, "Lifter");
        LifterWeight = RoleWeight(config, "Lifter");
        LifterStrengthLevels = UpgradeLevel(config, "Lifter", "StrengthUpgradeLevels", 25);

        LauncherEnabled = RoleEnabled(config, "Launcher");
        LauncherWeight = RoleWeight(config, "Launcher");
        LauncherLaunchLevels = UpgradeLevel(config, "Launcher", "LaunchUpgradeLevels", 10);

        ClimberEnabled = RoleEnabled(config, "Climber");
        ClimberWeight = RoleWeight(config, "Climber");
        ClimberClimbLevels = UpgradeLevel(config, "Climber", "ClimbUpgradeLevels", 50);
        ClimberRangeLevels = UpgradeLevel(config, "Climber", "RangeUpgradeLevels", 20);

        FlyerEnabled = RoleEnabled(config, "Flyer");
        FlyerWeight = RoleWeight(config, "Flyer");
        FlyerWingsLevels = UpgradeLevel(config, "Flyer", "WingsUpgradeLevels", 10);

        TrackerEnabled = RoleEnabled(config, "Tracker");
        TrackerWeight = RoleWeight(config, "Tracker");
        TrackerHealthLevels = UpgradeLevel(config, "Tracker", "HealthUpgradeLevels", 3);

        GhostEnabled = RoleEnabled(config, "Ghost");
        GhostWeight = RoleWeight(config, "Ghost", 80);
        GhostDeathHeadBatteryLevels = UpgradeLevel(config, "Ghost", "DeathHeadBatteryUpgradeLevels", 50);

        BomberEnabled = RoleEnabled(config, "Bomber");
        BomberWeight = RoleWeight(config, "Bomber", 80);
        BomberDistance = BindFloat(config, "Bomber", "DistancePerGrenade", 8f, 1f, 100f, "Travel distance required per grenade.");
        BomberMaximumActiveGrenades = BindInt(config, "Bomber", "MaximumActiveGrenades", 30, 1, 30, "Maximum retained generated grenades per Bomber. Placing another removes the oldest grenade.");
        BomberAllowTruckSpawns = BindBool(config, "Bomber", "AllowTruckSpawns", false, "Allows grenade placement inside the truck.");
        BomberExplosiveEnabled = BindBool(config, "Bomber", "ExplosiveGrenadesEnabled", true, "Includes the vanilla explosive grenade.");
        BomberStunEnabled = BindBool(config, "Bomber", "StunGrenadesEnabled", true, "Includes the vanilla stun grenade.");
        BomberShockwaveEnabled = BindBool(config, "Bomber", "ShockwaveGrenadesEnabled", true, "Includes the vanilla shockwave grenade.");
        BomberDuctTapedEnabled = BindBool(config, "Bomber", "DuctTapedGrenadesEnabled", true, "Includes the vanilla duct-taped grenade.");

        MedicEnabled = RoleEnabled(config, "Medic");
        MedicWeight = RoleWeight(config, "Medic");
        MedicHealAmount = BindInt(config, "Medic", "HealAmount", 5, 1, 100, "Health restored to each nearby player per tick.");
        MedicHealIntervalSeconds = BindFloat(config, "Medic", "HealIntervalSeconds", 2f, 0.1f, 30f, "Seconds between Medic healing ticks.");
        MedicHealRadius = BindFloat(config, "Medic", "HealRadius", 5f, 1f, 30f, "Maximum distance from the Medic for healing.");
        MedicTotalHealingLimit = BindInt(config, "Medic", "TotalHealingLimit", 150, 1, 10000, "Maximum total health restored by each Medic per stage.");

        PhoenixEnabled = RoleEnabled(config, "Phoenix");
        PhoenixWeight = RoleWeight(config, "Phoenix");
        PhoenixRevivalHealth = BindInt(config, "Phoenix", "RevivalHealth", 25, 1, 1000, "Health restored after Phoenix revival, capped at the player's maximum health.");
        PhoenixReviveDelaySeconds = BindFloat(config, "Phoenix", "ReviveDelaySeconds", 2f, 2f, 10f, "Delay before revival. Phoenix always waits at least two seconds.");
        PhoenixFailureGraceSeconds = BindFloat(config, "Phoenix", "FailureGraceSeconds", 5f, 1f, 15f, "Maximum time to hold a failed-stage transition while revival initializes.");

        JoblessEnabled = RoleEnabled(config, "Jobless");
        JoblessWeight = RoleWeight(config, "Jobless", 20);
        JoblessDamage = BindInt(config, "Jobless", "Damage", 1, 1, 100, "Damage applied per tick outside the truck.");
        JoblessDamageIntervalSeconds = BindFloat(config, "Jobless", "DamageIntervalSeconds", 0.1f, 0.05f, 10f, "Seconds between damage ticks outside the truck.");

        RescuerEnabled = RoleEnabled(config, "Rescuer");
        RescuerWeight = RoleWeight(config, "Rescuer", 80);
        RescuerRevivalHealth = BindInt(config, "Rescuer", "RevivalHealth", 25, 1, 1000, "Health restored to a player revived by Rescuer, capped at the player's maximum health.");
        RescuerReviveDelaySeconds = BindFloat(config, "Rescuer", "ReviveDelaySeconds", 2f, 0f, 10f, "Delay before reviving another player.");
        RescuerRadius = BindFloat(config, "Rescuer", "Radius", 3f, 1f, 50f, "Maximum distance to a dead player.");
        RescuerMaximumRevives = BindInt(config, "Rescuer", "MaximumRevives", 2, 1, 10, "Maximum revivals per stage.");

        VampireEnabled = RoleEnabled(config, "Vampire");
        VampireWeight = RoleWeight(config, "Vampire");
        VampireTier1HealAmount = BindInt(config, "Vampire", "Tier1HealAmount", 5, 1, 100, "Health restored by a nearby Tier 1 enemy death.");
        VampireTier2HealAmount = BindInt(config, "Vampire", "Tier2HealAmount", 10, 1, 100, "Health restored by a nearby Tier 2 enemy death.");
        VampireTier3HealAmount = BindInt(config, "Vampire", "Tier3HealAmount", 50, 1, 100, "Health restored by a nearby Tier 3 enemy death.");
        VampireRadius = BindFloat(config, "Vampire", "Radius", 10f, 1f, 50f, "Maximum distance from a dying enemy.");

        KingEnabled = RoleEnabled(config, "King");
        KingWeight = RoleWeight(config, "King");

        TunaEnabled = RoleEnabled(config, "Tuna");
        TunaWeight = RoleWeight(config, "Tuna", 50);
        TunaStationaryDelaySeconds = BindFloat(config, "Tuna", "StationaryDelaySeconds", 3f, 0.1f, 30f, "Seconds without movement before damage starts.");
        TunaDamage = BindInt(config, "Tuna", "Damage", 1, 1, 100, "Damage applied per stationary tick.");
        TunaDamageIntervalSeconds = BindFloat(config, "Tuna", "DamageIntervalSeconds", 0.1f, 0.05f, 10f, "Seconds between stationary damage ticks.");

        MusicianEnabled = RoleEnabled(config, "Musician");
        MusicianWeight = RoleWeight(config, "Musician", 60);
        MusicianHealAmount = BindInt(config, "Musician", "HealAmount", 5, 1, 100, "Health restored to the Musician and each nearby player when an instrument note is played.");
        MusicianHealRadius = BindFloat(config, "Musician", "HealRadius", 10f, 1f, 50f, "Maximum healing distance from the Musician when an instrument note is played.");

        MageEnabled = RoleEnabled(config, "Mage");
        MageWeight = RoleWeight(config, "Mage");
        MageCastIntervalSeconds = BindFloat(config, "Mage", "CastIntervalSeconds", 3f, 0.1f, 30f, "Minimum seconds between spell activations.");
        MageStarExpression = ExpressionName(config, "Mage", "StarExpression", "Angry", "Star Wand");
        MageRollExpression = ExpressionName(config, "Mage", "RollExpression", "Sad", "Roll Staff");
        MageGravityExpression = ExpressionName(config, "Mage", "GravityExpression", "Suspicious", "Zero Gravity Staff");
        MageVoidExpression = ExpressionName(config, "Mage", "VoidExpression", "EyesClosed", "Void Staff");
        MageLaserExpression = ExpressionName(config, "Mage", "LaserExpression", "Scared", "Wizard Staff beam");
        MageAutoRecoveryEnabled = BindBool(config, "Mage", "AutoRecoveryEnabled", true, "Enables Mage health recovery after avoiding damage.");
        MageAutoRecoveryDelaySeconds = BindFloat(config, "Mage", "AutoRecoveryDelaySeconds", 10f, 0f, 300f, "Seconds without taking damage before automatic health recovery starts.");
        MageAutoRecoveryIntervalSeconds = BindFloat(config, "Mage", "AutoRecoveryIntervalSeconds", 2f, 0.1f, 60f, "Seconds between automatic Mage health recovery ticks.");
        MageAutoRecoveryAmount = BindInt(config, "Mage", "AutoRecoveryAmount", 1, 0, 100, "Health restored by each automatic Mage recovery tick.");
        MageAutoRecoveryTotalHealingLimit = BindInt(config, "Mage", "AutoRecoveryTotalHealingLimit", 120, 1, 10000, "Maximum total health restored by each player's Mage automatic recovery per stage. Other healing is not counted.");
        MageStarHealthCost = BindInt(config, "Mage", "StarHealthCost", 10, 0, 100, "Health consumed after a successful Star Wand cast.");
        MageGravityHealthCost = BindInt(config, "Mage", "GravityHealthCost", 10, 0, 100, "Health consumed after a successful Zero Gravity Staff cast.");
        MageRollHealthCost = BindInt(config, "Mage", "RollHealthCost", 15, 0, 100, "Health consumed after a successful Roll Staff cast.");
        MageVoidHealthCost = BindInt(config, "Mage", "VoidHealthCost", 30, 0, 100, "Health consumed after a successful Void Staff cast.");
        MageLaserHealthCost = BindInt(config, "Mage", "LaserHealthCost", 50, 0, 100, "Health consumed after a successful Wizard Staff beam cast.");

        GamblerEnabled = RoleEnabled(config, "Gambler");
        GamblerWeight = RoleWeight(config, "Gambler");
        GamblerWinChancePercent = BindInt(config, "Gambler", "WinChancePercent", 50, 0, 100, "Chance for a wagered valuable to use the win multiplier.");
        GamblerWinValueMultiplier = BindFloat(config, "Gambler", "WinValueMultiplier", 2f, 0f, 10f, "Value multiplier applied when a wager wins.");
        GamblerGambitGreenHealAmount = BindInt(config, "Gambler", "GambitGreenHealAmount", 50, 25, 1000, "Total health restored when Gambit lands on green for a Gambler.");
        GamblerGambitRedDamage = BindInt(config, "Gambler", "GambitRedDamage", 100, 50, 1000, "Total damage dealt when Gambit lands on red for a Gambler.");
        GamblerGambitWhiteHealthUpgradeLevels = BindInt(config, "Gambler", "GambitWhiteHealthUpgradeLevels", 5, 0, RoleUpgradeScaling.MaximumUpgradeLevel, "Temporary Health upgrade levels added when Gambit lands on white for a Gambler.");

        HunterEnabled = RoleEnabled(config, "Hunter");
        HunterWeight = RoleWeight(config, "Hunter", 80);
        HunterBatteryConsumptionPercent = BindInt(config, "Hunter", "WeaponBatteryConsumptionPercent", 75, 0, 100, "Percentage of normal weapon battery consumption used by Hunter.");
        HunterDoubleOrbChancePercent = BindFloat(config, "Hunter", "DoubleOrbChancePercent", 10f, 0f, 100f, "Chance for an enemy defeated by Hunter to drop twice its normal orb count.");
        HunterJackpotOrbChancePercent = BindFloat(config, "Hunter", "JackpotOrbChancePercent", 0.5f, 0f, 100f, "Chance for an enemy defeated by Hunter to drop the configured jackpot orb count.");
        HunterJackpotOrbCount = BindInt(config, "Hunter", "JackpotOrbCount", 10, 1, 30, "Orb count used when Hunter's jackpot roll succeeds.");

        StinkerEnabled = RoleEnabled(config, "Stinker");
        StinkerWeight = RoleWeight(config, "Stinker", 80);
        StinkerDistance = BindFloat(config, "Stinker", "DistancePerCloud", 2f, 1f, 100f, "Travel distance required per uranium cloud.");
        StinkerSafetyDistance = BindFloat(config, "Stinker", "MinimumSafetyDistance", 2f, 1f, 30f, "Minimum distance from the newest pending cloud when it is created.");
        StinkerAllowTruckSpawns = BindBool(config, "Stinker", "AllowTruckSpawns", false, "Allows uranium cloud creation inside the truck.");

        EngineerEnabled = RoleEnabled(config, "Engineer");
        EngineerWeight = RoleWeight(config, "Engineer", 70);

        TricksterEnabled = RoleEnabled(config, "Trickster");
        TricksterWeight = RoleWeight(config, "Trickster");
        TricksterExpression = ExpressionName(config, "Trickster", "DecoyExpression", "Happy", "decoy placement");
        TricksterActiveSeconds = BindFloat(config, "Trickster", "ActiveSeconds", 25f, 1f, 120f, "Seconds before the active decoy is removed.");
        TricksterCooldownSeconds = BindFloat(config, "Trickster", "CooldownSeconds", 45f, 1f, 300f, "Seconds after an effective decoy ends before another can be placed.");
        TricksterNoTargetCooldownSeconds = BindFloat(config, "Trickster", "NoTargetCooldownSeconds", 10f, 0f, 300f, "Seconds after a decoy that attracted no enemies ends before another can be placed.");
        TricksterInvestigateRadius = BindFloat(config, "Trickster", "InvestigateRadius", 40f, 1f, 100f, "Enemy investigation radius around the decoy.");
        TricksterPulseIntervalSeconds = BindFloat(config, "Trickster", "PulseIntervalSeconds", 1f, 0.25f, 10f, "Seconds between enemy investigation pulses.");
        TricksterPlacementDistance = BindFloat(config, "Trickster", "PlacementDistance", 2f, 0.5f, 10f, "Distance in front of Trickster used for decoy placement.");

        MechanicEnabled = RoleEnabled(config, "Mechanic");
        MechanicWeight = RoleWeight(config, "Mechanic");
        MechanicRepairPercentPerSecond = BindFloat(config, "Mechanic", "RepairPercentPerSecond", 2f, 0f, 100f, "Percentage of a held valuable's original value repaired per second.");
        MechanicMaximumRepairPercentPerStage = BindFloat(config, "Mechanic", "MaximumRepairPercentPerStage", 50f, 0f, 100f, "Maximum cumulative original-value percentage repaired by each Mechanic per stage.");

        ElectricianEnabled = RoleEnabled(config, "Electrician");
        ElectricianWeight = RoleWeight(config, "Electrician", 80);
        ElectricianChargePercentPerSecond = BindFloat(config, "Electrician", "ChargePercentPerSecond", 10f, 0f, 100f, "Battery percentage restored per second to held inactive rechargeable items.");
        ElectricianMaximumChargePercentPerStage = BindFloat(config, "Electrician", "MaximumChargePercentPerStage", 100f, 0f, 1000f, "Maximum cumulative battery percentage restored by each Electrician per stage.");

        WardenEnabled = RoleEnabled(config, "Warden");
        WardenWeight = RoleWeight(config, "Warden");
        WardenAdditionalStunSeconds = BindFloat(config, "Warden", "AdditionalStunSeconds", 3f, 0f, 30f, "Seconds added to enemy stuns caused by Warden.");

        NinjaEnabled = RoleEnabled(config, "Ninja");
        NinjaWeight = RoleWeight(config, "Ninja");
        NinjaVisionRecognitionMultiplier = BindFloat(
            config,
            "Ninja",
            "VisionRecognitionMultiplier",
            2f,
            1f,
            10f,
            "Multiplier applied to the time enemies need to visually recognize Ninja. Player-generated investigation sounds remain suppressed.");

        ExecutionerEnabled = RoleEnabled(config, "Executioner");
        ExecutionerWeight = RoleWeight(config, "Executioner");
        ExecutionerStunnedDamageMultiplier = BindFloat(config, "Executioner", "StunnedDamageMultiplier", 2f, 1f, 10f, "Damage multiplier for direct attacks by Executioner against enemies that were already stunned.");

        RiderEnabled = RoleEnabled(config, "Rider");
        RiderWeight = RoleWeight(config, "Rider", 60);
        RiderEnemyDamageMultiplier = BindFloat(config, "Rider", "EnemyDamageMultiplier", 3f, 1f, 10f, "Multiplier for vanilla vehicle impact damage against enemies while Rider is driving.");
        RiderPlayerKnockbackMultiplier = BindFloat(config, "Rider", "PlayerKnockbackMultiplier", 2f, 1f, 10f, "Multiplier for existing vanilla vehicle Tumble knockback against players while Rider is driving.");

        InfluencerEnabled = RoleEnabled(config, "Influencer");
        InfluencerWeight = RoleWeight(config, "Influencer");
        InfluencerRadius = BindFloat(config, "Influencer", "PlayerRadius", 20f, 1f, 100f, "Radius used to count nearby living players.");
        InfluencerPlayerCheckIntervalSeconds = BindFloat(config, "Influencer", "PlayerCheckIntervalSeconds", 0.5f, 0.1f, 10f, "Seconds between nearby-player and upgrade checks.");
        InfluencerNoiseRadiusMultiplier = BindFloat(config, "Influencer", "NoiseRadiusMultiplier", 2f, 1f, 10f, "Multiplier applied to vanilla footstep, landing, voice, and chat TTS investigation radii.");
        InfluencerTtsInvestigateRadius = BindFloat(config, "Influencer", "TTSInvestigateRadius", 20f, 1f, 100f, "Enemy investigation radius produced by periodic Influencer TTS.");
        InfluencerMinimumTtsIntervalSeconds = BindFloat(config, "Influencer", "MinimumTTSIntervalSeconds", 20f, 1f, 300f, "Minimum randomly selected interval between periodic TTS messages.");
        InfluencerMaximumTtsIntervalSeconds = BindFloat(config, "Influencer", "MaximumTTSIntervalSeconds", 40f, 1f, 300f, "Maximum randomly selected interval between periodic TTS messages.");
        InfluencerTtsQuietPeriodSeconds = BindFloat(config, "Influencer", "TTSQuietPeriodSeconds", 1.5f, 0f, 10f, "Required quiet period after any TTS before Influencer may speak.");
        InfluencerUpgradeScaling = UpgradeScalingEntries(
            config,
            "Influencer",
            useHealthPercent: false,
            includeHealth: true);

        WerewolfEnabled = RoleEnabled(config, "Werewolf");
        WerewolfWeight = RoleWeight(config, "Werewolf", 40);
        WerewolfPlayerDamageMultiplier = BindFloat(config, "Werewolf", "PlayerDamageMultiplier", 2f, 1f, 10f, "Multiplier for identifiable player-origin damage dealt by Werewolf to another player.");

        BerserkerEnabled = RoleEnabled(config, "Berserker");
        BerserkerWeight = RoleWeight(config, "Berserker");
        BerserkerUpgradeScaling = UpgradeScalingEntries(
            config,
            "Berserker",
            useHealthPercent: true,
            includeHealth: false);

        BodyguardEnabled = RoleEnabled(config, "Bodyguard");
        BodyguardWeight = RoleWeight(config, "Bodyguard", 80);
        BodyguardHealthLevels = UpgradeLevel(config, "Bodyguard", "HealthUpgradeLevels", 11);
        BodyguardRadius = BindFloat(config, "Bodyguard", "ProtectionRadius", 15f, 1f, 100f, "Maximum distance from a player for Bodyguard to absorb enemy damage.");
        BodyguardDamageSharePercent = BindFloat(config, "Bodyguard", "DamageSharePercent", 50f, 0f, 100f, "Percentage of enemy damage transferred to Bodyguard, without reducing Bodyguard below 1 HP.");

        RammerEnabled = RoleEnabled(config, "Rammer");
        RammerWeight = RoleWeight(config, "Rammer");
        RammerTumbleDamage = BindInt(config, "Rammer", "TumbleAttackDamage", 100, 0, 100000, "Enemy damage dealt by Rammer's Tumble Attack.");
        RammerSelfDamage = BindInt(config, "Rammer", "SelfDamage", 15, 0, 100000, "Damage Rammer takes after its Tumble Attack hits an enemy.");

        DiverEnabled = RoleEnabled(config, "Diver");
        DiverWeight = RoleWeight(config, "Diver");
        DiverUnderfloorDurationSeconds = BindFloat(config, "Diver", "UnderfloorDurationSeconds", 10f, 1f, 120f, "Maximum seconds Diver can remain below a floor before dying.");
        DiverMovementForce = BindFloat(config, "Diver", "MovementForce", 8f, 1f, 30f, "Force used for free movement while Diver is below a floor.");

        SniperEnabled = RoleEnabled(config, "Sniper");
        SniperWeight = RoleWeight(config, "Sniper");
        SniperReferenceDistance = BindFloat(config, "Sniper", "ReferenceDistance", 8f, 1f, 50f, "Distance in meters where Sniper deals normal damage.");
        SniperMinimumDamageMultiplier = BindFloat(config, "Sniper", "MinimumDamageMultiplier", 0.5f, 0f, 1f, "Damage multiplier at zero distance.");
        SniperMaximumDamageMultiplier = BindFloat(config, "Sniper", "MaximumDamageMultiplier", 2f, 1f, 10f, "Maximum long-range damage multiplier.");
        SniperMaximumMultiplierDistance = BindFloat(config, "Sniper", "MaximumMultiplierDistance", 24f, 1f, 100f, "Distance in meters where Sniper reaches the maximum damage multiplier.");

        ImitatorEnabled = RoleEnabled(config, "Imitator");
        ImitatorWeight = RoleWeight(config, "Imitator");

        AvengerEnabled = RoleEnabled(config, "Avenger");
        AvengerWeight = RoleWeight(config, "Avenger");
        AvengerDamageMultiplier = BindFloat(config, "Avenger", "DamageMultiplier", 1.5f, 1f, 10f, "Enemy damage multiplier after another player dies.");
        AvengerDurationSeconds = BindFloat(config, "Avenger", "DurationSeconds", 20f, 1f, 120f, "Duration of Avenger's enemy damage bonus after another player dies.");
        AvengerTriggerRadius = BindFloat(config, "Avenger", "TriggerRadius", 30f, 1f, 100f, "Maximum distance in meters from a dying player that activates Avenger.");

        BrawlerEnabled = RoleEnabled(config, "Brawler");
        BrawlerWeight = RoleWeight(config, "Brawler");
        BrawlerMeleeDamageMultiplier = BindFloat(config, "Brawler", "MeleeDamageMultiplier", 1.25f, 0f, 10f, "Damage multiplier for identifiable melee weapon attacks.");
        BrawlerRangedDamageMultiplier = BindFloat(config, "Brawler", "RangedDamageMultiplier", 0.75f, 0f, 10f, "Damage multiplier for identifiable ranged weapon attacks.");

        SuperbotEnabled = BindBool(config, "???1", "Enabled", true, "???");
        DisasterEnabled = BindBool(config, "???2", "Enabled", true, "???");
    }

    internal ConfigEntry<bool> Enabled { get; }
    internal ConfigEntry<bool> UniqueRoles { get; }
    internal ConfigEntry<int> ShopUpgradeItemCount { get; }
    internal ConfigEntry<bool> EnhancedEnemyRewardsEnabled { get; }
    internal ConfigEntry<bool> ShowcaseGuaranteesEnabled { get; }
    internal ConfigEntry<string> ShowcaseMinimumsByPlayerCount { get; }
    internal ConfigEntry<bool> SupportGuaranteesEnabled { get; }
    internal ConfigEntry<string> SupportMinimumsByPlayerCount { get; }
    internal ConfigEntry<bool> RiskCombinationLimitsEnabled { get; }
    internal ConfigEntry<string> DangerRoleLimit { get; }
    internal ConfigEntry<string> HardshipRoleLimit { get; }
    internal ConfigEntry<int> SmallPartyCombinedRiskPlayerCount { get; }
    internal ConfigEntry<int> SmallPartyCombinedRiskRoleLimit { get; }
    internal ConfigEntry<bool> PreventConsecutiveSameRole { get; }
    internal ConfigEntry<int> HardshipPersonalCooldownStages { get; }
    internal ConfigEntry<bool> ExcludeUnavailableContextRoles { get; }
    internal ConfigEntry<bool> AnnouncementsEnabled { get; }
    internal ConfigEntry<float> AnnouncementDelaySeconds { get; }
    internal ConfigEntry<float> StageFluxAnnouncementDelaySeconds { get; }
    internal ConfigEntry<string> GuideLanguage { get; }
    internal ConfigEntry<int> HudFontSize { get; }
    internal ConfigEntry<bool> HudEnabled { get; }
    internal ConfigEntry<string> HudRoleDisplay { get; }
    internal ConfigEntry<int> HudIconSize { get; }
    internal ConfigEntry<string> HudAnchor { get; }
    internal ConfigEntry<string> HudAlignment { get; }
    internal ConfigEntry<int> HudOffsetX { get; }
    internal ConfigEntry<int> HudOffsetY { get; }
    internal ConfigEntry<int> HudScalePercent { get; }
    internal ConfigEntry<int> HudPlayersPerPage { get; }
    internal ConfigEntry<float> HudPageIntervalSeconds { get; }
    internal ConfigEntry<float> HudTransitionDurationSeconds { get; }
    internal ConfigEntry<bool> HudPinLocalPlayer { get; }
    internal ConfigEntry<bool> SetRoleCommandEnabled { get; }

    internal ConfigEntry<string> BaseHealthLevels { get; }
    internal ConfigEntry<string> BaseStaminaLevels { get; }
    internal ConfigEntry<string> BaseExtraJumpLevels { get; }
    internal ConfigEntry<string> BaseSpeedLevels { get; }
    internal ConfigEntry<string> BaseStrengthLevels { get; }
    internal ConfigEntry<string> BaseRangeLevels { get; }
    internal ConfigEntry<string> BaseLaunchLevels { get; }
    internal ConfigEntry<string> BaseTumbleClimbLevels { get; }
    internal ConfigEntry<string> BaseTumbleWingsLevels { get; }
    internal ConfigEntry<string> BaseCrouchRestLevels { get; }
    internal ConfigEntry<string> BaseMapPlayerCountLevels { get; }
    internal ConfigEntry<string> BaseDeathHeadBatteryLevels { get; }
    internal ConfigEntry<bool> TruckUpgradeDrawEnabled { get; }
    internal ConfigEntry<int> TruckUpgradeDrawMaximumLevel { get; }
    internal ConfigEntry<float> TruckUpgradeDrawCappedWeightMultiplier { get; }
    internal ConfigEntry<float> TruckUpgradeDrawWeightFalloffExponent { get; }
    internal ConfigEntry<string> TruckUpgradeDrawDeltaWeights { get; }

    internal int TruckUpgradeDrawWeight(string commandName) =>
        _truckUpgradeDrawWeights.TryGetValue(
            commandName,
            out ConfigEntry<int>? entry)
            ? Math.Clamp(entry.Value, 0, 1000)
            : 0;

    internal ConfigEntry<bool> TankEnabled { get; }
    internal ConfigEntry<int> TankWeight { get; }
    internal ConfigEntry<int> TankHealthLevels { get; }
    internal ConfigEntry<bool> RunnerEnabled { get; }
    internal ConfigEntry<int> RunnerWeight { get; }
    internal ConfigEntry<int> RunnerSpeedLevels { get; }
    internal ConfigEntry<int> RunnerStaminaLevels { get; }
    internal ConfigEntry<bool> JumperEnabled { get; }
    internal ConfigEntry<int> JumperWeight { get; }
    internal ConfigEntry<int> JumperExtraJumpLevels { get; }
    internal ConfigEntry<bool> LifterEnabled { get; }
    internal ConfigEntry<int> LifterWeight { get; }
    internal ConfigEntry<int> LifterStrengthLevels { get; }
    internal ConfigEntry<bool> LauncherEnabled { get; }
    internal ConfigEntry<int> LauncherWeight { get; }
    internal ConfigEntry<int> LauncherLaunchLevels { get; }
    internal ConfigEntry<bool> ClimberEnabled { get; }
    internal ConfigEntry<int> ClimberWeight { get; }
    internal ConfigEntry<int> ClimberClimbLevels { get; }
    internal ConfigEntry<int> ClimberRangeLevels { get; }
    internal ConfigEntry<bool> FlyerEnabled { get; }
    internal ConfigEntry<int> FlyerWeight { get; }
    internal ConfigEntry<int> FlyerWingsLevels { get; }
    internal ConfigEntry<bool> TrackerEnabled { get; }
    internal ConfigEntry<int> TrackerWeight { get; }
    internal ConfigEntry<int> TrackerHealthLevels { get; }
    internal ConfigEntry<bool> GhostEnabled { get; }
    internal ConfigEntry<int> GhostWeight { get; }
    internal ConfigEntry<int> GhostDeathHeadBatteryLevels { get; }
    internal ConfigEntry<bool> BomberEnabled { get; }
    internal ConfigEntry<int> BomberWeight { get; }
    internal ConfigEntry<float> BomberDistance { get; }
    internal ConfigEntry<int> BomberMaximumActiveGrenades { get; }
    internal ConfigEntry<bool> BomberAllowTruckSpawns { get; }
    internal ConfigEntry<bool> BomberExplosiveEnabled { get; }
    internal ConfigEntry<bool> BomberStunEnabled { get; }
    internal ConfigEntry<bool> BomberShockwaveEnabled { get; }
    internal ConfigEntry<bool> BomberDuctTapedEnabled { get; }
    internal ConfigEntry<bool> MedicEnabled { get; }
    internal ConfigEntry<int> MedicWeight { get; }
    internal ConfigEntry<int> MedicHealAmount { get; }
    internal ConfigEntry<float> MedicHealIntervalSeconds { get; }
    internal ConfigEntry<float> MedicHealRadius { get; }
    internal ConfigEntry<int> MedicTotalHealingLimit { get; }
    internal ConfigEntry<bool> PhoenixEnabled { get; }
    internal ConfigEntry<int> PhoenixWeight { get; }
    internal ConfigEntry<int> PhoenixRevivalHealth { get; }
    internal ConfigEntry<float> PhoenixReviveDelaySeconds { get; }
    internal ConfigEntry<float> PhoenixFailureGraceSeconds { get; }
    internal ConfigEntry<bool> JoblessEnabled { get; }
    internal ConfigEntry<int> JoblessWeight { get; }
    internal ConfigEntry<int> JoblessDamage { get; }
    internal ConfigEntry<float> JoblessDamageIntervalSeconds { get; }
    internal ConfigEntry<bool> RescuerEnabled { get; }
    internal ConfigEntry<int> RescuerWeight { get; }
    internal ConfigEntry<int> RescuerRevivalHealth { get; }
    internal ConfigEntry<float> RescuerReviveDelaySeconds { get; }
    internal ConfigEntry<float> RescuerRadius { get; }
    internal ConfigEntry<int> RescuerMaximumRevives { get; }
    internal ConfigEntry<bool> VampireEnabled { get; }
    internal ConfigEntry<int> VampireWeight { get; }
    internal ConfigEntry<int> VampireTier1HealAmount { get; }
    internal ConfigEntry<int> VampireTier2HealAmount { get; }
    internal ConfigEntry<int> VampireTier3HealAmount { get; }
    internal ConfigEntry<float> VampireRadius { get; }
    internal ConfigEntry<bool> KingEnabled { get; }
    internal ConfigEntry<int> KingWeight { get; }
    internal ConfigEntry<bool> TunaEnabled { get; }
    internal ConfigEntry<int> TunaWeight { get; }
    internal ConfigEntry<float> TunaStationaryDelaySeconds { get; }
    internal ConfigEntry<int> TunaDamage { get; }
    internal ConfigEntry<float> TunaDamageIntervalSeconds { get; }
    internal ConfigEntry<bool> MusicianEnabled { get; }
    internal ConfigEntry<int> MusicianWeight { get; }
    internal ConfigEntry<int> MusicianHealAmount { get; }
    internal ConfigEntry<float> MusicianHealRadius { get; }
    internal ConfigEntry<bool> MageEnabled { get; }
    internal ConfigEntry<int> MageWeight { get; }
    internal ConfigEntry<float> MageCastIntervalSeconds { get; }
    internal ConfigEntry<string> MageStarExpression { get; }
    internal ConfigEntry<string> MageRollExpression { get; }
    internal ConfigEntry<string> MageGravityExpression { get; }
    internal ConfigEntry<string> MageVoidExpression { get; }
    internal ConfigEntry<string> MageLaserExpression { get; }
    internal ConfigEntry<bool> MageAutoRecoveryEnabled { get; }
    internal ConfigEntry<float> MageAutoRecoveryDelaySeconds { get; }
    internal ConfigEntry<float> MageAutoRecoveryIntervalSeconds { get; }
    internal ConfigEntry<int> MageAutoRecoveryAmount { get; }
    internal ConfigEntry<int> MageAutoRecoveryTotalHealingLimit { get; }
    internal ConfigEntry<int> MageStarHealthCost { get; }
    internal ConfigEntry<int> MageGravityHealthCost { get; }
    internal ConfigEntry<int> MageRollHealthCost { get; }
    internal ConfigEntry<int> MageVoidHealthCost { get; }
    internal ConfigEntry<int> MageLaserHealthCost { get; }
    internal ConfigEntry<bool> GamblerEnabled { get; }
    internal ConfigEntry<int> GamblerWeight { get; }
    internal ConfigEntry<int> GamblerWinChancePercent { get; }
    internal ConfigEntry<float> GamblerWinValueMultiplier { get; }
    internal ConfigEntry<int> GamblerGambitGreenHealAmount { get; }
    internal ConfigEntry<int> GamblerGambitRedDamage { get; }
    internal ConfigEntry<int> GamblerGambitWhiteHealthUpgradeLevels { get; }
    internal ConfigEntry<bool> HunterEnabled { get; }
    internal ConfigEntry<int> HunterWeight { get; }
    internal ConfigEntry<int> HunterBatteryConsumptionPercent { get; }
    internal ConfigEntry<float> HunterDoubleOrbChancePercent { get; }
    internal ConfigEntry<float> HunterJackpotOrbChancePercent { get; }
    internal ConfigEntry<int> HunterJackpotOrbCount { get; }
    internal ConfigEntry<bool> StinkerEnabled { get; }
    internal ConfigEntry<int> StinkerWeight { get; }
    internal ConfigEntry<float> StinkerDistance { get; }
    internal ConfigEntry<float> StinkerSafetyDistance { get; }
    internal ConfigEntry<bool> StinkerAllowTruckSpawns { get; }
    internal ConfigEntry<bool> EngineerEnabled { get; }
    internal ConfigEntry<int> EngineerWeight { get; }
    internal ConfigEntry<bool> TricksterEnabled { get; }
    internal ConfigEntry<int> TricksterWeight { get; }
    internal ConfigEntry<string> TricksterExpression { get; }
    internal ConfigEntry<float> TricksterActiveSeconds { get; }
    internal ConfigEntry<float> TricksterCooldownSeconds { get; }
    internal ConfigEntry<float> TricksterNoTargetCooldownSeconds { get; }
    internal ConfigEntry<float> TricksterInvestigateRadius { get; }
    internal ConfigEntry<float> TricksterPulseIntervalSeconds { get; }
    internal ConfigEntry<float> TricksterPlacementDistance { get; }
    internal ConfigEntry<bool> MechanicEnabled { get; }
    internal ConfigEntry<int> MechanicWeight { get; }
    internal ConfigEntry<float> MechanicRepairPercentPerSecond { get; }
    internal ConfigEntry<float> MechanicMaximumRepairPercentPerStage { get; }
    internal ConfigEntry<bool> ElectricianEnabled { get; }
    internal ConfigEntry<int> ElectricianWeight { get; }
    internal ConfigEntry<float> ElectricianChargePercentPerSecond { get; }
    internal ConfigEntry<float> ElectricianMaximumChargePercentPerStage { get; }
    internal ConfigEntry<bool> WardenEnabled { get; }
    internal ConfigEntry<int> WardenWeight { get; }
    internal ConfigEntry<float> WardenAdditionalStunSeconds { get; }
    internal ConfigEntry<bool> NinjaEnabled { get; }
    internal ConfigEntry<int> NinjaWeight { get; }
    internal ConfigEntry<float> NinjaVisionRecognitionMultiplier { get; }
    internal ConfigEntry<bool> ExecutionerEnabled { get; }
    internal ConfigEntry<int> ExecutionerWeight { get; }
    internal ConfigEntry<float> ExecutionerStunnedDamageMultiplier { get; }
    internal ConfigEntry<bool> RiderEnabled { get; }
    internal ConfigEntry<int> RiderWeight { get; }
    internal ConfigEntry<float> RiderEnemyDamageMultiplier { get; }
    internal ConfigEntry<float> RiderPlayerKnockbackMultiplier { get; }
    internal ConfigEntry<bool> InfluencerEnabled { get; }
    internal ConfigEntry<int> InfluencerWeight { get; }
    internal ConfigEntry<float> InfluencerRadius { get; }
    internal ConfigEntry<float> InfluencerPlayerCheckIntervalSeconds { get; }
    internal ConfigEntry<float> InfluencerNoiseRadiusMultiplier { get; }
    internal ConfigEntry<float> InfluencerTtsInvestigateRadius { get; }
    internal ConfigEntry<float> InfluencerMinimumTtsIntervalSeconds { get; }
    internal ConfigEntry<float> InfluencerMaximumTtsIntervalSeconds { get; }
    internal ConfigEntry<float> InfluencerTtsQuietPeriodSeconds { get; }
    internal IReadOnlyDictionary<string, ConfigEntry<string>> InfluencerUpgradeScaling { get; }
    internal ConfigEntry<bool> WerewolfEnabled { get; }
    internal ConfigEntry<int> WerewolfWeight { get; }
    internal ConfigEntry<float> WerewolfPlayerDamageMultiplier { get; }
    internal ConfigEntry<bool> BerserkerEnabled { get; }
    internal ConfigEntry<int> BerserkerWeight { get; }
    internal IReadOnlyDictionary<string, ConfigEntry<string>> BerserkerUpgradeScaling { get; }
    internal ConfigEntry<bool> BodyguardEnabled { get; }
    internal ConfigEntry<int> BodyguardWeight { get; }
    internal ConfigEntry<int> BodyguardHealthLevels { get; }
    internal ConfigEntry<float> BodyguardRadius { get; }
    internal ConfigEntry<float> BodyguardDamageSharePercent { get; }
    internal ConfigEntry<bool> RammerEnabled { get; }
    internal ConfigEntry<int> RammerWeight { get; }
    internal ConfigEntry<int> RammerTumbleDamage { get; }
    internal ConfigEntry<int> RammerSelfDamage { get; }
    internal ConfigEntry<bool> DiverEnabled { get; }
    internal ConfigEntry<int> DiverWeight { get; }
    internal ConfigEntry<float> DiverUnderfloorDurationSeconds { get; }
    internal ConfigEntry<float> DiverMovementForce { get; }
    internal ConfigEntry<bool> SniperEnabled { get; }
    internal ConfigEntry<int> SniperWeight { get; }
    internal ConfigEntry<float> SniperReferenceDistance { get; }
    internal ConfigEntry<float> SniperMinimumDamageMultiplier { get; }
    internal ConfigEntry<float> SniperMaximumDamageMultiplier { get; }
    internal ConfigEntry<float> SniperMaximumMultiplierDistance { get; }
    internal ConfigEntry<bool> ImitatorEnabled { get; }
    internal ConfigEntry<int> ImitatorWeight { get; }
    internal ConfigEntry<bool> AvengerEnabled { get; }
    internal ConfigEntry<int> AvengerWeight { get; }
    internal ConfigEntry<float> AvengerDamageMultiplier { get; }
    internal ConfigEntry<float> AvengerDurationSeconds { get; }
    internal ConfigEntry<float> AvengerTriggerRadius { get; }
    internal ConfigEntry<bool> BrawlerEnabled { get; }
    internal ConfigEntry<int> BrawlerWeight { get; }
    internal ConfigEntry<float> BrawlerMeleeDamageMultiplier { get; }
    internal ConfigEntry<float> BrawlerRangedDamageMultiplier { get; }
    internal ConfigEntry<bool> SuperbotEnabled { get; }
    internal ConfigEntry<bool> DisasterEnabled { get; }

    internal bool RoleIsEnabled(StageRole role) => RoleEnabledEntry(role)?.Value ?? false;

    internal ConfigEntry<bool>? RoleEnabledEntry(StageRole role) => role switch
    {
        StageRole.Tank => TankEnabled,
        StageRole.Runner => RunnerEnabled,
        StageRole.Jumper => JumperEnabled,
        StageRole.Lifter => LifterEnabled,
        StageRole.Launcher => LauncherEnabled,
        StageRole.Climber => ClimberEnabled,
        StageRole.Flyer => FlyerEnabled,
        StageRole.Tracker => TrackerEnabled,
        StageRole.Ghost => GhostEnabled,
        StageRole.Bomber => BomberEnabled,
        StageRole.Medic => MedicEnabled,
        StageRole.Phoenix => PhoenixEnabled,
        StageRole.Jobless => JoblessEnabled,
        StageRole.Rescuer => RescuerEnabled,
        StageRole.Vampire => VampireEnabled,
        StageRole.King => KingEnabled,
        StageRole.Tuna => TunaEnabled,
        StageRole.Musician => MusicianEnabled,
        StageRole.Mage => MageEnabled,
        StageRole.Gambler => GamblerEnabled,
        StageRole.Hunter => HunterEnabled,
        StageRole.Stinker => StinkerEnabled,
        StageRole.Engineer => EngineerEnabled,
        StageRole.Trickster => TricksterEnabled,
        StageRole.Mechanic => MechanicEnabled,
        StageRole.Electrician => ElectricianEnabled,
        StageRole.Warden => WardenEnabled,
        StageRole.Ninja => NinjaEnabled,
        StageRole.Executioner => ExecutionerEnabled,
        StageRole.Rider => RiderEnabled,
        StageRole.Influencer => InfluencerEnabled,
        StageRole.Werewolf => WerewolfEnabled,
        StageRole.Berserker => BerserkerEnabled,
        StageRole.Bodyguard => BodyguardEnabled,
        StageRole.Rammer => RammerEnabled,
        StageRole.Diver => DiverEnabled,
        StageRole.Sniper => SniperEnabled,
        StageRole.Imitator => ImitatorEnabled,
        StageRole.Avenger => AvengerEnabled,
        StageRole.Brawler => BrawlerEnabled,
        StageRole.Superbot => SuperbotEnabled,
        StageRole.Disaster => DisasterEnabled,
        _ => null
    };

    internal int RoleWeightValue(StageRole role) => role switch
    {
        StageRole.Tank => TankWeight.Value,
        StageRole.Runner => RunnerWeight.Value,
        StageRole.Jumper => JumperWeight.Value,
        StageRole.Lifter => LifterWeight.Value,
        StageRole.Launcher => LauncherWeight.Value,
        StageRole.Climber => ClimberWeight.Value,
        StageRole.Flyer => FlyerWeight.Value,
        StageRole.Tracker => TrackerWeight.Value,
        StageRole.Ghost => GhostWeight.Value,
        StageRole.Bomber => BomberWeight.Value,
        StageRole.Medic => MedicWeight.Value,
        StageRole.Phoenix => PhoenixWeight.Value,
        StageRole.Jobless => JoblessWeight.Value,
        StageRole.Rescuer => RescuerWeight.Value,
        StageRole.Vampire => VampireWeight.Value,
        StageRole.King => KingWeight.Value,
        StageRole.Tuna => TunaWeight.Value,
        StageRole.Musician => MusicianWeight.Value,
        StageRole.Mage => MageWeight.Value,
        StageRole.Gambler => GamblerWeight.Value,
        StageRole.Hunter => HunterWeight.Value,
        StageRole.Stinker => StinkerWeight.Value,
        StageRole.Engineer => EngineerWeight.Value,
        StageRole.Trickster => TricksterWeight.Value,
        StageRole.Mechanic => MechanicWeight.Value,
        StageRole.Electrician => ElectricianWeight.Value,
        StageRole.Warden => WardenWeight.Value,
        StageRole.Ninja => NinjaWeight.Value,
        StageRole.Executioner => ExecutionerWeight.Value,
        StageRole.Rider => RiderWeight.Value,
        StageRole.Influencer => InfluencerWeight.Value,
        StageRole.Werewolf => WerewolfWeight.Value,
        StageRole.Berserker => BerserkerWeight.Value,
        StageRole.Bodyguard => BodyguardWeight.Value,
        StageRole.Rammer => RammerWeight.Value,
        StageRole.Diver => DiverWeight.Value,
        StageRole.Sniper => SniperWeight.Value,
        StageRole.Imitator => ImitatorWeight.Value,
        StageRole.Avenger => AvengerWeight.Value,
        StageRole.Brawler => BrawlerWeight.Value,
        StageRole.Superbot => 1,
        StageRole.Disaster => 1,
        _ => 0
    };

    internal float ClampedBomberDistance => Mathf.Clamp(BomberDistance.Value, 1f, 100f);
    internal int ClampedBomberMaximum => Mathf.Clamp(BomberMaximumActiveGrenades.Value, 1, 30);
    internal float ClampedHunterBatteryConsumption =>
        Mathf.Clamp(HunterBatteryConsumptionPercent.Value, 0, 100) / 100f;
    internal float ClampedStinkerDistance =>
        Mathf.Clamp(StinkerDistance.Value, 1f, 100f);
    internal float ClampedStinkerSafetyDistance =>
        Mathf.Clamp(StinkerSafetyDistance.Value, 1f, 30f);
    internal float ClampedJoblessInterval => Mathf.Clamp(JoblessDamageIntervalSeconds.Value, 0.05f, 10f);
    internal float ClampedTunaDamageInterval => Mathf.Clamp(TunaDamageIntervalSeconds.Value, 0.05f, 10f);

    internal int ShowcaseMinimumForPlayerCount(int playerCount) =>
        ShowcaseGuaranteesEnabled.Value
            ? Mathf.Min(
                Mathf.Max(0, playerCount),
                PartySizeScalingValue(
                    ShowcaseMinimumsByPlayerCount.Value,
                    playerCount,
                    allowZero: true))
            : 0;

    internal int SupportMinimumForPlayerCount(int playerCount) =>
        SupportGuaranteesEnabled.Value
            ? Mathf.Min(
                Mathf.Max(0, playerCount),
                PartySizeScalingValue(
                    SupportMinimumsByPlayerCount.Value,
                    playerCount,
                    allowZero: true))
            : 0;

    internal int DangerRoleLimitForPlayerCount(int playerCount) =>
        PartySizeScalingValue(DangerRoleLimit.Value, playerCount);

    internal int HardshipRoleLimitForPlayerCount(int playerCount) =>
        PartySizeScalingValue(HardshipRoleLimit.Value, playerCount);

    private static int PartySizeScalingValue(
        string expression,
        int playerCount,
        bool allowZero = false)
    {
        RoleUpgradeScaling.TryParse(
            expression,
            MaximumSupportedPlayers,
            MaximumSupportedPlayers,
            out IReadOnlyList<UpgradeScalingRule> rules,
            out _);
        int result = 0;
        foreach (UpgradeScalingRule rule in rules)
        {
            if (playerCount >= rule.Condition)
            {
                result = rule.Level;
            }
        }
        return Mathf.Clamp(
            result,
            allowZero ? 0 : 1,
            MaximumSupportedPlayers);
    }

    private static ConfigEntry<bool> RoleEnabled(ConfigFile config, string section) =>
        BindBool(config, section, "Enabled", true, "Includes this role in stage assignment.");

    private static ConfigEntry<int> RoleWeight(
        ConfigFile config,
        string section,
        int defaultValue = 100) =>
        BindInt(config, section, "Weight", defaultValue, 0, 1000, "Relative assignment weight.");

    private static ConfigEntry<string> PartySizeScaling(
        ConfigFile config,
        string section,
        string key,
        string defaultValue,
        string description) =>
        config.Bind(
            section,
            key,
            defaultValue,
            description + " Use comma-separated party size:limit pairs.");

    private static ConfigEntry<string> ExpressionName(
        ConfigFile config,
        string section,
        string key,
        string defaultValue,
        string action) =>
        config.Bind(
            section,
            key,
            defaultValue,
            new ConfigDescription(
                $"Facial expression that triggers {action}.",
                new AcceptableValueList<string>(
                    "Disabled",
                    "Angry",
                    "Sad",
                    "Suspicious",
                    "EyesClosed",
                    "Scared",
                    "Happy")));

    private static ConfigEntry<int> UpgradeLevel(
        ConfigFile config,
        string section,
        string key,
        int defaultValue) =>
        BindInt(config, section, key, defaultValue, 0, RoleUpgradeScaling.MaximumUpgradeLevel, "Vanilla upgrade target level while this role is assigned.");

    private static IReadOnlyDictionary<string, ConfigEntry<string>> UpgradeScalingEntries(
        ConfigFile config,
        string section,
        bool useHealthPercent,
        bool includeHealth)
    {
        Dictionary<string, ConfigEntry<string>> result = new(StringComparer.Ordinal);
        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            if (!includeHealth && upgrade.Name == "Health")
            {
                continue;
            }
            string defaultValue = useHealthPercent
                ? RoleUpgradeScaling.BerserkerDefault(upgrade.Name)
                : RoleUpgradeScaling.InfluencerDefault(upgrade.Name);
            string condition = useHealthPercent
                ? "remaining HP percent at or below which the value applies"
                : "nearby-player count at or above which the value applies";
            result[upgrade.Name] = config.Bind(
                section,
                $"{upgrade.Name}UpgradeScaling",
                defaultValue,
                $"Comma-separated condition:level pairs ({condition}). " +
                $"Level range: 0-{upgrade.MaximumLevel}. Blank and conditions not yet reached use target level 0.");
        }
        return result;
    }

    private static ConfigEntry<string> BaseUpgradeScaling(
        ConfigFile config,
        string key,
        string defaultValue,
        int maximumLevel) =>
        config.Bind(
            "Base Upgrades",
            key,
            defaultValue,
            $"Comma-separated level:value pairs. The last entry at or below the current run level sets the base vanilla upgrade target. " +
            $"Run level range: 1-999999. Value range: 0-{maximumLevel}; out-of-range numeric values are clamped. " +
            $"Blank and levels not yet reached use target level 0.");

    private void BindTruckUpgradeDrawWeight(
        ConfigFile config,
        string upgradeName)
    {
        _baseUpgradeDrawEnabled[upgradeName] = BindBool(config, "Base Upgrade Draw Selection", upgradeName, true,
            "Includes this upgrade in future truck draws, including All Upgrades results. Existing levels and configured base targets are kept. " +
            "For AllUpgrades, controls the combined draw result only; individual upgrade switches still apply.");
        _truckUpgradeDrawWeights[upgradeName] = BindInt(
            config,
            "Base Upgrade Draw Weights",
            upgradeName,
            TruckDrawWeight.DefaultWeight(upgradeName),
            0,
            1000,
            $"Relative selection weight for {upgradeName} in the truck Base Upgrade draw. " +
            (upgradeName == "AllUpgrades"
                ? "All Upgrades uses this weight directly. "
                : "Weight gradually decreases toward CappedUpgradeWeightMultiplier as the upgrade approaches its draw maximum. Defaults reflect vanilla shop stock. ") +
            "Zero excludes it from direct selection.");
    }

    private static ConfigEntry<bool> BindBool(
        ConfigFile config,
        string section,
        string key,
        bool defaultValue,
        string description) =>
        config.Bind(section, key, defaultValue, description);

    private static ConfigEntry<int> BindInt(
        ConfigFile config,
        string section,
        string key,
        int defaultValue,
        int minimum,
        int maximum,
        string description) =>
        config.Bind(
            section,
            key,
            defaultValue,
            new ConfigDescription(
                description,
                new AcceptableValueRange<int>(minimum, maximum)));

    private static ConfigEntry<float> BindFloat(
        ConfigFile config,
        string section,
        string key,
        float defaultValue,
        float minimum,
        float maximum,
        string description) =>
        config.Bind(
            section,
            key,
            defaultValue,
            new ConfigDescription(
                description,
                new AcceptableValueRange<float>(minimum, maximum)));
}
