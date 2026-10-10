# New-role presets — v4.6.0 build532

Date: 2026-10-10 JST. Branch: `version/4.6.0`.

| Preset | Signalman | Twins | Porter | Total |
|---|---|---|---|---|
| Standard | On | On | On | 46 |
| Beginner | On | Off | On | 21 |
| Cooperative | On | On | On | 19 |
| Chaos | Off | On | Off | 16 |
| Challenge | Off | Off | Off | 15 |

Twins fits shared-survival and chaos play; its linked deaths exclude it from
Beginner. Porter fits simple transport and team support. Existing role switches
are preserved until the host applies a preset. Party-size and other eligibility
restrictions still apply.

Validation: Release build532 completed with zero warnings/errors. Existing
role-settings checks passed (343), including preset application, persistence,
UI counts and preservation of unrelated settings. The same suite passed ability
settings (350), combat migration (23), Hunter migration (72), Base settings (128)
and saved Base adjustments (248). All 579 original-game field references passed.
README is 99,979 characters; CHANGELOG is 20,055, both UTF-8 without BOM.

RSO_TEST was updated with the game stopped; all seven package files matched.
Build, package and deployed DLL SHA-256:
`a8f3f3de72b5febaf77a7a16f12e5671e3678fcf59c32c61cdabed78835fdf6c`.
Prior deployment: `tmp/deployments/before-build532-20261010-091245`.
Existing configuration and release ZIP were preserved. No live-game test was run.
