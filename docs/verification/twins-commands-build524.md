# Explicit Twins test targets — v4.5.4 build524

Date: 2026-10-10 (JST). Branch: `version/4.5.4`.

## Internal test command

- `/setrole Twins 1 2` assigns Twins to the two specified player numbers in one operation. `/sr 43 1 2` uses the existing command and numeric-role aliases. Steam IDs may replace either player number; unambiguous single-token names also resolve.
- Both targets are required. Missing/extra targets, `all`, duplicate identities, missing/ambiguous players, dead players and conflicts with an existing pair are rejected before any role, HP or upgrade mutation. Reapplying to the same two living members is allowed. To select a different pair, clear the existing pair with an ordinary all-player role assignment or randomize all first.
- Removed automatic partner selection from the single-target controller entry point. The all-player entry point also rejects Twins, even in a two-player party. Random draws retain their existing pair-selection behavior.
- Completion offers player numbers for both targets, omits `all`, and excludes the first target when it was specified by number or Steam ID.
- Exact duplicate names now fail resolution instead of selecting the first match. Ordinary multi-word player-name targeting, other roles and random commands retain their existing syntax.

## Verification

- Release build: **0 warnings, 0 errors**.
- `tools/Test-TwinsCommands.ps1`: **41** checks compile the production command parsing, completion, controller assignment and player resolution methods with game stand-ins. Covers atomic assignment, non-adjacent targets, role alias43, Steam IDs, invalid requests without mutations, existing pairs, duplicate names, authority rejection and ordinary-command regressions.
- `tools/TwinsChecks`: **32** production runtime/cooperation checks.
- `tools/Test-SecretRoles.ps1`: **90** catalog/planner/migration checks.
- `tools/Test-GameFieldAccess.ps1`: **557** compiled field references match the installed game assembly.
- Live game and vanilla-guest multiplayer tests were not performed. The previously accepted host-only shared-HP synchronization limitation remains.

Build DLL SHA-256:

`3a007d25e2d9a54e7a58fabb4e84240f54a0bc1bc06d879e3d70dba9300fe184`

The previous build and package DLLs were backed up before building under `tmp/build-backups/before-build524-20261009-164442`. Public documents, artwork and existing release ZIPs are unchanged.

RSO_TEST deployment completed with the game stopped; all seven copied files passed hash verification. Build, package and deployed DLLs share the hash above. Previous package/profile files and the deployment record are retained under `tmp/deployments/before-build524-20261009-164526`.
