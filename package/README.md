# RoleShuffle

## English

### Overview

RoleShuffle gives each player a random role at the start of a stage. Roles offer movement, healing, combat and other abilities until the stage ends. Base upgrades stay active between stages and can grow as the run progresses.

Only the host needs RoleShuffle for gameplay effects. Sessions of up to 30 players are supported. Players without the mod receive their role, upgrades, effects, and vanilla chat/TTS announcement normally. Participants who also install RoleShuffle can use the full role HUD.

By default, upgrade items are removed from the shop pool so stage roles remain the source of temporary upgrades.

### Contact

For questions, bug reports, or feedback, please use [GitHub Issues](https://github.com/CapacityDown/RoleShuffle/issues). A GitHub account is required to submit an issue.

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
- Mage and Trickster do not activate abilities when an expression is cleared. The host's own menu expression restoration is also ignored; other players' expressions restored after closing a menu can still activate an ability.
- Single-player uses the same role system, but `Tracker`, `Ghost`, `Medic`, `Courier`, `Rescuer`, `Influencer`, `Werewolf`, `Bodyguard`, `Imitator`, `Avenger`, and `Influenza` are excluded from random assignment when only one player is present.

### Role selection and presets

Open `ROLES` in the lobby or Escape menu, then `ROLE SETTINGS`. The host can click any role to toggle it ON/OFF, including disabled roles and the two secret roles. Changes save to the same `Enabled` entries as MOD settings and apply to future role assignments, including new arrivals; players keep their currently assigned roles. Participants can view the host's switches but cannot change them. Older hosts that do not provide this information show an unavailable message.

Choose `PRESETS` and apply a play style to replace the role switches. Presets preserve role weights, abilities, balance rules, Base Upgrades and HUD settings. A role with weight `0` still cannot be selected even when ON. Manual combinations appear as `Custom`. Existing installations keep their selection until a preset is explicitly applied.

| Preset | Enabled roles | Play style |
| --- | --- | --- |
| Standard | 43 | Every role, including both secret roles; restores the default ON/OFF selection. |
| Beginner | 19 | Basic upgrades, recovery and protection without passive hazard or hardship roles. |
| Cooperative | 16 | Team support, healing, repairs and shared survival. |
| Chaos | 15 | Explosions, magic, gambles and unpredictable effects. |
| Challenge | 15 | Risky and specialized roles without dedicated healing or revival roles. |

Party-size, context and balance restrictions still apply. The screen identifies a disabled global assignment setting and an empty candidate pool; selecting a preset does not override either `General.Enabled` or zero weights.

### Role command

- Enter `/roles` in the in-game chat to announce only your assigned role name.
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
- A player normally cannot receive the same role in consecutive stages. After receiving `Courier`, `Tuna`, or `Influenza`, that player is excluded from these Hardship roles for the next two stages. These restrictions are relaxed only when needed to avoid leaving a player without a role.
- `Jumper`, `Launcher`, `Climber`, `Flyer`, `Tracker`, and `Ghost` are excluded from random assignment whenever any matching base upgrade target is equal to or higher than that role's configured target, including increases from truck draws.
- `Influencer` is excluded from random assignment when none of the upgrade targets reachable with the current party size exceed the current Base Upgrades.
- `Tracker`, `Ghost`, `Medic`, `Courier`, `Rescuer`, `Influencer`, `Werewolf`, `Bodyguard`, `Imitator`, `Avenger`, and `Influenza` are not selected in single-player or a one-player session.
- `Imitator` is selected only after another active player has received a role it can copy.
- By default, context-dependent roles are excluded when their ability has no usable target. This includes `Musician`, `Engineer`, `Electrician`, and `Rider` when their required object is absent, `Sniper` when neither a melee weapon nor a gun is available, and `Brawler` when no melee weapon is available. Weapon-like valuables do not count as weapons for either role's assignment, and staffs do not count for Sniper assignment.
- Players who leave are removed from the role list. Returning players regain their previous role; new players receive a role, its upgrades and an announcement.
- Roles and their stage effects are cleared only when the stage actually ends. They remain active if a failed-stage transition is canceled by a revival effect.

### Roles

| Role | Function and default effect | Important limitation or risk | Configurable values |
|---|---|---|---|
| Tank | Raises maximum HP to 1.5 times Base, up to the 4,100 HP growth limit. | Health minimum 21. | Minimum, multiplier, cap |
| Runner | Raises sprint speed and stamina to 1.5 times Base, with growth limits of 205 and 2,040. | Minimum Speed 6, Stamina 46. | Minimums, multipliers, caps |
| Jumper | Adds up to 10 extra jumps before landing by setting Extra Jump to level 10. | Uses the vanilla Extra Jump upgrade and has no separate active ability. | Extra Jump target `0`–`100` |
| Lifter | Heavy-object lifting and turning use the strongest values across Strength levels 0–200. Light objects and shop items (including guns and melee weapons) use Strength level-1 handling. | Strength displays level 200. Excluded from random assignment at Base Strength level 50, which already reaches the maximum. | Fixed ability strength |
| Launcher | Launches the player farther forward when starting a Tumble. Launch is level 10. | Uses the vanilla Launch upgrade and has no separate active ability. | Launch target `0`–`100` |
| Climber | Improves Tumble climbing and allows objects to be grabbed from farther away. Tumble Climb is level 50 and Range is level 20. | Uses the two vanilla upgrades and has no separate active ability. | Tumble Climb and Range targets `0`–`100` |
| Flyer | Keeps Tumble Wings active longer for extended movement through the air. Tumble Wings is level 10. | Uses the vanilla Tumble Wings upgrade and has no separate active ability. | Tumble Wings target `0`–`100` |
| Tracker | Increases maximum health and allows map tools to track another player. Health is level 3 and Map Player Count is level 1. | Map Player Count is fixed at `1`. Never selected randomly when only one player is present. | Health target `0`–`100` |
| Ghost | Sets Death Head Battery to level 50. | Never selected randomly when only one player is present. | Battery target |
| Bomber | Drops a random armed grenade every 8 m traveled. Up to 30 generated grenades remain active per Bomber, and the oldest is removed at the limit. | Grenades can injure players and damage valuables. Only Bomber can hold generated grenades. Movement inside the truck does not count while truck placement is disabled. | Distance, limit, truck placement, grenade types |
| Medic | Heals nearby teammates for 5 HP every 2 seconds within 5 m, up to 150 total HP per stage. | Never heals the Medic. Only living teammates within range are affected. Healing stops when that Medic's stage limit is exhausted. Never selected randomly when only one player is present. | Amount, interval, radius, total limit |
| Phoenix | Automatically revives itself with 25 HP once per stage after dying. | One use per stage. Revival HP cannot exceed the player's maximum HP. | Revival HP, revival delay, failure grace |
| Courier | Hold a valuable worth money and carry it 5 m outside delivery areas, then put it in the truck or an extraction point. Each delivery heals HP and pauses Courier's automatic HP loss by size (below). | Starts with a 30-second pause. Otherwise loses 1 HP per 0.1 seconds outside the truck; can die. Other damage still applies. Unlimited deliveries; each item rewards each player once per stage. Early drops reset distance. Not selected solo. | Delivery and HP loss |
| Rescuer | While alive, approaching within 3 m of a dead teammate's Death Head revives the nearest eligible teammate with 25 HP. | Up to 2 revivals per stage. Revival HP cannot exceed the target's maximum HP. Never selected randomly when only one player is present. | Revival HP, delay, radius, maximum revivals |
| Vampire | Heals when an enemy dies within 10 m: Tier 1 = 5, Tier 2 = 10, Tier 3 = 50. | Uses the enemy's vanilla Danger Level. When Enhanced enemy rewards are enabled with Elite Enemy Variants, Enhanced enemies count one tier higher up to Tier 3. The Vampire must be alive and close to the dying enemy. | Amount per tier, radius |
| King | Crown; grants nearby allies Speed/Range +2 and up to Strength +5 within 12 m. | No self-buff or stacking. Level cap 200; Strength cannot weaken grip/rotation. Leaving removes only King bonuses. One King per stage. | Radius and bonus levels |
| Tuna | After a 5-second grace period at stage start, standing still for 3 seconds causes 1 damage every 0.1 seconds until moving. | Can kill the player. The stationary timer starts after the initial grace period. Moving resets it immediately; lost health is not restored. | Delay, damage, interval |
| Musician | Each instrument note played by the Musician heals the Musician and living players within 10 m for 5 HP. | Requires a vanilla musical valuable. By default, not randomly selected if none is present. | Amount, radius |
| Mage | Uses `star`, `gravity`, `roll`, `void`, or `laser` in chat, or the configured facial expressions, to cast vanilla attacks for 10, 10, 15, 30, or 50 HP. After 10 seconds without damage, restores 1 HP every 2 seconds, up to 120 HP total per stage. Magic cast using a held staff lasts 1.3 times as long. | Selecting a matching expression activates its spell; clearing it does not. Spells share a 3-second cooldown, cannot be cast if the cost would be fatal, and can harm players or valuables. The duration bonus does not apply to chat or expression casts, or the Star Wand's instantaneous attack. | Facial expression per spell, cast cooldown, health cost per spell, automatic recovery toggle, delay, interval, amount, and stage limit |
| Gambler | A held valuable has a 50% chance to double its current value; otherwise it is destroyed. Gambit green heals 50 HP, red deals 100 damage, black kills, and white fully heals and adds 5 Health levels for the stage. | One wager is available at a time and returns after extraction. Damaged valuables use their reduced value. | Win chance, win multiplier, Gambit green heal, red damage, white temporary Health levels |
| Hunter | Held or equipped weapons use 75% of normal battery. Enemies killed by Hunter have a 10% chance to drop twice as many orbs and a 0.5% chance to drop 10. | Only Hunter's held or equipped weapons and confirmed kills receive these effects. When Enhanced enemy rewards are enabled with Elite Enemy Variants, Enhanced enemies raise only the quality tier of Hunter's added orbs; the normal orb count is unchanged. | Battery consumption, both orb chances, jackpot count |
| Stinker | Leaves a harmful uranium cloud after every 2 m traveled. Clouds appear one at a time after Stinker moves at least 2 m away from the newest location. Spawned uranium valuables have a 0.5-second grace period before breaking. | Clouds can damage other players, and Stinker can be hurt by walking back into one. Truck placement is disabled by default. | Distance, safety distance, truck placement |
| Engineer | Prevents supported effect valuables from activating while the Engineer holds them. | Camera, Propane Tank, Snowmobile, Flashlight, Clown Doll, and Love Potion are not affected. By default, not randomly selected if no supported effect valuable is present. | Selection only |
| Trickster | Type `decoy` in chat or select the configured facial expression to place a fixed Scream Doll that repeatedly attracts enemies within 40 m for 25 seconds. | Another decoy cannot be placed while one is active. Its cooldown begins after the decoy ends: 45 seconds if it attracted an enemy, or 10 seconds if it attracted none. The decoy cannot be grabbed, damaged, or delivered. | Decoy facial expression, active time, normal and no-target cooldowns, radius, pulse interval, placement distance |
| Mechanic | While directly holding damaged valuables, restores a shared 2% of original sale value per second, up to 50 percentage points per stage. | Only value that is actually restored counts toward the stage limit. Fully repaired and non-valuable objects are ignored. Multiple held valuables share the same repair rate and stage limit. The sale value is restored; damaged visual appearance may remain. | Repair rate, total stage limit |
| Electrician | While directly holding inactive rechargeable items, restores a shared 10 battery percentage per second, up to 100 percentage points per stage. | Active, full, and unchargeable items are ignored. By default, not randomly selected if no rechargeable item is present. | Charge rate, total stage limit |
| Warden | Extends enemy stuns caused by Warden's weapons, grenades, or held damaging objects by 3 seconds. | The attack must normally be able to stun the enemy. | Additional stun time |
| Ninja | Footsteps, landings, voice chat, and chat TTS do not draw enemy investigation. Enemies take twice as long to recognize Ninja visually. | Close contact and noises from weapons, valuables, impacts, explosions, or hazards can still reveal Ninja. | Visual recognition multiplier |
| Executioner | Deals 2x direct attack damage to enemies that were already stunned before the hit. | The hit that initially causes the stun receives no bonus. Collisions alone do not receive the bonus. | Stunned-enemy damage multiplier |
| Rider | While driving a vanilla vehicle, deals 3x impact damage to enemies and doubles existing Tumble knockback against players. | Player damage is not increased. By default, not randomly selected if no vanilla vehicle is present. | Enemy damage and player knockback multipliers |
| Influencer | Gains stronger upgrades as more living teammates enter a 20 m radius and periodically speaks an English TTS line. | Its footsteps, landings, VC, and chat TTS reach enemies from twice as far away. | Radius, check rate, per-upgrade player-count scaling, noise multiplier, TTS timing and radius |
| Werewolf | Deals 2x damage to another player when Werewolf can be identified as the attacker. | Self-damage, environment damage, and damage with no identifiable player attacker are unchanged. Never selected randomly when only one player is present. | Player damage multiplier |
| Berserker | Gains stronger upgrades as remaining HP becomes lower. | Bonuses decrease again when healed. | Per-upgrade HP-percentage scaling |
| Bodyguard | Sets Health to level 11 and absorbs 50% of enemy damage dealt to a teammate within 15 m. | Transferred damage cannot reduce Bodyguard below 1 HP. | Health target, radius, damage share |
| Rammer | Tumble Attack deals 100 damage to enemies and deals 15 damage to Rammer after a successful hit. | Launch, Tumble Climb, and Tumble Wings are fixed at level 0 for the stage regardless of Base Upgrades. | Tumble Attack and self damage |
| Diver | Tumbling while continuing to look down at a fixed floor enables movement below it for up to 10 seconds. | Failing to return through a floor in time kills Diver. Returning above the floor starts a cooldown equal to 1.5 times the previous dive time. The countdown and ready state are announced by TTS. | Underfloor duration, movement force |
| Sniper | Enemy damage changes continuously with distance: 0.5x at point-blank range, 1x at 8 m, and up to 2x at 24 m. | Applies only when Sniper can be identified as the attacker. Vehicle impacts and Tumble Attacks are unchanged. | Reference distance, minimum and maximum multipliers, maximum-multiplier distance |
| Imitator | Starts with Base Upgrades only. Grabbing another player's health-transfer point copies that teammate's upgrades, abilities, and drawbacks for the rest of the stage. The assignment list then shows the copied role beside Imitator. | Cannot copy Imitator, King, Bomber, Stinker, Werewolf, Courier, Tuna, Influenza, or ???1 or ???2. The first valid copy is fixed for the stage. Never selected randomly when only one player is present. | Selection only |
| Avenger | When another player dies within 30 m, deals 1.5x damage to enemies for 20 seconds. | Activation, duration refreshes, and expiration are announced by TTS. Another nearby death refreshes the duration without stacking the multiplier. Its own death does not activate the effect. Never selected randomly when only one player is present. | Trigger radius, damage multiplier, and duration |
| Brawler | Deals 1.25x damage with identifiable melee weapon attacks and 0.75x damage with identifiable guns, staff projectiles, and lasers. | Vehicle impacts, Tumble Attacks, grenades, and ordinary held-object collisions are unchanged. | Melee and ranged damage multipliers |
| Influenza | **Onset:** 30 seconds after becoming Influenza, maximum HP is fixed at 75; only symptomatic players spread it.<br>**Sneezing:** automatic every 30–90 seconds (about once a minute). Each teammate in front within 5m and 20° to either side has a 60% infection chance.<br>**Voice/chat:** after onset, each utterance or message gives each teammate in front within 3m and 30° to either side a 30% chance. | **If infected:** the teammate immediately becomes Influenza, losing their previous role. They develop symptoms after 30 seconds and can then infect others.<br>**Enemy attention:** sneezes can be heard in every direction (base radius 5m; varies with enemy hearing).<br>Death/revival does not reset the timer. Infection ends with the stage. Excluded from solo draws. | Enabled; Weight (default 20) |
| ???1 | ??? | ??? | ??? |
| ???2 | ??? | ??? | ??? |

### Role growth and support settings

- **Tank / Runner:** strengthen your Base maximum HP, sprint speed and stamina by 1.5 by default, while keeping configured minimums. Their growth limits are 4,100 HP, 205 sprint speed and 2,040 stamina. Existing Base values are preserved. A role is excluded from random assignment if any corresponding Base value reaches its limit or it cannot provide further upgrades.
- **Lifter:** heavy-object lifting and turning stay at the maximum available across Strength levels 0–200, regardless of Base Strength. Small items and shop equipment use Strength level-1 handling; the displayed Strength level is 200. Base Strength level 50 already reaches the maximum and is excluded from random assignment.
- **Courier:** delivering a valuable restores HP and pauses automatic HP loss according to its size. Each item rewards each player once per stage, including after revival or rejoining.
- **King:** grants nearby allies temporary Speed, Range and Strength upgrades. The bonus ends outside the area. It does not affect the King or increase HP or stamina.

Host settings except HUD:

| Key | Default | Range | Effect |
|---|---|---|---|
| `Tank.HealthMultiplier` / `Tank.MaximumHealth` | `1.5` / `4100` | 1–10 / 100–4100 | Maximum HP |
| `Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed` | `1.5` / `205` | 1–10 / 5–205 | Sprint speed |
| `Runner.StaminaMultiplier` / `Runner.MaximumStamina` | `1.5` / `2040` | 1–10 / 40–2040 | Stamina capacity |
| `Courier.ContractDistance` | `5` | 1–50 m | Carry distance outside delivery areas. |
| `Courier.InitialGraceSeconds` | `30` | 0–300 s | Initial attrition break. |
| `Courier.<Size>GraceSeconds` | `30/30/60/90/90/90/120` | 0–300 s | Tiny/Small/Medium/Big/Wide/Tall/VeryTall. Never shortens remaining grace. |
| `Courier.<Size>HealAmount` | `10/25/50/100/100/100/100` | 0–10000 HP | Same size order; fixed HP, capped at maximum health. |
| `King.SpeedBonusLevels` / `King.RangeBonusLevels` | `2` / `2` | 0–200 | Speed / Range bonus levels. |
| `King.StrengthBonusLevels` | `5` | 0–200 | Maximum safe Strength bonus levels. |
| `King.UpgradeRadius` | `12` | 1–30 m | Upgrade aura radius. |
| `HUD.ResourceHudEnabled` | `true` | Boolean | Personal resource HUD at the top left, independent of the role list. |
| `HUD.ResourceHudScalePercent` | `100` | `50`–`200` | Resource HUD scale; fits the screen automatically. |
| `HUD.ResourceHudOffsetX` / `ResourceHudOffsetY` | `0` / `0` | `0`–`3840` / `0`–`2160` | Moves the resource display right or down from below stamina. |

Ability resources appear below stamina in one column and shrink to fit when necessary. The role list shows each player's role. Install the same RoleShuffle version as the host to use the resource HUD.

### Configuration

All settings are available through REPOConfig. Host-controlled settings affect the session. Local settings affect only the installed player's HUD.

You can customize roles, Base Upgrades and notifications below. HUD and language settings are personal preferences.

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
| `HUD.IconSize` | `64` | `32`–`128` | Role icon size before HUD scaling. The HUD grows for six rows and scales to fit the screen. | Local |
| `HUD.Anchor` | `BottomLeft` | `TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight` | Selects the HUD anchor. | Local |
| `HUD.Alignment` | `Left` | `Left`, `Center`, `Right` | Selects the role text alignment. | Local |
| `HUD.OffsetX` | `0` | `-3840`–`3840` | Horizontal offset from the anchor in pixels. | Local |
| `HUD.OffsetY` | `80` | `-2160`–`2160` | Vertical offset from the anchor in pixels. | Local |
| `HUD.ScalePercent` | `70` | `50`–`200` | HUD display scale percentage. | Local |
| `HUD.PlayersPerPage` | `8` | `2`–`20` | Maximum players shown at once, including the pinned local player. Values of 6 or more reserve room for at least six, even with taller fonts or icons. | Local |
| `HUD.PageIntervalSeconds` | `5` | `1`–`30` | Seconds each role page remains visible. | Local |
| `HUD.TransitionDurationSeconds` | `0.2` | `0`–`1` | Duration of the fade between role pages. | Local |
| `HUD.PinLocalPlayer` | `true` | `true`, `false` | Keeps the local player's role visible on every page. | Local |

#### Role balance

All entries in this table are host-controlled.

Showcase roles are `Bomber`, `Stinker`, `Mage`, `Gambler`, `Trickster`, `King`, `Rider`, `Influencer`, and `Diver`. Support roles are `Medic`, `Rescuer`, `Mechanic`, `Electrician`, `Warden`, and `Bodyguard`. Danger roles are `Bomber`, `Stinker`, `Werewolf`, and `Influenza`; Hardship roles are `Courier`, `Tuna`, and `Influenza`. Influencer is not a Danger role. If these rules leave no eligible role, RoleShuffle gradually loosens the limits so every player can still receive a role.

| Key | Default | Range / values | Effect |
| --- | ---: | --- | --- |
| `Role Balance - Guarantees.ShowcaseEnabled` | `true` | `true`, `false` | Enables party-size-based minimum Showcase assignments. |
| `Role Balance - Guarantees.ShowcaseMinimums` | `3:1,7:2,14:3,24:4` | `party size:minimum` pairs | Minimum Showcase assignments by party size. |
| `Role Balance - Guarantees.SupportEnabled` | `true` | `true`, `false` | Enables party-size-based minimum Support assignments. |
| `Role Balance - Guarantees.SupportMinimums` | `4:1,8:2,14:3,24:4` | `party size:minimum` pairs | Minimum Support assignments by party size. |
| `Role Balance - Limits.Enabled` | `true` | `true`, `false` | Enables Danger, Hardship, and small-party combined limits. |
| `Role Balance - Limits.DangerMaximums` | `1:1,8:2,16:3` | `party size:limit` pairs | Maximum simultaneous `Bomber`, `Stinker`, `Werewolf`, and `Influenza` roles by party size. |
| `Role Balance - Limits.HardshipMaximums` | `1:1,12:2` | `party size:limit` pairs | Maximum simultaneous `Courier`, `Tuna`, and `Influenza` roles by party size. |
| `Role Balance - Limits.SmallPartyMaximumPlayers` | `4` | `1`–`30` | Largest party size using the combined Danger and Hardship limit. |
| `Role Balance - Limits.SmallPartyCombinedMaximum` | `1` | `1`–`30` | Maximum combined Danger and Hardship roles in a small party. |
| `Role Balance - Variety.PreventSameRole` | `true` | `true`, `false` | Prevents the same player receiving the same role in consecutive stages when alternatives exist. |
| `Role Balance - Variety.HardshipCooldownStages` | `2` | `0`–`10` | Stages after `Courier`, `Tuna`, or `Influenza` during which that player is normally excluded from these roles. |
| `Role Balance - Variety.ExcludeUnavailableRoles` | `true` | `true`, `false` | Excludes context-dependent roles when their required enemy, weapon, or usable object is absent. |

#### Base upgrades

All entries in this table are host-controlled.

Base targets stay active between stages and apply to upgrades not overridden by the current role. Use comma-separated `run level:value` pairs: `1:1,5:3,10:6` means 1 on levels 1–4, 3 on levels 5–9, and 6 from level 10. Run levels accept 1–999999. Blank settings and levels before the first entry use 0. Values are clamped to the allowed range; malformed pairs are ignored. Throw is never changed.

Truck draws occur after leaving the shop, during truck preparation. They select an upgrade and change amount using relative weights. Amounts that no eligible target can receive within its limits are excluded. Positive results can select `All Upgrades`, affecting enabled types with room below their limits. Results persist for the run and are announced by the host.

Open `ROLES` → `BASE UPGRADES` → `BASE UPGRADE SETTINGS` to toggle the draw and its 13 entries (12 types plus `All Upgrades`). These switches share the saved REPOConfig values. Hosts edit; installed participants view the host's settings. Apply pending REPOConfig edits before leaving its page; Roles UI saves immediately.

OFF excludes a type from future individual and `All Upgrades` draws without removing its base levels or acquired bonuses. An active draw keeps its starting selection. `No individual draw` means weight `0`; an ON type can still receive `All Upgrades`. `No draw` on `All Upgrades` means its weight is `0`. Disabling that entry affects only combined results. All-off selections produce no result.

Manual adjustments are off by default. Enable `Base Upgrades.ManualAdjustmentEnabled` to use +/- in the lobby, truck or shop. Each click changes the displayed total by one, within 0–200 (Map Player Count: 0–1). Adjustments are saved for each game and remain after reloading. The page shows configured levels, manual adjustments and draw bonuses. Turning the setting off removes the manual contribution until re-enabled. Only the host can adjust levels, and the lobby must support saving.

| Key | Default | Range / values | Effect |
| --- | ---: | --- | --- |
| `Base Upgrades.ManualAdjustmentEnabled` | `false` | `true`, `false` | Enables manual adjustments and the minus/plus buttons in the lobby, truck and shop. Off hides the buttons, the Manual breakdown and related guidance, and excludes saved manual amounts from base targets; re-enabling restores them. Base rules and truck draws are unaffected. |
| `Base Upgrades.HealthUpgradeLevels` | `1:1` | `level:value` pairs; value `0`–`200` | Base Health target by run level. |
| `Base Upgrades.StaminaUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Stamina target by run level. |
| `Base Upgrades.ExtraJumpUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Extra Jump target by run level. |
| `Base Upgrades.SpeedUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Speed target by run level. |
| `Base Upgrades.StrengthUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Strength target by run level. |
| `Base Upgrades.RangeUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Range target by run level. |
| `Base Upgrades.LaunchUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Launch target by run level. |
| `Base Upgrades.TumbleClimbUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Tumble Climb target by run level. |
| `Base Upgrades.TumbleWingsUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Tumble Wings target by run level. |
| `Base Upgrades.CrouchRestUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Crouch Rest target by run level. |
| `Base Upgrades.MapPlayerCountUpgradeLevels` | Blank | `level:value` pairs; value `0`–`1` | Base Map Player Count target by run level. |
| `Base Upgrades.DeathHeadBatteryUpgradeLevels` | Blank | `level:value` pairs; value `0`–`200` | Base Death Head Battery target by run level. |
| `Base Upgrade Draw.Enabled` | `true` | `true`, `false` | Enables the shared Base Upgrade draw after leaving the shop. |
| `Base Upgrade Draw.MaximumLevel` | `200` | `0`–`200` | Maximum target that positive truck draws can reach. Map Player Count remains limited to 1. |
| `Base Upgrade Draw.CappedUpgradeWeightMultiplier` | `0.25` | `0`–`1` | Upgrade weights decrease as their combined configured and truck levels approach the draw maximum, reaching this multiplier at the maximum. `0` excludes capped upgrades; `1` disables the decrease. All Upgrades is unaffected. |
| `Base Upgrade Draw.WeightFalloffExponent` | `2` | `0.1`–`10` | Controls the weight decrease curve. `1` decreases evenly; larger values preserve more weight until near the maximum. |
| `Base Upgrade Draw.ChangeAmountWeights` | `-1:10,0:15,1:60,2:15` | `change amount:weight` pairs; amount `-200`–`200`, weight `0`–`1000` | Relative weights for the possible change amounts. |
| `Base Upgrade Draw Selection.Health` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.Stamina` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.ExtraJump` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.Speed` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.Strength` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.Range` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.Launch` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.TumbleClimb` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.TumbleWings` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.CrouchRest` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.MapPlayerCount` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.DeathHeadBattery` | `true` | `true`, `false` | Include this type in future draws. |
| `Base Upgrade Draw Selection.AllUpgrades` | `true` | `true`, `false` | Enables the combined positive result for enabled types only. |
| `Base Upgrade Draw Weights.Health` | `20` | `0`–`1000` | Relative Health selection weight. |
| `Base Upgrade Draw Weights.Stamina` | `40` | `0`–`1000` | Relative Stamina selection weight. |
| `Base Upgrade Draw Weights.ExtraJump` | `10` | `0`–`1000` | Relative Extra Jump selection weight. |
| `Base Upgrade Draw Weights.Speed` | `30` | `0`–`1000` | Relative Speed selection weight. |
| `Base Upgrade Draw Weights.Strength` | `20` | `0`–`1000` | Relative Strength selection weight. |
| `Base Upgrade Draw Weights.Range` | `30` | `0`–`1000` | Relative Range selection weight. |
| `Base Upgrade Draw Weights.Launch` | `10` | `0`–`1000` | Relative Launch selection weight. |
| `Base Upgrade Draw Weights.TumbleClimb` | `10` | `0`–`1000` | Relative Tumble Climb selection weight. |
| `Base Upgrade Draw Weights.TumbleWings` | `10` | `0`–`1000` | Relative Tumble Wings selection weight. |
| `Base Upgrade Draw Weights.CrouchRest` | `30` | `0`–`1000` | Relative Crouch Rest selection weight. |
| `Base Upgrade Draw Weights.MapPlayerCount` | `10` | `0`–`1000` | Relative Map Player Count selection weight. |
| `Base Upgrade Draw Weights.DeathHeadBattery` | `10` | `0`–`1000` | Relative Death Head Battery selection weight. |
| `Base Upgrade Draw Weights.AllUpgrades` | `1` | `0`–`1000` | Relative `ALL UPGRADES` selection weight for positive results. |

Default draw weights are Stamina 40; Speed, Range and Crouch Rest 30; Health and Strength 20; other supported upgrades 10; ALL UPGRADES 1. Larger weights make a result more likely. As an upgrade approaches its draw limit, its weight decreases toward `CappedUpgradeWeightMultiplier`. Shop stock settings do not affect these weights.

#### Role selection

Host-controlled. Each standard role has an `Enabled` switch (`true`/`false`) and a relative `Weight` (`0`–`1000`). OFF or weight `0` excludes it from random assignment. The two secret roles have fixed weights. Exact keys and defaults are listed below.

| Enabled key and default | Weight key and default |
|---|---|
| `Tank.Enabled = true` | `Tank.Weight = 100` |
| `Runner.Enabled = true` | `Runner.Weight = 100` |
| `Jumper.Enabled = true` | `Jumper.Weight = 100` |
| `Lifter.Enabled = true` | `Lifter.Weight = 100` |
| `Launcher.Enabled = true` | `Launcher.Weight = 100` |
| `Climber.Enabled = true` | `Climber.Weight = 100` |
| `Flyer.Enabled = true` | `Flyer.Weight = 100` |
| `Tracker.Enabled = true` | `Tracker.Weight = 100` |
| `Ghost.Enabled = true` | `Ghost.Weight = 80` |
| `Bomber.Enabled = true` | `Bomber.Weight = 80` |
| `Medic.Enabled = true` | `Medic.Weight = 100` |
| `Phoenix.Enabled = true` | `Phoenix.Weight = 100` |
| `Courier.Enabled = true` | `Courier.Weight = 20` |
| `Rescuer.Enabled = true` | `Rescuer.Weight = 80` |
| `Vampire.Enabled = true` | `Vampire.Weight = 100` |
| `King.Enabled = true` | `King.Weight = 100` |
| `Tuna.Enabled = true` | `Tuna.Weight = 50` |
| `Musician.Enabled = true` | `Musician.Weight = 60` |
| `Mage.Enabled = true` | `Mage.Weight = 100` |
| `Gambler.Enabled = true` | `Gambler.Weight = 100` |
| `Hunter.Enabled = true` | `Hunter.Weight = 80` |
| `Stinker.Enabled = true` | `Stinker.Weight = 80` |
| `Engineer.Enabled = true` | `Engineer.Weight = 70` |
| `Trickster.Enabled = true` | `Trickster.Weight = 100` |
| `Mechanic.Enabled = true` | `Mechanic.Weight = 100` |
| `Electrician.Enabled = true` | `Electrician.Weight = 80` |
| `Warden.Enabled = true` | `Warden.Weight = 100` |
| `Ninja.Enabled = true` | `Ninja.Weight = 100` |
| `Executioner.Enabled = true` | `Executioner.Weight = 100` |
| `Rider.Enabled = true` | `Rider.Weight = 60` |
| `Influencer.Enabled = true` | `Influencer.Weight = 100` |
| `Werewolf.Enabled = true` | `Werewolf.Weight = 40` |
| `Berserker.Enabled = true` | `Berserker.Weight = 100` |
| `Bodyguard.Enabled = true` | `Bodyguard.Weight = 80` |
| `Rammer.Enabled = true` | `Rammer.Weight = 100` |
| `Diver.Enabled = true` | `Diver.Weight = 100` |
| `Sniper.Enabled = true` | `Sniper.Weight = 100` |
| `Imitator.Enabled = true` | `Imitator.Weight = 100` |
| `Avenger.Enabled = true` | `Avenger.Weight = 100` |
| `Brawler.Enabled = true` | `Brawler.Weight = 100` |
| `Influenza.Enabled = true` | `Influenza.Weight = 20` |
| `???1.Enabled = true` | Fixed |
| `???2.Enabled = true` | Fixed |

#### Upgrade and special-role settings

All entries in this table are host-controlled.

| Key | Default | Range / values | Effect |
| --- | ---: | --- | --- |
| `Tank.HealthUpgradeLevels` | `21` | `0`–`200` | Minimum Health level guaranteed to Tank. |
| `Tracker.HealthUpgradeLevels` | `3` | `0`–`200` | Health target while Tracker is assigned. |
| `Runner.SpeedUpgradeLevels` | `6` | `0`–`200` | Minimum Speed level guaranteed to Runner. |
| `Runner.StaminaUpgradeLevels` | `46` | `0`–`200` | Minimum Stamina level guaranteed to Runner. |
| `Jumper.ExtraJumpUpgradeLevels` | `10` | `0`–`200` | Extra Jump levels granted to Jumper. |
| `Launcher.LaunchUpgradeLevels` | `10` | `0`–`200` | Launch levels granted to Launcher. |
| `Climber.ClimbUpgradeLevels` | `50` | `0`–`200` | Tumble Climb levels granted to Climber. |
| `Climber.RangeUpgradeLevels` | `20` | `0`–`200` | Range levels granted to Climber. |
| `Flyer.WingsUpgradeLevels` | `10` | `0`–`200` | Tumble Wings levels granted to Flyer. |
| `Ghost.DeathHeadBatteryUpgradeLevels` | `50` | `0`–`200` | Death Head Battery levels granted to Ghost. |
| `Bomber.DistancePerGrenade` | `8` | `1`–`100` | Travel distance in meters required for each grenade placement. |
| `Bomber.MaximumActiveGrenades` | `30` | `1`–`30` | Maximum generated grenades retained per Bomber. Placing another removes the oldest one. |
| `Bomber.AllowTruckSpawns` | `false` | `true`, `false` | Allows grenade placement inside the truck. |
| `Bomber.ExplosiveGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla explosive grenades in the random selection. |
| `Bomber.StunGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla stun grenades in the random selection. |
| `Bomber.ShockwaveGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla shockwave grenades in the random selection. |
| `Bomber.DuctTapedGrenadesEnabled` | `true` | `true`, `false` | Includes vanilla duct-taped grenades in the random selection. |
| `Medic.HealAmount` | `5` | `1`–`100` | Health restored to each nearby teammate each time. |
| `Medic.HealIntervalSeconds` | `2` | `0.1`–`30` | Seconds between each round of Medic healing. |
| `Medic.HealRadius` | `5` | `1`–`30` | Maximum healing distance in meters. |
| `Medic.TotalHealingLimit` | `150` | `1`–`10000` | Maximum total health restored by each Medic per stage. Only health actually missing from a target consumes the limit. |
| `Phoenix.ReviveDelaySeconds` | `2` | `2`–`10` | Seconds before Phoenix revival. |
| `Phoenix.FailureGraceSeconds` | `5` | `1`–`15` | Maximum seconds to hold a failed-stage transition while revival initializes. |
| `Phoenix.RevivalHealth` | `25` | `1`–`1000` | Health after Phoenix revival, capped at the player's maximum health. |
| `Courier.Damage` | `1` | `1`–`100` | Damage each time outside the truck. |
| `Courier.DamageIntervalSeconds` | `0.1` | `0.05`–`10` | Seconds between damage outside the truck. |
| `Rescuer.ReviveDelaySeconds` | `2` | `0`–`10` | Seconds a target must remain dead before rescue. |
| `Rescuer.Radius` | `3` | `1`–`50` | Maximum rescue distance in meters. |
| `Rescuer.MaximumRevives` | `2` | `1`–`10` | Maximum revivals for each Rescuer per stage. |
| `Rescuer.RevivalHealth` | `25` | `1`–`1000` | Health after a Rescuer revival, capped at the target's maximum health. |
| `Vampire.Tier1HealAmount` | `5` | `1`–`100` | Health restored by a nearby Danger Level 1 enemy death. |
| `Vampire.Tier2HealAmount` | `10` | `1`–`100` | Health restored by a nearby Danger Level 2 enemy death. |
| `Vampire.Tier3HealAmount` | `50` | `1`–`100` | Health restored by a nearby Danger Level 3 enemy death. |
| `Vampire.Radius` | `10` | `1`–`50` | Maximum distance in meters from the dying enemy. |
| `Tuna.StationaryDelaySeconds` | `3` | `0.1`–`30` | Seconds without movement before damage begins. |
| `Tuna.Damage` | `1` | `1`–`100` | Damage per hit while stationary. |
| `Tuna.DamageIntervalSeconds` | `0.1` | `0.05`–`10` | Seconds between stationary damage. |
| `Musician.HealAmount` | `5` | `1`–`100` | Health restored to the Musician and each living player in range per instrument note. |
| `Musician.HealRadius` | `10` | `1`–`50` | Maximum healing distance in meters from the Musician. |
| `Mage.CastIntervalSeconds` | `3` | `0.1`–`30` | Minimum seconds between spell activations. |
| `Mage.StarExpression` | `Angry` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `star`. |
| `Mage.RollExpression` | `Sad` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `roll`. |
| `Mage.GravityExpression` | `Suspicious` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `gravity`. |
| `Mage.VoidExpression` | `EyesClosed` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `void`. |
| `Mage.LaserExpression` | `Scared` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that casts `laser`. |
| `Mage.AutoRecoveryEnabled` | `true` | `true`, `false` | Enables automatic Mage health recovery after avoiding damage. |
| `Mage.AutoRecoveryDelaySeconds` | `10` | `0`–`300` | Seconds without taking damage before automatic recovery starts. |
| `Mage.AutoRecoveryIntervalSeconds` | `2` | `0.1`–`60` | Seconds between each automatic recovery. |
| `Mage.AutoRecoveryAmount` | `1` | `0`–`100` | HP restored each time automatic recovery occurs. |
| `Mage.AutoRecoveryTotalHealingLimit` | `120` | `1`–`10000` | Maximum total automatic recovery per player per stage. Other healing is not counted. Death, revival, and role changes do not reset the amount already used. |
| `Mage.StarHealthCost` | `10` | `0`–`100` | Health consumed after a successful `star` cast. |
| `Mage.GravityHealthCost` | `10` | `0`–`100` | Health consumed after a successful `gravity` cast. |
| `Mage.RollHealthCost` | `15` | `0`–`100` | Health consumed after a successful `roll` cast. |
| `Mage.VoidHealthCost` | `30` | `0`–`100` | Health consumed after a successful `void` cast. |
| `Mage.LaserHealthCost` | `50` | `0`–`100` | Health consumed after a successful `laser` cast. |
| `Gambler.WinChancePercent` | `50` | `0`–`100` | Chance that a wager uses the win multiplier instead of destroying the valuable. |
| `Gambler.WinValueMultiplier` | `2` | `0`–`10` | Valuable multiplier applied to a winning wager. |
| `Gambler.GambitGreenHealAmount` | `50` | `25`–`1000` | Total HP restored by Gambit's green result for a Gambler. |
| `Gambler.GambitRedDamage` | `100` | `50`–`1000` | Total damage dealt by Gambit's red result to a Gambler. |
| `Gambler.GambitWhiteHealthUpgradeLevels` | `5` | `0`–`200` | Health upgrade levels added by Gambit's white result for the current stage only. |
| `Hunter.WeaponBatteryConsumptionPercent` | `75` | `0`–`100` | Percentage of normal weapon battery consumption used by Hunter. |
| `Hunter.DoubleOrbChancePercent` | `10` | `0`–`100` | Chance to double the normal orb count after Hunter defeats an enemy, checked only if the jackpot roll fails. |
| `Hunter.JackpotOrbChancePercent` | `0.5` | `0`–`100` | Chance to use the jackpot target after Hunter defeats an enemy. Missing orbs are added, but a normal drop above the target is not reduced. |
| `Hunter.JackpotOrbCount` | `10` | `1`–`30` | Target orb count used by Hunter's jackpot result. |
| `Stinker.DistancePerCloud` | `2` | `1`–`100` | Travel distance in meters between recorded uranium-cloud trail points. |
| `Stinker.MinimumSafetyDistance` | `2` | `1`–`30` | Minimum distance Stinker must be from the newest cloud position. Clouds appear one after another. |
| `Stinker.AllowTruckSpawns` | `false` | `true`, `false` | Allows uranium-cloud trail points to be recorded and created inside the truck. |
| `Trickster.ActiveSeconds` | `25` | `1`–`120` | Seconds before the active decoy is removed. |
| `Trickster.DecoyExpression` | `Happy` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | Facial expression that places the decoy. |
| `Trickster.CooldownSeconds` | `45` | `1`–`300` | Seconds after a decoy that attracted an enemy ends before another can be placed. |
| `Trickster.NoTargetCooldownSeconds` | `10` | `0`–`300` | Seconds after a decoy that attracted no enemies ends before another can be placed. |
| `Trickster.InvestigateRadius` | `40` | `1`–`100` | Enemy investigation radius around the decoy in meters. |
| `Trickster.PulseIntervalSeconds` | `1` | `0.25`–`10` | Seconds between enemy investigation pulses. |
| `Trickster.PlacementDistance` | `2` | `0.5`–`10` | Placement distance in front of Trickster in meters. |
| `Mechanic.RepairPercentPerSecond` | `2` | `0`–`100` | Shared original-value percentage repaired per second while Mechanic directly holds damaged valuables. |
| `Mechanic.MaximumRepairPercentPerStage` | `50` | `0`–`100` | Maximum cumulative original-value percentage repaired by each Mechanic per stage. |
| `Electrician.ChargePercentPerSecond` | `10` | `0`–`100` | Shared battery percentage restored per second while Electrician directly holds inactive rechargeable items. |
| `Electrician.MaximumChargePercentPerStage` | `100` | `0`–`1000` | Maximum cumulative battery percentage restored by each Electrician per stage. |
| `Warden.AdditionalStunSeconds` | `3` | `0`–`30` | Seconds added to enemy stuns caused by Warden. |
| `Ninja.VisionRecognitionMultiplier` | `2` | `1`–`10` | Multiplier applied to the time enemies need to visually recognize Ninja. |
| `Executioner.StunnedDamageMultiplier` | `2` | `1`–`10` | Direct attack damage multiplier against enemies already stunned before the hit. |
| `Rider.EnemyDamageMultiplier` | `3` | `1`–`10` | Vehicle impact damage multiplier against enemies while Rider drives. |
| `Rider.PlayerKnockbackMultiplier` | `2` | `1`–`10` | Multiplier for existing vehicle Tumble knockback against players. |
| `Influencer.PlayerRadius` | `20` | `1`–`100` | Radius in meters used to count nearby living teammates. |
| `Influencer.PlayerCheckIntervalSeconds` | `0.5` | `0.1`–`10` | Seconds between nearby-player and dynamic-upgrade checks. |
| `Influencer.NoiseRadiusMultiplier` | `2` | `1`–`10` | Multiplier for footstep, landing, VC, and chat TTS enemy investigation radii. |
| `Influencer.TTSInvestigateRadius` | `20` | `1`–`100` | Enemy investigation radius of periodic TTS. |
| `Influencer.MinimumTTSIntervalSeconds` | `20` | `1`–`300` | Minimum randomly selected periodic TTS interval. |
| `Influencer.MaximumTTSIntervalSeconds` | `40` | `1`–`300` | Maximum randomly selected periodic TTS interval. Values are safely swapped when configured below the minimum. |
| `Influencer.TTSQuietPeriodSeconds` | `1.5` | `0`–`10` | Quiet time required after any TTS, including Stage Flux, before periodic TTS fires. |
| `Influencer.<Upgrade>UpgradeScaling` | Per upgrade | `count:level` pairs | Sets that upgrade's minimum target by nearby-player count without lowering its pre-role level. Example: Strength defaults to `1:1,2:4,3:9,4:13,5:20`. Available upgrades match Base Upgrades. Blank grants no level. |
| `Werewolf.PlayerDamageMultiplier` | `2` | `1`–`10` | Multiplier for identifiable damage Werewolf deals to another player. |
| `Berserker.<Upgrade>UpgradeScaling` | Per upgrade | `HP%:level` pairs | Sets that upgrade's minimum target while remaining HP is at or below the percentage, without lowering its pre-role level. Example: Strength defaults to `80:1,60:4,40:9,20:13,10:20`. Available upgrades match Base Upgrades except Health. Blank grants no level. |
| `Bodyguard.HealthUpgradeLevels` | `11` | `0`–`200` | Health target while Bodyguard is assigned. |
| `Bodyguard.ProtectionRadius` | `15` | `1`–`100` | Maximum teammate-protection distance in meters. |
| `Bodyguard.DamageSharePercent` | `50` | `0`–`100` | Percentage of enemy damage transferred without reducing Bodyguard below 1 HP. |
| `Rammer.TumbleAttackDamage` | `100` | `0`–`100000` | Enemy damage dealt by Rammer's Tumble Attack. |
| `Rammer.SelfDamage` | `15` | `0`–`100000` | Damage Rammer takes after its Tumble Attack hits an enemy. |
| `Diver.UnderfloorDurationSeconds` | `10` | `1`–`120` | Maximum time below a floor before Diver dies. |
| `Diver.MovementForce` | `8` | `1`–`30` | Free-movement force while Diver is below a floor. |
| `Sniper.ReferenceDistance` | `8` | `1`–`50` | Distance in meters where enemy damage is unchanged. |
| `Sniper.MinimumDamageMultiplier` | `0.5` | `0`–`1` | Enemy damage multiplier at point-blank range. Positive hits still deal at least 1 damage. |
| `Sniper.MaximumDamageMultiplier` | `2` | `1`–`10` | Maximum enemy damage multiplier at long range. |
| `Sniper.MaximumMultiplierDistance` | `24` | `1`–`100` | Distance in meters where the maximum multiplier is reached. Values at or below Reference Distance use a point just beyond it. |
| `Avenger.DamageMultiplier` | `1.5` | `1`–`10` | Enemy damage multiplier after another player dies. |
| `Avenger.DurationSeconds` | `20` | `1`–`120` | Duration of the enemy damage bonus. Another death refreshes this duration. |
| `Avenger.TriggerRadius` | `30` | `1`–`100` | Maximum distance in meters from a dying player that activates Avenger. |
| `Brawler.MeleeDamageMultiplier` | `1.25` | `0`–`10` | Damage multiplier for identifiable melee weapon attacks against enemies and players. |
| `Brawler.RangedDamageMultiplier` | `0.75` | `0`–`10` | Damage multiplier for identifiable gun, staff-projectile, and laser attacks against enemies and players. |

The default Base level is Health 1 and 0 for other upgrades. Role upgrade settings specify the level used during that role; they are not levels added to Base. Influencer and Berserker preserve stronger upgrades when their conditions change. `Tracker` uses Map Player Count 1 with configurable Health. `Rammer` sets Launch, Tumble Climb and Tumble Wings to 0 during the stage. Throw is unaffected.

Influencer and Berserker scaling accepts comma- or semicolon-separated `condition:level` pairs. Influencer applies the last target whose player-count condition has been reached. Berserker applies the target belonging to the lowest configured HP threshold currently reached. Default expressions omit pairs whose target level would be `0`; add a `condition:0` pair manually when an explicit zero override is needed. Levels are limited to `0`–`100`, except Map Player Count which is limited to `0`–`1`. Entries in the wrong format are ignored.

### Player tools

Open `ROLES` in the top-right of the Escape or lobby menu, then select `TOOLS` or `DRAW HISTORY`.

- **HUD editor:** `TOOLS` → `HUD EDITOR` opens a sample using the actual HUD layout. Drag it to move it; adjust the anchor, alignment, text size, icon size, overall scale, display mode, and HUD visibility. `SAVE` keeps the local settings. `CANCEL` or Escape discards them; `RESET` restores defaults in the preview. The editor uses mouse input and remains available in the lobby.
- **Bug report:** open `TOOLS` → `REPORT A PROBLEM`, then copy or open the report. Check its contents, add the steps that caused the problem, and submit it through `OPEN GITHUB ISSUES`. The report contains your mod versions, settings and recent activity. Reports are saved in `BepInEx/RoleShuffleReports` and are not sent automatically.
- **Language:** change `UI.GuideLanguage` in MOD settings or use the existing language toggle in Roles. Both controls share the same saved choice. The default is English; an existing selection is preserved. Choices use native names: English, 日本語, 한국어, 简体中文, 繁體中文, Français, Deutsch, Español, Português (Brasil), Italiano, Русский, Polski, Türkçe, Українська. The menu, role explanations, HUD editor, draw history and sync status follow this selection. Role names, upgrade identifiers, chat commands and diagnostic report contents remain in English.
- **Sync status:** `TOOLS` shows whether the role list, guide, Base Upgrades and history are up to date. If the display is delayed, use `REFRESH DISPLAY DATA`. This refreshes the information shown without changing your role or upgrades.
- **Draw history:** `DRAW HISTORY` shows the latest 50 completed truck draws, newest first, including level, selected upgrade, rolled change, and actual before/after Base Upgrade targets. Zero changes and capped outcomes are retained. History is saved with the host's run and shared with installed participants. Cancelled draws are not recorded.

`HUD.FontSize` defaults to `28` (range `16`–`48`). `UI.GuideLanguage` defaults to `English`. Both are personal settings. The default HUD display mode is `NameOnly`.

### Notifications and HUD

- **Resources:** the remaining healing, revives, repair, charge and wagers appear below stamina as cyan icons and numbers in one column. The display fits the available space, and empty resources turn red. It appears while you are alive and have the mod installed. Adjust it with `HUD.ResourceHud*`.
- Role emblems appear beside roles in `CURRENT ROLES` and `ROLE GUIDE`, and optionally in the HUD. The area outside each hexagonal emblem is transparent. Unrevealed secret roles use a shared question-mark emblem until revealed. Emblems are visible to players who have the mod installed.
- At stage start, each player announces the assigned English role name through vanilla chat and TTS.
- RoleShuffle's forced notification TTS does not attract enemies. Ordinary microphone input and other world sounds keep their vanilla behavior unless suppressed by Ninja.
- When Stage Flux is present, RoleShuffle uses the separate Stage Flux announcement delay to avoid overlapping stage-start messages.
- RoleShuffle waits until Stage Flux TTS has finished and remains quiet for 1.5 seconds before announcing roles.
- Role assignments, role-effect notices, and automatic role-query responses are spoken one at a time and do not overlap Stage Flux announcements.
- Influencer TTS also waits for other announcements, but intentionally remains audible to enemies as part of the role ability.
- The local `ROLES` list defaults to names at the bottom left, with your role pinned and pages rotating every 5 seconds. It fits six multilingual/icon rows when `HUD.PlayersPerPage` is 6 or higher; smaller limits are respected. Long player names are shortened to keep role names visible. See HUD settings above.
- The Base Upgrade draw animation is shown to the host and participants who have RoleShuffle installed.
- Open `ROLES` at the top-right of Escape (`CURRENT ROLES`) or the lobby (`ROLE GUIDE`; current roles unavailable). Your role appears first and expanded; click players to toggle descriptions. The left column also opens `BASE UPGRADES`: host targets, configured targets and accumulated truck-draw bonuses.
- `ROLE GUIDE` explains enabled roles in your selected language. In multiplayer, descriptions use the host's settings. If a description is unavailable in your language, it appears in English. Single-player uses your own settings.

### Compatibility

Stage Flux is optional and is not required to install RoleShuffle.

- Second Chance takes priority over Phoenix, Rescuer, and Bodyguard revival effects. Revival effects that are not needed remain available.
- If Second Chance or Phoenix prevents a failed stage transition, current roles remain active.
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

- `Bomber` leaves armed grenades that can injure players and damage valuables.
- `Mage` casts the spell named in chat. Spells can injure players and damage valuables; uppercase or lowercase commands both work.
- `Courier` and `Tuna` continuously deal real damage under their stated conditions and can kill their owner. The damage is not automatically restored.
- `Medic` never heals itself.
- `Phoenix` revives itself once per stage. `Rescuer` revives other nearby players up to the configured limit. Each role uses its own configurable revival HP, with a default of 25.
- Role-specific upgrade overrides return to their configured base levels at stage end. Other managed upgrades stay at their base levels, and Throw is untouched.
- Setting every role to disabled or weight `0` leaves no eligible random role to assign.

## 日本語

### 概要

RoleShuffleはステージ開始時に各プレイヤーへ役職を抽選し、移動・回復・戦闘などの能力を付与します。役職はステージ終了まで有効です。基礎アップグレードはステージ外でも維持され、ランの進行に応じて強化できます。

ゲームプレイ効果はホストだけの導入で利用でき、最大30人のセッションをサポートします。MODを導入していない参加者にも、役職、アップグレード、効果、バニラのチャット／TTS通知が適用されます。RoleShuffleを導入している参加者は、すべての役職を確認できるHUDも利用できます。

デフォルトでは、役職による一時強化を中心にするため、ショップのアップグレードアイテム出現数は`0`です。

### お問い合わせ

ご質問、不具合報告、ご意見は[GitHub Issues](https://github.com/CapacityDown/RoleShuffle/issues)からお送りください。投稿にはGitHubアカウントが必要です。

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
- MageとTricksterは表情の解除では能力を発動しません。ホスト自身のメニュー終了時の表情復帰も除外しますが、参加者のメニュー終了時に復帰した表情では能力が発動する場合があります。
- シングルプレイでも同じ役職システムを使用しますが、参加者が1人だけのときは`Tracker`、`Ghost`、`Medic`、`Courier`、`Rescuer`、`Influencer`、`Werewolf`、`Bodyguard`、`Imitator`、`Avenger`、`Influenza`がランダム抽選から除外されます。

### ロール選択とプリセット

ロビーまたはEscメニューの`ROLES`から`ロール設定`を開くと、ホストが各ロールのON/OFFを切り替えられます。無効なロールや2種類のシークレットも一覧に残ります。変更はMOD設定と同じ`Enabled`へ保存され、途中参加者を含む次のロール抽選から反映されます。割当済みのロールは維持されます。参加者はホストの設定を閲覧できますが、変更はできません。情報を提供しない旧ホストでは未受信と表示します。

`プリセット`から遊び方を選んで適用すると、全ロールのON/OFFを置き換えます。抽選重み・能力値・バランスルール・Base Upgrade・HUD設定は保持します。重み`0`のロールはONでも抽選されません。個別に変更した組み合わせは`カスタム`と表示されます。既存の設定はプリセットを適用するまで変更されません。

| プリセット | 有効なロール数 | 遊び方 |
| --- | --- | --- |
| 標準 | 43 | シークレットを含む全ロール。初期状態のON/OFF構成へ戻します。 |
| 初心者向け | 19 | 基本強化・回復・防御が中心。自動で危害を加える役やハンデ役を除外します。 |
| 協力重視 | 16 | チーム支援・回復・修理を中心に協力して生き残ります。 |
| カオス | 15 | 爆発・魔法・ギャンブルなど、予測しづらい展開を楽しみます。 |
| 高難度 | 15 | 専用の回復・蘇生役を外し、リスクのある特化型ロールで挑みます。 |

人数・状況・バランスによる抽選制限は引き続き適用されます。全体の抽選が無効の場合や候補がない場合は画面に表示します。プリセットを適用しても`General.Enabled`や重み`0`は変更しません。

### 役職確認コマンド

- ゲーム内チャットで`/roles`を入力すると、自分に割り当てられた役職名だけを通知します。
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
- 同じプレイヤーへ前ステージと同じ役職を通常は連続で割り当てません。`Courier`、`Tuna`、`Influenza`のいずれかの後、2ステージはそのプレイヤーをこれらの役職から除外します。役職未割り当てを防ぐ必要がある場合だけ制限を緩和します。
- `Jumper`、`Launcher`、`Climber`、`Flyer`、`Tracker`、`Ghost`は、トラック抽選分を含む基礎アップグレード目標値のいずれかが、対応する役職の設定値以上の場合、ランダム抽選から除外されます。
- `Influencer`は、現在の参加人数で到達可能なアップグレード目標値が現在のBase Upgradeを1項目も上回らない場合、ランダム抽選から除外されます。
- `Tracker`、`Ghost`、`Medic`、`Courier`、`Rescuer`、`Influencer`、`Werewolf`、`Bodyguard`、`Imitator`、`Avenger`、`Influenza`はシングルプレイまたは1人のセッションでは抽選されません。
- `Imitator`は、コピー可能な役職を持つ他の参加者が確保された場合だけ抽選されます。
- デフォルトでは、能力を使える対象がない状況依存役を抽選から除外します。必要なオブジェクトがない`Musician`、`Engineer`、`Electrician`、`Rider`と、近接武器も銃もない場合の`Sniper`、近接武器がない場合の`Brawler`が対象です。両ロールの抽選判定では、武器として使える貴重品も武器に含めません。Sniperの抽選判定では杖も対象外です。
- 退出したプレイヤーは役職一覧から外れます。再参加すると以前の役職に戻り、新しい参加者には役職・強化・通知が適用されます。
- 役職とステージ中の効果は、ステージが実際に終了した場合だけ解除します。復活効果によって失敗時のステージ移行が中断された場合は維持します。

### 役職一覧

| 役職 | 機能とデフォルト効果 | 主な制限・危険性 | 調整可能な値 |
|---|---|---|---|
| Tank | 基礎の最大HPを1.5倍に強化します。倍率強化の上限は4,100HPです。 | 最低Health 21。 | 最低値・倍率・上限 |
| Runner | 基礎の走行速度・スタミナを1.5倍に強化します。倍率強化の上限は205・2,040です。 | 最低Speed 6・Stamina 46。 | 最低値・倍率・上限 |
| Jumper | Extra Jumpがレベル10になり、着地するまでに最大10回の追加ジャンプを使えます。 | バニラのExtra Jumpアップグレードを使用し、別の能動的な能力はありません。 | Extra Jump目標値`0`～`100` |
| Lifter | 重い物をつかむ力・回転力をStrength Lv0～200の中の最大値に固定。小物やショップ購入アイテム（銃・近接武器を含む）はStrength Lv1相当の力で扱います。 | Strengthの表示はLv200。Base Strengthが既に最大値に達するLv50の場合は抽選対象外です。 | 固定能力 |
| Launcher | Tumble開始時にプレイヤーをより遠く前方へ飛ばします。Launchはレベル10です。 | バニラのLaunchアップグレードを使用し、別の能動的な能力はありません。 | Launch目標値`0`～`100` |
| Climber | Tumble中の登りやすさが増し、より遠くの物を掴めます。Tumble Climbはレベル50、Rangeはレベル20です。 | 2種類のバニラアップグレードを使用し、別の能動的な能力はありません。 | Tumble ClimbとRangeの目標値`0`～`100` |
| Flyer | Tumble Wingsの効果時間が延び、空中をより長く移動できます。Tumble Wingsはレベル10です。 | バニラのTumble Wingsアップグレードを使用し、別の能動的な能力はありません。 | Tumble Wings目標値`0`～`100` |
| Tracker | 最大HPが増加し、マップ系アイテムで別のプレイヤーを追跡できるようになります。Healthはレベル3、Map Player Countはレベル1です。 | Map Player Countは`1`固定です。参加者が1人だけの場合はランダム抽選されません。 | Health目標値`0`～`100` |
| Ghost | Death Head Batteryがレベル50になります。 | 参加者が1人だけの場合はランダム抽選されません。 | Battery目標値 |
| Bomber | 8 m移動するごとに起動済みグレネードをランダム設置します。Bomber 1人につき最大30個まで残り、上限では最も古いものを削除します。 | グレネードはプレイヤーやValuableにも危険です。生成されたグレネードを持てるのはBomberだけです。トラック内設置が無効な場合、トラック内の移動距離は加算されません。 | 距離、上限、トラック内設置、グレネード種類 |
| Medic | 5 m以内の仲間を2秒ごとに5 HP回復し、1ステージにつき合計150 HPまで回復します。 | Medic自身は回復しません。範囲内で生存している仲間だけが対象です。そのMedicの上限を使い切ると回復を停止します。参加者が1人だけの場合はランダム抽選されません。 | 回復量、間隔、範囲、合計上限 |
| Phoenix | 死亡すると、1ステージに1回だけデフォルト25 HPで自動復活します。 | 1ステージにつき1回です。復活後HPはプレイヤーの最大HPを超えません。 | 復活後HP、復活遅延、失敗時猶予 |
| Courier | 価値のある貴重品をつかんだまま搬入先の外で5m運び、トラックか納品所に入れると配達完了。サイズ別にHP回復・自動HP減少の一時停止（下表）。 | 開始時は30秒停止。効果切れ中はトラック外で0.1秒ごと1HP減少、死亡あり。敵の攻撃等は防げません。配達回数無制限、同じ品の報酬は各自1ステージ1回。途中で放すと運搬やり直し。1人では抽選対象外。 | 配達・自動HP減少 |
| Rescuer | 生存中に、死亡した仲間のDeath Headから3 m以内へ近づくと、最も近い対象をデフォルト25 HPで復活させます。 | 1ステージにつき最大2回です。復活後HPは対象の最大HPを超えません。参加者が1人だけの場合はランダム抽選されません。 | 復活後HP、遅延、範囲、最大復活回数 |
| Vampire | 10 m以内で敵が死亡すると、Tier 1は5、Tier 2は10、Tier 3は50回復します。 | 敵のバニラDanger Levelを使用します。Elite Enemy Variants導入時にEnhanced報酬補正が有効なら、Enhanced個体を最大Tier 3まで1段階上として扱います。Vampireが生存し、死亡した敵の近くにいる必要があります。 | Tier別回復量、範囲 |
| King | Crownと12mの強化範囲。味方へSpeed・Range各＋2、Strength最大＋5。 | 自分は対象外。重複なし。上限200、掴む力・回転力の低下なし。範囲外で追加分を解除。1ステージ1人。 | 半径・追加レベル |
| Tuna | ステージ開始時の5秒間の猶予後、3秒間停止すると、移動するまで0.1秒ごとに1ダメージを受けます。 | 死亡する可能性があります。停止時間の計測は最初の猶予後に始まります。動くと即座にリセットし、失った体力は回復しません。 | 停止時間、ダメージ、間隔 |
| Musician | Musicianが楽器で音を鳴らすたびに、本人を含む10 m以内の生存プレイヤーを5 HP回復します。 | バニラの楽器系貴重品が必要です。デフォルトでは存在しない場合に抽選されません。 | 回復量、範囲 |
| Mage | チャットで`star`、`gravity`、`roll`、`void`、`laser`を入力するか、設定した表情を選択し、順に10、10、15、30、50 HPを消費してバニラ攻撃を発動します。10秒間ダメージを受けなければ、2秒ごとに1 HP回復します。自動回復は1ステージの累計120 HPまでです。保持した杖で発動する魔法の効果時間は1.3倍になります。 | 表情を解除して標準へ戻す操作では発動しません。全魔法で3秒のクールダウンを共有し、消費で死亡する場合は発動しません。攻撃はプレイヤーやValuableにも危険です。効果時間の延長はチャット・表情での魔法や、星杖の瞬間的な攻撃には適用しません。 | 魔法ごとの表情、発射クールダウン、魔法ごとのHP消費量、自動回復の有効化、待機時間、間隔、回復量、ステージごとの回復上限 |
| Gambler | 持ったValuableは50%の確率で現在価格が2倍になり、失敗すると破壊されます。Gambitは緑で50 HP回復、赤で100ダメージ、黒で死亡、白で全回復してステージ中のHealthを5レベル増やします。 | 賭けは一度に1回だけ使用でき、納品後に復帰します。傷ついたValuableは低下後の価格を基準にします。 | 勝率、勝利倍率、Gambitの緑回復量、赤ダメージ、白の一時Healthレベル |
| Hunter | 保持または装備中の武器のバッテリー消費量が通常の75%になります。Hunterが倒した敵は10%の確率でオーブが2倍、0.5%の確率で10個になります。 | Hunter本人が保持または装備した武器と、Hunterが倒したと確認できる敵だけが対象です。Elite Enemy Variants導入時にEnhanced報酬補正が有効なら、Enhanced個体によってHunterが追加するオーブの品質Tierだけが上がり、通常オーブ数は変わりません。 | バッテリー消費率、両方のオーブ確率、特賞個数 |
| Stinker | 2 m移動するごとに、ダメージを与えるウラン雲を残します。雲は、Stinkerが最新の発生位置から2 m以上離れた後に1つずつ発生します。出現したウラン貴重品は0.5秒の猶予後に壊れます。 | ウラン雲はほかのプレイヤーへダメージを与えます。Stinkerも雲へ戻るとダメージを受ける可能性があります。デフォルトではトラック内に発生しません。 | 距離、安全距離、トラック内発生 |
| Engineer | 対応している効果付きValuableを保持している間、その効果が発動しないようにします。 | Camera、Propane Tank、Snowmobile、Flashlight、Clown Doll、Love Potionは対象外です。デフォルトでは対応Valuableが存在しない場合に抽選されません。 | 抽選設定のみ |
| Trickster | チャットで`decoy`と入力するか、設定した表情を選択すると、半径40 m以内の敵を25秒間繰り返し引きつける固定式Scream Dollを設置します。 | デコイが有効な間は新しいデコイを設置できません。クールダウンは終了後から始まり、敵を引きつけた場合は45秒、一体も引きつけなかった場合は10秒です。デコイは掴む、破壊する、納品することができません。 | デコイ用の表情、有効時間、通常時と空振り時のクールダウン、範囲、誘導間隔、設置距離 |
| Mechanic | 破損したValuableを直接掴んでいる間、元の売却価格の合計2%を毎秒回復し、1ステージにつき合計50ポイントまで回復します。 | 実際に回復した価格だけがステージ上限へ加算されます。修復済みのValuableとValuable以外は対象外です。複数を同時に掴んだ場合、回復速度とステージ上限を共有します。売却価格を回復する効果で、破損した外見は残る場合があります。 | 回復速度、ステージ合計上限 |
| Electrician | 使用していない充電可能なアイテムを直接掴んでいる間、合計10%のバッテリーを毎秒回復し、1ステージにつき合計100ポイントまで回復します。 | 使用中、満充電、充電不可能なアイテムは対象外です。デフォルトでは充電可能アイテムが存在しない場合に抽選されません。 | 充電速度、ステージ合計上限 |
| Warden | Wardenの武器、グレネード、保持している攻撃用オブジェクトによる敵のスタン時間を3秒延長します。 | 元の攻撃がその敵をスタンさせられる場合だけ適用します。 | 追加スタン時間 |
| Ninja | 足音、着地音、VC、チャットTTSで敵の調査行動を発生させず、視覚で認識されるまでの時間が2倍になります。 | 接近や、武器、Valuable、衝突、爆発、危険物などの物音では発見される可能性があります。 | 視認時間倍率 |
| Executioner | 攻撃前からスタンしている敵への直接攻撃ダメージが2倍になります。 | 最初にスタンさせる攻撃には倍率が適用されません。物がぶつかっただけのダメージには倍率が適用されません。 | スタン中ダメージ倍率 |
| Rider | バニラ車両の運転中、敵への衝突ダメージを3倍にし、プレイヤーへ元から発生するTumbleノックバックを2倍にします。 | プレイヤーへのダメージは増えません。デフォルトではバニラ車両が存在しない場合に抽選されません。 | 敵ダメージ倍率、プレイヤーノックバック倍率 |
| Influencer | 半径20 m以内の生存中の仲間が多いほど強化され、定期的に英語TTSを発言します。 | 足音、着地音、VC、チャットTTSが敵へ届く範囲は2倍です。 | 範囲、確認間隔、アップグレード別の人数連動設定、物音倍率、TTS間隔と範囲 |
| Werewolf | Werewolfが攻撃者と特定できる、他のプレイヤーへのダメージを2倍にします。 | 自傷、環境ダメージ、攻撃者を特定できないダメージは変化しません。1人のセッションでは抽選されません。 | プレイヤーダメージ倍率 |
| Berserker | 残りHPが少ないほど強化されます。 | HPを回復すると強化も戻ります。 | アップグレード別のHP割合連動設定 |
| Bodyguard | Healthがレベル11になり、15 m以内の仲間が敵から受けたダメージの50%を肩代わりします。 | 肩代わりによってBodyguardのHPが1未満になることはありません。 | Health目標、範囲、肩代わり率 |
| Rammer | Tumble Attackで敵へ100ダメージを与え、命中後に自身が15ダメージを受けます。 | ステージ中はBase Upgradeにかかわらず、Launch、Tumble Climb、Tumble Wingsがレベル0に固定されます。 | Tumble Attack・自傷ダメージ |
| Diver | 固定された床で下を向いたままタンブルすると床を抜け、最大10秒間、床下を移動できます。 | 時間内に床を抜けて戻れないと死亡します。床上へ戻ると直前の潜航時間の1.5倍のクールダウンが始まり、残り時間と再使用可能状態はTTSで通知されます。 | 床下制限時間、移動力 |
| Sniper | 敵へのダメージが距離に応じて連続変化し、密着時は0.5倍、8 mで1倍、24 mで最大2倍になります。 | Sniperが攻撃者と特定できる場合だけ適用します。車両衝突とTumble Attackは変化しません。 | 基準距離、最低・最高倍率、最高倍率到達距離 |
| Imitator | 最初はBase Upgradesだけが適用されます。他のプレイヤーへHPを渡すときと同じ位置をつかむと、その仲間のアップグレード、能力、デメリットを残りのステージ中コピーします。以後、割り当て一覧にはImitatorとコピー先の役職を併記します。 | Imitator、King、Bomber、Stinker、Werewolf、Courier、Tuna、Influenza、???1、???2はコピーしません。最初に成功したコピーはそのステージ中固定です。1人のセッションでは抽選されません。 | 抽選設定のみ |
| Avenger | 他のプレイヤーが30m以内で死亡すると、20秒間、敵へのダメージが1.5倍になります。 | 発動、残り時間の更新、終了はTTSで通知されます。効果中に近くで別のプレイヤーが死亡した場合は、倍率を重複せず残り時間だけを更新します。自身の死亡では発動しません。1人のセッションでは抽選されません。 | 発動範囲、ダメージ倍率、持続時間 |
| Brawler | 攻撃者を特定できる近接武器のダメージが1.25倍になり、銃、杖の弾、レーザーによるダメージは0.75倍になります。 | 車両衝突、Tumble Attack、グレネード、通常の保持物による衝突は変化しません。 | 近接・遠隔ダメージ倍率 |
| Influenza（インフルエンザ） | **発症：** この役職になって30秒後、最大HPが75に固定。発症するまでは感染を広げません。<br>**くしゃみ：** 発症後、30～90秒ごと（平均約1分）に自動で発生。前方5m以内・左右それぞれ20度の範囲にいる仲間へ、1人ずつ60％で感染。<br>**VC・チャット：** 発症後、前方3m以内・左右それぞれ30度の範囲にいる仲間へ、1人ずつ30％で感染。VCはひとまとまりの発話につき1回、チャットは1投稿につき1回判定。 | **感染した仲間：** その場で元の役職を失い、インフルエンザへ変更。30秒後に発症し、さらに感染を広げます。<br>**敵への音：** くしゃみは全方向の敵にも届きます。基本は半径5mで、敵の聴力によって変わります。<br>死亡・蘇生で発症までの時間はリセットされず、ステージ終了で解除。1人では抽選対象外。 | 有効・無効、抽選重み（初期値20） |
| ???1 | ??? | ??? | ??? |
| ???2 | ??? | ??? | ??? |

### 役職の成長・支援設定

- **Tank / Runner：** 基礎の最大HP・走行速度・スタミナを、設定した最低値を保ちながら初期設定で1.5倍に強化します。倍率強化の上限は4,100HP・走行速度205・スタミナ2,040。高い基礎値は維持します。対応する基礎値のいずれかが上限に達している場合や、追加の強化がない場合は抽選対象外です。
- **Lifter：** Base Strengthにかかわらず、重い物をつかむ力・回転力をStrength Lv0～200の中の最大値に固定します。小物やショップ購入アイテム（銃・近接武器を含む）はStrength Lv1相当の力で扱い、Strengthの表示はLv200です。Base Strengthが既に最大値に達するLv50の場合は抽選対象外です。
- **Courier：** 貴重品を配達すると、大きさに応じてHPが回復し、自動HP減少が一時停止します。同じ品の報酬は各自1ステージ1回で、蘇生や再参加でも再獲得できません。
- **King：** 範囲内の味方にSpeed・Range・Strengthを一時付与し、範囲外で解除します。King自身や、HP・スタミナは対象外です。

HUD以外はホスト設定です。

| キー | 初期値 | 範囲 | 効果 |
|---|---|---|---|
| `Tank.HealthMultiplier` / `Tank.MaximumHealth` | `1.5` / `4100` | 1–10 / 100–4100 | 最大HP |
| `Runner.SpeedMultiplier` / `Runner.MaximumSprintSpeed` | `1.5` / `205` | 1–10 / 5–205 | 走行速度 |
| `Runner.StaminaMultiplier` / `Runner.MaximumStamina` | `1.5` / `2040` | 1–10 / 40–2040 | スタミナ容量 |
| `Courier.ContractDistance` | `5` | 1–50m | 搬入先の外での運搬距離。 |
| `Courier.InitialGraceSeconds` | `30` | 0–300秒 | 開始猶予。 |
| `Courier.<Size>GraceSeconds` | `30/30/60/90/90/90/120` | 0–300秒 | Tiny/Small/Medium/Big/Wide/Tall/VeryTall（極小/小/中/大/横長/縦長/超縦長）。残り猶予は短縮しません。 |
| `Courier.<Size>HealAmount` | `10/25/50/100/100/100/100` | 0–10000HP | 同じサイズ順。固定HP回復、最大HPまで。 |
| `King.SpeedBonusLevels` / `King.RangeBonusLevels` | `2` / `2` | 0–200 | Speed・Range追加レベル。 |
| `King.StrengthBonusLevels` | `5` | 0–200 | 悪化しないStrengthの追加上限。 |
| `King.UpgradeRadius` | `12` | 1–30 m | 味方に強化を付与する範囲。 |
| `HUD.ResourceHudEnabled` | `true` | 真偽値 | 左上に自分の能力残量を表示。役職一覧とは独立。 |
| `HUD.ResourceHudScalePercent` | `100` | `50`–`200` | 残量HUDの大きさ。画面内に収まるよう自動調整。 |
| `HUD.ResourceHudOffsetX` / `ResourceHudOffsetY` | `0` / `0` | `0`–`3840` / `0`–`2160` | スタミナ直下を基準に、残量表示を右／下へ移動します。 |

能力の残量はスタミナ直下に縦1列で表示し、項目が多い場合は収まるよう縮小します。役職一覧には各プレイヤーの役職を表示します。残量HUDを使う場合は、ホストと同じバージョンのRoleShuffleを導入してください。

### 設定

すべての設定はREPOConfigから変更できます。ホスト設定はセッション全体へ、ローカル設定はMOD導入者本人のHUDだけに反映されます。

役職・基礎アップグレード・通知は以下の項目で調整できます。HUDと言語は各自の好みに設定できます。

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
| `HUD.IconSize` | `64` | `32`～`128` | HUD全体の倍率を適用する前の役職アイコンの大きさです。6人分の高さを確保し、画面に収まる倍率へ調整します。 | ローカル |
| `HUD.Anchor` | `BottomLeft` | `TopLeft`, `TopCenter`, `TopRight`, `MiddleLeft`, `MiddleCenter`, `MiddleRight`, `BottomLeft`, `BottomCenter`, `BottomRight` | HUDの基準位置を選択します。 | ローカル |
| `HUD.Alignment` | `Left` | `Left`, `Center`, `Right` | 役職テキストの揃え方を選択します。 | ローカル |
| `HUD.OffsetX` | `0` | `-3840`～`3840` | 基準位置からの水平オフセットです。単位はピクセルです。 | ローカル |
| `HUD.OffsetY` | `80` | `-2160`～`2160` | 基準位置からの垂直オフセットです。単位はピクセルです。 | ローカル |
| `HUD.ScalePercent` | `70` | `50`～`200` | HUDの表示倍率です。 | ローカル |
| `HUD.PlayersPerPage` | `8` | `2`～`20` | 自分の固定表示を含め、同時に表示する最大人数です。6以上なら、文字やアイコンが高くても6人分の表示領域を確保します。 | ローカル |
| `HUD.PageIntervalSeconds` | `5` | `1`～`30` | 各役職ページを表示する秒数です。 | ローカル |
| `HUD.TransitionDurationSeconds` | `0.2` | `0`～`1` | 役職ページ切替時のフェード時間です。 | ローカル |
| `HUD.PinLocalPlayer` | `true` | `true`, `false` | 自分の役職をすべてのページへ固定表示します。 | ローカル |

#### 役職バランス

この表はすべてホスト設定です。

Showcase役は`Bomber`、`Stinker`、`Mage`、`Gambler`、`Trickster`、`King`、`Rider`、`Influencer`、`Diver`です。Support役は`Medic`、`Rescuer`、`Mechanic`、`Electrician`、`Warden`、`Bodyguard`です。Danger役は`Bomber`、`Stinker`、`Werewolf`、`Influenza`、Hardship役は`Courier`、`Tuna`、`Influenza`です。InfluencerはDanger役ではありません。これらの条件で割り当て可能な役職がなくなる場合は、全員に役職を割り当てられるまで制限を段階的に緩和します。

| キー | デフォルト | 範囲・値 | 内容 |
| --- | ---: | --- | --- |
| `Role Balance - Guarantees.ShowcaseEnabled` | `true` | `true`, `false` | 参加人数別のShowcase最低保証を有効にします。 |
| `Role Balance - Guarantees.ShowcaseMinimums` | `3:1,7:2,14:3,24:4` | `参加人数:最低人数`の組 | 参加人数ごとに保証するShowcaseの最低人数です。 |
| `Role Balance - Guarantees.SupportEnabled` | `true` | `true`, `false` | 参加人数別のSupport最低保証を有効にします。 |
| `Role Balance - Guarantees.SupportMinimums` | `4:1,8:2,14:3,24:4` | `参加人数:最低人数`の組 | 参加人数ごとに保証するSupportの最低人数です。 |
| `Role Balance - Limits.Enabled` | `true` | `true`, `false` | Danger、Hardship、少人数時の複合上限を有効にします。 |
| `Role Balance - Limits.DangerMaximums` | `1:1,8:2,16:3` | `参加人数:上限`の組 | 参加人数ごとの`Bomber`、`Stinker`、`Werewolf`、`Influenza`の合計上限です。 |
| `Role Balance - Limits.HardshipMaximums` | `1:1,12:2` | `参加人数:上限`の組 | 参加人数ごとの`Courier`、`Tuna`、`Influenza`の合計上限です。 |
| `Role Balance - Limits.SmallPartyMaximumPlayers` | `4` | `1`～`30` | DangerとHardshipの複合上限を使用する最大参加人数です。 |
| `Role Balance - Limits.SmallPartyCombinedMaximum` | `1` | `1`～`30` | 少人数時に許可するDangerとHardshipの合計最大人数です。 |
| `Role Balance - Variety.PreventSameRole` | `true` | `true`, `false` | ほかの候補がある場合、同じプレイヤーへの同役職の連続割り当てを防ぎます。 |
| `Role Balance - Variety.HardshipCooldownStages` | `2` | `0`～`10` | `Courier`、`Tuna`、`Influenza`のいずれかの後、そのプレイヤーをこれらの役職から通常除外するステージ数です。 |
| `Role Balance - Variety.ExcludeUnavailableRoles` | `true` | `true`, `false` | 必要な敵、武器、使用対象が存在しない状況依存役を抽選から除外します。 |

#### 基礎アップグレード

この表はすべてホスト設定です。

役職が上書きしない強化には基礎値を適用し、ステージ外でも維持します。カンマ区切りの`ランレベル:設定値`で指定します。`1:1,5:3,10:6`ならレベル1～4は1、5～9は3、10以降は6です。ランレベルは1～999999。空欄や最初の指定レベルより前は0、範囲外の値は上限・下限に補正し、不正な組は無視します。Throwは変更しません。

トラック抽選はショップを出た後の準備中に行い、相対Weightで種類と増減値を選びます。範囲内で適用できる対象がない増減値は除外します。正の結果では`All Upgrades`も候補となり、ONの種類のうち上限まで余地があるものを強化します。結果はホストが発言し、そのラン中維持します。

`ROLES` → `基本アップグレード` → `基本アップグレード設定`で、抽選全体と13項目（12種類＋`All Upgrades`）を切り替えます。REPOConfigと同じ値を保存し、ホストは編集、導入済み参加者はホストの設定を閲覧できます。REPOConfigでは変更を適用してから閉じてください。Roles UIは即時保存します。

OFFの種類は次回以降の個別・一括抽選から除外し、基礎値や獲得済みボーナスは保持します。実行中の抽選は開始時の設定を使用します。`個別抽選なし`は重み`0`で、ONなら`All Upgrades`の対象になります。`All Upgrades`の`抽選なし`は一括抽選の重み`0`を示し、OFFは一括抽選だけを無効にします。全項目OFFなら抽選結果は発生しません。

手動調整は初期OFFです。`Base Upgrades.ManualAdjustmentEnabled`をONにすると、ロビー・トラック・ショップで±を操作できます。1回押すごとに表示値が1変わり、範囲は0～200（Map Player Countは0～1）です。調整分はセーブごとに保存し、再開後も維持します。画面には設定値・手動調整・抽選分の内訳を表示します。OFFにすると手動調整分を除外し、再びONにすると戻ります。操作できるのはホストだけで、保存に対応したロビーが必要です。

| キー | デフォルト | 範囲・値 | 内容 |
| --- | ---: | --- | --- |
| `Base Upgrades.ManualAdjustmentEnabled` | `false` | `true`、`false` | ロビー・トラック・ショップでの手動調整と±ボタンを有効にします。OFFではボタン・内訳の「手動」・手動調整の案内を非表示にし、保存済み調整値を基礎値から除外します。再びONにすると復元します。設定式とトラック抽選には影響しません。 |
| `Base Upgrades.HealthUpgradeLevels` | `1:1` | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのHealth基礎目標値です。 |
| `Base Upgrades.StaminaUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのStamina基礎目標値です。 |
| `Base Upgrades.ExtraJumpUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのExtra Jump基礎目標値です。 |
| `Base Upgrades.SpeedUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのSpeed基礎目標値です。 |
| `Base Upgrades.StrengthUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのStrength基礎目標値です。 |
| `Base Upgrades.RangeUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのRange基礎目標値です。 |
| `Base Upgrades.LaunchUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのLaunch基礎目標値です。 |
| `Base Upgrades.TumbleClimbUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのTumble Climb基礎目標値です。 |
| `Base Upgrades.TumbleWingsUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのTumble Wings基礎目標値です。 |
| `Base Upgrades.CrouchRestUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのCrouch Rest基礎目標値です。 |
| `Base Upgrades.MapPlayerCountUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`1` | ランレベルごとのMap Player Count基礎目標値です。 |
| `Base Upgrades.DeathHeadBatteryUpgradeLevels` | 空欄 | `レベル:設定値`の組、設定値`0`～`200` | ランレベルごとのDeath Head Battery基礎目標値です。 |
| `Base Upgrade Draw.Enabled` | `true` | `true`, `false` | ショップ後の共有Base Upgrade抽選を有効にします。 |
| `Base Upgrade Draw.MaximumLevel` | `200` | `0`～`200` | 正のトラック抽選で到達できる最大目標値です。Map Player Countは最大1のままです。 |
| `Base Upgrade Draw.CappedUpgradeWeightMultiplier` | `0.25` | `0`～`1` | アップグレードが抽選上限に近づくほどWeightが低下し、上限でこの倍率になります。設定分とトラック抽選分の合計を使用します。`0`では上限到達後に対象外となり、`1`では低下しません。All Upgradesは対象外です。 |
| `Base Upgrade Draw.WeightFalloffExponent` | `2` | `0.1`～`10` | 抽選Weightの低下カーブです。`1`は一定のペースで低下し、大きい値ほど上限付近まで重みを維持します。 |
| `Base Upgrade Draw.ChangeAmountWeights` | `-1:10,0:15,1:60,2:15` | `増減値:重み`の組、増減値`-200`～`200`、重み`0`～`1000` | 抽選される増減値の相対Weightです。 |
| `Base Upgrade Draw Selection.Health` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.Stamina` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.ExtraJump` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.Speed` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.Strength` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.Range` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.Launch` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.TumbleClimb` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.TumbleWings` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.CrouchRest` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.MapPlayerCount` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.DeathHeadBattery` | `true` | `true`, `false` | この種類を抽選対象にする。 |
| `Base Upgrade Draw Selection.AllUpgrades` | `true` | `true`, `false` | ONの種類だけを対象に、正の一括抽選を有効にします。 |
| `Base Upgrade Draw Weights.Health` | `20` | `0`～`1000` | Healthの相対Weightです。 |
| `Base Upgrade Draw Weights.Stamina` | `40` | `0`～`1000` | Staminaの相対Weightです。 |
| `Base Upgrade Draw Weights.ExtraJump` | `10` | `0`～`1000` | Extra Jumpの相対Weightです。 |
| `Base Upgrade Draw Weights.Speed` | `30` | `0`～`1000` | Speedの相対Weightです。 |
| `Base Upgrade Draw Weights.Strength` | `20` | `0`～`1000` | Strengthの相対Weightです。 |
| `Base Upgrade Draw Weights.Range` | `30` | `0`～`1000` | Rangeの相対Weightです。 |
| `Base Upgrade Draw Weights.Launch` | `10` | `0`～`1000` | Launchの相対Weightです。 |
| `Base Upgrade Draw Weights.TumbleClimb` | `10` | `0`～`1000` | Tumble Climbの相対Weightです。 |
| `Base Upgrade Draw Weights.TumbleWings` | `10` | `0`～`1000` | Tumble Wingsの相対Weightです。 |
| `Base Upgrade Draw Weights.CrouchRest` | `30` | `0`～`1000` | Crouch Restの相対Weightです。 |
| `Base Upgrade Draw Weights.MapPlayerCount` | `10` | `0`～`1000` | Map Player Countの相対Weightです。 |
| `Base Upgrade Draw Weights.DeathHeadBattery` | `10` | `0`～`1000` | Death Head Batteryの相対Weightです。 |
| `Base Upgrade Draw Weights.AllUpgrades` | `1` | `0`～`1000` | 正の結果で使う`ALL UPGRADES`の相対Weightです。 |

抽選Weightの初期値はStaminaが40、Speed・Range・Crouch Restが30、Health・Strengthが20、その他の対象アップグレードが10、ALL UPGRADESが1です。大きい値ほど選ばれやすくなります。上限に近づくほどWeightが低下し、上限では`CappedUpgradeWeightMultiplier`の倍率になります。ショップの商品数の設定は影響しません。

#### 役職の抽選設定

ホスト設定です。通常役職には`Enabled`（`true`/`false`）と相対的な`Weight`（`0`～`1000`）があり、OFFまたは重み`0`でランダム抽選から除外します。隠し役職2種類の重みは固定です。正確なキーと初期値は次のとおりです。

| Enabledキーとデフォルト | Weightキーとデフォルト |
|---|---|
| `Tank.Enabled = true` | `Tank.Weight = 100` |
| `Runner.Enabled = true` | `Runner.Weight = 100` |
| `Jumper.Enabled = true` | `Jumper.Weight = 100` |
| `Lifter.Enabled = true` | `Lifter.Weight = 100` |
| `Launcher.Enabled = true` | `Launcher.Weight = 100` |
| `Climber.Enabled = true` | `Climber.Weight = 100` |
| `Flyer.Enabled = true` | `Flyer.Weight = 100` |
| `Tracker.Enabled = true` | `Tracker.Weight = 100` |
| `Ghost.Enabled = true` | `Ghost.Weight = 80` |
| `Bomber.Enabled = true` | `Bomber.Weight = 80` |
| `Medic.Enabled = true` | `Medic.Weight = 100` |
| `Phoenix.Enabled = true` | `Phoenix.Weight = 100` |
| `Courier.Enabled = true` | `Courier.Weight = 20` |
| `Rescuer.Enabled = true` | `Rescuer.Weight = 80` |
| `Vampire.Enabled = true` | `Vampire.Weight = 100` |
| `King.Enabled = true` | `King.Weight = 100` |
| `Tuna.Enabled = true` | `Tuna.Weight = 50` |
| `Musician.Enabled = true` | `Musician.Weight = 60` |
| `Mage.Enabled = true` | `Mage.Weight = 100` |
| `Gambler.Enabled = true` | `Gambler.Weight = 100` |
| `Hunter.Enabled = true` | `Hunter.Weight = 80` |
| `Stinker.Enabled = true` | `Stinker.Weight = 80` |
| `Engineer.Enabled = true` | `Engineer.Weight = 70` |
| `Trickster.Enabled = true` | `Trickster.Weight = 100` |
| `Mechanic.Enabled = true` | `Mechanic.Weight = 100` |
| `Electrician.Enabled = true` | `Electrician.Weight = 80` |
| `Warden.Enabled = true` | `Warden.Weight = 100` |
| `Ninja.Enabled = true` | `Ninja.Weight = 100` |
| `Executioner.Enabled = true` | `Executioner.Weight = 100` |
| `Rider.Enabled = true` | `Rider.Weight = 60` |
| `Influencer.Enabled = true` | `Influencer.Weight = 100` |
| `Werewolf.Enabled = true` | `Werewolf.Weight = 40` |
| `Berserker.Enabled = true` | `Berserker.Weight = 100` |
| `Bodyguard.Enabled = true` | `Bodyguard.Weight = 80` |
| `Rammer.Enabled = true` | `Rammer.Weight = 100` |
| `Diver.Enabled = true` | `Diver.Weight = 100` |
| `Sniper.Enabled = true` | `Sniper.Weight = 100` |
| `Imitator.Enabled = true` | `Imitator.Weight = 100` |
| `Avenger.Enabled = true` | `Avenger.Weight = 100` |
| `Brawler.Enabled = true` | `Brawler.Weight = 100` |
| `Influenza.Enabled = true` | `Influenza.Weight = 20` |
| `???1.Enabled = true` | 固定 |
| `???2.Enabled = true` | 固定 |

#### アップグレード・特殊役職設定

この表はすべてホスト設定です。

| キー | デフォルト | 範囲・値 | 内容 |
| --- | ---: | --- | --- |
| `Tank.HealthUpgradeLevels` | `21` | `0`～`200` | Tankに保証する最低Healthレベルです。 |
| `Tracker.HealthUpgradeLevels` | `3` | `0`～`200` | TrackerのHealth目標値です。 |
| `Runner.SpeedUpgradeLevels` | `6` | `0`～`200` | Runnerに保証する最低Speedレベルです。 |
| `Runner.StaminaUpgradeLevels` | `46` | `0`～`200` | Runnerに保証する最低Staminaレベルです。 |
| `Jumper.ExtraJumpUpgradeLevels` | `10` | `0`～`200` | Jumperへ付与するExtra Jumpレベルです。 |
| `Launcher.LaunchUpgradeLevels` | `10` | `0`～`200` | Launcherへ付与するLaunchレベルです。 |
| `Climber.ClimbUpgradeLevels` | `50` | `0`～`200` | Climberへ付与するTumble Climbレベルです。 |
| `Climber.RangeUpgradeLevels` | `20` | `0`～`200` | Climberへ付与するRangeレベルです。 |
| `Flyer.WingsUpgradeLevels` | `10` | `0`～`200` | Flyerへ付与するTumble Wingsレベルです。 |
| `Ghost.DeathHeadBatteryUpgradeLevels` | `50` | `0`～`200` | Ghostへ付与するDeath Head Batteryレベルです。 |
| `Bomber.DistancePerGrenade` | `8` | `1`～`100` | グレネードを1個設置するために必要な移動距離です。単位はメートルです。 |
| `Bomber.MaximumActiveGrenades` | `30` | `1`～`30` | Bomberごとに保持する生成済みグレネードの最大数です。さらに設置すると最も古いものを削除します。 |
| `Bomber.AllowTruckSpawns` | `false` | `true`, `false` | トラック内でのグレネード設置を許可します。 |
| `Bomber.ExplosiveGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのExplosive Grenadeを含めます。 |
| `Bomber.StunGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのStun Grenadeを含めます。 |
| `Bomber.ShockwaveGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのShockwave Grenadeを含めます。 |
| `Bomber.DuctTapedGrenadesEnabled` | `true` | `true`, `false` | ランダム抽選へバニラのDuct Taped Grenadeを含めます。 |
| `Medic.HealAmount` | `5` | `1`～`100` | 1回につき周囲の各プレイヤーを回復する量です。 |
| `Medic.HealIntervalSeconds` | `2` | `0.1`～`30` | Medicの回復間隔です。 |
| `Medic.HealRadius` | `5` | `1`～`30` | 回復可能な最大距離です。単位はメートルです。 |
| `Medic.TotalHealingLimit` | `150` | `1`～`10000` | Medic一人が1ステージで回復できる合計HPです。対象が実際に失っているHPだけを消費します。 |
| `Phoenix.ReviveDelaySeconds` | `2` | `2`～`10` | Phoenixが復活するまでの秒数です。 |
| `Phoenix.FailureGraceSeconds` | `5` | `1`～`15` | 復活準備中に、失敗時のステージ遷移を保留する最大秒数です。 |
| `Phoenix.RevivalHealth` | `25` | `1`～`1000` | Phoenix復活後のHPです。プレイヤーの最大HPが上限です。 |
| `Courier.Damage` | `1` | `1`～`100` | トラック外で1回ごとに受けるダメージです。 |
| `Courier.DamageIntervalSeconds` | `0.1` | `0.05`～`10` | トラック外でダメージを受ける間隔です。 |
| `Rescuer.ReviveDelaySeconds` | `2` | `0`～`10` | 復活対象が死亡してから必要な秒数です。 |
| `Rescuer.Radius` | `3` | `1`～`50` | 復活可能な最大距離です。単位はメートルです。 |
| `Rescuer.MaximumRevives` | `2` | `1`～`10` | Rescuer1人あたり、1ステージで復活できる最大回数です。 |
| `Rescuer.RevivalHealth` | `25` | `1`～`1000` | Rescuerによる復活後のHPです。対象の最大HPが上限です。 |
| `Vampire.Tier1HealAmount` | `5` | `1`～`100` | 周囲でDanger Level 1の敵が死亡した際の回復量です。 |
| `Vampire.Tier2HealAmount` | `10` | `1`～`100` | 周囲でDanger Level 2の敵が死亡した際の回復量です。 |
| `Vampire.Tier3HealAmount` | `50` | `1`～`100` | 周囲でDanger Level 3の敵が死亡した際の回復量です。 |
| `Vampire.Radius` | `10` | `1`～`50` | 死亡した敵からの最大距離です。単位はメートルです。 |
| `Tuna.StationaryDelaySeconds` | `3` | `0.1`～`30` | 停止してからダメージが始まるまでの秒数です。 |
| `Tuna.Damage` | `1` | `1`～`100` | 停止中に1回ごとに受けるダメージです。 |
| `Tuna.DamageIntervalSeconds` | `0.1` | `0.05`～`10` | 停止中にダメージを受ける間隔です。 |
| `Musician.HealAmount` | `5` | `1`～`100` | 楽器で音を鳴らすたびに、本人と範囲内の各生存プレイヤーを回復する量です。 |
| `Musician.HealRadius` | `10` | `1`～`50` | Musicianから回復可能な最大距離です。単位はメートルです。 |
| `Mage.CastIntervalSeconds` | `3` | `0.1`～`30` | 魔法発動の最短間隔です。 |
| `Mage.StarExpression` | `Angry` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `star`を発動する表情です。 |
| `Mage.RollExpression` | `Sad` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `roll`を発動する表情です。 |
| `Mage.GravityExpression` | `Suspicious` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `gravity`を発動する表情です。 |
| `Mage.VoidExpression` | `EyesClosed` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `void`を発動する表情です。 |
| `Mage.LaserExpression` | `Scared` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | `laser`を発動する表情です。 |
| `Mage.AutoRecoveryEnabled` | `true` | `true`, `false` | ダメージを受けていないMageのHP自動回復を有効にします。 |
| `Mage.AutoRecoveryDelaySeconds` | `10` | `0`～`300` | 最後にダメージを受けてから自動回復が始まるまでの秒数です。 |
| `Mage.AutoRecoveryIntervalSeconds` | `2` | `0.1`～`60` | HPを自動回復する間隔です。 |
| `Mage.AutoRecoveryAmount` | `1` | `0`～`100` | 自動回復1回あたりの回復量です。 |
| `Mage.AutoRecoveryTotalHealingLimit` | `120` | `1`～`10000` | プレイヤーごとの、1ステージ中の自動回復の累計上限です。他の回復は数えず、死亡・復活・役職変更でも使用済みの回復量はリセットされません。 |
| `Mage.StarHealthCost` | `10` | `0`～`100` | `star`の発動成功後に消費するHPです。 |
| `Mage.GravityHealthCost` | `10` | `0`～`100` | `gravity`の発動成功後に消費するHPです。 |
| `Mage.RollHealthCost` | `15` | `0`～`100` | `roll`の発動成功後に消費するHPです。 |
| `Mage.VoidHealthCost` | `30` | `0`～`100` | `void`の発動成功後に消費するHPです。 |
| `Mage.LaserHealthCost` | `50` | `0`～`100` | `laser`の発動成功後に消費するHPです。 |
| `Gambler.WinChancePercent` | `50` | `0`～`100` | Valuableを破壊せず、勝利倍率を適用する確率です。 |
| `Gambler.WinValueMultiplier` | `2` | `0`～`10` | 勝利時にValuableへ適用する価格倍率です。 |
| `Gambler.GambitGreenHealAmount` | `50` | `25`～`1000` | Gamblerに対するGambitの緑結果で回復する合計HPです。 |
| `Gambler.GambitRedDamage` | `100` | `50`～`1000` | Gamblerに対するGambitの赤結果で受ける合計ダメージです。 |
| `Gambler.GambitWhiteHealthUpgradeLevels` | `5` | `0`～`200` | Gambitの白結果で、そのステージ中のみ追加するHealthアップグレードレベルです。 |
| `Hunter.WeaponBatteryConsumptionPercent` | `75` | `0`～`100` | Hunterが使用する武器バッテリー消費量の通常時に対する割合です。 |
| `Hunter.DoubleOrbChancePercent` | `10` | `0`～`100` | Hunterが敵を倒した際、特賞に外れた場合に通常のオーブ個数を2倍にする確率です。 |
| `Hunter.JackpotOrbChancePercent` | `0.5` | `0`～`100` | Hunterが敵を倒した際、特賞目標を使用する確率です。不足分は追加しますが、通常ドロップが目標を上回る場合は減らしません。 |
| `Hunter.JackpotOrbCount` | `10` | `1`～`30` | Hunterの特賞で使用する目標オーブ個数です。 |
| `Stinker.DistancePerCloud` | `2` | `1`～`100` | ウラン雲の通過地点を記録する間隔です。 |
| `Stinker.MinimumSafetyDistance` | `2` | `1`～`30` | 最新の発生待ち地点にウラン雲を発生させるために必要なStinker本人との最低距離です。古い待機地点は順番に発生します。 |
| `Stinker.AllowTruckSpawns` | `false` | `true`, `false` | トラック内でウラン雲の地点記録と発生を許可します。 |
| `Trickster.ActiveSeconds` | `25` | `1`～`120` | 設置したデコイを削除するまでの秒数です。 |
| `Trickster.DecoyExpression` | `Happy` | `Disabled`, `Angry`, `Sad`, `Suspicious`, `EyesClosed`, `Scared`, `Happy` | デコイを設置する表情です。 |
| `Trickster.CooldownSeconds` | `45` | `1`～`300` | 敵を引きつけたデコイの終了後、次に設置できるまでの秒数です。 |
| `Trickster.NoTargetCooldownSeconds` | `10` | `0`～`300` | 敵を一体も引きつけなかったデコイの終了後、次に設置できるまでの秒数です。 |
| `Trickster.InvestigateRadius` | `40` | `1`～`100` | デコイを中心に敵へ調査を指示する半径です。単位はメートルです。 |
| `Trickster.PulseIntervalSeconds` | `1` | `0.25`～`10` | 敵へ調査を指示する間隔です。 |
| `Trickster.PlacementDistance` | `2` | `0.5`～`10` | Tricksterの前方へデコイを設置する距離です。単位はメートルです。 |
| `Mechanic.RepairPercentPerSecond` | `2` | `0`～`100` | Mechanicが破損Valuableを直接保持している間、毎秒修復する元価格に対する合計割合です。 |
| `Mechanic.MaximumRepairPercentPerStage` | `50` | `0`～`100` | Mechanic一人が1ステージで修復できる元価格に対する合計割合です。 |
| `Electrician.ChargePercentPerSecond` | `10` | `0`～`100` | Electricianが使用していない充電可能アイテムを直接保持している間、毎秒回復する合計バッテリー割合です。 |
| `Electrician.MaximumChargePercentPerStage` | `100` | `0`～`1000` | Electrician一人が1ステージで回復できる合計バッテリー割合です。 |
| `Warden.AdditionalStunSeconds` | `3` | `0`～`30` | Wardenが敵へ発生させたスタンに追加する秒数です。 |
| `Ninja.VisionRecognitionMultiplier` | `2` | `1`～`10` | 敵が視覚でNinjaを認識するまでの時間へ適用する倍率です。 |
| `Executioner.StunnedDamageMultiplier` | `2` | `1`～`10` | 攻撃前からスタンしている敵への直接攻撃ダメージ倍率です。 |
| `Rider.EnemyDamageMultiplier` | `3` | `1`～`10` | Riderが運転中の車両が敵へ与える衝突ダメージ倍率です。 |
| `Rider.PlayerKnockbackMultiplier` | `2` | `1`～`10` | プレイヤーへ元から発生する車両Tumbleノックバック倍率です。 |
| `Influencer.PlayerRadius` | `20` | `1`～`100` | 周囲の生存中の仲間を数える半径です。単位はメートルです。 |
| `Influencer.PlayerCheckIntervalSeconds` | `0.5` | `0.1`～`10` | 周囲人数と動的アップグレードを確認する間隔です。 |
| `Influencer.NoiseRadiusMultiplier` | `2` | `1`～`10` | 足音、着地音、VC、チャットTTSの敵探知範囲倍率です。 |
| `Influencer.TTSInvestigateRadius` | `20` | `1`～`100` | 定期TTSが発生させる敵探知範囲です。 |
| `Influencer.MinimumTTSIntervalSeconds` | `20` | `1`～`300` | 定期TTS間隔を抽選する最小秒数です。 |
| `Influencer.MaximumTTSIntervalSeconds` | `40` | `1`～`300` | 定期TTS間隔を抽選する最大秒数です。最小値より小さい場合は、小さい方を最小値として使用します。 |
| `Influencer.TTSQuietPeriodSeconds` | `1.5` | `0`～`10` | Stage Fluxを含む他のTTS終了後に必要な無音時間です。 |
| `Influencer.<Upgrade>UpgradeScaling` | アップグレードごと | `人数:レベル`の組 | 周囲人数に応じた最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`1:1,2:4,3:9,4:13,5:20`です。対象はBase Upgradesと同じで、空欄ならレベルを付与しません。 |
| `Werewolf.PlayerDamageMultiplier` | `2` | `1`～`10` | Werewolfが他のプレイヤーへ与える特定可能なダメージ倍率です。 |
| `Berserker.<Upgrade>UpgradeScaling` | アップグレードごと | `HP割合:レベル`の組 | 残りHPが指定割合以下の間に使う最低目標値で、役職付与前のレベルは下げません。例としてStrengthの既定値は`80:1,60:4,40:9,20:13,10:20`です。対象はHealthを除くBase Upgradesと同じで、空欄ならレベルを付与しません。 |
| `Bodyguard.HealthUpgradeLevels` | `11` | `0`～`200` | BodyguardのHealth目標値です。 |
| `Bodyguard.ProtectionRadius` | `15` | `1`～`100` | 仲間を保護できる最大距離です。単位はメートルです。 |
| `Bodyguard.DamageSharePercent` | `50` | `0`～`100` | BodyguardのHPを1残して肩代わりする敵ダメージ割合です。 |
| `Rammer.TumbleAttackDamage` | `100` | `0`～`100000` | RammerのTumble Attackが敵へ与えるダメージです。 |
| `Rammer.SelfDamage` | `15` | `0`～`100000` | RammerのTumble Attackが敵へ命中した後、自身が受けるダメージです。 |
| `Diver.UnderfloorDurationSeconds` | `10` | `1`～`120` | Diverが死亡せず床下に滞在できる最大秒数です。 |
| `Diver.MovementForce` | `8` | `1`～`30` | Diverが床下を自由移動するときの力です。 |
| `Sniper.ReferenceDistance` | `8` | `1`～`50` | 敵へのダメージが1倍になる距離です。単位はメートルです。 |
| `Sniper.MinimumDamageMultiplier` | `0.5` | `0`～`1` | 密着時の敵ダメージ倍率です。元が正のダメージなら最低1ダメージを与えます。 |
| `Sniper.MaximumDamageMultiplier` | `2` | `1`～`10` | 遠距離での敵ダメージの最高倍率です。 |
| `Sniper.MaximumMultiplierDistance` | `24` | `1`～`100` | 最高倍率へ到達する距離です。基準距離以下の場合は、基準距離の直後として扱います。 |
| `Avenger.DamageMultiplier` | `1.5` | `1`～`10` | 他のプレイヤーが死亡した後の、敵へのダメージ倍率です。 |
| `Avenger.DurationSeconds` | `20` | `1`～`120` | 敵へのダメージ増加が続く秒数です。別の死亡で残り時間を更新します。 |
| `Avenger.TriggerRadius` | `30` | `1`～`100` | Avengerが発動する、死亡したプレイヤーからの最大距離です。 |
| `Brawler.MeleeDamageMultiplier` | `1.25` | `0`～`10` | 攻撃者を特定できる近接武器が敵とプレイヤーへ与えるダメージ倍率です。 |
| `Brawler.RangedDamageMultiplier` | `0.75` | `0`～`10` | 攻撃者を特定できる銃、杖の弾、レーザーが敵とプレイヤーへ与えるダメージ倍率です。 |

基礎アップグレードの初期値はHealthが1、ほかが0です。役職のアップグレード設定は、その役職で使用するレベルを表し、基礎値への加算ではありません。InfluencerとBerserkerは条件が変わっても元の高い強化を維持します。`Tracker`はMap Player Countが1で、Healthを設定できます。`Rammer`はステージ中のLaunch・Tumble Climb・Tumble Wingsを0にします。Throwには影響しません。

InfluencerとBerserkerの記述式は、`条件:レベル`をカンマまたはセミコロンで区切ります。Influencerは到達した人数条件のうち最後の目標値、Berserkerは現在到達している最も低いHP境界の目標値を適用します。目標値が`0`になる組はデフォルトの記述から省略し、明示的に0へ上書きしたい場合だけ`条件:0`を手動で追加します。レベルはMap Player Countだけ`0`～`1`、ほかは`0`～`100`です。形式が正しくない項目は無視されます。

### プレイヤー向けツール

Escまたはロビーメニュー右上の`ROLES`から、`TOOLS`または`DRAW HISTORY`を選択します。

- **HUD編集：** `TOOLS` → `HUD編集モード`で、実際のHUDと同じレイアウトのサンプルを表示します。ドラッグで移動し、基準位置・整列・文字サイズ・アイコンサイズ・全体倍率・表示形式・HUDの表示／非表示を調整できます。「保存」でローカル設定に反映し、「取消」またはEscで破棄します。「初期値」はプレビューを初期設定に戻します。マウスで操作でき、ロビーでも使用できます。
- **不具合レポート：** `TOOLS` → `不具合レポート`からレポートをコピーするか、保存したファイルを開きます。内容を確認して発生手順を追記し、「GitHub Issuesを開く」から投稿してください。レポートにはMODのバージョン・設定・最近の動作状況が含まれます。保存先は`BepInEx/RoleShuffleReports`で、自動送信は行いません。
- **言語：** MOD設定の`UI.GuideLanguage`と、Roles内の既存の言語トグルの両方から変更できます。同じ選択値を保存し、メニューの再表示やゲーム再起動時にも復元します。初期値は英語で、既存の選択は引き継ぎます。選択肢は各言語の名称で表示します：English、日本語、한국어、简体中文、繁體中文、Français、Deutsch、Español、Português (Brasil)、Italiano、Русский、Polski、Türkçe、Українська。メニュー、役職説明、HUD編集、抽選履歴、同期状態に適用します。役職名、アップグレード識別名、チャットコマンド、不具合レポートの診断本文は英語表記です。
- **同期状態：** `TOOLS`で、役職一覧・ガイド・Base Upgrade・履歴が最新の状態か確認できます。表示が遅れている場合は「表示データを再取得」を使ってください。表示だけを更新し、役職や強化値は変わりません。
- **抽選履歴：** `DRAW HISTORY`で、完了したトラック抽選の直近50回を新しい順に表示します。レベル・抽選対象・抽選値・実際のBase Upgrade目標値の前後を確認でき、変化なしや上限・下限に達した結果も記録します。ホストのセーブに保存し、MOD導入済み参加者にも共有します。中断した抽選は記録しません。

`HUD.FontSize`の初期値は`28`（範囲`16`～`48`）、`UI.GuideLanguage`の初期値は`English`です。どちらも個人設定です。HUDの初期表示形式は`NameOnly`です。

### 通知とHUD

- **残量HUD：** 回復・蘇生・修理・充電・賭けの残量を、スタミナの下に水色のアイコンと数字で縦1列に表示します。画面に収まるよう大きさを調整し、使い切った項目は赤く表示します。MODを導入している本人の生存中に表示され、`HUD.ResourceHud*`で調整できます。
- `CURRENT ROLES`と`ROLE GUIDE`の役職にエンブレムを表示し、HUDでも設定で表示できます。六角形のエンブレムの外側は透過表示です。未開示の隠し役職は共通の「?」エンブレムで表示し、開示時に役職固有のエンブレムへ切り替わります。エンブレムはMOD導入済みのプレイヤーに表示されます。
- ステージ開始時、各プレイヤーは割り当てられた英語の役職名をバニラのチャット／TTSで発言します。
- RoleShuffleが生成する通知TTSでは敵が反応しません。通常のマイク入力やその他のワールド音は、Ninjaで抑止される場合を除いてバニラの動作を維持します。
- Stage Flux導入時は、ステージ開始通知が重ならないようStage Flux用の通知遅延を使用します。
- Stage FluxのTTS終了後、1.5秒間の無音を確認してから役職を通知します。
- 役職割り当て、役職効果通知、役職照会への自動応答は1件ずつ順番に発話し、Stage Fluxの通知とも重なりません。
- InfluencerのTTSもほかの通知が終わるまで待機しますが、役職能力として意図的に敵へ聞こえる状態を維持します。
- `ROLES`一覧は初期設定で左下に名前のみを表示し、自分を固定して5秒ごとにページを切り替えます。`HUD.PlayersPerPage`が6以上なら多言語・アイコン付きでも6人分を確保し、6未満は設定を優先します。長いプレイヤー名は、役職名が見えるよう短く表示します。表示・サイズ設定は上表を参照してください。
- Base Upgradeの抽選演出は、ホストとRoleShuffleを導入している参加者に表示されます。
- Escメニュー・ロビーの右上にある`ROLES`ボタンからRolesページを開けます。Escメニューからは`CURRENT ROLES`、ロビーからは`ROLE GUIDE`を最初に表示し、ロビーでは`CURRENT ROLES`を無効にします。左カラムから利用可能な表示や`BASE UPGRADES`へ切り替えられます。`CURRENT ROLES`では自分を先頭に表示し、画面を開いた時点で自分の役職説明を展開します。プレイヤーをクリックすると、その役職の説明を表示または非表示にできます。`BASE UPGRADES`では、各アップグレードの現在の共有目標値、設定上の目標値、トラック抽選で累積した追加値を確認できます。MOD導入済み参加者にはホストの現在値を表示します。
- `ROLE GUIDE`では有効な役職の説明を選択した言語で表示します。マルチプレイではホストの設定値に沿った説明になります。選択言語の説明がない場合は英語で表示します。シングルプレイでは自分の設定を使用します。

### 互換性

Stage Fluxは任意の対応MODであり、RoleShuffleの必須MODではありません。

- Second ChanceをPhoenix、Rescuer、Bodyguardの復活効果より優先し、使用されなかった復活効果は維持します。
- Second ChanceまたはPhoenixが失敗時のステージ移行を止めた場合、現在の役職は維持されます。
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

- `Bomber`が残す起動済みグレネードは、プレイヤーや貴重品にも被害を与えます。
- `Mage`はチャットで指定した魔法を発動します。魔法はプレイヤーや貴重品にも被害を与えます。コマンドの大文字・小文字は問いません。
- `Courier`と`Tuna`は条件を満たしている間、実際に継続ダメージを与え、死亡する可能性があります。受けたダメージは自動回復しません。
- `Medic`は自身を回復しません。
- `Phoenix`は1ステージに1回だけ自己復活します。`Rescuer`は設定された回数まで周囲の別プレイヤーを復活させます。復活後HPは役職ごとに設定でき、デフォルトは25です。
- 役職が置き換えたアップグレードはステージ終了時に基礎値へ戻ります。その他の管理対象アップグレードは基礎値を維持し、Throwには触れません。
- すべての役職を無効化するか、すべての`Weight`を`0`にすると、ランダム抽選できる役職がなくなります。
