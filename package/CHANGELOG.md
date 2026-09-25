## 4.5.2
- Changed Lifter to handle carts like heavy objects. Other shop equipment keeps its configured holding level.
- Added optional retention of consumed upgrade items, off by default. Choose upgrades for the consumer or the whole team; retained levels survive role changes, stage transitions, and reloading the same save.
- Set Mage laser spell duration to a configurable 7 seconds by default and kept its firing position fixed. The temporary staff is hidden for RoleShuffle users; unmodded guests can still see it.

## 4.5.1
- Added Influenza, a Danger role: after a 30-second incubation, maximum HP is fixed at 75. Irregular sneezes and ordinary voice or text chat can infect nearby players in front, replacing their role with Influenza. Sneezes also alert nearby enemies. Infection ends with the stage. Onset, HP, sneeze intervals, infection conditions, and enemy hearing are configurable.
- Unified role emblem background colors by category.
- Updated hidden-role abilities.
- Changed Lifter's default heavy-object lifting and turning strength to the highest values across Strength levels 0–200, with configurable holding strength.
- Gave Stinker's spawned uranium valuables a configurable grace period before breaking, defaulting to 0.5 seconds in both solo and multiplayer.
- Increased King's default support to Speed/Range +2 and up to Strength +5 within 12 m.

## 4.5.0
- Changed Tank and Runner to strengthen Base health, sprint speed and stamina by 1.5 by default, with configurable minimums and growth limits. They are excluded from random assignment when no further benefit is available or a corresponding Base value reaches its limit.
- Changed Lifter to make heavy objects easier to lift and turn, while small items use Strength level-1 handling. Strength displays level 200.
- Renamed Jobless to Courier with a new emblem. Deliver valuables to the truck or an extraction point to recover HP and pause automatic HP loss. Rewards depend on the valuable's size. Deliveries are unlimited, with one reward per item per player each stage.
- Changed King to grant nearby allies temporary Speed, Range and Strength upgrades. The bonus does not affect Kings or stack, and ends outside the area.
- Added a personal ability-resource HUD below stamina for healing, revives, repairs, charge and wagers, with adjustable position, size and visibility.
- Changed `/roles` to announce only the assigned role name and simplified the role-list HUD.

## 4.4.7
- Fixed Mage and Trickster abilities activating when expressions end or when opening the Escape menu.
- Improved Current Roles to list your role first, show its description on opening, and keep long messages within the menu.
- Improved six-player HUD layouts for multibyte names and role icons.
- Added host-controlled role ON/OFF settings and Standard, Beginner, Cooperative, Chaos, and Challenge presets in Roles UI, shared with MOD settings.
- Added host-controlled Base Upgrade draw ON/OFF settings in Roles UI, shared with REPOConfig.
- Added optional Base Upgrade +/- adjustments saved per game, available in the lobby, truck, and shop. Disabled by default.

## 4.4.6
- Moved questions, bug reports, and feedback to GitHub Issues.
- Added a HUD editor with draggable preview, text/icon sizing, display options, and Save/Cancel controls.
- Added fresh local bug reports on copy/open with up to 500 recent RoleShuffle log entries, and confirmation before opening GitHub Issues in a browser.
- Expanded menu language support to 14 languages by adding Korean, Simplified Chinese, Traditional Chinese, French, German, Spanish, Brazilian Portuguese, Italian, Russian, Polish, Turkish, and Ukrainian alongside English and Japanese. Includes bundled fonts, native language names, and a saved selection shared by the menu toggle and MOD settings. English is the default.
- Added host synchronization status with display-data refresh.
- Added the latest 50 Base Upgrade draw results, saved with the host's run and shared with installed participants.

## 4.4.5
- Added role emblems to Current Roles and Role Guide, plus optional HUD icons with adjustable size and display modes. Unrevealed secret roles use a shared question-mark emblem.
- Moved the ROLES button to the top-right of the Escape and lobby menus. The lobby menu opens directly to the Role Guide.

## 4.4.4
- Changed upgrade setting limits to 200, except Map Player Count, which remains limited to 1.
- Renamed ??? to ???1 and added ???2.
- Updated default Base Upgrade truck draw weights to reflect vanilla shop stock limits. Existing custom weights are preserved.
- Added settings to gradually reduce Base Upgrade truck draw weights as upgrades approach their maximum, reaching 0.25 times their normal weight by default.
- Tank, Runner, Jumper, Lifter, Launcher, Climber, Flyer, Tracker, and Ghost are now excluded from random assignment when any matching Base Upgrade is equal to or higher than the role's target.
- Magic cast by Mage using a held staff now lasts 1.3 times as long.
- Improved Stinker's cloud generation to prevent repeated errors for other players. Generated valuables are now worth $0 and cannot be moved.

