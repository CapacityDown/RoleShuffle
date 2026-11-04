# Custom preset notes — v5.0.1 build595

Date: 2026-11-04 (Asia/Tokyo).

## Behavior

- Roles → Role settings → Presets offers a note editor for each saved Custom 1–3 slot.
- Notes are local to the profile, single-line plain text, limited to 120 UTF-16 code units without splitting surrogate pairs. Blank saves remove the note.
- Saving/canceling a note leaves active roles, role switches, weights and ability settings unchanged. Role-selection overwrites retain that slot's note.
- Legacy schema 1 presets remain readable; schema 2 stores notes alongside the three slots. Note-only writes preserve unknown role names, use an atomic replacement, and back up the previous file.
- Guests and unused slots cannot edit notes. Saving rechecks authority and the active settings service. Failed writes retain the editor draft.
- The editor uses MenuLib keyboard/clipboard input, literal text with a plain caret, and a full-width input area. IME is enabled while focused and restored on focus loss or closure.
- Added four UI phrases in all 21 game languages and only the missing glyphs to the existing Chinese/Korean font subsets.

## Validation

- Release build: zero warnings and errors.
- 63 custom-preset checks passed, including restart persistence, legacy migration, per-slot isolation, role overwrite preservation, blank removal, bounds/Unicode handling, authority, locked-file failure and caret preprocessing. Menu dispatch uses stand-ins; it does not render Unity UI.
- Existing configuration/role/Base Upgrade/role-test checks passed in RoleSettingsChecks.
- 721 compiled field-access checks and 662 installed v0.5.0 API/Harmony/reflection checks passed on the final DLL.
- 25,786 localization checks passed; 6,880 translated phrases, Japanese UI keys and new Japanese glyphs verified. The installed game's TMP shaped all 688 Arabic/Thai phrases and glyph coverage passed.
- Build-time and RSO_TEST MenuLib DLLs match: `d9c03a047436e6b0cf3d779031095ed1da64a3a7eef4c17102becaeb4aadbbbc`.
- Public README: 99,911 / 100,000 characters. CHANGELOG: 23,419 characters. UTF-8 without BOM; no blank line between the new version heading and its entry. Existing v5.0.0 changelog retained.
- No live-game rendering, IME interaction, Save/Cancel/Escape interaction or multiplayer test was performed. These remain in-game verification points.

## Deployment

- DLL SHA-256: `fc580a68cc099aa22a5ef8ff9eba485386933ee25d0a2e777db564ce1b464e51`.
- RSO_TEST: seven package files deployed and checksum-verified; no configuration or existing custom-preset file was edited by deployment.
- Prior plugin and configuration backup: `D:\Documents\R.E.P.O. MOD\RoleShuffle\tmp\deployments\before-build595-20261104-004050`.
- Deployment record: `tmp/deploy-build595.json`.
- Both existing v5.0.0 ZIP files remain byte-for-byte unchanged; no new release ZIP was created for this feature task.
