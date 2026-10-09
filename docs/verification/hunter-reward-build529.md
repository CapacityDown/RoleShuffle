# Hunter reward tuning — v4.6.0 build529

Date: 2026-10-10 JST. Branch: `version/4.6.0`.

The user identified excessive additional orbs as Hunter's balance problem.
The default guaranteed-extra-orb interval is now five qualifying personal kills.
Battery consumption remains 75%; the existing 10% double-drop chance, 0.5%
jackpot chance and jackpot target of ten orbs are unchanged. Random bonus orbs
still satisfy the guarantee, and the kill count still resets each stage.

For ten kills of enemies that normally drop one orb, the expected extra count is
`10 * (0.005 * 9 + 0.995 * 0.10) + floor(10 / interval) * 0.995 * 0.90`.
This gives 5.9225 extra orbs at interval two and 3.236 at interval five: about
45.4% fewer extras. The example assumes all ten credited kills occur in one
stage and all rewards spawn successfully. It is an expectation, not a per-run
promise or a reduction in normal drops.

Schema 42 changes serialized `Hunter.GuaranteedOrbEveryKills = 2` to `5` only
for older schemas. Other values, including disabled (`0`), are preserved, as
are current/future-schema configurations. Schema-41 files are backed up to
`.pre-v4.6.0-hunter.bak`; older backup conventions remain available.

## Validation and delivery

- Main Release build529: zero warnings/errors, automatic copying disabled.
- Original-game field access verification: 565 references passed.
- Ability configuration checks: 330; Hunter migration/backup/reload checks: 72;
  combat migration checks: 23; role settings: 341; Base settings: 128; saved Base
  adjustments: 248 passed.
- README: 99,869 decoded characters. CHANGELOG: 19,678. Both UTF-8 without BOM.
  Current-version descriptions state the final five-kill interval; localized
  role guides already interpolate the configured interval.
- RSO_TEST had schema 40 with no guarantee entry. Configuration was read only;
  the new default and schema migration will apply on the next load.
- Build, package and deployed DLL SHA-256:
  `43a059bc81a449df0ae31147c502f9a575e8ed6f2f700da13bd9a5db080c36a6`.
- All seven package files verified after deployment; game was stopped.
- Prior DLLs: `tmp/build-backups/before-build529-20261009-182625`.
- Prior profile and deployment log: `tmp/deployments/before-build529-*`.
- Existing release ZIP was not regenerated. No live-game or vanilla-guest
  multiplayer test was performed for this adjustment.
