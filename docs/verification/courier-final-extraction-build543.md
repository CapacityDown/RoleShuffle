# Courier final-extraction exemption — v4.6.1 build543

Date: 2026-10-23 JST. Branch: `version/4.6.1`.

## Behavior

Courier automatic HP loss stops when the native game confirms final extraction,
for the remainder of that stage. Intermediate extractions retain normal delivery
pauses. Revival, leaving the truck and a new Courier assignment retain the final
exemption. Stage cleanup clears it. Delivery healing remains available; enemy,
fall and other incoming damage are unchanged. Tuna is unchanged.

The finite grace countdown is omitted after final extraction. The detailed guide
in all 14 languages and the bilingual README document the exemption. CHANGELOG
contains the combined final Courier changes under v4.6.1.

## Native contract

The installed RoundDirector.ExtractionCompletedAllCheck compares completed points
against extractionPoints - 1 while the successful extraction is still processing,
and only the host or solo game invokes ExtractionCompletedAllRPC. The receiver
validates MasterOnlyRPC before setting allExtractionPointsCompleted. A Harmony
postfix reads that accepted result and forwards it to the active authoritative
controller. It does not wait for the later payout/ExtractionCompleted callback.
The existing intermediate-extraction hook is kept separate.

## Verification

- Release build543: zero warnings and errors; assembly version 4.6.1.0.
- Installed game field-access checks: 609 passed.
- New native Courier contract checks: 20 passed, including Harmony target/field,
  native host gating, separate intermediate completion, active-stage authority
  and HUD connection.
- Overhaul/runtime checks: 76,440 passed. Added cases cover indefinite exemption,
  queued damage removal, duplicate completion, one-HP revival, truck exit, late
  assignment and stage reset, including remote-player configuration.
- Localization: 13,986 passed. Guide synchronization: 2,100 passed with installed
  Photon Protocol18; configured catalog payload 36,371 bytes.
- Chinese/Korean translation glyphs are all present in existing font subsets.
- README: 98,173 characters; CHANGELOG: 20,755.
  UTF-8 without BOM, LF, no blank line between version heading and first bullet.
- ZIP: seven required entries, valid CRC, every byte matches package sources.
- Actual gameplay and host-only multiplayer have not been tested.

## Artifacts and deployment

- DLL SHA-256: `7c0453347a2351116c87b64343946977bd9b09afc2b22a1abef80321966a4686`.
- ZIP SHA-256: `ac1bc6c3cbd38931e1ad27387bc61fca85f2c948fa9212fbcd820d2183eee306`; 5,172,004 bytes.
- Archives: `RoleShuffle.zip`, `output/release/RoleShuffle-4.6.1.zip`.
- Previous build542 archives preserved in `tmp/packages/before-build543-20261023-133632`.
- v4.6.0 build540 archive remains unchanged: `260b05c258c5db1682ccb5f0150f74206200878b6aa8a3426f97802823df7497`.
- RSO_TEST deployed with the game stopped; all seven files match package hashes.
  Backup and record: `tmp/deployments/before-build543-20261023-133533`.
  Profile configuration was not changed.
