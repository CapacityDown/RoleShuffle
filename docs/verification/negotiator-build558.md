# Negotiator — v5.0.0 build558

## Behavior

- Public role/test/icon number 46, enum 45, network ID 46. Previous IDs remain stable.
- Receiving the capability grants one non-stacking right for the next shop.
  Death, role replacement and avatar reconnection preserve it within the session.
  Copying via Imitator and inherited capabilities use the same once-per-stage grant.
- The holder personally presents a shop item to the native Shopkeeper. Its normal
  successful discount is guaranteed without taking the rejection/destruction path.
  Ordinary shared attempts are neither required nor consumed. Further negotiations
  return to normal after that right is spent.
- Consume only after the item's price actually decreases. Interrupted attempts
  release their reservation without consumption. Remaining rights expire on leaving
  that shop; run reset, main menu, lobby menu, game-over arena and authority loss clear
  session state. Rights are not persisted across application restarts or host migration.
- Support category; Standard and Cooperative presets. REPOConfig Enabled and Weight
  settings are available. Ability is intentionally one use with native discount values.

## Shop identification

- Connected entitled players remain in the role list in the shop, including after use.
  The local holder sees a price-tag HUD symbol and 1/1 or 0/1. HUD/Menu settings apply.
- Entry announces `Negotiator` through that player's native chat/TTS. `/roles` returns
  `YourRole:Negotiator` there. Successful use sends the short `Discounted` notice.
  Automatic notices respect AnnouncementsEnabled; ordinary per-player queues remain.
- Reuse the existing role and budget protocols. Shop snapshots do not keep unrelated
  combat roles or temporary upgrades active. Unchanged role maps avoid repeat writes.
- Notifications share the existing notifier, hosted on the persistent plugin object
  so they continue after the stage controller's GameObject is deactivated.

## Native integration and limits

Four small transpilers retain the installed v0.5.0 Shopkeeper's validation, scan,
animation and RPC flow. Attempt comparisons bypass the shared limit for an eligible
holder; the counter increment is neutralized without rewriting the counter/RPC payload.
The positive decision is forced before ItemShownSet records rejection. A reservation
binds the holder to the accepted Shopkeeper/object until completion or native cleanup.
Native price-change RPCs and speech support unmodified guests. This is code-level
verification, not a completed live host-only multiplayer test. Ordinary item damage,
pre-existing damage/effects and Shopkeeper reactions to player misconduct are unchanged.

## Artwork and documents

- Rejected anatomy was regenerated with built-in image_gen using approved Medic/Tank
  references: same-width shallow neck band, high single-piece cone arms, cylindrical
  torso/bottom rim and short cone legs. Source and all prompts retained under
  Assets/role-emblems-semibot-v1/selected. Support background normalized to #24563E.
- Native alpha and pixels outside the background mask preserved; reviewed on light
  and dark backgrounds at 256/64/32 pixels. DLL resource matches approved runtime PNG.
- Public icon 46 is pinned to artwork commit 598b87aa9a19793afa49b4ca7b809d17b19aeefd.
  Public/full catalogs rebuilt; public secret names and art remain hidden.
- Overview, Details and resource labels translated in all 14 supported catalogs.
  Added required glyphs to Korean and Chinese subsets; Japanese coverage verified.
- README: 99,946 characters. CHANGELOG: 21,935 characters. UTF-8 without BOM, LF;
  no blank line after the version heading. All 47 GitHub image references byte-verified.

## Automated verification

| Check | Result |
|---|---:|
| Release build558 | 0 warnings / 0 errors |
| Production Negotiator runtime + installed-game IL | 56 |
| Role settings / ability configuration | 362 / 356 |
| Localization | 14216 |
| Guide sync and native Photon codec | 2187 (38577-byte payload) |
| Role IDs, copying and secrets | 96 |
| Overhaul, resources, icons and synchronization | 84132 |
| Assignment caching + shop snapshot checks | 108 + 3 |
| Notifications and private notice queues | 57 |
| Compiled game field accesses | 617 |
| Installed v0.5.0 methods, patches and reflection | 585 |
| Embedded Negotiator icon | SHA-256 match |

Runtime checks execute the production NegotiatorState, NegotiatorRuntime and patch
helpers with Unity/player adapters. They cover wrong player/item/keeper, death during
an accepted negotiation, retry, duplicate completion, shared attempt preservation,
shop entry readiness, display after use, reconnect and expiry/reset. Patch shape tests
read the installed Assembly-CSharp.dll. Tests do not run native Unity animations,
Photon transport or actual shop interactions.

## Delivery

- RSO_TEST deployed while R.E.P.O. was stopped. All seven files hash-verified.
- Prior plugin, settings, log, ZIP and release metadata preserved at
  tmp/deployments/before-build558-20261031-035537.
- ZIP contains exactly the seven required release files, byte-matched with package.
- Build/package/deployed DLL SHA-256: 230e11866a6174c7620ef202b6cb11f49a0894039efcdef9e18060504b50aad0.
- ZIP SHA-256: 077148341a9603ba1effa5a6efef5c8c053613e9c8e769993442980d6a5dd11c.

## Remaining live verification

Assign Negotiator in a stage, enter the next shop, check holder identification and
1/1, present an item, observe the normal successful discount and 0/1, then verify a
second attempt uses normal behavior. Also exercise interruption, all three ordinary
attempts already spent, reconnect, leaving with an unused right and an unmodified
remote player as the holder. Actual game and host-only multiplayer tests remain open.
