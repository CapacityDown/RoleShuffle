using System;
using System.Globalization;

namespace REPOJP.StageRoles;

internal static class RoleGuideCatalog
{
    internal const string InfluenzaSummary = "After incubation, maximum HP becomes 75. Sneezing and ordinary speech can turn nearby teammates into Influenza.";
    internal const string InfluenzaDescription = "Symptoms begin 30 seconds after assignment or infection and fix maximum HP at 75. Sneezes occur every 30–90 seconds (60 seconds on average): players within 5 m and 20 degrees to either side have a 60% infection chance. Each ordinary voice utterance or chat message gives players within 3 m and 30 degrees to either side a 30% chance. Infection replaces the original role with Influenza; the newly infected can spread it after their own incubation. Death and revival do not reset incubation. Infection ends at the end of the stage.";
    private const string InfluenzaSummaryJapanese = "発症すると最大HPが75に固定されます。くしゃみや通常の会話で、近くの仲間の役職をインフルエンザに変えてしまいます。";
    private const string InfluenzaDescriptionJapanese = "役職の割り当て・感染から30秒後に発症し、最大HPが75に固定されます。くしゃみは30～90秒間隔（平均60秒）で発生。前方の左右20度ずつ・5m以内にいる各プレイヤーへ60％の確率で感染します。通常のVCはひとまとまりの発話ごと、チャットは1投稿ごとに、前方の左右30度ずつ・3m以内の各プレイヤーへ30％の確率で感染します。感染すると元の役職を失い、Influenzaに変更されます。感染した仲間も30秒後の発症から感染を広げます。死亡・蘇生で発症までの時間はリセットされず、ステージ終了で解除されます。";
    internal static string RevealedSecretDescription(StageRole role, RoleGuideLanguage language) =>
        RoleText.Description(RevealedSecretText(role, language), language);

    private static string RevealedSecretText(StageRole role, RoleGuideLanguage language) =>
        role == StageRole.Disaster
            ? language == RoleGuideLanguage.Japanese
                ? "Bomber・Stinker・Tunaの能力と制約を併せ持ちます。移動した場所に起動済みグレネードとウランの雲を発生させ、一定時間動かないと継続ダメージを受けます。移動すると停止時間のカウントとダメージが止まりますが、失ったHPは戻りません。各能力は対応する役職の設定に従います。"
                : "Combines Bomber, Stinker, and Tuna, including their drawbacks. Movement leaves armed grenades and uranium clouds. Remaining still for too long causes repeated damage; moving resets the inactivity timer and stops further damage without restoring lost HP. Each ability follows its corresponding role settings."
            : RevealedSuperbotDescription(language);

    internal static string RevealedSuperbotDescription(
        RoleGuideLanguage language) =>
        RoleText.Description(RevealedSuperbotText(language), language);

    private static string RevealedSuperbotText(RoleGuideLanguage language) =>
        language == RoleGuideLanguage.Japanese
            ? "Influenza、Bomber、Stinker、Werewolf、Courier、Tuna、\nKing、Diver、Imitator、Sniper、Brawlerを除く役職のアップグレードと能力を併せ持ちます。RammerはTumble Attackのダメージだけが適用され、Tumble系アップグレードの0固定は発生しません。Influencerは人数連動強化だけが適用され、物音の増加と定期TTSは発生しません。"
            : "Combines role upgrades and abilities except Influenza, Bomber, Stinker, Werewolf, Courier, Tuna, King, Diver, Imitator, Sniper, and Brawler. Rammer contributes only its Tumble Attack damage without the level-0 Tumble upgrade locks. Only Influencer's nearby-player upgrades apply; louder noises and periodic TTS are excluded.";

    internal static string GenericDescription(
        StageRole role,
        RoleGuideLanguage language) =>
        language == RoleGuideLanguage.Japanese
            ? GenericDescriptionJapanese(role)
            : RoleText.Description(GenericDescription(role), language);

