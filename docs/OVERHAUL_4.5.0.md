# RoleShuffle 4.5.0 development

## Preservation and scope

- Branch: `feature/roleshuffle-4.5.0-overhaul`.
- Separate worktree: `RoleShuffle`; the original `StageRoles` checkout remains on `main`.
- Starting commit: `06f54e467a43f50f06b53c342dbf9d4b37b9a27e`.
- Keep the package name, assembly name and plugin GUID unchanged.
- Keep the existing Default profile and original package/ZIP intact. The user authorized deployment to the separate `RSO_TEST` profile on 2026-09-16.
- Original package DLL SHA-256: `6BD1FC2D8D21570A52BCC9E7E2250DF88F7D4540915E94A9B31CB1AB52A9A5F4`.
- Original ZIP SHA-256: `F275B92D50696D06236AF2B5ECA4B53A2040C7D3B4EDC22D48A8F8368CB5778F`.

## Standard v4.5.0 rules

1. Tank and Runner multiply Base effective values and convert them to vanilla upgrade levels, capped at HP 4100, sprint speed 205 and stamina 2040. Base and configured minimums are preserved. Lifter keeps native display level 200. In v4.5.1, heavy grip/rotation coefficients use each curve's exact maximum across vanilla levels 0–200: 143/24 (5.9583333333), reached at level 50. Light objects below mass 2 use level-1 values to reduce holding oscillation. Shop items (`ItemAttributes` on the held object), guns and melee weapons of any mass receive level-1 Strength as the local input before native overrides. Item blends then follow vanilla instead of the heavy-object target. The scoped `ItemMelee.GrabOverridesLogic` patch also evaluates the first holder's holding bonus at level 1, because melee computes minimum grip and torque independently of the shared physics loop. Scope restoration runs in a finalizer; swing force and cooldown calculations outside the holding scope remain native. Shared grabber Strength and upgrade levels remain unchanged. Base 50 is excluded from random Lifter assignment because it already reaches the heavy-object maximum. Copied roles and Superbot inherit these targets.
2. Courier (formerly Jobless) carries distinct positive-value valuables outside delivery areas, then delivers to the truck or extraction point. Unlimited deliveries grant configurable fixed healing and attrition exemption by vanilla size category; each valuable rewards each player once per stage. Tiny/Small/Medium/Big/Wide/Tall/VeryTall default to 10/25/50/100/100/100/100 HP and 30/30/60/90/90/90/120 seconds. Initial grace is 30 seconds; recovery clamps at maximum HP and shorter rewards cannot shorten remaining grace. Drops, death and teleports reset carrying progress.
3. King keeps the crown and, with v4.5.1 defaults, grants temporary native Speed/Range +2 and up to safe Strength +5 to living allies within 12 m. Excludes Kings and never stacks. King bonuses are removed on leaving, death, departure or role change. Schema 40 updates saved old defaults per key and preserves custom values, with an exact configuration backup before migration.
4. Stinker and Bomber remain movement-triggered automatic hazards. Neither has a manual pause, activation switch or charge-and-release operation. This is an explicit user requirement. In v4.5.1, Stinker's carrier waits one initialization frame plus 0.5 seconds before breaking in both solo and multiplayer, then rechecks authority, stage, owner, truck and safety-distance conditions.
5. The host publishes a separate, versioned ability-status snapshot for installed participants. The local resource HUD shows remaining resources in one vertical column. The role list has no auxiliary status line; `/roles` announces only the assigned role name. Existing assignment synchronization is preserved.

These five role updates are the standard v4.5.0 behavior. Schema 37 removes the old mode switch, unused Lifter Strength level and obsolete ability-status line setting after backing up the configuration. Role selection and current tuning are preserved. Resource display has a separate local HUD toggle. The older build remains available in the preserved checkout and deployment backups.

## Clearer Courier guide — UI build 480

- Reorganized the guide in all 14 languages into the role's benefit, two delivery steps, one reward row per native size, HP-loss risk and reward limits. The generic fallback now describes delivery and recovery as well.
- Replaced ambiguous exemption wording with a pause of Courier's automatic HP loss. The guide explicitly states that attacks, falls and other damage still apply. It explains uninterrupted carrying outside delivery areas, both destinations, one reward per item per player per stage, maximum-health clamping and taking the longer pause instead of adding durations.
- Updated the English/Japanese package summary and both role-list PDFs. Each PDF remains 10 pages; only page 8 changed. Both rendered pages were visually checked, with the existing font sizes retained. Public secret-role masking remains intact.
- Description-only update: gameplay rules and configuration values are unchanged. Verification: 12,079 localization checks and all required Japanese/CJK glyph checks pass. Build 480 has zero warnings/errors; 473 compiled game-field references pass the installed-game audit. Live in-game text layout has not been inspected for this build.

