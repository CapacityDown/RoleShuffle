# Porter cargo handling and HUD — build534

Date: 2026-10-10. Branch: version/4.6.0. Version: 4.6.0.

## Behavior

- Smooth custom HUD silhouettes use the union of solid vector triangles, 4x
  supersampling into a 256px alpha texture, filtered mipmaps and the existing
  vanilla font/size/cyan tint. Native HP and stamina sprites remain native.
- Porter HUD shows stored weight and the current sum of native valuable prices.
  Currency uses digit grouping with no fictitious maximum. Values over 10,000
  survive the status codec; other metrics retain their existing bounds.
- Confirmed enemy HP damage scatters all original stored objects, without
  duplicating or replacing them. Objects that fail to return remain in custody
  and retry automatically. Successful drops resume their original behaviors.
- Speed level is floor(unloaded level * (1 - clamp(weight/capacity, 0, 1))).
  At default capacity 15, unloaded Lv10 becomes Lv5 at 7.5 and Lv0 at 15.
  Unloading, death, departure and role cleanup remove the deduction.
- A temporary deduction ledger preserves native purchases, shared upgrade
  grants and King/Ghost Speed auras, including when actual Speed is already 0.
  Absolute role/base assignments replace the old deduction. Walking speed and
  native level-0 sprint speed are unaffected.
- Existing capacity/size/hold/unload/drop configuration remains available.
  Full guide translations and cargo-value labels cover all supported languages.
  Additional glyphs are included in the bundled fonts.

## Verification

- Release build: 0 warnings, 0 errors.
- PorterChecks: 12,827 accounting/lifecycle/value/scatter/Speed checks.
- OverhaulChecks: 76,425 checks, including composed Speed/aura/item grants,
  reset and send-failure accounting, cargo currency codec and raster coverage.
- GuideSyncChecks: 676 checks against installed Photon Protocol18.
  Full guide payload: 36,099 bytes; the binary transport regression stays fixed.
- LocalizationChecks: 13,698 checks.
- Porter translation/placeholder/bundled-glyph checks: 195.
- CombatChecks: 73; LifterChecks: 249,993 (including installed-game IL).
- Installed non-publicized game field references: 579.
- Porter native contracts: 24, including native Speed RPC and immediate movement update.
- Package README: 99,947 decoded characters; UTF-8 without BOM, LF.
- Git whitespace checks passed.
- HUD symbol preview visually reviewed at 112px, 75px and 25px:
  [Preview](porter-hud-build534.png).
- DLL SHA-256: cdb634c62f745847805f4598f2563229671f6467d40bb3f475404c473373b36d.

These are source/native-contract, automated and offline visual checks.
Gameplay appearance and host-only multiplayer behavior need in-game validation.
No new release ZIP was requested or produced; earlier releases remain preserved.

## Deployment

RSO_TEST deployment completed at 2026-10-10T09:56:20Z with the game stopped.
All seven package files matched their deployed SHA-256 hashes. Previous package
and profile files were backed up; user configuration was preserved.