    internal static string GenericDescription(StageRole role) =>
        role switch
        {
            StageRole.Influenza => InfluenzaSummary,
            StageRole.Tank => "Increases maximum health, making the player harder to defeat.",
            StageRole.Runner => "Increases movement speed and stamina for faster, longer sprints.",
            StageRole.Jumper => "Adds extra jumps that can be used before landing.",
            StageRole.Lifter => "Increases grab strength, making heavy objects easier to handle.",
            StageRole.Launcher => "Launches the player farther forward when starting a Tumble.",
            StageRole.Climber => "Improves Tumble climbing and allows objects to be grabbed from farther away.",
            StageRole.Flyer => "Keeps Tumble Wings active longer for extended movement through the air.",
            StageRole.Tracker => "Improves Health and enables Map Player Count so map tools can track another player.",
            StageRole.Ghost => "Extends Death Head battery life.",
            StageRole.Bomber => "Drops random armed grenades while moving. The explosions can harm players and valuables.",
            StageRole.Medic => "Periodically heals nearby living teammates, but never heals the Medic.",
            StageRole.Phoenix => "Automatically revives itself once per stage after dying.",
            StageRole.Jobless => "Deliver valuables to heal yourself and temporarily stop Courier's automatic HP loss outside the truck.",
            StageRole.Rescuer => "Revives a nearby dead teammate by approaching their Death Head.",
            StageRole.Vampire => "Recovers health when an enemy dies nearby. Stronger enemies restore more health.",
            StageRole.King => "Receives the vanilla Crown for the stage. Only one King can be assigned.",
            StageRole.Tuna => "Takes continuous damage after standing still too long. Moving stops the damage.",
            StageRole.Musician => "Heals itself and nearby living players whenever it plays an instrument note.",
            StageRole.Mage => "Uses chat or configured facial expressions to cast five vanilla attacks and recovers health after avoiding damage. Magic cast using a held staff lasts 1.3 times as long.",
            StageRole.Gambler => "Risks a held valuable for a chance to multiply its value; losing destroys it.",
            StageRole.Hunter => "Uses less weapon battery and can gain additional orbs from enemies it kills.",
            StageRole.Stinker => "Leaves harmful uranium clouds behind while moving. The clouds can harm other players.",
            StageRole.Engineer => "Prevents supported effect valuables from activating while holding them.",
            StageRole.Trickster => "Uses chat or a configured facial expression to place a fixed Scream Doll decoy that draws nearby enemies.",
            StageRole.Mechanic => "Repairs damaged valuables while directly holding them.",
            StageRole.Electrician => "Recharges inactive battery-powered items while holding them.",
            StageRole.Warden => "Extends enemy stuns caused by the Warden's attacks.",
            StageRole.Ninja => "Suppresses personal movement and voice noises and takes longer to recognize visually.",
            StageRole.Executioner => "Deals increased direct damage to enemies that were already stunned.",
            StageRole.Rider => "Increases vehicle impact damage against enemies and vehicle knockback against players.",
            StageRole.Influencer => "Gains stronger upgrades near more teammates, but creates louder noise and periodic TTS.",
            StageRole.Werewolf => "Deals increased identifiable player-origin damage to other players.",
            StageRole.Berserker => "Gains stronger upgrades as remaining health becomes lower.",
            StageRole.Bodyguard => "Absorbs part of nearby teammates' enemy damage without being reduced below 1 HP.",
            StageRole.Rammer => "Turns Tumble Attack into a powerful enemy strike while disabling Tumble-related upgrades.",
            StageRole.Diver => "Tumbling while continuing to look down dives through the floor and enables movement below it for a limited time.",
            StageRole.Sniper => "Deals less damage to nearby enemies and progressively more damage as distance increases.",
            StageRole.Imitator => "Copies an eligible teammate's complete role by grabbing their health-transfer point.",
            StageRole.Avenger => "Temporarily deals more damage to enemies when another player dies nearby.",
            StageRole.Brawler => "Deals more damage with melee weapons but less damage with ranged weapons.",
            StageRole.Superbot => "???",
            StageRole.Disaster => "???",
            _ => "No role description is available."
        };

