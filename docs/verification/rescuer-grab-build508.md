# Rescuer grab revival — v4.5.4 build508

Date: 2026-10-08 (Asia/Tokyo)

## Behavior

- Rescuer revives the dead teammate whose Death Head the rescuer is currently
  grabbing. Moving and rotating heads are eligible; proximity alone is not.
- The target is matched against the game's `PhysGrabObject.playerGrabbing` list
  using the rescuer's `PlayerAvatar.physGrabber`. The installed game's grab RPCs
  update this list on the host, including grabs from unmodded participants.
- Grabbing is checked again immediately before requesting revival. Releasing
  during the configured delay or head setup leaves no queued revival or used charge.
- Kept the configured death delay, revival HP, stage-use limit, pending-request
  deduplication, and Phoenix / Stage Flux Second Chance priority.
- Removed the obsolete `Rescuer.Radius` binding and public setting description.
  Reach is determined by the player's normal grab capability. Existing user
  configuration files were not edited.
- Phoenix still requires a stationary head and prevents game over without a
  timeout while a revival remains available or pending.
- Updated the role guide in all 14 languages and both README catalogs. Historical
  translation keys remain available for descriptions from older hosts.

## Verification

- Production-method regression harness: 6,594 assertions passed. Added cases for
  nearby unheld heads, another player's grab, moving heads beyond the old radius,
  early release, regrabbing, use limits, final-use HP, two rescuers sharing a head,
  delayed remote acknowledgement, head setup and revival priority.
- Localization: 13,071 checks passed.
- Release build: zero warnings and errors; compiled version 4.5.4.0, UI build 508.
- Game field access: 518 references passed against the installed game assembly.
- README: 99,234 characters; CHANGELOG: 17,657; both UTF-8 without BOM.
- Build, package and RSO_TEST DLL SHA-256:
  `1984ea73a8b43f8e9372b409db7201e08be2824940dfd72dec5ad83b6dbb1168`.
- RSO_TEST received all seven package files with the game closed; every file
  matched by hash. Previous profile files are preserved under
  `tmp/deployments/before-build508-20261007-183230`.
- The v4.5.3 release ZIP remains unchanged; no new release ZIP was created.

Physics and transport regression tests use stand-ins. No fresh in-game or
host-only multiplayer test was performed.
