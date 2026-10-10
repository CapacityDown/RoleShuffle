using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

// A reversible deduction from upgrade levels, including temporary aura grants.
// Native item purchases still add their actual level before the next reconciliation.
internal static class PorterSpeed
{
    private sealed class Load
    {
        internal int Removed;
        internal float Weight, Capacity;
    }
    private static readonly Dictionary<string, Load> Loads = new(StringComparer.Ordinal);
    private const string Key = "playerUpgradeSpeed";

    internal static int WithoutPenalty(string id, string key, int level) =>
        key == Key && Loads.TryGetValue(id, out Load? load)
            ? (int)Math.Min(int.MaxValue, (long)Math.Max(0, level) + load.Removed) : level;

    // An absolute role/base assignment replaces both the actual level and deduction.
    internal static void Forget(string id, string key) { if (key == Key) Loads.Remove(id); }

    internal static void Update(string id, float weight, float capacity)
    {
        if (!Loads.TryGetValue(id, out Load? load))
        {
            if (weight <= 0) return;
            Loads[id] = load = new Load();
        }
        load.Weight = weight;
        load.Capacity = capacity;
        if (!UpgradeService.TryGetLevels(id, out var levels)) return;
        int current = levels.GetValueOrDefault(Key, 0);
        Apply(id, load, current, WithoutPenalty(id, Key, current));
    }

    internal static bool TryAdd(string id, string command, int delta, out bool result)
    {
        result = false;
        if (command != "Speed" || !Loads.TryGetValue(id, out Load? load)) return false;
        if (!UpgradeService.TryGetLevels(id, out var levels)) return true;
        int current = levels.GetValueOrDefault(Key, 0);
        int baseline = (int)Math.Clamp((long)WithoutPenalty(id, Key, current) + delta, 0, int.MaxValue);
        result = Apply(id, load, current, baseline);
        return true;
    }

    private static bool Apply(string id, Load load, int current, int baseline)
    {
        int target = PorterRules.SpeedLevel(baseline, load.Weight, load.Capacity);
        bool sent = target == current || UpgradeService.AddLevelsHostAuthoritativeRaw(id, "Speed", target - current);
        // The local upgrade may already have succeeded before a peer send failed.
        // Never double-deduct or restore a grant that was not actually applied.
        if (UpgradeService.TryGetLevels(id, out var after) && after.GetValueOrDefault(Key, 0) == target)
        {
            load.Removed = baseline - target;
            if (load.Weight <= 0 && load.Removed == 0) Loads.Remove(id);
        }
        return sent;
    }
}
