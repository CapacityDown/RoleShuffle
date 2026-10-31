# Musician — v5.0.0 build562

## Behavior

- Supports Piano, Harp, Arctic Guitar and Desert Banjo, plus Pan Flute,
  Harmonica and Mouth Harp. Boombox and the unused Ocarina enum are excluded.
- All instruments share one performer's healing clock: default 1 second.
  Continuous instruments require active, attached and audible native playback.
- Self and living teammates within 10 m receive 5 HP per pulse by default.
  Combined stage allowance is 1000 HP per performer, not per recipient.
- Cooldown and budget survive instrument switches, role changes, avatar
  replacement, death and revival. Stage cleanup resets the ledger.
- REPOConfig: Musician.HealCooldownSeconds = 1 (0.1–30),
  Musician.TotalHealingLimit = 1000 (1–10000). Existing amount/radius remain.
- Cyan music-note HUD shows remaining budget. Exhaustion uses the existing
  per-player notification queue with MusicEmpty. Updated all 14 guide languages.

## Host-only behavior and limits

Legacy notes use native PlayKey after the game's RPC validation. New instruments
use native SoundUpdate and its attached player/loudness state. The controller
checks active stage, authority and current Musician capability before healing.
The supported new instruments also count toward context eligibility.

Healing uses the existing native HealOther path, so no new client RPC is required.
The shared capped-healing service serializes pending heals per recipient. Local
heals charge actual restored HP. Remote sends reserve missing HP; ambiguous or
unacknowledged results keep their reservation to prevent delayed messages from
exceeding the allowance. Therefore remote lost/clamped heals can consume budget
without restoring that full amount. Live voice playback and host-only transport
have not been tested in the game.

## Verification

| Check | Result |
|---|---:|
| Main Release build562 | 0 warnings / 0 errors |
| Musician production runtime/hooks and installed-game contract | 243 passed |
| Ability configuration | 374 passed |
| Role settings | 362 passed |
| Localization | 14,256 passed |
| Guide/native Photon round-trip | 2,187 passed |
| Resource HUD, budgets, codec and synchronization | 84,280 passed |
| Compiled game-field access | 621 passed |
| Installed v0.5.0 calls and Harmony/reflection bindings | 595 passed |
| README GitHub images | 47 byte matches |

Tests include rapid notes, 30/60/144 FPS continuous playback, independent performer
clocks, partial final heals, full/dead/inactive/distant recipients, duplicate
recipients, changed settings, local failures, remote reservations/timeouts,
late callbacks and supported/attached/active/audible instrument gates. Runtime
objects are deterministic stand-ins; native method/field contracts use the
installed game assembly. Existing wire metric IDs remain unchanged.

Reviewed the music-note HUD raster on light/dark backgrounds at 150, 50 and
25 pixels. New CJK characters were added to the existing font subsets; font
coverage checks pass. README: 99,954 characters, UTF-8 without BOM and LF.

## Delivery

- R.E.P.O. was running. RSO_TEST was left unchanged; deployment is pending.
- Updated package DLL and both local release ZIP paths. No remote release.
- Previous package DLL, release archive/metadata and log retained in
  tmp/deployments/before-build562-20261031-083211.
- Archive contains exactly seven required files, each byte-matched to package.
- DLL SHA-256: b4884cfbfccf2dffb9b55be1fed702b7f09d3032500f0c71bcaffd9954823ac0.
- ZIP SHA-256: ccce5ffa87ce39a385b06b5b4650864d692a95be7ab143dcfe4df65442953977.
- Actual game healing, live voice/host-only multiplayer and in-game HUD remain
  unverified. Generated HUD raster inspection is separate from in-game testing.
