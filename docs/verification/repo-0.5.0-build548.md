# R.E.P.O. v0.5.0 compatibility — v5.0.0 build548

## Scope and baseline

- Requested 2026-10-30: move the v4.6.1 work into v5.0.0 and support R.E.P.O. v0.5.0.
- Outgoing `version/4.6.1` at `537e1436896ac82b0668067a9511680323f75c65` was fast-forwarded into `main` and pushed. Development continues on `version/5.0.0`.
- A verified Git bundle and previous release artifacts were saved in `tmp/versions/before-5.0.0-20261030-031156`. The previous branch and versioned ZIP remain intact.
- Courier's longer grace periods and automatic HP-loss exemption after final extraction are included in v5.0.0. No separate v4.6.1 changelog entry is published.
- Actual game: Steam public build 26294889, Unity 6000.3.17f1. Assembly-CSharp SHA-256: `aa6b1ce0deef8d2b7104a3722864c4ba94c107257726471f8e44b45c99e36888`.

## Compatibility changes

- Compile against installed Unity 6/game assemblies using `REPO_GAME_DIR` or ignored `Directory.Repo.props`. No game assemblies or decompiled sources are committed.
- Use current rigidbody, scene lookup and TMP APIs. Preserve lookup order and HUD font feature settings.
- Patch both native Lifter grip and torque helper call sites, preserving native force/torque stabilization. Update the Strength model to the native high-level floor for Lifter eligibility and King's non-weakening bonus selection. Keep existing fixed-strength gameplay and Lv1 item handling. Native heavy grip/rotation reaches approximately 5.958335747 at Lv50 and remains there through Lv200.
- Support committed local expression-wheel selections without treating preview/restoration as ability input. Vanilla guest owner-RPC handling stays in place.
- Apply Hunter continuous weapon drain savings at the new central drain method; remove duplicate leaf-blower handling. Preserve full-bar and direct-deduction handling.
- Add the locale parameter required by private chat/TTS RPCs. Host authorization remains supported by the native receiver.
- Track Mage impact effects in `SlowProjectile.DestroyLogic`, where the game now creates them.
- Match the new menu wheel calculation while retaining gamepad/keyboard branches and unrelated menu behavior.
- Require REPOModding MenuLib 2.6.0 and REPOConfig 1.2.7. Their BepInEx GUIDs remain unchanged. The old MenuLib 2.5.4 has a missing native MenuEffectClick overload and cannot be used for this game version.

## Automated verification

Release build548: **0 warnings, 0 errors**. Earlier failed/intermediate main build attempts used their own incremented UI build numbers (544–547).

| Check | Passed |
|---|---:|
| Compiled field access against installed game | 610 |
| Game calls, Harmony targets/arguments, reflection contracts | 553 |
| New MenuLib / REPOConfig native contracts | 138 / 111 |
| Lifter math, installed IL, emitted IL, isolated runtime | 250698 |
| Upgrade/overhaul rules | 76439 |
| Expression input | 111 |
| Scroll calculation using installed IL | 71 |
| Hunter battery handlers | 85 |
| Mage beam / staff | 17228 / 38 |
| Porter / Twins / combat | 13053 / 32 / 73 |
| Courier / Porter / DualWielder native contracts | 20 / 37 / 71 |
| DualWielder delayed pose/runtime | 7357 |
| Influenza / Tracker | 154 / 59 |
| Upgrade-item retention | 89 |
| Stinker | 912 |
| Localization / guide synchronization | 13986 / 2100 |
| Budget and recovery review | 2704 |
| Utility / utility runtime / optimization | 6654 / 74 / 108 |
| Per-player notification and private TTS queues | 57 |
| Revival stillness and Phoenix transitions | 6594 |
| HP-ratio reset and lifecycle | 20405 |

RoleSettingsChecks passed ability settings, combat/Hunter/Courier migration, role selection, Base selection and save adjustment checks. Historical test adapters were updated for Unity 6 and current Porter references; these adapters are not a running Unity game.

Native host-accepting guards were inspected for health, upgrades, valuable value and private speech. This does not prove live vanilla-client multiplayer behavior.

## Packaging and deployment

- README: 98,209 characters; CHANGELOG: 20,800 characters. Both UTF-8 without BOM and LF. Version headings directly precede their first bullet.
- Exactly seven ZIP entries, each byte-compared with its source; no game DLLs or dependency MODs in the release ZIP.
- Full/public v5.0.0 icon catalogs generated from unchanged approved artwork; public secret-name/art checks passed.
- RSO_TEST updated while the game was stopped. Previous plugin files, dependency folders, profile metadata and configuration were backed up. MenuLib/REPOConfig were moved to their REPOModding package folders to avoid duplicate plugin GUIDs. RoleShuffle settings were not edited.
- Build, package and deployed RoleShuffle DLL SHA-256: `03230fdd5f431a804d6f4512a0bcf4272ef81934cb17acd7eef7706421c3a82d`.
- MenuLib 2.6.0 SHA-256: `d9c03a047436e6b0cf3d779031095ed1da64a3a7eef4c17102becaeb4aadbbbc`.
- REPOConfig 1.2.7 SHA-256: `c24793d2272e0386e8cbcbeb74d7b6ec0ed0dfc46f91aecc094de0ee07ccdc14`.
- Deployment backup: `tmp/deployments/before-build548-20261030-034516`.
- ZIP SHA-256: `c7f3b97b7b5d424a2eb565e7dd4d36455d72d8c6af6096128307991778b20af9`.

## Verification limits

No fresh Unity startup, rendering, physical grabbing, scene transition or host-only multiplayer play test was performed. Other optional MODs in RSO_TEST were not certified for v0.5.0. The checks above establish build/static compatibility and modeled behavior, not successful live gameplay. A Thunderstore/GitHub release was not published.

## Dependency sources

- [MenuLib 2.6.0](https://thunderstore.io/c/repo/p/REPOModding/MenuLib/)
- [REPOConfig 1.2.7](https://thunderstore.io/c/repo/p/REPOModding/REPOConfig/)

Both downloaded official package changelogs identify v0.5.0 compatibility.
