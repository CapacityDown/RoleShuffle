# Player utility checks

Run `dotnet run --project tools/UtilityChecks/UtilityChecks.csproj -c Release` from the repository root.

The checks link production code for bounded draw-history storage and serialization, snapshot hashes and timestamp rollover, report redaction, and HUD geometry across resolutions. They do not simulate Unity input, TMP rendering, real Photon transport, or a game save on disk.

Also run `dotnet run --project tools/UtilityRuntimeChecks/UtilityRuntimeChecks.csproj -c Release`. This harness links the runtime service with deterministic game/network stand-ins. It covers initial publication, unchanged heartbeats, refresh requests, mismatched data, delayed updates, version differences, host migration, reconnects, host-only history recording, real UTF-8 report file I/O, privacy masking, bounded logs, and failed file writes. Four-part version metadata must remain intact even when the same string in a log is treated as an IP address. Generated reports stay under the ignored test build output.

Before releasing, verify in the game:

1. Open TOOLS from both the lobby and Escape menu. Open the HUD editor, drag slowly and quickly, try all display modes, anchors and font/icon sizes at 16:9 and 4:3. Save, reopen, cancel, and reset. Confirm that Escape returns to the Roles page and underlying menu buttons cannot activate while editing.
2. Select Japanese, reopen the menu and restart the game. Confirm the guide and utility pages restore Japanese; another player's setting remains independent.
3. Connect two modded clients and a vanilla guest. Change host settings, perform a draw, request display refresh, reconnect the guest, and change hosts. Confirm displayed synchronization and history; stop host updates to check delayed status. Test an older host without status metadata.
4. Perform positive, negative, zero, All Upgrades and capped draws. Cancel a draw before applying it. Reload the host's save, then select a different save. Confirm only completed draws in the selected run appear, with actual capped values.
5. Create a report, view/copy/open it, and open GitHub Issues. Check that the file contains useful versions, settings and recent diagnostic logs, masks player details and paths, and does not upload anything. Add reproduction steps manually before submitting.
