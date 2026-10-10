# Porter verification

Run `dotnet run --project tools/PorterChecks/PorterChecks.csproj -c Release`.
The suite compiles the production rules and runtime against deterministic game
stand-ins. It exercises weight/size boundaries, continuous solo holding,
proportional unloading, interruption, enemy spills, same-item notice suppression,
full-capacity and unload-completion notices, rollback/retry, and lifecycle returns.
There is no real Photon transport, Unity scene or live player in these checks.

After the main build, run `pwsh -NoProfile -File tools/Test-PorterNativeContract.ps1`
and `tools/Test-GameFieldAccess.ps1` against the installed unmodified game assembly.
Run `tools/Test-PorterLocalization.py` with fontTools available for translation,
placeholder and bundled-font coverage. The normal role-settings, localization,
combat routing and HUD suites also cover the new role.

Live follow-up: assign Porter to a vanilla guest, store several different-size
valuables, confirm their value after unloading, test an enemy hit and death,
interrupt unloading by leaving the area, change roles, and finish a stage.
