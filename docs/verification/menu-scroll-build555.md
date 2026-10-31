# Roles mouse-wheel scrolling — build555

## Cause and change

The previous wheel helper divided every input delta by a Windows-specific
constant of 120. Normalized input of 1 therefore moved only 0.8125 content units,
rather than the intended 97.5 units (three guide body lines).

The installed R.E.P.O. v0.5.0 InputManager reads its `<Mouse>/scroll/y` action
without converting it. Unity.InputSystem 1.19.0 supports normalized and
platform-specific scroll ranges. Its own InputSystemUIInputModule.OnScrollCallback
divides the delta by InputSystem.scrollWheelDeltaPerTick, which reads the active
native input runtime. RoleShuffle now uses that same getter through a cached
Harmony delegate. The getter is internal, so direct C# access failed in build554;
no build554 DLL was deployed. The current value is read on each wheel operation,
so changes in the runtime input range do not leave a stale conversion factor.

Fractional and multiple detents keep their proportional distance in both
directions. Current content and handle travel still determine the conversion
between content movement and scrollbar movement. Only the open Roles scroll box
uses this helper; native input gates, keyboard/controller navigation, dragging,
bounds and animation are retained. Invalid tick sizes produce no movement.

## Verification

- Release build555 against the installed game: zero warnings and errors.
- 243 scroll checks using the installed MenuScrollBox.Update IL and the production
  transpiler/helper. Cases cover tick sizes 1, 120 and 10; positive/negative,
  fractional and multiple detents; three page geometries; stale native heights;
  runtime unit changes; invalid input/tick sizes; no travel; unrelated/closed menus.
- The installed Unity input assembly is inspected to confirm the internal float
  getter, its runtime read and its use as the divisor by Unity's UI callback.
- 613 compiled game-field references and 547 v0.5.0 compatibility contracts pass.
- Previous scroll checks supplied only the old 120-unit convention; the new cases
  cover normalized input explicitly. Unity objects/input remain test stand-ins.
- Package contains the seven required entries, byte-compared with source files;
  README is 98,531 characters, with 45 GitHub image references verified.

## Delivery

- Version 5.0.0, UI build555, branch `version/5.0.0`.
- RSO_TEST updated with the game stopped; all seven deployed files hash-verified.
- Previous plugin, configuration, log and release ZIP retained under
  `tmp/deployments/before-build555-20261031-013350`.
- Build/package/deployed DLL SHA-256:
  `3f246c543e2fb096c02c7830eea7e55f4c25a51790b787be62b39018b0a51bcb`.
- Release ZIP SHA-256:
  `d22e8a43da474599a1b36ec8284ea06456890b17442c1e2198972af868b2be76`.

## Limits

No live input capture, rendered-menu scrolling or multiplayer session was
performed. Confirm the wheel distance and direction in the Roles menu in game.