## Lifter light-object handling — UI build 479

- Light objects (native Rigidbody mass below 2) now use vanilla Strength level-1 grip and rotation coefficients: 1.1812987013. Heavy objects keep the sixfold level-1 value, 7.1607272727. Native upgrade display remains 200; Base levels do not change either fixed target. Copies and Superbot inherit the same rule.
- Reduced the light-object fixed value directly. The existing two-blend host patch remains unchanged; no extra force patch, damping, Rigidbody mutation or settings are introduced. Special overrides, authority checks and role cleanup retain their existing behavior. Eligibility follows the heavy-object benefit, so Base 0–200 stays eligible.
- Numerical regressions use vanilla's free-hold spring/torque equations and 50 Hz stepping. The prior light coefficients produce persistent movement in the selected small-object cases, while level-1 values settle without custom damping. These simulations do not establish actual Unity collision, multiplayer or gameplay outcomes; in-game confirmation remains pending.
- Verification: 31,487 Lifter math/installed-game IL/emitted-IL/runtime checks, including mass-boundary cases and Lifter-to-Runner restoration; 75,856 existing role/runtime/sync checks; 12,079 localization checks. Build 479 has zero warnings/errors; 473 compiled game-field references pass against the installed game assembly.
- All 14 guide translations and English/Japanese release documents describe the light/heavy distinction. CJK font coverage passes. Both role-list PDFs remain 10 pages; only page 2 changed in each edition, and both renders were visually checked. Public secret-role masking remains intact.

## Courier delivery rewards — UI build 478

- Adopted the user's concept A: Courier / 配送員, with a parcel, delivery cap and teal hexagonal emblem. The source and 256 px transparent runtime PNG are tracked with hashes and the generation prompt. Role ID 13 is unchanged; `/role Courier` and the legacy `/role Jobless` both select it.
- Both native truck and extraction room volumes qualify based on the valuable's location. Carry progress is counted only outside those areas. A release hook refreshes the native room check before discarding progress, covering delivery and release between periodic scans.
- Removed the per-stage completion cap and its remaining-budget HUD entry. Distinct valuables can be delivered without a count limit; each valuable rewards each player once per stage, including after revival/rejoining. The existing 5 m carrying requirement remains.
- Healing is fixed HP by category: Tiny 10, Small 25, Medium 50, Big/Wide/Tall/VeryTall 100. Grace defaults are 30/30/60/90/90/90/120 seconds respectively. All seven HP and grace values are independently configurable. Native owner-side healing clamps at maximum HP; a short grace reward cannot reduce a longer active grace period. These rewards do not alter pending Medic/Mage reservations.
- Schema 39 backs up existing settings, moves Jobless keys to Courier, retains the old shared grace as initial grace, and removes obsolete shared healing and completion-limit keys. Existing Courier keys take priority. The initial grace default remains 30 seconds.
- Updated all 14 language catalogs and checked font coverage; refreshed only the three CJK subsets needing new glyphs. Detailed and public Japanese role-list PDFs remain 10 pages. All seven changed rendered pages were visually checked; the public edition still hides secret names, descriptions and artwork.
- Verification: 75,856 runtime/rules/sync checks; 298 role settings/migration, 128 Base settings and 248 save-adjustment checks; 12,079 localization checks; 2,704 healing-budget/notification/weight regressions; 26,659 Lifter math/IL/runtime regressions. Build 478 has zero warnings/errors; 473 compiled game-field references pass the installed-game audit. All 43 emblems are embedded, and Courier's embedded bytes match the runtime PNG.
- RSO_TEST deployment and live game verification remain deferred while the user continues playing. This build retains the Lifter restoration repair from build 476.

## Jobless full healing — UI build 477

