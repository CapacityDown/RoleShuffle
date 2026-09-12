# Role settings checks

```powershell
dotnet run --project tools/RoleSettingsChecks/RoleSettingsChecks.csproj -c Release
```

Compiles the production role identifiers, configuration bindings, migration, preset definitions, settings service and role-settings page callbacks. Uses real BepInEx configuration files in ignored build output and lightweight stand-ins for Unity, Photon and menu layout.

Checks all 42 role-to-setting mappings, five complete presets, the default selection, beginner/cooperative exclusions, preservation of weights and other settings, persistence after reload, and restoration of either autosave mode. UI callbacks verify enabling/disabling, keeping OFF roles visible, zero-weight warnings, Custom selection, preset navigation, guest read-only state, host-state display, missing host data, and losing host authority after a button was rendered. All-off configurations and disabled global assignment remain visible.

These checks do not render the game or simulate Photon transport. Remaining multiplayer checks require a second game client: as a participant, verify the host's selection updates and no role or preset can be changed; check the next assignment and late joins after a settings change.

## Verification on 2026-09-13

- Production build 425 (v4.4.7): no warnings or errors. RoleSettingsChecks: 241 passing checks, including all 14 localized preset labels and a locked-file save failure with rollback.
- Regression checks: LocalizationChecks 9,691; OptimizationChecks 87; UtilityRuntimeChecks 69; ScrollChecks 71, all passing.
- R.E.P.O. 0.4.4.3, MenuLib 2.5.4, Default test profile, private host lobby: clicked Tank OFF then ON in Japanese. The disabled row remained visible, enabled count changed 42 → 41 → 42, and the selection changed Standard → Custom → Standard.
- Applied Beginner from the preset page using the mouse. The page showed 19/42 enabled roles. Read the actual saved configuration: only role Enabled entries changed; weights and other settings remained intact.
- Restarted with build 425 and German UI. Beginner and 19/42 persisted. All left navigation labels, including Rolleneinstellungen, fit within the panel. RoleShuffle logged no warnings or errors.
- The new preset page responds to the shared three-body-line mouse-wheel scrolling. Japanese descriptions and preset buttons render correctly. The runtime tests use a backed-up config that is restored afterwards.
- Starting a stage with Beginner assigned Ninja, which belongs to that preset. Stage-menu clicks remain unverified: the automation's Escape input did not open the base game's pause menu, including after refocusing. No second client was available for the multiplayer checks above.
