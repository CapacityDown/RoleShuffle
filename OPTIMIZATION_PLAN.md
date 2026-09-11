# RoleShuffle Optimization Plan

## Status

- Status: First implementation pass completed; live multiplayer profiling pending
- Implementation: Assignment indexes, conditional utility scans, synchronization/UI caches, shared Mage cleanup, lazy Trickster expiration collection
- Release target: 4.4.3
- Last reviewed: 2026-09-05
- Scope: Internal development guidance. Do not include this file in the public release ZIP.

This document preserves the agreed optimization direction so that work can begin later without repeating the initial audit. Optimization must not be mixed into unrelated feature or bug-fix work. Before implementation, recheck the current code because line numbers and runtime behavior may have changed.

## Implementation pass: v4.4.3

- A1: Added Steam ID and avatar indexes. Registration, confirmed departure, avatar replacement, restoration, and cleanup maintain them. Capability checks read the assignment's current effective role, including Imitator and Superbot. Actor/role-list indexes remain deferred.
- A2/B3: Utility scans are now lazy and shared within each existing 0.25-second tick. No scene search occurs unless a living Mechanic/Electrician capability holder has a positive rate and unused stage allowance. The existing scan remains when needed; the held-object registry is not implemented yet.
- A3: Cache local English/Japanese/legacy guide content until a configuration change, cache received guide decoding, and skip identical guide publications. Configuration-driven publication is debounced by 150 ms. Room-property formats and fallback behavior are unchanged.
- A4/A6: Cache read-only assignment snapshots by effective payload, preview count, and preview local identity/name. HUD/menu reuse those snapshots and cached signatures; HUD layout uses typed comparisons instead of per-frame strings.
- C3: Mage generated-object maintenance runs once per controller frame rather than once per Mage. Trickster allocates an expiration list only when a decoy actually expires, preserving local ownership across callbacks.
- Validation: Release build and production synchronization-source tests; see `tools/OptimizationChecks/README.md`. Live Unity/Photon performance measurements and vanilla-participant regression tests are still pending.
- Deferred: shared held-object registry, global role-presence/category indexes, Engineer/Hunter component caches, broader runtime scheduling, prefab catalog, wrapping changes, and other Phase B/C items. No gameplay tick intervals were changed.

## Goals

1. Reduce repeated scene searches, hierarchy searches, reflection, parsing, and temporary allocations.
2. Keep host-only installation and vanilla participant compatibility.
3. Preserve gameplay timing and role behavior unless a separate specification explicitly changes them.
4. Apply changes in small, independently measurable passes with a rollback point after each pass.

## Required compatibility and behavior

The following requirements take priority over performance:

- Only the host is required to install RoleShuffle.
- Vanilla participants must continue to receive upgrades, healing, revival, chat, and other effects through vanilla-compatible behavior.
- Keep StageFlux ordering and compatibility, including the existing `HarmonyAfter` relationship and `RunManager.ChangeLevel` `__runOriginal` handling.
- Do not account for host migration unless the project requirements change.
- Keep the participant-presence check interval at 0.5 seconds.
- Keep removal after 10 consecutive absence confirmations.
- Keep Jobless and Tuna damage timing at 0.1-second intervals.
- Do not casually pool Photon network objects such as grenades and valuables. View IDs, vanilla effects, and late replication make this unsafe without dedicated multiplayer tests.
- Do not replace required vanilla RPCs with custom batched RPCs merely to reduce traffic.
- Do not change public room-property formats without a compatibility plan.

## Baseline before implementation

Before changing code, capture the following in the same test scenario:

- Host frame time and garbage collection frequency.
- Allocations per second.
- Photon room-property updates and approximate payload size.
- Number of active valuables, enemies, spawned role objects, and players.
- Results at 1, 4, 10, 20, and 30 players where practical.
- Results with no relevant role assigned and with each optimized role assigned.
- A StageFlux-enabled run and a vanilla-participant run.

Use a repeatable stage and record the game version, RoleShuffle version, configuration, frame-rate cap, and installed MOD list. Performance work is accepted only when it is compared with this baseline.

## Phase A: Low-risk structural improvements

Implement these first. Each item should be a separate reviewable change.

### A1. Stage role indexes and presence flags

Primary code: `StageRoleController.cs`

Maintain stage-scoped indexes instead of scanning every assignment for each query:

- Steam ID to role assignment.
- Photon actor number to role assignment.
- Player avatar instance ID to role assignment.
- Role to assigned-player list.
- Per-role member count or presence flag.

Update the indexes only when assignments can change: stage setup, test-command reassignment, random reassignment, participant join, confirmed departure, restoration, and stage cleanup. Centralize assignment mutation so indexes cannot become stale.

Expected result: hot role checks such as Ninja, Hunter, Engineer, and notification processing become constant-time lookups.

### A2. Early exits when a role is absent

