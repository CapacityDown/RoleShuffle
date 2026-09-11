# Localization checks

Run `dotnet run --project tools/LocalizationChecks/LocalizationChecks.csproj -c Release`.
With fontTools installed, also run `python tools/check_localization_resources.py`
to check source-catalog drift and every added-language glyph against the bundled fonts.
This compiles the production language, text, acceptable-values and role-guide code.
Game configuration entries are stand-ins with deliberately non-default values;
configuration serialization and reload use the actual BepInEx library.

Coverage: all language names and legacy aliases, UTF-8 config save/reload, toggle
cycling, catalog and placeholder completeness, each role's generic/configured
description, revealed/hidden secrets, Mage recovery on/off, host value retention,
and complete English fallback for unknown future descriptions. OptimizationChecks
also tests the production V1/V2 sync receiver with translated host values.

Manual game checks still required:
1. Open MOD settings before opening Roles. Cycle the language choices and verify
   native names, especially 한국어, 繁體中文 and Українська, show without missing glyphs.
2. Change `UI.GuideLanguage`, save, open Roles, and check navigation, role descriptions,
   Base Upgrades, history, Tools and the HUD editor. Check long buttons fit at 720p.
3. Change language with the existing toggle; reopen MOD settings and restart the game
   to confirm it persists. Check migration from a config containing `Japanese`.
4. Join a host with different numeric role settings. Change language and confirm
   values stay identical; request a data refresh and test an older V1/V2 host.
5. Confirm Japanese typography, return to English, and repeat locale switches to
   check font fallback restoration. HUD Cancel must still discard draft changes.

Automated tests do not run Unity, render the menu, or provide native-speaker review.
