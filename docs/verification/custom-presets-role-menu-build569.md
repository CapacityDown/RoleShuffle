# Custom presets and role-test menu — v5.0.0 build569

Date: 2026-11-02 (JST). Source base: 66450b7de3605e7518327f92452ddca8fe89e03e.

## Behavior

- Roles > Role Settings > Presets has exactly three local custom slots. Save
  captures all role ON/OFF switches, including secrets, without applying them.
  Applying restores the switches through the existing REPOConfig entries and
  one-save rollback path. Weights, abilities, balance, Base Upgrades, HUD and
  existing stage assignments are unchanged, matching the built-in presets.
- Occupied slots require an explicit overwrite confirmation in the menu.
  Empty slots differ from a saved all-OFF configuration. Slots persist across
  restarts in the profile beside the main config as *.presets.json. Schema 1
  stores stable role names rather than reorderable numbers. Unrecognized future
  roles never alias another role. Missing/new roles remain OFF when applied.
- Writes use a flushed temporary file and atomic replacement; the previous file
  is retained as .bak. Memory changes only after success. Unreadable, oversized
  or unsupported-version files are preserved and not silently overwritten.
- Roles > Tools > Change player roles is visible only with Testing.Enabled and
  host/solo authority. Actual changes additionally require ready stage roles.
  The service rechecks permission, testing mode, stage and exact player identity
  at each action. The UI lists authoritative assignments, never synthetic HUD
  preview players. Player numbers distinguish duplicate display names.
- Pick one player or all players, then an explicit role or random role. Twins
  requires two distinct living players selected together. Existing pair and
  linked-infection restrictions follow the production command implementation.
  Splitting a Twins pair individually is rejected; the all-player operation can
  replace the pair. Gameplay cleanup, upgrades, HP and sync use existing paths.
- Disconnects and stale selection callbacks cannot target another player. Back
  works after permissions or the stage change; nested selection and action
  feedback reset the scroll position. Utility navigation no longer refreshes
  the previous page over a newly opened view.
- All 21 language catalogs include the 21 new phrases. Four missing Chinese
  characters were added to their corresponding subset fonts; other fonts are
  preserved. New Japanese glyphs were checked against Checkpoint Revenge.

## Verification

- Final Release build569: zero warnings / errors.
- Installed-game fields: 622 passed. Native calls/Harmony/reflection: 605 passed.
- Existing production Twins command/target harness: 41 passed.
- Custom preset persistence, authority, failure and menu tests: 40 passed.
- Role-test service/menu dispatch, stale targets, stage/permission loss, pairing,
  scroll and navigation tests: 23 passed (game/controller stand-ins).
- Role settings: 845; Base settings: 135; saved Base adjustments: 248.
- Migration/config regression checks: Lifter 19, abilities 374, combat 23,
  Hunter 72, Courier grace 491.
- LocalizationChecks: 25,159; GameUiChecks: 773.
- Catalog/font checks: 6,660 phrases plus Japanese UI placeholder consistency.
- Installed TMP shaped all 666 Arabic/Thai phrases; shaped glyph coverage passed.
- Final DLL's 21 catalogs and seven fonts match source bytes (28 resources).
- Seven ZIP entries match package sources; 47 pinned GitHub icons verified.
- README: 99,932 decoded characters; UTF-8/LF; changelog headings
  have no blank line before their first bullet.

No live Unity menu, gameplay or host-only multiplayer session was run. Automated
checks use production code and deterministic stand-ins as described above; text
shaping and compiled game API validation use the actual installed assemblies.

## Delivery

RSO_TEST was stopped. Seven package files deployed with matching hashes; the
profile's configuration was not edited and testing was not automatically enabled.
Previous DLL, ZIP, release record, profile and config backup:

`D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build569-20261101-183748`

DLL SHA-256: `699e0297bbb279afc41cf8a68b48daf8a59981a628a8309c6b28ff2acf94a56c`.

ZIP SHA-256: `11c3a273a7df4124108cd5f5b12b8590f2c1876f2374c663536ecd8575b52ee6`; 5,618,643 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
