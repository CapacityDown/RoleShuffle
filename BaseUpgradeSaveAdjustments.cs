using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class BaseUpgradeManualStore
{
    private const string KeyPrefix = "RoleShuffle.BaseUpgradeManual.";

    // runStats belongs to the loaded save and is cleared by the game's reset.
    // Do not offer a persistent edit when the native save operation would skip it.
    internal static string SaveIdentity => GameSaveState.CanSave ? GameSaveState.CurrentName : string.Empty;

    internal static int Get(string dictionaryName) => StatsManager.instance == null
        ? 0 : StatsManager.instance.runStats.GetValueOrDefault(KeyPrefix + dictionaryName, 0);

    internal static bool TrySave(string dictionaryName, int value, string saveIdentity)
    {
        if (string.IsNullOrEmpty(saveIdentity) || SaveIdentity != saveIdentity) return false;
        var stats = StatsManager.instance!;
        string key = KeyPrefix + dictionaryName;
        bool existed = stats.runStats.TryGetValue(key, out int previous);
        stats.runStats[key] = value;
        try { stats.SaveFileSave(); }
        catch
        {
            if (existed) stats.runStats[key] = previous;
            else stats.runStats.Remove(key);
            throw;
        }
        return true;
    }

    internal static int EffectiveLevel(string dictionaryName, int configured, int maximum) =>
        (int)Math.Max(0L, Math.Min(maximum,
            (long)configured + Get(dictionaryName) + BaseUpgradeBonusStore.Get(dictionaryName)));
}

internal static class BaseUpgradeBonusStore
{
    private const string KeyPrefix = "RoleShuffle.BaseUpgradeBonus.";

    internal static int Get(string dictionaryName) => StatsManager.instance == null ||
        string.IsNullOrEmpty(dictionaryName) ? 0 :
        StatsManager.instance.runStats.GetValueOrDefault(KeyPrefix + dictionaryName, 0);

    internal static bool ApplyEffectiveDelta(string dictionaryName, int currentLevel,
        int configuredLevel, int delta, int maximumLevel)
    {
        if (StatsManager.instance == null || string.IsNullOrEmpty(dictionaryName) || delta == 0) return false;
        int desired = (int)Math.Max(0L, Math.Min(maximumLevel, (long)currentLevel + delta));
        if (desired == currentLevel) return false;
        long bonus = (long)desired - configuredLevel - BaseUpgradeManualStore.Get(dictionaryName);
        if (bonus < int.MinValue || bonus > int.MaxValue) return false;
        StatsManager.instance.runStats[KeyPrefix + dictionaryName] = (int)bonus;
        return true;
    }
}
