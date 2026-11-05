# MenuLib 2.6.2 update — RoleShuffle v5.0.1

Date: 2026-11-05. UI build: 599. Branch: `version/5.0.1`.

## Dependency and source

Latest active version 2.6.2 was confirmed with the official
[Thunderstore API](https://thunderstore.io/api/experimental/package/REPOModding/MenuLib/).
Published: `2026-11-05T12:15:27.699643Z`.
Downloaded the official [2.6.2 package](https://thunderstore.io/package/download/REPOModding/MenuLib/2.6.2/).

- ZIP SHA-256: `fbb37a20cd5e51e24f51eec5b87d2e368879241e047c927685cb4f14f8e7e17e`.
- DLL SHA-256: `173c7e41193ff1a7f9b1bbbc93cdb917860a6d37e389802ffa2f05223647982c`.
- Reference assembly: `MenuLib, Version=2.6.2.0`.
- Updated tracked `lib/MenuLib.dll`, runtime BepInEx minimum, package dependency,
  current installation/build instructions and release guide.
  Historical verification records and previously published ZIPs remain unchanged.
- Reviewed 2.6.0 to 2.6.2 UI changes: bounds checking for controls, label anchors,
  slider labels/values, toggle positioning and popup-header overflow. Existing
  RoleShuffle layout overrides require no code changes. No dependency is bundled
  inside the RoleShuffle package; MenuLib remains a separately installed requirement.

## Validation

- Release build 599: zero warnings and errors, against installed R.E.P.O. v0.5.1.
- RoleShuffle: 670 game call/Harmony/reflection checks, 723 field references,
  15 native localization-refresh contracts passed.
- MenuLib: 150 game call/shared-reflection checks and 65 game field references passed.
- All 225 old public MenuLib API entries are retained. All 19 MenuLib reflection
  bindings exist in the installed game, including `MenuButton.menuScrollBox`.
- Compiled RoleShuffle reference, BepInEx minimum and manifest all require 2.6.2.
- ScrollChecks: 243; GameUiChecks: 787; bilingual documentation checks: 1,590.
- README remains 49,801 characters and UTF-8 without BOM. Changelog headings
  retain their required spacing. `git diff --check` passed.
- Unity rendering, menu interaction and multiplayer were not exercised in-game.

## Deployment

RSO_TEST updated at `2026-11-05T14:29:26.3268064Z` while the game was closed.
Seven RoleShuffle files and five MenuLib files were hash-verified. Profile
`mods.yml` now records MenuLib 2.6.2 and RoleShuffle 5.0.1 with the new dependency;
other entries retain their metadata. Gameplay configuration was not modified.

Backup: `tmp/deployments/before-build599-20261105-142925`, including the previous
plugin folders, config, profile metadata and MenuLib build reference.

RoleShuffle DLL SHA-256:
`238cf73781caed935555741421af1e242bed782b3d0bc388113799c35e9c74aa`.
Existing v5.0.0 release ZIP SHA-256:
`8922f890529184dd953bc488f767d4b41a9256201f6a042bc168f93260211a32`.
