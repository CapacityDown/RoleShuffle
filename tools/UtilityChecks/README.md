# Player utility checks

Run `dotnet run --project tools/UtilityChecks/UtilityChecks.csproj -c Release` from the repository root.

The checks link production code for bounded draw-history storage and serialization, snapshot hashes and timestamp rollover, report redaction, and HUD geometry across resolutions. They do not simulate Unity input, TMP rendering, real Photon transport, or a game save on disk.

## Multibyte HUD display command

Enable `Testing.Enabled` locally. The regular command `/hudmultibyte` and short form `/hmb` use the same handler.

- `/hmb`: preview six players including YOU, with Japanese, Simplified Chinese, Traditional Chinese, Korean and Cyrillic names.
- `/hmb 12`: preview a specified count from 1 to 30 to check paging.
- `/hmb reset`: restore the previous HUD data.

View the stage HUD, or open `ROLES` → `TOOLS` → `HUD EDITOR` in the lobby or stage to see the same sample. The heading is `ROLES [TEST]`. Stage display follows `HUD.Enabled` and the normal stage visibility rules. To see six rows simultaneously, set `HUD.PlayersPerPage` to 6 or higher (default 8). Compare NameOnly, IconAndName and IconOnly; test all anchors and high font/icon sizes. Long names should shorten before the role name, and all six rows should remain on screen. The editor's SAVE/CANCEL still apply only to local layout settings.

This preview is read only by the HUD and HUD editor. It never changes assignments, CURRENT ROLES, command target numbers, save data, reports or network payloads. It clears on scene, room, save or local-player changes, when testing is disabled, on shutdown, or with `reset`. Invalid arguments preserve the current preview. Multiplayer participants may use it locally.

Run `pwsh -NoProfile -File tools/Test-HudMultibyteCommands.ps1` to check the production registration methods, both aliases, Testing permission checks, suggestions and callbacks. OptimizationChecks compiles the production preview store to verify default counts, invalid arguments, reset, context changes and separation from real assignment data. UtilityChecks verifies six-row geometry across fallback-font heights and Unicode-safe width truncation using synthetic glyph measurements; actual TMP rendering remains a game check.

Build 437 (v4.4.7), 2026-09-13: production build passed with no warnings or errors. UtilityChecks: 6,654; HUD command callbacks: 21; OptimizationChecks: 108; UtilityRuntimeChecks: 69; RoleSettingsChecks: 241 role, 156 Base Upgrade selection and 134 save-adjustment checks, all passing. Actual game rendering is left to the user; no game controls were operated.

Also run `dotnet run --project tools/UtilityRuntimeChecks/UtilityRuntimeChecks.csproj -c Release`. This harness links the runtime service with deterministic game/network stand-ins. It covers initial publication, unchanged heartbeats, refresh requests, mismatched data, delayed updates, version differences, host migration, reconnects, host-only history recording, preview arguments/variety/reset/isolation, real UTF-8 report file I/O, privacy masking, a 500-entry log window including long messages, and failed file writes. Four-part version metadata must remain intact even when the same string in a log is treated as an IP address. Generated reports stay under the ignored test build output.

## Internal draw-history display command

Enable `Testing.Enabled` in the local MOD settings. Enter `/drawhistory` (short form `/dh`) in the game's command input, then open `ROLES` → `DRAW HISTORY`. Both command names use the same handler and require the testing setting.

- `/dh` or `/drawhistory`: show 50 sample draws, newest first.
- `/dh 10`: show a chosen count from 0 to 50. Use `/dh 0` to inspect the empty state.
- `/dh reset`: restore the actual history. `reset` is case-insensitive.

Samples cover individual upgrades, All Upgrades, increases, decreases, zero results and capped results. The menu marks them as a local test preview. They never change upgrades, saved history, network history or the report's actual draw records. Invalid arguments leave the existing preview intact. The preview clears when the scene, room or save changes, when testing is disabled, or when the mod shuts down.

## Game checks

Before releasing, verify in the game:

1. Open TOOLS from both the lobby and Escape menu. Open the HUD editor, drag slowly and quickly, try all display modes, anchors and font/icon sizes at 16:9 and 4:3. Confirm the outlined pointer stays above the editor and its tip matches button/drag hit tests. Alt-tab out and back; dragging must stop while unfocused. Save, reopen, cancel, and reset. Confirm that Escape returns to the Roles page, the normal game cursor returns, and underlying menu buttons cannot activate while editing.
2. Select Japanese, reopen the menu and restart the game. Confirm the guide and utility pages restore Japanese; another player's setting remains independent.
3. Connect two modded clients and a vanilla guest. Change host settings, perform a draw, request display refresh, reconnect the guest, and change hosts. Confirm displayed synchronization and history; stop host updates to check delayed status. Test an older host without status metadata.
4. Perform positive, negative, zero, All Upgrades and capped draws. Cancel a draw before applying it. Reload the host's save, then select a different save. Confirm only completed draws in the selected run appear, with actual capped values.
   Run both `/drawhistory` and `/dh` with the examples above in the lobby and during a run. Check the test marker, 0/1/10/50 rows, language switching, scrolling and reset. Verify the command is unavailable with `Testing.Enabled = false`; disabling testing or changing scenes clears samples. A second client must keep showing actual history while the preview is active.
5. Open the report menu before generating any report: both copy and open must be available; no create button or report preview should appear. Copy, change a setting, then open a report and copy again. Each action must save a fresh file with current data; clipboard contents must match that action's file. Check useful versions/settings/logs, privacy masking, and failure feedback for an unwritable report directory. Open GitHub Issues: only the dialog's confirm button may open a browser. Cancel and Escape must keep the report menu open; repeat clicks must not open multiple tabs. Check translated dialog text in all languages. Nothing is uploaded; reproduction steps are added manually.
