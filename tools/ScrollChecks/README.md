# Menu scroll checks

Run `dotnet run --project tools/ScrollChecks/ScrollChecks.csproj -c Release`.

The harness reads `MenuScrollBox.Update` from the installed game assembly, applies the production transpiler, and executes the patched wheel expression with minimal Unity stand-ins. Pass a game assembly path after `--` to override the default local installation. Checks cover the actual patch location, retention of native input gates/keyboard math/bounds/animation, a fixed body-line distance across content lengths and scrollbar sizes, stale native content height, idle/invalid input, and unrelated menus. It also checks that the pinned MenuLib does not register its obsolete scrollSpeed hook; testing that unused formula previously missed the live regression. Unity input polling, patch installation and rendering still need the game checks below.

From build 421, each wheel notch must move three body lines: 97.5 UI units. One body line remains 25 UI units plus 3 units of padding on each side and 1.5 units of row spacing. Compare every Roles page in the lobby and stage after the animation settles. Use `/dh 10` and `/dh 50` to verify that history size does not change the distance. At the top/bottom boundary, movement is limited to the remaining distance. Check reverse input, multiple notches, scrollbar dragging, keyboard navigation and unrelated menus.

The Windows game reports 120 Input System units per wheel detent (observed in the running game while the legacy input API reported zero). Preserve the magnitude so five detents delivered in one frame move fifteen lines, just as five separate frames do. High-resolution fractional input moves the corresponding proportion of three lines.

## Historical in-game validation (build 420: one line per notch)

Build 420 was checked with R.E.P.O. 0.4.4.3 and MenuLib 2.5.4 on Windows at 2560 × 1440, using the English UI. Wheel events were sent to the actual game window; the values below are settled content movement in UI units, where one body line is 32.5 units. A temporary local probe supplied history preview data and observed positions without modifying scroll input or calculations. See the [recorded operation videos, comparison table and CSV](../../docs/verification/scroll-build420/README.md) for all 14 recorded cases and the measurement method.

| Page | Lobby: one notch | Stage: one notch |
| --- | --- | --- |
| ROLE GUIDE | 32.497 | 32.499 |
| BASE UPGRADES | 32.500 | 32.500 |
| TOOLS | 32.500 | 32.500 |
| REPORT A PROBLEM | 32.500 | 32.500 |
| DRAW HISTORY, 10 entries | 32.499 | 32.499 |
| DRAW HISTORY, 50 entries | 32.499 | 32.499 |

In the recorded session with 50 history entries, reversing one notch moved −32.498 units; five notches in one event moved 162.498 units. TOOLS stopped at the bottom boundary. Earlier build 420 validation also checked dragging the scrollbar into the middle of the history: the next wheel notch still moved 32.499 units (not included in these videos). The preview was reset and the temporary probe removed after validation.

Keyboard navigation and unrelated menus were not manually exercised; the automated checks verify retention of their native instructions. The single-player CURRENT ROLES page had no overflow to scroll.
