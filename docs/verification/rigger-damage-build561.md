# Rigger cable damage — v5.0.0 build561

## Behavior

- Added Rigger.EnemyDamageMultiplier, default 2, configurable from 1 to 10 in
  REPOConfig. Direct Human Adventure Cable hits against enemies use this factor.
- Enemy restraint remains 1.5x; hit/throw durability cost remains 0.5x by default.
  Player damage/restraint, ordinary weapons and thrown-object collision damage
  do not receive the new damage factor.
- Current enabled/authority/living/role state is checked on every hit. Role
  changes, stage cleanup and death remove the bonus immediately. The existing
  capability path also supports copied/inherited Rigger abilities.

## Implementation

The native cable reparents its strike collider outside the weapon hierarchy in
Awake. A weak-key registry records that exact collider and its cable after Awake.
The native strike captures playerCausingHurtOverride; this identifies the original
attacker even if the weapon changes hands before impact. Inactive cables and
replaced/unregistered colliders are excluded.

A narrow HurtCollider.EnemyHurt transpiler scales only the integer damage passed
to EnemyHealth.Hurt. Native positive-damage/health gates, stun, instant-kill and
death-pit paths are preserved. No weapon/collider damage field is rewritten,
so repeated hits cannot compound and no restoration state persists. Invalid
multipliers fall back to native damage; damage uses native integer rounding
and the existing 100,000 damage ceiling. Other role multipliers still compose
through the existing pre-hit overrides.

Enemy hits use the host path and require no new client messages. This was
verified against installed-game IL; live unmodded-client play remains untested.

## Verification

| Check | Result |
|---|---:|
| Main Release build561 | 0 warnings / 0 errors |
| Rigger runtime and installed-game IL | 157 passed |
| Role settings | 362 passed |
| Ability configuration | 362 passed |
| Localization | 14,256 passed |
| Guide/native Photon round-trip | 2,187 passed |
| Compiled game-field references | 619 passed |
| Installed v0.5.0 calls, Harmony/reflection contracts | 591 passed |
| CJK/European catalogs and Japanese font coverage | No missing glyphs |
| README GitHub icon downloads | 47 byte matches |

Rigger checks cover configured/default damage, integer rounding, overflow,
zero damage, repeated hits, inactive/replaced/unregistered colliders, missing
attackers, handoffs in both directions, role changes/death/cleanup, non-hosts,
disabled state, inherited capability, invalid settings and ambiguous IL.
Existing hold-duration and durability checks remain. Unity/player/controller
lookup objects in the executable checks are stand-ins.

All 14 guide languages describe the new factor and preserve custom host values.
README is 99,988 characters, UTF-8 without BOM/LF; release Markdown checks pass.
The v5.0.0 Rigger changelog bullet describes its final behavior, without adding
within-version development history. Artwork was not changed.

## Delivery

- RSO_TEST deployed while the game was stopped; all seven files verified.
- Previous package/plugin/configuration/log/ZIP retained in
  tmp/deployments/before-build561-20261031-071612.
- ZIP contains exactly seven required entries, byte-matched with package. Root
  and versioned archives match.
- DLL SHA-256: 6c62fcc311d47d23b70bea65658612581377205342407ae3333359549754d5e8.
- ZIP SHA-256: 7553d625f65280ddc0566de8b8dc2c6c0e7d98dabba47fef56e8ebb7c34c09e4.
- Actual cable damage, rendered guide and host-only multiplayer were not tested
  in a live game. No remote release was published.