Primary code: `MedicRoleRuntime.cs`, `UtilityRoleRuntime.cs`, `HunterBatteryRuntime.cs`, `NotificationEnemyReactionGuard.cs`, and relevant patches.

Return immediately before scene, component, reflection, or collection work when no player currently has the relevant role. Use the stage role presence flags from A1.

Expected result: roles that were not drawn have close to zero recurring cost.

### A3. Role Guide serialization and publication cache

Primary code: `RoleGuideSync.cs`, `StageRolesPlugin.cs`, `StageRoleController.cs`

- Generate English, Japanese, and legacy-compatible payloads together and cache them.
- Debounce related configuration changes for approximately 100 to 250 milliseconds.
- Compare the new payload with the currently published payload and skip identical room-property updates.
- Maintain a numeric guide revision and increment it only when effective guide content changes.
- Preserve the current room-property keys and payload formats.

Expected result: changing a configuration slider does not repeatedly serialize and replicate the full guide.

### A4. Parsed assignment snapshot cache

Primary code: `RoleAssignmentSync.cs`, `RoleHud.cs`, `RoleMenu.cs`

Cache parsed role snapshots by the effective raw assignment payload and preview-player count. Expose a numeric revision. HUD and menu code should rebuild visible data only when the revision changes.

Expected result: Base64 decoding, string splitting, and list construction are not repeated by both HUD and menu polling.

### A5. Reuse temporary collections

Primary code: participant-presence handling in `StageRoleController.cs` and role runtimes that create per-tick lists or sets.

Reuse private scratch collections with `Clear()` where the call is not re-entrant. Replace repeated string keys with value tuples or small structs where appropriate. Keep collection ownership explicit to avoid retaining player or Unity object references after stage cleanup.

Expected result: lower garbage collection pressure without changing timing.

### A6. HUD typed state comparison

Primary code: `RoleHud.cs`

Replace per-frame interpolated layout-signature strings with typed cached fields or a small value struct. Rebuild page data only on assignment revision changes. Continue updating every frame only while a fade or other visible transition is active.

Expected result: less recurring string allocation and UI layout work.

## Phase B: High-impact runtime changes

Begin only after Phase A is stable. These changes require multiplayer regression testing.

### B1. Shared held-object registry

Primary code: `LifecyclePatches.cs`, `UtilityRoleRuntime.cs`, `GamblerRoleRuntime.cs`, `StageRoleController.cs`

Use the existing grab-started and grab-ended events to maintain:

- Objects currently held by each player.
- Current grabbers for each object.
- Whether at least one grabber has Engineer or another relevant role.
- Newly grabbed valuables awaiting Gambler processing.

Support multiple grabbers. Refresh owner information after role reassignment, reconnect, revival, and object destruction. Add a low-frequency reconciliation scan, approximately every 2 to 5 seconds, to recover from missed events without returning to per-frame full searches.

Expected result: Mechanic, Electrician, Engineer, Gambler, and Bomber restrictions can share one authoritative object-ownership source.

### B2. Engineer suppression fast path

Primary code: `LifecyclePatches.cs`, `StageRoleController.cs`

Current suppression checks can perform component searches from patched `Update`, `FixedUpdate`, or `LateUpdate` methods. Replace the hot path with:

1. Immediate return if no Engineer exists.
2. Constant-time lookup of the affected object in the held-object registry.
3. Cached classification of whether the object is an effect valuable.
4. A short, explicitly timed release grace period where required by the existing behavior.

Cache component instance ID to `PhysGrabObject` and `PhysGrabObject` instance ID to classification. Remove entries when Unity objects are destroyed and on stage cleanup.

Do not dynamically patch and unpatch all Engineer targets per stage as the first implementation; a stable patch with a cheap presence check is safer.

### B3. Mechanic and Electrician event-driven processing

Primary code: `UtilityRoleRuntime.cs`

Stop calling `FindObjectsOfType<PhysGrabObject>()` on the normal 0.25-second path. Process only the valuables currently held by relevant players. Permit a stage-start discovery and the shared low-frequency reconciliation scan.

Expected result: removal of one of the largest recurring scene-wide allocations.

### B4. Gambler event-driven activation

Primary code: `GamblerRoleRuntime.cs`

When a Gambler begins holding a valuable, enqueue that valuable for processing on the following frame so vanilla value initialization can complete. Do not scan the entire valuable list every frame. Retain a low-frequency reconciliation path for missed grab events.

### B5. Hunter battery classification cache

Primary code: `HunterBatteryRuntime.cs`

Cache stable component references and weapon classification per `ItemBattery`. Refresh the current owner from grab or equip events. The battery-consumption patch should perform an early Hunter-presence check and constant-time role lookup before any hierarchy search.

### B6. Scheduled role ticks and player-state snapshots

