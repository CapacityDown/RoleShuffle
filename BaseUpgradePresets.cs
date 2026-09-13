using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal enum BaseUpgradePreset { Standard, Survival, Exploration, Hauling, Cooperative }

internal sealed class BaseUpgradePresetDefinition(BaseUpgradePreset id, string name, string description, params string[] upgrades)
{
    private readonly HashSet<string> _upgrades = new(upgrades, StringComparer.Ordinal);
    internal BaseUpgradePreset Id { get; } = id;
    internal string Name { get; } = name;
    internal string Description { get; } = description;
    internal int Count => _upgrades.Count;
    internal bool Includes(string name) => _upgrades.Contains(name);
}

internal static class BaseUpgradePresets
{
    // Stable order is shared by the versioned settings payload.
    internal static readonly IReadOnlyList<string> UpgradeNames = Array.AsReadOnly(new[]
    {
        "Health", "Stamina", "ExtraJump", "Speed", "Strength", "Range", "Launch",
        "TumbleClimb", "TumbleWings", "CrouchRest", "MapPlayerCount", "DeathHeadBattery", "AllUpgrades"
    });
    internal static readonly IReadOnlyList<BaseUpgradePresetDefinition> All = Array.AsReadOnly(new[]
    {
        new BaseUpgradePresetDefinition(BaseUpgradePreset.Standard, "Standard",
            "All upgrade types and All Upgrades are enabled.", new List<string>(UpgradeNames).ToArray()),
        new BaseUpgradePresetDefinition(BaseUpgradePreset.Survival, "Survival",
            "Health, stamina, resting and death-head battery for survival.",
            "Health", "Stamina", "CrouchRest", "DeathHeadBattery"),
        new BaseUpgradePresetDefinition(BaseUpgradePreset.Exploration, "Exploration",
            "Stamina, movement and the player map for exploring.",
            "Stamina", "ExtraJump", "Speed", "TumbleClimb", "TumbleWings", "MapPlayerCount"),
        new BaseUpgradePresetDefinition(BaseUpgradePreset.Hauling, "Hauling",
            "Stamina, strength, range and launch for moving valuables.",
            "Stamina", "Strength", "Range", "Launch"),
        new BaseUpgradePresetDefinition(BaseUpgradePreset.Cooperative, "Cooperative",
            "Health, stamina, carrying and team tracking for cooperation.",
            "Health", "Stamina", "Strength", "Range", "MapPlayerCount", "DeathHeadBattery")
    });

    internal static BaseUpgradePresetDefinition? Find(BaseUpgradePreset id)
    {
        foreach (var preset in All) if (preset.Id == id) return preset;
        return null;
    }
    internal static string MatchingName(Func<string, bool> enabled)
    {
        foreach (var preset in All)
        {
            bool matches = true;
            foreach (string name in UpgradeNames)
                if (enabled(name) != preset.Includes(name)) { matches = false; break; }
            if (matches) return preset.Name;
        }
        return "Custom";
    }
}
