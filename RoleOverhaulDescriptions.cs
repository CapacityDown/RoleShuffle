using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulDescriptions
{
    internal const string Growth = "Health minimum level {0}. Target Base maximum HP x{1}, growth cap {2} HP. Round up to an upgrade level; preserve Base and the minimum.";
    internal const string RunnerGrowth = "Minimum levels: Speed {0}, Stamina {1}. Target Base sprint speed x{2} (cap {3}) and stamina capacity x{4} (cap {5}). Round up to upgrade levels; preserve Base and minimums.";
    internal const string LifterFixed = "Displays Strength level {0}. Normal grip and rotation coefficients are fixed at {1} times their level-{2} values, independent of Base. Temporary overrides still apply. Base levels 0-200 remain eligible.";
    internal const string Contract = "Carry a different valuable {0} m outside the truck, then bring it into the truck. Up to {1} contracts per stage. Each restores {2} HP and pauses attrition for {3} seconds, also granted at stage start. Otherwise, lose {4} HP every {5} seconds outside the truck.";
    internal const string Aura = "Receives the Crown. Living allies within {0} m gain Speed +{1}, Range +{2}, and up to Strength +{3}, capped at level 200. Strength must not weaken grip or rotation. Only King bonuses are removed outside the aura. Excludes Kings; does not stack.";
    internal const string DrawLimit = " Excluded from random assignment if any corresponding Base effective value is at or above its cap.";

    internal static string? For(StageRole role, StageRolesConfig config, RoleGuideLanguage language)
    {
        if (!config.OverhaulEnabled.Value) return null;
        string Format(string text, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, RoleText.Get(text, language), args);
        string? description = role switch
        {
            StageRole.Tank => Format(Growth, config.TankHealthLevels.Value, config.TankHealthMultiplier.Value, config.TankMaximumHealth.Value),
            StageRole.Runner => Format(RunnerGrowth, config.RunnerSpeedLevels.Value, config.RunnerStaminaLevels.Value,
                config.RunnerSpeedMultiplier.Value, config.RunnerMaximumSpeed.Value, config.RunnerStaminaMultiplier.Value, config.RunnerMaximumStamina.Value),
            StageRole.Lifter => Format(LifterFixed, RoleOverhaulRules.LifterStrengthLevel,
                RoleOverhaulRules.LifterEffectiveMultiplier, RoleOverhaulRules.LifterReferenceLevel),
            StageRole.Jobless => Format(Contract, config.JoblessContractDistance.Value, config.JoblessContractLimit.Value,
                config.JoblessContractHeal.Value, config.JoblessContractGrace.Value, config.JoblessDamage.Value, config.JoblessDamageIntervalSeconds.Value),
            StageRole.King => Format(Aura, config.KingUpgradeRadius.Value, config.KingSpeedBonus.Value, config.KingRangeBonus.Value, config.KingStrengthBonus.Value),
            _ => null
        };
        return description != null && RoleOverhaulRules.GrowsWithBase(role)
            ? description + RoleText.Get(DrawLimit, language)
            : description;
    }
}