- Each completed contract now restores the worker to maximum HP, including upgraded health. The native owner-side heal clamps at maximum; sending maximum HP avoids under-healing an unmodded guest when the host's current HP is stale. The reward is sent once per completion even while a capped Medic/Mage heal is pending; existing healing reservations are neither refunded nor overwritten. Contract counts, grace duration and death handling are unchanged.
- Removed `Jobless.ContractHeal` from configuration and UI. Schema 38 preserves an exact backup before removing it, including previously disabled (`0`) healing. All other Jobless settings remain intact.
- Updated the role guide in all 14 languages, English/Japanese release documents, and detailed/public Japanese role-list PDFs. Public hidden-role masking is retained.
- Verification: 75,644 runtime/rules/sync checks; 292 role settings/migration checks, 128 Base settings and 248 save-adjustment checks; 12,079 localization checks; 2,704 existing healing-budget/notification/weight checks. All changed text is covered by existing bundled fonts.
- Release build 477 has zero warnings/errors; 467 compiled game-field references pass the installed-game audit. Both PDFs remain 10 pages; only the Jobless page changed, and both rendered pages were visually checked.
- RSO_TEST deployment and live gameplay verification remain deferred at the user's request to continue playing. This build also includes build 476's Strength restoration fix.

## Strength restoration — UI build 476

- Report: a forced Lifter → Runner change left heavy objects easy to lift; the local log confirms both players changed roles. Live internal Strength values were not captured, so the precise source of their divergence remains unconfirmed.
- The installed game's upgrade method adds `0.2 * level delta` to a separate `PhysGrabber.grabStrength` cache. A stale contribution survives level restoration. Absolute role/Base resets now reconcile the host's cached force to `1 + 0.2 * confirmed Strength level`, including zero-delta resets. This covers the host's replicas of guest avatars. Configured/manual/truck Base levels remain in the normal target calculation; temporary player/object overrides remain untouched. Ordinary additive King/event grants are unchanged.
- Regression: a simulated stale Lv200 contribution fails against the old production setter. The repaired setter and production physics patch pass 26,659 checks, including repeated Lifter/Superbot → Runner changes at Base 0–200, light/heavy grip and rotation, no-op resets, stage cleanup, other players, authority, overrides and simulated native guest RPCs. Installed IL verifies the additive cache operation. This is not a live multiplayer test.
- Existing checks: 75,596 growth/contract/aura/resource/sync checks and 13,541 production target/eligibility checks pass. Release build 476 has zero warnings/errors; 464 compiled game-field references pass against the installed game.
- In-game acceptance is pending. The user chose to continue playing, so RSO_TEST remains on build 475 until deployment is requested after the game exits.

## Standard role integration — UI build 475

- Removed the optional mode and all runtime branches to the old Tank/Runner/Lifter/Jobless/King behavior. Lifter's unused level setting and obsolete status-line setting are removed by schema 37; an exact pre-migration backup is retained. Tests cover both saved mode-switch values, preserved tuning/selection/HUD preferences and repeat migration.
- Release README/CHANGELOG and both Japanese role-list PDFs describe the standard v4.5.0 rules. The public PDF still masks both secret roles; all 20 PDF pages were rendered and inspected.
- Verification: 75,596 growth/contract/aura/resource/sync checks; 13,541 production target/eligibility checks; 7,354 Lifter physics/IL checks; 12,079 localization checks; 280 role settings/migration, 128 Base settings and 248 save-adjustment checks. Build 475 has zero warnings/errors; 461 compiled game-field references pass against the installed game assembly.
- This integration has not been exercised in live Unity or multiplayer. Older verification entries below are historical and may describe superseded settings.

## Host-only constraint

Vanilla `HurtCollider.PlayerHurt` applies damage only on the target player's owning client. Changing a host-side uranium collider does not establish immunity for an unmodified guest. Stinker immunity and additional damage-based role passives are therefore deferred until a host-only implementation is demonstrated. No immunity is advertised in this slice.

## Verification

Build and deterministic checks are required before packaging. Live host/guest, revival, reconnect, stage transitions, high Base Upgrades, Stage Flux and Elite Enemy Variants scenarios remain required before release. Record actual verification results below; do not treat a compiled build as multiplayer validation.

### Automated results — 2026-09-15

- Release build: version 4.5.0, UI build 458, zero warnings/errors.
- OverhaulChecks: 8,089 checks, including production contract/aura runtime, capped remote healing reservations, growth, malformed/stale snapshots and shutdown without GameManager.
- Production eligibility/target methods: 10,312 checks, including legacy behavior and high/capped Base Upgrades.
- LocalizationChecks: 11,203 checks across all 14 languages; new host parameters remain intact.
- Regenerated only the Korean, Simplified Chinese and Traditional Chinese font subsets: added coverage for 2, 10 and 12 missing characters respectively. Verified catalog coverage and the new Japanese text against its source font.
- RoleSettingsChecks: 241 role settings, 128 Base Upgrade settings and 248 save-adjustment checks.
- ReviewChecks: 2,704 existing budget/recovery/weight checks.
- OptimizationChecks: 108 existing assignment/guide synchronization assertions.
- UtilityChecks: 6,654 existing history/privacy/HUD geometry assertions. These do not render the new ability line.
- ExpressionChecks: 106 checks; StinkerChecks: 138 coroutine checks.
- Compiled field-access audit: 437 game field references verified against the installed game assembly.
- English/Japanese release README: 99,309 characters; CHANGELOG: 15,098 characters. Both pass the 100,000-character limit and UTF-8 check.
- Original `StageRoles` is clean on the starting `main` commit; original DLL and ZIP hashes match the preservation record.
- StinkerRoleRuntime, BomberRoleRuntime and LifecyclePatches have no source changes from the branch point.

