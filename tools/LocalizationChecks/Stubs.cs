namespace REPOJP.StageRoles;
internal enum StageRole
{
    Tank,
    Runner,
    Jumper,
    Lifter,
    Launcher,
    Climber,
    Flyer,
    Tracker,
    Ghost,
    Bomber,
    Medic,
    Phoenix,
    Jobless,
    Rescuer,
    Vampire,
    King,
    Tuna,
    Musician,
    Mage,
    Gambler,
    Hunter,
    Stinker,
    Engineer,
    Trickster,
    Mechanic,
    Electrician,
    Warden,
    Ninja,
    Executioner,
    Rider,
    Influencer,
    Werewolf,
    Berserker,
    Bodyguard,
    Rammer,
    Diver,
    Sniper,
    Imitator,
    Avenger,
    Brawler,
    Superbot = 1001,
    Disaster = 1002
}
internal sealed class Entry<T>(T value) { public T Value { get; set; } = value; }
internal sealed class StageRolesConfig
{
    internal Entry<bool> OverhaulEnabled { get; } = new(true);
    internal Entry<float> TankHealthMultiplier { get; } = new(7.25f);
    internal Entry<float> RunnerSpeedMultiplier { get; } = new(7.25f);
    internal Entry<float> RunnerStaminaMultiplier { get; } = new(7.25f);
    internal Entry<float> LifterStrengthMultiplier { get; } = new(7.25f);
    internal Entry<int> TankMaximumHealth { get; } = new(17);
    internal Entry<int> RunnerMaximumSpeed { get; } = new(17);
    internal Entry<int> RunnerMaximumStamina { get; } = new(17);
    internal Entry<int> LifterMaximumStrength { get; } = new(17);
    internal Entry<float> JoblessContractDistance { get; } = new(7.25f);
    internal Entry<float> JoblessContractGrace { get; } = new(7.25f);
    internal Entry<int> JoblessContractLimit { get; } = new(17);
    internal Entry<int> JoblessContractHeal { get; } = new(17);
    internal Entry<int> KingHealAmount { get; } = new(17);
    internal Entry<float> KingHealInterval { get; } = new(7.25f);
    internal Entry<float> KingHealRadius { get; } = new(7.25f);
    internal Entry<int> KingHealLimit { get; } = new(17);
    internal Entry<float> AvengerDamageMultiplier { get; } = new(7.25f);
    internal Entry<float> AvengerDurationSeconds { get; } = new(7.25f);
    internal Entry<float> AvengerTriggerRadius { get; } = new(7.25f);
    internal Entry<float> BodyguardDamageSharePercent { get; } = new(7.25f);
    internal Entry<int> BodyguardHealthLevels { get; } = new(17);
    internal Entry<float> BodyguardRadius { get; } = new(7.25f);
    internal Entry<float> BomberDistance { get; } = new(7.25f);
    internal Entry<int> BomberMaximumActiveGrenades { get; } = new(17);
    internal Entry<float> BrawlerMeleeDamageMultiplier { get; } = new(7.25f);
    internal Entry<float> BrawlerRangedDamageMultiplier { get; } = new(7.25f);
    internal Entry<int> ClimberClimbLevels { get; } = new(17);
    internal Entry<int> ClimberRangeLevels { get; } = new(17);
    internal Entry<float> DiverUnderfloorDurationSeconds { get; } = new(7.25f);
    internal Entry<float> ElectricianChargePercentPerSecond { get; } = new(7.25f);
    internal Entry<float> ElectricianMaximumChargePercentPerStage { get; } = new(7.25f);
    internal Entry<float> ExecutionerStunnedDamageMultiplier { get; } = new(7.25f);
    internal Entry<int> FlyerWingsLevels { get; } = new(17);
    internal Entry<int> GamblerGambitGreenHealAmount { get; } = new(17);
    internal Entry<int> GamblerGambitRedDamage { get; } = new(17);
    internal Entry<int> GamblerGambitWhiteHealthUpgradeLevels { get; } = new(17);
    internal Entry<int> GamblerWinChancePercent { get; } = new(17);
    internal Entry<float> GamblerWinValueMultiplier { get; } = new(7.25f);
    internal Entry<int> GhostDeathHeadBatteryLevels { get; } = new(17);
    internal Entry<int> HunterBatteryConsumptionPercent { get; } = new(17);
    internal Entry<float> HunterDoubleOrbChancePercent { get; } = new(7.25f);
    internal Entry<float> HunterJackpotOrbChancePercent { get; } = new(7.25f);
    internal Entry<int> HunterJackpotOrbCount { get; } = new(17);
    internal Entry<float> InfluencerNoiseRadiusMultiplier { get; } = new(7.25f);
    internal Entry<float> InfluencerRadius { get; } = new(7.25f);
    internal Entry<int> JoblessDamage { get; } = new(17);
    internal Entry<float> JoblessDamageIntervalSeconds { get; } = new(7.25f);
    internal Entry<int> JumperExtraJumpLevels { get; } = new(17);
    internal Entry<int> LauncherLaunchLevels { get; } = new(17);
    internal Entry<int> LifterStrengthLevels { get; } = new(17);
    internal Entry<int> MageAutoRecoveryAmount { get; } = new(17);
    internal Entry<float> MageAutoRecoveryDelaySeconds { get; } = new(7.25f);
    internal Entry<bool> MageAutoRecoveryEnabled { get; } = new(true);
    internal Entry<float> MageAutoRecoveryIntervalSeconds { get; } = new(7.25f);
    internal Entry<int> MageAutoRecoveryTotalHealingLimit { get; } = new(17);
    internal Entry<float> MageCastIntervalSeconds { get; } = new(7.25f);
    internal Entry<string> MageGravityExpression { get; } = new("Expression_token");
    internal Entry<int> MageGravityHealthCost { get; } = new(17);
    internal Entry<string> MageLaserExpression { get; } = new("Expression_token");
    internal Entry<int> MageLaserHealthCost { get; } = new(17);
    internal Entry<string> MageRollExpression { get; } = new("Expression_token");
    internal Entry<int> MageRollHealthCost { get; } = new(17);
    internal Entry<string> MageStarExpression { get; } = new("Expression_token");
    internal Entry<int> MageStarHealthCost { get; } = new(17);
    internal Entry<string> MageVoidExpression { get; } = new("Expression_token");
    internal Entry<int> MageVoidHealthCost { get; } = new(17);
    internal Entry<float> MechanicMaximumRepairPercentPerStage { get; } = new(7.25f);
    internal Entry<float> MechanicRepairPercentPerSecond { get; } = new(7.25f);
    internal Entry<int> MedicHealAmount { get; } = new(17);
    internal Entry<float> MedicHealIntervalSeconds { get; } = new(7.25f);
    internal Entry<float> MedicHealRadius { get; } = new(7.25f);
    internal Entry<int> MedicTotalHealingLimit { get; } = new(17);
    internal Entry<int> MusicianHealAmount { get; } = new(17);
    internal Entry<float> MusicianHealRadius { get; } = new(7.25f);
    internal Entry<float> NinjaVisionRecognitionMultiplier { get; } = new(7.25f);
    internal Entry<int> PhoenixRevivalHealth { get; } = new(17);
    internal Entry<int> RammerSelfDamage { get; } = new(17);
    internal Entry<int> RammerTumbleDamage { get; } = new(17);
    internal Entry<int> RescuerMaximumRevives { get; } = new(17);
    internal Entry<float> RescuerRadius { get; } = new(7.25f);
    internal Entry<int> RescuerRevivalHealth { get; } = new(17);
    internal Entry<float> RiderEnemyDamageMultiplier { get; } = new(7.25f);
    internal Entry<float> RiderPlayerKnockbackMultiplier { get; } = new(7.25f);
    internal Entry<int> RunnerSpeedLevels { get; } = new(17);
    internal Entry<int> RunnerStaminaLevels { get; } = new(17);
    internal Entry<float> SniperMaximumDamageMultiplier { get; } = new(7.25f);
    internal Entry<float> SniperMaximumMultiplierDistance { get; } = new(7.25f);
    internal Entry<float> SniperMinimumDamageMultiplier { get; } = new(7.25f);
    internal Entry<float> SniperReferenceDistance { get; } = new(7.25f);
    internal Entry<float> StinkerDistance { get; } = new(7.25f);
    internal Entry<float> StinkerSafetyDistance { get; } = new(7.25f);
    internal Entry<int> TankHealthLevels { get; } = new(17);
    internal Entry<int> TrackerHealthLevels { get; } = new(17);
    internal Entry<float> TricksterActiveSeconds { get; } = new(7.25f);
    internal Entry<float> TricksterCooldownSeconds { get; } = new(7.25f);
    internal Entry<string> TricksterExpression { get; } = new("Expression_token");
    internal Entry<float> TricksterInvestigateRadius { get; } = new(7.25f);
    internal Entry<float> TricksterNoTargetCooldownSeconds { get; } = new(7.25f);
    internal Entry<int> TunaDamage { get; } = new(17);
    internal Entry<float> TunaDamageIntervalSeconds { get; } = new(7.25f);
    internal Entry<float> TunaStationaryDelaySeconds { get; } = new(7.25f);
    internal Entry<float> VampireRadius { get; } = new(7.25f);
    internal Entry<int> VampireTier1HealAmount { get; } = new(17);
    internal Entry<int> VampireTier2HealAmount { get; } = new(17);
    internal Entry<int> VampireTier3HealAmount { get; } = new(17);
    internal Entry<float> WardenAdditionalStunSeconds { get; } = new(7.25f);
    internal Entry<float> WerewolfPlayerDamageMultiplier { get; } = new(7.25f);
}
