using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulDescriptions
{
    internal const string Growth = "More maximum HP helps you survive hits.\nGuarantees at least {0} HP and aims for {1} times your HP without the role. Multiplier growth stops at {2} HP; higher Base HP is kept.";
    internal const string RunnerGrowth = "Run faster with more stamina.\nAims for {0} times your sprint speed and {1} times your stamina without the role, within the growth limits. Guarantees at least Speed level {2} and Stamina level {3}; higher Base upgrades are kept.";
    internal const string LifterFixed = "Heavy objects are easier to lift and turn.\nGrip strength stays at {0} times Strength level {1}, regardless of your Base upgrades.";
    internal const string Contract = "Complete hauling jobs to stop losing HP outside the truck.\nCarry a valuable at least {0} m outside, then enter the truck while holding it. Use a different valuable for each job.\nEach job restores {2} HP and pauses HP loss for {3} seconds, up to {1} jobs per stage. You also get this grace period at stage start. Outside the grace period, lose {4} HP every {5} seconds while outside the truck; this can kill you.";
    internal const string Aura = "Stay near teammates to improve their movement and grabbing.\nAllies within {0} m gain Speed +{1}, Range +{2}, and up to Strength +{3} levels. The bonuses end when they leave your range. You and other Kings do not receive them; multiple Kings do not stack. You also receive a crown.";

    internal static string? For(StageRole role, StageRolesConfig config, RoleGuideLanguage language)
    {
        if (!config.OverhaulEnabled.Value) return null;
        string Format(string text, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, RoleText.Get(text, language), args);
        return role switch
        {
            StageRole.Tank => Format(Growth, RoleOverhaulRules.UpgradeValue("Health", config.TankHealthLevels.Value),
                config.TankHealthMultiplier.Value, config.TankMaximumHealth.Value),
            StageRole.Runner => Format(RunnerGrowth, config.RunnerSpeedMultiplier.Value, config.RunnerStaminaMultiplier.Value,
                config.RunnerSpeedLevels.Value, config.RunnerStaminaLevels.Value),
            StageRole.Lifter => Format(LifterFixed, RoleOverhaulRules.LifterEffectiveMultiplier, RoleOverhaulRules.LifterReferenceLevel),
            StageRole.Jobless => Format(Contract, config.JoblessContractDistance.Value, config.JoblessContractLimit.Value,
                config.JoblessContractHeal.Value, config.JoblessContractGrace.Value, config.JoblessDamage.Value, config.JoblessDamageIntervalSeconds.Value),
            StageRole.King => Format(Aura, config.KingUpgradeRadius.Value, config.KingSpeedBonus.Value, config.KingRangeBonus.Value, config.KingStrengthBonus.Value),
            _ => null
        };
    }
}