### Pending live acceptance

The new DLL/ZIP is a development build, deployed only to the user-authorized `RSO_TEST` profile. Actual HUD layout, host-only multiplayer, live transport timing, Stage Flux and Elite Enemy Variants combinations have not been exercised. Use [the acceptance checklist](../tools/OverhaulChecks/README.md) in the isolated test profile before a public release.

### Lobby loading resolved — 2026-09-16, build 459

- The user reproduced a lobby loading stall with build 458. A temporary local diagnostic plugin observed Photon remaining in `Disconnecting` for at least 50 seconds, with the message queue enabled and the password page not yet created. The game's `NetworkConnect.CreateLobby` coroutine waits for disconnection before proceeding.
- Ability-status cleanup previously accessed Photon even before this instance had published anything. It now records the room it published to and avoids all Photon access when there is nothing to remove. Cleanup also checks room identity and current host authority, without depending on a live GameManager.
- Added a regression that fails against the previous implementation when startup cleanup touches Photon. The updated OverhaulChecks suite passes 8,095 checks, including repeated cleanup, shutdown, room changes and host migration.
- Release build 459: zero warnings/errors. Deployed to RSO_TEST with previous DLLs/logs backed up and configuration preserved. No gameplay role changes.
- The user confirmed the loading stall is resolved with build 459. The retry log records disconnection completing, the password page appearing, connection to the master server, successful room creation/join in region `jp`, and the lobby unlocking. This verifies this host's lobby-entry recovery; broader multiplayer/stage acceptance remains pending.
- The successful log and temporary diagnostic DLL were archived locally. The diagnostic DLL was renamed out of the `.dll` extension so it is not loaded on the next launch; the running game was not stopped. Diagnostics remain excluded from the release package and Git.

### Lifter Strength correction — 2026-09-17, build 462

- Verified the installed R.E.P.O. 0.4.4.3 `PhysGrabObject` code: raw grab Strength is `s = 1 + 0.2 * level`; normal translational force uses `lerp(s, s / (1 + min(s, 30)), min((s - 1) / d, 0.9))`, with `d = 7` below mass 2 and `d = 20` otherwise. Rotation uses the same blend but substitutes `s / (1 + s)` for the reduced value. These curves are not monotonic. For example, levels 20 to 25 reduce light-object force from about 2.619 to 2.327; levels 50 to 55 also reduce heavy-object force.
- The requested level remains `max(configured role level, Base + bonus)`, capped at 200. The guaranteed floor is `max(Base, configured role level)`: the user's setting takes priority over penalties up to this level. Search downward from the requested limit to find a level above the floor whose corrected translation and rotation are no worse than the floor in either weight category and better in at least one. Otherwise keep the floor. Vanilla physics and temporary object/player overrides are unchanged.
- Default examples: Base 15, 20 and 24 all receive configured Strength 25 and remain eligible; Base 25 and 50 retain Base and exclude Lifter; Base 90 selects 95. With Base 70, a configured minimum of 200 is honored even though rotation weakens. With minimum 25 and bonus 130, the same target of 200 is rejected because only the extra growth is penalty-limited. Manual/draw adjustments are included in Base. Copied Lifter and Superbot inherit this rule; legacy mode and other roles are unchanged.
- OverhaulChecks: 23,186 checks, including all Base levels 0–200 across 25 role-target/bonus combinations. Production eligibility/target checks: 10,358, including configured-floor priority, zero bonus, exact floor, high minimum versus high bonus, Superbot and legacy behavior. In-game carry feel remains unverified.
- Build 462: zero warnings/errors, 11,239 localization checks, and 437 verified game field references. Font coverage checked after updating the 14 language descriptions. Package and ZIP validated with the original StageRoles DLL/ZIP preserved.
- Deployed all five matching package files to RSO_TEST, with build 461 and profile configuration backed up. Configuration is unchanged. Live carrying and rotation tests have not been performed for build 462.

