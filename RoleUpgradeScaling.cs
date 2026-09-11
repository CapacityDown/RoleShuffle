using System;
using System.Collections.Generic;
using System.Globalization;

namespace REPOJP.StageRoles;

internal sealed class DynamicUpgradeDefinition
{
    internal DynamicUpgradeDefinition(
        string name,
        string commandName,
        string dictionaryName,
        int maximumLevel)
    {
        Name = name;
        CommandName = commandName;
        DictionaryName = dictionaryName;
        MaximumLevel = maximumLevel;
    }

    internal string Name { get; }
    internal string CommandName { get; }
    internal string DictionaryName { get; }
    internal int MaximumLevel { get; }
}

internal readonly struct UpgradeScalingRule
{
    internal UpgradeScalingRule(int condition, int level)
    {
        Condition = condition;
        Level = level;
    }

    internal int Condition { get; }
    internal int Level { get; }
}

internal static class RoleUpgradeScaling
{
    internal const int MaximumUpgradeLevel = 200;
    internal static readonly IReadOnlyList<DynamicUpgradeDefinition> Definitions =
        new[]
        {
            new DynamicUpgradeDefinition("Health", "Health", "playerUpgradeHealth", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("Stamina", "Stamina", "playerUpgradeStamina", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("ExtraJump", "ExtraJump", "playerUpgradeExtraJump", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("Speed", "Speed", "playerUpgradeSpeed", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("Strength", "Strength", "playerUpgradeStrength", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("Range", "Range", "playerUpgradeRange", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("Launch", "Launch", "playerUpgradeLaunch", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("TumbleClimb", "TumbleClimb", "playerUpgradeTumbleClimb", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("TumbleWings", "TumbleWings", "playerUpgradeTumbleWings", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("CrouchRest", "CrouchRest", "playerUpgradeCrouchRest", MaximumUpgradeLevel),
            new DynamicUpgradeDefinition("MapPlayerCount", "MapPlayerCount", "playerUpgradeMapPlayerCount", 1),
            new DynamicUpgradeDefinition("DeathHeadBattery", "DeathHeadBattery", "playerUpgradeDeathHeadBattery", MaximumUpgradeLevel)
        };

    internal static string InfluencerDefault(string upgradeName) =>
        upgradeName switch
        {
            "Strength" => "1:1,2:4,3:9,4:13,5:20",
            "Speed" => "1:1,2:2,3:3,4:3,5:3",
            "Stamina" => "1:3,2:6,3:10,4:12,5:15",
            "ExtraJump" => "2:1,3:2,4:4,5:5",
            "TumbleWings" => "1:1,2:2,3:3,4:4,5:5",
            "Health" => "4:6,5:11",
            _ => string.Empty
        };

    internal static string BerserkerDefault(string upgradeName) =>
        upgradeName switch
        {
            "Strength" => "80:1,60:4,40:9,20:13,10:20",
            "Speed" => "80:1,60:2,40:3,20:3,10:4",
            "Stamina" => "80:3,60:6,40:10,20:12,10:20",
            "ExtraJump" => "60:1,40:2,20:4,10:5",
            "TumbleWings" => string.Empty,
            _ => string.Empty
        };

    internal static bool TryParse(
        string expression,
        int maximumCondition,
        int maximumLevel,
        out IReadOnlyList<UpgradeScalingRule> rules,
        out string error) =>
        TryParse(
            expression,
            minimumCondition: 0,
            maximumCondition,
            maximumLevel,
            clampOutOfRange: false,
            out rules,
            out error);

    internal static bool TryParse(
        string expression,
        int minimumCondition,
        int maximumCondition,
        int maximumLevel,
        bool clampOutOfRange,
        out IReadOnlyList<UpgradeScalingRule> rules,
        out string error)
    {
        List<UpgradeScalingRule> parsed = new();
        List<string> errors = new();
        Dictionary<int, int> byCondition = new();
        string[] tokens = (expression ?? string.Empty).Split(
            new[] { ',', ';' },
            StringSplitOptions.RemoveEmptyEntries);
        foreach (string sourceToken in tokens)
        {
            string token = sourceToken.Trim();
            int separator = token.IndexOf(':');
            if (separator <= 0 || separator == token.Length - 1 ||
                !int.TryParse(
                    token.Substring(0, separator).Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int condition) ||
                !int.TryParse(
                    token.Substring(separator + 1).Trim(),
                    NumberStyles.Integer,
                    CultureInfo.InvariantCulture,
                    out int level))
            {
                errors.Add(token);
                continue;
            }
            if (condition < minimumCondition || condition > maximumCondition ||
                level < 0 || level > maximumLevel)
            {
                if (!clampOutOfRange)
                {
                    errors.Add(token);
                    continue;
                }
                condition = Math.Max(
                    minimumCondition,
                    Math.Min(maximumCondition, condition));
                level = Math.Max(0, Math.Min(maximumLevel, level));
            }
            byCondition[condition] = level;
        }

        foreach (KeyValuePair<int, int> pair in byCondition)
        {
            parsed.Add(new UpgradeScalingRule(pair.Key, pair.Value));
        }
        parsed.Sort((left, right) => left.Condition.CompareTo(right.Condition));
        rules = parsed;
        error = errors.Count == 0
            ? string.Empty
            : string.Join(", ", errors);
        return errors.Count == 0;
    }
}
