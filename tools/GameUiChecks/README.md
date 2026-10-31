# Game UI regression checks

`dotnet run --project tools/GameUiChecks/GameUiChecks.csproj -c Release`

Runs production GameLocalization, RoleLanguage, RoleText and RoleFontMaterial with
small Unity/native-data stand-ins. Covers 22 locale variants, per-client official
upgrade labels, changing language and changing back, deferred table availability,
unknown IDs, absent managers, RTL metadata, legacy null materials, missing atlas
textures, SDF sampling values, idempotence and unavailable shaders. It cannot
verify actual Unity rendering, button clicks or native multiplayer.

See ../LocalizationChecks/README.md for the live-game checklist.