    private static string GenericDescriptionJapanese(StageRole role) =>
        role switch
        {
            StageRole.Influenza => InfluenzaSummaryJapanese,
            StageRole.Rider => "運転中の車両が敵へ与える衝突ダメージと、プレイヤーへのノックバックが増加します。",
            StageRole.Influencer => "近くの仲間が多いほど強化されますが、物音が広がり、定期的にTTSで発言します。",
            StageRole.Werewolf => "自身が攻撃者と特定できる、他のプレイヤーへのダメージが増加します。",
            StageRole.Berserker => "残りHPが少なくなるほど、複数のアップグレードが強化されます。",
            StageRole.Bodyguard => "近くの仲間が敵から受けたダメージの一部を、HPを1残して肩代わりします。",
            StageRole.Rammer => "Tumble系アップグレードを無効化する代わりに、Tumble Attackで敵へ大ダメージを与えます。",
            StageRole.Diver => "下を向いたままタンブルすると床を抜け、制限時間内だけ床下を移動できます。",
            StageRole.Sniper => "近距離の敵には与ダメージが減少し、距離が離れるほど段階なく増加します。",
            StageRole.Imitator => "対象となる仲間へHPを渡すようにつかむと、その役職をステージ中コピーします。",
            StageRole.Avenger => "近くにいる他のプレイヤーが死亡すると、一時的に敵へのダメージが増加します。",
            StageRole.Brawler => "近接武器のダメージが増加する代わりに、遠隔武器のダメージが減少します。",
            StageRole.Tank => "最大HPが増加し、倒されにくくなります。",
            StageRole.Runner => "移動速度とスタミナが増加し、より速く長く走れます。",
            StageRole.Jumper => "着地するまでに使える追加ジャンプが増えます。",
            StageRole.Lifter => "つかむ力が強くなり、重い物を扱いやすくなります。",
            StageRole.Launcher => "Tumble開始時に、より遠くまで前方へ飛び出します。",
            StageRole.Climber => "Tumble中の登攀能力が上がり、遠くの物をつかめるようになります。",
            StageRole.Flyer => "Tumble Wingsの持続時間が延び、より長く空中を移動できます。",
            StageRole.Tracker => "Healthが強化され、Map Player Countが有効になってマップ系アイテムで他のプレイヤーを追跡できます。",
            StageRole.Ghost => "Death Headのバッテリーが長持ちします。",
            StageRole.Bomber => "移動するとランダムな起動済みグレネードを落とします。爆発はプレイヤーやValuableにも危険です。",
            StageRole.Medic => "周囲の生存中の仲間を定期的に回復します。Medic自身は回復しません。",
            StageRole.Phoenix => "死亡すると、ステージ中に一度だけ自動で復活します。",
            StageRole.Jobless => "貴重品の配達でHPを回復し、トラック外で起きるCourierの自動HP減少を一時停止します。",
            StageRole.Rescuer => "死亡した仲間のDeath Headへ近づくと、その仲間を復活させます。",
            StageRole.Vampire => "近くで敵が死亡するとHPを回復します。強い敵ほど回復量が増えます。",
            StageRole.King => "ステージ中、バニラのCrownを受け取ります。Kingは一人だけ割り当てられます。",
            StageRole.Tuna => "長時間静止すると継続的にダメージを受けます。移動するとダメージが止まります。",
            StageRole.Musician => "楽器で音を鳴らすたびに、自分と周囲の生存中のプレイヤーを回復します。",
            StageRole.Mage => "HPを消費して5種類のバニラ攻撃を発動し、しばらくダメージを受けなければHPを自動回復します。保持した杖で発動する魔法の効果時間は1.3倍になります。",
            StageRole.Gambler => "保持したValuableを賭け、成功すると価格を倍増させ、失敗すると破壊します。",
            StageRole.Hunter => "武器のバッテリー消費を抑え、倒した敵から追加のオーブを得ることがあります。",
            StageRole.Stinker => "移動した後に有害なウラン雲を残します。雲は他のプレイヤーにも危険です。",
            StageRole.Engineer => "対応する効果付きValuableを持っている間、その効果の発動を防ぎます。",
            StageRole.Trickster => "周囲の敵を繰り返し引きつける固定式のScream Dollデコイを設置し、有効時間中は再配置できます。",
            StageRole.Mechanic => "破損したValuableを直接持っている間、修復します。",
            StageRole.Electrician => "停止中のバッテリーアイテムを持っている間、充電します。",
            StageRole.Warden => "Wardenの攻撃で発生した敵のスタン時間を延長します。",
            StageRole.Ninja => "自身の移動音と声を抑え、敵から視覚で認識されるまでの時間も延ばします。",
            StageRole.Executioner => "攻撃前からスタンしていた敵への直接ダメージが増加します。",
            StageRole.Superbot => "???",
            StageRole.Disaster => "???",
            _ => "この職業の説明はありません。"
        };

    internal static string Description(
        StageRole role,
        StageRolesConfig config,
        RoleGuideLanguage language) =>
        language == RoleGuideLanguage.Japanese
            ? DescriptionJapanese(role, config)
            : RoleText.Description(Description(role, config), language);

