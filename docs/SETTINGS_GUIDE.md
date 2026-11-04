# RoleShuffle Settings Guide / 設定ガイド

[README](../package/README.md)

## English

### Role growth and support settings

|Key|Default|Range|Effect|
|---|---|---|---|
|`Tank.HealthMultiplier` / `Tank.MaximumHealth`|1.5 / 4100|1–10 / 100–4100|Max HP|
|`Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed`|1.5 / 205|1–10 / 5–205|Sprint speed|
|`Runner.StaminaMultiplier` / `Runner.MaximumStamina`|1.5 / 2040|1–10 / 40–2040|Stamina capacity|
|`Courier.ContractDistance`|5|1–50 m|Carry distance outside delivery areas.|
|`Courier.InitialGraceSeconds`|120|0–300 s|HP-loss pause at stage start/revival.|
|`Courier.<Size>GraceSeconds`|`90/120/180/240/240/240/300`|0–300 s|Tiny/Small/Medium/Big/Wide/Tall/VeryTall. Never shortens remaining grace.|
|`Courier.<Size>HealAmount`|`10/25/50/100/100/100/100`|0–10000 HP|Same size order; fixed HP, capped at maximum health.|
|`King.SpeedBonusLevels` / `King.RangeBonusLevels`|2 / 2|0–200|Speed / Range bonus levels.|
|`King.StrengthBonusLevels`|5|0–200|Temporary Strength bonus levels; total cap 200.|
|`King.UpgradeRadius`|12|1–30 m|Upgrade aura radius.|
|`HUD.ResourceHudEnabled`|true|Boolean|Personal resource HUD at the top left, independent of the role list.|
|`HUD.ResourceHudScalePercent`|100|50–200|Resource HUD scale; fits the screen automatically.|
|`HUD.ResourceHudOffsetX` / `ResourceHudOffsetY`|0 / 0|0–3840 / 0–2160|Moves the resource display right or down from below stamina.|

Ability resources appear below stamina in one column and shrink to fit when necessary. The role list shows each player's role. The resource HUD requires the host's RoleShuffle version.

### Ability tuning

Host: REPOConfig → RoleShuffle → role name. Defaults are listed above. Strength, HP and infection checks use current settings. Incubation changes affect new infections; scheduled sneezes keep their deadlines. Stinker delay affects new valuables. Existing settings stay.

|Setting|Default|Range|Effect|
|---|---|---|---|
|`Lifter.StrengthLevel`|200|0–200|Strength upgrade level while the role is active.|
|`Lifter.LightItemStrengthLevel`|1|0–200|Holding level for light objects/shop equipment, excluding carts/bikes.|
|`Stinker.BreakGraceSeconds`|0.5|0–10 s|Delay before a spawned uranium valuable breaks.|
|`Influenza.IncubationSeconds`|30|0–300 s|Delay from infection to symptoms.|
|`Influenza.MaximumHealth`|75|1–10000 HP|Max HP after onset. Increasing it does not heal.|
|`Influenza.SneezeMinimumSeconds` / `Influenza.SneezeMaximumSeconds`|30 / 90|1–600 s|Random interval; endpoints are sorted. Equal values give a regular interval.|
|`Influenza.SneezeRange` / `Influenza.SpeechRange`|5 / 3|0.1–30 m|Infection distance for sneezes / voice and text chat.|
|`Influenza.SneezeAngleDegrees` / `Influenza.SpeechAngleDegrees`|40 / 60|1–360°|Full infection angle. Half applies on each side.|
|`Influenza.SneezeChancePercent` / `Influenza.SpeechChancePercent`|60 / 30|0–100%|Chance per teammate per sneeze / utterance or message.|
|`Influenza.SpeechSilenceSeconds`|0.75|0.1–5 s|Silent gap that starts a new utterance.|
|`Influenza.SneezeNoiseRadius`|5|0–100 m|Enemy hearing distance, modified by enemy hearing. Zero disables the alert.|
|`Twins.CarryStrengthMultiplier`|1.5|1–3|Co-carry Strength target multiplier, granted as temporary upgrades up to level 200.|
|`Twins.CollisionReductionPercent`|50|0–100%|Co-carry collision loss prevented.|
|`Twins.RestRadius` / `RestHoldSeconds`|3 / 3|0.5–10 m / 1–30 s|Rest distance / continuous crouched stillness without damage.|
|`Twins.RestHealPercent` / `RestCooldownSeconds`|10 / 60|0–100% / 1–600 s|Shared max-HP healing / pair cooldown. Interrupted rest costs nothing.|
|`Twins.RendezvousStartRange` / `RendezvousEndRange`|15 / 5|1–100 m / 0.5–100 m|Reunion assist start/end; smaller value is always the end.|
|`Twins.RendezvousSpeedMultiplier`|1.25|1–2|Sprint multiplier toward a distant partner, rounded up to Speed levels.|
|`Twins.DeliveryCarrySeconds` / `DeliveryRadius`|3 / 5|1–30 s / 0.5–20 m|Joint carry time outside delivery areas / both partners’ distance at delivery.|
|`Twins.DeliveryBonusPercent` / `DeliveryStageLimit`|10 / 5000|0–100% / 0–10000|Value bonus / pair’s stage budget; each item pays once.|
|`Porter.Capacity`|15|1–100|Total cargo weight limit; no item-count limit. HUD: weight, current cargo value and unloading time.|
|`Porter.HoldSeconds`|3|1–30 s|Continuous solo hold before storage.|
|`Porter.FullUnloadSeconds`|10|0–60 s|Full-load unloading time; scales with weight. 0 unloads immediately.|
|`Porter.DropOnEnemyHit`|true|Boolean|Enemy HP damage scatters all cargo.|
|`Porter.AllowedSizes`|Tiny,Small,Medium|Comma-separated: Tiny, Small, Medium, Big, Wide, Tall, VeryTall|Allowed sizes; empty/unknown entries allow no additional sizes.|
|`DualWielder.FollowDelaySeconds` / `DualWielder.LeftOffset`|0.3 s / 0.5 m|0.05–2 s / 0.1–1.5 m|Copy delay and offset.|
|`Rigger.EnemyDamageMultiplier` / `Rigger.EnemyHoldDurationMultiplier` / `Rigger.DurabilityConsumptionMultiplier`|2 / 1.5 / 0.5|1–10 / 1–3 / 0.1–1|Enemy hit damage / restraint time / durability cost.|
|`Negotiator.DeliveryCarryDistance`|20|1–200 m|Personal carry distance outside the truck before release at extraction.|
|`Negotiator.DeliveryAllowedSizes`|Big,Wide,Tall,VeryTall|Comma-separated native size names|Allowed delivery sizes; case/spacing ignored. Blank disables delivery earnings; unknown names ignored.|
|`Negotiator.EnemyTier`|3|1–3|Exact native tier for a personal kill; Enhanced reward bonuses do not qualify.|

### Configuration

Edit through REPOConfig. All settings are host-controlled except `HUD.*`, which affects only your display. Grouped keys share the first section.

#### General, notifications, and HUD

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`General.Enabled`|true|On/Off|Enables stage role assignment.|
|`General.UniqueRoles`|true|On/Off|Avoids duplicates until every enabled eligible role has been assigned once.|
|`General.ShopUpgradeItemCount`|0|0–30|Requests this number of upgrade items per shop. 0 removes upgrade items from the shop pool.|
|`Compatibility.EnhancedEnemyRewardsEnabled`|true|On/Off|When enabled, Elite Enemy Variants Enhanced enemies count one reward tier higher for Vampire healing and Hunter bonus-orb quality.|
|`Notifications.Enabled`|true|On/Off|Enables role-name and role-effect chat and TTS notifications.|
|`Notifications.DelaySeconds`|2.5|0–30|Delay before role announcements without Stage Flux.|
|`Notifications.StageFluxDelaySeconds`|5|0–30|Stage Flux announcement delay; also waits for its TTS to end and 1.5 s of silence.|
|`HUD.Enabled`|true|On/Off|Shows the current roles during a stage.|
|`HUD.RoleDisplay`|`NameOnly`|`IconAndName`, `NameOnly`, `IconOnly`|Shows role icons and names, names only, or icons only. Player names remain visible.|
|`HUD.IconSize`|64|32–128|Role icon size before HUD scaling. The HUD grows for six rows and scales to fit the screen.|
|`HUD.Anchor`|`BottomLeft`|`TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight`|Selects the HUD anchor.|
|`HUD.Alignment`|`Left`|`Left`, `Center`, `Right`|Selects the role text alignment.|
|`HUD.OffsetX`|0|`-3840`–3840|Horizontal offset from the anchor in pixels.|
|`HUD.OffsetY`|80|`-2160`–2160|Vertical offset from the anchor in pixels.|
|`HUD.ScalePercent`|70|50–200|HUD display scale percentage.|
|`HUD.PlayersPerPage`|8|2–20|Max visible players including the pinned local player. At 6+, reserves six rows even with tall fonts/icons.|
|`HUD.PageIntervalSeconds`|5|1–30|Seconds each role page remains visible.|
|`HUD.TransitionDurationSeconds`|0.2|0–1|Duration of the fade between role pages.|
|`HUD.PinLocalPlayer`|true|On/Off|Keeps the local player's role visible on every page.|


