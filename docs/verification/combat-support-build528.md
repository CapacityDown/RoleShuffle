# Five-role improvements — v4.6.0 build528

Date: 2026-10-10 JST. Branch: `version/4.6.0`.

## Scope

The user confirmed Ghost, Avenger and Hunter together with Brawler and Sniper.

- Ghost retains Death Head Battery level 50. While dead with an active, set-up
  Death Head, pulses 1 HP per nearby living ally every 2 seconds within 5 m.
  One 50 HP stage budget covers all recipients and survives revival, role changes
  and same-stage reconnection. Remote heals reserve budget conservatively using
  the shared healing dispatcher. Head carriers receive temporary Speed +1;
  multiple heads do not stack. The shared aura ledger uses the stronger of Ghost
  and King Speed grants and preserves upgrades acquired while carrying.
- Avenger reacts to an ally losing at least 15 HP to one confirmed enemy hit
  within 15 m: 1.25x enemy damage for 10 seconds, with a 20-second cooldown.
  Existing nearby-death range/multiplier remain 30 m/1.5x, now for 30 seconds.
  Independent timers select the stronger multiplier without multiplication.
  Existing hit-origin validation excludes self/friendly/environment damage.
- Hunter retains 75% battery use, 10% double-orb chance and 0.5% chance of 10
  orbs. Every two qualifying credited kills guarantees at least one extra orb;
  random extra orbs satisfy the guarantee. Only actual vanilla orb drops count.
  Existing enemy-tier reward handling and stage-scoped attack credit are retained.
- Brawler starts at 1.25x melee damage, adds 0.25 per consecutive confirmed
  damaging hit against the same enemy, capped at 2x. Target changes, four seconds
  without a successful hit, death and role changes reset the combo. Same-frame
  callbacks reuse one step. Friendly-fire and ranged multipliers remain fixed.
  The effective cap never falls below the configured starting multiplier.
- Sniper defaults: 0.5x at zero distance, 1x at 6 m, 2x from 18 m. Existing
  exclusions and custom multipliers remain unchanged.

New settings are bounded REPOConfig entries. Schema 41 replaces only old default
Sniper distances and Avenger death duration, preserves custom/future-schema
values, and backs up schema-40 files as `.pre-v4.6.0-combat.bak`. Deployment did
not edit the profile configuration; migration runs when the mod next loads.

## Verification

- Main Release build528: zero warnings/errors; automatic profile copying disabled.
- Installed original-game field access check: 565 compiled references passed.
- Reflected native fields checked: `EnemyHealth.healthCurrent` (int),
  `PlayerAvatar.deadSet` (bool), `PlayerDeathHead.setup` (bool).
- Native `EnemyHealth.Hurt` IL inspected: authority updates HP synchronously;
  `HurtCollider.EnemyHurt` returning true alone does not establish damage.
  Attack credit and combo confirmation therefore compare HP before and after.
- Combat production rules/runtime checks: 73 passed, plus extraction/routing checks.
- REPOConfig ability bindings/ranges/persistence: 330 passed.
- Combat config migration/backup/idempotence: 23 passed.
- Existing role settings: 341; Base settings: 128; saved adjustments: 248 passed.
- Upgrade aura, healing locks, HUD resources/codec/layout regressions: 76,235 passed.
- Base eligibility/target checks: 13,556 passed, including high-Base Ghost.
- Tracker regression checks: 59 passed.
- Full localization checks: 13,446 passed.
- Eight updated guide templates across 13 catalogs, bundled glyphs and document
  checks: 327 passed; Japanese descriptions are authored directly in source.
- README: 99,869 decoded characters; CHANGELOG: 19,677. UTF-8 without BOM,
  LF newlines, no blank line after version headings. All configuration keys,
  defaults and ranges were retained while condensing repeated prose.

Tests use stand-ins for scene objects and transport. No live game session,
visual in-game HUD review or unmodded-guest multiplayer test was performed.
Remote Avenger hits reuse conservative origin matching: overlapping or delayed
health reports can be ignored when the source cannot be established reliably.

## Artifacts and rollback

Final DLL SHA-256:
`4c8bc03e902ad4ccaaf13fec382760522f7e47c086414cd86be467d53fe98aa8`.

The prior stable build526 DLLs are preserved in
`tmp/build-backups/before-build527-20261009-180818` and the prior profile in
`tmp/deployments/before-build527-20261009-180931/RSO_TEST`.

Build output, package and RSO_TEST were verified with SHA-256 equality; all seven
package files were checked during deployment. R.E.P.O. was not running and was
not stopped by this task. The deployment log is retained under
`tmp/deployments/before-build528-*/deployment.json`.

The existing v4.5.3 `RoleShuffle.zip` is unchanged:
`921a52df372166f6a63f5c5d017b744bcdf802ed20319a01c5fbd72d49cdf109`.
No release ZIP, PDF or icon artwork was requested or regenerated.
