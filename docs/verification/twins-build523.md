# Twins — v4.5.4 build523

Date: 2026-10-10 (JST). Branch: `version/4.5.4`.

## Delivered behavior

- Added ordinary role ID43 (`StageRole.Twins`, enum42), preserving every existing ID.
- A weighted pair offer reserves two living players before category guarantees use the remaining slots. At most one pair; never a solo/join/fallback singleton. The pair is exempt from the ordinary unique-role limit. Imitator and Superbot cannot acquire Twins.
- Combines both ordinary maximum/current HP without healing. Native HP updates mirror the shared pool; a publication is excluded from observation so it cannot recursively count as another heal or hit. Pending same-frame owner changes accumulate before publication.
- Shared HP exhaustion and non-HP deaths kill both through the native death RPC. One native revival revives the partner at its own head and shares the source revival's restored HP once. Native death callbacks are checked against actual death/HP state. Interrupted revivals cannot reuse a stale source.
- Pair-to-pair native HP transfers are filtered during the authoritative `PlayerHealthGrab.Update`; outsider transfers remain available. Stage end, changed roles, disconnected/replaced avatars and infection remove temporary speed and restore HP in proportion, using the existing floor/minimum-one convention.
- Influenza captures the partner before unlinking. Both roles become Influenza with the same incubation start, regardless of distance. Departure never reforms the same pair on reconnection.
- Cooperative carry: both directly hold the same valuable, each gets 1.5x grip/rotation input, collision value loss is reduced50%. Shop items are excluded. Collision reduction is scoped to the native collision call, not explosions or other scripted breaks.
- Rest: both crouch within3m and remain stationary in all three axes without damage for3s. Rest restores10% of shared maximum HP and both stamina pools. Shared cooldown60s; interruption spends nothing. Native stamina +1/-1 restores the original upgrade level immediately.
- Rendezvous: separation15m arms assistance; running toward the partner enables the speed bonus; within5m disarms it. Native integer Speed levels are rounded up toward1.25x and capped at level200. Only the temporary contribution is removed, preserving other upgrades.
- Joint delivery: continuously co-carry a valuable for3s outside delivery areas, then bring it into a truck/extraction room with both living players within5m of the item. +10% of current value, floored to whole dollars, once per item; stage cap$5,000. Value is marked paid before dispatch. A release hook refreshes the native room check for quick placement between polls.
- Added thirteen bounded REPOConfig ability settings, Enabled/Weight, cyan HUD cooldown/budget symbols, fourteen-language guide text and three updated CJK font subsets.

## Host-only limitation accepted by the user

The user selected host-only compatibility with some shared-HP error on simultaneous events. Native owners send absolute remaining HP, without a damage delta or acknowledgement of host publications. Two changes received before a publication are summed correctly. A delayed owner snapshot crossing a host publication can undercount or overcount its delta; historical HP cannot be reconstructed exactly from that packet. This is a remaining gameplay limitation, not a claim of exact synchronization. Live vanilla-guest multiplayer verification has not been performed.

## Verification

- Release build: **0 warnings, 0 errors**.
- `Test-GameFieldAccess.ps1`: **557** compiled game field references checked against the installed non-publicized game assembly.
- `tools/TwinsChecks`: **32** checks directly compile production `TwinsRuntime.cs` and `TwinsRules.cs` with native-interface stand-ins. Covers initial sum, damage/heal echo exclusion, simultaneous changes and lethal sum, linked death/revival, proportional unlink, departure, infection handoff, rest damage/vertical movement/cooldown, temporary sprint removal, equipment exclusion, release delivery, repeat payment, stage cap and distance hysteresis. Native transport/physics remain simulated.
- `Test-SecretRoles.ps1`: **90** production catalog/planner checks, including atomic pair creation, duplicate identities, dead-player exclusion, solo/join exclusion and copy/capability restrictions.
- Influenza production runtime harness: **154** checks, including infection of an out-of-range twin and equal onset times.
- Overhaul checks: **76,091**; localization: **13,431**; notification/Signalman: **97**.
- Ability config: **204**; role settings: **341**; base settings: **128**; saved adjustments: **248**.
- Twins asset/translation/glyph checks: **59**; Signalman regression: **34**.
- README: **98,248/100,000** decoded characters, UTF-8 without BOM. Version-heading spacing in CHANGELOG retained. Repeated per-role Enabled/Weight rows were replaced with equivalent general instructions.

## Artwork

Built-in image generation used the approved Tank and Medic originals as references. The full prompt is retained in `Assets/role-emblems-semibot-v1/selected/prompts.json` under Twins. Selected original, normalized master and256px runtime were retained; existing artwork was unchanged. Purple Special interior normalized to `#50356E`; cream frame, character and native exterior alpha preserved. Reviewed at256px and64px on light/dark backgrounds in `output/twins/icon-review.png`.

- Selected: `Assets/role-emblems-semibot-v1/selected/43-Twins.png`.
- Runtime: `Assets/role-emblems-semibot-v1/transparent/runtime/43-Twins.png`.
- Runtime/embedded resource SHA-256: `153fa58db94d94fab951aae8c251a0221ea96c30994644cd72aa8e1b48c334a9`.
- Public thumbnail: `docs/icons/43.png`, pinned in README to asset commit `d5f615a8cf9ee60a6b0ae98104a051d2ad64a193`.
- Full/public icon catalogs regenerated in parallel under `output/icon-catalog` (46/45 cards). Public secret names/artwork checks passed.

## Deployment

Build, package and RSO_TEST DLL SHA-256:

`c0c39a8cda1e7a9f7777f62327df77ecdc5219f5c7ecd8913c729eda55045ec8`

Game process absent during deployment. Seven files copied and individually hash-verified. Previous package DLL and RSO_TEST folder backed up to `tmp/deployments/before-build523-20261009-163653`. Deployment record: `deployment.json` in that folder. Existing release ZIPs were not replaced. Neither a live game session nor live host-only multiplayer was tested.