## 4.4.3
- Disabled roles are now hidden from Role Guide, including for players connected to the host.
- Medic, Mage automatic recovery, Mechanic, and Electrician now announce when their recovery allowance runs out.
- Improved Bodyguard's handling of simultaneous damage and reduced incorrect damage bonuses when attacks overlap.
- Fixed Rescuer's configured revival health not being applied after its final use.
- Improved Medic and Mage healing-limit handling when several healing effects overlap.
- Expanded descriptions in Current Roles now update when the host changes role settings.
- Brawler is now excluded from random assignment when no melee weapon is available. Sniper requires a melee weapon or a gun; staffs do not count. Weapon-like valuables do not count as weapons for either role's assignment.
- Moved the default role HUD upward.
- Mage automatic recovery now restores up to 120 HP per stage, with a configurable limit.
- Reduced unnecessary processing during gameplay and while viewing the Roles menu.

## 4.4.2
- Mage spells and Trickster decoys can now be activated with configurable facial expressions.
- Trickster can no longer create another decoy while its current decoy is active.

## 4.4.1
- Mage-created valuables are now worth $0.

## 4.4.0
- Added Sniper. Its enemy damage decreases at close range and increases continuously with distance.
- Added Imitator. Grabbing an eligible teammate to transfer health copies that role for the rest of the stage.
- Added Avenger. It temporarily deals more damage to enemies after another player dies nearby, with spoken activation, refresh, and expiration notices.
- Added Brawler. Melee weapons deal more damage, while ranged weapons deal less damage.
- Added Rammer. Its Tumble Attack deals heavy damage to enemies while Tumble-related upgrades are disabled.
- Added Diver. Tumbling while continuing to look down lets it move below floors for a limited time, with a spoken countdown, a fatal timeout, and a cooldown after returning.
- Added ???.
- Mage now restores health over time after avoiding damage, with configurable recovery settings.
- Strengthened Trickster's decoy with a wider, longer, and more frequent lure, active repositioning, and a shorter cooldown when no enemies were attracted.
- Reorganized configuration sections and combined the Showcase and Support party-size guarantees into concise scaling settings.
- Roles from each player's five most recent assignments now use half weight for that player's next selection.
- Added configurable selection weights for each upgrade in the truck Base Upgrade draw.
- Added a configurable `change amount:weight` list for the truck Base Upgrade draw.
- The Base Upgrade draw animation is now shown to participants who have RoleShuffle installed.
- Added the installed RoleShuffle version to the Roles menu.

## 4.3.5
- Added clickable player entries to Current Roles. Selecting a player shows that role's description in English or Japanese.
- Electrician can now restore fully depleted melee weapons and staffs to a usable charge.
- Added an optional slot-style Base Upgrade draw during the truck preparation phase after each shop. Each draw affects one shared upgrade by -1, 0, +1, or +2, positive results can strengthen all supported upgrades at once, and the host announces the result.
- Added a Base Upgrades view to the Roles menu so current shared targets and accumulated truck-draw bonuses can be checked at any time.
- Expanded configurable Base Upgrade targets to 9999, except Map Player Count which remains limited to 1, and made out-of-range numeric values adjust to the nearest limit.
- Added a host setting for the maximum Base Upgrade target reachable through positive truck draws.
- Fixed Bomber carrying travel distance from inside the truck into the stage and rapidly placing stored grenades after leaving.

## 4.3.4
- Mage spells cast by participants now follow that player's own view direction instead of changes in the Semibot's head direction.
- Trickster now announces when the decoy is ready to use again.
- Mechanic now uses its stage repair allowance only when valuable price is actually restored.

## 4.3.3
- Added optional host-only compatibility with Elite Enemy Variants. Living Ninjas avoid attacks that specifically seek unseen players, Enhanced enemies can improve Vampire and Hunter rewards, and Gambler effects stay attached to the correct roulette target.
- Improved compatibility with Stage Flux. Mechanic works correctly with value-changing events, revival effects resolve in a consistent order, announcements no longer overlap, Enemy Purge does not activate Hunter rewards, and Dangerous Valuables respects Engineer protection.

## 4.3.2
- Prevented Influencer and Berserker scaling from lowering upgrades below their pre-role levels, and excluded Influencer from random assignment when none of its reachable configured targets exceed the current Base Upgrades.

## 4.3.1
- Changed Influencer so it remains a Showcase role but no longer counts toward Danger-role limits.
- Expanded RoleShuffle support and role-balance configuration ranges to sessions of up to 30 players.
- Base Upgrade values can now change with the run level.
- Prevented Tank, Runner, Jumper, Lifter, Launcher, Climber, Flyer, Tracker, and Ghost from being randomly assigned when a matching Base Upgrade target is already higher than the role's configured target.
- Rebalanced role assignment for larger groups with an extra-large Showcase and Support tier, party-size-scaled Danger and Hardship limits, longer personal Hardship cooldowns, and adjusted default role weights.
- Prevented Musician, Engineer, Electrician, and Rider from being randomly assigned when the stage has no vanilla object their ability can use.

