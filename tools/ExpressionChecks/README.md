# Expression checks

```powershell
dotnet run --project tools/ExpressionChecks/ExpressionChecks.csproj -c Release -- "E:\SteamLibrary\steamapps\common\REPO\REPO_Data\Managed\Assembly-CSharp.dll"
```

Runs the production expression callbacks with lightweight Unity/controller stand-ins and checks all patch targets and argument names against the installed game's assembly. The game assembly remains local and is not included in this repository.

The six expression slots cover quick toggle taps, held keys, duplicate notifications, stop/reset events, delayed input arriving after a menu opens, restoration after a menu closes, stale input, preview avatars, and owner validation. Cast requests are counted before role cooldowns so an accidental extra request cannot be hidden by the Mage cooldown. Remote players can still activate expressions while the host's menu is open; their stop/reset events cannot activate abilities.

This does not run Unity or prove that the patches attach at game startup. Runtime verification should use a test session: assign Mage, select an expression, wait beyond its cooldown, open and close Escape, and verify that no extra spell or HP cost occurs for the host. Select another expression to verify deliberate casting. Repeat in hold mode and with Trickster.

The native expression protocol does not distinguish a remote player's manual selection from restoration of a toggled expression after closing a menu. Remote restoration can still activate an ability; the local input check applies to the authoritative host's own avatar. Opening Escape and other expression-stop events no longer activate abilities for any player. Testing the real game with another player remains necessary for multiplayer validation.

For build 422, 106 expression checks and 8,755 localization checks passed, and the Release build had zero warnings/errors. Runtime testing and deployment were deferred because a multiplayer session was in progress.
