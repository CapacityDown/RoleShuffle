# v4.4.3 optimization checks

This internal harness links the production `RoleAssignmentSync.cs` and `RoleGuideSync.cs` directly. Photon, player identity, and role descriptions are replaced with deterministic test stubs. No external packages are required, and nothing from this directory belongs in the public ZIP.

Run with the installed .NET 9 SDK:

```powershell
dotnet restore tools/OptimizationChecks/OptimizationChecks.csproj --configfile tools/OptimizationChecks/NuGet.Config
dotnet run --project tools/OptimizationChecks/OptimizationChecks.csproj -c Release --no-restore
```

## Result: 2026-09-05

39 assertions passed, covering:

- Thirty player entries, Unicode/delimiter-containing names, assigned/effective roles, read-only snapshots, unchanged-payload reuse, reassignment and rename invalidation.
- Preview size limits, local identity/name changes, clearing preview state and assignment state.
- Legacy and malformed assignments, guest reads, and changing rooms.
- Unchanged English/Japanese/legacy guide wire formats, cache invalidation, matching host settings on clients, language changes, legacy/generic fallbacks, recovery after malformed data, and room exit.
- Identical guide publication suppression and publication into a new room.

Allocation check, 5,000 warmed reads on this harness:

| Work | Cached | Forced cache invalidation on every read |
|---|---:|---:|
| 30 assignment snapshots | 0 bytes | 71,561,600 bytes |
| Guide signature, stub role text | 0 bytes | 38,161,600 bytes |

The uncached comparison intentionally invalidates the optimized cache; it is not an execution of the old DLL. These numbers demonstrate removed recurring allocations, not an in-game speedup percentage. The guide test uses four stub roles and is not a full-game guide-size benchmark.

## Source audit

- All assignment additions/clears pass through index-maintaining helpers. The sole indexed removal and the avatar-replacement path update both indexes. Restored/new players register before runtime activation; effective role changes are read directly from the indexed assignment object.
- Utility scans remain at most once per original 0.25-second tick and are skipped without eligible repair/charge capability holders. Ninja maintenance and existing repair/charge methods remain intact.
- Participant checks remain 0.5 seconds with 10 consecutive misses; no changes were made to Jobless/Tuna timing, role cooldowns, revival logic, notification arbitration, or compatibility APIs.

## Remaining live checks

The game was not running during this pass. These tests cannot verify Unity frame time, native object lifecycle callbacks, Photon replication, or multiplayer behavior. Before public release, compare the same stages at 1/4/10/20/30 players, with vanilla guests and StageFlux/EEV, and exercise role changes, reconnect/revival, Engineer suppression, Mechanic/Electrician limits, HUD/menu language switching and Superbot revelation.

## Build and deployment

- RoleShuffle v4.4.3, RoleUI build-358. Release build: 0 warnings, 0 errors.
- Build, package, and Default-profile DLLs have identical SHA-256:
  `8766A9CE4300A3BC37066BDFDD105DA0EB03BA4146F4C7AA98956FADD2A80EAF`.
- Previous package/Default DLLs are preserved in `output/deploy-backups/before-4.4.3-build-358`.
- The release ZIP was not rebuilt during this implementation pass.
