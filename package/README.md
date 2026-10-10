# RoleShuffle

## English

### Overview

RoleShuffle assigns stage-long movement, healing, combat and other roles. Base Upgrades persist between stages and can grow during the run.

Only the host needs RoleShuffle for gameplay, with up to 30 players. Vanilla guests receive roles, upgrades, effects and chat/TTS announcements. Installed participants also get the full role HUD.

Upgrade items are removed from shops by default; roles grant temporary upgrades.

### Contact

Questions, bugs and feedback: [GitHub Issues](https://github.com/CapacityDown/RoleShuffle/issues). Requires a GitHub account.

### Requirements

- BepInExPack 5.4.2305 or a later compatible version
- REPOConfig 1.2.6 or a later compatible version
- MenuLib 2.5.2 or a later compatible version

### Installation

Install with a mod manager or place `RoleShuffle.dll` in the profile's `BepInEx/plugins`.

### Multiplayer

- The host assigns roles and controls gameplay settings.
- Vanilla participants do not need RoleShuffle.
- Installed participants receive the full role HUD and the scrollable `Roles` page in the Escape menu; each player can choose their own HUD layout.
- Mage and Trickster do not activate abilities when an expression is cleared. The host's own menu expression restoration is also ignored; other players' expressions restored after closing a menu can still activate an ability.

### Role selection and presets

Open `ROLES` → `ROLE SETTINGS` from the lobby or Escape menu. Hosts toggle any role, including secret roles, for future assignments and arrivals. Current roles stay. These switches share MOD settings; participants can only view them. Older hosts may not support them.

`PRESETS` replaces role switches only. Weights, abilities, balance, Base Upgrades and HUD settings stay. Weight 0 stays excluded. Manual selections show `Custom` and remain until applying a preset.

|Preset|Enabled roles|Play style|
|---|---|---|
|Standard|46|Every role, including both secret roles; restores the default ON/OFF selection.|
|Beginner|21|Upgrades, recovery and protection; Signalman/Porter. Excludes passive hazards and Hardship.|
|Cooperative|19|Team support, healing, repairs and shared survival; Signalman/Twins/Porter.|
|Chaos|16|Explosions, magic, gambles and unpredictable effects; Twins.|
|Challenge|15|Risky and specialized roles without dedicated healing or revival roles.|

Party-size, context and balance restrictions apply; presets preserve `General.Enabled` and zero weights.

### Role command

- Enter `/roles` in the in-game chat to announce only your assigned role name.
- Vanilla participants can use this command when the host has RoleShuffle installed.
- Outside an active stage, the response is `YourRole:Unavailable`.

### How roles are assigned

- One role is selected for each player at the start of a playable stage.
- Enabled roles are selected by relative `Weight`.
- `General.UniqueRoles = true` avoids duplicates until all eligible roles have been used.
- `King` is assigned to at most one player per stage.
- Showcase and Support groups have configurable minimum assignments by party size; see Role balance.
- Default minimums: Showcase and Support each reach four at 24 players. Danger maximum: one, two at 8 players, three at 16. Hardship maximum: one, two at 12. Parties of four or fewer get at most one role across both risk groups. Influencer is Showcase, outside Danger.
- Roles in a player's last five assignments use half weight for that player; others' histories have no effect.
- No consecutive repeat roles. After `Courier`, `Tuna`, or `Influenza`, that player avoids all three for two stages. Restrictions relax only as needed to assign everyone a role.
- `Jumper`, `Launcher`, `Climber`, and `Flyer` are excluded from random assignment whenever any matching base upgrade target is equal to or higher than that role's configured target, including increases from truck draws.
- `Influencer` is excluded from random assignment when none of the upgrade targets reachable with the current party size exceed the current Base Upgrades.
- `Tracker`, `Ghost`, `Medic`, `Courier`, `Rescuer`, `Influencer`, `Werewolf`, `Bodyguard`, `Imitator`, `Avenger`, `Influenza`, `Signalman`, and `Twins` are not selected in single-player or a one-player session.
- `Imitator` requires another active player with a copyable role.
- By default, context-dependent roles are excluded when their ability has no usable target. This includes `Musician`, `Engineer`, `Electrician`, and `Rider` when their required object is absent, `Sniper` when neither a melee weapon nor a gun is available, and `Brawler` when no melee weapon is available. Weapon-like valuables do not count as weapons for either role's assignment, and staffs do not count for Sniper assignment.
- Leaving removes a player from the role list. Returning restores their role; new arrivals receive a role, upgrades and announcement.
- Roles and effects end with the stage. They stay if revival cancels a failed-stage transition.

### Roles

|Role|Function and default effect|Important limitation or risk|Configurable values|
|---|---|---|---|
|![][i01]<br>Tank|Raises maximum HP to 1.5 times Base, up to the 4,100 HP growth limit.|Health minimum 21.|Minimum, multiplier, cap|
|![][i02]<br>Runner|Raises sprint speed and stamina to 1.5 times Base, with growth limits of 205 and 2,040.|Minimum Speed 6, Stamina 46.|Minimums, multipliers, caps|
|![][i03]<br>Jumper|Adds up to 10 extra jumps before landing by setting Extra Jump to level 10.|Vanilla upgrades; no separate active ability.|Extra Jump target 0–100|
|![][i04]<br>Lifter|Heavy objects, carts and bikes use peak lifting/turning strength from Strength Lv0–200.|Strength displays Lv200. Excluded if Base meets both strength targets (default: Lv50).|Holding strength|
|![][i05]<br>Launcher|Launches the player farther forward when starting a Tumble. Launch is level 10.|Vanilla upgrades; no separate active ability.|Launch target 0–100|
|![][i06]<br>Climber|Improves Tumble climbing and allows objects to be grabbed from farther away. Tumble Climb is level 50 and Range is level 20.|Vanilla upgrades; no separate active ability.|Tumble Climb and Range targets 0–100|
|![][i07]<br>Flyer|Keeps Tumble Wings active longer for extended movement through the air. Tumble Wings is level 10.|Vanilla upgrades; no separate active ability.|Tumble Wings target 0–100|
|![][i08]<br>Tracker|Passively reports the nearest enemy within 25m, warns within 8m, and tracks the nearest teammate's Death Head at any distance. Health minimum 3; Map Player Count minimum 1; preserves stronger Base upgrades.|Private reports only on meaningful changes, normally 20s apart. Close warnings bypass the wait without repeating while danger stays nearby. Straight-line directions relative to your view, including above/below. No item or command needed; requires 2+ players.|Health 0–200; detection, warning, interval and distance-change settings below.|
|![][i09]<br>Ghost|Death Head Battery Lv50. While dead, heals living allies within 5 m of its head for 1 HP every 2 seconds, up to 50 HP per stage. Head carriers gain Speed +1.|Revival does not replenish healing. Multiple heads do not stack; the stronger King Speed bonus takes priority. Excluded with one player; high Base levels do not exclude it.|Battery, healing, carrier Speed|
|![][i10]<br>Bomber|Drops a random armed grenade every 8 m traveled. Up to 30 generated grenades remain active per Bomber, and the oldest is removed at the limit.|Grenades can injure players and damage valuables. Only Bomber can hold generated grenades. Movement inside the truck does not count while truck placement is disabled.|Distance, limit, truck placement, grenade types|
|![][i11]<br>Medic|Heals nearby teammates for 5 HP every 2 seconds within 5 m, up to 150 total HP per stage.|Never heals the Medic. Only living teammates within range are affected. Healing stops when that Medic's stage limit is exhausted. Never selected randomly when only one player is present.|Amount, interval, radius, total limit|
|![][i12]<br>Phoenix|Automatically revives itself with 25 HP once per stage after dying.|Requires a stationary Death Head. An unused revival prevents game over without a time limit. Revival HP cannot exceed maximum HP.|Revival HP, revival delay|
|![][i13]<br>Courier|Carry a valuable worth money 5 m while holding it outside delivery areas, then place it in the truck or an extraction point. Delivery heals HP and pauses HP loss by size (below).|30-second pause at stage start/revival. Otherwise loses 1 HP/0.1 s outside the truck; can die. Other damage still applies. Unlimited deliveries; each item rewards each player once per stage. Early drops reset distance. Not selected solo.|Delivery and HP loss|
|![][i14]<br>Rescuer|While alive, grab a dead teammate's Death Head to revive them with 25 HP, even while the head is moving.|Keep holding until revival; releasing while waiting cancels the rescue. Up to 2 revivals per stage. Revival HP cannot exceed the target's maximum HP. Never selected randomly when only one player is present.|Revival HP, delay, maximum revivals|
|![][i15]<br>Vampire|Heals when an enemy dies within 10 m: Tier 1 = 5, Tier 2 = 10, Tier 3 = 50.|Uses the enemy's vanilla Danger Level. When Enhanced enemy rewards are enabled with Elite Enemy Variants, Enhanced enemies count one tier higher up to Tier 3. The Vampire must be alive and close to the dying enemy.|Amount per tier, radius|
|![][i16]<br>King|Crown; grants nearby allies Speed/Range +2 and up to Strength +5 within 12 m.|No self-buff or stacking. Level cap 200; Strength cannot weaken grip/rotation. Leaving removes only King bonuses. One King per stage.|Radius and bonus levels|
|![][i17]<br>Tuna|After a 5-second grace period at stage start, standing still for 3 seconds causes 1 damage every 0.1 seconds until moving.|Can kill the player. The stationary timer starts after the initial grace period. Moving resets it immediately; lost health is not restored.|Delay, damage, interval|
|![][i18]<br>Musician|Each instrument note played by the Musician heals the Musician and living players within 10 m for 5 HP.|Requires a vanilla musical valuable. By default, not randomly selected if none is present.|Amount, radius|
|![][i19]<br>Mage|Uses `star`, `gravity`, `roll`, `void`, or `laser` in chat, or the configured facial expressions, to cast vanilla attacks for 10, 10, 15, 30, or 50 HP. After 10 seconds without damage, restores 1 HP every 2 seconds, up to 120 HP total per stage. Magic cast using a held staff lasts 1.3 times as long.|Matching expressions cast spells; clearing them does not. Spells share a 3-second cooldown, require surviving the HP cost, and can harm players or valuables. The staff duration bonus excludes chat/expression casts and the Star Wand's instant attack.|Facial expression per spell, cast cooldown, health cost per spell, automatic recovery toggle, delay, interval, amount, and stage limit|
|![][i20]<br>Gambler|A held valuable has a 50% chance to double its current value; otherwise it is destroyed. Gambit green heals 50 HP, red deals 100 damage, black kills, and white fully heals and adds 5 Health levels for the stage.|One wager is available at a time and returns after extraction. Damaged valuables use their reduced value.|Win chance, win multiplier, Gambit green heal, red damage, white temporary Health levels|
|![][i21]<br>Hunter|Weapons use 75% battery. Personal kills of orb-dropping enemies have a 10% chance of double drops and a 0.5% chance of 10 orbs. Every 5 qualifying kills guarantees at least 1 extra orb.|Random extras satisfy the guarantee. Count resets each stage. Only held/equipped weapons and confirmed personal kills qualify. Enhanced rewards affect extra-orb quality only.|Battery, orb chances/count, guarantee interval|
|![][i22]<br>Stinker|Leaves a harmful uranium cloud after every 2 m traveled. Clouds appear one at a time after Stinker moves at least 2 m away from the newest location. Spawned uranium valuables have a 0.5-second grace period before breaking.|Clouds can damage other players, and Stinker can be hurt by walking back into one. Truck placement is disabled by default.|Distance, safety distance, truck spawning, break delay|
|![][i23]<br>Engineer|Prevents supported effect valuables from activating while the Engineer holds them.|Camera, Propane Tank, Snowmobile, Flashlight, Clown Doll, and Love Potion are not affected. By default, not randomly selected if no supported effect valuable is present.|Selection only|
|![][i24]<br>Trickster|Type `decoy` in chat or select the configured facial expression to place a fixed Scream Doll that repeatedly attracts enemies within 40 m for 25 seconds.|Another decoy cannot be placed while one is active. Its cooldown begins after the decoy ends: 45 seconds if it attracted an enemy, or 10 seconds if it attracted none. The decoy cannot be grabbed, damaged, or delivered.|Decoy facial expression, active time, normal and no-target cooldowns, radius, pulse interval, placement distance|
|![][i25]<br>Mechanic|While directly holding damaged valuables, restores a shared 2% of original sale value per second, up to 50 percentage points per stage.|Only value that is actually restored counts toward the stage limit. Fully repaired and non-valuable objects are ignored. Multiple held valuables share the same repair rate and stage limit. The sale value is restored; damaged visual appearance may remain.|Repair rate, total stage limit|
|![][i26]<br>Electrician|While directly holding inactive rechargeable items, restores a shared 10 battery percentage per second, up to 100 percentage points per stage.|Active, full, and unchargeable items are ignored. By default, not randomly selected if no rechargeable item is present.|Charge rate, total stage limit|
|![][i27]<br>Warden|Extends enemy stuns caused by Warden's weapons, grenades, or held damaging objects by 3 seconds.|The attack must normally be able to stun the enemy.|Additional stun time|
|![][i28]<br>Ninja|Footsteps, landings, voice chat, and chat TTS do not draw enemy investigation. Enemies take twice as long to recognize Ninja visually.|Close contact and noises from weapons, valuables, impacts, explosions, or hazards can still reveal Ninja.|Visual recognition multiplier|
|![][i29]<br>Executioner|Deals 2x direct attack damage to enemies that were already stunned before the hit.|The hit that initially causes the stun receives no bonus. Collisions alone do not receive the bonus.|Stunned-enemy damage multiplier|
|![][i30]<br>Rider|While driving a vanilla vehicle, deals 3x impact damage to enemies and doubles existing Tumble knockback against players.|Player damage is not increased. By default, not randomly selected if no vanilla vehicle is present.|Enemy damage and player knockback multipliers|
|![][i31]<br>Influencer|Gains stronger upgrades as more living teammates enter a 20 m radius and periodically speaks an English TTS line.|Its footsteps, landings, VC, and chat TTS reach enemies from twice as far away.|Radius, check rate, per-upgrade player-count scaling, noise multiplier, TTS timing and radius|
|![][i32]<br>Werewolf|Deals 2x damage to another player when Werewolf can be identified as the attacker.|Self-damage, environment damage, and damage with no identifiable player attacker are unchanged. Never selected randomly when only one player is present.|Player damage multiplier|
|![][i33]<br>Berserker|Gains stronger upgrades as remaining HP becomes lower.|Bonuses decrease again when healed.|Per-upgrade HP-percentage scaling|
|![][i34]<br>Bodyguard|Sets Health to level 11 and absorbs 50% of enemy damage dealt to a teammate within 15 m.|Transferred damage cannot reduce Bodyguard below 1 HP.|Health target, radius, damage share|
|![][i35]<br>Rammer|Tumble Attack deals 100 damage to enemies and deals 15 damage to Rammer after a successful hit.|Launch, Tumble Climb, and Tumble Wings are fixed at level 0 for the stage regardless of Base Upgrades.|Tumble Attack and self damage|
|![][i36]<br>Diver|Tumbling while continuing to look down at a fixed floor enables movement below it for up to 10 seconds.|Failing to return through a floor in time kills Diver. Returning above the floor starts a cooldown equal to 1.5 times the previous dive time. The countdown and ready state are announced by TTS.|Underfloor duration, movement force|
|![][i37]<br>Sniper|Enemy damage scales with distance: 0.5x at point-blank range, 1x at 6 m, and up to 2x at 18 m.|Requires an identifiable attacker. Vehicle impacts and Tumble Attacks are unchanged.|Distances and multipliers|
|![][i38]<br>Imitator|Starts with Base Upgrades only. Grabbing another player's health-transfer point copies that teammate's upgrades, abilities, and drawbacks for the rest of the stage. The assignment list then shows the copied role beside Imitator.|Cannot copy Imitator, King, Bomber, Stinker, Werewolf, Courier, Tuna, Influenza, Signalman, Twins, or ???1 or ???2; announces `CannotCopy` on an ineligible attempt. The first valid copy is fixed for the stage. Never selected randomly when only one player is present.|Selection only|
|![][i39]<br>Avenger|An ally losing at least 15 HP to one enemy hit within 15 m grants 1.25x enemy damage for 10 seconds (20-second cooldown). An ally dying within 30 m grants 1.5x for 30 seconds.|Activation and expiration are announced. Only the stronger active bonus applies. Deaths refresh their bonus without waiting for the hit cooldown. Self-damage and friendly fire do not trigger the hit bonus. Own death does not activate it. Excluded with one player.|Hit/death range, threshold, multipliers, durations, cooldown|
|![][i40]<br>Brawler|Melee hits on the same enemy rise from 1.25x by 0.25 per hit, up to 2x. Guns, staff projectiles and lasers deal 0.75x.|Changing enemy or 4 seconds without a hit resets the combo. Friendly fire stays at 1.25x. Vehicle impacts, Tumble Attacks, grenades and ordinary objects are unchanged.|Melee/ranged multipliers, combo step/cap/timeout|
|![][i41]<br>Influenza|**Onset:** 30 seconds after becoming Influenza, maximum HP is fixed at 75; only symptomatic players spread it.<br>**Sneezing:** automatic every 30–90 seconds (about once a minute). Each teammate in front within 5m and 20° to either side has a 60% infection chance.<br>**Voice/chat:** after onset, each utterance or message gives each teammate in front within 3m and 30° to either side a 30% chance.|**If infected:** the teammate immediately becomes Influenza, losing their previous role. They develop symptoms after 30 seconds and can then infect others.<br>**Enemy attention:** sneezes can be heard in every direction (base radius 5m; varies with enemy hearing).<br>Death/revival does not reset the timer. Infection ends with the stage. Excluded from solo draws.|Onset, HP, sneeze intervals, infection range/angle/chance, hearing|
|![][i42]<br>Signalman|Relays ordinary chat unchanged to every living teammate at any distance, in each receiver's own TTS voice. Receives private teammate death alerts. Radio cooldown: 20 seconds.|Commands and automatic notices are excluded. Cooldown posts stay ordinary chat and are never relayed later. Waits for each receiver; countdowns have priority. Drops messages after 5 seconds waiting. At least two players; at most one drawn. Normal VC range is unchanged.|Cooldown, waiting limit, death alerts, command exclusions|
|![][i43]<br>Twins|Two players share their combined current and maximum HP. Carrying the same valuable together gives 1.5x grab strength and halves collision value loss. Both crouching still within 3 m for 3 seconds restores 10% of shared maximum HP and all stamina, with a shared 60-second cooldown. Sprinting toward a partner separated by 15 m gives about 1.25x speed until within 5 m. Co-carry a valuable outside delivery areas for 3 seconds, then deliver with both players within 5 m for +10% value, once per item, up to $5,000 per stage.|At least two players; at most one fixed pair. Either death kills both; one revival revives both using one revival’s HP amount. Infection turns both into Influenza and ends sharing. Stage end or a partner leaving ends the link and preserves remaining HP proportion. No HP transfers within the pair. Cannot be copied; shop equipment gets no carry bonus.|Selection, carry, rest, reunion and delivery|
|![][i44]<br>Porter|Solo-hold a Tiny/Small/Medium valuable for 3 s outside delivery areas to store it. Capacity 15 by weight, no count limit. In delivery areas or aboard a cart, unload in 10 s at full load or 5 s at half.|Enemy HP damage scatters all cargo. Speed levels fall with weight (rounded down): 0 at full load, restored on unloading. Leaving or spilling resets the wait. Death/role change/disconnect/stage end returns cargo. No shop gear. Private capacity/unload notices.|Capacity, sizes, hold/unload time, enemy-hit drop|
|![][iu]<br>???1|???|???|???|
|![][iu]<br>???2|???|???|???|

### Role growth and support settings

|Key|Default|Range|Effect|
|---|---|---|---|
|`Tank.HealthMultiplier` / `Tank.MaximumHealth`|1.5 / 4100|1–10 / 100–4100|Maximum HP|
|`Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed`|1.5 / 205|1–10 / 5–205|Sprint speed|
|`Runner.StaminaMultiplier` / `Runner.MaximumStamina`|1.5 / 2040|1–10 / 40–2040|Stamina capacity|
|`Courier.ContractDistance`|5|1–50 m|Carry distance outside delivery areas.|
|`Courier.InitialGraceSeconds`|30|0–300 s|HP-loss pause at stage start/revival.|
|`Courier.<Size>GraceSeconds`|`30/30/60/90/90/90/120`|0–300 s|Tiny/Small/Medium/Big/Wide/Tall/VeryTall. Never shortens remaining grace.|
|`Courier.<Size>HealAmount`|`10/25/50/100/100/100/100`|0–10000 HP|Same size order; fixed HP, capped at maximum health.|
|`King.SpeedBonusLevels` / `King.RangeBonusLevels`|2 / 2|0–200|Speed / Range bonus levels.|
|`King.StrengthBonusLevels`|5|0–200|Maximum safe Strength bonus levels.|
|`King.UpgradeRadius`|12|1–30 m|Upgrade aura radius.|
|`HUD.ResourceHudEnabled`|true|Boolean|Personal resource HUD at the top left, independent of the role list.|
|`HUD.ResourceHudScalePercent`|100|50–200|Resource HUD scale; fits the screen automatically.|
|`HUD.ResourceHudOffsetX` / `ResourceHudOffsetY`|0 / 0|0–3840 / 0–2160|Moves the resource display right or down from below stamina.|

Ability resources appear below stamina in one column and shrink to fit when necessary. The role list shows each player's role. The resource HUD requires the host's RoleShuffle version.

### Ability tuning

Host: REPOConfig → RoleShuffle → role name. Defaults are listed above. Strength, HP and infection checks use current settings. Incubation changes affect new infections; scheduled sneezes keep their deadlines. Stinker delay affects new valuables. Existing settings stay.

|Setting|Default|Range|Effect|
|---|---|---|---|
|`Porter.Capacity`|15|1–100|Total cargo weight limit; no item-count limit. HUD: weight, current cargo value and unloading time.|
|`Porter.HoldSeconds`|3|1–30 s|Continuous solo hold before storage.|
|`Porter.FullUnloadSeconds`|10|0–60 s|Full-load unloading time; scales with weight. 0 unloads immediately.|
|`Porter.DropOnEnemyHit`|true|Boolean|Enemy HP damage scatters all cargo.|
|`Porter.AllowedSizes`|Tiny,Small,Medium|Comma-separated: Tiny, Small, Medium, Big, Wide, Tall, VeryTall|Allowed sizes; empty/unknown entries allow no additional sizes.|
|`Lifter.HeavyGripMultiplier` / `Lifter.HeavyRotationMultiplier`|1 / 1|0.1–5|Lifting / turning strength relative to the maximum across Strength Lv0–200.|
|`Lifter.LightItemStrengthLevel`|1|0–200|Holding level for light objects/shop equipment, excluding carts/bikes.|
|`Stinker.BreakGraceSeconds`|0.5|0–10 s|Delay before a spawned uranium valuable breaks.|
|`Influenza.IncubationSeconds`|30|0–300 s|Delay from infection to symptoms.|
|`Influenza.MaximumHealth`|75|1–10000 HP|Maximum HP after onset. Increasing it does not heal.|
|`Influenza.SneezeMinimumSeconds` / `Influenza.SneezeMaximumSeconds`|30 / 90|1–600 s|Random interval; endpoints are sorted. Equal values give a regular interval.|
|`Influenza.SneezeRange` / `Influenza.SpeechRange`|5 / 3|0.1–30 m|Infection distance for sneezes / voice and text chat.|
|`Influenza.SneezeAngleDegrees` / `Influenza.SpeechAngleDegrees`|40 / 60|1–360°|Full infection angle. Half applies on each side.|
|`Influenza.SneezeChancePercent` / `Influenza.SpeechChancePercent`|60 / 30|0–100%|Chance per teammate per sneeze / utterance or message.|
|`Influenza.SpeechSilenceSeconds`|0.75|0.1–5 s|Silent gap that starts a new utterance.|
|`Influenza.SneezeNoiseRadius`|5|0–100 m|Enemy hearing distance, modified by enemy hearing. Zero disables the alert.|
|`Signalman.CooldownSeconds` / `Signalman.MaximumDelaySeconds`|20 / 5|1–300 / 1–30 s|Radio cooldown / maximum wait for a busy receiver.|
|`Signalman.DeathAlerts`|true|true, false|Private teammate death notices; also follows `Notifications.Enabled`.|
|`Signalman.ExcludedCommands` / `Signalman.ExcludedPrefixes`|`star,roll,gravity,void,laser,decoy` / `!`|Comma-separated|Extra exact commands / prefixes to exclude. Slash commands and exact built-in spell names are always excluded.|

### Configuration

All settings are available through REPOConfig. Host-controlled settings affect the session. Local settings affect only the installed player's HUD.

#### General, notifications, and HUD

|Key|Default|Range / values|Effect|Control|
|---|---:|---|---|---|
|`General.Enabled`|true|true, false|Enables stage role assignment.|Host|
|`General.UniqueRoles`|true|true, false|Avoids duplicates until every enabled eligible role has been assigned once.|Host|
|`General.ShopUpgradeItemCount`|0|0–30|Requests this number of upgrade items for each shop. 0 removes upgrade items from the shop pool.|Host|
|`Compatibility.EnhancedEnemyRewardsEnabled`|true|true, false|When enabled, Elite Enemy Variants Enhanced enemies count one reward tier higher for Vampire healing and Hunter bonus-orb quality.|Host|
|`Notifications.Enabled`|true|true, false|Enables role-name and role-effect chat and TTS notifications.|Host|
|`Notifications.DelaySeconds`|2.5|0–30|Delay before role announcements without Stage Flux.|Host|
|`Notifications.StageFluxDelaySeconds`|5|0–30|Minimum delay before role announcements when Stage Flux is installed. RoleShuffle also waits for Stage Flux TTS to finish and for a 1.5-second quiet period.|Host|
|`HUD.Enabled`|true|true, false|Shows the current roles during a stage.|Local|
|`HUD.RoleDisplay`|`NameOnly`|`IconAndName`, `NameOnly`, `IconOnly`|Shows role icons and names, names only, or icons only. Player names remain visible.|Local|
|`HUD.IconSize`|64|32–128|Role icon size before HUD scaling. The HUD grows for six rows and scales to fit the screen.|Local|
|`HUD.Anchor`|`BottomLeft`|`TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight`|Selects the HUD anchor.|Local|
|`HUD.Alignment`|`Left`|`Left`, `Center`, `Right`|Selects the role text alignment.|Local|
|`HUD.OffsetX`|0|`-3840`–3840|Horizontal offset from the anchor in pixels.|Local|
|`HUD.OffsetY`|80|`-2160`–2160|Vertical offset from the anchor in pixels.|Local|
|`HUD.ScalePercent`|70|50–200|HUD display scale percentage.|Local|
|`HUD.PlayersPerPage`|8|2–20|Maximum players shown at once, including the pinned local player. Values of 6 or more reserve room for at least six, even with taller fonts or icons.|Local|
|`HUD.PageIntervalSeconds`|5|1–30|Seconds each role page remains visible.|Local|
|`HUD.TransitionDurationSeconds`|0.2|0–1|Duration of the fade between role pages.|Local|
|`HUD.PinLocalPlayer`|true|true, false|Keeps the local player's role visible on every page.|Local|

#### Role balance

All settings below are host-controlled.

Showcase roles are `Bomber`, `Stinker`, `Mage`, `Gambler`, `Trickster`, `King`, `Rider`, `Influencer`, and `Diver`. Support roles are `Medic`, `Rescuer`, `Mechanic`, `Electrician`, `Warden`, `Bodyguard`, `Signalman`, and `Porter`. Danger roles are `Bomber`, `Stinker`, `Werewolf`, and `Influenza`; Hardship roles are `Courier`, `Tuna`, and `Influenza`. Influencer is not a Danger role. If these rules leave no eligible role, RoleShuffle gradually loosens the limits so every player can still receive a role.

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Role Balance - Guarantees.ShowcaseEnabled`|true|true, false|Enables party-size-based minimum Showcase assignments.|
|`Role Balance - Guarantees.ShowcaseMinimums`|`3:1,7:2,14:3,24:4`|`party size:minimum` pairs|Minimum Showcase assignments by party size.|
|`Role Balance - Guarantees.SupportEnabled`|true|true, false|Enables party-size-based minimum Support assignments.|
|`Role Balance - Guarantees.SupportMinimums`|`4:1,8:2,14:3,24:4`|`party size:minimum` pairs|Minimum Support assignments by party size.|
|`Role Balance - Limits.Enabled`|true|true, false|Enables Danger, Hardship, and small-party combined limits.|
|`Role Balance - Limits.DangerMaximums`|`1:1,8:2,16:3`|`party size:limit` pairs|Maximum simultaneous `Bomber`, `Stinker`, `Werewolf`, and `Influenza` roles by party size.|
|`Role Balance - Limits.HardshipMaximums`|`1:1,12:2`|`party size:limit` pairs|Maximum simultaneous `Courier`, `Tuna`, and `Influenza` roles by party size.|
|`Role Balance - Limits.SmallPartyMaximumPlayers`|4|1–30|Largest party size using the combined Danger and Hardship limit.|
|`Role Balance - Limits.SmallPartyCombinedMaximum`|1|1–30|Maximum combined Danger and Hardship roles in a small party.|
|`Role Balance - Variety.PreventSameRole`|true|true, false|Prevents the same player receiving the same role in consecutive stages when alternatives exist.|
|`Role Balance - Variety.HardshipCooldownStages`|2|0–10|Stages after `Courier`, `Tuna`, or `Influenza` during which that player is normally excluded from these roles.|
|`Role Balance - Variety.ExcludeUnavailableRoles`|true|true, false|Excludes context-dependent roles when their required enemy, weapon, or usable object is absent.|

#### Base upgrades

Item retention starts when enabled; earlier uses are not recovered. Saved levels add to role/base targets; Lifter/Rammer fixed levels and Influenza HP take priority. BASE UPGRADES excludes item bonuses; these appear in vanilla stats. To sell upgrades, set `General.ShopUpgradeItemCount` above 0.

All settings below are host-controlled.

Base targets stay active between stages and apply to upgrades not overridden by the current role. Use comma-separated `run level:value` pairs: `1:1,5:3,10:6` means 1 on levels 1–4, 3 on levels 5–9, and 6 from level 10. Run levels accept 1–999999. Blank settings and levels before the first entry use 0. Values are clamped to the allowed range; malformed pairs are ignored. Base settings and truck draws do not change Throw.

Truck draws occur after leaving the shop, during truck preparation. They select an upgrade and change amount using relative weights. Amounts that no eligible target can receive within its limits are excluded. Positive results can select `All Upgrades`, affecting enabled types with room below their limits. Results persist for the run and are announced by the host.

Open `ROLES` → `BASE UPGRADES` → `BASE UPGRADE SETTINGS` to toggle the draw and its 13 entries (12 types plus `All Upgrades`). These switches share the saved REPOConfig values. Hosts edit; installed participants view the host's settings. Apply pending REPOConfig edits before leaving its page; Roles UI saves immediately.

OFF excludes a type from future individual and `All Upgrades` draws without removing its base levels or acquired bonuses. An active draw keeps its starting selection. `No individual draw` means weight 0; an ON type can still receive `All Upgrades`. `No draw` on `All Upgrades` means its weight is 0. Disabling that entry affects only combined results. All-off selections produce no result.

Manual adjustments persist per save. Each +/- click changes the total by 1, within 0–200 (Map Player Count: 0–1). The host needs a save-supported lobby.

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Base Upgrades.KeepUpgradeItems`|false|true, false|Keep consumed levels in this save. Off stops recording and excludes saved amounts at the next role/base reset without deleting them; Throw remains permanent.|
|`Base Upgrades.UpgradeItemScope`|`Player`|`Player`, `AllPlayers`|New uses benefit the consumer or everyone, including later joiners. Past uses keep their original recipients.|
|`Base Upgrades.ManualAdjustmentEnabled`|false|true, false|Enables manual +/- in the lobby, truck and shop. Off hides manual controls and explanations and excludes saved adjustments; on restores them. Base rules and truck draws remain active.|
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
|`Base Upgrade Draw.Enabled`|true|true, false|Enables the shared Base Upgrade draw after leaving the shop.|
|`Base Upgrade Draw.MaximumLevel`|200|0–200|Maximum target that positive truck draws can reach. Map Player Count remains limited to 1.|
|`Base Upgrade Draw.CappedUpgradeWeightMultiplier`|0.25|0–1|Upgrade weights decrease as their combined configured and truck levels approach the draw maximum, reaching this multiplier at the maximum. 0 excludes capped upgrades; 1 disables the decrease. All Upgrades is unaffected.|
|`Base Upgrade Draw.WeightFalloffExponent`|2|0.1–10|Controls the weight decrease curve. 1 decreases evenly; larger values preserve more weight until near the maximum.|
|`Base Upgrade Draw.ChangeAmountWeights`|`-1:10,0:15,1:60,2:15`|`change amount:weight` pairs; amount `-200`–200, weight 0–1000|Relative weights for the possible change amounts.|
|`Base Upgrade Draw Selection.Health`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.Stamina`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.ExtraJump`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.Speed`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.Strength`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.Range`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.Launch`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.TumbleClimb`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.TumbleWings`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.CrouchRest`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.MapPlayerCount`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.DeathHeadBattery`|true|true, false|Include in draws.|
|`Base Upgrade Draw Selection.AllUpgrades`|true|true, false|Enables the combined positive result for enabled types only.|
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

All settings below are host-controlled.

|Key|Default|Range / values|Effect|
|---|---:|---|---|
|`Tank.HealthUpgradeLevels`|21|0–200|Minimum Health level guaranteed to Tank.|
|`Tracker.HealthUpgradeLevels`|3|0–200|Minimum Health level; preserves stronger Base upgrades.|
|`Tracker.EnemyDetectionRange`|25|1–100|Enemy detection distance (m), including through walls.|
|`Tracker.DangerAlertRange`|8|0–100|Close warning distance (m), capped at detection range. 0 disables warnings.|
|`Tracker.NotificationIntervalSeconds`|20|5–120|Minimum report interval (s). Close warnings bypass normal reports' wait, with their own same-length interval.|
|`Tracker.DistanceChangeThreshold`|5|1–50|Distance change (m) for an update. Target changes, turns of 60° or above/below changes also update.|
|`Runner.SpeedUpgradeLevels`|6|0–200|Minimum Speed level guaranteed to Runner.|
|`Runner.StaminaUpgradeLevels`|46|0–200|Minimum Stamina level guaranteed to Runner.|
|`Jumper.ExtraJumpUpgradeLevels`|10|0–200|Extra Jump levels granted to Jumper.|
|`Launcher.LaunchUpgradeLevels`|10|0–200|Launch levels granted to Launcher.|
|`Climber.ClimbUpgradeLevels`|50|0–200|Tumble Climb levels granted to Climber.|
|`Climber.RangeUpgradeLevels`|20|0–200|Range levels granted to Climber.|
|`Flyer.WingsUpgradeLevels`|10|0–200|Tumble Wings levels granted to Flyer.|
|`Ghost.DeathHeadBatteryUpgradeLevels`|50|0–200|Death Head Battery levels granted to Ghost.|
|`Ghost.HealRadius`|5|0–30|Healing radius around the head (m).|
|`Ghost.HealAmount`|1|0–100|HP per ally per pulse.|
|`Ghost.HealIntervalSeconds`|2|0.5–60|Healing interval (s) while dead.|
|`Ghost.TotalHealingLimit`|50|0–10000|Shared healing budget per stage; revival does not reset it.|
|`Ghost.CarrierSpeedBonusLevels`|1|0–20|Temporary Speed levels while carrying the head. No stacking; stronger King Speed aura wins.|
|`Bomber.DistancePerGrenade`|8|1–100|Travel distance in meters required for each grenade placement.|
|`Bomber.MaximumActiveGrenades`|30|1–30|Maximum generated grenades retained per Bomber. Placing another removes the oldest one.|
|`Bomber.AllowTruckSpawns`|false|true, false|Allows grenade placement inside the truck.|
|`Bomber.ExplosiveGrenadesEnabled`|true|true, false|Includes vanilla explosive grenades in the random selection.|
|`Bomber.StunGrenadesEnabled`|true|true, false|Includes vanilla stun grenades in the random selection.|
|`Bomber.ShockwaveGrenadesEnabled`|true|true, false|Includes vanilla shockwave grenades in the random selection.|
|`Bomber.DuctTapedGrenadesEnabled`|true|true, false|Includes vanilla duct-taped grenades in the random selection.|
|`Medic.HealAmount`|5|1–100|Health restored to each nearby teammate each time.|
|`Medic.HealIntervalSeconds`|2|0.1–30|Seconds between each round of Medic healing.|
|`Medic.HealRadius`|5|1–30|Maximum healing distance in meters.|
|`Medic.TotalHealingLimit`|150|1–10000|Maximum total health restored by each Medic per stage. Only health actually missing from a target consumes the limit.|
|`Phoenix.ReviveDelaySeconds`|2|2–10|Minimum seconds before revival; also waits for the Death Head to stop moving.|
|`Phoenix.RevivalHealth`|25|1–1000|Health after Phoenix revival, capped at their maximum health.|
|`Courier.Damage`|1|1–100|Damage each time outside the truck.|
|`Courier.DamageIntervalSeconds`|0.1|0.05–10|Seconds between damage outside the truck.|
|`Rescuer.ReviveDelaySeconds`|2|0–10|Minimum seconds after death. Keep holding the head until revival is ready.|
|`Rescuer.MaximumRevives`|2|1–10|Maximum revivals for each Rescuer per stage.|
|`Rescuer.RevivalHealth`|25|1–1000|Health after a Rescuer revival, capped at the target's maximum health.|
|`Vampire.Tier1HealAmount`|5|1–100|Health restored by a nearby Danger Level 1 enemy death.|
|`Vampire.Tier2HealAmount`|10|1–100|Health restored by a nearby Danger Level 2 enemy death.|
|`Vampire.Tier3HealAmount`|50|1–100|Health restored by a nearby Danger Level 3 enemy death.|
|`Vampire.Radius`|10|1–50|Maximum distance in meters from the dying enemy.|
|`Tuna.StationaryDelaySeconds`|3|0.1–30|Seconds without movement before damage begins.|
|`Tuna.Damage`|1|1–100|Damage per hit while stationary.|
|`Tuna.DamageIntervalSeconds`|0.1|0.05–10|Seconds between stationary damage.|
|`Musician.HealAmount`|5|1–100|Health restored to the Musician and each living player in range per instrument note.|
|`Musician.HealRadius`|10|1–50|Maximum healing distance in meters from the Musician.|
|`Mage.CastIntervalSeconds`|3|0.1–30|Minimum seconds between spell activations.|
|`Mage.BeamDurationSeconds`|5|0.1–30|Laser spell duration in seconds. Held staff attacks are unchanged.|
|`Mage.StarExpression`|`Angry`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `star`.|
|`Mage.RollExpression`|`Sad`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `roll`.|
|`Mage.GravityExpression`|`Suspicious`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `gravity`.|
|`Mage.VoidExpression`|`EyesClosed`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `void`.|
|`Mage.LaserExpression`|`Scared`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that casts `laser`.|
|`Mage.AutoRecoveryEnabled`|true|true, false|Enables automatic Mage health recovery after avoiding damage.|
|`Mage.AutoRecoveryDelaySeconds`|10|0–300|Seconds without taking damage before automatic recovery starts.|
|`Mage.AutoRecoveryIntervalSeconds`|2|0.1–60|Seconds between each automatic recovery.|
|`Mage.AutoRecoveryAmount`|1|0–100|HP restored each time automatic recovery occurs.|
|`Mage.AutoRecoveryTotalHealingLimit`|120|1–10000|Maximum total automatic recovery per player per stage. Other healing is not counted. Death, revival, and role changes do not reset the amount already used.|
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
|`Hunter.JackpotOrbChancePercent`|0.5|0–100|Chance to use the jackpot target after Hunter defeats an enemy. Missing orbs are added, but a normal drop above the target is not reduced.|
|`Hunter.JackpotOrbCount`|10|1–30|Target orb count used by Hunter's jackpot result.|
|`Hunter.GuaranteedOrbEveryKills`|5|0–100|Qualifying personal kills per guaranteed extra orb; 0 disables. Random extras count toward the guarantee.|
|`Stinker.DistancePerCloud`|2|1–100|Travel distance in meters between recorded uranium-cloud trail points.|
|`Stinker.MinimumSafetyDistance`|2|1–30|Minimum distance Stinker must be from the newest cloud position. Clouds appear one after another.|
|`Stinker.AllowTruckSpawns`|false|true, false|Allows uranium-cloud trail points to be recorded and created inside the truck.|
|`Trickster.ActiveSeconds`|25|1–120|Seconds before the active decoy is removed.|
|`Trickster.DecoyExpression`|`Happy`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|Facial expression that places the decoy.|
|`Trickster.CooldownSeconds`|45|1–300|Seconds after a decoy that attracted an enemy ends before another can be placed.|
|`Trickster.NoTargetCooldownSeconds`|10|0–300|Seconds after a decoy that attracted no enemies ends before another can be placed.|
|`Trickster.InvestigateRadius`|40|1–100|Enemy investigation radius around the decoy in meters.|
|`Trickster.PulseIntervalSeconds`|1|0.25–10|Seconds between enemy investigation pulses.|
|`Trickster.PlacementDistance`|2|0.5–10|Placement distance in front of Trickster in meters.|
|`Mechanic.RepairPercentPerSecond`|2|0–100|Shared original-value percentage repaired per second while Mechanic directly holds damaged valuables.|
|`Mechanic.MaximumRepairPercentPerStage`|50|0–100|Maximum cumulative original-value percentage repaired by each Mechanic per stage.|
|`Electrician.ChargePercentPerSecond`|10|0–100|Shared battery percentage restored per second while Electrician directly holds inactive rechargeable items.|
|`Electrician.MaximumChargePercentPerStage`|100|0–1000|Maximum cumulative battery percentage restored by each Electrician per stage.|
|`Warden.AdditionalStunSeconds`|3|0–30|Seconds added to enemy stuns caused by Warden.|
|`Ninja.VisionRecognitionMultiplier`|2|1–10|Multiplier applied to the time enemies need to visually recognize Ninja.|
|`Executioner.StunnedDamageMultiplier`|2|1–10|Direct attack damage multiplier against enemies already stunned before the hit.|
|`Rider.EnemyDamageMultiplier`|3|1–10|Vehicle impact damage multiplier against enemies while Rider drives.|
|`Rider.PlayerKnockbackMultiplier`|2|1–10|Multiplier for existing vehicle Tumble knockback against players.|
|`Influencer.PlayerRadius`|20|1–100|Radius (m) used to count nearby living teammates.|
|`Influencer.PlayerCheckIntervalSeconds`|0.5|0.1–10|Seconds between nearby-player and dynamic-upgrade checks.|
|`Influencer.NoiseRadiusMultiplier`|2|1–10|Multiplier for footstep, landing, VC, and chat TTS enemy investigation radii.|
|`Influencer.TTSInvestigateRadius`|20|1–100|Enemy investigation radius of periodic TTS.|
|`Influencer.MinimumTTSIntervalSeconds`|20|1–300|Minimum randomly selected periodic TTS interval.|
|`Influencer.MaximumTTSIntervalSeconds`|40|1–300|Maximum randomly selected periodic TTS interval. Values are safely swapped when configured below the minimum.|
|`Influencer.TTSQuietPeriodSeconds`|1.5|0–10|Quiet time after this player's TTS before periodic speech.|
|`Influencer.<Upgrade>UpgradeScaling`|Per upgrade|`count:level` pairs|Sets that upgrade's minimum target by nearby-player count without lowering its pre-role level. Example: Strength defaults to `1:1,2:4,3:9,4:13,5:20`. Available upgrades match Base Upgrades. Blank grants no level.|
|`Werewolf.PlayerDamageMultiplier`|2|1–10|Multiplier for identifiable damage Werewolf deals to another player.|
|`Berserker.<Upgrade>UpgradeScaling`|Per upgrade|`HP%:level` pairs|Sets that upgrade's minimum target while remaining HP is at or below the percentage, without lowering its pre-role level. Example: Strength defaults to `80:1,60:4,40:9,20:13,10:20`. Available upgrades match Base Upgrades except Health. Blank grants no level.|
|`Bodyguard.HealthUpgradeLevels`|11|0–200|Health target while Bodyguard is assigned.|
|`Bodyguard.ProtectionRadius`|15|1–100|Maximum teammate-protection distance in meters.|
|`Bodyguard.DamageSharePercent`|50|0–100|Percentage of enemy damage transferred without reducing Bodyguard below 1 HP.|
|`Rammer.TumbleAttackDamage`|100|0–100000|Enemy damage dealt by Rammer's Tumble Attack.|
|`Rammer.SelfDamage`|15|0–100000|Damage Rammer takes after its Tumble Attack hits an enemy.|
|`Diver.UnderfloorDurationSeconds`|10|1–120|Maximum time below a floor before Diver dies.|
|`Diver.MovementForce`|8|1–30|Free-movement force while Diver is below a floor.|
|`Sniper.ReferenceDistance`|6|1–50|Distance (m) where enemy damage is unchanged.|
|`Sniper.MinimumDamageMultiplier`|0.5|0–1|Enemy damage multiplier at point-blank range. Positive hits still deal at least 1 damage.|
|`Sniper.MaximumDamageMultiplier`|2|1–10|Maximum enemy damage multiplier at long range.|
|`Sniper.MaximumMultiplierDistance`|18|1–100|Distance (m) where the maximum multiplier is reached. Values at or below Reference Distance use a point just beyond it.|
|`Avenger.DamageMultiplier`|1.5|1–10|Enemy damage multiplier after another player dies.|
|`Avenger.DurationSeconds`|30|1–120|Duration of the enemy damage bonus. Another death refreshes this duration.|
|`Avenger.TriggerRadius`|30|1–100|Maximum distance in meters from a dying player that activates Avenger.|
|`Avenger.AllyHitDamageThreshold`|15|1–1000|Minimum HP lost to one enemy hit.|
|`Avenger.AllyHitTriggerRadius`|15|1–100|Maximum distance to the hit ally (m).|
|`Avenger.AllyHitDamageMultiplier`|1.25|1–10|Hit-triggered enemy damage multiplier. Stronger active bonus wins.|
|`Avenger.AllyHitDurationSeconds`|10|1–120|Hit-triggered bonus duration (s).|
|`Avenger.AllyHitCooldownSeconds`|20|1–300|Hit-trigger cooldown (s); deaths bypass it.|
|`Brawler.MeleeDamageMultiplier`|1.25|0–10|Damage multiplier for identifiable melee weapon attacks against enemies and players.|
|`Brawler.RangedDamageMultiplier`|0.75|0–10|Damage multiplier for identifiable gun, staff-projectile, and laser attacks against enemies and players.|
|`Brawler.ComboMultiplierStep`|0.25|0–5|Multiplier added per consecutive hit on the same enemy. No friendly-fire increase.|
|`Brawler.ComboMaximumMultiplier`|2|0–10|Combo cap; never below the starting melee multiplier.|
|`Brawler.ComboTimeoutSeconds`|4|0.1–30|Seconds without a hit before reset. Changing enemy also resets it.|

The default Base level is Health 1 and 0 for other upgrades. Role upgrade settings specify the level used during that role; they are not levels added to Base. Influencer and Berserker preserve stronger upgrades when their conditions change. `Tracker` keeps stronger Base upgrades and remains eligible at high Base levels. `Rammer` sets Launch, Tumble Climb and Tumble Wings to 0 during the stage. Throw is unaffected.

Influencer and Berserker scaling accepts comma- or semicolon-separated `condition:level` pairs. Influencer applies the last target whose player-count condition has been reached. Berserker applies the target belonging to the lowest configured HP threshold currently reached. Default expressions omit pairs whose target level would be 0; add a `condition:0` pair manually when an explicit zero override is needed. Levels are limited to 0–100, except Map Player Count which is limited to 0–1. Entries in the wrong format are ignored.

### Player tools

Open `ROLES` in the top-right of the Escape or lobby menu, then select `TOOLS` or `DRAW HISTORY`.

- **HUD editor:** in `TOOLS`, drag the HUD sample and adjust its anchor, alignment, text/icon size, scale, mode and visibility. `SAVE` keeps local settings; `CANCEL` or Esc discards them; `RESET` previews defaults. Mouse controls also work in the lobby.
- **Bug report:** in `TOOLS`, copy or open the report, review it, add reproduction steps and submit through `OPEN GITHUB ISSUES`. Reports contain mod versions, settings and recent activity, are saved in `BepInEx/RoleShuffleReports`, and are never sent automatically.
- **Language:** `UI.GuideLanguage` and the Roles toggle share a saved choice, defaulting to English. All 14 languages appear by native name. Menus, role descriptions, HUD editor, history and sync status follow it; role names, upgrade identifiers, commands and diagnostics remain English.
- **Sync status:** `TOOLS` shows whether roles, guide, Base Upgrades and history are current. `REFRESH DISPLAY DATA` refreshes these displays without changing roles or upgrades.
- **Draw history:** shows the latest 50 completed truck draws, including level, target, rolled change and actual before/after targets. Zero/capped results are retained; cancelled draws are omitted. History is saved with the host’s run and shared with installed participants.

`HUD.FontSize` defaults to 28 (range 16–48). `UI.GuideLanguage` defaults to `English`. Both are personal settings. The default HUD display mode is `NameOnly`.

### Notifications and HUD

- **Resources:** the remaining healing, revives, repair, charge and wagers appear below stamina as cyan icons and numbers in one column. The display fits the available space, and empty resources turn red. It appears while you are alive and have the mod installed. Adjust it with `HUD.ResourceHud*`. Signalman also shows its radio cooldown here.
- Role emblems appear beside roles in `CURRENT ROLES` and `ROLE GUIDE`, and optionally in the HUD. The area outside each hexagonal emblem is transparent. Unrevealed secret roles use a shared question-mark emblem until revealed. Emblems are visible to players who have the mod installed.
- At stage start, each player announces the assigned English role name through vanilla chat and TTS.
- RoleShuffle's forced notification TTS does not attract enemies. Ordinary microphone input and other world sounds keep their vanilla behavior unless suppressed by Ninja.
- When Stage Flux is present, RoleShuffle uses the separate Stage Flux announcement delay to avoid overlapping stage-start messages.
- RoleShuffle waits until Stage Flux TTS has finished and remains quiet for 1.5 seconds before announcing roles.
- Initial role announcements play one at a time. Later notices and replies wait only for the same player's chat/TTS; different players can speak together.
- Influencer speech and Influenza sneezes wait for their own player's messages and remain audible to enemies. Diver counts at 15-second marks (30, 15), then every second from 10 to 0, interrupting previous speech; normal notices wait.
- The local `ROLES` list defaults to names at the bottom left, with your role pinned and pages rotating every 5 seconds. It fits six multilingual/icon rows when `HUD.PlayersPerPage` is 6 or higher; smaller limits are respected. Long player names are shortened to keep role names visible. See HUD settings above.
- The Base Upgrade draw animation is shown to the host and participants who have RoleShuffle installed.
- Open `ROLES` at the top-right of Escape (`CURRENT ROLES`) or the lobby (`ROLE GUIDE`; current roles unavailable). Your role appears first and expanded; click players to toggle descriptions. The left column also opens `BASE UPGRADES`: host targets, configured targets and accumulated truck-draw bonuses.
- `ROLE GUIDE` explains enabled roles in your selected language. In multiplayer, descriptions use the host's settings. If a description is unavailable in your language, it appears in English. Single-player uses your own settings.

### Compatibility

Stage Flux is optional and not required.

- Second Chance takes priority over Phoenix, Rescuer, and Bodyguard revival effects. Revival effects that are not needed remain available.
- If Second Chance or Phoenix prevents a failed stage transition, current roles remain active.
- During Value Surge or Value Crash, Mechanic repairs only the value that was actually lost.
- Initial roles wait for Stage Flux announcements. Later messages wait for Stage Flux speech by the same player.
- Enemy Purge defeats do not activate Hunter rewards.
- Dangerous Valuables respects valuables protected by Engineer.

Elite Enemy Variants is optional and not required.

- Living `Ninja` players are excluded from Elite Enemy Variants attacks that specifically require an unseen player target. Dead Ninjas and all other players remain eligible.
- When `Compatibility.EnhancedEnemyRewardsEnabled` is true, Enhanced enemies provide stronger Vampire healing and better Hunter bonus orbs, up to the Tier 3 reward. This does not change the normal number of orbs.
- Gambler's roulette effect stays attached to the correct player even when an Enhanced Gambit moves after capture.
- RoleShuffle does not add Elite Enemy Variants enemy counts, state, or defeat notices to its HUD or menu.

### Gameplay notes

- Twins supports host-only play. Simultaneous damage or healing can cause some difference in the shared HP result.

- `Mage` spells can harm players and valuables; spell names ignore case. The laser follows the caster's view. RoleShuffle users see only the beam; unmodded guests still see its temporary staff.
- `Courier` and `Tuna` continuously deal real damage under their stated conditions and can kill their owner. The damage is not automatically restored.
- Role upgrades end at stage end. Base levels include retained item amounts when enabled. Throw remains permanent.
- When a role's maximum HP bonus ends, remaining HP keeps its proportion, rounded down (at least 1 HP while alive): 300/520 becomes 69/120.
- Setting every role to disabled or weight 0 leaves no eligible random role to assign.

## 日本語

### 概要

RoleShuffleはステージごとに役職を抽選し、移動・回復・戦闘などの能力を付与。基礎アップグレードはステージ外でも維持され、ランの進行に応じて強化可能です。

ゲームプレイ効果はホストだけの導入で利用でき、最大30人のセッションをサポートします。MODを導入していない参加者にも、役職、アップグレード、効果、バニラのチャット／TTS通知が適用されます。RoleShuffleを導入している参加者は、すべての役職を確認できるHUDも利用可能です。

デフォルトでは、役職による一時強化を中心にするため、ショップのアップグレードアイテム出現数は0です。

### お問い合わせ

ご質問、不具合報告、ご意見は[GitHub Issues](https://github.com/CapacityDown/RoleShuffle/issues)からお送りください。投稿にはGitHubアカウントが必要です。

### 必須MOD

- BepInExPack 5.4.2305以降の互換バージョン
- REPOConfig 1.2.6以降の互換バージョン
- MenuLib 2.5.2以降の互換バージョン

### 導入方法

MODマネージャーで導入するか、`RoleShuffle.dll`をプロファイルの`BepInEx/plugins`へ配置してください。

### マルチプレイ

- 役職の抽選とゲームプレイ設定はホストが管理します。
- MOD未導入の参加者にも対応しており、RoleShuffleの導入は不要です。
- MOD導入済みの参加者には全員の役職を確認できるHUDと、Escメニュー内のスクロール可能な`Roles`ページが表示されます。HUDの配置は各プレイヤーが個別に変更可能です。
- MageとTricksterは表情の解除では能力を発動しません。ホスト自身のメニュー終了時の表情復帰も除外しますが、参加者のメニュー終了時に復帰した表情では能力が発動する場合があります。

### ロール選択とプリセット

ロビーまたはEscメニューの`ROLES`から`ロール設定`を開くと、ホストが各ロールのON/OFFを切り替えられます。無効なロールや2種類のシークレットも一覧に残ります。変更はMOD設定と同じ`Enabled`へ保存され、途中参加者を含む次のロール抽選から反映されます。割当済みのロールは維持されます。参加者はホストの設定を閲覧できますが、変更はできません。情報を提供しない旧ホストでは未受信と表示。

`プリセット`から遊び方を選んで適用すると、全ロールのON/OFFを置き換えます。抽選重み・能力値・バランスルール・Base Upgrade・HUD設定は保持します。重み0のロールはONでも抽選されません。個別に変更した組み合わせは`カスタム`と表示されます。既存の設定はプリセットを適用するまで変更されません。

|プリセット|有効なロール数|遊び方|
|---|---|---|
|標準|46|シークレットを含む全ロール。初期状態のON/OFF構成へ戻します。|
|初心者向け|21|基本強化・回復・防御。Signalman・Porterを含み、自動加害役・ハンデ役は除外。|
|協力重視|19|チーム支援・回復・修理・共有生存。Signalman・Twins・Porterを含む。|
|カオス|16|爆発・魔法・ギャンブルなど予測しづらい展開。Twinsを含む。|
|高難度|15|専用の回復・蘇生役を外し、リスクのある特化型ロールで挑みます。|

人数・状況・バランスによる抽選制限は引き続き適用されます。全体の抽選が無効の場合や候補がない場合は画面に表示。プリセットを適用しても`General.Enabled`や重み0は変更しません。

### 役職確認コマンド

- ゲーム内チャットで`/roles`を入力すると、自分に割り当てられた役職名だけを通知します。
- ホストがRoleShuffleを導入していれば、MOD未導入の参加者も使用可能です。
- ステージ外では`YourRole:Unavailable`と応答します。

### 役職の抽選

- プレイ可能なステージの開始時、各プレイヤーへ役職を1つ割り当てます。
- 有効な役職を相対的な`Weight`に基づいて抽選します。
- `General.UniqueRoles = true`の場合、抽選可能な役職を一巡するまで重複を避けます。
- `King`はステージごとに最大1人です。
- Showcase役とSupport役を、人数に応じた最低人数まで優先して割り当てます。対象は「役職バランス」を参照。
- デフォルトでは、24人以上でShowcaseとSupportをそれぞれ最低4人保証します。Danger上限は8人で2人、16人で3人へ増え、Hardship上限は12人で2人へ増えます。InfluencerはShowcase役であり、Danger役として扱いません。4人以下では両リスクグループを合わせて最大1人です。
- 各プレイヤーが直近5回に割り当てられた役職は、そのプレイヤーの次回抽選時に通常の半分のWeightで扱います。他のプレイヤーの履歴は影響しません。
- 同じプレイヤーへ前ステージと同じ役職を通常は連続で割り当てません。`Courier`、`Tuna`、`Influenza`のいずれかの後、2ステージはそのプレイヤーをこれらの役職から除外します。役職未割り当てを防ぐ必要がある場合だけ制限を緩和します。
- `Jumper`、`Launcher`、`Climber`、`Flyer`は、トラック抽選分を含む基礎アップグレード目標値のいずれかが、対応する役職の設定値以上の場合、ランダム抽選から除外されます。
- `Influencer`は、現在の参加人数で到達可能なアップグレード目標値が現在のBase Upgradeを1項目も上回らない場合、ランダム抽選から除外されます。
- `Tracker`、`Ghost`、`Medic`、`Courier`、`Rescuer`、`Influencer`、`Werewolf`、`Bodyguard`、`Imitator`、`Avenger`、`Influenza`、`Signalman`、`Twins`はシングルプレイまたは1人のセッションでは抽選されません。
- `Imitator`は、コピー可能な役職を持つ他の参加者が確保された場合だけ抽選されます。
- デフォルトでは、能力を使える対象がない状況依存役を抽選から除外します。必要なオブジェクトがない`Musician`、`Engineer`、`Electrician`、`Rider`と、近接武器も銃もない場合の`Sniper`、近接武器がない場合の`Brawler`が対象。両ロールの抽選判定では、武器として使える貴重品も武器に含めません。Sniperの抽選判定では杖も対象外です。
- 退出したプレイヤーは役職一覧から外れます。再参加すると以前の役職に戻り、新しい参加者には役職・強化・通知が適用されます。
- 役職とステージ中の効果は、ステージが実際に終了した場合だけ解除します。復活効果によって失敗時のステージ移行が中断された場合は維持。

### 役職一覧

|役職|機能とデフォルト効果|主な制限・危険性|調整可能な値|
|---|---|---|---|
|![][i01]<br>Tank|基礎の最大HPを1.5倍に強化します。倍率強化の上限は4,100HPです。|最低Health 21。|最低値・倍率・上限|
|![][i02]<br>Runner|基礎の走行速度・スタミナを1.5倍に強化します。倍率強化の上限は205・2,040です。|最低Speed 6・Stamina 46。|最低値・倍率・上限|
|![][i03]<br>Jumper|Extra Jumpがレベル10になり、着地するまでに最大10回の追加ジャンプを使えます。|バニラのExtra Jumpアップグレードを使用し、別の能動的な能力はありません。|Extra Jump目標値0～100|
|![][i04]<br>Lifter|重い物・カート・バイクをつかむ力・回転力をStrength Lv0～200の最大値に固定。|Strengthの表示はLv200。基礎値が両方の目標に達すると抽選対象外（初期設定ではLv50）。|保持する力|
|![][i05]<br>Launcher|Tumble開始時にプレイヤーをより遠く前方へ飛ばします。Launchはレベル10です。|バニラのLaunchアップグレードを使用し、別の能動的な能力はありません。|Launch目標値0～100|
|![][i06]<br>Climber|Tumble中の登りやすさが増し、より遠くの物を掴めます。Tumble Climbはレベル50、Rangeはレベル20です。|2種類のバニラアップグレードを使用し、別の能動的な能力はありません。|Tumble ClimbとRangeの目標値0～100|
|![][i07]<br>Flyer|Tumble Wingsの効果時間が延び、空中をより長く移動可能です。Tumble Wingsはレベル10です。|バニラのTumble Wingsアップグレードを使用し、別の能動的な能力はありません。|Tumble Wings目標値0～100|
|![][i08]<br>Tracker|25m以内の最も近い敵を自動通知し、8m以内への接近を警告。死亡した仲間の最も近い頭は距離制限なく追跡。Health最低3、Map Player Count最低1となり、高い基礎強化を維持。|有意な変化があると本人だけに通常20秒間隔で通知。接近警告は待ち時間中も届きますが、危険が近くに留まる間は連発しません。視点基準の直線方向と上下を案内。アイテム・操作不要、2人以上で抽選。|Health 0～200。検知距離・警告距離・通知間隔・距離変化は下表。|
|![][i09]<br>Ghost|Death Head BatteryはLv50。死亡中、頭から5m以内の味方を2秒ごとに1HP回復し、1ステージで合計50HPまで回復。頭を持つ味方はSpeed＋1。|蘇生しても回復上限は戻りません。複数の頭では重複せず、KingのSpeed支援とは強い方を優先。1人では抽選されません。Baseが高くても抽選対象。|Battery、回復、運搬者のSpeed|
|![][i10]<br>Bomber|8 m移動するごとに起動済みグレネードをランダム設置します。Bomber 1人につき最大30個まで残り、上限では最も古いものを削除します。|グレネードはプレイヤーやValuableにも危険です。生成されたグレネードを持てるのはBomberだけです。トラック内設置が無効な場合、トラック内の移動距離は加算されません。|距離、上限、トラック内設置、グレネード種類|
|![][i11]<br>Medic|5 m以内の仲間を2秒ごとに5 HP回復し、ステージごとに合計150 HPまで回復します。|Medic自身は回復しません。範囲内で生存している仲間だけが対象。そのMedicの上限を使い切ると回復を停止します。参加者が1人だけの場合はランダム抽選されません。|回復量、間隔、範囲、合計上限|
|![][i12]<br>Phoenix|死亡すると、1ステージに1回だけデフォルト25 HPで自動復活します。|頭が静止していることが条件です。復活回数が残っている間は、時間制限なくゲームオーバーを防ぎます。復活後HPは最大HPを超えません。|復活後HP、復活遅延|
|![][i13]<br>Courier|価値のある貴重品をつかんだまま搬入先の外で5m運び、トラックか納品所に入れると配達完了。サイズ別にHP回復・自動HP減少の一時停止（下表）。|開始・蘇生時は30秒停止。効果切れ中はトラック外で0.1秒ごと1HP減少、死亡あり。敵の攻撃等は防げません。配達回数無制限、同じ品の報酬は各自1ステージ1回。途中で放すと運搬やり直し。1人では抽選対象外。|配達・自動HP減少|
|![][i14]<br>Rescuer|生存中に、死亡した仲間のDeath Headをつかむと、デフォルト25 HPで復活させます。頭が動いていても使用可能です。|復活まで頭をつかみ続けてください。待機中に手を離すと中止します。ステージごとに最大2回です。復活後HPは対象の最大HPを超えません。参加者が1人だけの場合はランダム抽選されません。|復活後HP、遅延、最大復活回数|
|![][i15]<br>Vampire|10 m以内で敵が死亡すると、Tier 1は5、Tier 2は10、Tier 3は50回復します。|敵のバニラDanger Levelを使用します。Elite Enemy Variants導入時にEnhanced報酬補正が有効なら、Enhanced個体を最大Tier 3まで1段階上として扱います。Vampireが生存し、死亡した敵の近くにいる必要があります。|Tier別回復量、範囲|
|![][i16]<br>King|Crownと12mの強化範囲。味方へSpeed・Range各＋2、Strength最大＋5。|自分は対象外。重複なし。上限200、掴む力・回転力の低下なし。範囲外で追加分を解除。1ステージ1人。|半径・追加レベル|
|![][i17]<br>Tuna|ステージ開始時の5秒間の猶予後、3秒間停止すると、移動するまで0.1秒ごとに1ダメージを受けます。|死亡する可能性があります。停止時間の計測は最初の猶予後に始まります。動くと即座にリセットし、失った体力は回復しません。|停止時間、ダメージ、間隔|
|![][i18]<br>Musician|Musicianが楽器で音を鳴らすたびに、本人を含む10 m以内の生存プレイヤーを5 HP回復します。|バニラの楽器系貴重品が必要です。デフォルトでは存在しない場合に抽選されません。|回復量、範囲|
|![][i19]<br>Mage|チャットで`star`、`gravity`、`roll`、`void`、`laser`を入力するか、設定した表情を選択し、順に10、10、15、30、50 HPを消費してバニラ攻撃を発動します。10秒間ダメージを受けなければ、2秒ごとに1 HP回復します。自動回復は1ステージの累計120 HPまでです。保持した杖で発動する魔法の効果時間は1.3倍になります。|表情を解除して標準へ戻す操作では発動しません。全魔法で3秒のクールダウンを共有し、消費で死亡する場合は発動しません。攻撃はプレイヤーやValuableにも危険です。効果時間の延長はチャット・表情での魔法や、星杖の瞬間的な攻撃には適用しません。|魔法ごとの表情、発射クールダウン、魔法ごとのHP消費量、自動回復の有効化、待機時間、間隔、回復量、ステージごとの回復上限|
|![][i20]<br>Gambler|持ったValuableは50%の確率で現在価格が2倍になり、失敗すると破壊されます。Gambitは緑で50 HP回復、赤で100ダメージ、黒で死亡、白で全回復してステージ中のHealthを5レベル増やします。|賭けは一度に1回だけ使用でき、納品後に復帰します。傷ついたValuableは低下後の価格を基準にします。|勝率、勝利倍率、Gambitの緑回復量、赤ダメージ、白の一時Healthレベル|
|![][i21]<br>Hunter|武器のバッテリー消費は通常の75％。自分が倒したオーブを落とす敵は10％でオーブ2倍、0.5％で10個。対象の敵を5体倒すごとに追加オーブを最低1個保証。|抽選の追加分は保証分を兼ねます。撃破数はステージごとにリセット。本人が保持・装備する武器と、本人の撃破が対象。Enhanced報酬は追加オーブの品質だけに影響します。|バッテリー、オーブの確率・個数、保証間隔|
|![][i22]<br>Stinker|2 m移動するごとに、ダメージを与えるウラン雲を残します。雲は、Stinkerが最新の発生位置から2 m以上離れた後に1つずつ発生します。出現したウラン貴重品は0.5秒の猶予後に壊れます。|ウラン雲はほかのプレイヤーへダメージを与えます。Stinkerも雲へ戻るとダメージを受ける可能性があります。デフォルトではトラック内に発生しません。|距離、安全距離、トラック内発生、破壊猶予|
|![][i23]<br>Engineer|対応している効果付きValuableを保持している間、その効果が発動しないようにします。|Camera、Propane Tank、Snowmobile、Flashlight、Clown Doll、Love Potionは対象外です。デフォルトでは対応Valuableが存在しない場合に抽選されません。|抽選設定のみ|
|![][i24]<br>Trickster|チャットで`decoy`と入力するか、設定した表情を選択すると、半径40 m以内の敵を25秒間繰り返し引きつける固定式Scream Dollを設置します。|デコイが有効な間は新しいデコイを設置できません。クールダウンは終了後から始まり、敵を引きつけた場合は45秒、一体も引きつけなかった場合は10秒です。デコイは掴む、破壊する、納品することができません。|デコイ用の表情、有効時間、通常時と空振り時のクールダウン、範囲、誘導間隔、設置距離|
|![][i25]<br>Mechanic|破損したValuableを直接掴んでいる間、元の売却価格の合計2%を毎秒回復し、ステージごとに合計50ポイントまで回復します。|実際に回復した価格だけがステージ上限へ加算されます。修復済みのValuableとValuable以外は対象外です。複数を同時に掴んだ場合、回復速度とステージ上限を共有します。売却価格を回復する効果で、破損した外見は残る場合があります。|回復速度、ステージ合計上限|
|![][i26]<br>Electrician|使用していない充電可能なアイテムを直接掴んでいる間、合計10%のバッテリーを毎秒回復し、ステージごとに合計100ポイントまで回復します。|使用中、満充電、充電不可能なアイテムは対象外です。デフォルトでは充電可能アイテムが存在しない場合に抽選されません。|充電速度、ステージ合計上限|
|![][i27]<br>Warden|Wardenの武器、グレネード、保持している攻撃用オブジェクトによる敵のスタン時間を3秒延長します。|元の攻撃がその敵をスタンさせられる場合だけ適用。|追加スタン時間|
|![][i28]<br>Ninja|足音、着地音、VC、チャットTTSで敵の調査行動を発生させず、視覚で認識されるまでの時間が2倍になります。|接近や、武器、Valuable、衝突、爆発、危険物などの物音では発見される可能性があります。|視認時間倍率|
|![][i29]<br>Executioner|攻撃前からスタンしている敵への直接攻撃ダメージが2倍になります。|最初にスタンさせる攻撃には倍率が適用されません。物がぶつかっただけのダメージには倍率が適用されません。|スタン中ダメージ倍率|
|![][i30]<br>Rider|バニラ車両の運転中、敵への衝突ダメージを3倍にし、プレイヤーへ元から発生するTumbleノックバックを2倍にします。|プレイヤーへのダメージは増えません。デフォルトではバニラ車両が存在しない場合に抽選されません。|敵ダメージ倍率、プレイヤーノックバック倍率|
|![][i31]<br>Influencer|半径20 m以内の生存中の仲間が多いほど強化され、定期的に英語TTSを発言します。|足音、着地音、VC、チャットTTSが敵へ届く範囲は2倍です。|範囲、確認間隔、アップグレード別の人数連動設定、物音倍率、TTS間隔と範囲|
|![][i32]<br>Werewolf|Werewolfが攻撃者と特定できる、他のプレイヤーへのダメージを2倍にします。|自傷、環境ダメージ、攻撃者を特定できないダメージは変化しません。1人のセッションでは抽選されません。|プレイヤーダメージ倍率|
|![][i33]<br>Berserker|残りHPが少ないほど強化されます。|HPを回復すると強化も戻ります。|アップグレード別のHP割合連動設定|
|![][i34]<br>Bodyguard|Healthがレベル11になり、15 m以内の仲間が敵から受けたダメージの50%を肩代わりします。|肩代わりによってBodyguardのHPが1未満になることはありません。|Health目標、範囲、肩代わり率|
|![][i35]<br>Rammer|Tumble Attackで敵へ100ダメージを与え、命中後に自身が15ダメージを受けます。|ステージ中はBase Upgradeにかかわらず、Launch、Tumble Climb、Tumble Wingsがレベル0に固定されます。|Tumble Attack・自傷ダメージ|
|![][i36]<br>Diver|固定された床で下を向いたままタンブルすると床を抜け、最大10秒間、床下を移動可能です。|時間内に床を抜けて戻れないと死亡します。床上へ戻ると直前の潜航時間の1.5倍のクールダウンが始まり、残り時間と再使用可能状態はTTSで通知されます。|床下制限時間、移動力|
|![][i37]<br>Sniper|敵へのダメージが距離に応じて連続変化し、密着時0.5倍、6mで1倍、18mで最大2倍。|攻撃者を特定できる場合に適用。車両衝突とTumble Attackは変化しません。|距離・倍率|
|![][i38]<br>Imitator|最初はBase Upgradesだけが適用されます。他のプレイヤーへHPを渡すときと同じ位置をつかむと、その仲間のアップグレード、能力、デメリットを残りのステージ中コピーします。以後、割り当て一覧にはImitatorとコピー先の役職を併記します。|Imitator、King、Bomber、Stinker、Werewolf、Courier、Tuna、Influenza、Signalman、Twins、???1、???2はコピーしません。対象外をつかむと`CannotCopy`（コピー不可）と通知します。最初に成功したコピーはそのステージ中固定です。1人のセッションでは抽選されません。|抽選設定のみ|
|![][i39]<br>Avenger|15m以内の味方が敵の一撃で15HP以上失うと、10秒間、敵へのダメージが1.25倍（クールダウン20秒）。30m以内の味方が死亡すると30秒間1.5倍。|発動・終了を通知。強い方だけ適用。死亡効果は被弾効果のクールダウン中も発動し、再発動で時間を更新。自傷・味方の攻撃では被弾効果は発動しません。自分の死亡では発動せず、1人では抽選されません。|被弾・死亡の範囲、閾値、倍率、時間、クールダウン|
|![][i40]<br>Brawler|同じ敵への近接攻撃が1.25倍から命中ごとに0.25ずつ上昇し、最大2倍。銃・杖の弾・レーザーは0.75倍。|別の敵に命中、または4秒間命中しないと連撃をリセット。味方への攻撃は1.25倍のまま。車両衝突・Tumble Attack・グレネード・通常の保持物は変化しません。|近接・遠隔倍率、連撃の上昇幅・上限・猶予|
|![][i41]<br>Influenza|**発症：** この役職になって30秒後、最大HPが75に固定。発症するまでは感染を広げません。<br>**くしゃみ：** 発症後、30～90秒ごと（平均約1分）に自動で発生。前方5m以内・左右それぞれ20度の範囲にいる仲間へ、1人ずつ60％で感染。<br>**VC・チャット：** 発症後、前方3m以内・左右それぞれ30度の範囲にいる仲間へ、1人ずつ30％で感染。VCはひとまとまりの発話につき1回、チャットは1投稿につき1回判定。|**感染した仲間：** その場で元の役職を失い、インフルエンザへ変更。30秒後に発症し、さらに感染を広げます。<br>**敵への音：** くしゃみは全方向の敵にも届きます。基本は半径5mで、敵の聴力によって変わります。<br>死亡・蘇生で発症までの時間はリセットされず、ステージ終了で解除。1人では抽選対象外。|発症時間、HP、くしゃみ間隔、感染範囲・角度・確率、敵への音|
|![][i42]<br>Signalman|通常チャットの本文を変えずに、生存中の仲間全員へ距離制限なく届けます。受信者自身の読み上げ音声で再生し、仲間の死亡通知も受け取ります。通信後は20秒待機。|コマンドと自動通知は除外。待機中の投稿は通常チャットのみで、後から転送しません。相手の発話を待ち、カウントダウンを優先。5秒待った通信は破棄。2人以上で最大1人を抽選。通常VCの距離は変わりません。|再使用待ち・待機上限・死亡通知・コマンド除外|
|![][i43]<br>Twins|2人の現在HP・最大HPを合算して共有。同じ貴重品を同時につかむと掴む力1.5倍、衝突による価値減少を半減。3m以内で2人ともしゃがみ、被弾せず3秒静止すると共有最大HPの10％と両者の全スタミナを回復し、60秒の共通待ち時間が発生。15m以上離れた相方へ走る間は速度約1.25倍、5m以内で終了。納品エリア外で3秒共同運搬した品を、2人が品の5m以内にいる状態で搬入すると価値＋10％。1品1回、ステージ合計上限$5,000。|2人以上で最大1組、相方は固定。片方が死亡すると2人とも死亡し、片方の蘇生で同じ回復量を共有して2人とも復活。片方への感染で2人ともInfluenzaとなり共有解除。ステージ終了・相方離脱でも共有を解除し、残りHP割合を維持。ペア内HP渡し・コピーは不可。購入装備は共同運搬の対象外。|抽選、運搬、休憩、合流、納品|
|![][i44]<br>Porter|納品エリア外でTiny・Small・Mediumの貴重品を1人で3秒つかむと収納。重量15まで、個数制限なし。トラック・納品所・カート上で荷下ろし。満載10秒、半分で5秒。|敵からHPダメージで全収納品をばらまく。重量に比例してSpeedレベルが減少（端数切り捨て）、満載で0、荷下ろしで復元。エリア退出・被弾落下で待ち直し。死亡・役職変更・離脱・ステージ終了は全返却。購入装備は対象外。満載・収納不可・荷下ろし完了を本人に通知。|重量上限、サイズ、収納・荷下ろし時間、被弾時落下|
|![][iu]<br>???1|???|???|???|
|![][iu]<br>???2|???|???|???|

### 役職の成長・支援設定

|キー|初期値|範囲|効果|
|---|---|---|---|
|`Tank.HealthMultiplier` / `Tank.MaximumHealth`|1.5 / 4100|1–10 / 100–4100|最大HP|
|`Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed`|1.5 / 205|1–10 / 5–205|走行速度|
|`Runner.StaminaMultiplier` / `Runner.MaximumStamina`|1.5 / 2040|1–10 / 40–2040|スタミナ容量|
|`Courier.ContractDistance`|5|1–50m|搬入先の外での運搬距離。|
|`Courier.InitialGraceSeconds`|30|0–300秒|開始・蘇生後のHP減少停止。|
|`Courier.<Size>GraceSeconds`|`30/30/60/90/90/90/120`|0–300秒|Tiny/Small/Medium/Big/Wide/Tall/VeryTall（極小/小/中/大/横長/縦長/超縦長）。残り猶予は短縮しません。|
|`Courier.<Size>HealAmount`|`10/25/50/100/100/100/100`|0–10000HP|同じサイズ順。固定HP回復、最大HPまで。|
|`King.SpeedBonusLevels` / `King.RangeBonusLevels`|2 / 2|0–200|Speed・Range追加レベル。|
|`King.StrengthBonusLevels`|5|0–200|悪化しないStrengthの追加上限。|
|`King.UpgradeRadius`|12|1–30 m|味方に強化を付与する範囲。|
|`HUD.ResourceHudEnabled`|true|真偽値|左上に自分の能力残量を表示。役職一覧とは独立。|
|`HUD.ResourceHudScalePercent`|100|50–200|残量HUDの大きさ。画面内に収まるよう自動調整。|
|`HUD.ResourceHudOffsetX` / `ResourceHudOffsetY`|0 / 0|0–3840 / 0–2160|スタミナ直下を基準に、残量表示を右／下へ移動します。|

能力の残量はスタミナ直下に縦1列で表示し、項目が多い場合は収まるよう縮小します。役職一覧には各プレイヤーの役職を表示。残量HUDを使う場合は、ホストと同じバージョンのRoleShuffleを導入してください。

### 能力の調整

ホストがREPOConfig → RoleShuffle → 役職名から設定。初期値は上記の能力です。保持する力・HP・感染判定には現在の値を使用します。発症時間は新しい感染から適用し、予約済みのくしゃみの時刻は維持。Stinkerの猶予は新しく出現する貴重品から適用。既存設定は保持します。

|設定|初期値|範囲|効果|
|---|---|---|---|
|`Porter.Capacity`|15|1～100|収納重量の上限。個数制限なし。HUDに重量・収納品の現在合計金額・荷下ろし残り時間を表示。|
|`Porter.HoldSeconds`|3|1～30秒|収納まで1人でつかみ続ける時間。|
|`Porter.FullUnloadSeconds`|10|0～60秒|満載時の荷下ろし時間。重量に比例。0なら即座に完了。|
|`Porter.DropOnEnemyHit`|true|真偽値|敵からHPダメージで全収納品をばらまく。|
|`Porter.AllowedSizes`|Tiny,Small,Medium|Tiny・Small・Medium・Big・Wide・Tall・VeryTallをカンマ区切り|収納可能サイズ。空欄・不明な名前は追加許可しない。|
|`Lifter.HeavyGripMultiplier` / `Lifter.HeavyRotationMultiplier`|1 / 1|0.1～5|Strength Lv0～200の最大値に対する、つかむ力／回転力の倍率。|
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
|`Signalman.CooldownSeconds` / `Signalman.MaximumDelaySeconds`|20 / 5|1～300 / 1～30秒|通信の再使用待ち／受信者の発話を待つ上限。|
|`Signalman.DeathAlerts`|true|true, false|仲間の死亡を本人へ通知。`Notifications.Enabled`にも従います。|
|`Signalman.ExcludedCommands` / `Signalman.ExcludedPrefixes`|`star,roll,gravity,void,laser,decoy` / `!`|カンマ区切り|除外する入力全文／先頭文字を追加。`/`で始まる入力と既存の魔法名の単独入力は常に除外。|

### 設定

すべての設定はREPOConfigから変更可能です。ホスト設定はセッション全体へ、ローカル設定はMOD導入者本人のHUDだけに反映されます。

#### 一般、通知、HUD

|キー|デフォルト|範囲・値|内容|管理|
|---|---:|---|---|---|
|`General.Enabled`|true|true, false|ステージごとの役職割り当てを有効化。|ホスト|
|`General.UniqueRoles`|true|true, false|抽選可能な役職を一巡するまで重複を避けます。|ホスト|
|`General.ShopUpgradeItemCount`|0|0～30|各ショップへ要求するアップグレードアイテム数です。0でショップの抽選対象から除外します。|ホスト|
|`Compatibility.EnhancedEnemyRewardsEnabled`|true|true, false|有効時、Elite Enemy VariantsのEnhanced個体をVampireの回復量とHunterの追加オーブ品質の計算で1段階上として扱います。|ホスト|
|`Notifications.Enabled`|true|true, false|役職名と役職効果のチャット／TTS通知を有効化。|ホスト|
|`Notifications.DelaySeconds`|2.5|0～30|Stage Flux未導入時の役職通知までの秒数。|ホスト|
|`Notifications.StageFluxDelaySeconds`|5|0～30|Stage Flux導入時、役職通知までに最低限待つ秒数。Stage FluxのTTS終了後、さらに1.5秒間の無音を確認します。|ホスト|
|`HUD.Enabled`|true|true, false|ステージ中、現在の全役職を表示。|ローカル|
|`HUD.RoleDisplay`|`NameOnly`|`IconAndName`, `NameOnly`, `IconOnly`|役職のアイコン＋名前、名前のみ、アイコンのみを選択します。プレイヤー名は常に表示。|ローカル|
|`HUD.IconSize`|64|32～128|HUD全体の倍率を適用する前の役職アイコンの大きさです。6人分の高さを確保し、画面に収まる倍率へ調整します。|ローカル|
|`HUD.Anchor`|`BottomLeft`|`TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight`|HUDの基準位置を選択します。|ローカル|
|`HUD.Alignment`|`Left`|`Left`, `Center`, `Right`|役職テキストの揃え方を選択します。|ローカル|
|`HUD.OffsetX`|0|`-3840`～3840|基準位置からの水平オフセットです。単位はピクセルです。|ローカル|
|`HUD.OffsetY`|80|`-2160`～2160|基準位置からの垂直オフセットです。単位はピクセルです。|ローカル|
|`HUD.ScalePercent`|70|50～200|HUDの表示倍率。|ローカル|
|`HUD.PlayersPerPage`|8|2～20|自分の固定表示を含め、同時に表示する最大人数です。6以上なら、文字やアイコンが高くても6人分の表示領域を確保します。|ローカル|
|`HUD.PageIntervalSeconds`|5|1～30|各役職ページを表示する秒数。|ローカル|
|`HUD.TransitionDurationSeconds`|0.2|0～1|役職ページ切替時のフェード時間。|ローカル|
|`HUD.PinLocalPlayer`|true|true, false|自分の役職をすべてのページへ固定表示。|ローカル|

#### 役職バランス

この表はすべてホスト設定。

Showcase役は`Bomber`、`Stinker`、`Mage`、`Gambler`、`Trickster`、`King`、`Rider`、`Influencer`、`Diver`です。Support役は`Medic`、`Rescuer`、`Mechanic`、`Electrician`、`Warden`、`Bodyguard`、`Signalman`、`Porter`です。Danger役は`Bomber`、`Stinker`、`Werewolf`、`Influenza`、Hardship役は`Courier`、`Tuna`、`Influenza`です。InfluencerはDanger役ではありません。これらの条件で割り当て可能な役職がなくなる場合は、全員に役職を割り当てられるまで制限を段階的に緩和します。

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`Role Balance - Guarantees.ShowcaseEnabled`|true|true, false|参加人数別のShowcase最低保証を有効化。|
|`Role Balance - Guarantees.ShowcaseMinimums`|`3:1,7:2,14:3,24:4`|`参加人数:最低人数`の組|参加人数ごとに保証するShowcaseの最低人数です。|
|`Role Balance - Guarantees.SupportEnabled`|true|true, false|参加人数別のSupport最低保証を有効化。|
|`Role Balance - Guarantees.SupportMinimums`|`4:1,8:2,14:3,24:4`|`参加人数:最低人数`の組|参加人数ごとに保証するSupportの最低人数です。|
|`Role Balance - Limits.Enabled`|true|true, false|Danger、Hardship、少人数時の複合上限を有効化。|
|`Role Balance - Limits.DangerMaximums`|`1:1,8:2,16:3`|`参加人数:上限`の組|参加人数ごとの`Bomber`、`Stinker`、`Werewolf`、`Influenza`の合計上限。|
|`Role Balance - Limits.HardshipMaximums`|`1:1,12:2`|`参加人数:上限`の組|参加人数ごとの`Courier`、`Tuna`、`Influenza`の合計上限。|
|`Role Balance - Limits.SmallPartyMaximumPlayers`|4|1～30|DangerとHardshipの複合上限を使用する最大参加人数です。|
|`Role Balance - Limits.SmallPartyCombinedMaximum`|1|1～30|少人数時に許可するDangerとHardshipの合計最大人数です。|
|`Role Balance - Variety.PreventSameRole`|true|true, false|ほかの候補がある場合、同じプレイヤーへの同役職の連続割り当てを防ぎます。|
|`Role Balance - Variety.HardshipCooldownStages`|2|0～10|`Courier`、`Tuna`、`Influenza`のいずれかの後、そのプレイヤーをこれらの役職から通常除外するステージ数です。|
|`Role Balance - Variety.ExcludeUnavailableRoles`|true|true, false|必要な敵、武器、使用対象が存在しない状況依存役を抽選から除外します。|

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
|`Base Upgrade Draw.Enabled`|true|true, false|ショップ後の共有Base Upgrade抽選を有効化。|
|`Base Upgrade Draw.MaximumLevel`|200|0～200|正のトラック抽選で到達できる最大目標値。Map Player Countは最大1のままです。|
|`Base Upgrade Draw.CappedUpgradeWeightMultiplier`|0.25|0～1|アップグレードが抽選上限に近づくほどWeightが低下し、上限でこの倍率になります。設定分とトラック抽選分の合計を使用します。0では上限到達後に対象外となり、1では低下しません。All Upgradesは対象外です。|
|`Base Upgrade Draw.WeightFalloffExponent`|2|0.1～10|抽選Weightの低下カーブです。1は一定のペースで低下し、大きい値ほど上限付近まで重みを維持。|
|`Base Upgrade Draw.ChangeAmountWeights`|`-1:10,0:15,1:60,2:15`|`増減値:重み`の組、増減値`-200`～200、重み0～1000|抽選される増減値の相対Weightです。|
|`Base Upgrade Draw Selection.Health`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.Stamina`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.ExtraJump`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.Speed`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.Strength`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.Range`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.Launch`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.TumbleClimb`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.TumbleWings`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.CrouchRest`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.MapPlayerCount`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.DeathHeadBattery`|true|true, false|抽選対象にする。|
|`Base Upgrade Draw Selection.AllUpgrades`|true|true, false|ONの種類だけを対象に、正の一括抽選を有効化。|
|`Base Upgrade Draw Weights.Health`|20|0～1000|Healthの相対Weightです。|
|`Base Upgrade Draw Weights.Stamina`|40|0～1000|Staminaの相対Weightです。|
|`Base Upgrade Draw Weights.ExtraJump`|10|0～1000|Extra Jumpの相対Weightです。|
|`Base Upgrade Draw Weights.Speed`|30|0～1000|Speedの相対Weightです。|
|`Base Upgrade Draw Weights.Strength`|20|0～1000|Strengthの相対Weightです。|
|`Base Upgrade Draw Weights.Range`|30|0～1000|Rangeの相対Weightです。|
|`Base Upgrade Draw Weights.Launch`|10|0～1000|Launchの相対Weightです。|
|`Base Upgrade Draw Weights.TumbleClimb`|10|0～1000|Tumble Climbの相対Weightです。|
|`Base Upgrade Draw Weights.TumbleWings`|10|0～1000|Tumble Wingsの相対Weightです。|
|`Base Upgrade Draw Weights.CrouchRest`|30|0～1000|Crouch Restの相対Weightです。|
|`Base Upgrade Draw Weights.MapPlayerCount`|10|0～1000|Map Player Countの相対Weightです。|
|`Base Upgrade Draw Weights.DeathHeadBattery`|10|0～1000|Death Head Batteryの相対Weightです。|
|`Base Upgrade Draw Weights.AllUpgrades`|1|0～1000|正の結果で使う`ALL UPGRADES`の相対Weightです。|

抽選Weightの初期値はStaminaが40、Speed・Range・Crouch Restが30、Health・Strengthが20、その他の対象アップグレードが10、ALL UPGRADESが1です。大きい値ほど選ばれやすくなります。上限に近づくほどWeightが低下し、上限では`CappedUpgradeWeightMultiplier`の倍率になります。ショップの商品数の設定は影響しません。

#### 役職の抽選設定

各通常役職にはホスト設定`<役職名>.Enabled = true`と`<役職名>.Weight = 100`（範囲0～1000）があり、OFFまたは重み0でランダム抽選から除外します。Courierの設定名は`Courier`です。隠し役職は`???1.Enabled`と`???2.Enabled`で、初期値はともにtrue、重みは固定です。ON/OFFは次の抽選から適用。Twinsは役職重複なしの設定でも2人1組として抽選します。

#### アップグレード・特殊役職設定

この表はすべてホスト設定。

|キー|デフォルト|範囲・値|内容|
|---|---:|---|---|
|`Tank.HealthUpgradeLevels`|21|0～200|Tankに保証する最低Healthレベル。|
|`Tracker.HealthUpgradeLevels`|3|0～200|Healthの最低レベル。より高い基礎強化を維持。|
|`Tracker.EnemyDetectionRange`|25|1～100|壁越しを含む敵の検知距離（m）。|
|`Tracker.DangerAlertRange`|8|0～100|接近警告の距離（m）。検知距離が上限、0で警告無効。|
|`Tracker.NotificationIntervalSeconds`|20|5～120|通常通知の最短間隔（秒）。接近警告は通常の待ち時間を無視しますが、警告同士には同じ間隔を設けます。|
|`Tracker.DistanceChangeThreshold`|5|1～50|更新する距離変化（m）。対象変更、方向が60度変化、上下の変化でも更新。|
|`Runner.SpeedUpgradeLevels`|6|0～200|Runnerに保証する最低Speedレベル。|
|`Runner.StaminaUpgradeLevels`|46|0～200|Runnerに保証する最低Staminaレベル。|
|`Jumper.ExtraJumpUpgradeLevels`|10|0～200|Jumperへ付与するExtra Jumpレベル。|
|`Launcher.LaunchUpgradeLevels`|10|0～200|Launcherへ付与するLaunchレベル。|
|`Climber.ClimbUpgradeLevels`|50|0～200|Climberへ付与するTumble Climbレベル。|
|`Climber.RangeUpgradeLevels`|20|0～200|Climberへ付与するRangeレベル。|
|`Flyer.WingsUpgradeLevels`|10|0～200|Flyerへ付与するTumble Wingsレベル。|
|`Ghost.DeathHeadBatteryUpgradeLevels`|50|0～200|Ghostへ付与するDeath Head Batteryレベル。|
|`Ghost.HealRadius`|5|0～30|頭からの回復範囲（m）。|
|`Ghost.HealAmount`|1|0～100|1回・1人あたりの回復HP。|
|`Ghost.HealIntervalSeconds`|2|0.5～60|死亡中の回復間隔（秒）。|
|`Ghost.TotalHealingLimit`|50|0～10000|味方全員で共有するステージごとの回復上限。蘇生では戻りません。|
|`Ghost.CarrierSpeedBonusLevels`|1|0～20|頭を持つ間のSpeed加算レベル。重複せず、King支援とは強い方を優先。|
|`Bomber.DistancePerGrenade`|8|1～100|グレネードを1個設置するために必要な移動距離。単位はメートルです。|
|`Bomber.MaximumActiveGrenades`|30|1～30|Bomberごとに保持する生成済みグレネードの最大数です。さらに設置すると最も古いものを削除します。|
|`Bomber.AllowTruckSpawns`|false|true, false|トラック内でのグレネード設置を許可します。|
|`Bomber.ExplosiveGrenadesEnabled`|true|true, false|ランダム抽選へバニラのExplosive Grenadeを含めます。|
|`Bomber.StunGrenadesEnabled`|true|true, false|ランダム抽選へバニラのStun Grenadeを含めます。|
|`Bomber.ShockwaveGrenadesEnabled`|true|true, false|ランダム抽選へバニラのShockwave Grenadeを含めます。|
|`Bomber.DuctTapedGrenadesEnabled`|true|true, false|ランダム抽選へバニラのDuct Taped Grenadeを含めます。|
|`Medic.HealAmount`|5|1～100|1回につき周囲の各プレイヤーを回復する量です。|
|`Medic.HealIntervalSeconds`|2|0.1～30|Medicの回復間隔です。|
|`Medic.HealRadius`|5|1～30|回復可能な最大距離。単位はメートルです。|
|`Medic.TotalHealingLimit`|150|1～10000|Medic一人が1ステージで回復できる合計HPです。対象が実際に失っているHPだけを消費します。|
|`Phoenix.ReviveDelaySeconds`|2|2～10|復活までの最低待ち時間。頭が動いている間は、さらに待機します。|
|`Phoenix.RevivalHealth`|25|1～1000|Phoenix復活後のHPです。プレイヤーの最大HPが上限。|
|`Courier.Damage`|1|1～100|トラック外で1回ごとに受けるダメージです。|
|`Courier.DamageIntervalSeconds`|0.1|0.05～10|トラック外でダメージを受ける間隔です。|
|`Rescuer.ReviveDelaySeconds`|2|0～10|死亡後の最低待ち時間。復活できるようになるまで頭をつかみ続けてください。|
|`Rescuer.MaximumRevives`|2|1～10|Rescuer1人あたり、1ステージで復活できる最大回数。|
|`Rescuer.RevivalHealth`|25|1～1000|Rescuerによる復活後のHPです。対象の最大HPが上限。|
|`Vampire.Tier1HealAmount`|5|1～100|周囲でDanger Level 1の敵が死亡した際の回復量です。|
|`Vampire.Tier2HealAmount`|10|1～100|周囲でDanger Level 2の敵が死亡した際の回復量です。|
|`Vampire.Tier3HealAmount`|50|1～100|周囲でDanger Level 3の敵が死亡した際の回復量です。|
|`Vampire.Radius`|10|1～50|死亡した敵からの最大距離。単位はメートルです。|
|`Tuna.StationaryDelaySeconds`|3|0.1～30|停止してからダメージが始まるまでの秒数。|
|`Tuna.Damage`|1|1～100|停止中に1回ごとに受けるダメージです。|
|`Tuna.DamageIntervalSeconds`|0.1|0.05～10|停止中にダメージを受ける間隔です。|
|`Musician.HealAmount`|5|1～100|楽器で音を鳴らすたびに、本人と範囲内の各生存プレイヤーを回復する量です。|
|`Musician.HealRadius`|10|1～50|Musicianから回復可能な最大距離。単位はメートルです。|
|`Mage.CastIntervalSeconds`|3|0.1～30|魔法発動の最短間隔です。|
|`Mage.BeamDurationSeconds`|5|0.1～30|ビーム魔法の継続秒数。手持ちの杖には適用しません。|
|`Mage.StarExpression`|`Angry`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`star`を発動する表情です。|
|`Mage.RollExpression`|`Sad`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`roll`を発動する表情です。|
|`Mage.GravityExpression`|`Suspicious`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`gravity`を発動する表情です。|
|`Mage.VoidExpression`|`EyesClosed`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`void`を発動する表情です。|
|`Mage.LaserExpression`|`Scared`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|`laser`を発動する表情です。|
|`Mage.AutoRecoveryEnabled`|true|true, false|ダメージを受けていないMageのHP自動回復を有効化。|
|`Mage.AutoRecoveryDelaySeconds`|10|0～300|最後にダメージを受けてから自動回復が始まるまでの秒数。|
|`Mage.AutoRecoveryIntervalSeconds`|2|0.1～60|HPを自動回復する間隔です。|
|`Mage.AutoRecoveryAmount`|1|0～100|自動回復1回あたりの回復量です。|
|`Mage.AutoRecoveryTotalHealingLimit`|120|1～10000|プレイヤーごとの、1ステージ中の自動回復の累計上限。他の回復は数えず、死亡・復活・役職変更でも使用済みの回復量はリセットされません。|
|`Mage.StarHealthCost`|10|0～100|`star`の発動成功後に消費するHPです。|
|`Mage.GravityHealthCost`|10|0～100|`gravity`の発動成功後に消費するHPです。|
|`Mage.RollHealthCost`|15|0～100|`roll`の発動成功後に消費するHPです。|
|`Mage.VoidHealthCost`|30|0～100|`void`の発動成功後に消費するHPです。|
|`Mage.LaserHealthCost`|50|0～100|`laser`の発動成功後に消費するHPです。|
|`Gambler.WinChancePercent`|50|0～100|Valuableを破壊せず、勝利倍率を適用する確率。|
|`Gambler.WinValueMultiplier`|2|0～10|勝利時にValuableへ適用する価格倍率。|
|`Gambler.GambitGreenHealAmount`|50|25～1000|Gamblerに対するGambitの緑結果で回復する合計HPです。|
|`Gambler.GambitRedDamage`|100|50～1000|Gamblerに対するGambitの赤結果で受ける合計ダメージです。|
|`Gambler.GambitWhiteHealthUpgradeLevels`|5|0～200|Gambitの白結果で、そのステージ中のみ追加するHealthアップグレードレベル。|
|`Hunter.WeaponBatteryConsumptionPercent`|75|0～100|Hunterが使用する武器バッテリー消費量の通常時に対する割合です。|
|`Hunter.DoubleOrbChancePercent`|10|0～100|Hunterが敵を倒した際、特賞に外れた場合に通常のオーブ個数を2倍にする確率。|
|`Hunter.JackpotOrbChancePercent`|0.5|0～100|Hunterが敵を倒した際、特賞目標を使用する確率。不足分は追加しますが、通常ドロップが目標を上回る場合は減らしません。|
|`Hunter.JackpotOrbCount`|10|1～30|Hunterの特賞で使用する目標オーブ個数です。|
|`Hunter.GuaranteedOrbEveryKills`|5|0～100|追加オーブを保証する対象撃破数。0で保証なし。抽選の追加分を含む。|
|`Stinker.DistancePerCloud`|2|1～100|ウラン雲の通過地点を記録する間隔です。|
|`Stinker.MinimumSafetyDistance`|2|1～30|最新の発生待ち地点にウラン雲を発生させるために必要なStinker本人との最低距離。古い待機地点は順番に発生します。|
|`Stinker.AllowTruckSpawns`|false|true, false|トラック内でウラン雲の地点記録と発生を許可します。|
|`Trickster.ActiveSeconds`|25|1～120|設置したデコイを削除するまでの秒数。|
|`Trickster.DecoyExpression`|`Happy`|`Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy`|デコイを設置する表情です。|
|`Trickster.CooldownSeconds`|45|1～300|敵を引きつけたデコイの終了後、次に設置できるまでの秒数。|
|`Trickster.NoTargetCooldownSeconds`|10|0～300|敵を一体も引きつけなかったデコイの終了後、次に設置できるまでの秒数。|
|`Trickster.InvestigateRadius`|40|1～100|デコイを中心に敵へ調査を指示する半径です。単位はメートルです。|
|`Trickster.PulseIntervalSeconds`|1|0.25～10|敵へ調査を指示する間隔です。|
|`Trickster.PlacementDistance`|2|0.5～10|Tricksterの前方へデコイを設置する距離。単位はメートルです。|
|`Mechanic.RepairPercentPerSecond`|2|0～100|Mechanicが破損Valuableを直接保持している間、毎秒修復する元価格に対する合計割合です。|
|`Mechanic.MaximumRepairPercentPerStage`|50|0～100|Mechanic一人が1ステージで修復できる元価格に対する合計割合です。|
|`Electrician.ChargePercentPerSecond`|10|0～100|Electricianが使用していない充電可能アイテムを直接保持している間、毎秒回復する合計バッテリー割合です。|
|`Electrician.MaximumChargePercentPerStage`|100|0～1000|Electrician一人が1ステージで回復できる合計バッテリー割合です。|
|`Warden.AdditionalStunSeconds`|3|0～30|Wardenが敵へ発生させたスタンに追加する秒数。|
|`Ninja.VisionRecognitionMultiplier`|2|1～10|敵が視覚でNinjaを認識するまでの時間へ適用する倍率。|
|`Executioner.StunnedDamageMultiplier`|2|1～10|攻撃前からスタンしている敵への直接攻撃ダメージ倍率。|
|`Rider.EnemyDamageMultiplier`|3|1～10|Riderが運転中の車両が敵へ与える衝突ダメージ倍率。|
|`Rider.PlayerKnockbackMultiplier`|2|1～10|プレイヤーへ元から発生する車両Tumbleノックバック倍率。|
|`Influencer.PlayerRadius`|20|1～100|周囲の生存中の仲間を数える半径です。単位はメートルです。|
|`Influencer.PlayerCheckIntervalSeconds`|0.5|0.1～10|周囲人数と動的アップグレードを確認する間隔です。|
|`Influencer.NoiseRadiusMultiplier`|2|1～10|足音、着地音、VC、チャットTTSの敵探知範囲倍率。|
|`Influencer.TTSInvestigateRadius`|20|1～100|定期TTSが発生させる敵探知範囲です。|
|`Influencer.MinimumTTSIntervalSeconds`|20|1～300|定期TTS間隔を抽選する最小秒数。|
|`Influencer.MaximumTTSIntervalSeconds`|40|1～300|定期TTS間隔を抽選する最大秒数。最小値より小さい場合は、小さい方を最小値として使用します。|
|`Influencer.TTSQuietPeriodSeconds`|1.5|0～10|本人のTTS終了後、定期発言までに必要な無音時間。|
|`Influencer.<Upgrade>UpgradeScaling`|アップグレードごと|`人数:レベル`の組|周囲人数に応じた最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`1:1,2:4,3:9,4:13,5:20`です。対象はBase Upgradesと同じで、空欄ならレベルを付与しません。|
|`Werewolf.PlayerDamageMultiplier`|2|1～10|Werewolfが他のプレイヤーへ与える特定可能なダメージ倍率。|
|`Berserker.<Upgrade>UpgradeScaling`|アップグレードごと|`HP割合:レベル`の組|残りHPが指定割合以下の間に使う最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`80:1,60:4,40:9,20:13,10:20`です。対象はHealthを除くBase Upgradesと同じで、空欄ならレベルを付与しません。|
|`Bodyguard.HealthUpgradeLevels`|11|0～200|BodyguardのHealth目標値。|
|`Bodyguard.ProtectionRadius`|15|1～100|仲間を保護できる最大距離。単位はメートルです。|
|`Bodyguard.DamageSharePercent`|50|0～100|BodyguardのHPを1残して肩代わりする敵ダメージ割合です。|
|`Rammer.TumbleAttackDamage`|100|0～100000|RammerのTumble Attackが敵へ与えるダメージです。|
|`Rammer.SelfDamage`|15|0～100000|RammerのTumble Attackが敵へ命中した後、自身が受けるダメージです。|
|`Diver.UnderfloorDurationSeconds`|10|1～120|Diverが死亡せず床下に滞在できる最大秒数。|
|`Diver.MovementForce`|8|1～30|Diverが床下を自由移動するときの力です。|
|`Sniper.ReferenceDistance`|6|1～50|敵へのダメージが1倍になる距離。単位はメートルです。|
|`Sniper.MinimumDamageMultiplier`|0.5|0～1|密着時の敵ダメージ倍率。元が正のダメージなら最低1ダメージを与えます。|
|`Sniper.MaximumDamageMultiplier`|2|1～10|遠距離での敵ダメージの最高倍率。|
|`Sniper.MaximumMultiplierDistance`|18|1～100|最高倍率へ到達する距離。基準距離以下の場合は、基準距離の直後として扱います。|
|`Avenger.DamageMultiplier`|1.5|1～10|他のプレイヤーが死亡した後の、敵へのダメージ倍率。|
|`Avenger.DurationSeconds`|30|1～120|敵へのダメージ増加が続く秒数。別の死亡で残り時間を更新します。|
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

InfluencerとBerserkerの記述式は、`条件:レベル`をカンマまたはセミコロンで区切ります。Influencerは到達した人数条件のうち最後の目標値、Berserkerは現在到達している最も低いHP境界の目標値を適用。目標値が0になる組はデフォルトの記述から省略し、明示的に0へ上書きしたい場合だけ`条件:0`を手動で追加します。レベルはMap Player Countだけ0～1、ほかは0～100です。形式が正しくない項目は無視されます。

### プレイヤー向けツール

Escまたはロビーメニュー右上の`ROLES`から、`TOOLS`または`DRAW HISTORY`を選択します。

- **HUD編集：** `TOOLS`でサンプルをドラッグし、基準位置・整列・文字／アイコンサイズ・全体倍率・表示形式・表示切替を調整します。「保存」で個人設定を保存、「取消」またはEscで破棄、「初期値」で初期状態をプレビューします。ロビーでもマウスで操作可能です。
- **不具合レポート：** `TOOLS`からコピーまたはファイルを開き、内容と発生手順を確認してGitHub Issuesへ投稿します。MODのバージョン・設定・最近の動作状況を含み、`BepInEx/RoleShuffleReports`へ保存します。自動送信はしません。
- **言語：** `UI.GuideLanguage`とRoles内の切替は同じ選択を保存します。初期値は英語、全14言語を各言語の名称で表示。メニュー・説明・HUD編集・履歴・同期状態に適用し、役職名・アップグレード識別名・コマンド・診断本文は英語です。
- **同期状態：** `TOOLS`で役職・ガイド・Base Upgrade・履歴が最新か確認します。「表示データを再取得」は表示だけを更新し、役職や強化値を変えません。
- **抽選履歴：** 完了したトラック抽選の直近50回を表示。レベル・対象・抽選値・実際の目標値の前後を記録し、変化なしや上限到達も残します。ホストのセーブに保存してMOD導入済み参加者と共有し、中断した抽選は記録しません。

`HUD.FontSize`の初期値は28（範囲16～48）、`UI.GuideLanguage`の初期値は`English`です。どちらも個人設定。HUDの初期表示形式は`NameOnly`です。

### 通知とHUD

- **残量HUD：** 回復・蘇生・修理・充電・賭けの残量を、スタミナの下に水色のアイコンと数字で縦1列に表示。画面に収まるよう大きさを調整し、使い切った項目は赤く表示。MODを導入している本人の生存中に表示され、`HUD.ResourceHud*`で調整可能です。Signalmanの通信待ち時間も表示。
- `CURRENT ROLES`と`ROLE GUIDE`の役職にエンブレムを表示し、HUDでも設定で表示可能です。六角形のエンブレムの外側は透過表示です。未開示の隠し役職は共通の「?」エンブレムで表示し、開示時に役職固有のエンブレムへ切り替わります。エンブレムはMOD導入済みのプレイヤーに表示されます。
- ステージ開始時、各プレイヤーは割り当てられた英語の役職名をバニラのチャット／TTSで発言します。
- RoleShuffleが生成する通知TTSでは敵が反応しません。通常のマイク入力やその他のワールド音は、Ninjaで抑止される場合を除いてバニラの動作を維持。
- Stage Flux導入時は、ステージ開始通知が重ならないようStage Flux用の通知遅延を使用します。
- Stage FluxのTTS終了後、1.5秒間の無音を確認してから役職を通知します。
- 最初の役職通知は全員で1件ずつ順番に発話します。以後の通知・応答は本人のチャット／TTSだけを待ち、別の人とは同時に発話可能です。
- Influencerの発言とInfluenzaのくしゃみは本人のメッセージを待ち、敵にも聞こえます。Diverは残り時間を15秒刻み（30、15）、残り10秒から0秒までは毎秒発話します。前の発話を中断してカウントを優先し、通常の通知を待機させます。
- `ROLES`一覧は初期設定で左下に名前のみを表示し、自分を固定して5秒ごとにページを切り替えます。`HUD.PlayersPerPage`が6以上なら多言語・アイコン付きでも6人分を確保し、6未満は設定を優先します。長いプレイヤー名は、役職名が見えるよう短く表示。表示・サイズ設定は上表を参照してください。
- Base Upgradeの抽選演出は、ホストとRoleShuffleを導入している参加者に表示されます。
- Esc／ロビー右上の`ROLES`から開きます。Escでは`CURRENT ROLES`、ロビーでは`ROLE GUIDE`を表示。自分が先頭に並び、プレイヤーをクリックすると説明を開閉可能です。`BASE UPGRADES`では共有目標・設定値・累積抽選値を確認し、参加者にはホストの値を表示。
- `ROLE GUIDE`では有効な役職の説明を選択した言語で表示。マルチプレイではホストの設定値に沿った説明になります。選択言語の説明がない場合は英語で表示。シングルプレイでは自分の設定を使用します。

### 互換性

Stage Fluxは任意の対応MODであり、RoleShuffleの必須MODではありません。

- Second ChanceをPhoenix、Rescuer、Bodyguardの復活効果より優先し、使用されなかった復活効果は維持。
- Second ChanceまたはPhoenixが失敗時のステージ移行を止めた場合、現在の役職は維持されます。
- Value SurgeまたはValue Crash中も、Mechanicは実際に失われた貴重品価値だけを修復します。
- 最初の役職通知はStage Fluxの通知を待ちます。以後は、同じプレイヤーによるStage Fluxの発話だけを待ちます。
- Enemy Purgeによる撃破ではHunterの報酬は発生しません。
- Dangerous ValuablesはEngineerが保護している貴重品へ効果を与えません。

Elite Enemy Variantsは任意の対応MODであり、RoleShuffleの必須MODではありません。

- 生存中の`Ninja`は、Elite Enemy Variantsの「見られていないプレイヤー」を条件とする攻撃対象から除外されます。死亡中のNinjaとその他のプレイヤーは対象のままです。
- `Compatibility.EnhancedEnemyRewardsEnabled`がtrueの場合、Enhanced個体から得られるVampireの回復とHunterの追加オーブがTier 3相当まで強化されます。通常のオーブ数は変わりません。
- Enhanced Gambitが捕獲後に移動しても、Gamblerのルーレット効果は正しいプレイヤーへ適用されます。
- RoleShuffleのHUDやメニューへElite Enemy Variantsの敵数、状態、撃破通知は追加しません。

### ゲームプレイ上の注意

- Twinsはホストのみ導入で動作します。同時に被弾・回復したとき、共有HPへの反映量に多少の差が出る場合があります。

- `Bomber`が残す起動済みグレネードは、プレイヤーや貴重品にも被害を与えます。
- `Mage`の魔法はプレイヤーや貴重品にも被害を与えます。魔法名の大文字・小文字は問いません。ビームは視点に追従し、MOD導入側では杖を表示しません。未導入の参加者には一時的な杖が見えます。
- `Courier`と`Tuna`は条件を満たしている間、実際に継続ダメージを与え、死亡する可能性があります。受けたダメージは自動回復しません。
- `Medic`は自身を回復しません。
- `Phoenix`は1ステージに1回だけ自己復活します。`Rescuer`は設定された回数まで、死亡した仲間の頭をつかんで復活させます。復活後HPは役職ごとに設定でき、デフォルトは25です。
- 役職の強化はステージ終了時に基礎値へ戻ります。使用分の保持がONなら、その分も加算します。Throwは残ります。
- 役職の最大HP増加が解除されると、残りHPの割合を維持して端数を切り捨てます（生存中は最低1HP）。例：300/520 → 69/120。
- すべての役職を無効化するか、すべての`Weight`を0にすると、ランダム抽選できる役職がなくなります。

[i01]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/01.png
[i02]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/02.png
[i03]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/03.png
[i04]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/04.png
[i05]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/05.png
[i06]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/06.png
[i07]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/07.png
[i08]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/08.png
[i09]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/09.png
[i10]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/10.png
[i11]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/11.png
[i12]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/12.png
[i13]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/13.png
[i14]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/14.png
[i15]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/15.png
[i16]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/16.png
[i17]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/17.png
[i18]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/18.png
[i19]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/19.png
[i20]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/20.png
[i21]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/21.png
[i22]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/22.png
[i23]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/23.png
[i24]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/24.png
[i25]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/25.png
[i26]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/26.png
[i27]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/27.png
[i28]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/28.png
[i29]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/29.png
[i30]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/30.png
[i31]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/31.png
[i32]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/32.png
[i33]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/33.png
[i34]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/34.png
[i35]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/35.png
[i36]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/36.png
[i37]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/37.png
[i38]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/38.png
[i39]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/39.png
[i40]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/40.png
[i41]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/41.png
[iu]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/980c0419ea924e2008e9c963ed68ff67eedd5a04/docs/icons/unknown.png
[i42]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/2207c9a37f217fcea8399ef3efdc0945ce62a04e/docs/icons/42.png
[i43]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/d5f615a8cf9ee60a6b0ae98104a051d2ad64a193/docs/icons/43.png

[i44]: https://raw.githubusercontent.com/CapacityDown/RoleShuffle/06ef1e70cf0efdaba265cee9138ba3af5eea994d/docs/icons/44.png
