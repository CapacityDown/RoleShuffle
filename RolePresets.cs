using System;
using System.Collections.Generic;

namespace REPOJP.StageRoles;

internal enum RolePreset { Standard, Beginner, Cooperative, Chaos, Challenge }

internal sealed class RolePresetDefinition(RolePreset id, string name, string description, params StageRole[] roles)
{
    private readonly HashSet<StageRole> _roles = new(roles);
    internal RolePreset Id { get; } = id;
    internal string Name { get; } = name;
    internal string Description { get; } = description;
    internal int Count => _roles.Count;
    internal bool Includes(StageRole role) => _roles.Contains(role);
}

internal static class RolePresets
{
    internal static readonly IReadOnlyList<RolePresetDefinition> All = new[]
    {
        new RolePresetDefinition(RolePreset.Standard, "Standard",
            "All roles enabled, including secret roles.", (StageRole[])Enum.GetValues(typeof(StageRole))),
        new RolePresetDefinition(RolePreset.Beginner, "Beginner",
            "Simple upgrades, recovery and protection for learning the game.",
            StageRole.Tank, StageRole.Runner, StageRole.Jumper, StageRole.Lifter, StageRole.Launcher,
            StageRole.Climber, StageRole.Flyer, StageRole.Tracker, StageRole.Ghost, StageRole.Medic,
            StageRole.Phoenix, StageRole.Rescuer, StageRole.Engineer, StageRole.Mechanic,
            StageRole.Electrician, StageRole.Warden, StageRole.Ninja, StageRole.Influencer, StageRole.Bodyguard),
        new RolePresetDefinition(RolePreset.Cooperative, "Cooperative",
            "Team support, healing, repairs and shared survival.",
            StageRole.Tank, StageRole.Runner, StageRole.Lifter, StageRole.Tracker, StageRole.Ghost,
            StageRole.Medic, StageRole.Phoenix, StageRole.Rescuer, StageRole.Musician, StageRole.Engineer,
            StageRole.Trickster, StageRole.Mechanic, StageRole.Electrician, StageRole.Warden,
            StageRole.Influencer, StageRole.Bodyguard),
        new RolePresetDefinition(RolePreset.Chaos, "Chaos",
            "Explosions, magic, gambles and unpredictable roles.",
            StageRole.Bomber, StageRole.King, StageRole.Tuna, StageRole.Mage, StageRole.Gambler,
            StageRole.Stinker, StageRole.Trickster, StageRole.Rider, StageRole.Werewolf, StageRole.Berserker,
            StageRole.Rammer, StageRole.Diver, StageRole.Imitator, StageRole.Disaster),
        new RolePresetDefinition(RolePreset.Challenge, "Challenge",
            "Risky and specialized roles without dedicated healing or revival roles.",
            StageRole.Jobless, StageRole.Tuna, StageRole.Vampire, StageRole.Mage, StageRole.Gambler,
            StageRole.Hunter, StageRole.Executioner, StageRole.Werewolf, StageRole.Berserker,
            StageRole.Rammer, StageRole.Diver, StageRole.Sniper, StageRole.Brawler, StageRole.Avenger)
    };

    internal static RolePresetDefinition? Find(RolePreset id)
    {
        foreach (RolePresetDefinition preset in All)
            if (preset.Id == id) return preset;
        return null;
    }

    internal static string MatchingName(Func<StageRole, bool> enabled)
    {
        foreach (RolePresetDefinition preset in All)
        {
            bool matches = true;
            foreach (StageRole role in Enum.GetValues(typeof(StageRole)))
                if (enabled(role) != preset.Includes(role)) { matches = false; break; }
            if (matches) return preset.Name;
        }
        return "Custom";
    }
}