### Effective-value multipliers — build 463

- Defaults: Tank HealthMultiplier 1.5; Runner SpeedMultiplier/StaminaMultiplier 1.5; Lifter StrengthMultiplier 1.5. Vanilla upgrade grants remain host-authoritative; no custom force multiplier or guest plugin is required for the upgrade itself.
- Verified vanilla HP = 100 + 20 × level; sprint speed = 5 + level; stamina capacity = 40 + 10 × level. The installed `level0` PlayerController (path ID 3228) serializes SprintSpeed 5 and EnergyStart 40 at offsets 164/224. C# field initializers (1/100) are not the gameplay defaults. PunManager applies +20 HP, +1 sprint speed and +10 stamina per upgrade. Other mods' overrides and temporary states are outside this vanilla conversion.
- Enumerating levels 0–200 gives maxima 4100/205/2040 at level 200. Light grip peaks at level 200 (5.29032258); heavy grip peaks at level 50 (5.95833333). Their common rounded-up cap is 6. Rotation coefficients remain safety constraints, not advertised final torque multipliers.
- Tank/Runner target `min(Base effective value × multiplier, effective growth cap)`, rounded up to a supported level without crossing the cap. A non-level-aligned cap uses the highest level below it. Existing Base and configured role minimums have priority over lower user growth caps. The independent upgrade level limit remains 200.
- Lifter starts at `max(Base, role minimum)`. It searches levels up to 200 in ascending order, rejecting extra levels that weaken light/heavy grip or rotation relative to that floor or cross the effective cap. The first candidate meeting both multiplied Base grip goals wins. Otherwise choose the candidate maximizing the weaker of the two relative grip gains, with ties retaining the lower level. Multiplier 1 disables extra growth. The configured minimum can still lower force compared with Base, as explicitly requested.
- Example defaults: Tank Base 100 → 153 (2100 → 3160 HP); Runner Speed Base 6 → 12 (11 → 17), Stamina Base 46 → 71 (500 → 750); Lifter Base 0 → 25, 25 → 200, 50 → 50 (excluded), 70 → 70 (rotation blocks growth), 90 → 156. Effective targets are not always attainable, particularly Lifter; integer levels can overshoot a multiplier goal within the effective cap. Runner stamina is capacity, not guaranteed sprint duration: speed also raises sprint drain.
- Configuration schema 33 removes the four obsolete Base*Bonus entries and creates a separate `.pre-v4.5.0-multipliers.bak` backup for schema-32 upgrades. Existing minima and any already-set new multiplier values are retained. Descriptions updated in all 14 languages.
- Build 463: zero warnings/errors; 104,594 overhaul checks; 10,335 production target/eligibility checks; 246 role settings + 128 Base settings + 248 save-adjustment checks; 11,239 localization checks; 437 installed-game field references. Updated CJK subsets and verified Japanese coverage. Live multiplayer/carrying checks remain unperformed for this build.
- Packaged and deployed all five files to RSO_TEST with hash verification. DLL SHA-256: `AAE189A6C1EB6D1A240A106A642743A31A4FF2581FB89E0723D48F6F7CE136EA`. Build 462 and the profile settings were backed up; settings migration runs on the next launch. The game was not launched. Original StageRoles DLL/ZIP hashes remain unchanged.

### King upgrade support — UI build 464

- Replaces the healing aura with configurable Speed, Range and Strength upgrade bonuses (default +1 each; radius 8 m). Health and Stamina are excluded because vanilla changes also heal/hurt or refill stamina on the owning client. This keeps range crossings free of those resource effects with host-only installation.
- Strength selects the highest non-weakening level within the requested bonus; tests cover both mass classes, grip and rotation across levels 0–200. Existing levels are preserved, new grants stop at 200.
- A separate host ledger removes only its own additions. Absolute role resets invalidate replaced contributions; dynamic roles record baselines without King bonuses. Native RPCs synchronize the changes to guests. No manual ability activation.
- HUD reports supported allies. Metric 19 is appended without changing existing metric IDs; old healing metric 7 remains decodable. Fourteen locale catalogs updated.
- Schema 34 removes King's healing settings, carries HealRadius to UpgradeRadius, preserves explicit new settings and backs up schema-33 configs as `.pre-v4.5.0-king-upgrades.bak`.
- Deterministic checks include range boundaries, repeated ticks, purchases, role resets, dynamic baselines, caps, deaths, rejoining, overlapping Kings, guest authority, partial RPC failure and stage cleanup. Live multiplayer verification remains outstanding.

