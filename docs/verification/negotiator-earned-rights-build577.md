# Negotiator earned rights — v5.0.0 build577

Date: 2026-11-02 (JST). Source base: d374ab418b7d71101813357a157f41e9ee8264cf.

## Behavior

- Receiving Negotiator grants zero rights. Each stage has two independent
  earning sources, each payable once; hold at most two rights.
- Delivery: personally carry a positive-value Big, Wide, Tall or VeryTall
  valuable at least 20 m outside the truck, then release it at an extraction
  point. Tiny, Small, Medium, truck delivery and truck/extraction overlap are
  excluded. Progress belongs to the player and object; unheld movement and
  another player's distance cannot be credited. Re-grabbing the same object
  preserves only personally carried distance. Release refreshes native room
  membership, including between polling ticks.
- Distance uses player travel, not object shaking. The discontinuity guard
  scales with elapsed time and allows native Speed levels through 200.
  Held-object filtering precedes value reflection; one scan is shared across
  eligible Negotiators every 0.25 seconds.
- Kill: the existing attributed HurtCollider damage callback awards only a
  confirmed lethal hit against a native Tier3 enemy. Assists, Tier1/2 and
  compatibility reward-tier bonuses do not qualify. Unknown enemy tiers fall
  back to Tier1 and cannot earn rights.
- Death or losing the capability revokes all rights and blocks both sources
  until the next stage, even if no right had been earned yet. Revival and
  reassignment do not bypass the lock. Inherited capabilities use the same rules.
- Normal stage completion retains rights for the next shop. A successful
  guaranteed discount spends one; duplicate completion callbacks cannot spend
  the second. Interrupted negotiations spend none. Shop exit expires leftovers.
- Stage and shop HUD use a maximum of two. Spent shop holders remain identifiable
  at 0/2; revoked holders disappear. Earnings use the player's notification queue.
- All 21 current guide translations and EN/JA release documents describe the
  final rules. Old host translation keys remain available. Three Noto subsets
  gained missing characters without removing existing glyphs.

## Verification

- Final Release build577: zero warnings and errors.
- Production Negotiator state/runtime/patches with lightweight game adapters:
  131 checks, covering category/tier boundaries, 20 m boundary, released/held
  movement, high Speed, authority, revocation before/after earning, source caps,
  carryover, shop HUD, consumption, interrupted and duplicate callbacks.
- Installed game field access: 627; v0.5.0 API/Harmony/reflection contracts: 612.
- Native IL inspection confirms HurtCollider.EnemyHurt returns true after its
  EnemyHealth.Hurt call; native lethal damage clamps current health to zero.
  Controller wiring uses confirmed before/after health and native enemy tier.
- Guide sync/native Photon: 3,237 checks. Localization: 25,330 checks.
- Font/catalog coverage: 6,720 phrases plus Japanese key/placeholder checks.
  Installed TMP shaped all 672 Arabic/Thai catalog values; output glyphs are
  covered by bundled fonts. These checks used the same final guide/font bytes
  before the gameplay-only build577 correction.
- README: 99,988 decoded characters; CHANGELOG:
  23,023; UTF-8/LF and version heading format validated.
- Seven ZIP entries match package sources; all 47 pinned GitHub icons verified.
- Live gameplay and host-only multiplayer delivery/kill/shop testing were not
  performed. Unity/game adapters and native assembly checks are not live tests.

## Delivery

RSO_TEST was stopped. All seven package files deployed and hash-verified.
Configuration was unchanged. Previous ZIP, package DLL, profile and configuration:

D:/Documents/R.E.P.O. MOD/RoleShuffle/tmp/deployments/before-build577-20261101-194158

DLL SHA-256: f5525264653b3aa5dcdd9c41470f2484329aad8500683136a3170228131eed20

ZIP SHA-256: 96fb171f0c19e406a47981dacdd9b7e7409c1af5a9a034770940fd982b9b5532; 5,634,198 bytes.

RoleShuffle.zip and output/release/RoleShuffle-5.0.0.zip are identical.
No remote release or Thunderstore publication was performed.
