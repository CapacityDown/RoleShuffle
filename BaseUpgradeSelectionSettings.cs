using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx.Configuration;

namespace REPOJP.StageRoles;

internal sealed class BaseUpgradeSelectionSettings(StageRolesConfig settings, ConfigFile file)
{
    internal static int CurrentRunLevel => (int)Math.Max(1L, Math.Min(999999L,
        RunManager.instance != null ? (long)RunManager.instance.levelsCompleted + 1 : 1));

    internal bool CanAdjustLevel(string name, int delta, int runLevel) =>
        RoleSelectionSettings.CanEdit && TryBuildLevelAdjustment(name, delta, runLevel, out _, out _);

    internal bool TryAdjustLevel(string name, int delta, int runLevel)
    {
        if (!RoleSelectionSettings.CanEdit ||
            !TryBuildLevelAdjustment(name, delta, runLevel, out var entry, out string expression)) return false;
        RoleSelectionSettings.SaveEntries(file, new Dictionary<ConfigEntry<string>, string> { [entry!] = expression });
        return true;
    }

    private bool TryBuildLevelAdjustment(string name, int delta, int runLevel,
        out ConfigEntry<string>? entry, out string expression)
    {
        entry = settings.BaseUpgradeLevelsEntry(name);
        expression = string.Empty;
        if (entry == null || delta is not (-1 or 1) || runLevel != CurrentRunLevel) return false;
        int maximum = name == "MapPlayerCount" ? 1 : RoleUpgradeScaling.MaximumUpgradeLevel;
        // Refuse malformed expressions instead of discarding user-authored rules.
        if (!RoleUpgradeScaling.TryParse(entry.Value, 1, 999999, maximum, true, out var rules, out _)) return false;
        int current = 0;
        SortedDictionary<int, int> levels = new();
        foreach (UpgradeScalingRule rule in rules)
        {
            levels[rule.Condition] = rule.Level;
            if (rule.Condition <= runLevel) current = rule.Level;
        }
        int next = current + delta;
        if (next < 0 || next > maximum) return false;
        // Add/replace only this run-level threshold. Earlier and later rules
        // retain their behavior, including the next explicitly configured level.
        levels[runLevel] = next;
        List<string> pairs = new(levels.Count);
        foreach (var pair in levels)
            pairs.Add(pair.Key.ToString(CultureInfo.InvariantCulture) + ":" + pair.Value.ToString(CultureInfo.InvariantCulture));
        expression = string.Join(",", pairs);
        return true;
    }

    internal bool TrySetEnabled(string name, bool enabled)
    {
        if (!RoleSelectionSettings.CanEdit || settings.BaseUpgradeDrawEnabledEntry(name) is not { } entry) return false;
        RoleSelectionSettings.SaveEntries(file, new Dictionary<ConfigEntry<bool>, bool> { [entry] = enabled });
        return true;
    }
    internal bool TrySetDrawEnabled(bool enabled)
    {
        if (!RoleSelectionSettings.CanEdit) return false;
        RoleSelectionSettings.SaveEntries(file, new Dictionary<ConfigEntry<bool>, bool> { [settings.TruckUpgradeDrawEnabled] = enabled });
        return true;
    }
    internal bool TryApplyPreset(BaseUpgradePreset id)
    {
        if (!RoleSelectionSettings.CanEdit || BaseUpgradePresets.Find(id) is not { } preset) return false;
        Dictionary<ConfigEntry<bool>, bool> entries = new();
        foreach (string name in BaseUpgradePresets.UpgradeNames)
            entries[settings.BaseUpgradeDrawEnabledEntry(name)!] = preset.Includes(name);
        RoleSelectionSettings.SaveEntries(file, entries);
        return true;
    }
}
