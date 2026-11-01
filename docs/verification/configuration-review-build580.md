# Configuration review — v5.0.0 build580

Date: 2026-11-02 (JST). Source base: f52d9b9b1e1e4258761d01415c62fb0005f24736.

## Scope and behavior

- Audited all 393 actual BepInEx bindings. Compared with the preceding 390-entry
  inventory: all existing section/key pairs, types, defaults and ranges are
  unchanged. The configuration schema remains compatible; no reset is required.
- Added Negotiator.DeliveryCarryDistance (20 m, range 1-200),
  Negotiator.DeliveryAllowedSizes (Big,Wide,Tall,VeryTall) and
  Negotiator.EnemyTier (3, range 1-3, exact native tier).
- Size tokens are case-insensitive and ignore whitespace and unknown names;
  an empty list disables delivery earnings. Native unknown categories never
  qualify. Unknown enemy tiers cannot qualify even when Tier1 is selected;
  other roles retain their previous tier fallback.
- Each Negotiator source still pays once per stage, with two rights maximum.
  Changing conditions does not remove already-earned rights. Death, capability
  loss, stage/shop transitions and consumption keep their previous behavior.
- Guides use the host's configured distance, sizes and tier in all 21 languages.
  Sniper's guide now displays the same effective maximum-multiplier distance
  used by gameplay when its two distance settings overlap.
- Corrected descriptions for danger/hardship membership, role eligibility and
  weight, resource HUD, Phoenix, Rescuer, Medic and party-size rules.
- README now covers the omitted Twins controls and dynamic upgrade defaults,
  uses the current level cap of 200 (MapPlayerCount: 1), and explains current
  settings in both English and Japanese. Existing obsolete localization keys
  remain for older-host guide compatibility.

## Verification

- Final Release build580: zero warnings and errors; output saved in tmp/build580.log.
- Installed game field access: 627 checks. Native v0.5.0 API, Harmony and reflection
  contracts: 612 checks, both run after the final build.
- Negotiator production runtime/state/patch checks: 140, including custom carry
  distance, allowed sizes, exact native tiers, unknown tiers, retained rights,
  earning caps, revocation and shop transitions.
- Configuration audit: 2,465 checks across 393 bindings, covering defaults,
  ranges, non-finite values, entry uniqueness and saved custom values.
- Other configuration checks: Lifter migration 19; ability settings 386;
  combat migration 23; Hunter migration 72; Courier grace migration 491;
  role settings 845; base upgrade settings 135; base save adjustment 248;
  custom presets 40; role test menu 23.
- Guide synchronization: 3,258 checks. Localization: 25,444 checks.
- Native TMP Arabic/Thai shaping: 676 values. Font/catalog coverage: 6,760
  phrases plus Japanese key and placeholder checks. These checks used the
  final guide/font bytes before the gameplay-only unknown-tier correction.
- README settings coverage/default/range/table checks: 1,576.
- README: 99,826 decoded characters; CHANGELOG: 23,125. UTF-8/LF, heading
  spacing, seven ZIP entries and all 47 pinned GitHub icon bytes validated.
- Live gameplay and host-only multiplayer were not tested. Automated adapters
  and installed native assembly checks do not establish in-game behavior.

## Delivery

R.E.P.O. was stopped. Seven package files were deployed to RSO_TEST and
hash-verified. The existing configuration was not rewritten; new keys are
bound when the updated plugin starts. The preceding package DLL, ZIP,
profile and configuration are backed up at:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build580-20261101-200905

The pre-review build577 backup is also retained at:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build578-20261101-200456

DLL SHA-256: 5749e716b77b4180462a04d706273e6f3ca3664f037907df5ef92473aea0d9d8

ZIP SHA-256: a7709aaf5ee50150d7a4681a4aeaaf5c1bbe454ec5831648f4bca07d16a14bc9; 5,637,877 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No GitHub release or Thunderstore publication was performed.
