using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleOverhaulDescriptions
{
    internal const string Growth = "More maximum HP helps you survive hits.\nGuarantees at least {0} HP and aims for {1} times your HP without the role. Multiplier growth stops at {2} HP; higher Base HP is kept.";
    internal const string RunnerGrowth = "Run faster with more stamina.\nAims for {0} times your sprint speed and {1} times your stamina without the role, within the growth limits. Guarantees at least Speed level {2} and Stamina level {3}; higher Base upgrades are kept.";
    internal const string LifterFixed = "Heavy objects are easier to lift and turn.\nFor heavy objects, lifting and turning strength are fixed at their highest values across Strength levels 0-{0}, regardless of Base upgrades. Light objects and shop items, including guns and melee weapons, use level {1} handling to reduce shaking while held.";
    internal const string Contract = "Deliver valuables to heal yourself and temporarily stop Courier's automatic HP loss.\n\nHow to deliver\n1. Hold a valuable that still has value and carry it at least {0} m outside the truck and extraction points.\n2. Place that valuable in the truck or an extraction point.\nLetting go before delivery resets your carried distance.\n\nReward per delivery (HP restored / automatic HP-loss pause)\nTiny: {2} HP / {3}s\nSmall: {4} HP / {5}s\nMedium: {6} HP / {7}s\nBig: {8} HP / {9}s\nWide: {10} HP / {11}s\nTall: {12} HP / {13}s\nVeryTall: {14} HP / {15}s\n\nYou also start each stage with a {1}-second pause. When it ends, you lose {16} HP every {17} seconds outside the truck; this can kill you. Enemy attacks, falls and other damage still hurt you.\n\nDeliveries are unlimited. Each player can earn the reward from each valuable once per stage. Healing stops at maximum HP. Pause times do not add together: keep the longer of your remaining time and the delivery reward.";
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
            StageRole.Lifter => Format(LifterFixed, RoleOverhaulRules.LifterStrengthLevel, RoleOverhaulRules.LifterReferenceLevel),
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
