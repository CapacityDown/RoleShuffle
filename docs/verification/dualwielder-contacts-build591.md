# DualWielder contacts — build 591

Date: 2026-11-03 JST. RoleShuffle v5.0.0, branch `version/5.0.0`.
Source base: `0ff3d50b66e8bf663340c6d9be2331492e6add92`.

## Behavior

The second weapon now detects contacts with valuables, physical objects, static
scenery and other players. Existing enemy hit processing remains in place.
Enabled non-trigger body colliders become attack-only trigger sensors; non-convex
mesh colliders and auxiliary/disabled colliders remain disabled. This avoids
uncontrolled solid-body pushes from a weapon whose pose is replayed by the host.
Both initial contact and continuing contact are handled.

- Replay delay, offset, shared battery/durability and original weapon identity
  remain unchanged. Empty reserves, hit cooldowns, release, death and role loss
  stop contact effects. Cleanup disables sensors immediately, including when
  network destruction must retry.
- Ignore the user, original weapon and generated copies. Native enemy collision
  detection handles enemies without a second body-contact hit.
- Physical objects use native impact/destruction/hinge flags and callbacks.
  Native per-target cooldowns are shared with the existing HurtCollider detector,
  avoiding duplicate charges from multiple colliders or both detection paths.
- Walls/floors use native GroundHit sound/particles/cooldown, without durability
  drain. Scenery is detected as contact; the delayed weapon remains pose-driven
  and is not physically constrained or bounced back by a wall.
- Player contacts run through the patched native PlayerHurt entry point, retaining
  existing role-hit observers. Native owner-side HurtOther handles health;
  native tumble/force RPCs handle knockback, including vanilla participants.
  The weapon's recorded player damage is retained, including normal stage damage
  of zero. This does not introduce general friendly-fire HP damage in stages.
- Player hit feedback uses the native shared battery path without calling the
  local-player disarm routine on the host for a remote target.
- Ordinary weapons and non-authority execution retain native processing.

## Validation

- Release build591: zero warnings and errors.
- 7,377 history/runtime/contact checks using production code with isolated Unity
  and native adapters. Includes initial contact, valuables, duplicate target
  colliders, hinges, scenery, self/source exclusions, remote-player knockback,
  damage, empty battery, authority, cleanup and unchanged ordinary weapons.
- 118 native contract checks, including 37 public game method calls, reflection
  types/signatures, shared consumption and owner-authorized health/force RPCs.
- 721 compiled field access checks against the installed non-publicized assembly.
- 662 installed v0.5.0 calls, Harmony bindings and reflection contracts.
- 25,558 localization checks; resource validation covers 6,800 phrases and glyphs.
- 3,258 guide publication/Photon round-trip checks; payload 40,349 bytes.
- README: 99,969/100,000 characters; CHANGELOG: 23,219/100,000, UTF-8 without BOM.
- Seven ZIP entries match the package; all 47 GitHub README icon assets verified.

The adapters do not simulate the Unity physics scheduler or install Harmony
hooks in the .NET test process. Live hit feel, fast/edge contacts, weapon-specific
collider shapes, native animations and host-only multiplayer remain untested.
No continuous collision/sweep guarantee is claimed. Actual game validation is
still required before treating this as verified multiplayer behavior.

## Package and deployment

R.E.P.O. was running, so RSO_TEST was not modified. The package and ZIP are ready;
deployment remains pending until the game closes. No user configuration changed.

- DLL SHA-256: `92ee59ccfb848ba230925b12fa1e636e222b744c71047bdb1d7445e3a47db75b`.
- ZIP SHA-256: `69983a402e1fe221bd04fc057d25a48e53d497ff1dfab0a7ca6bb9c198e93170` (5,649,946 bytes).
- ZIP: `RoleShuffle.zip`; versioned copy: `output/release/RoleShuffle-5.0.0.zip`.
- Backup: `tmp/deployments/before-build591-20261103-053900`.
- Deployment record: `tmp/deploy-build591.json`.

## Deployment completed

Deployed to RSO_TEST on 2026-11-03 at 15:03 JST after R.E.P.O. exited.
All seven deployed files match the package by SHA-256; the deployed DLL matches
build591. Previous profile files and configuration were backed up to
`tmp/deployments/before-build591-20261103-060351`. Configuration was not changed.
This supersedes the pending deployment above. Live gameplay remains untested.
