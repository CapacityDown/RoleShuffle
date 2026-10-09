# v4.5.4 contents relabeled as v4.6.0

Date: 2026-10-10 (Asia/Tokyo).

- Integrated `version/4.5.4` into `main` by fast-forward and verified the remote at `82dfbe5d6ebe479a6ad4c9aa4260815d26ddd91d`.
- Created `version/4.6.0` from that commit. The old version branch and historical verification records remain intact.
- Updated the project, plugin, package manifest and current working/release guides to v4.6.0. Incremented the UI counter to build525 for the build.
- Renamed the existing unpublished CHANGELOG heading from4.5.4 to4.6.0. Its content is otherwise identical; no separate4.5.4 entry was retained. Gameplay code, settings, player-facing README and artwork are unchanged.
- Release build: **0 warnings, 0 errors**. Mono.Cecil verified assembly version4.6.0.0, plugin version4.6.0, BepInEx metadata and UI build525. Project, manifest and newest CHANGELOG versions agree.
- Game-field verification: **557** compiled references passed against the installed game assembly.
- Markdown verification: README **98,248/100,000** decoded characters; CHANGELOG **18,661/100,000**. Both are UTF-8 without BOM. Version headings have no blank line before their first bullet.
- Backed up the old package and build DLL before modification under `tmp/versions/before-4.6.0-20261009-164706`.
- Deployed all seven package files to RSO_TEST while the game was stopped; every copied file matched by hash. Previous profile/package files and deployment record are under `tmp/deployments/before-build525-20261009-164830`.
- Build, package and deployed DLL SHA-256: `99c74b6af31e7c2f0bdc4fabab889b90640f335850291de00a0e930354a22851`.
- Existing v4.5.3 `RoleShuffle.zip` remains unchanged: `921a52df372166f6a63f5c5d017b744bcdf802ed20319a01c5fbd72d49cdf109`. No new ZIP was requested or created.
- No fresh game or multiplayer test was performed; gameplay is carried over from build524 without changes.