- Verification: Release build 464 passed with zero warnings/errors; OverhaulChecks 106,224; localization 11,275; role/settings migration 250, Base settings 128, save adjustments 248; all 437 compiled game field references valid. CJK font coverage verified.
- Deployed to RSO_TEST at 2026-09-17 11:38 JST. Previous build 463, configuration and profile metadata saved to `tmp/deployments/RSO_TEST-before-build464-20260917-113817`. All five deployed package files matched their sources; config was unchanged. Schema 34 applies on next startup. Game was not launched. DLL SHA-256: `1E801437AF95D7A3F8B2F9331C7248D43E1D593E1A7B1DCECD30ACD6B5D22F1D`.

### Growth role eligibility at effective caps — UI build 465

- Random selection checks Base effective values before role minimums and multipliers. Tank uses HP; Runner is excluded if either sprint speed or stamina capacity reaches its cap; Lifter compares both mass classes and excludes when either grip value reaches its cap. Equality and values above a custom cap are excluded.
- The common candidate filter covers stage starts, late joins and every history/uniqueness/risk relaxation pass. Forced test assignments retain the existing targets and minimum priority. Roles whose upgrades do not grow remain excluded even below the effective cap.
- Default limits remain HP 4100, sprint speed 205, stamina 2040 and grip 6. Grip 6 is rounded up from the level-0–200 maximum 5.958333; it is not reached by vanilla upgrades in that range, so Lifter's no-gain/penalty constraints still matter. Custom grip caps use the actual non-monotonic curve, not an upgrade-level threshold.
- Role descriptions in all 14 languages state the exclusion. No configuration keys or schema changes.

- Verification: build 465 passed with zero warnings/errors; 11,160 production target/eligibility checks, 106,224 overhaul checks and 11,347 localization checks passed. All 437 compiled game field references validated against the installed game. CJK glyph coverage verified.
- Deployed to RSO_TEST at 2026-09-17 12:46 JST. Build 464, config and profile metadata backed up to `tmp/deployments/RSO_TEST-before-build465-20260917-124645`; all five package files hash-verified. Config unchanged; original StageRoles DLL/ZIP unchanged. Game not launched; live multiplayer remains unverified. DLL SHA-256: `BFFD6E3F2C2FB51D6EFEB0B5ED9B0E6537659743311DE534820F84E585DBE935`.

### Fixed Lifter Strength — 2026-09-19, UI build 466

- User selected native Strength level 200 only. Lifter no longer uses an effective-value multiplier, growth cap, level search or custom physics correction. Base 0–199 targets 200; Base 200 is excluded from all random candidate paths. Forced assignments still target 200. Copied Lifter and Superbot inherit the same target, with normal stage cleanup and vanilla upgrade RPCs.
- Vanilla 0.4.4.3 at level 200: grip coefficient 5.29032258 and rotation coefficient 4.97857143 for both mass classes. Heavy grip decreases relative to Base 32–68; the heavy rotation coefficient decreases relative to Base 28–72. Rotation coefficients are intermediate values, not final torque. This behavior is intentionally retained; no compensation is applied.
- Schema 35 removes obsolete Lifter StrengthMultiplier and MaximumEffectiveStrength. Schema-34 configs are copied to `.pre-v4.5.0-lifter-200.bak` first. StrengthUpgradeLevels is preserved for legacy mode; overhaul ignores it. Fourteen language descriptions and player documentation reflect the fixed target.
- Verification: build 466 completed with zero warnings/errors; 14,346 production target/eligibility checks, 74,061 overhaul/King/runtime checks, 11,863 localization checks, 255 role settings checks, 128 Base settings checks and 248 save-adjustment checks passed. All 437 compiled game field references matched the installed game. Updated Chinese font subsets and checked CJK/Japanese coverage.
- Deployed all five package files to RSO_TEST at 2026-09-19 15:27 JST with hash verification. Build 465, config and metadata backed up to `tmp/deployments/RSO_TEST-before-build466-20260919-152746`. Config unchanged; schema 35 migrates on next launch. Original StageRoles DLL/ZIP unchanged. Game not launched; live multiplayer remains unverified. DLL SHA-256: `15B25BA7F110E88CCDF5E91EF465BEA2C263B49A4F2021C2FA35B37424B7789E`.

### Lifter fixed effective strength — UI build 467

