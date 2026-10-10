# Guide publication failure — v4.6.0 build533

Date: 2026-10-10 JST. Branch: `version/4.6.0`.

## Log diagnosis

RSO_TEST recorded 53,515 copies of Photon Protocol18's `NotSupportedException`:
the outgoing string was 35,183 UTF-8 bytes, above the 32,767-byte string limit.
Stacks identified both `RoleGuideSync.Publish` and `RoleSyncStatus.PublishNow`.
The same exception escaped the `PrepareStage` coroutine before
`CompleteStagePreparation`, leaving `_assignmentsInitialized` false. Changing
the role to Porter changed its assignment but could not start its Update loop.
Porter settings were the defaults (capacity 15, hold 3 s, unload 10 s,
enemy spill enabled, Tiny/Small/Medium allowed).

The log also contained one Photon room-cleanup NullReferenceException after
Steam auth cancellation on shutdown. No evidence linked that later cleanup
exception to the failed storage attempt. Raw logs and player identifiers were
not added to the repository.

## Fix

- Small guide payloads retain the string format. Larger payloads use Photon
  byte-array framing with lossless UTF-8. Receivers accept both formats.
- Both normal guide publication and periodic/manual status publication use the
  same encoding. Status hashes cover the full reconstructed text. The legacy
  English guide remains available; invalid data falls back safely.
- Publication errors are contained and duplicate warning messages suppressed.
  Status publication schedules its next attempt before sending, so failures
  cannot trigger a per-frame retry storm.
- Gameplay preparation completes before room display-data publication.
  Announcement initialization still precedes Twins partner-notice scheduling.

Porter's storage conditions and capacities were not changed for this repair.
The fault observed in this report occurred before those checks could execute.

## Validation and deployment

- The installed game's actual Photon Protocol18 serializer reproduced the
  35,183-byte failure. The revised format round-tripped boundary sizes, larger
  payloads, Japanese and supplementary Unicode without loss.
- Guide checks: 676 passed, including all roles/all 14 languages, a 35,695-byte
  configured catalog fixture, forced publication, fallback and injected failure.
- Production status runtime checks: 74 passed, including large-guide stamps,
  failure backoff and recovery. Porter runtime: 354; localization: 13,662 passed.
- Release build533: zero warnings/errors. Native field access: 579 references;
  Porter native API and embedded-artwork checks: 21 passed.
- RSO_TEST updated with the game stopped; all seven package files verified.
  Build/package/deployed DLL SHA-256:
  `7e2818bc086b9cc64fe38dc7cd3de6e90328ff29d8195286ca0361844ad1c4a4`.
- Previous deployment and log:
  `tmp/deployments/before-build533-20261010-092931`.
- Existing configuration, release documents and previous ZIP were preserved.

No live-game storage or vanilla-guest multiplayer retest was performed. The
tests establish the reported serialization failure and its correction; the
user should confirm Porter storage after restarting with build533.
