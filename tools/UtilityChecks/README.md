# Player utility checks

Run `dotnet run --project tools/UtilityChecks/UtilityChecks.csproj -c Release` from the repository root.

The checks link production code for bounded draw-history storage and serialization, snapshot hashes and timestamp rollover, report redaction, and HUD geometry across resolutions. They do not simulate Unity input, TMP rendering, real Photon transport, or a game save on disk.

Also run `dotnet run --project tools/UtilityRuntimeChecks/UtilityRuntimeChecks.csproj -c Release`. This harness links the runtime service with deterministic game/network stand-ins. It covers initial publication, unchanged heartbeats, refresh requests, mismatched data, delayed updates, version differences, host migration, reconnects, host-only history recording, real UTF-8 report file I/O, privacy masking, bounded logs, and failed file writes. Four-part version metadata must remain intact even when the same string in a log is treated as an IP address. Generated reports stay under the ignored test build output.

Before releasing, verify in the game:

1. Open TOOLS from both the lobby and Escape menu. Open the HUD editor, drag slowly and quickly, try all display modes, anchors and font/icon sizes at 16:9 and 4:3. Confirm the outlined pointer stays above the editor and its tip matches button/drag hit tests. Alt-tab out and back; dragging must stop while unfocused. Save, reopen, cancel, and reset. Confirm that Escape returns to the Roles page, the normal game cursor returns, and underlying menu buttons cannot activate while editing.
2. Select Japanese, reopen the menu and restart the game. Confirm the guide and utility pages restore Japanese; another player's setting remains independent.
3. Connect two modded clients and a vanilla guest. Change host settings, perform a draw, request display refresh, reconnect the guest, and change hosts. Confirm displayed synchronization and history; stop host updates to check delayed status. Test an older host without status metadata.
4. Perform positive, negative, zero, All Upgrades and capped draws. Cancel a draw before applying it. Reload the host's save, then select a different save. Confirm only completed draws in the selected run appear, with actual capped values.
5. Open the report menu before generating any report: both copy and open must be available; no create button or report preview should appear. Copy, change a setting, then open a report and copy again. Each action must save a fresh file with current data; clipboard contents must match that action's file. Check useful versions/settings/logs, privacy masking, and failure feedback for an unwritable report directory. Open GitHub Issues: only the dialog's confirm button may open a browser. Cancel and Escape must keep the report menu open; repeat clicks must not open multiple tabs. Check translated dialog text in all languages. Nothing is uploaded; reproduction steps are added manually.