## 4.3.0
- Added Rider. Rider increases vehicle impact damage against enemies and existing vehicle knockback against players.
- Added Influencer. Influencer gains stronger upgrades near more teammates, produces louder player noises, and periodically speaks audible TTS.
- Added Werewolf. Werewolf deals increased damage to other players when identified as the attacker.
- Added Berserker. Berserker gains stronger upgrades as remaining health becomes lower.
- Added Bodyguard. Bodyguard gains additional Health and absorbs part of nearby teammates' enemy damage while retaining at least 1 HP.
- Added configurable per-upgrade scaling for Influencer and Berserker.

## 4.2.3
- Improved Bomber's automatic grenade placement: Duct Taped Grenades no longer spawn directly under the player, and the oldest generated grenade is replaced when the active limit is reached.
- Increased Stinker's default cloud spacing and minimum safety distance from 1 m to 2 m.
- Fixed Medic healing errors and repeated Stinker warning messages.

## 4.2.2
- Added configurable role-selection rules that help larger teams receive a mix of event-focused and support roles. Hosts can also limit difficult role combinations, prevent the same role from repeating, and stop Jobless or Tuna from being assigned to the same player again too soon.
- Increased Hunter's default jackpot chance from 0.01% to 0.5%, Mechanic's repair speed from 1% to 2% per second, and Electrician's charge speed from 5% to 10% per second.
- Reduced Jobless's default selection weight from 100 to 20.
- Reduced Medic's default total healing limit from 200 HP to 150 HP per stage.

## 4.2.1
- Added an English and Japanese language toggle to the in-game Role Guide. English is shown first.

## 4.2.0
- Added Mechanic. Mechanic repairs damaged valuables while directly holding them, up to a configurable total per stage.
- Added Electrician. Electrician restores rechargeable item batteries while directly holding them, up to a configurable total per stage.
- Added Warden. Warden extends enemy stuns caused by their weapons, grenades, or held damaging objects.
- Added Ninja. Ninja suppresses personal movement, voice, and chat TTS noises and makes enemies take longer to visually recognize them.
- Added Executioner. Executioner deals increased direct attack damage to enemies that were already stunned before the hit.
- Added Trickster. Type `decoy` in chat to place a temporary fixed Scream Doll that attracts nearby enemies.
- Improved one-player role selection by excluding Tracker, Ghost, Medic, Jobless, and Rescuer from random assignment.
- Enhanced Gambler interactions with Gambit: green heals 50 HP, red deals 100 damage, black kills, and white fully heals and grants 5 Health upgrade levels for the current stage only.

## 4.1.4
- Fixed role upgrades occasionally overlapping when a stage began.
- Fixed dead players temporarily disappearing from role displays and having their role or purchased upgrades reapplied after revival.
- Fixed Rescuer range detection so it follows the dead player's current death head position.

## 4.1.3
- Added an English in-game Role Guide to the `Roles` page. The left column switches between current assignments and role descriptions, and current assignments are shown first whenever the page opens.
- In multiplayer, installed participants now see Role Guide values based on the host's settings.

## 4.1.2
- Older configuration files now keep compatible customized values, carry supported renamed settings forward, and create a one-time backup before being updated.
- Improved role handling when players leave, return, or join during a stage. Returning players regain their previous role, while new players receive a role, upgrades, an announcement, and a HUD entry.
- Engineer no longer affects Camera, Propane Tank, Snowmobile, Flashlight, Clown Doll, or Love Potion, preventing problems for players without RoleShuffle.

## 4.1.1
- Gambler's spent effect is now restored whenever the team completes an extraction. Gambler reacts when a valuable doubles or is destroyed and is notified when the effect is restored.

## 4.1.0
- Added configurable base upgrade levels for every supported vanilla player upgrade except Throw. Role-specific levels temporarily replace the matching base levels during a stage.
- Added Hunter. Hunter weapons use 75% of their normal battery, and enemies defeated by Hunter can drop additional orbs.
- Added Stinker. Stinker leaves sequential uranium clouds along the path traveled.
- Added Engineer. Effect valuables held by Engineer are prevented from activating whenever possible.
- Added Mage with five chat-selected vanilla spells: `star`, `gravity`, `roll`, `void`, and `laser`. Each spell has a configurable health cost and cannot be cast when it would kill the Mage.
- Added separate configurable revival health values for Phoenix and Rescuer.
- Redesigned the role HUD as a rotating one-column display that keeps the local player's role visible and supports large groups.
- Added a scrollable `Roles` page to the Escape menu. The HUD and role list now remove players who leave during a stage.
- Automatically generated Bomber grenades can now be held only by players currently assigned Bomber.
- Changed Gambler's losing outcome from reducing a valuable's price to destroying the valuable.

## 4.0.3
- Added `/roles`, allowing each player to check their own assigned role through in-game chat. Vanilla participants can use it when the host has RoleShuffle installed.
- Added Duct Taped Grenades to Bomber's automatic grenade selection.
- Added Gambler, which can randomly increase or decrease the value of a directly grabbed valuable.

## 4.0.2
- Base Health upgrades now remain active between stages.
- Improved Health upgrade changes when switching roles.

## 4.0.1
- Added Health +1 to every role except Tank.
- Phoenix and Rescuer now revive players with 25 HP by default.
- Improved documentation consistency.

## 4.0.0
- First Release
