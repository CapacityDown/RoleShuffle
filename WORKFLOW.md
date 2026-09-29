# RoleShuffle working methods

The user instructed this task to inherit all working methods from
[RoleShuffle-S001](codex://threads/019fbdb5-6974-7f70-b783-b426be968fe3) and
[RoleShuffle-S002](codex://threads/01a091a3-dc1e-7463-bad9-ef2e5a5fd3d9).
These methods are retained below. Newer user instructions take precedence over
historical version numbers, deployment profiles and superseded design choices.

## Source and Git

- Work in `RoleShuffle`; preserve the original `StageRoles` checkout and previous
  release artifacts so the stable version can be restored.
- Keep the MOD name RoleShuffle. Continue the current development version (4.5.3)
  unless the user changes it. Do not revive the old overhaul setting.
- The user's standing authorization for Git operations is permanent for
  `https://github.com/CapacityDown/RoleShuffle.git` and is not limited to a
  branch. It includes local commits, merges, normal pushes (including `main`),
  and organizing branches with recoverable history. Do not ask again for each
  ordinary operation. Do not discard unrelated work or use destructive force
  pushes. This authorization does not override platform approval enforcement.
- On 2026-09-30 the user explicitly requested integration of the current work
  into `main`, including the role-safety audit, its diagnostic results and
  hashes, and the verification tools committed as `d4c2698`.
- Use `main` in the `RoleShuffle` working folder as the integrated development
  baseline. The original `StageRoles` checkout remains on
  `archive/main-before-4.5.3` at its preserved v4.4.8 state. Keep the separate
  v4.4.7 PDF worktree and release branch unless later instructed otherwise.
- Before bulk branch cleanup, save and verify a complete Git bundle and the
  branch/worktree inventory. Remove only branches already contained in `main`
  and not in use by a worktree. Keep unmerged and explicit archive/release
  branches. Publish and verify `main` before deleting integrated remote branches.

## Builds and deployment

- Increment the Role UI build number before each main build attempt. Keep the
  build number in the Role UI, and include it when reporting a build result.
- Build Release with automatic profile copying disabled:
  `dotnet build StageRoles.csproj -c Release --no-restore -p:CopyFilesToPluginOutputDirectoryOnBuild=false -p:CopyFilesToPluginOutputDirectoryOnRun=false`.
- Require zero warnings/errors and run `tools/Test-GameFieldAccess.ps1` after the
  main build. Run other checks appropriate to the change; distinguish automated
  checks from actual game and host-only multiplayer testing.
- Back up the previous package DLL, update `package/RoleShuffle.dll`, and verify
  SHA-256 equality with the build output. Deploy to **RSO_TEST**, following the
  current task's override of the earlier Default profile. Verify the deployed
  DLL against the same hash.
- Do not stop a running game or replace its loaded DLL. Prepare the files and
  state that deployment is pending until the game exits.

## Artwork

- Follow [ROLE_ICON_GUIDE.md](ROLE_ICON_GUIDE.md) for the approved hexagonal
  Semibot emblems, anatomy, category colors and secret-role handling.
- Retain selected source PNGs and generation prompts. New illustration work
  uses image generation; deterministic preservation work may use Python,
  Pillow and NumPy as approved in S002 (the user's response to the exterior
  transparency method was “進めてください”). The current instruction inherits
  that method for category-background normalization.
- Change only the requested region; preserve the cream frame, character, props,
  dark contours and true exterior transparency. Keep full-resolution masters,
  256 × 256 runtime PNGs, source/output hashes and visual comparison sheets.
- Review every affected icon on light and dark backgrounds and at HUD size.
  Check the embedded DLL resources against the approved runtime PNGs.
- Produce full and public icon catalogs in parallel. The public version hides
  secret role names and artwork behind the common unrevealed icon.

## Documents and ZIPs

Follow [RELEASE_DOCUMENT_GUIDE.md](RELEASE_DOCUMENT_GUIDE.md), including:

- Explain current player-facing behavior, without technical details or changes
  made within the same unpublished version.
- README is English followed by Japanese, UTF-8 without BOM, at most 100,000
  decoded characters. CHANGELOG is English only, with no blank line between a
  version heading and its first bullet.
- Document only `/roles` as a public role command; conceal secret role details
  in public documents. Keep a separate detailed version when requested.
- `RoleShuffle.zip` contains the five required root files and font licenses,
  without PDFs, source, configuration, logs or an enclosing directory. Preserve
  the earlier ZIP and compare every archived entry with its validated source.
- Render and inspect all pages of generated PDFs. Report automated validation
  and actual game verification separately and accurately.
