# RoleShuffle 4.5.0 development

## Preservation and scope

- Branch: `feature/roleshuffle-4.5.0-overhaul`.
- Separate worktree: `RoleShuffle-4.5.0`; the original `StageRoles` checkout remains on `main`.
- Starting commit: `06f54e467a43f50f06b53c342dbf9d4b37b9a27e`.
- Keep the package name, assembly name and plugin GUID unchanged.
- Keep the existing Default profile and original package/ZIP intact. The user authorized deployment to the separate `RSO_TEST` profile on 2026-09-16.
- Original package DLL SHA-256: `6BD1FC2D8D21570A52BCC9E7E2250DF88F7D4540915E94A9B31CB1AB52A9A5F4`.
- Original ZIP SHA-256: `F275B92D50696D06236AF2B5ECA4B53A2040C7D3B4EDC22D48A8F8368CB5778F`.

## Initial playable slice

1. Tank and Runner multiply Base effective values and convert them to vanilla upgrade levels, capped at HP 4100, sprint speed 205 and stamina 2040. Base and configured minimums are preserved. Lifter keeps native display level 200 and fixes normal grip/rotation coefficients at six times vanilla level-1 values. Base 0–200 remains eligible. Copied roles and Superbot inherit these targets.
2. Jobless receives a starting grace period. Carry a different, positive-value valuable at least the configured distance and bring it into the truck to complete a contract. A contract pauses attrition and restores the worker's health. Completions are limited per player per stage; drops, death and teleports do not accumulate work.
3. King keeps the crown and grants temporary native Speed/Range +1 and up to safe Strength +1 to living allies within 8 m. Excludes Kings and never stacks. King bonuses are removed on leaving, death, departure or role change.
4. Stinker and Bomber remain movement-triggered automatic hazards. Neither has a manual pause, activation switch or charge-and-release operation. This is an explicit user requirement.
5. The host publishes a separate, versioned ability-status snapshot for installed participants. The local HUD and `/roles` show remaining resources. Existing assignment synchronization is preserved.

`General.OverhaulEnabled = false` restores legacy behavior for the five changed roles. Ability-status display has a separate local HUD toggle.

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
