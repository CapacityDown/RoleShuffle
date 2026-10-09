# Imitator copy rejection notification — v4.5.4 build511

## Behavior

- An uncopied Imitator grabbing the health-transfer point of an ineligible
  teammate receives the short message `CannotCopy` through the existing role
  notification path. Role names are omitted, including secret role names.
- The message has no spaces, following the user's report that spaces interrupt
  notifications.
- Repeated attempts are limited to one queued notice per player per two seconds.
  A queued notice is discarded if the player has copied a role or the stage ends.
- Existing copy exclusions, successful copying and once-per-stage behavior are
  unchanged. Rejected attempts do not change roles or upgrades.
- The existing automatic-notification setting still controls this role notice.
  The notification uses the existing vanilla chat/TTS path for vanilla guests.

## Verification

- Reviewed the grab-start callback, rejection branch, cooldown and conditional
  notification validity against the existing success path.
- Release build: 0 warnings, 0 errors; UI build 511.
- Installed-game field-access validation: 523 compiled references pass.
- Release Markdown validation: README 99,529 / 100,000 characters;
  CHANGELOG 17,898 / 100,000; UTF-8 without BOM.
- No fresh in-game or host-only multiplayer test was performed.

## Deployment

- Build, package and RSO_TEST DLL SHA-256:
  `c7dcc76d8c30ef68947380cfe3b3898a720573634c361f7c7af95e4901a48efb`.
- All seven profile package files verified; deployment performed with R.E.P.O.
  closed. Configuration unchanged; published release ZIPs were not rebuilt.
- Previous DLLs: `tmp/versions/before-build511-20261009-104752`.
- Previous profile/package: `tmp/deployments/before-build511-20261009-104806`.
