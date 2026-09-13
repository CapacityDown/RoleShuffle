using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal static class BaseUpgradeManualStore
{
    private const string KeyPrefix = "RoleShuffle.BaseUpgradeManual.";

    // runStats belongs to the loaded save and is cleared by the game's reset.
    // A reset run can prepare its next native save on the first lobby edit.
    internal static string SaveIdentity => GameSaveState.CanSave
        ? GameSaveState.CurrentName : GameSaveState.PendingNewRunIdentity;

    internal static int Get(string dictionaryName) => StatsManager.instance == null
        ? 0 : StatsManager.instance.runStats.GetValueOrDefault(KeyPrefix + dictionaryName, 0);

    internal static bool TrySave(string dictionaryName, int value, string saveIdentity)
    {
        if (string.IsNullOrEmpty(saveIdentity) || SaveIdentity != saveIdentity) return false;
        if (!GameSaveState.CanSave && !GameSaveState.PrepareNewRun(saveIdentity)) return false;
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

    internal static int Applied(string dictionaryName, bool enabled) => enabled ? Get(dictionaryName) : 0;

    internal static int EffectiveLevel(string dictionaryName, int configured, int maximum, bool manualEnabled) =>
        (int)Math.Max(0L, Math.Min(maximum,
            (long)configured + Applied(dictionaryName, manualEnabled) + BaseUpgradeBonusStore.Get(dictionaryName)));
}

internal static class BaseUpgradeBonusStore
{
    private const string KeyPrefix = "RoleShuffle.BaseUpgradeBonus.";

    internal static int Get(string dictionaryName) => StatsManager.instance == null ||
        string.IsNullOrEmpty(dictionaryName) ? 0 :
        StatsManager.instance.runStats.GetValueOrDefault(KeyPrefix + dictionaryName, 0);

    internal static bool ApplyEffectiveDelta(string dictionaryName, int currentLevel,
        int configuredLevel, int delta, int maximumLevel, bool manualEnabled)
    {
        if (StatsManager.instance == null || string.IsNullOrEmpty(dictionaryName) || delta == 0) return false;
        int desired = (int)Math.Max(0L, Math.Min(maximumLevel, (long)currentLevel + delta));
        if (desired == currentLevel) return false;
        // Disabled manual amounts stay in the save, but must not be compensated
        // for by a draw or they would leak into the independent truck bonus.
        long bonus = (long)desired - configuredLevel - BaseUpgradeManualStore.Applied(dictionaryName, manualEnabled);
        if (bonus < int.MinValue || bonus > int.MaxValue) return false;
        StatsManager.instance.runStats[KeyPrefix + dictionaryName] = (int)bonus;
        return true;
    }
}
