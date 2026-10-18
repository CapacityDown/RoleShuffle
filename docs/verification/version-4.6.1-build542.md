# v4.6.1 boundary and build542

Date: 2026-10-19 JST. Current branch: `version/4.6.1`.

## Version boundary

The user assigned the Courier difficulty easing and all subsequent work to v4.6.1.
v4.6.0 is the previous build540 feature set; v4.6.1 includes the longer Courier
pause periods. No ability values changed during this version correction.

Before changing branches, all Git refs were saved in a verified complete bundle,
and the incorrectly 4.6.0-labelled build541 ZIP/DLL/record were copied to
`tmp/versions/before-4.6.1-20261018-154359`.

- `75251806c0cc9435621fdf3d26153129abeeeaf4`: outgoing version/4.6.0 and main.
  Its complete source tree equals the verified build540 boundary at `da67633`.
  The outgoing branch and main were normally pushed and remote main was verified.
- New `version/4.6.1` was created from that main. The Courier change was carried
  forward as `fd8024b`; the original `e23dece` remains recoverable in history.
- Plugin, assembly, package manifest and current release-guide examples are 4.6.1.
  UI build is 542. Courier's schema-43 backup name now identifies v4.6.1; schema
  remains 44 so already-migrated settings are not retuned.
- The Courier bullet alone is under CHANGELOG 4.6.1. All 4.6.0-and-earlier text
  exactly equals main's CHANGELOG. README retains the approved Courier values.
- Full/public icon catalogs were generated with v4.6.1 labels, retaining all
  artwork and public secret concealment. v4.6.0 catalogs are retained.

## Validation

- Release build542: 0 warnings, 0 errors. Assembly version 4.6.1.0 confirmed.
- Installed game field access: 609 checks.
- Courier migration: 359; ability configuration: 350; combat migration: 23;
  Hunter migration: 72; role settings: 349; Base Upgrade settings: 128;
  save adjustments: 248. All passed.
- README: 98,081 characters; CHANGELOG: 20,677, UTF-8 without BOM and LF.
  No blank line follows a version heading. README text equals the approved
  Courier revision; its 45 pinned public icon references and local image hashes
  still match the earlier anonymous-download verification record.
- ZIP: exactly seven required files, valid CRC, every entry SHA-256 matches its
  package source. No source, configuration, log, PDF or enclosing folder.
- `git diff --check` passed. Actual gameplay and host-only multiplayer are untested.

## Artifacts and deployment

v4.6.1 build542:

- DLL SHA-256: `638c432e5bc5257d83afdb40ae6125a0f4123afbf48de249a784a4f958a1cff1`.
- ZIP SHA-256: `228dc0a6cd15fd58b55c527dacd1244c0bf57c2e2055771db1b77353cceb5406`.
- ZIP size: 5,170,791 bytes.
- Current upload archive: `RoleShuffle.zip`.
- Versioned archive: `output/release/RoleShuffle-4.6.1.zip`.

v4.6.0's versioned archive was restored to the verified build540 ZIP, SHA-256
`260b05c258c5db1682ccb5f0150f74206200878b6aa8a3426f97802823df7497`, and kept at
`output/release/RoleShuffle-4.6.0.zip`. Its manifest, README and CHANGELOG match
main/build540 exactly. The earlier incorrectly labelled build541 ZIP is backed up.

RSO_TEST was updated with the game stopped at 2026-10-19 00:46 JST. All seven
profile files matched the package hashes. Previous profile files are retained in
`tmp/deployments/before-build542-20261018-154647/RSO_TEST`; profile configuration
was not edited. The new settings migration runs on the next plugin startup.
