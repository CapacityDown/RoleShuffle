using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx.Configuration;

namespace REPOJP.StageRoles;

internal static class RoleConfigMigration
{
    private const int CurrentSchemaVersion = 39;
    private static readonly ConfigDefinition SchemaDefinition =
        new("Migration", "ConfigVersion");

    private static readonly string[] RemovedSections =
    {
        "Camper",
        "Commands",
        "Decoy",
        "Longarm",
        "Librarian",
        "Magician",
        "Magnet",
        "Marathoner",
        "Mimic",
        "Musicain",
        "Navigator",
        "Physician",
        "Sapper",
        "SpeedStar",
        "Sprinter",
        "Striker",
        "Tamer",
        "Thrower",
        "Unemployed"
    };

    internal static void Apply(ConfigFile config)
    {
        string path = config.ConfigFilePath;
        string temporaryPath = path + ".roleshuffle.tmp";
        try
        {
            Dictionary<ConfigDefinition, string> values = ReadValues(path);
            int sourceVersion = ReadSchemaVersion(values);
            if (sourceVersion > CurrentSchemaVersion)
            {
                StageRolesPlugin.ModLogger.LogWarning(
                    $"RoleShuffle config schema {sourceVersion} is newer than " +
                    $"the supported schema {CurrentSchemaVersion}; migration was skipped.");
                return;
            }

            int migrated = 0;
            int removed = 0;
            MoveKeys(values, "???", "???1", ref migrated, ref removed, "Enabled");
            migrated += MigrateKeys(
                values,
                "SpeedStar",
                "Runner",
                "Enabled", "Weight", "SpeedUpgradeLevels", "StaminaUpgradeLevels");
            migrated += MigrateKeys(
                values,
                "Sprinter",
                "Runner",
                "Enabled", "Weight", "SpeedUpgradeLevels");
            migrated += MigrateKeys(
                values,
                "Marathoner",
                "Runner",
                "Enabled", "Weight", "StaminaUpgradeLevels");
            migrated += MigrateKeys(
                values,
                "Longarm",
                "Climber",
                "RangeUpgradeLevels");
            migrated += MigrateKeys(
                values,
                "Unemployed",
                "Jobless",
                "Enabled", "Weight", "Damage", "DamageIntervalSeconds");
            migrated += MigrateKeys(
                values,
                "Musicain",
                "Musician",
                "Enabled", "Weight", "HealAmount", "HealRadius");
            migrated += MigrateKeys(
                values,
                "Magician",
                "Mage",
                "Enabled", "Weight", "CastIntervalSeconds");
            migrated += MigrateKeys(
                values,
                "Physician",
                "Medic",
                "Enabled", "Weight", "HealAmount", "HealIntervalSeconds",
                "HealRadius", "TotalHealingLimit");
            migrated += MigrateKeys(
                values,
                "Navigator",
                "Medic",
                "Enabled", "Weight", "HealAmount", "HealIntervalSeconds",
                "HealRadius", "TotalHealingLimit");
            migrated += MigrateKeys(
                values,
                "Librarian",
                "Ninja",
                "Enabled", "Weight");
            migrated += MigrateKeys(
                values,
                "Striker",
                "Rammer",
                "Enabled", "Weight", "TumbleAttackDamage");
            migrated += MigrateKeys(
                values,
                "Mimic",
                "Imitator",
                "Enabled", "Weight");

            if (sourceVersion < 5)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Jobless",
                    "Weight",
                    "100",
                    "20");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Medic",
                    "TotalHealingLimit",
                    "200",
                    "150");
            }

            if (sourceVersion < 6)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Hunter",
                    "JackpotOrbChancePercent",
                    "0.01",
                    "0.5");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mechanic",
                    "RepairPercentPerSecond",
                    "1",
                    "2");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Electrician",
                    "ChargePercentPerSecond",
                    "5",
                    "10");
            }

            if (sourceVersion < 7)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Stinker",
                    "DistancePerCloud",
                    "1",
                    "2");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Stinker",
                    "MinimumSafetyDistance",
                    "1",
                    "2");
            }

            if (sourceVersion == 7)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Stinker",
                    "DistancePerCloud",
                    "3",
                    "2");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Stinker",
                    "MinimumSafetyDistance",
                    "3",
                    "2");
            }

            if (sourceVersion < 10)
            {
                migrated += MigrateDynamicUpgradeScaling(values);
            }
            if (sourceVersion < 11)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Influencer",
                    "ExtraJumpUpgradeScaling",
                    "1:0,2:1,3:2,4:4,5:5",
                    "2:1,3:2,4:4,5:5");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Influencer",
                    "HealthUpgradeScaling",
                    "1:0,2:0,3:0,4:6,5:11",
                    "4:6,5:11");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Berserker",
                    "ExtraJumpUpgradeScaling",
                    "80:0,60:1,40:2,20:4,10:5",
                    "60:1,40:2,20:4,10:5");
            }
            if (sourceVersion < 12)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Berserker",
                    "SpeedUpgradeScaling",
                    "80:1,60:2,40:3,20:3,10:3",
                    "80:1,60:2,40:3,20:3,10:4");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Berserker",
                    "StaminaUpgradeScaling",
                    "80:3,60:6,40:10,20:12,10:15",
                    "80:3,60:6,40:10,20:12,10:20");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Berserker",
                    "TumbleWingsUpgradeScaling",
                    "80:1,60:2,40:3,20:4,10:5",
                    string.Empty);
            }
            if (sourceVersion < 13)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Rider",
                    "EnemyDamageMultiplier",
                    "2",
                    "3");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Rider",
                    "EnemyDamageMultiplier",
                    "2.0",
                    "3");
            }
            if (sourceVersion < 14)
            {
                migrated += MigrateBaseUpgradeScaling(values);
            }
            if (sourceVersion < 15)
            {
                migrated += MigratePartySizeLimit(
                    values,
                    "DangerRoleLimit",
                    "1:1,8:2,16:3");
                migrated += MigratePartySizeLimit(
                    values,
                    "HardshipRoleLimit",
                    "1:1,12:2");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Role Balance",
                    "HardshipPersonalCooldownStages",
                    "1",
                    "2");
                migrated += UpdateDefaultRoleWeights(values);
            }
            if (sourceVersion < 17)
            {
                MigrateGuaranteeScaling(
                    values,
                    "Showcase",
                    new[] { 3, 7, 14, 24 },
                    new[] { 1, 2, 3, 4 },
                    ref migrated,
                    ref removed);
                MigrateGuaranteeScaling(
                    values,
                    "Support",
                    new[] { 4, 8, 14, 24 },
                    new[] { 1, 2, 3, 4 },
                    ref migrated,
                    ref removed);
                MoveKeys(
                    values,
                    "Role Balance",
                    "Role Balance - Guarantees",
                    ref migrated,
                    ref removed,
                    "ShowcaseGuaranteesEnabled",
                    "SupportGuaranteesEnabled");
                MoveKeys(
                    values,
                    "Role Balance",
                    "Role Balance - Limits",
                    ref migrated,
                    ref removed,
                    "RiskCombinationLimitsEnabled",
                    "DangerRoleLimit",
                    "HardshipRoleLimit",
                    "SmallPartyCombinedRiskPlayerCount",
                    "SmallPartyCombinedRiskRoleLimit");
                MoveKeys(
                    values,
                    "Role Balance",
                    "Role Balance - Variety",
                    ref migrated,
                    ref removed,
                    "PreventConsecutiveSameRole",
                    "HardshipPersonalCooldownStages",
                    "ExcludeUnavailableContextRoles");
                MoveKeys(
                    values,
                    "Base Upgrades",
                    "Base Upgrade Draw",
                    ref migrated,
                    ref removed,
                    "TruckUpgradeDrawEnabled",
                    "TruckUpgradeDrawMaximumLevel",
                    "TruckUpgradeDrawDeltaWeights");
            }
            if (sourceVersion < 18)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Trickster",
                    "ActiveSeconds",
                    "15",
                    "25");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Trickster",
                    "InvestigateRadius",
                    "30",
                    "40");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Trickster",
                    "PulseIntervalSeconds",
                    "2",
                    "1");
            }
            if (sourceVersion < 20)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Diver",
                    "UnderfloorDurationSeconds",
                    "30",
                    "15");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Diver",
                    "UnderfloorDurationSeconds",
                    "30.0",
                    "15");
            }
            if (sourceVersion < 21)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Rammer",
                    "TumbleAttackDamage",
                    "150",
                    "100");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "CastIntervalSeconds",
                    "5",
                    "3");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "CastIntervalSeconds",
                    "5.0",
                    "3");
            }
            if (sourceVersion < 22)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Diver",
                    "UnderfloorDurationSeconds",
                    "15",
                    "10");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Diver",
                    "UnderfloorDurationSeconds",
                    "15.0",
                    "10");
            }
            if (sourceVersion < 23)
            {
                MoveKey(values, "Role Balance - Guarantees", "ShowcaseGuaranteesEnabled", "Role Balance - Guarantees", "ShowcaseEnabled", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Guarantees", "ShowcaseMinimumsByPlayerCount", "Role Balance - Guarantees", "ShowcaseMinimums", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Guarantees", "SupportGuaranteesEnabled", "Role Balance - Guarantees", "SupportEnabled", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Guarantees", "SupportMinimumsByPlayerCount", "Role Balance - Guarantees", "SupportMinimums", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Limits", "RiskCombinationLimitsEnabled", "Role Balance - Limits", "Enabled", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Limits", "DangerRoleLimit", "Role Balance - Limits", "DangerMaximums", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Limits", "HardshipRoleLimit", "Role Balance - Limits", "HardshipMaximums", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Limits", "SmallPartyCombinedRiskPlayerCount", "Role Balance - Limits", "SmallPartyMaximumPlayers", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Limits", "SmallPartyCombinedRiskRoleLimit", "Role Balance - Limits", "SmallPartyCombinedMaximum", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Variety", "PreventConsecutiveSameRole", "Role Balance - Variety", "PreventSameRole", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Variety", "HardshipPersonalCooldownStages", "Role Balance - Variety", "HardshipCooldownStages", ref migrated, ref removed);
                MoveKey(values, "Role Balance - Variety", "ExcludeUnavailableContextRoles", "Role Balance - Variety", "ExcludeUnavailableRoles", ref migrated, ref removed);
                MoveKey(values, "Notifications", "AnnouncementDelaySeconds", "Notifications", "DelaySeconds", ref migrated, ref removed);
                MoveKey(values, "Notifications", "StageFluxAnnouncementDelaySeconds", "Notifications", "StageFluxDelaySeconds", ref migrated, ref removed);
                MoveKey(values, "Testing", "SetRoleCommandEnabled", "Testing", "Enabled", ref migrated, ref removed);
                MoveKey(values, "Base Upgrade Draw", "TruckUpgradeDrawEnabled", "Base Upgrade Draw", "Enabled", ref migrated, ref removed);
                MoveKey(values, "Base Upgrade Draw", "TruckUpgradeDrawMaximumLevel", "Base Upgrade Draw", "MaximumLevel", ref migrated, ref removed);
                MoveKey(values, "Base Upgrade Draw", "TruckUpgradeDrawDeltaWeights", "Base Upgrade Draw", "ChangeAmountWeights", ref migrated, ref removed);
            }
            if (sourceVersion < 24)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Brawler",
                    "MeleeDamageMultiplier",
                    "2",
                    "1.25");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Brawler",
                    "MeleeDamageMultiplier",
                    "2.0",
                    "1.25");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Brawler",
                    "RangedDamageMultiplier",
                    "0.5",
                    "0.75");
            }
            if (sourceVersion < 27)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "StarExpression",
                    "Happy",
                    "Angry");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "RollExpression",
                    "Angry",
                    "Sad");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "VoidExpression",
                    "Crazy",
                    "EyesClosed");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Mage",
                    "LaserExpression",
                    "Eyes Closed",
                    "Scared");
                migrated += ReplaceValueIfEqual(
                    values,
                    "Trickster",
                    "DecoyExpression",
                    "Sad",
                    "Angry");
            }
            if (sourceVersion < 28)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Trickster",
                    "DecoyExpression",
                    "Angry",
                    "Happy");
            }
            if (sourceVersion < 29)
            {
                migrated += ReplaceValueIfEqual(
                    values,
                    "Testing",
                    "Enabled",
                    "true",
                    "false");
            }

            if (sourceVersion < 31 &&
                (!values.TryGetValue(new ConfigDefinition("HUD", "Anchor"), out string? hudAnchor) ||
                 string.Equals(hudAnchor, "BottomLeft", StringComparison.OrdinalIgnoreCase)))
            {
                migrated += ReplaceValueIfEqual(values, "HUD", "OffsetY", "0", "80");
                migrated += ReplaceValueIfEqual(values, "HUD", "OffsetY", "10", "80");
            }

            if (sourceVersion < 36)
            {
                migrated += ReplaceValueIfEqual(values, "HUD", "ResourceHudOffsetX", "16", "0");
                migrated += ReplaceValueIfEqual(values, "HUD", "ResourceHudOffsetY", "24", "0");
            }

            foreach (string section in RemovedSections)
            {
                removed += RemoveSection(values, section);
            }

            removed += RemoveKey(values, "General", "OverhaulEnabled");
            removed += RemoveKey(values, "Lifter", "StrengthUpgradeLevels");
            removed += RemoveKey(values, "HUD", "AbilityStatusEnabled");
            removed += RemoveKey(values, "Jobless", "ContractHeal");
            MoveKey(values, "Jobless", "ContractGraceSeconds", "Jobless", "InitialGraceSeconds", ref migrated, ref removed);
            removed += RemoveKey(values, "Jobless", "ContractsPerStage");
            MoveKeys(values, "Jobless", "Courier", ref migrated, ref removed,
                "Enabled", "Weight", "Damage", "DamageIntervalSeconds", "ContractDistance", "InitialGraceSeconds",
                "TinyGraceSeconds", "SmallGraceSeconds", "MediumGraceSeconds", "BigGraceSeconds",
                "WideGraceSeconds", "TallGraceSeconds", "VeryTallGraceSeconds", "TinyHealAmount", "SmallHealAmount",
                "MediumHealAmount", "BigHealAmount", "WideHealAmount", "TallHealAmount", "VeryTallHealAmount");
            removed += RemoveKey(values, "Courier", "ContractHeal");
            removed += RemoveKey(values, "Courier", "ContractsPerStage");
            MoveKey(values, "King", "HealRadius", "King", "UpgradeRadius", ref migrated, ref removed);
            removed += RemoveKey(values, "King", "HealAmount");
            removed += RemoveKey(values, "King", "HealIntervalSeconds");
            removed += RemoveKey(values, "King", "TotalHealingLimit");
            removed += RemoveKey(values, "Gambler", "LoseValueMultiplier");
            removed += RemoveKey(values, "Gambler", "MaximumWagersPerStage");
            removed += RemoveKey(values, "Gambler", "HealAmountPerExtraction");
            removed += RemoveKey(values, "Gambler", "GambitYellowValuableValue");
            removed += RemoveKey(values, "Mage", "BeamChancePercent");
            removed += RemoveKey(values, "Medic", "OrbFollowHeight");
            removed += RemoveKey(values, "Medic", "OrbFollowSide");
            removed += RemoveKey(values, "Ghost", "DeathHeadMovementEnabled");
            removed += RemoveKey(values, "Ghost", "DeathHeadMovementSpeed");
            removed += RemoveKey(values, "Ghost", "MovementBatteryUpgradeLevels");
            removed += RemoveKey(values, "Vampire", "HealAmount");
            removed += RemoveKey(values, "Tank", "BaseHealthBonus");
            removed += RemoveKey(values, "Runner", "BaseSpeedBonus");
            removed += RemoveKey(values, "Runner", "BaseStaminaBonus");
            removed += RemoveKey(values, "Lifter", "BaseStrengthBonus");
            removed += RemoveKey(values, "Lifter", "StrengthMultiplier");
            removed += RemoveKey(values, "Lifter", "MaximumEffectiveStrength");
            removed += RemoveLegacyDynamicUpgradeSettings(values);

            bool schemaChanged = sourceVersion != CurrentSchemaVersion;
            values[SchemaDefinition] = CurrentSchemaVersion.ToString(
                CultureInfo.InvariantCulture);
            if (!schemaChanged && migrated == 0 && removed == 0)
            {
                return;
            }

            if (sourceVersion < CurrentSchemaVersion && values.Count > 1 &&
                File.Exists(path))
            {
                string backupPath = path + (sourceVersion >= 38 ? ".pre-v4.5.0-delivery-grace.bak" : sourceVersion >= 37 ? ".pre-v4.5.0-jobless-full-heal.bak" : sourceVersion >= 36 ? ".pre-v4.5.0-standard-roles.bak" : sourceVersion >= 35 ? ".pre-v4.5.0-native-hud.bak" : sourceVersion >= 34 ? ".pre-v4.5.0-lifter-200.bak" : sourceVersion >= 33 ? ".pre-v4.5.0-king-upgrades.bak" : sourceVersion >= 32 ? ".pre-v4.5.0-multipliers.bak" : ".pre-v4.4.0.bak");
                if (!File.Exists(backupPath))
                {
                    File.Copy(path, backupPath, overwrite: false);
                }
            }

            WriteValues(temporaryPath, values);
            File.Copy(temporaryPath, path, overwrite: true);
            File.Delete(temporaryPath);
            config.Reload();
            StageRolesPlugin.ModLogger.LogInfo(
                $"RoleShuffle config migrated to schema {CurrentSchemaVersion}: " +
                $"{migrated} value(s) transferred, {removed} obsolete value(s) removed.");
        }
        catch (Exception exception)
        {
            TryDelete(temporaryPath);
            StageRolesPlugin.ModLogger.LogWarning(
                $"RoleShuffle config migration was skipped: {exception}");
        }
    }

    private static Dictionary<ConfigDefinition, string> ReadValues(string path)
    {
        Dictionary<ConfigDefinition, string> values = new();
        if (!File.Exists(path))
        {
            return values;
        }

        string section = string.Empty;
        foreach (string sourceLine in File.ReadAllLines(path))
        {
            string line = sourceLine.Trim();
            if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }
            if (line.StartsWith("[", StringComparison.Ordinal) &&
                line.EndsWith("]", StringComparison.Ordinal))
            {
                section = line.Substring(1, line.Length - 2);
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                continue;
            }
            string key = line.Substring(0, separator).Trim();
            string value = line.Substring(separator + 1).Trim();
            values[new ConfigDefinition(section, key)] = value;
        }
        return values;
    }

    private static int ReadSchemaVersion(
        IReadOnlyDictionary<ConfigDefinition, string> values) =>
        values.TryGetValue(SchemaDefinition, out string? serialized) &&
        int.TryParse(
            serialized,
            NumberStyles.Integer,
            CultureInfo.InvariantCulture,
            out int version)
            ? Math.Max(0, version)
            : 0;

    private static int MigrateKeys(
        IDictionary<ConfigDefinition, string> values,
        string oldSection,
        string newSection,
        params string[] keys)
    {
        int migrated = 0;
        foreach (string key in keys)
        {
            ConfigDefinition oldDefinition = new(oldSection, key);
            ConfigDefinition newDefinition = new(newSection, key);
            if (!values.TryGetValue(oldDefinition, out string? value) ||
                values.ContainsKey(newDefinition))
            {
                continue;
            }
            values[newDefinition] = value;
            migrated++;
        }
        return migrated;
    }

    private static void MoveKeys(
        IDictionary<ConfigDefinition, string> values,
        string oldSection,
        string newSection,
        ref int migrated,
        ref int removed,
        params string[] keys)
    {
        foreach (string key in keys)
        {
            ConfigDefinition oldDefinition = new(oldSection, key);
            if (!values.TryGetValue(oldDefinition, out string? value))
            {
                continue;
            }

            ConfigDefinition newDefinition = new(newSection, key);
            if (!values.ContainsKey(newDefinition))
            {
                values[newDefinition] = value;
                migrated++;
            }
            values.Remove(oldDefinition);
            removed++;
        }
    }

    private static void MoveKey(
        IDictionary<ConfigDefinition, string> values,
        string oldSection,
        string oldKey,
        string newSection,
        string newKey,
        ref int migrated,
        ref int removed)
    {
        ConfigDefinition oldDefinition = new(oldSection, oldKey);
        if (!values.TryGetValue(oldDefinition, out string? value))
        {
            return;
        }

        ConfigDefinition newDefinition = new(newSection, newKey);
        if (!values.ContainsKey(newDefinition))
        {
            values[newDefinition] = value;
            migrated++;
        }
        values.Remove(oldDefinition);
        removed++;
    }

    private static void MigrateGuaranteeScaling(
        IDictionary<ConfigDefinition, string> values,
        string prefix,
        int[] defaultStarts,
        int[] defaultMinimums,
        ref int migrated,
        ref int removed)
    {
        string[] startSuffixes =
        {
            "StartPlayerCount",
            "MediumPlayerCount",
            "LargePlayerCount",
            "ExtraLargePlayerCount"
        };
        string[] minimumSuffixes =
        {
            "MinimumSmall",
            "MinimumMedium",
            "MinimumLarge",
            "MinimumExtraLarge"
        };
        ConfigDefinition[] oldDefinitions = startSuffixes
            .Select(suffix => new ConfigDefinition("Role Balance", prefix + suffix))
            .Concat(minimumSuffixes.Select(suffix =>
                new ConfigDefinition("Role Balance", prefix + suffix)))
            .ToArray();
        if (!oldDefinitions.Any(values.ContainsKey))
        {
            return;
        }

        int[] starts = new int[4];
        int[] minimums = new int[4];
        for (int index = 0; index < starts.Length; index++)
        {
            starts[index] = Math.Clamp(
                ReadInt(
                    values,
                    "Role Balance",
                    prefix + startSuffixes[index],
                    defaultStarts[index]),
                1,
                30);
            if (index > 0)
            {
                starts[index] = Math.Max(starts[index - 1], starts[index]);
            }
            minimums[index] = Math.Clamp(
                ReadInt(
                    values,
                    "Role Balance",
                    prefix + minimumSuffixes[index],
                    defaultMinimums[index]),
                0,
                30);
        }

        ConfigDefinition destination = new(
            "Role Balance - Guarantees",
            prefix + "MinimumsByPlayerCount");
        if (!values.ContainsKey(destination))
        {
            List<string> tiers = new();
            int previous = 0;
            for (int playerCount = 1; playerCount <= 30; playerCount++)
            {
                int current = LegacyGuaranteeMinimum(
                    playerCount,
                    starts,
                    minimums);
                if (current == previous)
                {
                    continue;
                }
                tiers.Add(
                    $"{playerCount.ToString(CultureInfo.InvariantCulture)}:" +
                    current.ToString(CultureInfo.InvariantCulture));
                previous = current;
            }
            values[destination] = string.Join(",", tiers);
            migrated++;
        }

        foreach (ConfigDefinition definition in oldDefinitions)
        {
            if (values.Remove(definition))
            {
                removed++;
            }
        }
    }

    private static int LegacyGuaranteeMinimum(
        int playerCount,
        IReadOnlyList<int> starts,
        IReadOnlyList<int> minimums)
    {
        if (playerCount < starts[0])
        {
            return 0;
        }
        for (int index = starts.Count - 1; index >= 0; index--)
        {
            if (playerCount >= starts[index])
            {
                return Math.Clamp(minimums[index], 0, playerCount);
            }
        }
        return 0;
    }

    private static int MigrateDynamicUpgradeScaling(
        IDictionary<ConfigDefinition, string> values)
    {
        int migrated = 0;
        int[] healthThresholds = new int[5];
        for (int index = 0; index < healthThresholds.Length; index++)
        {
            healthThresholds[index] = ReadInt(
                values,
                "Berserker",
                $"Tier{index + 1}HealthPercent",
                new[] { 80, 60, 40, 20, 10 }[index]);
        }

        foreach (DynamicUpgradeDefinition upgrade in RoleUpgradeScaling.Definitions)
        {
            int[] levels = LegacyInfluencerLevels(values, upgrade.Name);
            bool[] includeExplicitZero = LegacyExplicitZeroOverrides(
                values,
                upgrade.Name);
            ConfigDefinition influencerDefinition = new(
                "Influencer",
                $"{upgrade.Name}UpgradeScaling");
            if (!values.ContainsKey(influencerDefinition))
            {
                values[influencerDefinition] = levels.Length == 0
                    ? string.Empty
                    : ScalingExpression(
                        new[] { 1, 2, 3, 4, 5 },
                        levels,
                        includeExplicitZero);
                migrated++;
            }

            if (upgrade.Name == "Health")
            {
                continue;
            }
            ConfigDefinition berserkerDefinition = new(
                "Berserker",
                $"{upgrade.Name}UpgradeScaling");
            if (!values.ContainsKey(berserkerDefinition))
            {
                values[berserkerDefinition] = levels.Length == 0
                    ? string.Empty
                    : ScalingExpression(
                        healthThresholds,
                        levels,
                        includeExplicitZero);
                migrated++;
            }
        }
        return migrated;
    }

    private static int[] LegacyInfluencerLevels(
        IDictionary<ConfigDefinition, string> values,
        string upgradeName)
    {
        int[] defaults = LegacyInfluencerDefaults(upgradeName);
        if (defaults.Length == 0)
        {
            return defaults;
        }
        int[] result = new int[defaults.Length];
        for (int index = 0; index < result.Length; index++)
        {
            result[index] = ReadInt(
                values,
                "Influencer",
                $"Tier{index + 1}{upgradeName}UpgradeLevels",
                defaults[index]);
        }
        return result;
    }

    private static bool[] LegacyExplicitZeroOverrides(
        IDictionary<ConfigDefinition, string> values,
        string upgradeName)
    {
        int[] defaults = LegacyInfluencerDefaults(upgradeName);
        bool[] result = new bool[defaults.Length];
        for (int index = 0; index < result.Length; index++)
        {
            ConfigDefinition definition = new(
                "Influencer",
                $"Tier{index + 1}{upgradeName}UpgradeLevels");
            result[index] = defaults[index] != 0 &&
                values.TryGetValue(definition, out string? serialized) &&
                int.TryParse(
                    serialized,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int value) &&
                value == 0;
        }
        return result;
    }

    private static int[] LegacyInfluencerDefaults(string upgradeName) =>
        upgradeName switch
        {
            "Strength" => new[] { 1, 4, 9, 13, 20 },
            "Speed" => new[] { 1, 2, 3, 3, 3 },
            "Stamina" => new[] { 3, 6, 10, 12, 15 },
            "ExtraJump" => new[] { 0, 1, 2, 4, 5 },
            "TumbleWings" => new[] { 1, 2, 3, 4, 5 },
            "Health" => new[] { 0, 0, 0, 6, 11 },
            _ => Array.Empty<int>()
        };

    private static int ReadInt(
        IDictionary<ConfigDefinition, string> values,
        string section,
        string key,
        int fallback) =>
        values.TryGetValue(new ConfigDefinition(section, key), out string? value) &&
        int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;

    private static string ScalingExpression(
        int[] conditions,
        int[] levels,
        bool[] includeExplicitZero) =>
        string.Join(
            ",",
            conditions.Zip(
                levels,
                (condition, level) => new { condition, level })
                .Select((pair, index) => new
                {
                    pair.condition,
                    pair.level,
                    IncludeZero = index < includeExplicitZero.Length &&
                                  includeExplicitZero[index]
                })
                .Where(pair => pair.level != 0 || pair.IncludeZero)
                .Select(pair =>
                    $"{pair.condition.ToString(CultureInfo.InvariantCulture)}:" +
                    pair.level.ToString(CultureInfo.InvariantCulture)));

    private static int RemoveLegacyDynamicUpgradeSettings(
        IDictionary<ConfigDefinition, string> values)
    {
        int removed = 0;
        string[] oldUpgrades =
        {
            "Strength", "Speed", "Stamina", "ExtraJump", "TumbleWings", "Health"
        };
        for (int tier = 1; tier <= 5; tier++)
        {
            foreach (string upgrade in oldUpgrades)
            {
                removed += RemoveKey(
                    values,
                    "Influencer",
                    $"Tier{tier}{upgrade}UpgradeLevels");
            }
            removed += RemoveKey(
                values,
                "Berserker",
                $"Tier{tier}HealthPercent");
        }
        return removed;
    }

    private static int RemoveSection(
        IDictionary<ConfigDefinition, string> values,
        string section)
    {
        ConfigDefinition[] definitions = values.Keys
            .Where(definition => string.Equals(
                definition.Section,
                section,
                StringComparison.Ordinal))
            .ToArray();
        foreach (ConfigDefinition definition in definitions)
        {
            values.Remove(definition);
        }
        return definitions.Length;
    }

    private static int ReplaceValueIfEqual(
        IDictionary<ConfigDefinition, string> values,
        string section,
        string key,
        string oldValue,
        string newValue)
    {
        ConfigDefinition definition = new(section, key);
        if (!values.TryGetValue(definition, out string? currentValue) ||
            !string.Equals(currentValue, oldValue, StringComparison.Ordinal))
        {
            return 0;
        }

        values[definition] = newValue;
        return 1;
    }

    private static int MigrateBaseUpgradeScaling(
        IDictionary<ConfigDefinition, string> values)
    {
        string[] keys =
        {
            "HealthUpgradeLevels",
            "StaminaUpgradeLevels",
            "ExtraJumpUpgradeLevels",
            "SpeedUpgradeLevels",
            "StrengthUpgradeLevels",
            "RangeUpgradeLevels",
            "LaunchUpgradeLevels",
            "TumbleClimbUpgradeLevels",
            "TumbleWingsUpgradeLevels",
            "CrouchRestUpgradeLevels",
            "MapPlayerCountUpgradeLevels",
            "DeathHeadBatteryUpgradeLevels"
        };
        int migrated = 0;
        foreach (string key in keys)
        {
            ConfigDefinition definition = new("Base Upgrades", key);
            if (!values.TryGetValue(definition, out string? source) ||
                !int.TryParse(
                    source,
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int level))
            {
                continue;
            }
            values[definition] = level > 0
                ? $"1:{level.ToString(CultureInfo.InvariantCulture)}"
                : string.Empty;
            migrated++;
        }
        return migrated;
    }

    private static int MigratePartySizeLimit(
        IDictionary<ConfigDefinition, string> values,
        string key,
        string newDefault)
    {
        ConfigDefinition definition = new("Role Balance", key);
        if (!values.TryGetValue(definition, out string? source) ||
            source.IndexOf(':') >= 0 ||
            !int.TryParse(
                source,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int oldLimit))
        {
            return 0;
        }
        values[definition] = oldLimit == 1
            ? newDefault
            : $"1:{Math.Max(1, oldLimit).ToString(CultureInfo.InvariantCulture)}";
        return 1;
    }

    private static int UpdateDefaultRoleWeights(
        IDictionary<ConfigDefinition, string> values)
    {
        (string Role, string Weight)[] defaults =
        {
            ("Ghost", "80"),
            ("Bomber", "80"),
            ("Rescuer", "80"),
            ("Tuna", "50"),
            ("Musician", "60"),
            ("Hunter", "80"),
            ("Stinker", "80"),
            ("Engineer", "70"),
            ("Electrician", "80"),
            ("Rider", "60"),
            ("Werewolf", "40"),
            ("Bodyguard", "80")
        };
        int migrated = 0;
        foreach ((string role, string weight) in defaults)
        {
            migrated += ReplaceValueIfEqual(
                values,
                role,
                "Weight",
                "100",
                weight);
        }
        return migrated;
    }

    private static int RemoveKey(
        IDictionary<ConfigDefinition, string> values,
        string section,
        string key) =>
        values.Remove(new ConfigDefinition(section, key)) ? 1 : 0;

    private static void WriteValues(
        string path,
        IReadOnlyDictionary<ConfigDefinition, string> values)
    {
        List<string> lines = new()
        {
            $"## RoleShuffle configuration schema {CurrentSchemaVersion}",
            "## Current setting descriptions are regenerated by BepInEx.",
            string.Empty
        };
        foreach (IGrouping<string, KeyValuePair<ConfigDefinition, string>> section in
                 values
                     .OrderBy(pair => pair.Key.Section, StringComparer.Ordinal)
                     .ThenBy(pair => pair.Key.Key, StringComparer.Ordinal)
                     .GroupBy(pair => pair.Key.Section))
        {
            lines.Add($"[{section.Key}]");
            lines.Add(string.Empty);
            foreach (KeyValuePair<ConfigDefinition, string> pair in section)
            {
                lines.Add($"{pair.Key.Key} = {pair.Value}");
            }
            lines.Add(string.Empty);
        }
        File.WriteAllLines(path, lines, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // The original config remains usable even if temporary cleanup fails.
        }
    }
}
