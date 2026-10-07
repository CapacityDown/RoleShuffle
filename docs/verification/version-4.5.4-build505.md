# v4.5.4 development start

Date: 2026-10-08 (Asia/Tokyo)

- Integrated `version/4.5.3` into `main` by fast-forward and verified the remote
  at `432106f1c341c0c90b748209a1bd9e5288616816`.
- Created `version/4.5.4` from that commit. Kept the outgoing branch and the
  original StageRoles checkout.
- Updated the project, plugin and package version to 4.5.4; UI build is 505.
  No gameplay behavior changed.
- Left the published CHANGELOG intact until there is a player-facing change.
- Release build: zero warnings and zero errors. Compiled assembly version
  4.5.4.0, plugin version 4.5.4 and UI build 505 were checked with Mono.Cecil.
- Game field check: 512 references passed against the installed game assembly.
- Markdown: README 99,252 characters; CHANGELOG 17,189 characters; both UTF-8
  without BOM. README icon references remain unchanged.
- Build, package and RSO_TEST DLL SHA-256:
  `2f469f989d001d7f36aefdb39c5ae92c8628842119f852705ea23094d62303c2`.
- RSO_TEST received the seven package files while the game was closed; all
  copied files matched by hash. Its previous files were backed up under
  `tmp/deployments/before-build505-20261007-180020`.
- Preserved the v4.5.3 ZIP and versioned copy without modification. ZIP SHA-256:
  `921a52df372166f6a63f5c5d017b744bcdf802ed20319a01c5fbd72d49cdf109`.
  Earlier DLLs and ZIPs were also backed up under
  `tmp/versions/before-4.5.4-3652a615`.
- No v4.5.4 release ZIP was created. No fresh game or multiplayer test was run.
