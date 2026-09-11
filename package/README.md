# RoleShuffle

## English

### Overview

RoleShuffle assigns every player a random role when a stage begins and clears all assigned roles when the stage ends. Each role specializes in a distinct gameplay theme built from vanilla upgrades or effects. Configurable base upgrade targets can scale with the current run level and remain active between stages. When a role specializes in an upgrade, the role's configured level replaces that base level for the stage; every other managed upgrade stays at its base level.

Only the host needs RoleShuffle for gameplay effects. Sessions of up to 30 players are supported. Players without the mod receive their role, upgrades, effects, and vanilla chat/TTS announcement normally. Participants who also install RoleShuffle can use the full role HUD.

By default, upgrade items are removed from the shop pool so stage roles remain the source of temporary upgrades.

### Contact

For questions, bug reports, or feedback, please use the [contact form](https://forms.gle/nYKsoqV3KKtX94t57).

### Requirements

- BepInExPack 5.4.2305 or a later compatible version
- REPOConfig 1.2.6 or a later compatible version
- MenuLib 2.5.2 or a later compatible version

### Installation

Install with a compatible mod manager, or place `RoleShuffle.dll` in the profile's `BepInEx/plugins` directory. Only the host needs the mod for gameplay effects.

### Multiplayer

- The host assigns roles and controls all gameplay settings.
- Vanilla participants are fully supported and do not need RoleShuffle.
- Installed participants receive the full role HUD and the scrollable `Roles` page in the Escape menu; each player can choose their own HUD layout.
- Single-player uses the same role system, but `Tracker`, `Ghost`, `Medic`, `Jobless`, `Rescuer`, `Influencer`, `Werewolf`, `Bodyguard`, `Imitator`, and `Avenger` are excluded from random assignment when only one player is present.

### Role command

- Enter `/roles` in the in-game chat to check your own assigned role.
- Vanilla participants can use this command when the host has RoleShuffle installed.
- Outside an active stage, the response is `YourRole:Unavailable`.

### How roles are assigned

- One role is selected for each player at the start of a playable stage.
- Enabled roles are selected by relative `Weight`.
- With `General.UniqueRoles = true`, duplicate roles are avoided until every eligible role has been used once.
- `King` is assigned to at most one player per stage.
- By default, Showcase roles (`Bomber`, `Stinker`, `Mage`, `Gambler`, `Trickster`, `King`, `Rider`, `Influencer`, `Diver`) and Support roles (`Medic`, `Rescuer`, `Mechanic`, `Electrician`, `Warden`, `Bodyguard`) receive configurable minimum guarantees based on party size.
- By default, Showcase and Support minimums rise to four each at 24 players. Danger limits rise from one to two at 8 players and three at 16 players; Hardship limits rise from one to two at 12 players. Influencer is a Showcase role and is not treated as a Danger role. A party of four or fewer receives at most one role across both risk groups.
- A role found among a player's five most recent assignments uses half of its normal selection weight for that player. Other players' histories do not affect that player's selection.
- A player normally cannot receive the same role in consecutive stages. After receiving `Jobless` or `Tuna`, that player is excluded from both Hardship roles for the next two stages. These restrictions are relaxed only when needed to avoid leaving a player without a role.
- `Tank`, `Runner`, `Jumper`, `Lifter`, `Launcher`, `Climber`, `Flyer`, `Tracker`, and `Ghost` are excluded from random assignment whenever any matching base upgrade target is equal to or higher than that role's configured target, including increases from truck draws. Forced test-role assignment is unaffected.
- `Influencer` is excluded from random assignment when none of the upgrade targets reachable with the current party size exceed the current Base Upgrades. Forced role assignment is unaffected.
- `Tracker`, `Ghost`, `Medic`, `Jobless`, `Rescuer`, `Influencer`, `Werewolf`, `Bodyguard`, `Imitator`, and `Avenger` are not selected in single-player or a one-player session.
- `Imitator` is selected only after another active player has received a role it can copy.
- By default, context-dependent roles are excluded when their ability has no usable target. This includes `Musician`, `Engineer`, `Electrician`, and `Rider` when their required object is absent, `Sniper` when neither a melee weapon nor a gun is available, and `Brawler` when no melee weapon is available. Weapon-like valuables do not count as weapons for either role's assignment, and staffs do not count for Sniper assignment.
- During a stage, the participant list is checked regularly. A player is removed after being absent for ten consecutive checks. A returning player regains the previous role, while a newly joined player receives a role, upgrades, announcement, and HUD entry.
- Roles and their stage effects are cleared only when the stage actually ends. They remain active if a failed-stage transition is canceled by a revival effect.

### Roles

| Role | Function and default effect | Important limitation or risk | Configurable values |
|---|---|---|---|
| Tank | Increases maximum health by setting Health to level 21. | Uses the vanilla Health upgrade and has no separate active ability. | Health target `0`–`100` |
| Runner | Increases movement speed and stamina for faster, longer sprints. Speed is level 6 and Stamina is level 46. | Uses the two vanilla upgrades and has no separate active ability. | Speed and Stamina targets `0`–`100` |
| Jumper | Adds up to 10 extra jumps before landing by setting Extra Jump to level 10. | Uses the vanilla Extra Jump upgrade and has no separate active ability. | Extra Jump target `0`–`100` |
| Lifter | Increases grab strength, making heavy objects easier to handle. Strength is level 25. | Uses the vanilla Strength upgrade and has no separate active ability. | Strength target `0`–`100` |
| Launcher | Launches the player farther forward when starting a Tumble. Launch is level 10. | Uses the vanilla Launch upgrade and has no separate active ability. | Launch target `0`–`100` |
| Climber | Improves Tumble climbing and allows objects to be grabbed from farther away. Tumble Climb is level 50 and Range is level 20. | Uses the two vanilla upgrades and has no separate active ability. | Tumble Climb and Range targets `0`–`100` |
| Flyer | Keeps Tumble Wings active longer for extended movement through the air. Tumble Wings is level 10. | Uses the vanilla Tumble Wings upgrade and has no separate active ability. | Tumble Wings target `0`–`100` |
| Tracker | Increases maximum health and allows map tools to track another player. Health is level 3 and Map Player Count is level 1. | Map Player Count is fixed at `1`. Never selected randomly when only one player is present. | Health target `0`–`100` |
| Ghost | Sets Death Head Battery to level 50. | Never selected randomly when only one player is present. | Battery target |
| Bomber | Drops a random armed grenade every 8 m traveled. Up to 30 generated grenades remain active per Bomber, and the oldest is removed at the limit. | Grenades can injure players and damage valuables. Only Bomber can hold generated grenades. Movement inside the truck does not count while truck placement is disabled. | Distance, limit, truck placement, grenade types |
| Medic | Heals nearby teammates for 5 HP every 2 seconds within 5 m, up to 150 total HP per stage. | Never heals the Medic. Only living teammates within range are affected. Healing stops when that Medic's stage limit is exhausted. Never selected randomly when only one player is present. | Amount, interval, radius, total limit |
| Phoenix | Automatically revives itself with 25 HP once per stage after dying. | One use per stage. Revival HP cannot exceed the player's maximum HP. | Revival HP, revival delay, failure grace |
| Jobless | Takes 1 damage every 0.1 seconds while outside the truck. | Can kill the player. Never selected randomly when only one player is present. | Damage, interval |
| Rescuer | While alive, approaching within 3 m of a dead teammate's Death Head revives the nearest eligible teammate with 25 HP. | Up to 2 revivals per stage. Revival HP cannot exceed the target's maximum HP. Never selected randomly when only one player is present. | Revival HP, delay, radius, maximum revivals |
| Vampire | Heals when an enemy dies within 10 m: Tier 1 = 5, Tier 2 = 10, Tier 3 = 50. | Uses the enemy's vanilla Danger Level. When Enhanced enemy rewards are enabled with Elite Enemy Variants, Enhanced enemies count one tier higher up to Tier 3. The Vampire must be alive and close to the dying enemy. | Amount per tier, radius |
| King | Receives the vanilla Crown for the stage. | Only one King can be assigned. The previous Crown holder is restored when the role ends. | Selection only |
| Tuna | After a 5-second grace period at stage start, standing still for 3 seconds causes 1 damage every 0.1 seconds until moving. | Can kill the player. The stationary timer starts after the initial grace period. Moving resets it immediately; lost health is not restored. | Delay, damage, interval |
| Musician | Each instrument note played by the Musician heals the Musician and living players within 10 m for 5 HP. | Requires a vanilla musical valuable. By default, not randomly selected if none is present. | Amount, radius |
| Mage | Uses `star`, `gravity`, `roll`, `void`, or `laser` in chat, or the configured facial expressions, to cast vanilla attacks for 10, 10, 15, 30, or 50 HP. After 10 seconds without damage, restores 1 HP every 2 seconds, up to 120 HP total per stage. Magic cast using a held staff lasts 1.3 times as long. | Selecting or clearing a matching expression activates its spell. Spells share a 3-second cooldown, cannot be cast if the cost would be fatal, and can harm players or valuables. The duration bonus does not apply to chat or expression casts, or the Star Wand's instantaneous attack. | Facial expression per spell, cast cooldown, health cost per spell, automatic recovery toggle, delay, interval, amount, and stage limit |
| Gambler | A held valuable has a 50% chance to double its current value; otherwise it is destroyed. Gambit green heals 50 HP, red deals 100 damage, black kills, and white fully heals and adds 5 Health levels for the stage. | One wager is available at a time and returns after extraction. Damaged valuables use their reduced value. | Win chance, win multiplier, Gambit green heal, red damage, white temporary Health levels |
| Hunter | Held or equipped weapons use 75% of normal battery. Enemies killed by Hunter have a 10% chance to drop twice as many orbs and a 0.5% chance to drop 10. | Only Hunter's held or equipped weapons and confirmed kills receive these effects. When Enhanced enemy rewards are enabled with Elite Enemy Variants, Enhanced enemies raise only the quality tier of Hunter's added orbs; the normal orb count is unchanged. | Battery consumption, both orb chances, jackpot count |
| Stinker | Leaves a harmful uranium cloud after every 2 m traveled. Clouds appear one at a time after Stinker moves at least 2 m away from the newest location. | Clouds can damage other players, and Stinker can be hurt by walking back into one. Truck placement is disabled by default. | Distance, safety distance, truck placement |
| Engineer | Prevents supported effect valuables from activating while the Engineer holds them. | Camera, Propane Tank, Snowmobile, Flashlight, Clown Doll, and Love Potion are not affected. By default, not randomly selected if no supported effect valuable is present. | Selection only |
| Trickster | Type `decoy` in chat or select or clear the configured facial expression to place a fixed Scream Doll that repeatedly attracts enemies within 40 m for 25 seconds. | Another decoy cannot be placed while one is active. Its cooldown begins after the decoy ends: 45 seconds if it attracted an enemy, or 10 seconds if it attracted none. The decoy cannot be grabbed, damaged, or delivered. | Decoy facial expression, active time, normal and no-target cooldowns, radius, pulse interval, placement distance |
| Mechanic | While directly holding damaged valuables, restores a shared 2% of original sale value per second, up to 50 percentage points per stage. | Only value that is actually restored counts toward the stage limit. Fully repaired and non-valuable objects are ignored. Multiple held valuables share the same repair rate and stage limit. The sale value is restored; damaged visual appearance may remain. | Repair rate, total stage limit |
| Electrician | While directly holding inactive rechargeable items, restores a shared 10 battery percentage per second, up to 100 percentage points per stage. | Active, full, and unchargeable items are ignored. By default, not randomly selected if no rechargeable item is present. | Charge rate, total stage limit |
| Warden | Extends enemy stuns caused by Warden's weapons, grenades, or held damaging objects by 3 seconds. | The attack must normally be able to stun the enemy. | Additional stun time |
| Ninja | Footsteps, landings, voice chat, and chat TTS do not draw enemy investigation. Enemies take twice as long to recognize Ninja visually. | Close contact and noises from weapons, valuables, impacts, explosions, or hazards can still reveal Ninja. | Visual recognition multiplier |
| Executioner | Deals 2x direct attack damage to enemies that were already stunned before the hit. | The hit that initially causes the stun receives no bonus. Physics-only collision damage is unchanged. | Stunned-enemy damage multiplier |
| Rider | While driving a vanilla vehicle, deals 3x impact damage to enemies and doubles existing Tumble knockback against players. | Player damage is not increased. By default, not randomly selected if no vanilla vehicle is present. | Enemy damage and player knockback multipliers |
| Influencer | Gains stronger upgrades as more living teammates enter a 20 m radius and periodically speaks an English TTS line. | Its footsteps, landings, VC, and chat TTS reach enemies from twice as far away. | Radius, check rate, per-upgrade player-count scaling, noise multiplier, TTS timing and radius |
| Werewolf | Deals 2x damage to another player when Werewolf can be identified as the attacker. | Self-damage, environment damage, and damage with no identifiable player attacker are unchanged. Never selected randomly when only one player is present. | Player damage multiplier |
| Berserker | Gains stronger upgrades as remaining HP becomes lower. | Bonuses decrease again when healed. | Per-upgrade HP-percentage scaling |
| Bodyguard | Sets Health to level 11 and absorbs 50% of enemy damage dealt to a teammate within 15 m. | Transferred damage cannot reduce Bodyguard below 1 HP. | Health target, radius, damage share |
| Rammer | Tumble Attack deals 100 damage to enemies and deals 15 damage to Rammer after a successful hit. | Launch, Tumble Climb, and Tumble Wings are fixed at level 0 for the stage regardless of Base Upgrades. | Tumble Attack and self damage |
| Diver | Tumbling while continuing to look down at a fixed floor enables movement below it for up to 10 seconds. | Failing to return through a floor in time kills Diver. Returning above the floor starts a cooldown equal to 1.5 times the previous dive time. The countdown and ready state are announced by TTS. | Underfloor duration, movement force |
| Sniper | Enemy damage changes continuously with distance: 0.5x at point-blank range, 1x at 8 m, and up to 2x at 24 m. | Applies only when Sniper can be identified as the attacker. Vehicle impacts and Tumble Attacks are unchanged. | Reference distance, minimum and maximum multipliers, maximum-multiplier distance |
| Imitator | Starts with Base Upgrades only. Grabbing another player's health-transfer point copies that teammate's upgrades, abilities, and drawbacks for the rest of the stage. The assignment list then shows the copied role beside Imitator. | Cannot copy Imitator, King, Bomber, Stinker, Werewolf, Jobless, Tuna, or ???1 or ???2. The first valid copy is fixed for the stage. Never selected randomly when only one player is present. | Selection only |
| Avenger | When another player dies within 30 m, deals 1.5x damage to enemies for 20 seconds. | Activation, duration refreshes, and expiration are announced by TTS. Another nearby death refreshes the duration without stacking the multiplier. Its own death does not activate the effect. Never selected randomly when only one player is present. | Trigger radius, damage multiplier, and duration |
| Brawler | Deals 1.25x damage with identifiable melee weapon attacks and 0.75x damage with identifiable guns, staff projectiles, and lasers. | Vehicle impacts, Tumble Attacks, grenades, and ordinary held-object collisions are unchanged. | Melee and ranged damage multipliers |
| ???1 | ??? | ??? | ??? |
| ???2 | ??? | ??? | ??? |

### Configuration

All settings are available through REPOConfig. Host-controlled settings affect the session. Local settings affect only the installed player's HUD.

When an older configuration is detected, RoleShuffle keeps compatible customized values, carries supported renamed settings forward, and updates settings that still use older defaults. Before reorganizing the current settings, the original file is saved as `REPOJP.RoleShuffle.cfg.pre-v4.4.0.bak`.

#### General, notifications, and HUD

| Key | Default | Range / values | Effect | Control |
|---|---:|---|---|---|
| `General.Enabled` | `true` | `true`, `false` | Enables stage role assignment. | Host |
| `General.UniqueRoles` | `true` | `true`, `false` | Avoids duplicates until every enabled eligible role has been assigned once. | Host |
| `General.ShopUpgradeItemCount` | `0` | `0`–`30` | Requests this number of upgrade items for each shop. `0` removes upgrade items from the shop pool. | Host |
| `Compatibility.EnhancedEnemyRewardsEnabled` | `true` | `true`, `false` | When enabled, Elite Enemy Variants Enhanced enemies count one reward tier higher for Vampire healing and Hunter bonus-orb quality. | Host |
| `Notifications.Enabled` | `true` | `true`, `false` | Enables role-name and role-effect chat and TTS notifications. | Host |
| `Notifications.DelaySeconds` | `2.5` | `0`–`30` | Delay before role announcements without Stage Flux. | Host |
| `Notifications.StageFluxDelaySeconds` | `5` | `0`–`30` | Minimum delay before role announcements when Stage Flux is installed. RoleShuffle also waits for Stage Flux TTS to finish and for a 1.5-second quiet period. | Host |
| `HUD.Enabled` | `true` | `true`, `false` | Shows the current roles during a stage. | Local |
| `HUD.RoleDisplay` | `NameOnly` | `IconAndName`, `NameOnly`, `IconOnly` | Shows role icons and names, names only, or icons only. Player names remain visible. | Local |
| `HUD.IconSize` | `64` | `32`–`128` | Role icon size before HUD scaling. Row spacing and the number of players per page adjust automatically. | Local |
| `HUD.Anchor` | `BottomLeft` | `TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight` | Selects the HUD anchor. | Local |
| `HUD.Alignment` | `Left` | `Left`, `Center`, `Right` | Selects the role text alignment. | Local |
| `HUD.OffsetX` | `0` | `-3840`–`3840` | Horizontal offset from the anchor in pixels. | Local |
| `HUD.OffsetY` | `80` | `-2160`–`2160` | Vertical offset from the anchor in pixels. | Local |
| `HUD.ScalePercent` | `70` | `50`–`200` | HUD display scale percentage. | Local |
| `HUD.PlayersPerPage` | `8` | `2`–`20` | Maximum players shown at once, including the pinned local player. The actual count may be lower to fit the icons and font height. | Local |
| `HUD.PageIntervalSeconds` | `5` | `1`–`30` | Seconds each role page remains visible. | Local |
| `HUD.TransitionDurationSeconds` | `0.2` | `0`–`1` | Duration of the fade between role pages. | Local |
| `HUD.PinLocalPlayer` | `true` | `true`, `false` | Keeps the local player's role visible on every page. | Local |

#### Role balance

Showcase roles are `Bomber`, `Stinker`, `Mage`, `Gambler`, `Trickster`, `King`, `Rider`, `Influencer`, and `Diver`. Support roles are `Medic`, `Rescuer`, `Mechanic`, `Electrician`, `Warden`, and `Bodyguard`. Danger roles are `Bomber`, `Stinker`, and `Werewolf`; Hardship roles are `Jobless` and `Tuna`. Influencer is not a Danger role. If these rules leave no eligible role, RoleShuffle gradually loosens the limits so every player can still receive a role.

| Key | Default | Range / values | Effect | Control |
|---|---:|---|---|---|
| `Role Balance - Guarantees.ShowcaseEnabled` | `true` | `true`, `false` | Enables party-size-based minimum Showcase assignments. | Host |
| `Role Balance - Guarantees.ShowcaseMinimums` | `3:1,7:2,14:3,24:4` | `party size:minimum` pairs | Minimum Showcase assignments by party size. | Host |
| `Role Balance - Guarantees.SupportEnabled` | `true` | `true`, `false` | Enables party-size-based minimum Support assignments. | Host |
| `Role Balance - Guarantees.SupportMinimums` | `4:1,8:2,14:3,24:4` | `party size:minimum` pairs | Minimum Support assignments by party size. | Host |
| `Role Balance - Limits.Enabled` | `true` | `true`, `false` | Enables Danger, Hardship, and small-party combined limits. | Host |
| `Role Balance - Limits.DangerMaximums` | `1:1,8:2,16:3` | `party size:limit` pairs | Maximum simultaneous `Bomber`, `Stinker`, and `Werewolf` roles by party size. | Host |
| `Role Balance - Limits.HardshipMaximums` | `1:1,12:2` | `party size:limit` pairs | Maximum simultaneous `Jobless` and `Tuna` roles by party size. | Host |
| `Role Balance - Limits.SmallPartyMaximumPlayers` | `4` | `1`–`30` | Largest party size using the combined Danger and Hardship limit. | Host |
| `Role Balance - Limits.SmallPartyCombinedMaximum` | `1` | `1`–`30` | Maximum combined Danger and Hardship roles in a small party. | Host |
| `Role Balance - Variety.PreventSameRole` | `true` | `true`, `false` | Prevents the same player receiving the same role in consecutive stages when alternatives exist. | Host |
| `Role Balance - Variety.HardshipCooldownStages` | `2` | `0`–`10` | Stages after `Jobless` or `Tuna` during which that player is normally excluded from both roles. | Host |
| `Role Balance - Variety.ExcludeUnavailableRoles` | `true` | `true`, `false` | Excludes context-dependent roles when their required enemy, weapon, or usable object is absent. | Host |

#### Base upgrades

These host settings define the upgrade target used whenever a role does not override that upgrade. Enter comma-separated `run level:value` pairs; the last entry at or below the current run level is used. Run levels accept 1–999999. For example, `1:1,5:3,10:6` uses value 1 on levels 1–4, value 3 on levels 5–9, and value 6 from level 10 onward. Blank settings and levels before the first entry use 0. Numeric values outside the allowed range are adjusted to the nearest limit; malformed pairs are ignored. Base levels remain active outside stages. When the optional truck draw is enabled, leaving the shop starts a draw in the following truck preparation phase. Each draw selects one upgrade type using its configured relative weight. Change amounts use comma-separated `change amount:weight` pairs; the defaults are -1 (10), no change (15), +1 (60), and +2 (15). A change amount is excluded when no selectable target can receive it without crossing the current limit, and the remaining weights are used. Positive results may select `ALL UPGRADES`, which strengthens every supported Base Upgrade that has room below its limit. Changes remain for the rest of that run, and the host announces the result. Throw is excluded and is never changed by RoleShuffle.

| Key | Default | Range / values | Effect | Control |
|---|---:|---|---|---|
| `Base Upgrades.HealthUpgradeLevels` | `1:1` | `level:value` pairs; value `0`–`200` | Base Health target by run level. | Host |
| `Base Upgrades.StaminaUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Stamina target by run level. | Host |
| `Base Upgrades.ExtraJumpUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Extra Jump target by run level. | Host |
| `Base Upgrades.SpeedUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Speed target by run level. | Host |
| `Base Upgrades.StrengthUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Strength target by run level. | Host |
| `Base Upgrades.RangeUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Range target by run level. | Host |
| `Base Upgrades.LaunchUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Launch target by run level. | Host |
| `Base Upgrades.TumbleClimbUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Tumble Climb target by run level. | Host |
| `Base Upgrades.TumbleWingsUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Tumble Wings target by run level. | Host |
| `Base Upgrades.CrouchRestUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Crouch Rest target by run level. | Host |
| `Base Upgrades.MapPlayerCountUpgradeLevels` | Blank | `level:value` pairs; value `0`–`1` | Base Map Player Count target by run level. | Host |
| `Base Upgrades.DeathHeadBatteryUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Death Head Battery target by run level. | Host |
| `Base Upgrade Draw.Enabled` | `true` | `true`, `false` | Enables the shared Base Upgrade draw after leaving the shop. | Host |
| `Base Upgrade Draw.MaximumLevel` | `200` | `0`–`200` | Maximum target that positive truck draws can reach. Map Player Count remains limited to 1. | Host |
| `Base Upgrade Draw.CappedUpgradeWeightMultiplier` | `0.25` | `0`–`1` | Upgrade weights decrease as their combined configured and truck levels approach the draw maximum, reaching this multiplier at the maximum. `0` excludes capped upgrades; `1` disables the decrease. All Upgrades is unaffected. | Host |
| `Base Upgrade Draw.WeightFalloffExponent` | `2` | `0.1`–`10` | Controls the weight decrease curve. `1` decreases evenly; larger values preserve more weight until near the maximum. | Host |
| `Base Upgrade Draw.ChangeAmountWeights` | `-1:10,0:15,1:60,2:15` | `change amount:weight` pairs; amount `-200`–`200`, weight `0`–`1000` | Relative weights for the possible change amounts. | Host |
| `Base Upgrade Draw Weights.Health` | `20` | `0`–`1000` | Relative Health selection weight. | Host |
| `Base Upgrade Draw Weights.Stamina` | `40` | `0`–`1000` | Relative Stamina selection weight. | Host |
| `Base Upgrade Draw Weights.ExtraJump` | `10` | `0`–`1000` | Relative Extra Jump selection weight. | Host |
| `Base Upgrade Draw Weights.Speed` | `30` | `0`–`1000` | Relative Speed selection weight. | Host |
| `Base Upgrade Draw Weights.Strength` | `20` | `0`–`1000` | Relative Strength selection weight. | Host |
| `Base Upgrade Draw Weights.Range` | `30` | `0`–`1000` | Relative Range selection weight. | Host |
| `Base Upgrade Draw Weights.Launch` | `10` | `0`–`1000` | Relative Launch selection weight. | Host |
| `Base Upgrade Draw Weights.TumbleClimb` | `10` | `0`–`1000` | Relative Tumble Climb selection weight. | Host |
| `Base Upgrade Draw Weights.TumbleWings` | `10` | `0`–`1000` | Relative Tumble Wings selection weight. | Host |
| `Base Upgrade Draw Weights.CrouchRest` | `30` | `0`–`1000` | Relative Crouch Rest selection weight. | Host |
| `Base Upgrade Draw Weights.MapPlayerCount` | `10` | `0`–`1000` | Relative Map Player Count selection weight. | Host |
| `Base Upgrade Draw Weights.DeathHeadBattery` | `10` | `0`–`1000` | Relative Death Head Battery selection weight. | Host |
| `Base Upgrade Draw Weights.AllUpgrades` | `1` | `0`–`1000` | Relative `ALL UPGRADES` selection weight for positive results. | Host |

Default Weights reflect vanilla maximum shop counts: Stamina 40; Speed, Range, and Crouch Rest 30; Health and Strength 20; other supported upgrades 10; ALL UPGRADES 1. The configured Weight is used directly without an additional shop multiplier. Weights decrease toward CappedUpgradeWeightMultiplier as upgrades approach the draw maximum. Existing saved weights are retained; reset individual entries to their defaults to adopt these values. Other mods' shop changes and ShopUpgradeItemCount do not alter the weights.

#### Role selection

Every standard role has an `Enabled` switch and a relative `Weight`. A standard role with `Enabled = false` or `Weight = 0` is excluded from random assignment. `???1` and `???2` each have an `Enabled` switch, but their selection weights are fixed.

| Role | Enabled key and default | Weight key and default | Allowed values | Control |
|---|---|---|---|---|
| Tank | `Tank.Enabled = true` | `Tank.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Runner | `Runner.Enabled = true` | `Runner.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Jumper | `Jumper.Enabled = true` | `Jumper.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Lifter | `Lifter.Enabled = true` | `Lifter.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Launcher | `Launcher.Enabled = true` | `Launcher.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Climber | `Climber.Enabled = true` | `Climber.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Flyer | `Flyer.Enabled = true` | `Flyer.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Tracker | `Tracker.Enabled = true` | `Tracker.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Ghost | `Ghost.Enabled = true` | `Ghost.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Bomber | `Bomber.Enabled = true` | `Bomber.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Medic | `Medic.Enabled = true` | `Medic.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Phoenix | `Phoenix.Enabled = true` | `Phoenix.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Jobless | `Jobless.Enabled = true` | `Jobless.Weight = 20` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Rescuer | `Rescuer.Enabled = true` | `Rescuer.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Vampire | `Vampire.Enabled = true` | `Vampire.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| King | `King.Enabled = true` | `King.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Tuna | `Tuna.Enabled = true` | `Tuna.Weight = 50` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Musician | `Musician.Enabled = true` | `Musician.Weight = 60` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Mage | `Mage.Enabled = true` | `Mage.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Gambler | `Gambler.Enabled = true` | `Gambler.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Hunter | `Hunter.Enabled = true` | `Hunter.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Stinker | `Stinker.Enabled = true` | `Stinker.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Engineer | `Engineer.Enabled = true` | `Engineer.Weight = 70` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Trickster | `Trickster.Enabled = true` | `Trickster.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Mechanic | `Mechanic.Enabled = true` | `Mechanic.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Electrician | `Electrician.Enabled = true` | `Electrician.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Warden | `Warden.Enabled = true` | `Warden.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Ninja | `Ninja.Enabled = true` | `Ninja.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Executioner | `Executioner.Enabled = true` | `Executioner.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Rider | `Rider.Enabled = true` | `Rider.Weight = 60` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Influencer | `Influencer.Enabled = true` | `Influencer.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Werewolf | `Werewolf.Enabled = true` | `Werewolf.Weight = 40` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Berserker | `Berserker.Enabled = true` | `Berserker.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Bodyguard | `Bodyguard.Enabled = true` | `Bodyguard.Weight = 80` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Rammer | `Rammer.Enabled = true` | `Rammer.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Diver | `Diver.Enabled = true` | `Diver.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Sniper | `Sniper.Enabled = true` | `Sniper.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Imitator | `Imitator.Enabled = true` | `Imitator.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Avenger | `Avenger.Enabled = true` | `Avenger.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| Brawler | `Brawler.Enabled = true` | `Brawler.Weight = 100` | Enabled: `true`, `false`; Weight: `0`–`1000` | Host |
| ???1 | `???1.Enabled = true` | Fixed | Enabled: `true`, `false` | Host |
| ???2 | `???2.Enabled = true` | Fixed | Enabled: `true`, `false` | Host |

#### Upgrade and special-role settings

| Key | Default | Range / values | Effect | Control |
|---|---:|---|---|---|
| `Tank.HealthUpgradeLevels` | `21` | `0`–`200` | Health levels granted to Tank. | Host |
| `Tracker.HealthUpgradeLevels` | `3` | `0`–`200` | Health target while Tracker is assigned. | Host |
| `Runner.SpeedUpgradeLevels` | `6` | `0`–`200` | Speed levels granted to Runner. | Host |
| `Runner.StaminaUpgradeLevels` | `46` | `0`–`200` | Stamina levels granted to Runner. | Host |
| `Jumper.ExtraJumpUpgradeLevels` | `10` | `0`–`200` | Extra Jump levels granted to Jumper. | Host |
| `Lifter.StrengthUpgradeLevels` | `25` | `0`–`200` | Strength levels granted to Lifter. | Host |
| `Launcher.LaunchUpgradeLevels` | `10` | `0`–`200` | Launch levels granted to Launcher. | Host |
| `Climber.ClimbUpgradeLevels` | `50` | `0`–`200` | Tumble Climb levels granted to Climber. | Host |
| `Climber.RangeUpgradeLevels` | `20` | `0`–`200` | Range levels granted to Climber. | Host |
| `Flyer.WingsUpgradeLevels` | `10` | `0`–`200` | Tumble Wings levels granted to Flyer. | Host |
| `Ghost.DeathHeadBatteryUpgradeLevels` | `50` | `0`–`200` | Death Head Battery levels granted to Ghost. | Host |
| `Bomber.DistancePerGrenade` | `8` | `1`–`100` | Travel distance in meters required for each grenade placement. | Host |
| `Bomber.MaximumActiveGrenades` | `30` | `1`–`30` | Maximum generated grenades retained per Bomber. Placing another removes the oldest one. | Host |
| `Bomber.AllowTruckSpawns` | `false` | `true`, `false` | Allows grenade placement inside the truck. | Host |
| `Bomber.ExplosiveGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla explosive grenades in the random selection. | Host |
| `Bomber.StunGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla stun grenades in the random selection. | Host |
| `Bomber.ShockwaveGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla shockwave grenades in the random selection. | Host |
| `Bomber.DuctTapedGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla duct-taped grenades in the random selection. | Host |
| `Medic.HealAmount` | `5` | `1`–`100` | Health restored to each nearby teammate per tick. | Host |
| `Medic.HealIntervalSeconds` | `2` | `0.1`–`30` | Seconds between Medic healing ticks. | Host |
| `Medic.HealRadius` | `5` | `1`–`30` | Maximum healing distance in meters. | Host |
| `Medic.TotalHealingLimit` | `150` | `1`–`10000` | Maximum total health restored by each Medic per stage. Only health actually missing from a target consumes the limit. | Host |
| `Phoenix.ReviveDelaySeconds` | `2` | `2`–`10` | Seconds before Phoenix revival. | Host |
| `Phoenix.FailureGraceSeconds` | `5` | `1`–`15` | Maximum seconds to hold a failed-stage transition while revival initializes. | Host |
| `Phoenix.RevivalHealth` | `25` | `1`–`1000` | Health after Phoenix revival, capped at the player's maximum health. | Host |
| `Jobless.Damage` | `1` | `1`–`100` | Damage per tick outside the truck. | Host |
| `Jobless.DamageIntervalSeconds` | `0.1` | `0.05`–`10` | Seconds between damage ticks outside the truck. | Host |
| `Rescuer.ReviveDelaySeconds` | `2` | `0`–`10` | Seconds a target must remain dead before rescue. | Host |
| `Rescuer.Radius` | `3` | `1`–`50` | Maximum rescue distance in meters. | Host |
| `Rescuer.MaximumRevives` | `2` | `1`–`10` | Maximum revivals for each Rescuer per stage. | Host |
| `Rescuer.RevivalHealth` | `25` | `1`–`1000` | Health after a Rescuer revival, capped at the target's maximum health. | Host |
| `Vampire.Tier1HealAmount` | `5` | `1`–`100` | Health restored by a nearby Danger Level 1 enemy death. | Host |
| `Vampire.Tier2HealAmount` | `10` | `1`–`100` | Health restored by a nearby Danger Level 2 enemy death. | Host |
| `Vampire.Tier3HealAmount` | `50` | `1`–`100` | Health restored by a nearby Danger Level 3 enemy death. | Host |
| `Vampire.Radius` | `10` | `1`–`50` | Maximum distance in meters from the dying enemy. | Host |
| `Tuna.StationaryDelaySeconds` | `3` | `0.1`–`30` | Seconds without movement before damage begins. | Host |
| `Tuna.Damage` | `1` | `1`–`100` | Damage per stationary tick. | Host |
| `Tuna.DamageIntervalSeconds` | `0.1` | `0.05`–`10` | Seconds between stationary damage ticks. | Host |
| `Musician.HealAmount` | `5` | `1`–`100` | Health restored to the Musician and each living player in range per instrument note. | Host |
| `Musician.HealRadius` | `10` | `1`–`50` | Maximum healing distance in meters from the Musician. | Host |
| `Mage.CastIntervalSeconds` | `3` | `0.1`–`30` | Minimum seconds between spell activations. | Host |
| `Mage.StarExpression` | `Angry` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `star`. | Host |
| `Mage.RollExpression` | `Sad` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `roll`. | Host |
| `Mage.GravityExpression` | `Suspicious` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `gravity`. | Host |
| `Mage.VoidExpression` | `EyesClosed` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `void`. | Host |
| `Mage.LaserExpression` | `Scared` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `laser`. | Host |
| `Mage.AutoRecoveryEnabled` | `true` | `true`, `false` | Enables automatic Mage health recovery after avoiding damage. | Host |
| `Mage.AutoRecoveryDelaySeconds` | `10` | `0`–`300` | Seconds without taking damage before automatic recovery starts. | Host |
| `Mage.AutoRecoveryIntervalSeconds` | `2` | `0.1`–`60` | Seconds between automatic recovery ticks. | Host |
| `Mage.AutoRecoveryAmount` | `1` | `0`–`100` | Health restored by each automatic recovery tick. | Host |
| `Mage.AutoRecoveryTotalHealingLimit` | `120` | `1`–`10000` | Maximum total automatic recovery per player per stage. Other healing is not counted. Death, revival, and role changes do not reset the amount already used. | Host |
| `Mage.StarHealthCost` | `10` | `0`–`100` | Health consumed after a successful `star` cast. | Host |
| `Mage.GravityHealthCost` | `10` | `0`–`100` | Health consumed after a successful `gravity` cast. | Host |
| `Mage.RollHealthCost` | `15` | `0`–`100` | Health consumed after a successful `roll` cast. | Host |
| `Mage.VoidHealthCost` | `30` | `0`–`100` | Health consumed after a successful `void` cast. | Host |
| `Mage.LaserHealthCost` | `50` | `0`–`100` | Health consumed after a successful `laser` cast. | Host |
| `Gambler.WinChancePercent` | `50` | `0`–`100` | Chance that a wager uses the win multiplier instead of destroying the valuable. | Host |
| `Gambler.WinValueMultiplier` | `2` | `0`–`10` | Valuable multiplier applied to a winning wager. | Host |
| `Gambler.GambitGreenHealAmount` | `50` | `25`–`1000` | Total HP restored by Gambit's green result for a Gambler. | Host |
| `Gambler.GambitRedDamage` | `100` | `50`–`1000` | Total damage dealt by Gambit's red result to a Gambler. | Host |
| `Gambler.GambitWhiteHealthUpgradeLevels` | `5` | `0`–`200` | Health upgrade levels added by Gambit's white result for the current stage only. | Host |
| `Hunter.WeaponBatteryConsumptionPercent` | `75` | `0`–`100` | Percentage of normal weapon battery consumption used by Hunter. | Host |
| `Hunter.DoubleOrbChancePercent` | `10` | `0`–`100` | Chance to double the normal orb count after Hunter defeats an enemy, checked only if the jackpot roll fails. | Host |
| `Hunter.JackpotOrbChancePercent` | `0.5` | `0`–`100` | Chance to use the jackpot target after Hunter defeats an enemy. Missing orbs are added, but a normal drop above the target is not reduced. | Host |
| `Hunter.JackpotOrbCount` | `10` | `1`–`30` | Target orb count used by Hunter's jackpot result. | Host |
| `Stinker.DistancePerCloud` | `2` | `1`–`100` | Travel distance in meters between recorded uranium-cloud trail points. | Host |
| `Stinker.MinimumSafetyDistance` | `2` | `1`–`30` | Minimum distance Stinker must be from the newest cloud position. Clouds appear one after another. | Host |
| `Stinker.AllowTruckSpawns` | `false` | `true`, `false` | Allows uranium-cloud trail points to be recorded and created inside the truck. | Host |
| `Trickster.ActiveSeconds` | `25` | `1`–`120` | Seconds before the active decoy is removed. | Host |
| `Trickster.DecoyExpression` | `Happy` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that places the decoy. | Host |
| `Trickster.CooldownSeconds` | `45` | `1`–`300` | Seconds after a decoy that attracted an enemy ends before another can be placed. | Host |
| `Trickster.NoTargetCooldownSeconds` | `10` | `0`–`300` | Seconds after a decoy that attracted no enemies ends before another can be placed. | Host |
| `Trickster.InvestigateRadius` | `40` | `1`–`100` | Enemy investigation radius around the decoy in meters. | Host |
| `Trickster.PulseIntervalSeconds` | `1` | `0.25`–`10` | Seconds between enemy investigation pulses. | Host |
| `Trickster.PlacementDistance` | `2` | `0.5`–`10` | Placement distance in front of Trickster in meters. | Host |
| `Mechanic.RepairPercentPerSecond` | `2` | `0`–`100` | Shared original-value percentage repaired per second while Mechanic directly holds damaged valuables. | Host |
| `Mechanic.MaximumRepairPercentPerStage` | `50` | `0`–`100` | Maximum cumulative original-value percentage repaired by each Mechanic per stage. | Host |
| `Electrician.ChargePercentPerSecond` | `10` | `0`–`100` | Shared battery percentage restored per second while Electrician directly holds inactive rechargeable items. | Host |
| `Electrician.MaximumChargePercentPerStage` | `100` | `0`–`1000` | Maximum cumulative battery percentage restored by each Electrician per stage. | Host |
| `Warden.AdditionalStunSeconds` | `3` | `0`–`30` | Seconds added to enemy stuns caused by Warden. | Host |
| `Ninja.VisionRecognitionMultiplier` | `2` | `1`–`10` | Multiplier applied to the time enemies need to visually recognize Ninja. | Host |
| `Executioner.StunnedDamageMultiplier` | `2` | `1`–`10` | Direct attack damage multiplier against enemies already stunned before the hit. | Host |
| `Rider.EnemyDamageMultiplier` | `3` | `1`–`10` | Vehicle impact damage multiplier against enemies while Rider drives. | Host |
| `Rider.PlayerKnockbackMultiplier` | `2` | `1`–`10` | Multiplier for existing vehicle Tumble knockback against players. | Host |
| `Influencer.PlayerRadius` | `20` | `1`–`100` | Radius in meters used to count nearby living teammates. | Host |
| `Influencer.PlayerCheckIntervalSeconds` | `0.5` | `0.1`–`10` | Seconds between nearby-player and dynamic-upgrade checks. | Host |
| `Influencer.NoiseRadiusMultiplier` | `2` | `1`–`10` | Multiplier for footstep, landing, VC, and chat TTS enemy investigation radii. | Host |
| `Influencer.TTSInvestigateRadius` | `20` | `1`–`100` | Enemy investigation radius of periodic TTS. | Host |
| `Influencer.MinimumTTSIntervalSeconds` | `20` | `1`–`300` | Minimum randomly selected periodic TTS interval. | Host |
| `Influencer.MaximumTTSIntervalSeconds` | `40` | `1`–`300` | Maximum randomly selected periodic TTS interval. Values are safely swapped when configured below the minimum. | Host |
| `Influencer.TTSQuietPeriodSeconds` | `1.5` | `0`–`10` | Quiet time required after any TTS, including Stage Flux, before periodic TTS fires. | Host |
| `Influencer.<Upgrade>UpgradeScaling` | Per upgrade | `count:level` pairs | Sets that upgrade's minimum target by nearby-player count without lowering its pre-role level. Example: Strength defaults to `1:1,2:4,3:9,4:13,5:20`. Available upgrades match Base Upgrades. Blank grants no level. | Host |
| `Werewolf.PlayerDamageMultiplier` | `2` | `1`–`10` | Multiplier for identifiable damage Werewolf deals to another player. | Host |
| `Berserker.<Upgrade>UpgradeScaling` | Per upgrade | `HP%:level` pairs | Sets that upgrade's minimum target while remaining HP is at or below the percentage, without lowering its pre-role level. Example: Strength defaults to `80:1,60:4,40:9,20:13,10:20`. Available upgrades match Base Upgrades except Health. Blank grants no level. | Host |
| `Bodyguard.HealthUpgradeLevels` | `11` | `0`–`200` | Health target while Bodyguard is assigned. | Host |
| `Bodyguard.ProtectionRadius` | `15` | `1`–`100` | Maximum teammate-protection distance in meters. | Host |
| `Bodyguard.DamageSharePercent` | `50` | `0`–`100` | Percentage of enemy damage transferred without reducing Bodyguard below 1 HP. | Host |
| `Rammer.TumbleAttackDamage` | `100` | `0`–`100000` | Enemy damage dealt by Rammer's Tumble Attack. | Host |
| `Rammer.SelfDamage` | `15` | `0`–`100000` | Damage Rammer takes after its Tumble Attack hits an enemy. | Host |
| `Diver.UnderfloorDurationSeconds` | `10` | `1`–`120` | Maximum time below a floor before Diver dies. | Host |
| `Diver.MovementForce` | `8` | `1`–`30` | Free-movement force while Diver is below a floor. | Host |
| `Sniper.ReferenceDistance` | `8` | `1`–`50` | Distance in meters where enemy damage is unchanged. | Host |
| `Sniper.MinimumDamageMultiplier` | `0.5` | `0`–`1` | Enemy damage multiplier at point-blank range. Positive hits still deal at least 1 damage. | Host |
| `Sniper.MaximumDamageMultiplier` | `2` | `1`–`10` | Maximum enemy damage multiplier at long range. | Host |
| `Sniper.MaximumMultiplierDistance` | `24` | `1`–`100` | Distance in meters where the maximum multiplier is reached. Values at or below Reference Distance use a point just beyond it. | Host |
| `Avenger.DamageMultiplier` | `1.5` | `1`–`10` | Enemy damage multiplier after another player dies. | Host |
| `Avenger.DurationSeconds` | `20` | `1`–`120` | Duration of the enemy damage bonus. Another death refreshes this duration. | Host |
| `Avenger.TriggerRadius` | `30` | `1`–`100` | Maximum distance in meters from a dying player that activates Avenger. | Host |
| `Brawler.MeleeDamageMultiplier` | `1.25` | `0`–`10` | Damage multiplier for identifiable melee weapon attacks against enemies and players. | Host |
| `Brawler.RangedDamageMultiplier` | `0.75` | `0`–`10` | Damage multiplier for identifiable gun, staff-projectile, and laser attacks against enemies and players. | Host |

The default base is Health 1 from run level 1 and 0 for every other managed upgrade. A static role-specific upgrade is an absolute target, not a bonus added to the base. When that role ends, RoleShuffle restores the configured base target; upgrades not overridden by the role stay at their base targets. Influencer and Berserker instead treat each dynamic target as a minimum and never lower the level captured when the role was assigned. Static-role levels change only with assignment or session state, while Influencer and Berserker re-evaluate their configured conditions at the Influencer check interval. `Tracker` always targets Map Player Count 1, while its Health target is configurable. `Rammer` always overrides Launch, Tumble Climb, and Tumble Wings to 0 during the stage. Throw is not managed. `King` has no effect-strength setting.

Influencer and Berserker scaling accepts comma- or semicolon-separated `condition:level` pairs. Influencer applies the last target whose player-count condition has been reached. Berserker applies the target belonging to the lowest configured HP threshold currently reached. Default expressions omit pairs whose target level would be `0`; add a `condition:0` pair manually when an explicit zero override is needed. Levels are limited to `0`–`100`, except Map Player Count which is limited to `0`–`1`. Entries in the wrong format are ignored.

### Notifications and HUD

- Role emblems appear beside roles in `CURRENT ROLES` and `ROLE GUIDE`, and optionally in the HUD. The area outside each hexagonal emblem is transparent. Unrevealed secret roles use a shared question-mark emblem until revealed. Emblems are visible to players who have the mod installed.
- At stage start, each player announces the assigned English role name through vanilla chat and TTS.
- RoleShuffle's forced notification TTS does not attract enemies. Ordinary microphone input and other world sounds keep their vanilla behavior unless suppressed by Ninja.
- When Stage Flux is present, RoleShuffle uses the separate Stage Flux announcement delay to avoid overlapping stage-start messages.
- RoleShuffle waits until Stage Flux TTS has finished and remains quiet for 1.5 seconds before announcing roles.
- Role assignments, role-effect notices, and automatic role-query responses are spoken one at a time and do not overlap Stage Flux announcements.
- Influencer TTS also waits for other announcements, but intentionally remains audible to enemies as part of the role ability.
- Installed participants see a one-column `ROLES` HUD with names only at the bottom left by default. Their own role stays pinned while other players rotate every 5 seconds. `HUD.RoleDisplay` can enable role icons. Icon size is adjustable with `HUD.IconSize`; larger icons use wider row spacing and fewer players per page. Icons at the default size show up to four players per page, including the pinned player. Names only show up to eight; the actual count adjusts to the font height so names remain visible.
- The Base Upgrade draw animation is shown to the host and participants who have RoleShuffle installed.
- A `ROLES` button at the top-right of the Escape and lobby menus opens the Roles page. The Escape menu opens on `CURRENT ROLES`; the lobby menu opens on `ROLE GUIDE`, with `CURRENT ROLES` disabled. The left column switches between available views and `BASE UPGRADES`. Click a player in `CURRENT ROLES` to show or hide that role's description. `BASE UPGRADES` shows the current shared target, configured target, and accumulated truck-draw bonus for every supported upgrade. Installed participants see the host's current values.
- `ROLE GUIDE` shows enabled role descriptions in English or Japanese; disabled roles are hidden. Use the language toggle inside the guide to switch immediately; English is selected whenever the Roles page is opened. Japanese descriptions use the Checkpoint Revenge font. In multiplayer, installed participants see descriptions and role visibility based on the host's settings. Until those settings are available, descriptions omit unconfirmed numeric values. Single-player uses the player's own settings.

### Compatibility

Stage Flux is optional and is not required to install RoleShuffle.

- Second Chance takes priority over Phoenix, Rescuer, and Bodyguard revival effects. Revival effects that are not needed remain available.
- If Second Chance or Phoenix prevents a failed stage transition, current roles remain active.
- RoleShuffle effects do not interfere with player movement or grounded checks.
- During Value Surge or Value Crash, Mechanic repairs only the value that was actually lost.
- RoleShuffle announcements and Influencer TTS wait for Stage Flux announcements instead of overlapping them.
- Enemy Purge defeats do not activate Hunter rewards.
- Dangerous Valuables respects valuables protected by Engineer.

Elite Enemy Variants is optional and is not required to install RoleShuffle.

- Living `Ninja` players are excluded from Elite Enemy Variants attacks that specifically require an unseen player target. Dead Ninjas and all other players remain eligible.
- When `Compatibility.EnhancedEnemyRewardsEnabled` is `true`, Enhanced enemies provide stronger Vampire healing and better Hunter bonus orbs, up to the Tier 3 reward. This does not change the normal number of orbs.
- Gambler's roulette effect stays attached to the correct player even when an Enhanced Gambit moves after capture.
- RoleShuffle does not add Elite Enemy Variants enemy counts, state, or defeat notices to its HUD or menu.

### Gameplay notes

- `Bomber` creates live vanilla hazards. Explosions and physics effects may injure players or damage valuables.
- `Mage` fires live vanilla attacks selected by case-insensitive chat keywords after trimming surrounding whitespace. Projectiles and beams may injure players or damage valuables.
- `Jobless` and `Tuna` continuously deal real damage under their stated conditions and can kill their owner. The damage is not automatically restored.
- `Medic` never heals itself.
- `Phoenix` revives itself once per stage. `Rescuer` revives other nearby players up to the configured limit. Each role uses its own configurable revival HP, with a default of 25.
- Role-specific upgrade overrides return to their configured base levels at stage end. Other managed upgrades stay at their base levels, and Throw is untouched.
- Setting every role to disabled or weight `0` leaves no eligible random role to assign.

## 日本語

### 概要

RoleShuffleは、ステージ開始時に各プレイヤーへランダムな役職を1つ割り当て、ステージ終了時に役職を解除します。各役職はバニラのアップグレードや効果を使った固有のゲームプレイテーマに特化しています。設定可能な基礎アップグレードは現在のランレベルに応じて変化させることができ、ステージ外でも維持します。役職が特化するアップグレードは、ステージ中だけ基礎値ではなく役職の設定値へ置き換え、それ以外は基礎値を維持します。

ゲームプレイ効果はホストだけの導入で利用でき、最大30人のセッションをサポートします。MODを導入していない参加者にも、役職、アップグレード、効果、バニラのチャット／TTS通知が適用されます。RoleShuffleを導入している参加者は、すべての役職を確認できるHUDも利用できます。

デフォルトでは、役職による一時強化を中心にするため、ショップのアップグレードアイテム出現数は`0`です。

### お問い合わせ

ご質問、不具合報告、ご意見は[お問い合わせフォーム](https://forms.gle/nYKsoqV3KKtX94t57)からお送りください。

### 必須MOD

- BepInExPack 5.4.2305以降の互換バージョン
- REPOConfig 1.2.6以降の互換バージョン
- MenuLib 2.5.2以降の互換バージョン

### 導入方法

対応するMODマネージャーでインストールするか、`RoleShuffle.dll`をプロファイルの`BepInEx/plugins`へ配置してください。ゲームプレイ効果はホストだけの導入で利用できます。

### マルチプレイ

- 役職の抽選とゲームプレイ設定はホストが管理します。
- MOD未導入の参加者にも対応しており、RoleShuffleの導入は不要です。
- MOD導入済みの参加者には全員の役職を確認できるHUDと、Escメニュー内のスクロール可能な`Roles`ページが表示されます。HUDの配置は各プレイヤーが個別に変更できます。
- シングルプレイでも同じ役職システムを使用しますが、参加者が1人だけのときは`Tracker`、`Ghost`、`Medic`、`Jobless`、`Rescuer`、`Influencer`、`Werewolf`、`Bodyguard`、`Imitator`、`Avenger`がランダム抽選から除外されます。

### 役職確認コマンド

- ゲーム内チャットで`/roles`を入力すると、自分に割り当てられた役職を確認できます。
- ホストがRoleShuffleを導入していれば、MOD未導入の参加者も使用できます。
- ステージ外では`YourRole:Unavailable`と応答します。

### 役職の抽選

- プレイ可能なステージの開始時、各プレイヤーへ役職を1つ割り当てます。
- 有効な役職を相対的な`Weight`に基づいて抽選します。
- `General.UniqueRoles = true`の場合、抽選可能な役職を一巡するまで重複を避けます。
- `King`は1ステージにつき最大1人です。
- デフォルトでは、Showcase役（`Bomber`、`Stinker`、`Mage`、`Gambler`、`Trickster`、`King`、`Rider`、`Influencer`、`Diver`）とSupport役（`Medic`、`Rescuer`、`Mechanic`、`Electrician`、`Warden`、`Bodyguard`）を、参加人数に応じて設定された最低人数まで優先的に割り当てます。
- デフォルトでは、24人以上でShowcaseとSupportをそれぞれ最低4人保証します。Danger上限は8人で2人、16人で3人へ増え、Hardship上限は12人で2人へ増えます。InfluencerはShowcase役であり、Danger役として扱いません。4人以下では両リスクグループを合わせて最大1人です。
- 各プレイヤーが直近5回に割り当てられた役職は、そのプレイヤーの次回抽選時に通常の半分のWeightで扱います。他のプレイヤーの履歴は影響しません。
- 同じプレイヤーへ前ステージと同じ役職を通常は連続で割り当てません。`Jobless`または`Tuna`の後、2ステージはそのプレイヤーを両方から除外します。役職未割り当てを防ぐ必要がある場合だけ制限を緩和します。
- `Tank`、`Runner`、`Jumper`、`Lifter`、`Launcher`、`Climber`、`Flyer`、`Tracker`、`Ghost`は、トラック抽選分を含む基礎アップグレード目標値のいずれかが、対応する役職の設定値以上の場合、ランダム抽選から除外されます。テストコマンドによる強制指定には影響しません。
- `Influencer`は、現在の参加人数で到達可能なアップグレード目標値が現在のBase Upgradeを1項目も上回らない場合、ランダム抽選から除外されます。強制割り当てには影響しません。
- `Tracker`、`Ghost`、`Medic`、`Jobless`、`Rescuer`、`Influencer`、`Werewolf`、`Bodyguard`、`Imitator`、`Avenger`はシングルプレイまたは1人のセッションでは抽選されません。
- `Imitator`は、コピー可能な役職を持つ他の参加者が確保された場合だけ抽選されます。
- デフォルトでは、能力を使える対象がない状況依存役を抽選から除外します。必要なオブジェクトがない`Musician`、`Engineer`、`Electrician`、`Rider`と、近接武器も銃もない場合の`Sniper`、近接武器がない場合の`Brawler`が対象です。両ロールの抽選判定では、武器として使える貴重品も武器に含めません。Sniperの抽選判定では杖も対象外です。
- ステージ中は参加者一覧を定期的に確認し、10回連続で不在だったプレイヤーを一覧から外します。戻ってきたプレイヤーには以前の役職を戻し、新しく参加したプレイヤーには役職、アップグレード、通知、HUD表示を適用します。
- 役職とステージ中の効果は、ステージが実際に終了した場合だけ解除します。復活効果によって失敗時のステージ移行が中断された場合は維持します。

### 役職一覧

| 役職 | 機能とデフォルト効果 | 主な制限・危険性 | 調整可能な値 |
|---|---|---|---|
| Tank | 最大HPが増加し、Healthがレベル21になります。 | バニラのHealthアップグレードを使用し、別の能動的な能力はありません。 | Health目標値`0`～`100` |
| Runner | 移動速度とスタミナが増加し、より速く長く走れます。Speedはレベル6、Staminaはレベル46です。 | 2種類のバニラアップグレードを使用し、別の能動的な能力はありません。 | SpeedとStaminaの目標値`0`～`100` |
| Jumper | Extra Jumpがレベル10になり、着地するまでに最大10回の追加ジャンプを使えます。 | バニラのExtra Jumpアップグレードを使用し、別の能動的な能力はありません。 | Extra Jump目標値`0`～`100` |
| Lifter | 掴む力が増加し、重い物を扱いやすくなります。Strengthはレベル25です。 | バニラのStrengthアップグレードを使用し、別の能動的な能力はありません。 | Strength目標値`0`～`100` |
| Launcher | Tumble開始時にプレイヤーをより遠く前方へ飛ばします。Launchはレベル10です。 | バニラのLaunchアップグレードを使用し、別の能動的な能力はありません。 | Launch目標値`0`～`100` |
| Climber | Tumble中の登りやすさが増し、より遠くの物を掴めます。Tumble Climbはレベル50、Rangeはレベル20です。 | 2種類のバニラアップグレードを使用し、別の能動的な能力はありません。 | Tumble ClimbとRangeの目標値`0`～`100` |
| Flyer | Tumble Wingsの効果時間が延び、空中をより長く移動できます。Tumble Wingsはレベル10です。 | バニラのTumble Wingsアップグレードを使用し、別の能動的な能力はありません。 | Tumble Wings目標値`0`～`100` |
| Tracker | 最大HPが増加し、マップ系アイテムで別のプレイヤーを追跡できるようになります。Healthはレベル3、Map Player Countはレベル1です。 | Map Player Countは`1`固定です。参加者が1人だけの場合はランダム抽選されません。 | Health目標値`0`～`100` |
| Ghost | Death Head Batteryがレベル50になります。 | 参加者が1人だけの場合はランダム抽選されません。 | Battery目標値 |
| Bomber | 8 m移動するごとに起動済みグレネードをランダム設置します。Bomber 1人につき最大30個まで残り、上限では最も古いものを削除します。 | グレネードはプレイヤーやValuableにも危険です。生成されたグレネードを持てるのはBomberだけです。トラック内設置が無効な場合、トラック内の移動距離は加算されません。 | 距離、上限、トラック内設置、グレネード種類 |
| Medic | 5 m以内の仲間を2秒ごとに5 HP回復し、1ステージにつき合計150 HPまで回復します。 | Medic自身は回復しません。範囲内で生存している仲間だけが対象です。そのMedicの上限を使い切ると回復を停止します。参加者が1人だけの場合はランダム抽選されません。 | 回復量、間隔、範囲、合計上限 |
| Phoenix | 死亡すると、1ステージに1回だけデフォルト25 HPで自動復活します。 | 1ステージにつき1回です。復活後HPはプレイヤーの最大HPを超えません。 | 復活後HP、復活遅延、失敗時猶予 |
| Jobless | トラック外にいる間、0.1秒ごとに1ダメージを受けます。 | 死亡する可能性があります。参加者が1人だけの場合はランダム抽選されません。 | ダメージ、間隔 |
| Rescuer | 生存中に、死亡した仲間のDeath Headから3 m以内へ近づくと、最も近い対象をデフォルト25 HPで復活させます。 | 1ステージにつき最大2回です。復活後HPは対象の最大HPを超えません。参加者が1人だけの場合はランダム抽選されません。 | 復活後HP、遅延、範囲、最大復活回数 |
| Vampire | 10 m以内で敵が死亡すると、Tier 1は5、Tier 2は10、Tier 3は50回復します。 | 敵のバニラDanger Levelを使用します。Elite Enemy Variants導入時にEnhanced報酬補正が有効なら、Enhanced個体を最大Tier 3まで1段階上として扱います。Vampireが生存し、死亡した敵の近くにいる必要があります。 | Tier別回復量、範囲 |
| King | ステージ中、バニラのCrownを受け取ります。 | Kingは最大1人です。役職終了時に以前のCrown所有者へ戻します。 | 抽選設定のみ |
| Tuna | ステージ開始時の5秒間の猶予後、3秒間停止すると、移動するまで0.1秒ごとに1ダメージを受けます。 | 死亡する可能性があります。停止時間の計測は最初の猶予後に始まります。動くと即座にリセットし、失った体力は回復しません。 | 停止時間、ダメージ、間隔 |
| Musician | Musicianが楽器で音を鳴らすたびに、本人を含む10 m以内の生存プレイヤーを5 HP回復します。 | バニラの楽器系貴重品が必要です。デフォルトでは存在しない場合に抽選されません。 | 回復量、範囲 |
| Mage | チャットで`star`、`gravity`、`roll`、`void`、`laser`を入力するか、設定した表情を選択・解除し、順に10、10、15、30、50 HPを消費してバニラ攻撃を発動します。10秒間ダメージを受けなければ、2秒ごとに1 HP回復します。自動回復は1ステージの累計120 HPまでです。保持した杖で発動する魔法の効果時間は1.3倍になります。 | 対応する表情から標準へ戻す操作でも発動します。全魔法で3秒のクールダウンを共有し、消費で死亡する場合は発動しません。攻撃はプレイヤーやValuableにも危険です。効果時間の延長はチャット・表情での魔法や、星杖の瞬間的な攻撃には適用しません。 | 魔法ごとの表情、発射クールダウン、魔法ごとのHP消費量、自動回復の有効化、待機時間、間隔、回復量、ステージごとの回復上限 |
| Gambler | 持ったValuableは50%の確率で現在価格が2倍になり、失敗すると破壊されます。Gambitは緑で50 HP回復、赤で100ダメージ、黒で死亡、白で全回復してステージ中のHealthを5レベル増やします。 | 賭けは一度に1回だけ使用でき、納品後に復帰します。傷ついたValuableは低下後の価格を基準にします。 | 勝率、勝利倍率、Gambitの緑回復量、赤ダメージ、白の一時Healthレベル |
| Hunter | 保持または装備中の武器のバッテリー消費量が通常の75%になります。Hunterが倒した敵は10%の確率でオーブが2倍、0.5%の確率で10個になります。 | Hunter本人が保持または装備した武器と、Hunterが倒したと確認できる敵だけが対象です。Elite Enemy Variants導入時にEnhanced報酬補正が有効なら、Enhanced個体によってHunterが追加するオーブの品質Tierだけが上がり、通常オーブ数は変わりません。 | バッテリー消費率、両方のオーブ確率、特賞個数 |
| Stinker | 2 m移動するごとに、ダメージを与えるウラン雲を残します。雲は、Stinkerが最新の発生位置から2 m以上離れた後に1つずつ発生します。 | ウラン雲はほかのプレイヤーへダメージを与えます。Stinkerも雲へ戻るとダメージを受ける可能性があります。デフォルトではトラック内に発生しません。 | 距離、安全距離、トラック内発生 |
| Engineer | 対応している効果付きValuableを保持している間、その効果が発動しないようにします。 | Camera、Propane Tank、Snowmobile、Flashlight、Clown Doll、Love Potionは対象外です。デフォルトでは対応Valuableが存在しない場合に抽選されません。 | 抽選設定のみ |
| Trickster | チャットで`decoy`と入力するか、設定した表情を選択・解除すると、半径40 m以内の敵を25秒間繰り返し引きつける固定式Scream Dollを設置します。 | デコイが有効な間は新しいデコイを設置できません。クールダウンは終了後から始まり、敵を引きつけた場合は45秒、一体も引きつけなかった場合は10秒です。デコイは掴む、破壊する、納品することができません。 | デコイ用の表情、有効時間、通常時と空振り時のクールダウン、範囲、誘導間隔、設置距離 |
| Mechanic | 破損したValuableを直接掴んでいる間、元の売却価格の合計2%を毎秒回復し、1ステージにつき合計50ポイントまで回復します。 | 実際に回復した価格だけがステージ上限へ加算されます。修復済みのValuableとValuable以外は対象外です。複数を同時に掴んだ場合、回復速度とステージ上限を共有します。売却価格を回復する効果で、破損した外見は残る場合があります。 | 回復速度、ステージ合計上限 |
| Electrician | 使用していない充電可能なアイテムを直接掴んでいる間、合計10%のバッテリーを毎秒回復し、1ステージにつき合計100ポイントまで回復します。 | 使用中、満充電、充電不可能なアイテムは対象外です。デフォルトでは充電可能アイテムが存在しない場合に抽選されません。 | 充電速度、ステージ合計上限 |
| Warden | Wardenの武器、グレネード、保持している攻撃用オブジェクトによる敵のスタン時間を3秒延長します。 | 元の攻撃がその敵をスタンさせられる場合だけ適用します。 | 追加スタン時間 |
| Ninja | 足音、着地音、VC、チャットTTSで敵の調査行動を発生させず、視覚で認識されるまでの時間が2倍になります。 | 接近や、武器、Valuable、衝突、爆発、危険物などの物音では発見される可能性があります。 | 視認時間倍率 |
| Executioner | 攻撃前からスタンしている敵への直接攻撃ダメージが2倍になります。 | 最初にスタンさせる攻撃には倍率が適用されません。物理衝突だけで発生するダメージは変更しません。 | スタン中ダメージ倍率 |
| Rider | バニラ車両の運転中、敵への衝突ダメージを3倍にし、プレイヤーへ元から発生するTumbleノックバックを2倍にします。 | プレイヤーへのダメージは増えません。デフォルトではバニラ車両が存在しない場合に抽選されません。 | 敵ダメージ倍率、プレイヤーノックバック倍率 |
| Influencer | 半径20 m以内の生存中の仲間が多いほど強化され、定期的に英語TTSを発言します。 | 足音、着地音、VC、チャットTTSが敵へ届く範囲は2倍です。 | 範囲、確認間隔、アップグレード別の人数連動設定、物音倍率、TTS間隔と範囲 |
| Werewolf | Werewolfが攻撃者と特定できる、他のプレイヤーへのダメージを2倍にします。 | 自傷、環境ダメージ、攻撃者を特定できないダメージは変化しません。1人のセッションでは抽選されません。 | プレイヤーダメージ倍率 |
| Berserker | 残りHPが少ないほど強化されます。 | HPを回復すると強化も戻ります。 | アップグレード別のHP割合連動設定 |
| Bodyguard | Healthがレベル11になり、15 m以内の仲間が敵から受けたダメージの50%を肩代わりします。 | 肩代わりによってBodyguardのHPが1未満になることはありません。 | Health目標、範囲、肩代わり率 |
| Rammer | Tumble Attackで敵へ100ダメージを与え、命中後に自身が15ダメージを受けます。 | ステージ中はBase Upgradeにかかわらず、Launch、Tumble Climb、Tumble Wingsがレベル0に固定されます。 | Tumble Attack・自傷ダメージ |
| Diver | 固定された床で下を向いたままタンブルすると床を抜け、最大10秒間、床下を移動できます。 | 時間内に床を抜けて戻れないと死亡します。床上へ戻ると直前の潜航時間の1.5倍のクールダウンが始まり、残り時間と再使用可能状態はTTSで通知されます。 | 床下制限時間、移動力 |
| Sniper | 敵へのダメージが距離に応じて連続変化し、密着時は0.5倍、8 mで1倍、24 mで最大2倍になります。 | Sniperが攻撃者と特定できる場合だけ適用します。車両衝突とTumble Attackは変化しません。 | 基準距離、最低・最高倍率、最高倍率到達距離 |
| Imitator | 最初はBase Upgradesだけが適用されます。他のプレイヤーへHPを渡すときと同じ位置をつかむと、その仲間のアップグレード、能力、デメリットを残りのステージ中コピーします。以後、割り当て一覧にはImitatorとコピー先の役職を併記します。 | Imitator、King、Bomber、Stinker、Werewolf、Jobless、Tuna、???1、???2はコピーしません。最初に成功したコピーはそのステージ中固定です。1人のセッションでは抽選されません。 | 抽選設定のみ |
| Avenger | 他のプレイヤーが30m以内で死亡すると、20秒間、敵へのダメージが1.5倍になります。 | 発動、残り時間の更新、終了はTTSで通知されます。効果中に近くで別のプレイヤーが死亡した場合は、倍率を重複せず残り時間だけを更新します。自身の死亡では発動しません。1人のセッションでは抽選されません。 | 発動範囲、ダメージ倍率、持続時間 |
| Brawler | 攻撃者を特定できる近接武器のダメージが1.25倍になり、銃、杖の弾、レーザーによるダメージは0.75倍になります。 | 車両衝突、Tumble Attack、グレネード、通常の保持物による衝突は変化しません。 | 近接・遠隔ダメージ倍率 |
| ???1 | ??? | ??? | ??? |
| ???2 | ??? | ??? | ??? |

### 設定

すべての設定はREPOConfigから変更できます。ホスト設定はセッション全体へ、ローカル設定はMOD導入者本人のHUDだけに反映されます。

古い設定ファイルを使用している場合も、対応しているカスタム設定を引き継ぎ、名称が変わった項目や古いデフォルト値を更新します。今回の設定整理前のファイルは同じ場所へ`REPOJP.RoleShuffle.cfg.pre-v4.4.0.bak`として保存します。

#### 一般、通知、HUD

| キー | デフォルト | 範囲・値 | 内容 | 管理 |
|---|---:|---|---|---|
| `General.Enabled` | `true` | `true`, `false` | ステージごとの役職割り当てを有効にします。 | ホスト |
| `General.UniqueRoles` | `true` | `true`, `false` | 抽選可能な役職を一巡するまで重複を避けます。 | ホスト |
| `General.ShopUpgradeItemCount` | `0` | `0`～`30` | 各ショップへ要求するアップグレードアイテム数です。`0`でショップの抽選対象から除外します。 | ホスト |
| `Compatibility.EnhancedEnemyRewardsEnabled` | `true` | `true`, `false` | 有効時、Elite Enemy VariantsのEnhanced個体をVampireの回復量とHunterの追加オーブ品質の計算で1段階上として扱います。 | ホスト |
| `Notifications.Enabled` | `true` | `true`, `false` | 役職名と役職効果のチャット／TTS通知を有効にします。 | ホスト |
| `Notifications.DelaySeconds` | `2.5` | `0`～`30` | Stage Flux未導入時の役職通知までの秒数です。 | ホスト |
| `Notifications.StageFluxDelaySeconds` | `5` | `0`～`30` | Stage Flux導入時、役職通知までに最低限待つ秒数です。Stage FluxのTTS終了後、さらに1.5秒間の無音を確認します。 | ホスト |
| `HUD.Enabled` | `true` | `true`, `false` | ステージ中、現在の全役職を表示します。 | ローカル |
| `HUD.RoleDisplay` | `NameOnly` | `IconAndName`, `NameOnly`, `IconOnly` | 役職のアイコン＋名前、名前のみ、アイコンのみを選択します。プレイヤー名は常に表示します。 | ローカル |
| `HUD.IconSize` | `64` | `32`～`128` | HUD全体の倍率を適用する前の役職アイコンの大きさです。行間と1ページの表示人数を自動調整します。 | ローカル |
| `HUD.Anchor` | `BottomLeft` | `TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight` | HUDの基準位置を選択します。 | ローカル |
| `HUD.Alignment` | `Left` | `Left`, `Center`, `Right` | 役職テキストの揃え方を選択します。 | ローカル |
| `HUD.OffsetX` | `0` | `-3840`～`3840` | 基準位置からの水平オフセットです。単位はピクセルです。 | ローカル |
| `HUD.OffsetY` | `80` | `-2160`～`2160` | 基準位置からの垂直オフセットです。単位はピクセルです。 | ローカル |
| `HUD.ScalePercent` | `70` | `50`～`200` | HUDの表示倍率です。 | ローカル |
| `HUD.PlayersPerPage` | `8` | `2`～`20` | 自分の固定表示を含め、同時に表示する最大人数です。アイコンの大きさと文字の高さに合わせ、実際の表示人数が少なくなる場合があります。 | ローカル |
| `HUD.PageIntervalSeconds` | `5` | `1`～`30` | 各役職ページを表示する秒数です。 | ローカル |
| `HUD.TransitionDurationSeconds` | `0.2` | `0`～`1` | 役職ページ切替時のフェード時間です。 | ローカル |
| `HUD.PinLocalPlayer` | `true` | `true`, `false` | 自分の役職をすべてのページへ固定表示します。 | ローカル |

#### 役職バランス

Showcase役は`Bomber`、`Stinker`、`Mage`、`Gambler`、`Trickster`、`King`、`Rider`、`Influencer`、`Diver`です。Support役は`Medic`、`Rescuer`、`Mechanic`、`Electrician`、`Warden`、`Bodyguard`です。Danger役は`Bomber`、`Stinker`、`Werewolf`、Hardship役は`Jobless`、`Tuna`です。InfluencerはDanger役ではありません。これらの条件で割り当て可能な役職がなくなる場合は、全員に役職を割り当てられるまで制限を段階的に緩和します。

| キー | デフォルト | 範囲・値 | 内容 | 管理 |
|---|---:|---|---|---|
| `Role Balance - Guarantees.ShowcaseEnabled` | `true` | `true`, `false` | 参加人数別のShowcase最低保証を有効にします。 | ホスト |
| `Role Balance - Guarantees.ShowcaseMinimums` | `3:1,7:2,14:3,24:4` | `参加人数:最低人数`の組 | 参加人数ごとに保証するShowcaseの最低人数です。 | ホスト |
| `Role Balance - Guarantees.SupportEnabled` | `true` | `true`, `false` | 参加人数別のSupport最低保証を有効にします。 | ホスト |
| `Role Balance - Guarantees.SupportMinimums` | `4:1,8:2,14:3,24:4` | `参加人数:最低人数`の組 | 参加人数ごとに保証するSupportの最低人数です。 | ホスト |
| `Role Balance - Limits.Enabled` | `true` | `true`, `false` | Danger、Hardship、少人数時の複合上限を有効にします。 | ホスト |
| `Role Balance - Limits.DangerMaximums` | `1:1,8:2,16:3` | `参加人数:上限`の組 | 参加人数ごとの`Bomber`、`Stinker`、`Werewolf`の合計上限です。 | ホスト |
| `Role Balance - Limits.HardshipMaximums` | `1:1,12:2` | `参加人数:上限`の組 | 参加人数ごとの`Jobless`と`Tuna`の合計上限です。 | ホスト |
| `Role Balance - Limits.SmallPartyMaximumPlayers` | `4` | `1`～`30` | DangerとHardshipの複合上限を使用する最大参加人数です。 | ホスト |
| `Role Balance - Limits.SmallPartyCombinedMaximum` | `1` | `1`～`30` | 少人数時に許可するDangerとHardshipの合計最大人数です。 | ホスト |
| `Role Balance - Variety.PreventSameRole` | `true` | `true`, `false` | ほかの候補がある場合、同じプレイヤーへの同役職の連続割り当てを防ぎます。 | ホスト |
| `Role Balance - Variety.HardshipCooldownStages` | `2` | `0`～`10` | `Jobless`または`Tuna`の後、そのプレイヤーを両役職から通常除外するステージ数です。 | ホスト |
| `Role Balance - Variety.ExcludeUnavailableRoles` | `true` | `true`, `false` | 必要な敵、武器、使用対象が存在しない状況依存役を抽選から除外します。 | ホスト |

#### 基礎アップグレード

役職が置き換えない場合に使用するアップグレード目標値です。カンマ区切りの`ランレベル:設定値`で記述し、現在のランレベル以下にある最後の設定値を使用します。ランレベルは1～999999に対応します。たとえば`1:1,5:3,10:6`なら、レベル1～4は1、レベル5～9は3、レベル10以降は6です。空欄および最初の指定レベルへ到達する前は0になります。設定範囲外の数値は最も近い上限または下限へ補正し、書式が不正な組だけを無視します。ホスト設定として動作し、基礎値はステージ外でも維持します。任意のトラック抽選を有効にすると、ショップを出た後のトラック準備フェーズに抽選を行います。1回の抽選で、個別に設定した相対Weightを使って対象アップグレードを1種類選びます。増減数はカンマ区切りの`増減値:重み`で設定し、デフォルトは-1（10）、変化なし（15）、+1（60）、+2（15）です。現在の上限または下限を越えずに適用できる対象が一つもない増減値は候補から外し、残った重みで抽選します。正の結果では`ALL UPGRADES`が選ばれることがあり、上限まで余地がある全Base Upgradeを強化します。変更はそのラン中維持され、結果はホストが発言します。Throwは対象外で、RoleShuffleから変更しません。

| キー | デフォルト | 範囲・値 | 内容 | 管理 |
|---|---:|---|---|---|
| `Base Upgrades.HealthUpgradeLevels` | `1:1` | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのHealth基礎目標値です。 | ホスト |
| `Base Upgrades.StaminaUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのStamina基礎目標値です。 | ホスト |
| `Base Upgrades.ExtraJumpUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのExtra Jump基礎目標値です。 | ホスト |
| `Base Upgrades.SpeedUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのSpeed基礎目標値です。 | ホスト |
| `Base Upgrades.StrengthUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのStrength基礎目標値です。 | ホスト |
| `Base Upgrades.RangeUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのRange基礎目標値です。 | ホスト |
| `Base Upgrades.LaunchUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのLaunch基礎目標値です。 | ホスト |
| `Base Upgrades.TumbleClimbUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのTumble Climb基礎目標値です。 | ホスト |
| `Base Upgrades.TumbleWingsUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのTumble Wings基礎目標値です。 | ホスト |
| `Base Upgrades.CrouchRestUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのCrouch Rest基礎目標値です。 | ホスト |
| `Base Upgrades.MapPlayerCountUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`1` | ランレベルごとのMap Player Count基礎目標値です。 | ホスト |
| `Base Upgrades.DeathHeadBatteryUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのDeath Head Battery基礎目標値です。 | ホスト |
| `Base Upgrade Draw.Enabled` | `true` | `true`, `false` | ショップ後の共有Base Upgrade抽選を有効にします。 | ホスト |
| `Base Upgrade Draw.MaximumLevel` | `200` | `0`～`200` | 正のトラック抽選で到達できる最大目標値です。Map Player Countは最大1のままです。 | ホスト |
| `Base Upgrade Draw.CappedUpgradeWeightMultiplier` | `0.25` | `0`～`1` | アップグレードが抽選上限に近づくほどWeightが低下し、上限でこの倍率になります。設定分とトラック抽選分の合計を使用します。`0`では上限到達後に対象外となり、`1`では低下しません。All Upgradesは対象外です。 | ホスト |
| `Base Upgrade Draw.WeightFalloffExponent` | `2` | `0.1`～`10` | 抽選Weightの低下カーブです。`1`は一定のペースで低下し、大きい値ほど上限付近まで重みを維持します。 | ホスト |
| `Base Upgrade Draw.ChangeAmountWeights` | `-1:10,0:15,1:60,2:15` | `増減値:重み`の組、増減値`-200`～`200`、重み`0`～`1000` | 抽選される増減値の相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Health` | `20` | `0`～`1000` | Healthの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Stamina` | `40` | `0`～`1000` | Staminaの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.ExtraJump` | `10` | `0`～`1000` | Extra Jumpの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Speed` | `30` | `0`～`1000` | Speedの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Strength` | `20` | `0`～`1000` | Strengthの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Range` | `30` | `0`～`1000` | Rangeの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.Launch` | `10` | `0`～`1000` | Launchの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.TumbleClimb` | `10` | `0`～`1000` | Tumble Climbの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.TumbleWings` | `10` | `0`～`1000` | Tumble Wingsの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.CrouchRest` | `30` | `0`～`1000` | Crouch Restの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.MapPlayerCount` | `10` | `0`～`1000` | Map Player Countの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.DeathHeadBattery` | `10` | `0`～`1000` | Death Head Batteryの相対Weightです。 | ホスト |
| `Base Upgrade Draw Weights.AllUpgrades` | `1` | `0`～`1000` | 正の結果で使う`ALL UPGRADES`の相対Weightです。 | ホスト |

Weightのデフォルト値はバニラのショップ最大出現数を反映し、Staminaは40、Speed・Range・Crouch Restは30、Health・Strengthは20、その他の対象アップグレードは10、ALL UPGRADESは1です。設定Weightをそのまま使い、ショップ出現数による追加の乗算は行いません。抽選上限に近づくほどWeightが低下し、上限ではCappedUpgradeWeightMultiplierの倍率になります。保存済みのWeightは保持されるため、新しいデフォルト値を使う場合は各設定を初期値へ戻してください。他MODによるショップの変更やShopUpgradeItemCountには影響されません。

#### 役職の抽選設定

通常役職には`Enabled`と相対的な`Weight`があります。`Enabled = false`または`Weight = 0`の通常役職はランダム抽選から除外されます。`???1`と`???2`にはそれぞれ`Enabled`だけがあり、抽選Weightは固定です。

| 役職 | Enabledキーとデフォルト | Weightキーとデフォルト | 設定可能値 | 管理 |
|---|---|---|---|---|
| Tank | `Tank.Enabled = true` | `Tank.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Runner | `Runner.Enabled = true` | `Runner.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Jumper | `Jumper.Enabled = true` | `Jumper.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Lifter | `Lifter.Enabled = true` | `Lifter.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Launcher | `Launcher.Enabled = true` | `Launcher.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Climber | `Climber.Enabled = true` | `Climber.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Flyer | `Flyer.Enabled = true` | `Flyer.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Tracker | `Tracker.Enabled = true` | `Tracker.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Ghost | `Ghost.Enabled = true` | `Ghost.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Bomber | `Bomber.Enabled = true` | `Bomber.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Medic | `Medic.Enabled = true` | `Medic.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Phoenix | `Phoenix.Enabled = true` | `Phoenix.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Jobless | `Jobless.Enabled = true` | `Jobless.Weight = 20` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Rescuer | `Rescuer.Enabled = true` | `Rescuer.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Vampire | `Vampire.Enabled = true` | `Vampire.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| King | `King.Enabled = true` | `King.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Tuna | `Tuna.Enabled = true` | `Tuna.Weight = 50` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Musician | `Musician.Enabled = true` | `Musician.Weight = 60` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Mage | `Mage.Enabled = true` | `Mage.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Gambler | `Gambler.Enabled = true` | `Gambler.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Hunter | `Hunter.Enabled = true` | `Hunter.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Stinker | `Stinker.Enabled = true` | `Stinker.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Engineer | `Engineer.Enabled = true` | `Engineer.Weight = 70` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Trickster | `Trickster.Enabled = true` | `Trickster.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Mechanic | `Mechanic.Enabled = true` | `Mechanic.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Electrician | `Electrician.Enabled = true` | `Electrician.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Warden | `Warden.Enabled = true` | `Warden.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Ninja | `Ninja.Enabled = true` | `Ninja.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Executioner | `Executioner.Enabled = true` | `Executioner.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Rider | `Rider.Enabled = true` | `Rider.Weight = 60` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Influencer | `Influencer.Enabled = true` | `Influencer.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Werewolf | `Werewolf.Enabled = true` | `Werewolf.Weight = 40` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Berserker | `Berserker.Enabled = true` | `Berserker.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Bodyguard | `Bodyguard.Enabled = true` | `Bodyguard.Weight = 80` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Rammer | `Rammer.Enabled = true` | `Rammer.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Diver | `Diver.Enabled = true` | `Diver.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Sniper | `Sniper.Enabled = true` | `Sniper.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Imitator | `Imitator.Enabled = true` | `Imitator.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Avenger | `Avenger.Enabled = true` | `Avenger.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| Brawler | `Brawler.Enabled = true` | `Brawler.Weight = 100` | Enabled: `true`, `false`、Weight: `0`～`1000` | ホスト |
| ???1 | `???1.Enabled = true` | 固定 | Enabled: `true`, `false` | ホスト |
| ???2 | `???2.Enabled = true` | 固定 | Enabled: `true`, `false` | ホスト |

#### アップグレード・特殊役職設定

| キー | デフォルト | 範囲・値 | 内容 | 管理 |
|---|---:|---|---|---|
| `Tank.HealthUpgradeLevels` | `21` | `0`～`200` | Tankへ付与するHealthレベルです。 | ホスト |
| `Tracker.HealthUpgradeLevels` | `3` | `0`～`200` | TrackerのHealth目標値です。 | ホスト |
| `Runner.SpeedUpgradeLevels` | `6` | `0`～`200` | Runnerへ付与するSpeedレベルです。 | ホスト |
| `Runner.StaminaUpgradeLevels` | `46` | `0`～`200` | Runnerへ付与するStaminaレベルです。 | ホスト |
| `Jumper.ExtraJumpUpgradeLevels` | `10` | `0`～`200` | Jumperへ付与するExtra Jumpレベルです。 | ホスト |
| `Lifter.StrengthUpgradeLevels` | `25` | `0`～`200` | Lifterへ付与するStrengthレベルです。 | ホスト |
| `Launcher.LaunchUpgradeLevels` | `10` | `0`～`200` | Launcherへ付与するLaunchレベルです。 | ホスト |
| `Climber.ClimbUpgradeLevels` | `50` | `0`～`200` | Climberへ付与するTumble Climbレベルです。 | ホスト |
| `Climber.RangeUpgradeLevels` | `20` | `0`～`200` | Climberへ付与するRangeレベルです。 | ホスト |
| `Flyer.WingsUpgradeLevels` | `10` | `0`～`200` | Flyerへ付与するTumble Wingsレベルです。 | ホスト |
| `Ghost.DeathHeadBatteryUpgradeLevels` | `50` | `0`～`200` | Ghostへ付与するDeath Head Batteryレベルです。 | ホスト |
| `Bomber.DistancePerGrenade` | `8` | `1`～`100` | グレネードを1個設置するために必要な移動距離です。単位はメートルです。 | ホスト |
| `Bomber.MaximumActiveGrenades` | `30` | `1`～`30` | Bomberごとに保持する生成済みグレネードの最大数です。さらに設置すると最も古いものを削除します。 | ホスト |
| `Bomber.AllowTruckSpawns` | `false` | `true`, `false` | トラック内でのグレネード設置を許可します。 | ホスト |
| `Bomber.ExplosiveGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのExplosive Grenadeを含めます。 | ホスト |
| `Bomber.StunGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのStun Grenadeを含めます。 | ホスト |
| `Bomber.ShockwaveGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのShockwave Grenadeを含めます。 | ホスト |
| `Bomber.DuctTapedGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのDuct Taped Grenadeを含めます。 | ホスト |
| `Medic.HealAmount` | `5` | `1`～`100` | 1回につき周囲の各プレイヤーを回復する量です。 | ホスト |
| `Medic.HealIntervalSeconds` | `2` | `0.1`～`30` | Medicの回復間隔です。 | ホスト |
| `Medic.HealRadius` | `5` | `1`～`30` | 回復可能な最大距離です。単位はメートルです。 | ホスト |
| `Medic.TotalHealingLimit` | `150` | `1`～`10000` | Medic一人が1ステージで回復できる合計HPです。対象が実際に失っているHPだけを消費します。 | ホスト |
| `Phoenix.ReviveDelaySeconds` | `2` | `2`～`10` | Phoenixが復活するまでの秒数です。 | ホスト |
| `Phoenix.FailureGraceSeconds` | `5` | `1`～`15` | 復活準備中に、失敗時のステージ遷移を保留する最大秒数です。 | ホスト |
| `Phoenix.RevivalHealth` | `25` | `1`～`1000` | Phoenix復活後のHPです。プレイヤーの最大HPが上限です。 | ホスト |
| `Jobless.Damage` | `1` | `1`～`100` | トラック外で1回ごとに受けるダメージです。 | ホスト |
| `Jobless.DamageIntervalSeconds` | `0.1` | `0.05`～`10` | トラック外でダメージを受ける間隔です。 | ホスト |
| `Rescuer.ReviveDelaySeconds` | `2` | `0`～`10` | 復活対象が死亡してから必要な秒数です。 | ホスト |
| `Rescuer.Radius` | `3` | `1`～`50` | 復活可能な最大距離です。単位はメートルです。 | ホスト |
| `Rescuer.MaximumRevives` | `2` | `1`～`10` | Rescuer1人あたり、1ステージで復活できる最大回数です。 | ホスト |
| `Rescuer.RevivalHealth` | `25` | `1`～`1000` | Rescuerによる復活後のHPです。対象の最大HPが上限です。 | ホスト |
| `Vampire.Tier1HealAmount` | `5` | `1`～`100` | 周囲でDanger Level 1の敵が死亡した際の回復量です。 | ホスト |
| `Vampire.Tier2HealAmount` | `10` | `1`～`100` | 周囲でDanger Level 2の敵が死亡した際の回復量です。 | ホスト |
| `Vampire.Tier3HealAmount` | `50` | `1`～`100` | 周囲でDanger Level 3の敵が死亡した際の回復量です。 | ホスト |
| `Vampire.Radius` | `10` | `1`～`50` | 死亡した敵からの最大距離です。単位はメートルです。 | ホスト |
| `Tuna.StationaryDelaySeconds` | `3` | `0.1`～`30` | 停止してからダメージが始まるまでの秒数です。 | ホスト |
| `Tuna.Damage` | `1` | `1`～`100` | 停止中に1回ごとに受けるダメージです。 | ホスト |
| `Tuna.DamageIntervalSeconds` | `0.1` | `0.05`～`10` | 停止中にダメージを受ける間隔です。 | ホスト |
| `Musician.HealAmount` | `5` | `1`～`100` | 楽器で音を鳴らすたびに、本人と範囲内の各生存プレイヤーを回復する量です。 | ホスト |
| `Musician.HealRadius` | `10` | `1`～`50` | Musicianから回復可能な最大距離です。単位はメートルです。 | ホスト |
| `Mage.CastIntervalSeconds` | `3` | `0.1`～`30` | 魔法発動の最短間隔です。 | ホスト |
| `Mage.StarExpression` | `Angry` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `star`を発動する表情です。 | ホスト |
| `Mage.RollExpression` | `Sad` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `roll`を発動する表情です。 | ホスト |
| `Mage.GravityExpression` | `Suspicious` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `gravity`を発動する表情です。 | ホスト |
| `Mage.VoidExpression` | `EyesClosed` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `void`を発動する表情です。 | ホスト |
| `Mage.LaserExpression` | `Scared` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `laser`を発動する表情です。 | ホスト |
| `Mage.AutoRecoveryEnabled` | `true` | `true`, `false` | ダメージを受けていないMageのHP自動回復を有効にします。 | ホスト |
| `Mage.AutoRecoveryDelaySeconds` | `10` | `0`～`300` | 最後にダメージを受けてから自動回復が始まるまでの秒数です。 | ホスト |
| `Mage.AutoRecoveryIntervalSeconds` | `2` | `0.1`～`60` | HPを自動回復する間隔です。 | ホスト |
| `Mage.AutoRecoveryAmount` | `1` | `0`～`100` | 自動回復1回あたりの回復量です。 | ホスト |
| `Mage.AutoRecoveryTotalHealingLimit` | `120` | `1`～`10000` | プレイヤーごとの、1ステージ中の自動回復の累計上限です。他の回復は数えず、死亡・復活・役職変更でも使用済みの回復量はリセットされません。 | ホスト |
| `Mage.StarHealthCost` | `10` | `0`～`100` | `star`の発動成功後に消費するHPです。 | ホスト |
| `Mage.GravityHealthCost` | `10` | `0`～`100` | `gravity`の発動成功後に消費するHPです。 | ホスト |
| `Mage.RollHealthCost` | `15` | `0`～`100` | `roll`の発動成功後に消費するHPです。 | ホスト |
| `Mage.VoidHealthCost` | `30` | `0`～`100` | `void`の発動成功後に消費するHPです。 | ホスト |
| `Mage.LaserHealthCost` | `50` | `0`～`100` | `laser`の発動成功後に消費するHPです。 | ホスト |
| `Gambler.WinChancePercent` | `50` | `0`～`100` | Valuableを破壊せず、勝利倍率を適用する確率です。 | ホスト |
| `Gambler.WinValueMultiplier` | `2` | `0`～`10` | 勝利時にValuableへ適用する価格倍率です。 | ホスト |
| `Gambler.GambitGreenHealAmount` | `50` | `25`～`1000` | Gamblerに対するGambitの緑結果で回復する合計HPです。 | ホスト |
| `Gambler.GambitRedDamage` | `100` | `50`～`1000` | Gamblerに対するGambitの赤結果で受ける合計ダメージです。 | ホスト |
| `Gambler.GambitWhiteHealthUpgradeLevels` | `5` | `0`～`200` | Gambitの白結果で、そのステージ中のみ追加するHealthアップグレードレベルです。 | ホスト |
| `Hunter.WeaponBatteryConsumptionPercent` | `75` | `0`～`100` | Hunterが使用する武器バッテリー消費量の通常時に対する割合です。 | ホスト |
| `Hunter.DoubleOrbChancePercent` | `10` | `0`～`100` | Hunterが敵を倒した際、特賞に外れた場合に通常のオーブ個数を2倍にする確率です。 | ホスト |
| `Hunter.JackpotOrbChancePercent` | `0.5` | `0`～`100` | Hunterが敵を倒した際、特賞目標を使用する確率です。不足分は追加しますが、通常ドロップが目標を上回る場合は減らしません。 | ホスト |
| `Hunter.JackpotOrbCount` | `10` | `1`～`30` | Hunterの特賞で使用する目標オーブ個数です。 | ホスト |
| `Stinker.DistancePerCloud` | `2` | `1`～`100` | ウラン雲の通過地点を記録する間隔です。 | ホスト |
| `Stinker.MinimumSafetyDistance` | `2` | `1`～`30` | 最新の発生待ち地点にウラン雲を発生させるために必要なStinker本人との最低距離です。古い待機地点は順番に発生します。 | ホスト |
| `Stinker.AllowTruckSpawns` | `false` | `true`, `false` | トラック内でウラン雲の地点記録と発生を許可します。 | ホスト |
| `Trickster.ActiveSeconds` | `25` | `1`～`120` | 設置したデコイを削除するまでの秒数です。 | ホスト |
| `Trickster.DecoyExpression` | `Happy` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | デコイを設置する表情です。 | ホスト |
| `Trickster.CooldownSeconds` | `45` | `1`～`300` | 敵を引きつけたデコイの終了後、次に設置できるまでの秒数です。 | ホスト |
| `Trickster.NoTargetCooldownSeconds` | `10` | `0`～`300` | 敵を一体も引きつけなかったデコイの終了後、次に設置できるまでの秒数です。 | ホスト |
| `Trickster.InvestigateRadius` | `40` | `1`～`100` | デコイを中心に敵へ調査を指示する半径です。単位はメートルです。 | ホスト |
| `Trickster.PulseIntervalSeconds` | `1` | `0.25`～`10` | 敵へ調査を指示する間隔です。 | ホスト |
| `Trickster.PlacementDistance` | `2` | `0.5`～`10` | Tricksterの前方へデコイを設置する距離です。単位はメートルです。 | ホスト |
| `Mechanic.RepairPercentPerSecond` | `2` | `0`～`100` | Mechanicが破損Valuableを直接保持している間、毎秒修復する元価格に対する合計割合です。 | ホスト |
| `Mechanic.MaximumRepairPercentPerStage` | `50` | `0`～`100` | Mechanic一人が1ステージで修復できる元価格に対する合計割合です。 | ホスト |
| `Electrician.ChargePercentPerSecond` | `10` | `0`～`100` | Electricianが使用していない充電可能アイテムを直接保持している間、毎秒回復する合計バッテリー割合です。 | ホスト |
| `Electrician.MaximumChargePercentPerStage` | `100` | `0`～`1000` | Electrician一人が1ステージで回復できる合計バッテリー割合です。 | ホスト |
| `Warden.AdditionalStunSeconds` | `3` | `0`～`30` | Wardenが敵へ発生させたスタンに追加する秒数です。 | ホスト |
| `Ninja.VisionRecognitionMultiplier` | `2` | `1`～`10` | 敵が視覚でNinjaを認識するまでの時間へ適用する倍率です。 | ホスト |
| `Executioner.StunnedDamageMultiplier` | `2` | `1`～`10` | 攻撃前からスタンしている敵への直接攻撃ダメージ倍率です。 | ホスト |
| `Rider.EnemyDamageMultiplier` | `3` | `1`～`10` | Riderが運転中の車両が敵へ与える衝突ダメージ倍率です。 | ホスト |
| `Rider.PlayerKnockbackMultiplier` | `2` | `1`～`10` | プレイヤーへ元から発生する車両Tumbleノックバック倍率です。 | ホスト |
| `Influencer.PlayerRadius` | `20` | `1`～`100` | 周囲の生存中の仲間を数える半径です。単位はメートルです。 | ホスト |
| `Influencer.PlayerCheckIntervalSeconds` | `0.5` | `0.1`～`10` | 周囲人数と動的アップグレードを確認する間隔です。 | ホスト |
| `Influencer.NoiseRadiusMultiplier` | `2` | `1`～`10` | 足音、着地音、VC、チャットTTSの敵探知範囲倍率です。 | ホスト |
| `Influencer.TTSInvestigateRadius` | `20` | `1`～`100` | 定期TTSが発生させる敵探知範囲です。 | ホスト |
| `Influencer.MinimumTTSIntervalSeconds` | `20` | `1`～`300` | 定期TTS間隔を抽選する最小秒数です。 | ホスト |
| `Influencer.MaximumTTSIntervalSeconds` | `40` | `1`～`300` | 定期TTS間隔を抽選する最大秒数です。最小値より小さい場合は、小さい方を最小値として使用します。 | ホスト |
| `Influencer.TTSQuietPeriodSeconds` | `1.5` | `0`～`10` | Stage Fluxを含む他のTTS終了後に必要な無音時間です。 | ホスト |
| `Influencer.<Upgrade>UpgradeScaling` | アップグレードごと | `人数:レベル`の組 | 周囲人数に応じた最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`1:1,2:4,3:9,4:13,5:20`です。対象はBase Upgradesと同じで、空欄ならレベルを付与しません。 | ホスト |
| `Werewolf.PlayerDamageMultiplier` | `2` | `1`～`10` | Werewolfが他のプレイヤーへ与える特定可能なダメージ倍率です。 | ホスト |
| `Berserker.<Upgrade>UpgradeScaling` | アップグレードごと | `HP割合:レベル`の組 | 残りHPが指定割合以下の間に使う最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`80:1,60:4,40:9,20:13,10:20`です。対象はHealthを除くBase Upgradesと同じで、空欄ならレベルを付与しません。 | ホスト |
| `Bodyguard.HealthUpgradeLevels` | `11` | `0`～`200` | BodyguardのHealth目標値です。 | ホスト |
| `Bodyguard.ProtectionRadius` | `15` | `1`～`100` | 仲間を保護できる最大距離です。単位はメートルです。 | ホスト |
| `Bodyguard.DamageSharePercent` | `50` | `0`～`100` | BodyguardのHPを1残して肩代わりする敵ダメージ割合です。 | ホスト |
| `Rammer.TumbleAttackDamage` | `100` | `0`～`100000` | RammerのTumble Attackが敵へ与えるダメージです。 | ホスト |
| `Rammer.SelfDamage` | `15` | `0`～`100000` | RammerのTumble Attackが敵へ命中した後、自身が受けるダメージです。 | ホスト |
| `Diver.UnderfloorDurationSeconds` | `10` | `1`～`120` | Diverが死亡せず床下に滞在できる最大秒数です。 | ホスト |
| `Diver.MovementForce` | `8` | `1`～`30` | Diverが床下を自由移動するときの力です。 | ホスト |
| `Sniper.ReferenceDistance` | `8` | `1`～`50` | 敵へのダメージが1倍になる距離です。単位はメートルです。 | ホスト |
| `Sniper.MinimumDamageMultiplier` | `0.5` | `0`～`1` | 密着時の敵ダメージ倍率です。元が正のダメージなら最低1ダメージを与えます。 | ホスト |
| `Sniper.MaximumDamageMultiplier` | `2` | `1`～`10` | 遠距離での敵ダメージの最高倍率です。 | ホスト |
| `Sniper.MaximumMultiplierDistance` | `24` | `1`～`100` | 最高倍率へ到達する距離です。基準距離以下の場合は、基準距離の直後として扱います。 | ホスト |
| `Avenger.DamageMultiplier` | `1.5` | `1`～`10` | 他のプレイヤーが死亡した後の、敵へのダメージ倍率です。 | ホスト |
| `Avenger.DurationSeconds` | `20` | `1`～`120` | 敵へのダメージ増加が続く秒数です。別の死亡で残り時間を更新します。 | ホスト |
| `Avenger.TriggerRadius` | `30` | `1`～`100` | Avengerが発動する、死亡したプレイヤーからの最大距離です。 | ホスト |
| `Brawler.MeleeDamageMultiplier` | `1.25` | `0`～`10` | 攻撃者を特定できる近接武器が敵とプレイヤーへ与えるダメージ倍率です。 | ホスト |
| `Brawler.RangedDamageMultiplier` | `0.75` | `0`～`10` | 攻撃者を特定できる銃、杖の弾、レーザーが敵とプレイヤーへ与えるダメージ倍率です。 | ホスト |

デフォルトの基礎値はランレベル1からHealthが`1`、その他の管理対象アップグレードが`0`です。固定役職のアップグレード値は基礎値への加算ではなく、ステージ中の絶対的な目標値です。役職終了時は設定された基礎値へ戻し、役職が置き換えないアップグレードは基礎値を維持します。InfluencerとBerserkerは動的目標値を最低値として扱い、役職付与時に記録したレベル未満へ下げません。固定効果の役職は割り当てやセッション状態が変化した場合だけ変更し、InfluencerとBerserkerはInfluencerの確認間隔ごとに設定条件を再評価します。`Tracker`はMap Player Countを常に`1`へ設定し、Health目標値のみ変更できます。`Rammer`はステージ中、Launch、Tumble Climb、Tumble Wingsを常に0へ上書きします。Throwは管理対象外です。`King`には効果強度の設定がありません。

InfluencerとBerserkerの記述式は、`条件:レベル`をカンマまたはセミコロンで区切ります。Influencerは到達した人数条件のうち最後の目標値、Berserkerは現在到達している最も低いHP境界の目標値を適用します。目標値が`0`になる組はデフォルトの記述から省略し、明示的に0へ上書きしたい場合だけ`条件:0`を手動で追加します。レベルはMap Player Countだけ`0`～`1`、ほかは`0`～`100`です。形式が正しくない項目は無視されます。

### 通知とHUD

- `CURRENT ROLES`と`ROLE GUIDE`の役職にエンブレムを表示し、HUDでも設定で表示できます。六角形のエンブレムの外側は透過表示です。未開示の隠し役職は共通の「?」エンブレムで表示し、開示時に役職固有のエンブレムへ切り替わります。エンブレムはMOD導入済みのプレイヤーに表示されます。
- ステージ開始時、各プレイヤーは割り当てられた英語の役職名をバニラのチャット／TTSで発言します。
- RoleShuffleが生成する通知TTSでは敵が反応しません。通常のマイク入力やその他のワールド音は、Ninjaで抑止される場合を除いてバニラの動作を維持します。
- Stage Flux導入時は、ステージ開始通知が重ならないようStage Flux用の通知遅延を使用します。
- Stage FluxのTTS終了後、1.5秒間の無音を確認してから役職を通知します。
- 役職割り当て、役職効果通知、役職照会への自動応答は1件ずつ順番に発話し、Stage Fluxの通知とも重なりません。
- InfluencerのTTSもほかの通知が終わるまで待機しますが、役職能力として意図的に敵へ聞こえる状態を維持します。
- MOD導入済みの参加者には、1列の`ROLES` HUDがデフォルトで左下に名前のみで表示されます。自分の役職を固定し、ほかのプレイヤーを5秒ごとに切り替えます。`HUD.RoleDisplay`でアイコン表示を有効にできます。`HUD.IconSize`でアイコンを拡大すると、行間を広げて1ページの表示人数を自動調整します。標準サイズのアイコン表示では自分を含め最大4人、名前のみでは最大8人を表示します。文字が消えないよう、使用フォントの高さに合わせて実際の表示人数を調整します。
- Base Upgradeの抽選演出は、ホストとRoleShuffleを導入している参加者に表示されます。
- Escメニュー・ロビーの右上にある`ROLES`ボタンからRolesページを開けます。Escメニューからは`CURRENT ROLES`、ロビーからは`ROLE GUIDE`を最初に表示し、ロビーでは`CURRENT ROLES`を無効にします。左カラムから利用可能な表示や`BASE UPGRADES`へ切り替えられます。`CURRENT ROLES`のプレイヤーをクリックすると、その役職の説明を表示または非表示にできます。`BASE UPGRADES`では、各アップグレードの現在の共有目標値、設定上の目標値、トラック抽選で累積した追加値を確認できます。MOD導入済み参加者にはホストの現在値を表示します。
- `ROLE GUIDE`では有効な役職の説明を英語または日本語で表示し、無効化された役職は非表示になります。ガイド内の言語トグルですぐに切り替えられ、Rolesページを開いた時点では毎回英語が選択されます。日本語の説明には「チェックポイント★リベンジ」を使用します。マルチプレイでは、MOD導入済み参加者にもホストの設定に従った説明と役職の表示・非表示を反映します。ホストの設定がまだ確認できない間は、未確認の数値を含まない説明を表示します。シングルプレイでは自分の設定を使用します。

### 互換性

Stage Fluxは任意の対応MODであり、RoleShuffleの必須MODではありません。

- Second ChanceをPhoenix、Rescuer、Bodyguardの復活効果より優先し、使用されなかった復活効果は維持します。
- Second ChanceまたはPhoenixが失敗時のステージ移行を止めた場合、現在の役職は維持されます。
- RoleShuffleの効果はプレイヤーの移動や接地判定へ干渉しません。
- Value SurgeまたはValue Crash中も、Mechanicは実際に失われた貴重品価値だけを修復します。
- RoleShuffleの通知とInfluencerのTTSは、Stage Fluxの通知と重ならず順番に再生されます。
- Enemy Purgeによる撃破ではHunterの報酬は発生しません。
- Dangerous ValuablesはEngineerが保護している貴重品へ効果を与えません。

Elite Enemy Variantsは任意の対応MODであり、RoleShuffleの必須MODではありません。

- 生存中の`Ninja`は、Elite Enemy Variantsの「見られていないプレイヤー」を条件とする攻撃対象から除外されます。死亡中のNinjaとその他のプレイヤーは対象のままです。
- `Compatibility.EnhancedEnemyRewardsEnabled`が`true`の場合、Enhanced個体から得られるVampireの回復とHunterの追加オーブがTier 3相当まで強化されます。通常のオーブ数は変わりません。
- Enhanced Gambitが捕獲後に移動しても、Gamblerのルーレット効果は正しいプレイヤーへ適用されます。
- RoleShuffleのHUDやメニューへElite Enemy Variantsの敵数、状態、撃破通知は追加しません。

### ゲームプレイ上の注意

- `Bomber`は起動済みのバニラハザードを生成します。爆発や物理効果によってプレイヤーや貴重品へ被害が出る可能性があります。
- `Mage`は前後の空白と大文字・小文字を無視したチャットキーワードで選択した、実体のあるバニラ攻撃を発射します。発射体とビームはプレイヤーや貴重品へ被害を与える可能性があります。
- `Jobless`と`Tuna`は条件を満たしている間、実際に継続ダメージを与え、死亡する可能性があります。受けたダメージは自動回復しません。
- `Medic`は自身を回復しません。
- `Phoenix`は1ステージに1回だけ自己復活します。`Rescuer`は設定された回数まで周囲の別プレイヤーを復活させます。復活後HPは役職ごとに設定でき、デフォルトは25です。
- 役職が置き換えたアップグレードはステージ終了時に基礎値へ戻ります。その他の管理対象アップグレードは基礎値を維持し、Throwには触れません。
- すべての役職を無効化するか、すべての`Weight`を`0`にすると、ランダム抽選できる役職がなくなります。
