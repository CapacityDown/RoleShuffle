using System;
namespace REPOJP.StageRoles;
internal enum RoleCategory { All, Enhancement, Support, Combat, Special, Danger, Hardship, Secret }
internal enum RoleListScope { All, Enabled, Eligible }
internal static class RoleListFilter
{
    internal static RoleCategory Category(StageRole role) => role switch
    {
        StageRole.Tank or StageRole.Runner or StageRole.Jumper or StageRole.Lifter or StageRole.Launcher or StageRole.Climber or StageRole.Flyer or StageRole.Tracker or StageRole.Ghost or StageRole.Ninja or StageRole.Berserker => RoleCategory.Enhancement,
        StageRole.Bomber or StageRole.Stinker or StageRole.Werewolf or StageRole.Influenza => RoleCategory.Danger,
        StageRole.Medic or StageRole.Rescuer or StageRole.Musician or StageRole.Engineer or StageRole.Mechanic or StageRole.Electrician or StageRole.Warden or StageRole.Bodyguard or StageRole.Porter or StageRole.Rigger or StageRole.Negotiator => RoleCategory.Support,
        StageRole.Phoenix or StageRole.Vampire or StageRole.King or StageRole.Gambler or StageRole.Trickster or StageRole.Influencer or StageRole.Diver or StageRole.Imitator or StageRole.Avenger or StageRole.Twins => RoleCategory.Special,
        StageRole.Jobless or StageRole.Tuna => RoleCategory.Hardship,
        StageRole.Mage or StageRole.Hunter or StageRole.Executioner or StageRole.Rider or StageRole.Rammer or StageRole.Sniper or StageRole.Brawler or StageRole.DualWielder => RoleCategory.Combat,
        _ => RoleCategory.Secret
    };
    internal static bool Matches(StageRole role, string query, RoleCategory category, RoleListScope scope,
        bool enabled, DrawStatus eligibility, bool revealed = false)
    {
        string name = revealed ? RoleCatalog.AssignmentName(role) : RoleCatalog.DisplayName(role);
        return name.IndexOf(query.Trim(), StringComparison.OrdinalIgnoreCase) >= 0 &&
            (category == RoleCategory.All || category == Category(role)) &&
            (scope != RoleListScope.Enabled || enabled) &&
            (scope != RoleListScope.Eligible || eligibility == DrawStatus.Eligible || eligibility == DrawStatus.Conditional);
    }
}
