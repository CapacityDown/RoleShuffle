# Courier grace extension — build541

Date: 2026-10-18 JST. RoleShuffle v4.6.0, branch `version/4.6.0`.

The user requested substantially easier Courier play and selected longer grace
periods as the primary change. The HP-loss rate, fixed healing rewards, required
carry distance, one reward per valuable per player and unlimited deliveries remain.

| Pause | Previous default | New default |
|---|---:|---:|
| Stage start / revival | 30 s | 120 s |
| Tiny delivery | 30 s | 90 s |
| Small delivery | 30 s | 120 s |
| Medium delivery | 60 s | 180 s |
| Big / Wide / Tall delivery | 90 s | 240 s |
| VeryTall delivery | 120 s | 300 s |

Schema 44 upgrades only exact previous default values for these eight keys,
including decimal/exponent representations. Legacy Jobless keys move before this
comparison. Zero and non-default durations remain unchanged. Current/future schemas
are not retuned; a user can choose an old duration again after upgrading. Schema 43
gets an exact `.pre-v4.6.0-courier-grace.bak` rollback copy. Earlier backup conventions
remain compatible. The role guide already displays host-configured values.

## Validation

- Release build541: 0 warnings, 0 errors; 609 compiled game field references passed.
- Courier migration: 359 checks using real BepInEx configuration, including old
  and current/future schemas, legacy names, decimal/exponent input, zero/custom/
  mixed durations, exact backup, reload, idempotence and REPOConfig ranges.
- Existing ability configuration 350, combat migration 23, Hunter migration 72,
  role settings 349, Base Upgrade settings 128 and save adjustments 248 passed.
- Existing runtime checks 76,420 and guide/Photon transport checks 2,100 passed.
- README is 98,081 characters; CHANGELOG 20,667. UTF-8 without BOM and LF.
- The ZIP has exactly seven required files. All entries match package SHA-256,
  CRC passes, and DLL version is 4.6.0. Existing public image URLs and bytes match
  the 45 anonymous GitHub downloads verified for build540 earlier this session.
- No gameplay or host-only multiplayer test was performed.

## Artifacts and deployment

DLL SHA-256: `c230e40e2c16de0f7c0d1816111a285f7c0d2da52ad71aa4a04845bed60d40bc`.
ZIP SHA-256: `397c86070f33f7f87222d31dcc4ba048d27b4935a70b1cbfac41c12514d15cc0`.
ZIP size: 5,170,786 bytes. Updated `RoleShuffle.zip` and the local versioned copy.
Previous build540 ZIPs are preserved in
`tmp/packages/before-build541-release-20261018-125231`.
Previous build/package DLLs are retained in
`tmp/build-backups/before-build541-20261018-125058`.

RSO_TEST deployment is pending because R.E.P.O. was running. No loaded DLL or
profile configuration was replaced. The observed profile has schema 43 and all
eight old default durations, so upgrading will apply the new defaults on startup.
Prepared `tmp/deploy-build541.ps1` backs up the profile and verifies all seven files
when the game is stopped. Package backup is in
`tmp/deployments/before-build541-20261018-125137`.
