# Twins Health upgrade synchronization — v5.0.0 build583

Date: 2026-11-03 (JST). Source base: 7c0adde270f2a1ce238f341c177d35b87208f0d6.

## Cause and fix

- The previous runtime inferred each twin's maximum changes from health packets.
  A vanilla remote upgrade can change its maximum on the host without a local
  Heal callback. Publication then depended on a later owner health packet;
  packets carrying a previous shared maximum could also reverse the gain.
- Inspected the installed v0.5.0 native IL. UpgradePlayerHealth clamps the actual
  level change, writes the upgrade level, then calls UpdateHealthRightAway with
  that delta. UpdateHealthRightAway adds 20 HP per level to the maximum on each
  peer and heals/damages only the local owner.
- A host-authoritative prefix on UpdateHealthRightAway records the actual delta
  in the recipient's individual contribution and queues shared HP publication.
  Both players receive the combined maximum on the next controller update,
  independently of owner healing packets or upgrade-item retention settings.
- Raw health-packet maxima no longer change individual contributions. Native
  healing remains on the existing shared-health path and is not added twice.
  If a full-health owner's upgrade heal arrives before the host's upgrade event,
  only its overflow is deferred until an actual maximum increase confirms it.
  Linked death clears deferred healing; maximum increases cannot revive the pair.
- Each real recipient contributes once, including multiple levels and two-player
  retention scope. Unlink restores each player's own upgraded maximum and the
  existing shared remaining-HP ratio. Existing host-only simultaneous-health
  timing limitations are unchanged; no claim of exact damage reconciliation.

## Verification

- Added a reproduction using the installed native event ordering. It failed on
  the old runtime: the partner stayed at maximum 240 after a remote upgrade.
- Twins harness: 58 production runtime/patch/cooperation checks passed. Includes
  remote and host owners, maximum-only changes, one native heal, stale/duplicate
  maximum packets, full-health packets arriving before the item event, both
  recipients, multiple levels, clamped decreases/no-ops, unlink, non-twins,
  authority/stage guards and linked death/revival.
- Final Release build583: zero warnings/errors. Installed game field checks:
  627; v0.5.0 call/Harmony/reflection contracts: 615, including the new hook.
- The test adapter calls the production prefix at the native event point.
  Networking/game objects are simulated; live gameplay and host-only multiplayer
  were not tested.
- README remains 99,826 characters. Seven ZIP entries match package files;
  all 47 pinned GitHub icons verified. No player-facing documentation rewrite
  or same-version changelog note was added for this correction.

## Artifacts and pending deployment

- R.E.P.O. was running. RSO_TEST was not overwritten and its configuration is
  unchanged. Deployment is pending until the game exits.
- The preceding build582 ZIP and package DLL are preserved at:
  tmp/deployments/before-build583-20261103-030716.
- Prepared deployment script: tmp/deploy-build583.ps1. Its process guard checks
  that the game is stopped before replacing any RSO_TEST file; it also backs up
  the profile/configuration and verifies all seven copied files.
- DLL SHA-256: d5a7253c8e54ecbde4b3113f7f0157efe04c9287ca2b5cc9c9c185fa9cbe7231.
- ZIP SHA-256: fd3a04dab222fe66afd5417f9a3a0d2d22597cb4f0d6842ac8aef2565ef3463a;
  5,638,235 bytes. RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip match.
- No GitHub release or Thunderstore publication was performed.
