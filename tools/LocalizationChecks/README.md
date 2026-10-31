# Localization checks

Run `dotnet run --project tools/LocalizationChecks/LocalizationChecks.csproj -c Release`
and `dotnet run --project tools/GameUiChecks/GameUiChecks.csproj -c Release`.

The first compiles production language/text/role-guide code and checks catalog and
placeholder completeness, configured values, hidden/revealed descriptions, and
English fallback. GameUiChecks checks native locale mapping, local upgrade names,
cache invalidation and recovery from the legacy Japanese font's missing material.
RoleSettingsChecks covers removal/backup of the former UI.GuideLanguage setting.

Manual game checks still required:

1. Choose Japanese in R.E.P.O. settings. Open Roles from both the lobby and Escape
   menu; verify descriptions render and navigation remains clickable.
2. Change the game language to English, Korean and Chinese. Reopen Roles and check
   navigation, descriptions, Base Upgrades, history, Tools and the HUD editor.
3. Confirm no separate language selector remains in Roles or REPOConfig.
4. Trigger a truck roulette and compare upgrade names with the native stats panel.
   Check long translated names fit. Change language while a result is visible.
5. With two modded players using different game languages, verify identical draw
   results with locally translated names. Vanilla participants still receive upgrades.
6. Check an unsupported menu language: English menus remain usable, while native
   roulette upgrade names still use that game language. Restart to check persistence.

These automated tests do not render Unity UI or replace live multiplayer testing.
