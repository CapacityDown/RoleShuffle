# Menu scroll checks

Run `dotnet run --project tools/ScrollChecks/ScrollChecks.csproj -c Release`.

This harness links the production wheel prefix/finalizer with minimal engine stand-ins and reproduces the installed MenuLib direction-based scroll-speed formula. It verifies the established lobby step for all four page multipliers, including when the legacy input API returns zero or a tiny value while the game's input reports scrolling. The input sources are varied independently: assuming they always agree missed the build-417 regression. Checks also cover reverse direction, different content lengths/scrollbars, idle frames, other menus, temporary-speed restoration on success/failure, and end stops. It does not execute Unity input polling, Harmony patch installation, or game rendering, and does not claim that multiple hardware detents merged into one game input frame are counted separately.

In the game, compare the same language/page in the lobby and stage, using both single notches and a fast spin. For DRAW HISTORY use `/dh 50` in each scene so the sample content is identical. Compare 30/60/high FPS after the scrolling animation settles. Check the top/bottom boundaries, scrollbar dragging, keyboard navigation, HUD editor, confirmation dialogs and unrelated menus.
