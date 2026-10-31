# Rigger verification

Run `dotnet run --project tools/RiggerChecks -c Release` from the repository root.
An optional argument selects the installed Assembly-CSharp.dll.

The checks apply the production Harmony transpilers to the installed game's IL
for EnemyHoldTick, ReleasePhysics and CrackDurabilityUse. They verify that only
one calculation is changed in each method and native gates remain in order.
Delayed-hit costs must use the original hit window holder, not a new cable holder.
Runtime checks cover duration at different frame rates, fractional durability,
role changes, death, stage cleanup, disabled mod, non-host clients and invalid
settings. Unity/player/controller lookup objects are stand-ins.

Also run RoleSettingsChecks, LocalizationChecks, GuideSyncChecks,
Test-SecretRoles.ps1, Test-GameFieldAccess.ps1 and Test-Repo050Compatibility.ps1.
These do not replace live cable interaction or host-only multiplayer testing.