#### Team composition adjustment

`ROLES` → `ROLE SETTINGS` offers this optional adjustment. OFF preserves the existing balance rules. ON prioritizes the support minimum before pair/showcase selection and applies the danger limit to future draws, including late arrivals. It does not enable roles, change weights, or bypass party/item requirements. Role history and lack of candidates can prevent the support target; danger limits can relax when otherwise no role can be assigned.

|Key|Default|Range / values|Effect|
|---|---|---|---|
|`Role Balance - Team.Enabled`|false|On/Off|Enable team composition adjustment.|
|`Role Balance - Team.SupportMinimum`|1|0–30|Desired minimum support roles, limited by party size.|
|`Role Balance - Team.DangerMaximum`|1|1–30|Maximum danger roles while alternatives are available.|

Support group: Medic, Rescuer, Mechanic, Electrician, Warden, Bodyguard, Porter, Rigger, Negotiator. Danger group: Bomber, Stinker, Werewolf, Influenza and the dangerous secret role. These assignment groups follow the balance settings and may differ from icon categories.

#### Role balance

Showcase roles are `Bomber`, `Stinker`, `Mage`, `Gambler`, `Trickster`, `King`, `Rider`, `Influencer`, and `Diver`. Support roles are `Medic`, `Rescuer`, `Mechanic`, `Electrician`, `Warden`, `Bodyguard`, `Porter`, `Rigger`, and `Negotiator`. Danger roles are `Bomber`, `Stinker`, `Werewolf`, and `Influenza`; Hardship roles are `Courier`, `Tuna`, and `Influenza`. Influencer is not a Danger role. If these rules leave no eligible role, RoleShuffle gradually loosens the limits so every player can still receive a role.

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Role Balance - Guarantees.ShowcaseEnabled`|true|On/Off|Enables party-size-based minimum Showcase assignments.|
|`Role Balance - Guarantees.ShowcaseMinimums`|`3:1,7:2,14:3,24:4`|`party size:minimum` pairs|Min Showcase assignments by party size.|
|`Role Balance - Guarantees.SupportEnabled`|true|On/Off|Enables party-size-based minimum Support assignments.|
|`Role Balance - Guarantees.SupportMinimums`|`4:1,8:2,14:3,24:4`|`party size:minimum` pairs|Min Support assignments by party size.|
|`Role Balance - Limits.Enabled`|true|On/Off|Enables Danger, Hardship, and small-party combined limits.|
|`Role Balance - Limits.DangerMaximums`|`1:1,8:2,16:3`|`party size:limit` pairs|Max simultaneous `Bomber`, `Stinker`, `Werewolf`, and `Influenza` roles by party size.|
|`Role Balance - Limits.HardshipMaximums`|`1:1,12:2`|`party size:limit` pairs|Max simultaneous `Courier`, `Tuna`, and `Influenza` roles by party size.|
|`Role Balance - Limits.SmallPartyMaximumPlayers`|4|1–30|Largest party size using the combined Danger and Hardship limit.|
|`Role Balance - Limits.SmallPartyCombinedMaximum`|1|1–30|Max combined Danger and Hardship roles in a small party.|
|`Role Balance - Variety.PreventSameRole`|true|On/Off|Prevents the same player receiving the same role in consecutive stages when alternatives exist.|
|`Role Balance - Variety.HardshipCooldownStages`|2|0–10|Cooldown stages after Courier/Tuna/Influenza before that player can normally draw these roles.|
|`Role Balance - Variety.ExcludeUnavailableRoles`|true|On/Off|Excludes context-dependent roles when their required enemy, weapon, or usable object is absent.|

#### Base upgrades

Item retention starts when enabled; earlier uses are not recovered. Saved levels add to role/base targets; Lifter/Rammer fixed levels and Influenza HP take priority. BASE UPGRADES excludes item bonuses; these appear in vanilla stats. To sell upgrades, set `General.ShopUpgradeItemCount` above 0.

Base targets stay active between stages and apply to upgrades not overridden by the current role. Use comma-separated `run level:value` pairs: `1:1,5:3,10:6` means 1 on levels 1–4, 3 on levels 5–9, and 6 from level 10. Run levels accept 1–999999. Blank settings and levels before the first entry use 0. Values are clamped to the allowed range; malformed pairs are ignored. Base settings and truck draws do not change Throw.

Truck draws occur after leaving the shop, during truck preparation. They select an upgrade and change amount using relative weights. Amounts that no eligible target can receive within its limits are excluded. Positive results can select `All Upgrades`, affecting enabled types with room below their limits. Results persist for the run and are announced by the host.

Open `ROLES` → `BASE UPGRADES` → `BASE UPGRADE SETTINGS` to toggle the draw and its 13 entries (12 types plus `All Upgrades`). These switches share the saved REPOConfig values. Hosts edit; installed participants view the host's settings. Apply pending REPOConfig edits before leaving its page; Roles UI saves immediately.

OFF excludes a type from future individual and `All Upgrades` draws without removing its base levels or acquired bonuses. An active draw keeps its starting selection. `No individual draw` means weight 0; an ON type can still receive `All Upgrades`. `No draw` on `All Upgrades` means its weight is 0. Disabling that entry affects only combined results. All-off selections produce no result.

Manual adjustments persist per save. Each +/- click changes the total by 1, within 0–200 (Map Player Count: 0–1). The host needs a save-supported lobby.

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Base Upgrades.KeepUpgradeItems`|false|On/Off|Keep consumed levels in this save. Off stops recording and excludes saved amounts at the next role/base reset without deleting them; Throw remains permanent.|
|`Base Upgrades.UpgradeItemScope`|`Player`|`Player`, `AllPlayers`|New uses benefit the consumer or everyone, including later joiners. Past uses keep their original recipients.|
|`Base Upgrades.ManualAdjustmentEnabled`|false|On/Off|Enables manual +/- in the lobby, truck and shop. Off hides manual controls and explanations and excludes saved adjustments; on restores them. Base rules and truck draws remain active.|
|`Base Upgrades.HealthUpgradeLevels`|`1:1`|`level:value` pairs; value 0–200|Health target.|
|`Base Upgrades.StaminaUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Stamina target.|
|`Base Upgrades.ExtraJumpUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Extra Jump target.|
|`Base Upgrades.SpeedUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Speed target.|
|`Base Upgrades.StrengthUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Strength target.|
|`Base Upgrades.RangeUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Range target.|
|`Base Upgrades.LaunchUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Launch target.|
|`Base Upgrades.TumbleClimbUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Tumble Climb target.|
|`Base Upgrades.TumbleWingsUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Tumble Wings target.|
|`Base Upgrades.CrouchRestUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Crouch Rest target.|
|`Base Upgrades.MapPlayerCountUpgradeLevels`|Blank|`level:value` pairs; value 0–1|Map Player Count target.|
|`Base Upgrades.DeathHeadBatteryUpgradeLevels`|Blank|`level:value` pairs; value 0–200|Death Head Battery target.|
|`Base Upgrade Draw.Enabled`|true|On/Off|Enables the shared Base Upgrade draw after leaving the shop.|
|`Base Upgrade Draw.MaximumLevel`|200|0–200|Max target that positive truck draws can reach. Map Player Count remains limited to 1.|
|`Base Upgrade Draw.CappedUpgradeWeightMultiplier`|0.25|0–1|Weight falls toward this multiplier as configured + truck levels approach the cap. 0 excludes capped types; 1 stops the decrease. All Upgrades is unaffected.|
|`Base Upgrade Draw.WeightFalloffExponent`|2|0.1–10|Controls the weight decrease curve. 1 decreases evenly; larger values preserve more weight until near the maximum.|
|`Base Upgrade Draw.ChangeAmountWeights`|`-1:10,0:15,1:60,2:15`|`change amount:weight` pairs; amount `-200`–200, weight 0–1000|Relative weights for the possible change amounts.|
|`Base Upgrade Draw Selection.Health`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.Stamina`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.ExtraJump`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.Speed`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.Strength`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.Range`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.Launch`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.TumbleClimb`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.TumbleWings`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.CrouchRest`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.MapPlayerCount`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.DeathHeadBattery`|true|On/Off|Include in draws.|
|`Base Upgrade Draw Selection.AllUpgrades`|true|On/Off|Enables the combined positive result for enabled types only.|
|`Base Upgrade Draw Weights.Health`|20|0–1000|Health weight.|
|`Base Upgrade Draw Weights.Stamina`|40|0–1000|Stamina weight.|
|`Base Upgrade Draw Weights.ExtraJump`|10|0–1000|Extra Jump weight.|
|`Base Upgrade Draw Weights.Speed`|30|0–1000|Speed weight.|
|`Base Upgrade Draw Weights.Strength`|20|0–1000|Strength weight.|
|`Base Upgrade Draw Weights.Range`|30|0–1000|Range weight.|
|`Base Upgrade Draw Weights.Launch`|10|0–1000|Launch weight.|
|`Base Upgrade Draw Weights.TumbleClimb`|10|0–1000|Tumble Climb weight.|
|`Base Upgrade Draw Weights.TumbleWings`|10|0–1000|Tumble Wings weight.|
|`Base Upgrade Draw Weights.CrouchRest`|30|0–1000|Crouch Rest weight.|
|`Base Upgrade Draw Weights.MapPlayerCount`|10|0–1000|Map Player Count weight.|
|`Base Upgrade Draw Weights.DeathHeadBattery`|10|0–1000|Death Head Battery weight.|
|`Base Upgrade Draw Weights.AllUpgrades`|1|0–1000|Relative `ALL UPGRADES` selection weight for positive results.|

