# Passive Tracker — v4.6.0 build526

Date: 2026-10-10 (Asia/Tokyo). Branch: `version/4.6.0`.

## Behavior

- Tracker passively detects the nearest spawned, active, living enemy within25m. Spawning/despawning enemies and pooled objects are excluded; living stunned enemies remain detectable. Native private `EnemyParent.Spawned` and `EnemyHealth.dead` fields were checked against the installed assembly and read through reflection.
- Warns when the nearest enemy enters8m. A warning bypasses ordinary-report cooldown, with its own20s minimum interval. It rearms only after every enemy has stayed beyond the warning radius plus a margin for3s. At defaults the reset boundary is11m. The rearm check includes enemies outside the configured detection radius, preventing boundary jitter when both configured radii match.
- Tracks the nearest active Death Head of a dead teammate at any distance. Reports the compact player name, direction, distance and above/below indication. Revived, disconnected and inactive targets are excluded. Distances are straight-line, rounded up; bearings are relative to horizontal view direction. Vertical hints start at3m, not a claim about actual floor topology.
- Ordinary reports share a20s minimum interval. Update triggers are target acquisition/loss/change, distance change of at least5m, bearing change of at least60 degrees, or a changed vertical hint. No empty startup reports or repeated unchanged/clear reports. Alternation prevents head reports from starving behind enemy updates.
- Reports use existing targeted vanilla chat/TTS and are sent only to the Tracker owner. They contain no spaces, use English direction/status tokens, and need no item, command or new HUD. Initial role announcements, native speech and countdown priority retain their existing ordering.
- At most one Tracker report waits per player. It samples the current scene again immediately before delivery and spends cooldown only after a successful send. Expiry, queue refusal and cancellation do not lock the state. Role changes, death, departure, avatar replacement, disabled announcements and stage end invalidate pending work.
- Host authority and ready-stage gates remain in the controller. Imitator copies Tracker normally; Superbot inherits its passive capability. Existing exclusion from solo/one-player random draws remains.
- Tracker remains eligible at high Base levels. Health uses the greater of the configured minimum(default3) and Base; Map Player Count uses at least1. Existing consumed-upgrade retention still applies.
- Added four bounded REPOConfig settings: `EnemyDetectionRange`, `DangerAlertRange`, `NotificationIntervalSeconds`, and `DistanceChangeThreshold`. A zero danger range disables proximity warnings. Effective danger range cannot exceed detection range. Existing Health key is retained with a minimum-level description.

## Verification

- Release build: **0 warnings, 0 errors**; assembly version4.6.0.0 and UI build526 verified.
- `tools/TrackerChecks`: **59** checks compile production rules/runtime against scene and transport stand-ins. Includes stale queued locations, direction sectors/wrap, warning hysteresis/cooldown, fairness, death/role/scene cleanup, failures, and enemy/head filters.
- Notification/Signalman production harness: **97** checks passed. Existing queue completion callbacks are now exposed to private notices without changing Signalman callers.
- Upgrade eligibility/targets: **13,555** checks; ability configuration: **228**; role settings: **341**; Base settings: **128**; saved adjustments: **248**.
- Localization: **13,431** checks. Tracker text/glyph checks: **117**. Updated two role templates in13 catalogs plus Japanese source descriptions; regenerated the existing bundled font subsets from the retained originals. No role artwork changed.
- Existing catalog/planner checks: **90**; Twins command checks: **41**.
- Installed game-field compatibility: **565** compiled references passed. Tracker's two private status fields were separately verified to exist with Boolean types.
- README: **99,481/100,000** decoded characters. CHANGELOG: **18,970/100,000**. UTF-8 without BOM; changes consolidated under4.6.0 with no blank line after its heading.
- No live game, visual UI or vanilla-guest multiplayer session was tested. Automated transport stand-ins do not establish actual remote playback or gameplay behavior.

## Deployment

Build/package/RSO_TEST DLL SHA-256:

`8bac609d7cfdf4e21224051ff14704c9aa0210d9bef5a3ccf659b1244e4e04d2`

Previous build/package DLLs: `tmp/build-backups/before-build526-20261009-171920`.
Previous package/profile and deployment record: `tmp/deployments/before-build526-20261009-172039`.
The game was stopped during deployment; seven files copied and individually hash-verified. Existing configuration and release ZIPs were preserved. New setting defaults bind on the next plugin load.
