# Porter — v4.6.0 build531

Date: 2026-10-10 JST. Branch: `version/4.6.0`.

## Approved behavior

- Hold one valuable alone for three continuous seconds outside delivery areas.
  Tiny, Small and Medium are allowed by default. Shop equipment is excluded.
- Store the original valuable with its existing value. Total original mass is
  limited to 15; there is no count cap or movement-speed penalty.
- Enemy-origin damage confirmed by an actual HP decrease spills the oldest
  stored valuable. Death releases all cargo. The existing conservative remote
  hit matching still declines ambiguous overlapping damage events.
- Enter the truck/extraction area to unload. Waiting scales with stored weight:
  full load takes 10 seconds, half load takes five. Leaving the area or an enemy
  spill resets the wait. Death, role change, disconnect and stage end bypass it.
- `CargoFull`, `TooHeavy`, `TooLarge`, `Stored`, `Unloading`, `Unloaded` and
  `CargoDropped` use the existing per-player notification queue. Repeated rejection
  of the same held item is suppressed. Completion requires every item returned.
- REPOConfig exposes capacity, allowed sizes, hold time, full-load unloading time
  and enemy-spill toggle, plus the usual selection switch and weight.
- Added to Support and the Cooperative preset. Imitator can copy Porter;
  Superbot excludes it to avoid automatically consuming held-item ability targets.

## Implementation and checks

Native inventory deactivation parks the original network object. Native owner
grab release runs before storage; return resets deactivation, teleports the
original and resumes previously enabled valuable effects. Failed returns retain
the cargo reference and weight for retry. The host makes storage decisions;
the resource HUD and role menu require RoleShuffle on the viewing client.

- Main Release build531: zero warnings/errors, automatic profile copying off.
  Build530's native-field check caught private `PhysGrabObject.photonView`
  access; build531 obtains the public PhotonView component instead.
- Original-game field access: 579 compiled references passed.
- Porter runtime/accounting/notice/rollback checks: 354 passed.
- Native Porter API, owner-release, storage and embedded-artwork checks: 21
  passed, covering 11 compiled game-method calls against the installed original DLL.
- Ability settings: 350; role settings: 343; combat migration: 23;
  Hunter migration: 72; Base settings: 128; saved Base adjustments: 248 passed.
- General localization: 13,662; Porter translation/placeholder/font checks: 156.
- HUD, growth, courier contracts, aura, healing locks and synchronization: 76,385.
- Base eligibility: 13,556. Combat runtime: 73, plus production event-routing checks.
- README: 99,910 decoded characters; CHANGELOG: 19,964. UTF-8 without BOM.
  The public guide lists 46 roles, masks both secrets, and includes all five
  Porter ability keys in English and Japanese. Selection keys use the shared rule.
- Porter artwork reviewed on light/dark backgrounds at 256, 64 and 25 pixels.
  Source, literal image-generation prompt, master and runtime hashes retained.
  Support background is `#24563E`. Public/full catalogs regenerated and checked
  for secret-art leakage. README thumbnail is pinned to asset commit `06ef1e7`.

## Deployment

- RSO_TEST deployed with the game stopped; all seven package files matched.
- Build/package/deployed DLL SHA-256:
  `fec93521c560cf42f97f72638c9ed0e8c730422e75236264e93e8290a11b2f4d`.
- Prior DLL backups: `tmp/build-backups/before530-20261010-082741`.
- Prior package/profile and deployment log:
  `tmp/deployments/before-build531-20261010-083738`.
- Existing profile configuration and earlier release ZIP were preserved.

Automated stand-ins and native API inspection do not establish live gameplay
or vanilla-guest transport behavior. No live game or host-only multiplayer test
was performed. Follow the live scenarios in `tools/PorterChecks/README.md`.
