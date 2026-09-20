using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulDescriptions
{
    internal const string Growth = "More maximum HP helps you survive hits.\nGuarantees at least {0} HP and aims for {1} times your HP without the role. Multiplier growth stops at {2} HP; higher Base HP is kept.";
    internal const string RunnerGrowth = "Run faster with more stamina.\nAims for {0} times your sprint speed and {1} times your stamina without the role, within the growth limits. Guarantees at least Speed level {2} and Stamina level {3}; higher Base upgrades are kept.";
    internal const string LifterFixed = "Heavy objects are easier to lift and turn.\nGrip strength stays at {0} times Strength level {1}, regardless of your Base upgrades.";
    internal const string Contract = "Deliver valuables to restore HP and pause HP loss.\nCarry a different valuable at least {0} m outside delivery areas, then place it in the truck or an extraction point. Deliveries are unlimited; each valuable rewards you once per stage.\nRecovery / exemption by size: Tiny {2} HP / {3}s; Small {4} HP / {5}s; Medium {6} HP / {7}s; Big {8} HP / {9}s; Wide {10} HP / {11}s; Tall {12} HP / {13}s; VeryTall {14} HP / {15}s. Recovery is capped at maximum HP. A shorter reward never reduces your remaining exemption.\nStage start grants {1} seconds of exemption. Otherwise, lose {16} HP every {17} seconds outside the truck; this can kill you.";
    internal const string Aura = "Stay near teammates to improve their movement and grabbing.\nAllies within {0} m gain Speed +{1}, Range +{2}, and up to Strength +{3} levels. The bonuses end when they leave your range. You and other Kings do not receive them; multiple Kings do not stack. You also receive a crown.";

    internal static string? For(StageRole role, StageRolesConfig config, RoleGuideLanguage language)
    {
        string Format(string text, params object[] args) =>
            string.Format(CultureInfo.InvariantCulture, RoleText.Get(text, language), args);
        return role switch
        {
            StageRole.Tank => Format(Growth, RoleOverhaulRules.UpgradeValue("Health", config.TankHealthLevels.Value),
                config.TankHealthMultiplier.Value, config.TankMaximumHealth.Value),
            StageRole.Runner => Format(RunnerGrowth, config.RunnerSpeedMultiplier.Value, config.RunnerStaminaMultiplier.Value,
                config.RunnerSpeedLevels.Value, config.RunnerStaminaLevels.Value),
            StageRole.Lifter => Format(LifterFixed, RoleOverhaulRules.LifterEffectiveMultiplier, RoleOverhaulRules.LifterReferenceLevel),
            StageRole.Jobless => Format(Contract, config.JoblessContractDistance.Value, config.JoblessInitialGrace.Value,
                config.JoblessTinyHeal.Value, config.JoblessTinyGrace.Value, config.JoblessSmallHeal.Value, config.JoblessSmallGrace.Value,
                config.JoblessMediumHeal.Value, config.JoblessMediumGrace.Value, config.JoblessBigHeal.Value, config.JoblessBigGrace.Value,
                config.JoblessWideHeal.Value, config.JoblessWideGrace.Value, config.JoblessTallHeal.Value, config.JoblessTallGrace.Value,
                config.JoblessVeryTallHeal.Value, config.JoblessVeryTallGrace.Value, config.JoblessDamage.Value, config.JoblessDamageIntervalSeconds.Value),
            StageRole.King => Format(Aura, config.KingUpgradeRadius.Value, config.KingSpeedBonus.Value, config.KingRangeBonus.Value, config.KingStrengthBonus.Value),
            _ => null
        };
    }
}
