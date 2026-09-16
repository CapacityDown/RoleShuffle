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

1. Tank, Runner and Lifter use the greater of their configured minimum and Base Upgrade plus their configured bonus, capped at 200. Runner remains eligible if either of its two upgrades provides a benefit. At the cap, a role with no remaining benefit is excluded.
2. Jobless receives a starting grace period. Carry a different, positive-value valuable at least the configured distance and bring it into the truck to complete a contract. A contract pauses attrition and restores the worker's health. Completions are limited per player per stage; drops, death and teleports do not accumulate work.
3. King keeps the crown and provides a small, budgeted healing aura to living teammates, excluding itself. Healing shares the existing recipient lock with Medic and Mage.
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

### Lobby loading investigation — 2026-09-16, build 459

- The user reproduced a lobby loading stall with build 458. A temporary local diagnostic plugin observed Photon remaining in `Disconnecting` for at least 50 seconds, with the message queue enabled and the password page not yet created. The game's `NetworkConnect.CreateLobby` coroutine waits for disconnection before proceeding.
- Ability-status cleanup previously accessed Photon even before this instance had published anything. It now records the room it published to and avoids all Photon access when there is nothing to remove. Cleanup also checks room identity and current host authority, without depending on a live GameManager.
- Added a regression that fails against the previous implementation when startup cleanup touches Photon. The updated OverhaulChecks suite passes 8,095 checks, including repeated cleanup, shutdown, room changes and host migration.
- Release build 459: zero warnings/errors. Deployed to RSO_TEST with previous DLLs/logs backed up and configuration preserved. No gameplay role changes.
- The loading stall's root cause and recovery still require the user's retry; the cleanup regression alone does not prove the full live issue is resolved. Temporary diagnostics remain in RSO_TEST for that retry and are excluded from the release package and Git.
