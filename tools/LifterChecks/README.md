# Fixed Lifter strength checks

Run `dotnet run --project tools/LifterChecks/LifterChecks.csproj -c Release` from the repository root.

Links the production physics runtime, transpiler and rules. Checks installed R.E.P.O. IL and field contracts, then decodes a small physics fixture with real Harmony, applies the production transpiler, emits the resulting IL and executes it. The fixture keeps locals to match the installed game's per-grabber loop. Unity objects, role lookup and authority are deterministic substitutes.

Coverage: upgrade levels 0–200, mass boundary 2, grip and rotation coefficients, unchanged non-Lifter grabbers sharing an object, repeated calls without compounding, native level preservation, authority, stage cleanup, death, role changes, copied Lifter/Superbot capability, legacy/disabled modes, tumbling and explicit overrides. A changed/unsupported method shape leaves the original instructions intact and disables random Lifter selection.

This does not execute the complete Unity physics engine or multiplayer transport. Live host with an unmodified guest, mixed grabbers, rotations, special objects, stage transitions and role changes remain in-game acceptance checks. Rotation coefficients are intermediate inputs; final torque is not claimed to be six times level-1 torque.
