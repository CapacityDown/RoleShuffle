# DualWielder verification

Run `dotnet run --project tools/DualWielderChecks -c Release`.

The executable links the production history and runtime against isolated Unity,
Photon and game adapters. It checks timestamp interpolation at 15/30/60/144 FPS,
rotation, attack timing, teleports, memory bounds, battery sharing, native hit
cooldowns, inventory/grab rejection and cleanup after release, switching, death,
role replacement, disconnect, disabling the mod and stage end. Network destroy
failure is injected to verify disabled-hitbox cleanup retries.

The stub melee hit consumes its battery field; it does not claim to reproduce the
game's physics. Run `tools/Test-DualWielderNativeContract.ps1` against a main build
for the installed game's actual hit-consumption paths, reflected members, Harmony
signatures, inventory hooks and embedded artwork. `tools/Test-GameFieldAccess.ps1`
checks every compiled game-field access. Live weapon physics, vanilla guest
replication, remote grab/equip rejection and combat still require in-game tests.