    internal static string Description(StageRole role, StageRolesConfig config) =>
        RoleOverhaulDescriptions.For(role, config, RoleGuideLanguage.English) ?? role switch
        {
            StageRole.Influenza => InfluenzaDescription,
            StageRole.Jumper =>
                $"Adds extra jumps that can be used before landing. Extra Jump is set to level {config.JumperExtraJumpLevels.Value}.",
            StageRole.Launcher =>
                $"Launches the player farther forward when starting a Tumble. Launch is set to level {config.LauncherLaunchLevels.Value}.",
            StageRole.Climber =>
                $"Improves Tumble climbing and allows objects to be grabbed from farther away. " +
                $"Tumble Climb is set to level {config.ClimberClimbLevels.Value} and Range to level {config.ClimberRangeLevels.Value}.",
            StageRole.Flyer =>
                $"Keeps Tumble Wings active longer for extended movement through the air. Tumble Wings is set to level {config.FlyerWingsLevels.Value}.",
            StageRole.Tracker =>
                $"Increases maximum health and allows map tools to track another player. " +
                $"Health is set to level {config.TrackerHealthLevels.Value} and Map Player Count to level 1.",
            StageRole.Ghost =>
                $"Sets Death Head Battery to level {config.GhostDeathHeadBatteryLevels.Value}.",
            StageRole.Bomber => (
                $"Drops one random enabled armed grenade every {Number(config.BomberDistance.Value)} m traveled. " +
                $"Up to {config.BomberMaximumActiveGrenades.Value} generated grenades remain active per Bomber, and the oldest is removed at the limit. " +
                "The explosions can harm players and valuables."),
            StageRole.Medic => (
                $"Heals living teammates within {Number(config.MedicHealRadius.Value)} m for {config.MedicHealAmount.Value} HP every {Number(config.MedicHealIntervalSeconds.Value)} seconds. " +
                $"The Medic cannot heal itself and stops after restoring {config.MedicTotalHealingLimit.Value} total HP during the stage."),
            StageRole.Phoenix =>
                $"Automatically revives itself once per stage after dying, returning with up to {config.PhoenixRevivalHealth.Value} HP.",
            StageRole.Rescuer => (
                $"Approaching within {Number(config.RescuerRadius.Value)} m of a dead teammate's Death Head revives them with up to {config.RescuerRevivalHealth.Value} HP. " +
                $"It can revive {config.RescuerMaximumRevives.Value} times per stage."),
            StageRole.Vampire => (
                $"Recovers health when an enemy dies within {Number(config.VampireRadius.Value)} m. Danger Level 1 restores " +
                $"{config.VampireTier1HealAmount.Value} HP, Level 2 restores {config.VampireTier2HealAmount.Value} HP, and Level 3 restores " +
                $"{config.VampireTier3HealAmount.Value} HP."),
            StageRole.Tuna => (
                $"After a 5-second grace period, standing still for {Number(config.TunaStationaryDelaySeconds.Value)} seconds causes {config.TunaDamage.Value} damage every {Number(config.TunaDamageIntervalSeconds.Value)} seconds. " +
                "Moving resets the timer and stops the damage, but does not restore lost health."),
            StageRole.Musician => (
                $"Each instrument note played by the Musician heals itself and living players within {Number(config.MusicianHealRadius.Value)} m for " +
                $"{config.MusicianHealAmount.Value} HP. A vanilla musical valuable must be played by the Musician."),
            StageRole.Mage => (
                $"Type star, gravity, roll, void, or laser in chat to cast a vanilla attack for {config.MageStarHealthCost.Value}, {config.MageGravityHealthCost.Value}, " +
                $"{config.MageRollHealthCost.Value}, {config.MageVoidHealthCost.Value}, or {config.MageLaserHealthCost.Value} HP. " +
                $"The configured facial expressions also cast them: {config.MageStarExpression.Value}=star, {config.MageGravityExpression.Value}=gravity, " +
                $"{config.MageRollExpression.Value}=roll, {config.MageVoidExpression.Value}=void, and {config.MageLaserExpression.Value}=laser. " +
                $"Spells share a {Number(config.MageCastIntervalSeconds.Value)}-second cooldown and cannot be cast if the HP cost would be fatal. " +
                "Magic cast using a held staff lasts 1.3 times as long; chat and expression casts are unchanged. " +
                (config.MageAutoRecoveryEnabled.Value
                    ? $"After taking no damage for {Number(config.MageAutoRecoveryDelaySeconds.Value)} seconds, restores {config.MageAutoRecoveryAmount.Value} HP every {Number(config.MageAutoRecoveryIntervalSeconds.Value)} seconds, up to {config.MageAutoRecoveryTotalHealingLimit.Value} HP total per stage."
                    : "Automatic health recovery is disabled.")),
            StageRole.Gambler => (
                $"When ready, holding a valuable gives it a {config.GamblerWinChancePercent.Value}% chance to become worth {Number(config.GamblerWinValueMultiplier.Value)}x as much; otherwise it is destroyed. " +
                $"Gambit green heals {config.GamblerGambitGreenHealAmount.Value} HP, red deals {config.GamblerGambitRedDamage.Value} damage, black kills, and white fully heals and adds {config.GamblerGambitWhiteHealthUpgradeLevels.Value} Health levels for the stage."),
            StageRole.Hunter => (
                $"Held or equipped weapons use {config.HunterBatteryConsumptionPercent.Value}% of their normal battery. " +
                $"Enemies killed by Hunter have a {Number(config.HunterDoubleOrbChancePercent.Value)}% chance to drop twice as many orbs and a {Number(config.HunterJackpotOrbChancePercent.Value)}% jackpot chance to drop {config.HunterJackpotOrbCount.Value}."),
            StageRole.Stinker => (
                $"Leaves a harmful uranium cloud every {Number(config.StinkerDistance.Value)} m traveled after moving {Number(config.StinkerSafetyDistance.Value)} m away from it. " +
                "The clouds appear one at a time and can harm other players."),
            StageRole.Engineer =>
                "Prevents supported effect valuables from activating while the Engineer holds them. Camera, Propane Tank, Snowmobile, Flashlight, Clown Doll, and Love Potion are not affected.",
            StageRole.Trickster => (
                $"Type decoy in chat to place a fixed Scream Doll that attracts enemies within {Number(config.TricksterInvestigateRadius.Value)} m for {Number(config.TricksterActiveSeconds.Value)} seconds. " +
                $"Selecting the configured {config.TricksterExpression.Value} facial expression performs the same action. " +
                $"Another decoy cannot be placed while one is active. After it ends, the cooldown is {Number(config.TricksterCooldownSeconds.Value)} seconds, or {Number(config.TricksterNoTargetCooldownSeconds.Value)} seconds if it attracted no enemies. " +
                "The decoy cannot be grabbed, damaged, or delivered."),
            StageRole.Mechanic => (
                $"Repairs directly held damaged valuables by {Number(config.MechanicRepairPercentPerSecond.Value)}% of their original value per second. " +
                $"It can restore up to {Number(config.MechanicMaximumRepairPercentPerStage.Value)} percentage points per stage."),
            StageRole.Electrician => (
                $"Recharges directly held inactive battery items by {Number(config.ElectricianChargePercentPerSecond.Value)}% per second. " +
                $"It can restore up to {Number(config.ElectricianMaximumChargePercentPerStage.Value)} percentage points per stage."),
            StageRole.Warden => (
                $"Extends enemy stuns caused by the Warden's weapons, grenades, or held damaging objects by {Number(config.WardenAdditionalStunSeconds.Value)} seconds. " +
                "The attack must normally be able to stun the enemy."),
            StageRole.Ninja => (
                $"Footsteps, landings, voice chat, and chat TTS do not draw enemy investigation. " +
                $"Enemies take {Number(config.NinjaVisionRecognitionMultiplier.Value)}x as long to recognize Ninja visually, but close contact and other noises can still reveal it."),
            StageRole.Executioner => (
                $"Deals {Number(config.ExecutionerStunnedDamageMultiplier.Value)}x direct attack damage to enemies that were already stunned before the hit. " +
                "The hit that first causes the stun and physics-only collision damage receive no bonus."),
            StageRole.Rider => (
                $"While driving a vanilla vehicle, enemy impact damage is multiplied by {Number(config.RiderEnemyDamageMultiplier.Value)}. " +
                $"Existing vehicle Tumble knockback against players is multiplied by {Number(config.RiderPlayerKnockbackMultiplier.Value)} without increasing player damage."),
            StageRole.Influencer => (
                $"Gains stronger upgrades as more living teammates enter a {Number(config.InfluencerRadius.Value)} m radius. " +
                $"Its noises reach enemies from {Number(config.InfluencerNoiseRadiusMultiplier.Value)}x farther away, and it periodically speaks a random English TTS line."),
            StageRole.Werewolf => (
                $"Damage dealt directly by Werewolf to another player is multiplied by {Number(config.WerewolfPlayerDamageMultiplier.Value)}. " +
                "Self-damage and environmental damage are unchanged."),
            StageRole.Berserker =>
                "Gains stronger upgrades as remaining HP becomes lower, and loses those bonuses again when healed.",
            StageRole.Bodyguard => (
                $"Health is set to level {config.BodyguardHealthLevels.Value}. Within {Number(config.BodyguardRadius.Value)} m, it absorbs {Number(config.BodyguardDamageSharePercent.Value)}% of enemy damage dealt to a teammate. " +
                "The transferred damage cannot reduce Bodyguard below 1 HP."),
            StageRole.Rammer =>
                $"Tumble Attack deals {config.RammerTumbleDamage.Value} damage to enemies and deals {config.RammerSelfDamage.Value} damage to Rammer after a successful hit. Launch, Tumble Climb, and Tumble Wings are fixed at level 0 for the stage regardless of Base Upgrades.",
            StageRole.Diver =>
                $"Tumbling while continuing to look down at a fixed floor starts underfloor movement for up to {Number(config.DiverUnderfloorDurationSeconds.Value)} seconds. Failing to return through a floor before it expires kills Diver. Returning above a floor restores normal movement and starts a cooldown equal to 1.5 times the time spent below the floor.",
            StageRole.Sniper =>
                $"Identifiable attacks against enemies scale continuously with distance: {Number(config.SniperMinimumDamageMultiplier.Value)}x at point-blank range, 1x at {Number(config.SniperReferenceDistance.Value)} m, and up to {Number(config.SniperMaximumDamageMultiplier.Value)}x at {Number(config.SniperMaximumMultiplierDistance.Value)} m. Vehicle impacts and Tumble Attacks are unchanged.",
            StageRole.Imitator =>
                "Starts with only Base Upgrades. Grabbing another player's health-transfer point copies that teammate's upgrades, abilities, and drawbacks for the rest of the stage. Imitator cannot copy Imitator, King, Bomber, Stinker, Werewolf, Courier, Tuna, Influenza, or ???1 or ???2.",
            StageRole.Avenger =>
                $"When another player dies within {Number(config.AvengerTriggerRadius.Value)} m, enemy damage is multiplied by {Number(config.AvengerDamageMultiplier.Value)} for {Number(config.AvengerDurationSeconds.Value)} seconds. Another nearby death refreshes the duration without stacking the multiplier.",
            StageRole.Brawler =>
                $"Identifiable melee weapon damage is multiplied by {Number(config.BrawlerMeleeDamageMultiplier.Value)}, while identifiable gun, staff-projectile, and laser damage is multiplied by {Number(config.BrawlerRangedDamageMultiplier.Value)}. Vehicle impacts, Tumble Attacks, grenades, and ordinary held-object collisions are unchanged.",
            StageRole.Superbot => "???",
            StageRole.Disaster => "???",
            _ => "No role description is available."
        };

