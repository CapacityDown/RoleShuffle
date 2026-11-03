# Role setting order — v5.0.0 build582

Date: 2026-11-03 (JST). Source base: 7046d54e1b6f03c1055ebcae8e524f36fb4c67bf.

## Change

- Role-specific REPOConfig sections follow public role numbers 1-46, then the
  two hidden roles. Each role's bindings stay together. The existing section
  names, keys, defaults, ranges, descriptions and stored values are preserved.
- Role rows within each English/Japanese README settings table follow the same
  order. All original table rows and text are preserved; README remains 99,826
  characters. Existing Roles menus and the main role lists were already ordered.
- Inspected installed REPOConfig 1.2.7: GetModConfigEntries enumerates the plugin
  ConfigFile without sorting entries; CreateModEntries groups by section in
  first-occurrence order. The final DLL constructor contains all 48 role
  sections in contiguous public-number order.

## Verification and delivery

- Release build582: zero warnings/errors. Compiled field checks: 627;
  native v0.5.0 API/Harmony/reflection checks: 612.
- Existing settings harness passed: 393 bindings, 2,465 configuration audit
  checks and all migration, settings, preset and role-menu checks. The subsequent
  final build only moved Rigger's damage binding into its own contiguous block.
- Bilingual README key/default/range/table checks: 1,576. Exact source declaration
  and README row preservation checked before packaging.
- Seven ZIP entries match package files; all 47 pinned GitHub icons verified.
- R.E.P.O. was stopped. RSO_TEST received all seven files, hash-verified.
  Existing configuration was not rewritten. No live in-game UI test was performed.
- Pre-task build580 backup: tmp/deployments/before-build581-20261103-005438.
  Final deployment backup: tmp/deployments/before-build582-20261103-005541.
- DLL SHA-256: 940ebd6f188365de0ccbac8cc2f833bac61297d09ba49153178fbcbecb598a41.
- ZIP SHA-256: d5be64de52d74dd62b50e0c01979b9c2e90b7887bab004273c882c444cf1332c;
  5,637,902 bytes. RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip match.
- No GitHub release or Thunderstore publication was performed.
