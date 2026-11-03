# Engineer v0.5.0 valuable support — build 588

Date: 2026-11-03 (JST). RoleShuffle 5.0.0, branch `version/5.0.0`.
Source base: `318fdb0be960005950e8fb62f616c89da7256687`.

## Result

The 12 effect-component families marked pending in the build 584 audit are now
included in Engineer suppression. The inventory describes native component
families, not a count of every prefab. Existing Cowboy support is retained.

| Native component | Handling while suppressed |
| --- | --- |
| LighterValuable | Reset opening/grab timers, extinguish the light and hold the lid closed. |
| SnakeInACanValuable | Block extension; retract an already extended snake through native cleanup. |
| ValuableCubeOfKnowledge | Stop talking, rejection, connection impulses and activation actions; retain earned value, scale and puzzle progress. |
| ValuableDesertBirdCage | Return to idle, reset chirp/death timing and prevent new chirps or death effects. |
| ValuableDesertEnergyCrystalChunk | Return to idle, stop energy/charge renewal and allow existing short effects to expire. |
| ValuableDesertExplosiveTower | Reset trigger/explosion states and block new triggers; preserve destruction cleanup. |
| ValuableDesertExplosiveTowerDynamite | Block explosion and pause the native standalone fuse without resetting elapsed progress; resume its previous speed on release. |
| ValuableDesertGolfcart | Return to idle, clear the damage collider and prevent powered driving. |
| ValuableDesertPopcornStand | Reset revving and investigation timers; retain native sound/visual wind-down. |
| ValuableDesertZeroGravCrystal | Block effect creation and renewal; allow existing effects to expire. |
| ValuableMusicInstrument | Make native attachment checks false, allowing normal detachment; suppress host enemy investigation during detachment. |
| ValuableWizardCrystal | Suppress the destruction effect without blocking ordinary destruction/unsubscription cleanup. |

The already-supported wheeled horse also returns from moving to idle, restores
its default collider material and fades its movement loop when held. An inactive
horse is not activated by this cleanup. Cosmetic-only snow-globe, rattlesnake
ornament and spring animation components retain their normal behavior. The
existing documented exclusions remain unchanged.

## Host-only path and lifecycle

- Authority and grab ownership are checked before interception. Dead players do
  not maintain Engineer suppression.
- Keep authoritative grabbers for physics. Temporarily remove client-visible
  grabber entries with the game's MasterOnly native RPC while suppression lasts.
- Cover ordinary co-holders joining an Engineer and Engineer joining an existing
  hold. On role loss, release/grace expiry or stage end, restore only still-held
  entries, once. A fresh native grab must not receive a duplicate restoration.
- Run ordinary animation, destruction, expiry and puzzle bookkeeping where
  needed. Suppress effect actions separately from cleanup.
- Preserve dynamite elapsed fuse progress. Restore its former fuse speed when
  suppression ends or the stage closes. Enemy-owned fuses are untouched.
- Scope music detachment noise suppression to its native frame and release the
  scope with a Harmony finalizer, including exceptions and nested calls.

## Validation

- Release DLL produced against the installed R.E.P.O. v0.5.0 assembly.
- 359 Engineer checks passed: production catalog/prefix/cleanup helpers, native
  method and field contracts, release/re-hold, co-holders, RPC restoration,
  authority, stage cleanup, paused fuses, horse cleanup and exception scope.
- 668 compiled game field references passed the non-publicized assembly check.
- 628 v0.5.0 calls, Harmony bindings and reflection contracts passed.
- 25,501 localization checks passed. Font/resource validation passed for 6,780
  phrases, Japanese UI coverage and native setting-name glyphs.
- Release Markdown validation passed: README 99,944/100,000 characters;
  CHANGELOG 23,169/100,000. Both are UTF-8 without BOM.
- All 47 GitHub README icon downloads match local assets. The ZIP contains the
  expected seven files, with each archived file matching its package source.

The Engineer checks exercise production helpers with simulated Unity/network
objects and inspect native IL contracts. They do not install Harmony detours in
the .NET test process and are not a live Unity or multiplayer test.

## Practical limits and pending checks

Native clients can retain some audio/visual effects until their normal cleanup.
Previously applied energy/zero-gravity effects have a short expiry interval;
this implementation does not remove unrelated effects globally. Native local
`grabbedLocal` is not changed by removing a client-visible grabber entry.
These limits are described in the guide and release README.

Live game startup, active valuable behavior and host-only multiplayer with a
vanilla participant still require in-game verification. R.E.P.O. was running
when deployment was checked, so RSO_TEST was not modified. The new package is
ready; deployment must wait until the game is closed.

## Artifacts

- DLL SHA-256: `239edc982b5eaeaae7d0fdcb32dd945b1d5dfc6f8428c94ba567a5ca727ac40c`.
- ZIP SHA-256: `f8cff2b64136f088a84314391f6dc41a7006ee344b10bb58cdaff261ab7cfbc8` (5,646,621 bytes).
- ZIP: `RoleShuffle.zip`; versioned copy: `output/release/RoleShuffle-5.0.0.zip`.
- Prior release/package/log backup: `tmp/deployments/before-build588-20261103-045954`.
- Deployment record: `tmp/deploy-build588.json`; status: pending.
- Installed game assembly SHA-256: `aa6b1ce0deef8d2b7104a3722864c4ba94c107257726471f8e44b45c99e36888`.