- Supersedes build 466 native-only Strength 200. The reference is upgrade level 1, not unupgraded level 0. Normal grip and rotation coefficients are independently fixed at six times their vanilla level-1 values: 7.08779220779 below mass 2; 7.16072727273 at mass 2 or above. Base does not multiply this target. Rotation coefficients are intermediate inputs, not a guarantee of final torque x6.
- Native Strength remains 200 for the existing upgrade display/client input. A host-only correction replaces precisely the two penalty blends inside `PhysGrabObject.PhysicsGrabbingManipulation`, per grabber. Other grabbers remain vanilla. Only active, living Lifter/copy/Superbot capabilities qualify. Normal cleanup, disabled/legacy mode and guest authority stop correction; tumbling, explicit temporary overrides and disabled extra strength retain priority.
- All Base levels 0–200 fall below the new effective target, so Base 200 is eligible again. Unsupported game IL disables the physics feature and excludes random Lifter rather than partially rewriting physics or disabling the entire mod. No new config keys or migration schema changes.
- Verification: build 467 passed with zero warnings/errors; 7,356 Lifter checks covered installed-game IL contracts, actual transpiler output executed as a fixture, all supported levels and runtime gates. Also passed 14,347 target/eligibility checks, 74,061 overhaul checks, 11,899 localization checks and 443 compiled game-field access checks. Updated and verified CJK, European and Japanese font coverage. These tests do not replace Unity or live multiplayer validation.
- Deployed all five package files to RSO_TEST at 2026-09-19 19:36 JST; hashes matched. Build 466, config and metadata backed up to `tmp/deployments/RSO_TEST-before-build467-20260919-193629`. Config and original StageRoles artifacts unchanged. Game not launched. DLL SHA-256: `2699EC8F42BEB156A32EE8A782C9331D43A52D1404F6C0980F091E309433F517`.

### Clearer role guide — UI build 468

- Rewrote the five overhaul role entries in all 14 languages. Each starts with its gameplay benefit, followed by the needed settings or actions in a separate paragraph. Tank shows its guaranteed minimum as HP instead of an upgrade level. Lifter explains lifting/turning and fixed grip strength; Jobless describes the hauling job before rewards and the HP-loss risk; King leads with nearby ally support.
- Removed conversion algorithms, physics coefficients and draw-filter details from the player guide. Full technical rules remain in the package README and development record. Earlier translation templates remain available for descriptions received from older hosts. Role mechanics and settings are unchanged.
- Verification: 12,079 localization checks, 108 optimization checks and 69 utility runtime checks passed. Updated CJK font subsets and checked all catalog glyphs plus the new Japanese text. Build 468 passed with zero warnings/errors. In-game guide rendering has not been visually verified for this build.
- Deployed all five matching package files to RSO_TEST at 2026-09-20 01:29 JST. Build 467, config and metadata are preserved in `tmp/deployments/RSO_TEST-before-build468-20260920-012904`. Config and original StageRoles artifacts unchanged; game not launched. DLL SHA-256: `F657D8F42FE05A9F26F0E21D8E632EA80B2E4B842D0C33819CA1F2E06B377B5A`.

### Personal ability resource HUD — UI build 470

- Adds a separate top-left HUD, using the live vanilla HealthUI number font with large remaining amounts, smaller limits, localized labels and existing role emblems. Targets Medic healing, Rescuer revives, Phoenix revival, Mage recovery, Mechanic repair, Electrician charge, Gambler wagers and overhaul Jobless contracts. Copies use their effective ability; Superbot displays its seven budgets together. Zero stays visible in red. HP and percent units are explicit.
- Reads the existing host-authoritative ability snapshot, preserving its identity/role matching and stale-data checks. No new RPC or packet format. Visibility does not depend on the role-list toggle, page or pinning. Hides outside gameplay, during death/spectating, when native HUD is hidden, or when no current snapshot exists. Scene-owned UI is recreated with the native canvas; game HUD logic is never cloned. Installed guests can display the HUD; vanilla guests retain gameplay effects without custom UI.
- Adds local `HUD.ResourceHudEnabled` (true), `ResourceHudScalePercent` (100), `ResourceHudOffsetX` (16) and `ResourceHudOffsetY` (24). Offsets use a 540-height reference. Layout fits all rows and reserves bottom space for vanilla HP/stamina. Cooldowns/progress stay below the role heading, with budgets returning there when the separate HUD is disabled. Fractional budgets round up, so a usable fraction does not appear exhausted. No gameplay budget changes.
- Verification: 75,568 overhaul/resource/layout/synchronization checks; 255 role settings, 128 Base settings and 248 save-adjustment checks passed. Build 470 has zero warnings/errors; all 454 compiled game field references are accessible in the installed game. Layout math covers 640x480 through 3840x2160, ultrawide, 1–8 resources, 50–200% scale and offset extremes. Native HealthUI/EnergyUI implementation inspected. Unity rendering and live multiplayer remain unverified.
- Packaged and deployed five hash-matched files to RSO_TEST at 2026-09-20 01:56 JST. Build 468, config and metadata preserved in `tmp/deployments/RSO_TEST-before-build470-20260920-015612`; config and original StageRoles artifacts unchanged. Game not launched. DLL SHA-256: `0CA6581909A0B29606D46065E1FFECB68B508A23C103D40509FA76475A21B72D`.

