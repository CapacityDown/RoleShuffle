# Menu scroll checks

Run `dotnet run --project tools/ScrollChecks/ScrollChecks.csproj -c Release`.

The harness reads `MenuScrollBox.Update` from the installed game assembly, applies the production transpiler, and executes the patched wheel expression with minimal Unity stand-ins. Pass a game assembly path after `--` to override the default local installation. Checks cover the actual patch location, retention of native input gates/keyboard math/bounds/animation, a fixed body-line distance across content lengths and scrollbar sizes, stale native content height, idle/invalid input, and unrelated menus. It also checks that the pinned MenuLib does not register its obsolete scrollSpeed hook; testing that unused formula previously missed the live regression. Unity input polling, patch installation and rendering still need the game checks below.

In the game, each wheel notch must move one body line (25 UI units plus 3 units of padding on each side and 1.5 units of row spacing). Compare every Roles page in the lobby and stage after the animation settles. Use `/dh 10` and `/dh 50` to verify that history size does not change the distance. Check reverse input, multiple notches, top/bottom boundaries, scrollbar dragging, keyboard navigation and unrelated menus.

The Windows game reports 120 Input System units per wheel detent (observed in the running game while the legacy input API reported zero). Preserve the magnitude so five detents delivered in one frame move five lines, just as five separate frames do. High-resolution fractional input moves the corresponding fraction of a line.

## In-game validation

Build 420 was checked with R.E.P.O. 0.4.4.3 and MenuLib 2.5.4 on Windows at 2560 × 1440, using the English UI. Wheel events were sent to the actual game window; the values below are settled content movement in UI units, where one body line is 32.5 units. A temporary local probe supplied history preview data and observed positions without modifying scroll input or calculations.

| Page | Lobby: one notch | Stage: one notch |
| --- | --- | --- |
| ROLE GUIDE | — | 32.498 |
| BASE UPGRADES | 32.499 | — |
| TOOLS | One line, visually verified | 32.500 |
| REPORT A PROBLEM | One line, visually verified | 32.500 |
| DRAW HISTORY, 10 entries | 32.499 | 32.499 |
| DRAW HISTORY, 50 entries | 32.499 | 32.499 |

With 50 history entries, reversing one notch moved −32.496 units; five notches in one event moved 162.498 units. Dragging the scrollbar into the middle of the history worked, and the next wheel notch still moved 32.499 units. TOOLS stopped at the bottom boundary. The preview was reset and the temporary probe removed after validation.

Keyboard navigation and unrelated menus were not manually exercised; the automated checks verify retention of their native instructions. The single-player CURRENT ROLES page had no overflow to scroll.
