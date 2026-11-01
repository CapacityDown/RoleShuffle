# Custom preset section dividers — v5.0.0 build571

Date: 2026-11-02 (JST). Source base: f50c4adafcd9593d4394c8f4d746a15f7911c9b4.

## Behavior

Each Custom 1–3 heading begins a section with one divider above its first
wrapped line. Apply, Save, Overwrite, Confirm and Cancel stay together within
that section without internal dividers, including disabled controls.
The preset page uses explicit section boundaries; built-in presets (including
Standard) and Back retain their separators. Other pages retain automatic
control grouping. Current Roles continues to show overviews.

## Verification

- Release build571: zero warnings / errors.
- Installed game fields: 622; v0.5.0 API/Harmony/reflection: 605.
- Role settings: 845; Base settings: 135; saved Base adjustments: 248.
- Custom preset persistence/authority/menu checks: 40.
- Test-role service/menu regression checks: 23.
- Configuration regressions: Lifter 19, abilities 374, combat 23,
  Hunter 72 and Courier grace 491.
- Seven ZIP entries match their sources; 47 pinned GitHub icons verified.
- README: 99,950 decoded characters; UTF-8/LF.
- Live Unity UI, gameplay and host-only multiplayer were not tested.

## Delivery

All seven files were deployed to RSO_TEST while the game was stopped.
Configuration was unchanged. Previous package/profile/configuration backup:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build571-20261101-185701

DLL SHA-256: 7d91e0052f9d57ba3c472821a287e36c1a2c01f1b40f6ac74836589141a8f3a9

ZIP SHA-256: cee90cab10b7c5f8c94e10f54f0ecf4a9008d23b5237f153cee5e2cd4bc2e739; 5,618,750 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
