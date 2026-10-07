# Health bonus removal — v4.5.4 build509

## Requested behavior

On removal of extra role Health, preserve remaining HP as a fraction of the old
maximum. Round down, retaining at least 1 HP when current HP is positive. Preserve
0 HP for dead players. The reported Tank case is 300/520 → 69/120.

## Implementation and native behavior inspected

- All absolute Health level reductions in `UpgradeService` use the shared reset,
  including role changes, stage cleanup, custom levels and retained Base levels.
  Positive upgrades and additive item/truck rewards keep their previous behavior.
- Vanilla `UpgradePlayerHealth` changes maximum HP and calls `Hurt` on the owner
  for negative deltas. `Hurt` can be skipped outside active gameplay; next-scene
  `PlayerHealth.Fetch` instead clamps persisted HP to the new maximum. Calling
  this negative upgrade before an HP correction can also kill a living player.
- `HealthUpgradeReset` uses the public native `PunManager.UpdateStat` to set the
  Health upgrade level and saved `playerHealth`, then native `UpdateHealthRPC` to
  set live current/maximum HP without a damage/healing call or effects. The saved
  HP write is explicit because `SetPlayerHealth(..., false)` skips writes in shop
  context. Departed players use their saved HP and level-derived maximum.
- The maximum changes by the removed upgrade amount, preserving any unrelated
  maximum-HP offset. The actual live maximum is the denominator when available.
- `SemiFunc.OnSceneSwitch` prefix completes role cleanup before vanilla saves and
  broadcasts dictionaries. The existing `ChangeLevel` postfix remains a fallback.
  Repeated cleanup cannot rescale twice because levels and assignments are already
  reset. Phoenix/Stage Flux cancellation of ChangeLevel never reaches scene save.
- Maximum-HP changes are excluded from combat/healing observation, so proportional
  conversion cannot be mistaken for an enemy hit for Bodyguard/Werewolf handling.
- Native `UpdateStatRPC` checks the master sender; `UpdateHealthRPC` accepts the
  master or owner. Public APIs, RPC attributes and parameter names were inspected
  in installed `Assembly-CSharp.dll`, SHA-256:
  `ce995a182ddc884ea965e87786f1986248d9616300fa825bcc04bca671ee6526`.

## Automated validation

- Release build: 0 warnings, 0 errors; Role UI build 509.
- `tools/Test-HealthUpgradeReset.py` generates a harness from production reset,
  upgrade dispatch, HP getters and lifecycle methods. 20,405 assertions pass:
  reported example; low/full/zero HP; every level reduction through Lv200;
  solo/host-to-vanilla transport fixture; shop persistence; saved scene/reload
  values; departed players; duplicate cleanup; missing transport; authority;
  normal positive/item upgrades; maximum offsets; ordinary damage observation.
- Existing OverhaulChecks: 75,937 pass.
- Existing LifterChecks: 249,993 pass.
- Existing revival/Phoenix checks: 6,594 pass.
- Installed-game field-access validation: 523 compiled references pass.
- Release Markdown validation: README 99,446 / 100,000 characters;
  CHANGELOG 17,821 / 100,000; UTF-8 without BOM. `git diff --check` passes.

## Artifacts and deployment

- Build, package and RSO_TEST DLL SHA-256:
  `1c47ca2f7b0f80ac494deddefde7ab7e23bfc0878c9b40c76476f366cb08dec2`.
- Previous build/package retained at
  `tmp/versions/before-build509-20261007-190402`.
- RSO_TEST deployed with the game closed; all seven package files verified.
  Previous profile/package retained at
  `tmp/deployments/before-build509-20261007-190543`.
- Configuration unchanged. Published v4.5.3 ZIPs remain unchanged, SHA-256:
  `921a52df372166f6a63f5c5d017b744bcdf802ed20319a01c5fbd72d49cdf109`.

## Remaining game verification

No fresh Unity/gameplay or host-only multiplayer test was performed. Native RPC
inspection and transport fixtures do not establish delivery timing under network
latency or interaction with other mods. In RSO_TEST, verify a Tank stage ending at
300/520 enters the shop at 69/120, keeps that value after reloading the save, and
behaves identically for an unmodded guest. Also check direct role change and a
low-HP survivor. Separate normal game healing from the role-removal conversion.