Primary code: `StageRoleController.cs`, `PlayerState.cs`, individual role runtimes.

Separate role logic from display frame rate. Initial target schedule:

| Timing | Work |
|---|---|
| Every frame | Visible transitions and operations that truly require frame timing |
| 0.05 seconds | Bomber, Stinker, and Tuna movement sampling; revival checks if required |
| 0.1 seconds | Jobless and Tuna damage; shared death-state sampling |
| 0.25 seconds or next-due time | Medic, Mechanic, Electrician, and Ninja maintenance |
| Event-driven | Mage, Gambler, Trickster, Hunter, Engineer, and Musician triggers |

At the start of a scheduled runtime tick, build one player-state snapshot containing only the values required by that tick, such as living state, position, truck state, death-head reference, and death-head position. Reuse it across roles instead of repeating private-field reflection and player scans.

Avoid introducing additional private-field dependencies. First reduce the number of existing reads; only consider faster typed access after compatibility testing.

## Phase C: Startup and UI refinements

### C1. Shared vanilla prefab catalog

Primary code: `VanillaRolePrefabResolver.cs`

Build one catalog from the vanilla item and prefab collections after the required assets are available. Resolve assets only for roles assigned in the current stage. If assets are not ready, retry with a controlled backoff such as one second; do not permanently cache an early failure.

Cache the enabled random-grenade candidates and rebuild the list only when the relevant configuration changes.

### C2. Role Guide wrapping cache

Primary code: `RoleMenu.cs`, `RoleGuideFont.cs`

Cache wrapped guide output by guide revision, language, content width, and font identity. Do not change the visual structure until the cache-only version is proven safe. Past issues with Japanese font sizing, the final line, empty space, and scroll limits make aggressive UI consolidation higher risk.

### C3. Small recurring-allocation fixes

- Change Stinker's pending position handling from list-front removal to a queue.
- Use squared distance for movement thresholds where exact distance is unnecessary.
- Perform Mage spawned-object cleanup once globally at low frequency, not once per Mage per frame.
- Reuse Trickster expiration buffers or schedule the next expiration directly.
- Prune expired Ninja vision-attempt records at low frequency.
- Make enemy-hit patch state a value type where safe and avoid reading the same attacker field twice.
- Rate-limit expected repetitive success and exception logs.
- Ensure HUD and cloned-font caches release destroyed Unity objects and are cleared on stage or menu teardown.

## Presence-check notes

Optimize allocation and lookup only; do not alter the agreed presence behavior.

- Run the real participant check every 0.5 seconds.
- Remove a participant only after 10 consecutive confirmed absences.
- Restore a previously removed participant if found again.
- Initialize and assign roles to genuinely new players as currently specified.
- Use an indexed Steam ID lookup to avoid nested assignment scans.

During implementation, separately verify whether a lingering Death Head can incorrectly keep a disconnected player marked as present. Treat any behavior change here as a correctness fix requiring dedicated tests, not as an incidental optimization.

## Validation matrix

Every completed phase must pass the relevant tests below before moving on:

1. Host-only RoleShuffle with vanilla participants.
2. RoleShuffle and StageFlux together, including stage completion and all revival paths.
3. One-player, late-join, voluntary leave, disconnect, reconnect, death, and revival scenarios.
4. Role reassignment by the supported test commands, including `all` and random selection.
5. Multiple players grabbing the same valuable or generated grenade.
6. Engineer valuables that previously caused partial effects or loops.
7. Mechanic repair and Electrician recovery limits.
8. Gambler activation and delivery-state recovery.
9. Hunter continuous-use weapons and jackpot orb behavior.
10. English and Japanese Role Guide, HUD pagination, 20-player display, and menu reopen behavior.
11. Stage cleanup with no role upgrades or state leaking into the next stage.
12. Log review for new exceptions, repeated warnings, missing RPC targets, and destroyed-object access.

## Acceptance criteria

An optimization is complete only when all of the following are true:

- Measured CPU time or allocations improve in the targeted scenario.
- No role rules, configured values, UI text, or timing change unintentionally.
- Vanilla participants and StageFlux compatibility remain intact.
- Stage cleanup releases caches and Unity references.
- The public package contents remain unchanged unless the release itself is intentionally rebuilt.
- The implementation and its user-visible consequences, if any, are documented in the appropriate release files at release time.

## Deferred implementation order

When optimization work is authorized, use this order:

1. A1 role indexes and presence flags.
2. A2 early exits.
3. A3 and A4 synchronization caches.
4. A5 and A6 allocation and HUD caches.
5. B1 shared held-object registry.
6. B2 through B5 role-specific event-driven paths.
7. B6 scheduled ticks and player-state snapshots.
8. C1 prefab catalog.
9. C2 and C3 UI and minor refinements.

Stop after each step, measure against the baseline, and keep the change only if the benefit is observable and behavior remains compatible.
