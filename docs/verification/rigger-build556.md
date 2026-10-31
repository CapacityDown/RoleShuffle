# Rigger — v5.0.0 build556

## Behavior

- Public role/test/icon number 45; ordinary enum value 44, wire ID 45.
  Existing wire ID 44 still decodes as DualWielder. Secret IDs are unchanged.
- Human Adventure Cable enemy restraint lasts 1.5x as long by default;
  hit and throw durability costs are multiplied by 0.5.
- REPOConfig settings: Rigger.EnemyHoldDurationMultiplier (1–3) and
  Rigger.DurabilityConsumptionMultiplier (0.1–1), plus Enabled and Weight.
- Uses normal cable controls, without new input actions or client RPCs.
  Player restraint and damage are unchanged. Native obstruction, release,
  empty-battery, self-hit and once-per-swing checks remain.
- Added to Standard, Cooperative and Challenge; Support balance/icon category.
  The existing ExcludeUnavailableContextRoles policy removes Rigger when no
  non-valuable Human Adventure Cable is available. Test assignment can override
  random eligibility. Imitator can copy it; Superbot inherits its capability.

## Implementation and host-only assessment

The installed ItemMeleeWhip runs enemy holding and durability spending on the
host. Three narrow Harmony transpilers change the arithmetic at native spending
points. EnemyHoldTick consumes elapsed time divided by the current duration
multiplier, so an ongoing hold returns to the normal countdown rate when the
role ends. ReleasePhysics scales its current holder's throw cost. Delayed hits
in CrackDurabilityUse use the holder captured in the original CrackHitWindow,
not a later owner of the same cable. Each consumption checks current role,
living status, stage readiness, enabled state and authority.

No persistent weapon field, save value, damage, client input or native network
message is rewritten. Invalid/out-of-range settings are guarded. Unsupported
native IL fails patch installation explicitly. Host-only operation is supported
by this code path; an actual unmodded-client session has not been exercised.

## Presentation

- Overview and configured Details in all 14 supported languages; host guide sync.
- Added five needed glyphs each to the Chinese Simplified, Chinese Traditional
  and Korean font subsets. Other fonts/artwork retained.
- Rigger icon generated with built-in image_gen, preserved at full resolution in
  Assets/role-emblems-semibot-v1/selected/45-Rigger.png. Prompt retained in that
  directory's prompts.json; normalized master retained locally in transparent/masters.
- Support background normalized to exact #24563E using the inherited mask tool.
  Native exterior alpha retained; pixels outside the mask unchanged. Inspected
  on light/dark backgrounds and at 256/64/32 px. Runtime is 256x256 RGBA.
- Public 64px thumbnail stored at docs/icons/45.png, referenced from README using
  immutable artwork commit ea93c5885f8e462c0af8e5196a5ebe510de043d9.
- Regenerated public/full icon catalogs. Public hides both secret names/artwork.

## Verification

| Check | Result |
|---|---:|
| Main Release build | 0 warnings / 0 errors |
| Rigger runtime and installed-game IL | 117 |
| Role settings and presets | 358 |
| Ability configuration | 356 |
| Localization | 14036 |
| Guide and native Photon protocol round-trip | 2144 |
| Role parsing, copying and secret-role rules | 92 |
| Compiled game field access | 613 |
| v0.5.0 calls, Harmony and reflection contracts | 553 |
| Embedded Rigger icon vs approved runtime hash | Match |
| Non-Japanese catalog glyph coverage | 13 catalogs, no missing characters |
| New Japanese text font coverage | Pass |
| GitHub README icon downloads | 46 byte matches |

Runtime tests cover hold durations at 30/60/144 FPS, all three difficulty
ranges, configurable multipliers, fractional spending, role changes, stage end,
death, missing holders, shared cables, disabled/non-host state and invalid
settings. Input/Unity/player objects are stand-ins; the patched method IL is
read from the installed game. Settings migration/Base/save checks also passed.

## Delivery

- RSO_TEST deployed while R.E.P.O. was stopped; all seven files hash-verified.
- Previous plugin/config/log/ZIP preserved in
  tmp/deployments/before-build556-20261031-031510.
- README: 99,402 characters; CHANGELOG: 21,575. UTF-8 without BOM/LF, and no blank
  line between changelog version and first entry.
- Release ZIP: exactly the seven required files, byte-compared with package.
- Build/package/deployed DLL SHA-256:
  cdef60052763ad481b0687bf6992341dee316b4aae4b5a8ca9bf39ad0cd5ffce.
- ZIP SHA-256:
  998b8503110eabd086d8561f0cbc21a7f0306e52fd99814ad45f480349b8e7d9.

## Remaining manual verification

In-game cable handling, obstruction, expiry visuals, original/delayed hit reports,
role changes during an active hold, rendered translated guide and host-only
multiplayer have not been tested live. Negotiator remains a proposal, not code.
