# Menu scroll checks

Run `dotnet run --project tools/ScrollChecks/ScrollChecks.csproj -c Release`.

This harness links the production wheel prefix/finalizer with minimal engine stand-ins and reproduces the installed MenuLib scroll-speed formula. It verifies equal travel for individually delivered versus batched wheel detents, fractional input, reverse direction, all four page multipliers, different content lengths/scrollbars, idle frames, other menus, temporary-speed restoration on success/failure, and end stops. It does not execute Unity input polling, Harmony patch installation, or game rendering.

In the game, compare the same language/page in the lobby and stage, using both single notches and a fast spin. For DRAW HISTORY use `/dh 50` in each scene so the sample content is identical. Compare 30/60/high FPS after the scrolling animation settles. Check the top/bottom boundaries, scrollbar dragging, keyboard navigation, HUD editor, confirmation dialogs and unrelated menus.