Higher weights are more likely; weights fall near the draw limit. Shop stock does not affect draw weights.

#### Role selection

Each standard role has host settings `<Role>.Enabled = true` and `<Role>.Weight = 100` (range 0–1000). OFF or weight 0 excludes it from random assignment. Use `Courier` for Courier's section. The two hidden roles use `???1.Enabled` and `???2.Enabled`, both true by default, with fixed weights. These switches apply from the next assignment. Twins is selected as one pair of two players, even when unique roles are enabled.

#### Upgrade and special-role settings

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Tank.HealthUpgradeLevels`|21|0–200|Min Health level guaranteed to Tank.|
|`Runner.SpeedUpgradeLevels`|6|0–200|Min Speed level guaranteed to Runner.|
|`Runner.StaminaUpgradeLevels`|46|0–200|Min Stamina level guaranteed to Runner.|
|`Jumper.ExtraJumpUpgradeLevels`|10|0–200|Extra Jump levels granted to Jumper.|
|`Launcher.LaunchUpgradeLevels`|10|0–200|Launch levels granted to Launcher.|
|`Climber.ClimbUpgradeLevels`|50|0–200|Tumble Climb levels granted to Climber.|
|`Climber.RangeUpgradeLevels`|20|0–200|Range levels granted to Climber.|
|`Flyer.WingsUpgradeLevels`|10|0–200|Tumble Wings levels granted to Flyer.|
|`Tracker.HealthUpgradeLevels`|3|0–200|Min Health level; preserves stronger Base upgrades.|
|`Tracker.EnemyDetectionRange`|25|1–100|Enemy detection distance (m), including through walls.|
|`Tracker.DangerAlertRange`|8|0–100|Close warning distance (m), capped at detection range. 0 disables warnings.|
|`Tracker.NotificationIntervalSeconds`|20|5–120|Min report interval (s). Close warnings bypass normal reports' wait, with their own same-length interval.|
|`Tracker.DistanceChangeThreshold`|5|1–50|Distance change (m) for an update. Target changes, turns of 60° or above/below changes also update.|
|`Ghost.DeathHeadBatteryUpgradeLevels`|50|0–200|Death Head Battery levels granted to Ghost.|
|`Ghost.HealRadius`|5|0–30|Healing radius around the head (m).|
|`Ghost.HealAmount`|1|0–100|HP per ally per pulse.|
|`Ghost.HealIntervalSeconds`|2|0.5–60|Healing interval (s) while dead.|
|`Ghost.TotalHealingLimit`|50|0–10000|Shared healing budget per stage; revival does not reset it.|
|`Ghost.CarrierSpeedBonusLevels`|1|0–20|Temporary Speed levels while carrying the head. No stacking; stronger King Speed aura wins.|
|`Bomber.DistancePerGrenade`|8|1–100|Travel distance (m) required per grenade placement.|
|`Bomber.MaximumActiveGrenades`|30|1–30|Max generated grenades retained per Bomber. Placing another removes the oldest one.|
|`Bomber.AllowTruckSpawns`|false|On/Off|Allows grenade placement inside the truck.|
|`Bomber.ExplosiveGrenadesEnabled`|true|On/Off|Includes vanilla explosive grenades in the random selection.|
|`Bomber.StunGrenadesEnabled`|true|On/Off|Includes vanilla stun grenades in the random selection.|
|`Bomber.ShockwaveGrenadesEnabled`|true|On/Off|Includes vanilla shockwave grenades in the random selection.|
|`Bomber.DuctTapedGrenadesEnabled`|true|On/Off|Includes vanilla duct-taped grenades in the random selection.|
|`Medic.HealAmount`|5|1–100|Health restored to each nearby teammate each time.|
|`Medic.HealIntervalSeconds`|2|0.1–30|Seconds between each round of Medic healing.|
|`Medic.HealRadius`|5|1–30|Max healing distance (m).|
|`Medic.TotalHealingLimit`|150|1–10000|Max total health restored by each Medic per stage. Only health actually missing from a target consumes the limit.|
|`Phoenix.ReviveDelaySeconds`|2|2–10|M(s) before revival; also waits for the Death Head to stop moving.|
|`Phoenix.RevivalHealth`|25|1–1000|Health after Phoenix revival, capped at their maximum health.|
|`Courier.Damage`|1|1–100|Damage each time outside the truck.|
|`Courier.DamageIntervalSeconds`|0.1|0.05–10|Seconds between damage outside the truck.|
|`Rescuer.ReviveDelaySeconds`|2|0–10|M(s) after death. Keep holding the head until revival is ready.|
|`Rescuer.MaximumRevives`|2|1–10|Max revivals per Rescuer per stage.|
|`Rescuer.RevivalHealth`|25|1–1000|Health after a Rescuer revival, capped at the target's maximum health.|
|`Vampire.Tier1HealAmount`|5|1–100|Health restored by a nearby Danger Level 1 enemy death.|
|`Vampire.Tier2HealAmount`|10|1–100|Health restored by a nearby Danger Level 2 enemy death.|
|`Vampire.Tier3HealAmount`|50|1–100|Health restored by a nearby Danger Level 3 enemy death.|
|`Vampire.Radius`|10|1–50|Max distance (m) from the dying enemy.|
|`Tuna.StationaryDelaySeconds`|3|0.1–30|Seconds without movement before damage begins.|
|`Tuna.Damage`|1|1–100|Damage per hit while stationary.|
|`Tuna.DamageIntervalSeconds`|0.1|0.05–10|Seconds between stationary damage.|
|`Musician.HealAmount`|5|1–100|HP per healing pulse per living recipient.|
|`Musician.HealRadius`|10|1–50|Healing radius (m).|
|`Musician.HealCooldownSeconds`|1|0.1–30|Healing interval (s), shared by all instruments.|
|`Musician.TotalHealingLimit`|1000|1–10000|Self + allies per stage; remaining budget on HUD.|
|`Mage.CastIntervalSeconds`|3|0.1–30|M(s) between spell activations.|
|`Mage.BeamDurationSeconds`|5|0.1–30|Laser spell duration (s). Held staff attacks are unchanged.|
|`Mage.StarExpression`|`Angry`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `star`.|
|`Mage.RollExpression`|`Sad`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `roll`.|
|`Mage.GravityExpression`|`Suspicious`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `gravity`.|
|`Mage.VoidExpression`|`EyesClosed`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `void`.|
|`Mage.LaserExpression`|`Scared`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `laser`.|
|`Mage.AutoRecoveryEnabled`|true|On/Off|Enables automatic Mage health recovery after avoiding damage.|
|`Mage.AutoRecoveryDelaySeconds`|10|0–300|Seconds without taking damage before automatic recovery starts.|
|`Mage.AutoRecoveryIntervalSeconds`|2|0.1–60|Seconds between each automatic recovery.|
|`Mage.AutoRecoveryAmount`|1|0–100|HP restored each time automatic recovery occurs.|
|`Mage.AutoRecoveryTotalHealingLimit`|120|1–10000|Auto-healing budget per player/stage, excluding other healing. Death, revival and role changes preserve usage.|
|`Mage.StarHealthCost`|10|0–100|Health consumed after a successful `star` cast.|
|`Mage.GravityHealthCost`|10|0–100|Health consumed after a successful `gravity` cast.|
|`Mage.RollHealthCost`|15|0–100|Health consumed after a successful `roll` cast.|
|`Mage.VoidHealthCost`|30|0–100|Health consumed after a successful `void` cast.|
|`Mage.LaserHealthCost`|50|0–100|Health consumed after a successful `laser` cast.|
|`Gambler.WinChancePercent`|50|0–100|Chance that a wager uses the win multiplier instead of destroying the valuable.|
|`Gambler.WinValueMultiplier`|2|0–10|Valuable multiplier applied to a winning wager.|
|`Gambler.GambitGreenHealAmount`|50|25–1000|Total HP restored by Gambit's green result for a Gambler.|
|`Gambler.GambitRedDamage`|100|50–1000|Total damage dealt by Gambit's red result to a Gambler.|
|`Gambler.GambitWhiteHealthUpgradeLevels`|5|0–200|Health upgrade levels added by Gambit's white result for the current stage only.|
|`Hunter.WeaponBatteryConsumptionPercent`|75|0–100|Percentage of normal weapon battery consumption used by Hunter.|
|`Hunter.DoubleOrbChancePercent`|10|0–100|Chance to double the normal orb count after Hunter defeats an enemy, checked only if the jackpot roll fails.|
|`Hunter.JackpotOrbChancePercent`|0.5|0–100|Personal-kill jackpot chance. Adds missing orbs up to the target; larger normal drops are preserved.|
|`Hunter.JackpotOrbCount`|10|1–30|Target orb count used by Hunter's jackpot result.|
|`Hunter.GuaranteedOrbEveryKills`|5|0–100|Qualifying personal kills per guaranteed extra orb; 0 disables. Random extras count toward the guarantee.|
|`Stinker.DistancePerCloud`|2|1–100|Travel distance (m) between recorded uranium-cloud trail points.|
|`Stinker.MinimumSafetyDistance`|2|1–30|Min distance Stinker must be from the newest cloud position. Clouds appear one after another.|
|`Stinker.AllowTruckSpawns`|false|On/Off|Allows uranium-cloud trail points to be recorded and created inside the truck.|
|`Trickster.ActiveSeconds`|25|1–120|Seconds before the active decoy is removed.|
|`Trickster.DecoyExpression`|`Happy`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that places the decoy.|
|`Trickster.CooldownSeconds`|45|1–300|Seconds after a decoy that attracted an enemy ends before another can be placed.|
|`Trickster.NoTargetCooldownSeconds`|10|0–300|Seconds after a decoy that attracted no enemies ends before another can be placed.|
|`Trickster.InvestigateRadius`|40|1–100|Enemy investigation radius around the decoy (m).|
|`Trickster.PulseIntervalSeconds`|1|0.25–10|Seconds between enemy investigation pulses.|
|`Trickster.PlacementDistance`|2|0.5–10|Placement distance in front of Trickster (m).|
|`Mechanic.RepairPercentPerSecond`|2|0–100|Shared original-value percentage repaired per second while Mechanic directly holds damaged valuables.|
|`Mechanic.MaximumRepairPercentPerStage`|50|0–100|Max cumulative original-value percentage repaired by each Mechanic per stage.|
|`Electrician.ChargePercentPerSecond`|10|0–100|Shared battery percentage restored per second while Electrician directly holds inactive rechargeable items.|
|`Electrician.MaximumChargePercentPerStage`|100|0–1000|Max cumulative battery percentage restored by each Electrician per stage.|
|`Warden.AdditionalStunSeconds`|3|0–30|Seconds added to enemy stuns caused by Warden.|
|`Ninja.VisionRecognitionMultiplier`|2|1–10|Multiplier applied to the time enemies need to visually recognize Ninja.|
|`Executioner.StunnedDamageMultiplier`|2|1–10|Direct attack damage multiplier against enemies already stunned before the hit.|
|`Rider.EnemyDamageMultiplier`|3|1–10|Vehicle impact damage multiplier against enemies while Rider drives.|
|`Rider.PlayerKnockbackMultiplier`|2|1–10|Multiplier for existing vehicle Tumble knockback against players.|
|`Influencer.PlayerRadius`|20|1–100|Radius (m) used to count nearby living teammates.|
|`Influencer.PlayerCheckIntervalSeconds`|0.5|0.1–10|Seconds between nearby-player and dynamic-upgrade checks.|
|`Influencer.NoiseRadiusMultiplier`|2|1–10|Multiplier for footstep, landing, VC, and chat TTS enemy investigation radii.|
|`Influencer.TTSInvestigateRadius`|20|1–100|Enemy investigation radius of periodic TTS.|
|`Influencer.MinimumTTSIntervalSeconds`|20|1–300|Min randomly selected periodic TTS interval.|
|`Influencer.MaximumTTSIntervalSeconds`|40|1–300|Max randomly selected periodic TTS interval. Values are safely swapped when configured below the minimum.|
|`Influencer.TTSQuietPeriodSeconds`|1.5|0–10|Quiet time after this player's TTS before periodic speech.|
|`Influencer.<Upgrade>UpgradeScaling`|See defaults table|`count:level` pairs|Min target by nearby-player count; never lowers the pre-role level. Blank grants nothing.|
|`Werewolf.PlayerDamageMultiplier`|2|1–10|Multiplier for identifiable damage Werewolf deals to another player.|
|`Berserker.<Upgrade>UpgradeScaling`|See defaults table|`HP%:level` pairs|Min target at or below the HP threshold; never lowers the pre-role level. Blank grants nothing; Health is unavailable.|
|`Bodyguard.HealthUpgradeLevels`|11|0–200|Health target while Bodyguard is assigned.|
|`Bodyguard.ProtectionRadius`|15|1–100|Max teammate-protection distance (m).|
|`Bodyguard.DamageSharePercent`|50|0–100|Percentage of enemy damage transferred without reducing Bodyguard below 1 HP.|
|`Rammer.TumbleAttackDamage`|100|0–100000|Enemy damage dealt by Rammer's Tumble Attack.|
|`Rammer.SelfDamage`|15|0–100000|Damage Rammer takes after its Tumble Attack hits an enemy.|
|`Diver.UnderfloorDurationSeconds`|10|1–120|Max time below a floor before Diver dies.|
|`Diver.MovementForce`|8|1–30|Free-movement force while Diver is below a floor.|
|`Sniper.ReferenceDistance`|6|1–50|Distance (m) where enemy damage is unchanged.|
|`Sniper.MinimumDamageMultiplier`|0.5|0–1|Enemy damage multiplier at point-blank range. Positive hits still deal at least 1 damage.|
|`Sniper.MaximumDamageMultiplier`|2|1–10|Max enemy damage multiplier at long range.|
|`Sniper.MaximumMultiplierDistance`|18|1–100|Max-damage distance; effective minimum is `ReferenceDistance + 0.1` m.|
|`Avenger.DamageMultiplier`|1.5|1–10|Enemy damage multiplier after another player dies.|
|`Avenger.DurationSeconds`|30|1–120|Duration of the enemy damage bonus. Another death refreshes this duration.|
|`Avenger.TriggerRadius`|30|1–100|Max distance (m) from a dying player that activates Avenger.|
|`Avenger.AllyHitDamageThreshold`|15|1–1000|Min HP lost to one enemy hit.|
|`Avenger.AllyHitTriggerRadius`|15|1–100|Max distance to the hit ally (m).|
|`Avenger.AllyHitDamageMultiplier`|1.25|1–10|Hit-triggered enemy damage multiplier. Stronger active bonus wins.|
|`Avenger.AllyHitDurationSeconds`|10|1–120|Hit-triggered bonus duration (s).|
|`Avenger.AllyHitCooldownSeconds`|20|1–300|Hit-trigger cooldown (s); deaths bypass it.|
|`Brawler.MeleeDamageMultiplier`|1.25|0–10|Damage multiplier for identifiable melee weapon attacks against enemies and players.|
|`Brawler.RangedDamageMultiplier`|0.75|0–10|Damage multiplier for identifiable gun, staff-projectile, and laser attacks against enemies and players.|
|`Brawler.ComboMultiplierStep`|0.25|0–5|Multiplier added per consecutive hit on the same enemy. No friendly-fire increase.|
|`Brawler.ComboMaximumMultiplier`|2|0–10|Combo cap; never below the starting melee multiplier.|
|`Brawler.ComboTimeoutSeconds`|4|0.1–30|Seconds without a hit before reset. Changing enemy also resets it.|

The default Base level is Health 1 and 0 for other upgrades. Role upgrade settings specify the level used during that role; they are not levels added to Base. Influencer and Berserker preserve stronger upgrades when their conditions change. `Tracker` keeps stronger Base upgrades and remains eligible at high Base levels. `Rammer` sets Launch, Tumble Climb and Tumble Wings to 0 during the stage. Throw is unaffected.


## 日本語

### 役職の成長・支援設定

|キー|初期値|範囲|効果|
|---|---|---|---|
|`Tank.HealthMultiplier` / `Tank.MaximumHealth`|1.5 / 4100|1–10 / 100–4100|最大HP|
|`Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed`|1.5 / 205|1–10 / 5–205|走行速度|
|`Runner.StaminaMultiplier` / `Runner.MaximumStamina`|1.5 / 2040|1–10 / 40–2040|スタミナ容量|
|`Courier.ContractDistance`|5|1–50m|搬入先の外での運搬距離。|
|`Courier.InitialGraceSeconds`|120|0–300秒|開始・蘇生後のHP減少停止。|
|`Courier.<Size>GraceSeconds`|`90/120/180/240/240/240/300`|0–300秒|Tiny/Small/Medium/Big/Wide/Tall/VeryTall（極小/小/中/大/横長/縦長/超縦長）。残り猶予は短縮しません。|
|`Courier.<Size>HealAmount`|`10/25/50/100/100/100/100`|0–10000HP|同じサイズ順。固定HP回復、最大HPまで。|
|`King.SpeedBonusLevels` / `King.RangeBonusLevels`|2 / 2|0–200|Speed・Range追加レベル。|
|`King.StrengthBonusLevels`|5|0–200|Strengthの一時追加レベル。合計上限Lv200。|
|`King.UpgradeRadius`|12|1–30 m|味方に強化を付与する範囲。|
|`HUD.ResourceHudEnabled`|true|真偽値|左上に自分の能力残量を表示。役職一覧とは独立。|
|`HUD.ResourceHudScalePercent`|100|50–200|残量HUDの大きさ。画面内に収まるよう自動調整。|
|`HUD.ResourceHudOffsetX` / `ResourceHudOffsetY`|0 / 0|0–3840 / 0–2160|スタミナ直下を基準に、残量表示を右／下へ移動する。|

能力の残量はスタミナ直下に縦1列で表示し、項目が多い場合は収まるよう縮小します。役職一覧には各プレイヤーの役職を表示。残量HUDを使う場合は、ホストと同じバージョンのRoleShuffleを導入してください。

### 能力の調整

ホストがREPOConfig → RoleShuffle → 役職名から設定。初期値は上記の能力です。保持する力・HP・感染判定には現在の値を使用します。発症時間は新しい感染から適用し、予約済みのくしゃみの時刻は維持。Stinkerの猶予は新しく出現する貴重品から適用。既存設定は保持します。

|設定|初期値|範囲|効果|
|---|---|---|---|
|`Lifter.StrengthLevel`|200|0～200|役職中に付与するStrengthアップグレードのレベル。|
|`Lifter.LightItemStrengthLevel`|1|0～200|軽量品・ショップ装備（カート・バイクを除く）を保持するStrengthレベル。|
|`Stinker.BreakGraceSeconds`|0.5|0～10秒|出現したウラン貴重品が壊れるまでの猶予。|
|`Influenza.IncubationSeconds`|30|0～300秒|感染から発症までの時間。|
|`Influenza.MaximumHealth`|75|1～10000HP|発症後の最大HP。増やしても回復しません。|
|`Influenza.SneezeMinimumSeconds` / `Influenza.SneezeMaximumSeconds`|30 / 90|1～600秒|くしゃみの間隔。大小を自動で揃え、同値なら一定間隔。|
|`Influenza.SneezeRange` / `Influenza.SpeechRange`|5 / 3|0.1～30m|くしゃみ／VC・チャットで感染する距離。|
|`Influenza.SneezeAngleDegrees` / `Influenza.SpeechAngleDegrees`|40 / 60|1～360度|感染範囲の全角。左右それぞれ半分の角度。|
|`Influenza.SneezeChancePercent` / `Influenza.SpeechChancePercent`|60 / 30|0～100％|くしゃみ／発話・投稿ごとの、仲間1人あたりの感染確率。|
|`Influenza.SpeechSilenceSeconds`|0.75|0.1～5秒|別の発話として判定する無音時間。|
|`Influenza.SneezeNoiseRadius`|5|0～100m|敵に聞こえる基本距離。敵の聴力で変化。0で反応なし。|
|`Twins.CarryStrengthMultiplier`|1.5|1～3|共同運搬中のStrength目標倍率。上限Lv200まで一時アップグレードを付与。|
|`Twins.CollisionReductionPercent`|50|0–100%|共同運搬時の衝突価値損失を軽減。|
|`Twins.RestRadius` / `RestHoldSeconds`|3 / 3|0.5–10 m / 1–30 s|休憩距離 / 被弾せずしゃがんで静止する時間。|
|`Twins.RestHealPercent` / `RestCooldownSeconds`|10 / 60|0–100% / 1–600 s|共有最大HPに対する回復率 / ペア共通クールタイム。中断は消費なし。|
|`Twins.RendezvousStartRange` / `RendezvousEndRange`|15 / 5|1–100 m / 0.5–100 m|合流支援の開始/終了距離。小さい方を終了距離として使用。|
|`Twins.RendezvousSpeedMultiplier`|1.25|1–2|離れた相方へ走る倍率。Speedレベルへ切り上げ。|
|`Twins.DeliveryCarrySeconds` / `DeliveryRadius`|3 / 5|1–30 s / 0.5–20 m|納品区域外で共同運搬する時間 / 納品時の2人と品の距離。|
|`Twins.DeliveryBonusPercent` / `DeliveryStageLimit`|10 / 5000|0–100% / 0–10000|共同納品の価値加算率 / ペアのステージ予算。品ごとに1回。|
|`Porter.Capacity`|15|1～100|収納重量の上限。個数制限なし。HUDに重量・収納品の現在合計金額・荷下ろし残り時間を表示。|
|`Porter.HoldSeconds`|3|1～30秒|収納まで1人でつかみ続ける時間。|
|`Porter.FullUnloadSeconds`|10|0～60秒|満載時の荷下ろし時間。重量に比例。0なら即座に完了。|
|`Porter.DropOnEnemyHit`|true|真偽値|敵からHPダメージで全収納品をばらまく。|
|`Porter.AllowedSizes`|Tiny,Small,Medium|Tiny・Small・Medium・Big・Wide・Tall・VeryTallをカンマ区切り|収納可能サイズ。空欄・不明な名前は追加許可しない。|
|`DualWielder.FollowDelaySeconds` / `DualWielder.LeftOffset`|0.3秒 / 0.5m|0.05～2秒 / 0.1～1.5m|追従の遅延と左への距離。|
|`Rigger.EnemyDamageMultiplier` / `Rigger.EnemyHoldDurationMultiplier` / `Rigger.DurabilityConsumptionMultiplier`|2 / 1.5 / 0.5|1～10 / 1～3 / 0.1～1|敵への命中ダメージ／拘束時間／耐久消費。|
|`Negotiator.DeliveryCarryDistance`|20|1–200 m|納品所で手放すまでに本人がトラック外で運ぶ距離。|
|`Negotiator.DeliveryAllowedSizes`|Big,Wide,Tall,VeryTall|サイズ名のカンマ区切り|納品対象サイズ。大文字小文字・周囲空白は無視。空欄は納品獲得なし、不明名は無視。|
|`Negotiator.EnemyTier`|3|1–3|本人討伐の対象となる敵本来のTier。完全一致で、Enhanced報酬補正は対象外。|

### 設定

REPOConfigで編集。`HUD.*`は本人の表示設定、それ以外はホスト設定です。複数キーの行では、省略したセクション名は最初のキーと共通です。

#### 一般、通知、HUD

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`General.Enabled`|true|On/Off|ステージごとの役職割り当てを有効化。|
|`General.UniqueRoles`|true|On/Off|抽選可能な役職を一巡するまで重複を避けます。|
|`General.ShopUpgradeItemCount`|0|0～30|各ショップへ要求するアップグレードアイテム数。0でショップの抽選対象から除外する。|
|`Compatibility.EnhancedEnemyRewardsEnabled`|true|On/Off|有効時、Elite Enemy VariantsのEnhanced個体をVampireの回復量とHunterの追加オーブ品質の計算で1段階上として扱います。|
|`Notifications.Enabled`|true|On/Off|役職名と役職効果のチャット／TTS通知を有効化。|
|`Notifications.DelaySeconds`|2.5|0～30|Stage Flux未導入時の役職通知までの秒数。|
|`Notifications.StageFluxDelaySeconds`|5|0～30|Stage Flux導入時、役職通知までに最低限待つ秒数。Stage FluxのTTS終了後、さらに1.5秒間の無音を確認する。|
|`HUD.Enabled`|true|On/Off|ステージ中、現在の全役職を表示。|
|`HUD.RoleDisplay`|`NameOnly`|`IconAndName`, `NameOnly`, `IconOnly`|役職のアイコン＋名前、名前のみ、アイコンのみを選択する。プレイヤー名は常に表示。|
|`HUD.IconSize`|64|32～128|HUD全体の倍率を適用する前の役職アイコンの大きさ。6人分の高さを確保し、画面に収まる倍率へ調整する。|
|`HUD.Anchor`|`BottomLeft`|`TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight`|HUDの基準位置を選択する。|
|`HUD.Alignment`|`Left`|`Left`, `Center`, `Right`|役職テキストの揃え方を選択する。|
|`HUD.OffsetX`|0|`-3840`～3840|基準位置からの水平オフセット。単位はピクセル。|
|`HUD.OffsetY`|80|`-2160`～2160|基準位置からの垂直オフセット。単位はピクセル。|
|`HUD.ScalePercent`|70|50～200|HUDの表示倍率。|
|`HUD.PlayersPerPage`|8|2～20|自分の固定表示を含め、同時に表示する最大人数。6以上なら、文字やアイコンが高くても6人分の表示領域を確保する。|
|`HUD.PageIntervalSeconds`|5|1～30|各役職ページを表示する秒数。|
|`HUD.TransitionDurationSeconds`|0.2|0～1|役職ページ切替時のフェード時間。|
|`HUD.PinLocalPlayer`|true|On/Off|自分の役職をすべてのページへ固定表示。|


#### チーム構成調整

「ROLES」→「ロール設定」から切り替えます。初期OFFでは既存のバランス設定を使用します。ONではペア・演出役の抽選より先に支援役の最低人数を優先し、危険役の上限を適用します。途中参加時も対象です。役職の有効状態・重み・人数条件・アイテム条件は維持します。履歴や候補不足により支援役の目標を満たせない場合や、割り当て先がない場合に危険役の上限を緩和する場合があります。

|設定|初期値|範囲・値|効果|
|---|---|---|---|
|`Role Balance - Team.Enabled`|false|On/Off|チーム構成調整を有効化。|
|`Role Balance - Team.SupportMinimum`|1|0–30|支援役の最低人数の目標。参加人数まで。|
|`Role Balance - Team.DangerMaximum`|1|1–30|他に候補がある場合の危険役の最大人数。|

支援役：Medic、Rescuer、Mechanic、Electrician、Warden、Bodyguard、Porter、Rigger、Negotiator。危険役：Bomber、Stinker、Werewolf、Influenza、および危険系の隠し役職。抽選上のグループのため、アイコンのカテゴリとは一部異なります。

#### 役職バランス

この表はすべてホスト設定。

Showcase役は`Bomber`、`Stinker`、`Mage`、`Gambler`、`Trickster`、`King`、`Rider`、`Influencer`、`Diver`です。Support役は`Medic`、`Rescuer`、`Mechanic`、`Electrician`、`Warden`、`Bodyguard`、`Porter`、`Rigger`、`Negotiator`です。Danger役は`Bomber`、`Stinker`、`Werewolf`、`Influenza`、Hardship役は`Courier`、`Tuna`、`Influenza`です。InfluencerはDanger役ではありません。これらの条件で割り当て可能な役職がなくなる場合は、全員に役職を割り当てられるまで制限を段階的に緩和します。

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`Role Balance - Guarantees.ShowcaseEnabled`|true|On/Off|参加人数別のShowcase最低保証を有効化。|
|`Role Balance - Guarantees.ShowcaseMinimums`|`3:1,7:2,14:3,24:4`|`参加人数:最低人数`の組|参加人数ごとに保証するShowcaseの最低人数。|
|`Role Balance - Guarantees.SupportEnabled`|true|On/Off|参加人数別のSupport最低保証を有効化。|
|`Role Balance - Guarantees.SupportMinimums`|`4:1,8:2,14:3,24:4`|`参加人数:最低人数`の組|参加人数ごとに保証するSupportの最低人数。|
|`Role Balance - Limits.Enabled`|true|On/Off|Danger、Hardship、少人数時の複合上限を有効化。|
|`Role Balance - Limits.DangerMaximums`|`1:1,8:2,16:3`|`参加人数:上限`の組|参加人数ごとの`Bomber`、`Stinker`、`Werewolf`、`Influenza`の合計上限。|
|`Role Balance - Limits.HardshipMaximums`|`1:1,12:2`|`参加人数:上限`の組|参加人数ごとの`Courier`、`Tuna`、`Influenza`の合計上限。|
|`Role Balance - Limits.SmallPartyMaximumPlayers`|4|1～30|DangerとHardshipの複合上限を使用する最大参加人数。|
|`Role Balance - Limits.SmallPartyCombinedMaximum`|1|1～30|少人数時に許可するDangerとHardshipの合計最大人数。|
|`Role Balance - Variety.PreventSameRole`|true|On/Off|ほかの候補がある場合、同じプレイヤーへの同役職の連続割り当てを防ぎます。|
|`Role Balance - Variety.HardshipCooldownStages`|2|0～10|`Courier`、`Tuna`、`Influenza`のいずれかの後、そのプレイヤーをこれらの役職から通常除外するステージ数。|
|`Role Balance - Variety.ExcludeUnavailableRoles`|true|On/Off|必要な敵、武器、使用対象が存在しない状況依存役を抽選から除外する。|

#### 基礎アップグレード

使用分の保持はONにした後からです。過去の使用は復元しません。役職・基礎値に加算しますが、Lifter・Rammerの固定値とInfluenzaのHPを優先します。アイテム分はBASE UPGRADESには含めず、バニラの能力値に反映します。販売する場合は`General.ShopUpgradeItemCount`を1以上にしてください。

この表はすべてホスト設定。

役職が上書きしない強化には基礎値を適用し、ステージ外でも維持。カンマ区切りの`ランレベル:設定値`で指定。`1:1,5:3,10:6`ならレベル1～4は1、5～9は3、10以降は6です。ランレベルは1～999999。空欄や最初の指定レベルより前は0、範囲外の値は上限・下限に補正し、不正な組は無視します。基礎設定・トラック抽選ではThrowを変更しません。

トラック抽選はショップを出た後の準備中に行い、相対Weightで種類と増減値を選びます。範囲内で適用できる対象がない増減値は除外します。正の結果では`All Upgrades`も候補となり、ONの種類のうち上限まで余地があるものを強化します。結果はホストが発言し、そのラン中維持。

`ROLES` → `基本アップグレード` → `基本アップグレード設定`で、抽選全体と13項目（12種類＋`All Upgrades`）を切り替えます。REPOConfigと同じ値を保存し、ホストは編集、導入済み参加者はホストの設定を閲覧可能です。REPOConfigでは変更を適用してから閉じてください。Roles UIは即時保存します。

OFFの種類は次回以降の個別・一括抽選から除外し、基礎値や獲得済みボーナスは保持します。実行中の抽選は開始時の設定を使用します。`個別抽選なし`は重み0で、ONなら`All Upgrades`の対象になります。`All Upgrades`の`抽選なし`は一括抽選の重み0を示し、OFFは一括抽選だけを無効にします。全項目OFFなら抽選結果は発生しません。

手動調整はセーブごとに保持します。±で表示値が1変わり、範囲は0～200（Map Player Countは0～1）。保存対応ロビーでホストが操作します。

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`Base Upgrades.KeepUpgradeItems`|false|true、false|使用分を同じセーブで保持。OFFで記録を止め、次の役職・基礎値適用から保存分を除外（削除なし）。Throwはバニラ同様に残ります。|
|`Base Upgrades.UpgradeItemScope`|`Player`|`Player`、`AllPlayers`|今後の使用分を本人／全員（後からの参加者を含む）へ加算。過去の加算先は変わりません。|
|`Base Upgrades.ManualAdjustmentEnabled`|false|true、false|ロビー・トラック・ショップの±操作を有効化。OFFで手動操作・内訳・案内を隠し、保存済み調整分を除外。ONで復元。設定式とトラック抽選は維持。|
|`Base Upgrades.HealthUpgradeLevels`|`1:1`|`レベル:設定値`の組、設定値0～200|Health基礎値。|
|`Base Upgrades.StaminaUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Stamina基礎値。|
|`Base Upgrades.ExtraJumpUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Extra Jump基礎値。|
|`Base Upgrades.SpeedUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Speed基礎値。|
|`Base Upgrades.StrengthUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Strength基礎値。|
|`Base Upgrades.RangeUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Range基礎値。|
|`Base Upgrades.LaunchUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Launch基礎値。|
|`Base Upgrades.TumbleClimbUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Tumble Climb基礎値。|
|`Base Upgrades.TumbleWingsUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Tumble Wings基礎値。|
|`Base Upgrades.CrouchRestUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Crouch Rest基礎値。|
|`Base Upgrades.MapPlayerCountUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～1|Map Player Count基礎値。|
|`Base Upgrades.DeathHeadBatteryUpgradeLevels`|空欄|`レベル:設定値`の組、設定値0～200|Death Head Battery基礎値。|
|`Base Upgrade Draw.Enabled`|true|On/Off|ショップ後の共有Base Upgrade抽選を有効化。|
|`Base Upgrade Draw.MaximumLevel`|200|0～200|正のトラック抽選で到達できる最大目標値。Map Player Countは最大1のまま。|
|`Base Upgrade Draw.CappedUpgradeWeightMultiplier`|0.25|0～1|アップグレードが抽選上限に近づくほどWeightが低下し、上限でこの倍率になります。設定分とトラック抽選分の合計を使用する。0では上限到達後に対象外となり、1では低下しません。All Upgradesは対象外。|
|`Base Upgrade Draw.WeightFalloffExponent`|2|0.1～10|抽選Weightの低下カーブ。1は一定のペースで低下し、大きい値ほど上限付近まで重みを維持。|
|`Base Upgrade Draw.ChangeAmountWeights`|`-1:10,0:15,1:60,2:15`|`増減値:重み`の組、増減値`-200`～200、重み0～1000|抽選される増減値の相対Weight。|
|`Base Upgrade Draw Selection.Health`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.Stamina`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.ExtraJump`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.Speed`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.Strength`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.Range`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.Launch`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.TumbleClimb`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.TumbleWings`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.CrouchRest`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.MapPlayerCount`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.DeathHeadBattery`|true|On/Off|抽選対象にする。|
|`Base Upgrade Draw Selection.AllUpgrades`|true|On/Off|ONの種類だけを対象に、正の一括抽選を有効化。|
|`Base Upgrade Draw Weights.Health`|20|0～1000|Healthの相対Weight。|
|`Base Upgrade Draw Weights.Stamina`|40|0～1000|Staminaの相対Weight。|
|`Base Upgrade Draw Weights.ExtraJump`|10|0～1000|Extra Jumpの相対Weight。|
|`Base Upgrade Draw Weights.Speed`|30|0～1000|Speedの相対Weight。|
|`Base Upgrade Draw Weights.Strength`|20|0～1000|Strengthの相対Weight。|
|`Base Upgrade Draw Weights.Range`|30|0～1000|Rangeの相対Weight。|
|`Base Upgrade Draw Weights.Launch`|10|0～1000|Launchの相対Weight。|
|`Base Upgrade Draw Weights.TumbleClimb`|10|0～1000|Tumble Climbの相対Weight。|
|`Base Upgrade Draw Weights.TumbleWings`|10|0～1000|Tumble Wingsの相対Weight。|
|`Base Upgrade Draw Weights.CrouchRest`|30|0～1000|Crouch Restの相対Weight。|
|`Base Upgrade Draw Weights.MapPlayerCount`|10|0～1000|Map Player Countの相対Weight。|
|`Base Upgrade Draw Weights.DeathHeadBattery`|10|0～1000|Death Head Batteryの相対Weight。|
|`Base Upgrade Draw Weights.AllUpgrades`|1|0～1000|正の結果で使う`ALL UPGRADES`の相対Weight。|

