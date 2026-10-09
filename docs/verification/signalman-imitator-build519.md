# Signalman copy exclusion — v4.5.4 build519

Signalman is excluded by `RoleCatalog.CanBeCopiedByImitator`. The existing grab
handler returns through its `CannotCopy` notification path before changing the
Imitator's effective role or upgrades. The player can still copy a later eligible
target. The same predicate prevents an Imitator draw when Signalman is the only
other assigned role, including relaxed fallback draws.

Superbot's Signalman capability is unchanged. English and Japanese role guides,
the 12 other translations, and the public role list include the exclusion.
This supersedes the copy-eligibility statement in the historical build517 report.

## Validation

- Catalog/planner checks: 81 passed, including Signalman rejection, absence of an
  eligible copy target, and successful Imitator selection when Tank is also present.
- Localization checks: 13,215 passed.
- Release build519: zero warnings/errors.
- Installed-game field access: 538 references passed.
- README: 99,962 characters, UTF-8 without BOM; CHANGELOG: 18,432 characters.
- RSO_TEST: seven files verified against the package while the game was closed.
- No live grab/UI or multiplayer test was performed.

## Deployment

- UTC: 2026-10-09 12:16:51.
- Build/package/deployed DLL SHA-256:
  `dc631cf40d7bba1db4b843a1ca1154f6c17e499225aa6bb74c201b918eaf30dc`.
- Backup: `tmp/deployments/before-build519-20261009-121651`.
- Pre-build backup: `tmp/versions/before-build518-20261009-121608`.
- Existing profile settings and previous release ZIPs were preserved.
