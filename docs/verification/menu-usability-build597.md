# RoleShuffle v5.0.1 — menu usability and regression verification

Date: 2026-11-04. UI build: 597. Development branch: `version/5.0.1`.

## Scope

Implemented proposals 2, 3, 4, 6, 7 and 8 from the approved improvement list.
Proposal 3 is limited to names and duplication between the existing three local
preset slots: the user subsequently deferred sharing, so there are no share
codes, clipboard exports/imports, or external preset-transfer UI.
Ability-status UI (1) and contribution statistics (5) are excluded.

- Personal upgrade breakdown under Base Upgrades: actual levels, base targets,
  retained items, role target, dynamic changes, support and cargo deductions.
  Retention is informational within the applicable role target, not added twice.
  Unattributed changes are shown as Other, not falsely credited to a role.
- Names and notes persist through schema 1/2 → 3 migration. Duplicate previews
  require confirmation, reject changed destinations, and never apply role switches.
- Guide/settings search and category/enabled/eligible filters retain numerical
  ordering and conceal unrevealed secret names. Conditional choices are included
  in the eligible filter; unknown results are excluded.
- Optional team adjustment defaults OFF. Existing draw order is preserved while
  OFF. ON prioritizes support and applies danger limits; existing history,
  eligibility and fallback rules still apply.
- Detailed setting tables moved to `docs/SETTINGS_GUIDE.md`; README keeps role
  icons, role descriptions, installation and operation instructions.

## Validation

- Release build: zero warnings/errors, against installed R.E.P.O. v0.5.0 / MenuLib 2.6.0.
- Compiled game access: 722 field references and 667 calls/Harmony/reflection contracts passed.
- Role settings: 396 bindings, 2,483 configuration audit checks; existing migration,
  menu/base/manual-setting suites, 63 existing preset tests, 229 new duplication,
  migration, confirmation, filtering and breakdown checks, and 23 role-test menu checks passed.
- Upgrade transport: 23 tests of the production sender/receiver with simulated
  Photon endpoints. The host derives the owner from the requesting actor; only
  that actor receives a response. Payloads contain no account IDs/player names.
  Spoofed senders, expired nonces, old scenes/rooms/hosts, malformed data and
  request floods are rejected. Slow replies are accepted before retry timeout.
- Draw eligibility/planner: 2,359 checks, including seeded OFF equivalence,
  support precedence in two-player teams, late arrivals, danger limits and
  exhausted candidate fallback without inventing disabled roles.
- Lifecycle runner `tools/Test-RoleLifecycle.ps1`: all 12 suites passed:
  Lifter 253,050; Review 2,704; Overhaul 84,371; Influenza 154; Twins 58;
  Porter 13,053; Negotiator 140; Musician 244; DualWielder 7,377;
  MageBeam 17,228; UtilityRuntime 74; UpgradeItem 89.
  Added Lifter-to-every-destination-role sequences for death/revival, stage cleanup,
  and reconnect at five target levels. Rejoin native level initialization is
  explicitly simulated; the real absolute setter repairs stale grab caches.
- Localization: all 21 catalogs; 27,952 localization checks; 7,640 resource/glyph
  checks plus Japanese UI coverage. All 764 Arabic/Thai values were shaped using
  installed game TMP; generated forms fit bundled fonts. CJK subsets preserve
  previous glyphs and add required characters. Japanese fallback coverage checked.
- Documentation: 1,590 bilingual configuration/default/range/table checks passed
  across README and Settings Guide. README 49,801 characters, CHANGELOG 23,846;
  UTF-8 without BOM. Existing v5.0.0 ZIPs unchanged.

## Boundaries

Most runtime suites execute production C# against deterministic Unity/network
adapters. Installed-game contracts and text shaping are verified separately.
Live Unity layout, native rejoin behavior and multiplayer packet delivery have
not been exercised in-game. New details require an installed guest and a host
with this version; vanilla gameplay clients need no additional installation.
Team support is a priority, not an unconditional guarantee when history or
candidate requirements prevent it. Presets still store role ON/OFF only.

## Artifact

DLL SHA-256: `f4f93fb6a8ed6537e4742de9536be4a52b9e68423640c8d65c22cc9222b46748`.
Previous published v5.0.0 ZIP SHA-256:
`8922f890529184dd953bc488f767d4b41a9256201f6a042bc168f93260211a32`.

## Deployment

RSO_TEST updated at `2026-11-04T04:28:57.4757914Z`; all seven deployed package files match their source hashes. The prior plugin, configuration and release artifacts are retained in `tmp/deployments/before-build597-20261104-042856`. Configuration values were not edited and the published v5.0.0 ZIPs remain unchanged.