抽選Weightの初期値はStaminaが40、Speed・Range・Crouch Restが30、Health・Strengthが20、その他の対象アップグレードが10、ALL UPGRADESが1です。大きい値ほど選ばれやすくなります。上限に近づくほどWeightが低下し、上限では`CappedUpgradeWeightMultiplier`の倍率になります。ショップの商品数の設定は影響しません。

#### 役職の抽選設定

各通常役職にはホスト設定`<役職名>.Enabled = true`と`<役職名>.Weight = 100`（範囲0～1000）があり、OFFまたは重み0でランダム抽選から除外します。Courierの設定名は`Courier`です。隠し役職は`???1.Enabled`と`???2.Enabled`で、初期値はともにtrue、重みは固定です。ON/OFFは次の抽選から適用。Twinsは役職重複なしの設定でも2人1組として抽選します。

#### アップグレード・特殊役職設定

この表はすべてホスト設定。

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`Tank.HealthUpgradeLevels`|21|0～200|Tankに保証する最低Healthレベル。|
|`Runner.SpeedUpgradeLevels`|6|0～200|Runnerに保証する最低Speedレベル。|
|`Runner.StaminaUpgradeLevels`|46|0～200|Runnerに保証する最低Staminaレベル。|
|`Jumper.ExtraJumpUpgradeLevels`|10|0～200|Jumperへ付与するExtra Jumpレベル。|
|`Launcher.LaunchUpgradeLevels`|10|0～200|Launcherへ付与するLaunchレベル。|
|`Climber.ClimbUpgradeLevels`|50|0～200|Climberへ付与するTumble Climbレベル。|
|`Climber.RangeUpgradeLevels`|20|0～200|Climberへ付与するRangeレベル。|
|`Flyer.WingsUpgradeLevels`|10|0～200|Flyerへ付与するTumble Wingsレベル。|
|`Tracker.HealthUpgradeLevels`|3|0～200|Healthの最低レベル。より高い基礎強化を維持。|
|`Tracker.EnemyDetectionRange`|25|1～100|壁越しを含む敵の検知距離（m）。|
|`Tracker.DangerAlertRange`|8|0～100|接近警告の距離（m）。検知距離が上限、0で警告無効。|
|`Tracker.NotificationIntervalSeconds`|20|5～120|通常通知の最短間隔（秒）。接近警告は通常の待ち時間を無視しますが、警告同士には同じ間隔を設けます。|
|`Tracker.DistanceChangeThreshold`|5|1～50|更新する距離変化（m）。対象変更、方向が60度変化、上下の変化でも更新。|
|`Ghost.DeathHeadBatteryUpgradeLevels`|50|0～200|Ghostへ付与するDeath Head Batteryレベル。|
|`Ghost.HealRadius`|5|0～30|頭からの回復範囲（m）。|
|`Ghost.HealAmount`|1|0～100|1回・1人あたりの回復HP。|
|`Ghost.HealIntervalSeconds`|2|0.5～60|死亡中の回復間隔（秒）。|
|`Ghost.TotalHealingLimit`|50|0～10000|味方全員で共有するステージごとの回復上限。蘇生では戻りません。|
|`Ghost.CarrierSpeedBonusLevels`|1|0～20|頭を持つ間のSpeed加算レベル。重複せず、King支援とは強い方を優先。|
|`Bomber.DistancePerGrenade`|8|1～100|グレネードを1個設置するために必要な移動距離。単位はメートル。|
|`Bomber.MaximumActiveGrenades`|30|1～30|Bomberごとに保持する生成済みグレネードの最大数。さらに設置すると最も古いものを削除する。|
|`Bomber.AllowTruckSpawns`|false|On/Off|トラック内でのグレネード設置を許可する。|
|`Bomber.ExplosiveGrenadesEnabled`|true|On/Off|ランダム抽選へバニラのExplosive Grenadeを含めます。|
|`Bomber.StunGrenadesEnabled`|true|On/Off|ランダム抽選へバニラのStun Grenadeを含めます。|
|`Bomber.ShockwaveGrenadesEnabled`|true|On/Off|ランダム抽選へバニラのShockwave Grenadeを含めます。|
|`Bomber.DuctTapedGrenadesEnabled`|true|On/Off|ランダム抽選へバニラのDuct Taped Grenadeを含めます。|
|`Medic.HealAmount`|5|1～100|1回につき周囲の生存中の仲間を回復するHP。本人は対象外。|
|`Medic.HealIntervalSeconds`|2|0.1～30|Medicの回復間隔。|
|`Medic.HealRadius`|5|1～30|回復可能な最大距離。単位はメートル。|
|`Medic.TotalHealingLimit`|150|1～10000|Medic一人が1ステージで回復できる合計HP。対象が実際に失っているHPだけを消費する。|
|`Phoenix.ReviveDelaySeconds`|2|2～10|復活までの最低待ち時間。頭が動いている間は、さらに待機する。|
|`Phoenix.RevivalHealth`|25|1～1000|Phoenix復活後のHP。プレイヤーの最大HPが上限。|
|`Courier.Damage`|1|1～100|トラック外で1回ごとに受けるダメージ。|
|`Courier.DamageIntervalSeconds`|0.1|0.05～10|トラック外でダメージを受ける間隔。|
|`Rescuer.ReviveDelaySeconds`|2|0～10|死亡後の最低待ち時間。復活できるようになるまで頭をつかみ続けてください。|
|`Rescuer.MaximumRevives`|2|1～10|Rescuer1人あたり、1ステージで復活できる最大回数。|
|`Rescuer.RevivalHealth`|25|1～1000|Rescuerによる復活後のHP。対象の最大HPが上限。|
|`Vampire.Tier1HealAmount`|5|1～100|周囲でDanger Level 1の敵が死亡した際の回復量。|
|`Vampire.Tier2HealAmount`|10|1～100|周囲でDanger Level 2の敵が死亡した際の回復量。|
|`Vampire.Tier3HealAmount`|50|1～100|周囲でDanger Level 3の敵が死亡した際の回復量。|
|`Vampire.Radius`|10|1～50|死亡した敵からの最大距離。単位はメートル。|
|`Tuna.StationaryDelaySeconds`|3|0.1～30|停止してからダメージが始まるまでの秒数。|
|`Tuna.Damage`|1|1～100|停止中に1回ごとに受けるダメージ。|
|`Tuna.DamageIntervalSeconds`|0.1|0.05～10|停止中にダメージを受ける間隔。|
|`Musician.HealAmount`|5|1～100|1回につき生存者1人を回復するHP。|
|`Musician.HealRadius`|10|1～50|回復範囲（m）。|
|`Musician.HealCooldownSeconds`|1|0.1～30|全楽器共通の回復間隔（秒）。|
|`Musician.TotalHealingLimit`|1000|1～10000|1ステージの合計回復HP。本人と味方で共用。HUDに残量を表示。|
|`Mage.CastIntervalSeconds`|3|0.1～30|魔法発動の最短間隔。|
|`Mage.BeamDurationSeconds`|5|0.1～30|ビーム魔法の継続秒数。手持ちの杖には適用しません。|
|`Mage.StarExpression`|`Angry`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`star`を発動する表情。|
|`Mage.RollExpression`|`Sad`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`roll`を発動する表情。|
|`Mage.GravityExpression`|`Suspicious`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`gravity`を発動する表情。|
|`Mage.VoidExpression`|`EyesClosed`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`void`を発動する表情。|
|`Mage.LaserExpression`|`Scared`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`laser`を発動する表情。|
|`Mage.AutoRecoveryEnabled`|true|On/Off|ダメージを受けていないMageのHP自動回復を有効化。|
|`Mage.AutoRecoveryDelaySeconds`|10|0～300|最後にダメージを受けてから自動回復が始まるまでの秒数。|
|`Mage.AutoRecoveryIntervalSeconds`|2|0.1～60|HPを自動回復する間隔。|
|`Mage.AutoRecoveryAmount`|1|0～100|自動回復1回あたりの回復量。|
|`Mage.AutoRecoveryTotalHealingLimit`|120|1～10000|プレイヤーごとの、1ステージ中の自動回復の累計上限。他の回復は数えず、死亡・復活・役職変更でも使用済みの回復量はリセットされません。|
|`Mage.StarHealthCost`|10|0～100|`star`の発動成功後に消費するHP。|
|`Mage.GravityHealthCost`|10|0～100|`gravity`の発動成功後に消費するHP。|
|`Mage.RollHealthCost`|15|0～100|`roll`の発動成功後に消費するHP。|
|`Mage.VoidHealthCost`|30|0～100|`void`の発動成功後に消費するHP。|
|`Mage.LaserHealthCost`|50|0～100|`laser`の発動成功後に消費するHP。|
|`Gambler.WinChancePercent`|50|0～100|Valuableを破壊せず、勝利倍率を適用する確率。|
|`Gambler.WinValueMultiplier`|2|0～10|勝利時にValuableへ適用する価格倍率。|
|`Gambler.GambitGreenHealAmount`|50|25～1000|Gamblerに対するGambitの緑結果で回復する合計HP。|
|`Gambler.GambitRedDamage`|100|50～1000|Gamblerに対するGambitの赤結果で受ける合計ダメージ。|
|`Gambler.GambitWhiteHealthUpgradeLevels`|5|0～200|Gambitの白結果で、そのステージ中のみ追加するHealthアップグレードレベル。|
|`Hunter.WeaponBatteryConsumptionPercent`|75|0～100|Hunterが使用する武器バッテリー消費量の通常時に対する割合。|
|`Hunter.DoubleOrbChancePercent`|10|0～100|Hunterが敵を倒した際、特賞に外れた場合に通常のオーブ個数を2倍にする確率。|
|`Hunter.JackpotOrbChancePercent`|0.5|0～100|Hunterが敵を倒した際、特賞目標を使用する確率。不足分は追加しますが、通常ドロップが目標を上回る場合は減らしません。|
|`Hunter.JackpotOrbCount`|10|1～30|Hunterの特賞で使用する目標オーブ個数。|
|`Hunter.GuaranteedOrbEveryKills`|5|0～100|追加オーブを保証する対象撃破数。0で保証なし。抽選の追加分を含む。|
|`Stinker.DistancePerCloud`|2|1～100|ウラン雲の通過地点を記録する間隔。|
|`Stinker.MinimumSafetyDistance`|2|1～30|最新の発生待ち地点にウラン雲を発生させるために必要なStinker本人との最低距離。古い待機地点は順番に発生する。|
|`Stinker.AllowTruckSpawns`|false|On/Off|トラック内でウラン雲の地点記録と発生を許可する。|
|`Trickster.ActiveSeconds`|25|1～120|設置したデコイを削除するまでの秒数。|
|`Trickster.DecoyExpression`|`Happy`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|デコイを設置する表情。|
|`Trickster.CooldownSeconds`|45|1～300|敵を引きつけたデコイの終了後、次に設置できるまでの秒数。|
|`Trickster.NoTargetCooldownSeconds`|10|0～300|敵を一体も引きつけなかったデコイの終了後、次に設置できるまでの秒数。|
|`Trickster.InvestigateRadius`|40|1～100|デコイを中心に敵へ調査を指示する半径。単位はメートル。|
|`Trickster.PulseIntervalSeconds`|1|0.25～10|敵へ調査を指示する間隔。|
|`Trickster.PlacementDistance`|2|0.5～10|Tricksterの前方へデコイを設置する距離。単位はメートル。|
|`Mechanic.RepairPercentPerSecond`|2|0～100|Mechanicが破損Valuableを直接保持している間、毎秒修復する元価格に対する合計割合。|
|`Mechanic.MaximumRepairPercentPerStage`|50|0～100|Mechanic一人が1ステージで修復できる元価格に対する合計割合。|
|`Electrician.ChargePercentPerSecond`|10|0～100|Electricianが使用していない充電可能アイテムを直接保持している間、毎秒回復する合計バッテリー割合。|
|`Electrician.MaximumChargePercentPerStage`|100|0～1000|Electrician一人が1ステージで回復できる合計バッテリー割合。|
|`Warden.AdditionalStunSeconds`|3|0～30|Wardenが敵へ発生させたスタンに追加する秒数。|
|`Ninja.VisionRecognitionMultiplier`|2|1～10|敵が視覚でNinjaを認識するまでの時間へ適用する倍率。|
|`Executioner.StunnedDamageMultiplier`|2|1～10|攻撃前からスタンしている敵への直接攻撃ダメージ倍率。|
|`Rider.EnemyDamageMultiplier`|3|1～10|Riderが運転中の車両が敵へ与える衝突ダメージ倍率。|
|`Rider.PlayerKnockbackMultiplier`|2|1～10|プレイヤーへ元から発生する車両Tumbleノックバック倍率。|
|`Influencer.PlayerRadius`|20|1～100|周囲の生存中の仲間を数える半径。単位はメートル。|
|`Influencer.PlayerCheckIntervalSeconds`|0.5|0.1～10|周囲人数と動的アップグレードを確認する間隔。|
|`Influencer.NoiseRadiusMultiplier`|2|1～10|足音、着地音、VC、チャットTTSの敵探知範囲倍率。|
|`Influencer.TTSInvestigateRadius`|20|1～100|定期TTSが発生させる敵探知範囲。|
|`Influencer.MinimumTTSIntervalSeconds`|20|1～300|定期TTS間隔を抽選する最小秒数。|
|`Influencer.MaximumTTSIntervalSeconds`|40|1～300|定期TTS間隔を抽選する最大秒数。最小値より小さい場合は、小さい方を最小値として使用する。|
|`Influencer.TTSQuietPeriodSeconds`|1.5|0～10|本人のTTS終了後、定期発言までに必要な無音時間。|
|`Influencer.<Upgrade>UpgradeScaling`|初期値表参照|`人数:レベル`の組|周囲人数に応じた最低目標値。役職前の値は下げず、空欄は付与なし。|
|`Werewolf.PlayerDamageMultiplier`|2|1～10|Werewolfが他のプレイヤーへ与える特定可能なダメージ倍率。|
|`Berserker.<Upgrade>UpgradeScaling`|初期値表参照|`HP割合:レベル`の組|HPがしきい値以下の最低目標値。役職前の値は下げず、空欄は付与なし。Healthは設定対象外。|
|`Bodyguard.HealthUpgradeLevels`|11|0～200|BodyguardのHealth目標値。|
|`Bodyguard.ProtectionRadius`|15|1～100|仲間を保護できる最大距離。単位はメートル。|
|`Bodyguard.DamageSharePercent`|50|0～100|BodyguardのHPを1残して肩代わりする敵ダメージ割合。|
|`Rammer.TumbleAttackDamage`|100|0～100000|RammerのTumble Attackが敵へ与えるダメージ。|
|`Rammer.SelfDamage`|15|0～100000|RammerのTumble Attackが敵へ命中した後、自身が受けるダメージ。|
|`Diver.UnderfloorDurationSeconds`|10|1～120|Diverが死亡せず床下に滞在できる最大秒数。|
|`Diver.MovementForce`|8|1～30|Diverが床下を自由移動するときの力。|
|`Sniper.ReferenceDistance`|6|1～50|敵へのダメージが1倍になる距離。単位はメートル。|
|`Sniper.MinimumDamageMultiplier`|0.5|0～1|密着時の敵ダメージ倍率。元が正のダメージなら最低1ダメージを与えます。|
|`Sniper.MaximumDamageMultiplier`|2|1～10|遠距離での敵ダメージの最高倍率。|
|`Sniper.MaximumMultiplierDistance`|18|1～100|最高倍率に達する距離。実効下限は`ReferenceDistance + 0.1` m。|
|`Avenger.DamageMultiplier`|1.5|1～10|他のプレイヤーが死亡した後の、敵へのダメージ倍率。|
|`Avenger.DurationSeconds`|30|1～120|敵へのダメージ増加が続く秒数。別の死亡で残り時間を更新する。|
|`Avenger.TriggerRadius`|30|1～100|Avengerが発動する、死亡したプレイヤーからの最大距離。|
|`Avenger.AllyHitDamageThreshold`|15|1～1000|敵の一撃で失うHPの発動閾値。|
|`Avenger.AllyHitTriggerRadius`|15|1～100|被弾した味方までの最大距離（m）。|
|`Avenger.AllyHitDamageMultiplier`|1.25|1～10|被弾反応の対敵ダメージ倍率。強い効果を優先。|
|`Avenger.AllyHitDurationSeconds`|10|1～120|被弾反応の持続時間（秒）。|
|`Avenger.AllyHitCooldownSeconds`|20|1～300|被弾反応のクールダウン（秒）。死亡反応は待たずに発動。|
|`Brawler.MeleeDamageMultiplier`|1.25|0～10|攻撃者を特定できる近接武器が敵とプレイヤーへ与えるダメージ倍率。|
|`Brawler.RangedDamageMultiplier`|0.75|0～10|攻撃者を特定できる銃、杖の弾、レーザーが敵とプレイヤーへ与えるダメージ倍率。|
|`Brawler.ComboMultiplierStep`|0.25|0～5|同じ敵への命中ごとに増える倍率。味方への攻撃は増加なし。|
|`Brawler.ComboMaximumMultiplier`|2|0～10|連撃の倍率上限。開始時の近接倍率を下回りません。|
|`Brawler.ComboTimeoutSeconds`|4|0.1～30|命中せずに連撃が途切れるまでの秒数。対象変更でもリセット。|

基礎アップグレードの初期値はHealthが1、ほかが0です。役職のアップグレード設定は、その役職で使用するレベルを表し、基礎値への加算ではありません。InfluencerとBerserkerは条件が変わっても元の高い強化を維持。`Tracker`は高い基礎強化を維持し、基礎値が高くても抽選されます。`Rammer`はステージ中のLaunch・Tumble Climb・Tumble Wingsを0にします。Throwには影響しません。
