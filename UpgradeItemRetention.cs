using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class UpgradeItemRetention
{
    private const string Prefix = "RoleShuffle.UsedUpgrade.";

    private static string Key(string steamId, string dictionaryName) =>
        Prefix + steamId.Length + ":" + steamId + ":" + dictionaryName;

    internal static int Get(string steamId, string dictionaryName) =>
        StatsManager.instance == null ? 0 : Math.Max(0,
            StatsManager.instance.runStats.GetValueOrDefault(Key(steamId, dictionaryName), 0));

    internal static int Total(StageRolesConfig config, string steamId, string dictionaryName) =>
        !config.KeepUpgradeItems.Value ? 0 : Add(Get(string.Empty, dictionaryName),
            string.IsNullOrEmpty(steamId) ? 0 : Get(steamId, dictionaryName));

    internal static int PendingSharedThrow(string steamId) => Math.Max(0,
        Get(string.Empty, "playerUpgradeThrow") - Get(steamId, "sharedThrowApplied"));

    internal static void MarkSharedThrowApplied(string steamId, int amount)
    {
        if (StatsManager.instance == null || string.IsNullOrEmpty(steamId) || amount <= 0) return;
        StatsManager.instance.runStats[Key(steamId, "sharedThrowApplied")] =
            Add(Get(steamId, "sharedThrowApplied"), amount);
    }

    internal static int Add(int level, int amount) =>
        (int)Math.Min(int.MaxValue, (long)Math.Max(0, level) + Math.Max(0, amount));

    internal static bool IsFixed(StageRole? role, string command) =>
        (role == StageRole.Rammer && command is "Launch" or "TumbleClimb" or "TumbleWings") ||
        (role is StageRole.Lifter or StageRole.Superbot && command == "Strength");

    internal static IReadOnlyList<UpgradeGrant> Apply(IReadOnlyList<UpgradeGrant> targets,
        StageRolesConfig config, string? steamId, StageRole? role = null)
    {
        if (!config.KeepUpgradeItems.Value || string.IsNullOrEmpty(steamId)) return targets;
        List<UpgradeGrant> result = new(targets.Count);
        foreach (UpgradeGrant target in targets)
        {
            int level = IsFixed(role, target.CommandName) ? target.Level :
                Add(target.Level, Total(config, steamId, target.DictionaryName));
            result.Add(new UpgradeGrant(target.CommandName, target.DictionaryName, level));
        }
        return result;
    }

    internal static List<UpgradeGrant> Record(string steamId, bool shared,
        IReadOnlyDictionary<string, int> before, IReadOnlyDictionary<string, int> after)
    {
        List<UpgradeGrant> changes = new();
        if (StatsManager.instance == null || string.IsNullOrEmpty(steamId)) return changes;
        List<DynamicUpgradeDefinition> definitions = new(RoleUpgradeScaling.Definitions)
        {
            new("Throw", "Throw", "playerUpgradeThrow", int.MaxValue)
        };
        foreach (DynamicUpgradeDefinition definition in definitions)
        {
            long delta = (long)after.GetValueOrDefault(definition.DictionaryName, 0) -
                before.GetValueOrDefault(definition.DictionaryName, 0);
            if (delta <= 0) continue;
            int amount = (int)Math.Min(int.MaxValue, delta);
            string owner = shared ? string.Empty : steamId;
            string key = Key(owner, definition.DictionaryName);
            StatsManager.instance.runStats[key] = Add(Get(owner, definition.DictionaryName), amount);
            changes.Add(new UpgradeGrant(definition.CommandName, definition.DictionaryName, amount));
        }
        return changes;
    }
}
