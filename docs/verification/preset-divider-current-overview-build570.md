# Preset divider and current-role overview — v5.0.0 build570

Date: 2026-11-02 (JST). Source base: a7bd3540e469cd68619300be4a5aed763cf13086.

## Behavior

- The Standard preset explicitly begins a new button group, drawing a divider
  above it after the three custom slots. Other consecutive controls retain
  their grouping; the role-guide layout and row heights are unchanged.
- Expanded Current Roles descriptions use the same localized overview as the
  guide. Assigned secret roles receive their revealed summary. Detailed
  host-configured descriptions remain available in the guide's Details mode.
- English and Japanese README text describes the overview display.

## Verification

- Release build570: zero warnings / errors.
- Installed game fields: 622; v0.5.0 API/Harmony/reflection checks: 605.
- Role settings: 845; Base settings: 135; saved Base adjustments: 248.
- Custom-preset tests: 40; role-test menu/service tests: 23.
- Migration/config regressions: Lifter 19, abilities 374, combat 23,
  Hunter 72, Courier grace 491.
- Guide publication, localized overview/detail and secret visibility,
  protocol round-trip, compatibility and failure isolation: 3,237 checks.
- README: 99,950 decoded characters; UTF-8/LF.
- Seven ZIP entries match package sources; 47 pinned GitHub icons verified.
- No live Unity UI, gameplay or host-only multiplayer session was run.

## Delivery

RSO_TEST was stopped; all seven package files deployed and hash-verified.
Configuration was unchanged. Previous package/profile/configuration backup:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build570-20261101-184952

DLL SHA-256: 6a279f5e3dae46cebb51006f515178bdedc6763a7ae82d931ecc428671172976

ZIP SHA-256: c5f6161fabfc08076ba40ceb62dea034d183a020a1794a51a39676c67f78165a; 5,618,702 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
