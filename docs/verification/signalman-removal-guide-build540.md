# Signalman removal, compact numbers and guide views — build540

Date: 2026-10-18 JST. RoleShuffle v4.6.0, branch `version/4.6.0`.

## Result

- Removed Signalman from role assignment, configuration, presets, capability lists,
  chat/death hooks, resource HUD, guide translations and active emblem assets.
- Ordinary role/test/emblem numbers are 1–44: Twins 42, Porter 43, DualWielder 44.
  Secret IDs remain 1001/1002. Internal enum values are 0–43 and 1001/1002.
- Existing room-property role IDs are translated at the wire boundary. Removed
  wire ID 41 is rejected; Twins/Porter/DualWielder retain wire IDs 42/43/44 to
  avoid a mixed-build peer interpreting a different role. This is not a claim
  of complete mixed-build UI compatibility.
- Schema 43 removes the old Signalman settings and preserves an exact pre-change
  configuration backup plus unrelated settings. Repeating migration is harmless.
- Retained shared private notification delivery for Tracker. Wait timeout is now
  five seconds without a removed-role setting. Ordinary chat forwarding is gone.
- The role guide opens in Overview and has Overview/Details buttons. Overview
  gives concise abilities; Details retains host-supplied configured values,
  conditions and limits. Switching modes resets the guide scroll position.
  Both views use the same enabled-role and secret-reveal rules in all 14 languages.
- Updated English/Japanese README, final v4.6.0 change summary, image references,
  localization fonts and full/public icon catalogs. Artwork pixels are preserved;
  only existing asset names/numbers change. The public catalog was visually reviewed.

## Verification

- Release build540: zero warnings, zero errors.
- Installed game field access: 609 compiled references checked.
- LocalizationChecks: 13,950 checks across 14 languages.
- GuideSyncChecks: 2,100 checks, including overview/details, host values, secret
  concealment, role ID translation and Photon Protocol18 transport (36,099 bytes).
- Ability configuration: 350; combat migration: 23; Hunter migration: 72;
  role settings: 349; Base Upgrade settings: 128; save adjustments: 248.
- Notification/private notice checks: 57, including per-recipient separation,
  busy-recipient expiry, dispatch exceptions and existing countdown behavior.
- OverhaulChecks: 76,420; OptimizationChecks: 108; InfluenzaChecks: 154.
- Secret-role/selector checks: 86; Twins command/target checks: 41;
  Twins artwork/localization checks: 59.
- Porter native contract: 37; DualWielder native contract: 71, including the
  renamed embedded emblem byte hashes.
- README: 98,067 characters; CHANGELOG: 20,469; UTF-8 without BOM and LF.
  Both README role tables contain 46 roles with English-only role names; no
  secret names/artwork are exposed and icon references use a fixed asset commit.
- Asset commit: `c5c96682f06f981c33c897d7c74c4111d04d37cb`.
- `git diff --check` passed.

## Package and deployment

Build/package DLL SHA-256:
`e60b29e326031b1b9385d64922b4e29442185bed1518f774f32cd66a90ba0b36`.

Deployed build540 to RSO_TEST at 2026-10-18 13:03 JST with the game stopped.
All seven package/profile files matched SHA-256. Previous profile files are
backed up in `tmp/deployments/before-build540-20261018-040344/RSO_TEST`.
The previous build539 package DLL remains in
`tmp/deployments/before-build540-20261018-040202/package-RoleShuffle.dll`.
The existing release ZIP and profile configuration are unchanged.

Actual in-game menu interaction and host-only multiplayer remain untested.
