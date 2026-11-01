# Native game locales — v5.0.0 build567

Date: 2026-11-01 (JST). Source base: 30c6135cd0e67fed72a432fa264a8459b87471a0.

## Delivered behavior

- Menus, role overviews/details, HUD-editor labels and draw-eligibility reasons
  follow all 21 installed R.E.P.O. v0.5.0 locales.
- Added Arabic, Danish, Finnish, Swedish, Thai, Portugal Portuguese and Latin
  American Spanish catalogs: 312 phrases each, 2,184 new translated values.
- Existing enum ordinals, game-language following and English fallback for unknown
  future locales are preserved. Roles/commands and compact spoken notices retain
  their canonical identifiers. The retired MOD language override is not restored.
- Native TMP shaping enables Arabic joining/bidi and Thai marks per MOD label;
  direction resets when switching languages. Thai wrapping preserves grapheme
  clusters and keeps preposed/spacing vowels with their bases. Arabic words stay
  intact. Numeric roulette deltas remain left-to-right.
- Two full upstream Noto fonts retain all joining forms and positioning tables.
  Source hashes/URLs are in Assets/Fonts/Localization/arabic-thai-sources.json.
  The upstream OFL is byte-identical to the existing bundled NotoSans license.
  No global TMP font/settings mutation or runtime font download was added.

## Native locale evidence

Read LocaleAsset.m_Identifier.m_Code from the installed localization locale bundle:

`E:/SteamLibrary/steamapps/common/REPO/REPO_Data/StreamingAssets/aa/StandaloneWindows64/localization-locales_assets_all.bundle`

SHA-256: `0a32d86d0d1ee5dee9f8d6ca1ff499ecb274d5fa3d662e7768497869cfea8edb`.

Locales: ar-001, da-DK, de-DE, en-US, es-419, es-ES, fi-FI, fr-FR, it-IT, ja-JP, ko-KR, pl-PL, pt-BR, pt-PT, ru-RU, sv-SE, th-TH, tr-TR, uk-UA, zh-CN, zh-TW.

The checked-in GameUiChecks/repo050-locales.json fixture contains these codes;
all map to 21 distinct catalogs rather than falling back to English.

## Validation

- Release build567: zero warnings and zero errors against installed v0.5.0.
- GameFieldAccess: 622 native field references passed.
- Repo050Compatibility: 605 calls, Harmony bindings and reflection contracts passed.
- LocalizationChecks: 23,962 passed, including every configured role description,
  host parameter preservation, placeholder multiplicity and secret concealment.
- GameUiChecks: 773 passed, including 21 distinct native locale mappings,
  regional aliases, native upgrade translations, direction reset, Arabic word and
  Thai grapheme wrapping, and legacy Japanese font material behavior.
- GuideSyncChecks: 3,237 passed; guide publication payload 39,341 bytes.
- DrawEligibility: 2,295 passed; full-lobby preview 8,219 bytes.
- OptimizationChecks: 122 passed plus Negotiator snapshot/suppression checks.
- UtilityRuntimeChecks: 74 passed.
- Catalog/font audit: 6,240 complete English/translation entries, Japanese UI
  placeholder consistency, native names and uppercase glyph coverage passed.
- NativeTextShaping: all 624 Arabic/Thai values passed through the installed
  Unity.TextMeshPro shaper. Joined Arabic presentation forms were produced, and
  every resulting glyph exists in the bundled fonts. The native API returns a
  reused null-terminated buffer; only characters before its terminator are used.
- All 21 catalogs and seven font files embedded in the final DLL matched source
  bytes (28 resources). Packaging checked all seven ZIP entries byte-for-byte,
  UTF-8/LF, version consistency, and 47 pinned GitHub icon downloads.
- README: 99,965 decoded characters, below 100,000.

Checks use deterministic stand-ins where Unity gameplay is required. Native text
shaping was exercised directly, but font atlases/layout were not rendered in the
game. No live gameplay or host-only multiplayer session was run. Translations
have not been professionally proofread by native speakers.

## Deployment and package

RSO_TEST was stopped; the seven package files were deployed and hash-verified.
No configuration or running game process was changed. Previous package DLL, ZIP,
release record, profile and configuration are preserved at:

`D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build567-20261101-072005`

- DLL SHA-256: `c01f24762a245fc6ee469c14a7ceee3dec4d3ac3b87e935e8b2fb6fc60fb5b10`
- ZIP SHA-256: `268fc1e8e781b2bc2bc477cae7f52cf6dcc424943c069d859390f4658c427676`
- ZIP: 5,603,279 bytes; RoleShuffle.zip and
  output/release/RoleShuffle-5.0.0.zip are identical.
- No GitHub release or Thunderstore publication was performed.
