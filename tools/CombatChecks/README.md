# Combat and Ghost support checks

Run from the repository root:

```powershell
python tools/Prepare-CombatChecks.py
dotnet run --project tools/CombatChecks/CombatChecks.csproj -c Release
dotnet run --project tools/RoleSettingsChecks/RoleSettingsChecks.csproj -c Release
dotnet run --project tools/OverhaulChecks/OverhaulChecks.csproj -c Release
dotnet run --project tools/LocalizationChecks/LocalizationChecks.csproj -c Release
python tools/Test-CombatLocalization.py
pwsh -NoProfile -File tools/Test-BaseUpgradeEligibility.ps1
```

Use the configured Python runtime; the localization script also requires fontTools.
Preparation extracts the actual Brawler, Avenger and Sniper damage methods into
ignored `tmp/combat-checks/Extracted.cs`. Tests compile those methods alongside
the production combo rules, Ghost runtime, capped healing dispatcher and upgrade
aura ledger. Game objects, health responses, time and networking are stand-ins.

Coverage includes combo progression, blocked/corpse hits, duplicate callbacks,
target/time resets, low custom increments, unchanged friendly-fire scaling,
Sniper distance boundaries, Avenger hit conditions/cooldown and non-stacking
bonuses, Hunter reward guarantees, Ghost shared healing reservations across
revival, and temporary carrier Speed cleanup without removing purchased levels.
Other suites check real BepInEx configuration migration, localized effective
values, HUD codec and pictograms, and high-Base Ghost eligibility.

These checks do not establish live gameplay or unmodded-client interoperability.