    private static string DescriptionJapanese(
        StageRole role,
        StageRolesConfig config) =>
        RoleOverhaulDescriptions.For(role, config, RoleGuideLanguage.Japanese) ?? role switch
        {
            StageRole.Influenza => InfluenzaDescriptionJapanese,
            StageRole.Rider =>
                $"バニラ車両を運転している間、敵への衝突ダメージが{Number(config.RiderEnemyDamageMultiplier.Value)}倍になります。プレイヤーへのダメージは増やさず、元から発生するTumbleノックバックだけを{Number(config.RiderPlayerKnockbackMultiplier.Value)}倍にします。",
            StageRole.Influencer =>
                $"半径{Number(config.InfluencerRadius.Value)}m以内の生存中の仲間が多いほど、複数のアップグレードが強化されます。" +
                $"物音が敵へ届く範囲は{Number(config.InfluencerNoiseRadiusMultiplier.Value)}倍になり、定期的にランダムな英語TTSを発します。",
            StageRole.Werewolf =>
                $"Werewolfが他のプレイヤーへ直接与えるダメージを{Number(config.WerewolfPlayerDamageMultiplier.Value)}倍にします。自傷と環境ダメージは変化しません。",
            StageRole.Berserker =>
                "残りHPが少ないほど複数のアップグレードが強化され、回復すると強化も戻ります。",
            StageRole.Bodyguard =>
                $"Healthがレベル{config.BodyguardHealthLevels.Value}になります。半径{Number(config.BodyguardRadius.Value)}m以内の仲間が敵から受けたダメージの{Number(config.BodyguardDamageSharePercent.Value)}%を肩代わりします。" +
                "肩代わりによってBodyguardのHPが1未満になることはありません。",
            StageRole.Rammer =>
                $"Tumble Attackで敵へ{config.RammerTumbleDamage.Value}ダメージを与え、命中後にRammer自身が{config.RammerSelfDamage.Value}ダメージを受けます。ステージ中はBase Upgradeにかかわらず、Launch、Tumble Climb、Tumble Wingsがレベル0に固定されます。",
            StageRole.Diver =>
                $"固定された床で下を向いたままタンブルすると、最大{Number(config.DiverUnderfloorDurationSeconds.Value)}秒間の床下移動を開始します。時間内に床を抜けて戻れないと死亡します。床上へ戻ると通常移動へ復帰し、直前に床下へ潜っていた時間の1.5倍のクールダウンが始まります。",
            StageRole.Jumper =>
                $"着地するまでに使える追加ジャンプが増えます。Extra Jumpはレベル{config.JumperExtraJumpLevels.Value}になります。",
            StageRole.Launcher =>
                $"Tumble開始時に、より遠くまで前方へ飛び出します。Launchはレベル{config.LauncherLaunchLevels.Value}になります。",
            StageRole.Climber =>
                $"Tumble中の登攀能力が上がり、遠くの物をつかめるようになります。Tumble Climbはレベル{config.ClimberClimbLevels.Value}、Rangeはレベル{config.ClimberRangeLevels.Value}になります。",
            StageRole.Flyer =>
                $"Tumble Wingsの持続時間が延び、より長く空中を移動できます。Tumble Wingsはレベル{config.FlyerWingsLevels.Value}になります。",
            StageRole.Tracker =>
                $"最大HPが増加し、マップ系アイテムで他のプレイヤーを追跡できるようになります。" +
                $"Healthはレベル{config.TrackerHealthLevels.Value}、Map Player Countはレベル1になります。",
            StageRole.Ghost =>
                $"Death Head Batteryがレベル{config.GhostDeathHeadBatteryLevels.Value}になります。",
            StageRole.Bomber =>
                $"{Number(config.BomberDistance.Value)}m移動するごとに、ランダムな起動済みグレネードを一つ落とします。Bomber一人につき最大{config.BomberMaximumActiveGrenades.Value}個まで残り、上限では最も古いものを削除します。" +
                "爆発はプレイヤーやValuableにも危険です。",
            StageRole.Medic =>
                $"半径{Number(config.MedicHealRadius.Value)}m以内の生存中の仲間を、{Number(config.MedicHealIntervalSeconds.Value)}秒ごとに{config.MedicHealAmount.Value}HP回復します。" +
                $"Medic自身は回復せず、ステージ中に合計{config.MedicTotalHealingLimit.Value}HPを回復すると効果が終了します。",
            StageRole.Phoenix =>
                $"死亡すると、ステージ中に一度だけ自動で復活します。復活時のHPは最大{config.PhoenixRevivalHealth.Value}です。",
            StageRole.Rescuer =>
                $"死亡した仲間のDeath Headから{Number(config.RescuerRadius.Value)}m以内へ近づくと、最大{config.RescuerRevivalHealth.Value}HPで復活させます。" +
                $"ステージごとに{config.RescuerMaximumRevives.Value}回まで使用できます。",
            StageRole.Vampire =>
                $"半径{Number(config.VampireRadius.Value)}m以内で敵が死亡するとHPを回復します。Danger Level 1では{config.VampireTier1HealAmount.Value}HP、" +
                $"Level 2では{config.VampireTier2HealAmount.Value}HP、Level 3では{config.VampireTier3HealAmount.Value}HP回復します。",
            StageRole.Tuna =>
                $"ステージ開始から5秒の猶予後、{Number(config.TunaStationaryDelaySeconds.Value)}秒間静止すると、{Number(config.TunaDamageIntervalSeconds.Value)}秒ごとに{config.TunaDamage.Value}ダメージを受けます。" +
                "移動するとタイマーとダメージが止まりますが、失ったHPは戻りません。",
            StageRole.Musician =>
                $"Musicianが楽器で音を鳴らすたびに、自分と半径{Number(config.MusicianHealRadius.Value)}m以内の生存中のプレイヤーを{config.MusicianHealAmount.Value}HP回復します。" +
                "Musician自身がバニラの楽器Valuableを演奏する必要があります。",
            StageRole.Mage =>
                $"チャットでstar、gravity、roll、void、laserと入力し、順に{config.MageStarHealthCost.Value}、{config.MageGravityHealthCost.Value}、{config.MageRollHealthCost.Value}、" +
                $"{config.MageVoidHealthCost.Value}、{config.MageLaserHealthCost.Value}HPを消費してバニラ攻撃を発動します。共通クールダウンは{Number(config.MageCastIntervalSeconds.Value)}秒で、HP不足では発動しません。" +
                "\n設定した表情でも発動できます。" +
                $"\n{config.MageStarExpression.Value}=star、{config.MageRollExpression.Value}=roll、" +
                $"\n{config.MageGravityExpression.Value}=gravity、{config.MageVoidExpression.Value}=void、" +
                $"\n{config.MageLaserExpression.Value}=laserです。" +
                "\n保持した杖で発動する魔法の効果時間は1.3倍になります。チャット・表情での魔法には適用しません。" +
                (config.MageAutoRecoveryEnabled.Value
                    ? $"{Number(config.MageAutoRecoveryDelaySeconds.Value)}秒間ダメージを受けなければ、{Number(config.MageAutoRecoveryIntervalSeconds.Value)}秒ごとに{config.MageAutoRecoveryAmount.Value}HP回復します。自動回復は1ステージの累計{config.MageAutoRecoveryTotalHealingLimit.Value}HPまでです。"
                    : "HPの自動回復は無効です。"),
            StageRole.Gambler =>
                $"効果が使用可能なときにValuableを持つと、{config.GamblerWinChancePercent.Value}%の確率で現在価格が{Number(config.GamblerWinValueMultiplier.Value)}倍になり、失敗すると破壊されます。" +
                $"Gambitは緑で{config.GamblerGambitGreenHealAmount.Value}HP回復、赤で{config.GamblerGambitRedDamage.Value}ダメージ、黒で死亡、白で全回復してステージ中のHealthを{config.GamblerGambitWhiteHealthUpgradeLevels.Value}レベル増やします。",
            StageRole.Hunter =>
                $"保持中または装備中の武器は、バッテリー消費量が通常の{config.HunterBatteryConsumptionPercent.Value}%になります。" +
                $"Hunterが倒した敵は{Number(config.HunterDoubleOrbChancePercent.Value)}%の確率でオーブが2倍になり、{Number(config.HunterJackpotOrbChancePercent.Value)}%の特賞では{config.HunterJackpotOrbCount.Value}個になります。",
            StageRole.Stinker =>
                $"{Number(config.StinkerDistance.Value)}m移動するごとに、発生場所から{Number(config.StinkerSafetyDistance.Value)}m離れた後で有害なウラン雲を残します。" +
                "雲は一つずつ発生し、他のプレイヤーにも危険です。",
            StageRole.Engineer =>
                "対応する効果付きValuableを持っている間、その効果の発動を防ぎます。Camera、Propane Tank、Snowmobile、Flashlight、Clown Doll、Love Potionには効果がありません。",
            StageRole.Trickster =>
                $"チャットでdecoyと入力すると、半径{Number(config.TricksterInvestigateRadius.Value)}m以内の敵を{Number(config.TricksterActiveSeconds.Value)}秒間引きつける固定式Scream Dollを設置します。" +
                $"設定した{config.TricksterExpression.Value}の表情を選択する操作でも発動します。" +
                $"デコイが有効な間は新しいデコイを設置できません。終了後のクールダウンは{Number(config.TricksterCooldownSeconds.Value)}秒で、敵を一体も引きつけなかった場合は{Number(config.TricksterNoTargetCooldownSeconds.Value)}秒です。" +
                "デコイはつかむ、破壊する、納品することができません。",
            StageRole.Mechanic =>
                $"破損したValuableを直接持っている間、元の売却価格に対して毎秒{Number(config.MechanicRepairPercentPerSecond.Value)}%ずつ修復します。" +
                $"ステージごとに最大{Number(config.MechanicMaximumRepairPercentPerStage.Value)}パーセントポイントまで回復できます。",
            StageRole.Electrician =>
                $"停止中の充電可能なアイテムを直接持っている間、毎秒{Number(config.ElectricianChargePercentPerSecond.Value)}%ずつ充電します。" +
                $"ステージごとに最大{Number(config.ElectricianMaximumChargePercentPerStage.Value)}パーセントポイントまで回復できます。",
            StageRole.Warden =>
                $"Wardenの武器、グレネード、または保持中の攻撃物で発生した敵のスタン時間を{Number(config.WardenAdditionalStunSeconds.Value)}秒延長します。" +
                "通常はスタンを発生させない攻撃には効果がありません。",
            StageRole.Ninja =>
                $"足音、着地音、VC、チャットTTSで敵の調査行動を発生させません。敵が視覚でNinjaを認識するまでの時間は{Number(config.NinjaVisionRecognitionMultiplier.Value)}倍になります。" +
                "接近やその他の物音では発見される可能性があります。",
            StageRole.Executioner =>
                $"攻撃前からスタンしていた敵への直接攻撃ダメージが{Number(config.ExecutionerStunnedDamageMultiplier.Value)}倍になります。" +
                "最初にスタンを発生させる攻撃と、物理衝突だけによるダメージには効果がありません。",
            StageRole.Sniper =>
                $"攻撃者を特定できる敵へのダメージが距離に応じて連続変化します。密着時は{Number(config.SniperMinimumDamageMultiplier.Value)}倍、{Number(config.SniperReferenceDistance.Value)}mで1倍、{Number(config.SniperMaximumMultiplierDistance.Value)}mで最大{Number(config.SniperMaximumDamageMultiplier.Value)}倍になります。車両衝突とTumble Attackは変化しません。",
            StageRole.Imitator =>
                "最初はBase Upgradesだけが適用されます。他のプレイヤーへHPを渡すときと同じ位置をつかむと、その仲間のアップグレード、能力、デメリットを残りのステージ中コピーします。\nImitator、King、Bomber、Stinker、Werewolf、\nCourier、Tuna、Influenza、???1、???2はコピーしません。",
            StageRole.Avenger =>
                $"他のプレイヤーが{Number(config.AvengerTriggerRadius.Value)}m以内で死亡すると、{Number(config.AvengerDurationSeconds.Value)}秒間、敵へのダメージが{Number(config.AvengerDamageMultiplier.Value)}倍になります。効果中の再発動では倍率を重複せず、残り時間だけを更新します。",
            StageRole.Brawler =>
                $"攻撃者を特定できる近接武器のダメージが{Number(config.BrawlerMeleeDamageMultiplier.Value)}倍になり、銃、杖の弾、レーザーによるダメージは{Number(config.BrawlerRangedDamageMultiplier.Value)}倍になります。車両衝突、Tumble Attack、グレネード、通常の保持物による衝突は変化しません。",
            StageRole.Superbot => "???",
            StageRole.Disaster => "???",
            _ => "この職業の説明はありません。"
        };

    private static string Number(float value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);
}
