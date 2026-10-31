# Roles menu and native language integration — build553

## Reported failure and evidence

The RSO_TEST log shows a `TMP_MaterialManager.GetFallbackMaterial` null-reference
exception while `RoleMenu.WrapGuideText` measures Japanese text. It propagates
through `RefreshAssignmentRows`, `SwitchView` and `OpenRolePage`, preventing the
page from opening and leaving repeated MenuLib/TMP update errors.

Inspection of the unchanged CheckpointRevenge bundle shows both Normal/Transmit
font assets serialize `material`, with 7,140 characters, one atlas and padding 15.
The installed Unity 6 TextMeshPro assembly reads `TMP_Asset.m_Material`. Its fallback
material path dereferences the target material and main texture. The missing
legacy material is repaired before this font can enter the fallback chain.

## Implemented behavior

- Reconstruct the missing material using the original atlas, actual dimensions,
  padding and font weights, plus the current native SDF shader. Rebuild font
  definitions after material assignment. Reuse healthy materials without churn;
  reject fonts with no atlas or available shader. Existing glyph metrics remain.
- Roles page creation catches failures and clears the failed page/tracking so a
  later click can retry. Text measurement restores temporary settings in a finally
  block. The menu no longer inserts MOD fonts into global TMP fallback settings.
- Roles navigation, descriptions, settings, history, Tools and the HUD editor use
  the local R.E.P.O. locale. Remove the independent language button/config entry.
  Schema 46 preserves the old configuration as `.pre-v5.0.0-game-language.bak`
  when migrating schema 45 and removes only UI.GuideLanguage.
- Map the game's locale codes to the existing 14 MOD catalogs. Regional Spanish
  and Portuguese variants share the available translation; other menu languages
  fall back to English. Language is resolved on opening and refreshed while open.
- Roulette upgrade names come from `StatsManager.upgradesInfo` and its localized
  display name, the same data source as native StatsUI. Cache names per local
  locale and retry if native tables were unavailable. Special AllUpgrades/None
  results and headings use the MOD catalogs. Canonical network IDs and selection
  probabilities are unchanged, so clients can display different languages for the
  same result. Refresh visible results on locale changes and autosize long names.
- Add the three missing catalog glyphs to the Chinese/Korean Noto subsets. All 14
  catalogs now have complete glyph coverage in the corresponding bundled fonts.

## Validation

| Check | Result |
|---|---:|
| Release compilation against installed game | 0 warnings / 0 errors |
| Native language/name/font production-code checks | 633 |
| Localization catalogs, placeholders and descriptions | 13892 |
| Guide synchronization / native Protocol18 round-trip | 2100 |
| Configuration / Base settings / save adjustments | 353 / 128 / 248 |
| Ability settings | 344 |
| Lifter / combat / Hunter / Courier migration | 19 / 23 / 72 / 491 |
| Draw selection and application | 131088 |
| Installed-game menu scroll IL checks | 71 |
| Installed game field references | 613 |
| Installed v0.5.0 calls and patch contracts | 547 |
| Catalog glyph coverage | 14 catalogs, 0 missing characters |
| GitHub README icon downloads | 45 byte comparisons |

New regression cases cover 22 locale variants, native localized names for all 12
roulette upgrade types, per-locale cache invalidation and switching back, absent
managers, temporary translation failure, unknown IDs, combined/empty results,
right-to-left metadata, missing legacy materials/textures, shader fallback and
idempotent material repair. Game/Unity objects are simulated in the executable tests.

## Delivery

- Version 5.0.0, UI build553; branch `version/5.0.0`.
- RSO_TEST updated while the game was stopped. Seven deployed files byte/hash
  verified. Settings will migrate on next startup; no save data was edited.
- Previous plugin, settings, error log and release ZIP retained in
  `tmp/deployments/before-build553-20261031-005830`.
- README 98,531 characters and CHANGELOG 21,314;
  UTF-8 without BOM, LF, with no blank line following version headings.
- Release ZIP contains exactly the seven approved files, each byte-compared.
- Build/package/deployed DLL SHA-256: `47074f383ec7acaefc3ea99a15aff6562c4f72a7a6bafa289cf4a2b1d3945583`.
- ZIP SHA-256: `a87ce9e3a57f6b2a4dd821678670d0e36a24b9bd0f1aee07aefbc76799c4afe5`.

## Limits

No live game launch, Roles click, rendered menu inspection or multiplayer session
was performed. The first restart must confirm Japanese Roles navigation and the
truck roulette in the user's game. Unsupported MOD menu languages use English;
native roulette upgrade names still use the game's translations. No remote release
was published.
