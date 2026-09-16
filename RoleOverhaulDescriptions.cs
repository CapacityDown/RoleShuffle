using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulDescriptions
{
    internal const string Growth = "Target {0}: max({1}, Base + {2}), capped at 200.";
    internal const string RunnerGrowth = "Targets: Speed max({0}, Base + {1}); Stamina max({2}, Base + {3}), capped at 200.";
    internal const string LifterGrowth = "Strength minimum {0}, capped at 200. Above max(Base, minimum), grow toward Base + {1} only if vanilla penalties do not weaken light/heavy object grip or rotation.";
    internal const string Contract = "Carry a different valuable {0} m outside the truck, then bring it into the truck. Up to {1} contracts per stage. Each restores {2} HP and pauses attrition for {3} seconds, also granted at stage start. Otherwise, lose {4} HP every {5} seconds outside the truck.";
    internal const string Aura = "Receives the Crown and heals living teammates within {0} m for {1} HP every {2} seconds, up to {3} HP per stage. Excludes the King. Only one King can be assigned.";

    internal static string? For(StageRole role, StageRolesConfig config, RoleGuideLanguage language)
    {
        if (!config.OverhaulEnabled.Value) return null;
        string Format(string text, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, RoleText.Get(text, language), args);
        return role switch
        {
            StageRole.Tank => Format(Growth, "Health", config.TankHealthLevels.Value, config.TankBaseBonus.Value),
            StageRole.Runner => Format(RunnerGrowth, config.RunnerSpeedLevels.Value, config.RunnerSpeedBaseBonus.Value,
                config.RunnerStaminaLevels.Value, config.RunnerStaminaBaseBonus.Value),
            StageRole.Lifter => Format(LifterGrowth, config.LifterStrengthLevels.Value, config.LifterBaseBonus.Value),
            StageRole.Jobless => Format(Contract, config.JoblessContractDistance.Value, config.JoblessContractLimit.Value,
                config.JoblessContractHeal.Value, config.JoblessContractGrace.Value, config.JoblessDamage.Value, config.JoblessDamageIntervalSeconds.Value),
            StageRole.King => Format(Aura, config.KingHealRadius.Value, config.KingHealAmount.Value, config.KingHealInterval.Value, config.KingHealLimit.Value),
            _ => null
        };
    }
}
