# Fixed Lifter strength checks

Run `dotnet run --project tools/LifterChecks/LifterChecks.csproj -c Release` from the repository root.

Links the production physics runtime, transpiler and rules. Checks installed R.E.P.O. IL and field contracts, then decodes a small physics fixture with real Harmony, applies the production transpiler, emits the resulting IL and executes it. The fixture keeps locals to match the installed game's per-grabber loop. Unity objects, role lookup and authority are deterministic substitutes.

Coverage: upgrade levels 0–200, mass boundary 2, level-1 light handling versus sixfold heavy grip/rotation, unchanged non-Lifter grabbers sharing an object, repeated calls without compounding, native level preservation, authority, stage cleanup, death, role changes, copied Lifter/Superbot capability, disabled mode, tumbling and explicit overrides. A changed/unsupported method shape leaves the original instructions intact and disables random Lifter selection.

The production `UpgradeService` is also linked. An additive vanilla upgrade/cache substitute exercises repeated Lifter/Superbot → Runner changes at every Base level, both in solo and with simulated native guest RPCs. A stale Lv200 cache contribution fails against the old setter; the repaired setter restores Base grip/rotation even if the stored level already matches. Stage cleanup, missing avatars, authority, higher-level minimum grants, temporary overrides and isolation from other players are checked. Installed-game IL confirms the additive cache operation; transport itself remains simulated.

This does not execute the complete Unity physics engine or multiplayer transport. Live host with an unmodified guest, mixed grabbers, rotations, special objects, stage transitions and role changes remain in-game acceptance checks. Rotation coefficients are intermediate inputs; final torque is not claimed to be six times level-1 torque.

A numerical free-hold regression uses the native spring/torque equations at 50 Hz with coefficients obtained from the emitted production patch. It reproduces persistent movement with the former sixfold light-object values and checks settling with level-1 values. This model covers centered, unconstrained holding; it does not establish live Unity contact or network behavior. No custom damping, Rigidbody changes or additional force patches are used.