### Match native HUD presentation — UI build 471

- User's in-game screenshot showed build 470 overlapping stamina and rendering too small. The actual `Game Hud` rect is 680x370, not a full-screen 540-height coordinate space. Installed level0 assets confirm Health at (28.7, 0), Energy at (28.7, -31), native text boxes 120x50, Teko-VariableFont_wght SDF 1 at 40/20 points, bold style, and a 25x25 icon inset by 28.1. Removed the extra canvas-height scaling and the incorrect assumption that vanilla HP/stamina occupy the bottom.
- New rows start one native row below stamina, aligned with native icons and numbers. Font, base material, size, weight, alignment, spacing, margins and padding come from live Health/HealthMax TMP components. The maximum follows native digit-dependent offsets and colored slash formatting. Rest positions come from public SemiUI.showPosition to avoid baking an active spring/hide animation into the layout. No native behavior components are cloned.
- Removed captions and role emblems from this compact HUD. Healing/charge use the live native Plus/Zap sprites; other budgets use single-color vector symbols: revival/person-arrow, Phoenix/bird, recovery/cross-star, repair/wrench, wagers/die, contracts/clipboard. No game font/sprite assets are redistributed. Four rows per column keep Superbot's seven budgets at native scale within the top portion of Game Hud; custom scaling still fits the available area.
- Schema 36 changes only old default offsets 16/24 to 0/0. Zero now means aligned immediately below stamina, with additional offsets in native HUD units. Custom offsets, scale and visibility remain. Schema-35 files are backed up as `.pre-v4.5.0-native-hud.bak` on startup.
- Verification: 75,585 overhaul/layout/resource/sync checks, 264 role settings/migration checks, 128 Base settings and 248 save checks passed. Build 471: zero warnings/errors; 461 game-field references valid against the installed non-publicized assembly. Reviewed a local preview rendered from the production vector meshes and installed Teko font; this is not live Unity validation.
- Deployed five hash-matched files to RSO_TEST at 2026-09-20 02:20 JST. Build 470/config/metadata saved in `tmp/deployments/RSO_TEST-before-build471-20260920-022055`. Configuration unchanged during deployment; migration runs on next launch. Original StageRoles artifacts preserved. Game not launched; corrected live rendering remains to be confirmed. DLL SHA-256: `3A62E01218309BD4DC5755C70C376EF03199187A84CDDDA1D98592071E9A6AB0`.

### Resource HUD visual polish — UI build 472

- User supplied a build-471 game screenshot and requested unit removal, cleaner icons and another color; explicitly selected cyan. Removed the percent suffix from repair/charge and use plain remaining/limit numbers for every resource. Icons, numbers and slash share cyan (#61D6FF); depleted resources remain red. Native HP/stamina, typography, size and row placement are unchanged.
- Replaced coarse icon construction with rounded silhouettes, curved Phoenix wings and a continuous open wrench jaw. Concave outlines use ear clipping rather than triangle fans. A 0.28-unit transparent fringe softens vector edges; native Plus/Zap sprites are still reused. Geometry remains cached and does not add a shader, texture dependency or gameplay behavior.
- Verification: 75,596 resource/layout/overhaul/sync checks, including transparent wrench opening, solid shank, die interior and feathered bounds. Local rendered preview inspected at native icon size and enlarged. Build 472 has zero warnings/errors; 461 compiled game-field references remain valid. Live Unity rendering of this revision is unverified.
- Packaged/deployed all five matching files to RSO_TEST at 2026-09-20 02:35 JST. Build 471, config and metadata preserved in `tmp/deployments/RSO_TEST-before-build472-20260920-023546`; config and original StageRoles artifacts unchanged. Game not launched. DLL SHA-256: `758337B857AEEF02CFA52BDA0FF99DD66D36CAB729D4FE3E00894BFF41BA9FB1`.
