# DualWielder — build539

Date: 2026-10-11 JST. RoleShuffle v4.6.0, branch version/4.6.0.

## Behavior

DualWielder (ID 44, emblem 45) creates one matching native melee weapon while
its owner alone holds the original. Default left offset is 0.5 m and follow delay
is 0.3 s. Recorded position, rotation, hitbox orientation and attack state are
replayed by timestamp; movement interpolates without predicting attack flags.
Battery/durability is the original weapon's reserve: the copy's native hit paths
consume that same component. Empty reserves and native hit cooldowns stop damage.

Release, switching, death, role removal, departure, disabling the mod and stage
end remove the copy. Multi-player holding suppresses duplication. Copies cannot
be equipped, grabbed or registered as saved inventory. A failed network destroy
disables damage immediately and retries. Teleports and setting changes clear
history. The original weapon and its inventory identity are preserved.

Enabled by default with weight 100; included in Standard and Challenge. Normal
context filtering excludes it without a melee weapon. Imitator can copy it, and
Superbot includes the capability through the existing capability rules. No new
key or command is required. REPOConfig exposes the role switch, weight, delay
(0.05–2 s) and offset (0.1–1.5 m). The test selector accepts DualWielder and 二刀流.

## Implementation and host-only evidence

Native Photon instantiation and transform synchronization publish the matching
weapon. The host controls its physical pose and native HurtCollider attacks.
The copy's ItemMelee battery field points to the original ItemBattery. Its own
battery writer is disabled. Native EnemyOrPVPSwingHitRPC and SwingHitRPC both
consume that field. HurtCollider's native detector reads the explicit attacker
override, providing the owning player to role damage accounting.

Native item-name assignment is shared without calling ItemAdd or ItemRemove;
purchased item counts are unchanged. Save-list registration and host equip/grab
requests are rejected for marked copies only. Rejected grabs use the vanilla
owner-release RPC. No custom guest component or RPC is required by this design.

These are source and installed-assembly findings, not proof of live multiplayer
behavior. Actual hit timing, weapon-specific behavior, settling/visual effects,
vanilla guest rendering and remote grab/equip rejection still require gameplay
verification. Host-only clients retain their native prefab presentation.

## Verification

- Release build539: 0 warnings, 0 errors.
- DualWielderChecks: 7,357 production history/runtime checks using isolated
  Unity/native adapters, including 15/30/60/144 FPS and injected destroy failure.
- Installed game field access: 616 checks.
- DualWielder native contract: 71 checks, including 17 game method calls,
  reflected fields, all eight Harmony target signatures, native hit consumption,
  attacker attribution, inventory restrictions and embedded emblem hash.
- LocalizationChecks: 13,842 checks across 14 guide languages.
- GuideSyncChecks: 690 checks; native Photon Protocol18 payload 37,592 bytes.
- Ability settings: 362; combat migrations: 23; Hunter migrations: 72;
  role settings: 349; Base Upgrade settings: 128; save adjustments: 248.
- README: 99,733 decoded characters, UTF-8 without BOM, LF. Both language lists,
  preset counts, secret concealment and commit-pinned icon reference checked.
- Existing localization keys retained for compatibility. New translations have
  matching placeholders and bundled glyph coverage.
- Combat emblem uses #80451F with transparent exterior. Selected art, full-size
  master, generation prompt/hashes and 256px runtime/64px README exports retained.
  Light/dark and HUD-size comparisons reviewed. Full/public catalogs generated;
  public gallery visually reviewed and secret names/artwork remain concealed.
- DLL SHA-256: d381125f934587d1bb912b2cf9287a00c6decafcb1d916384c170d3686593e9f.

## Deployment

Deployed build539 to RSO_TEST at 2026-10-10T16:04:01.9218074Z with the game stopped.
All seven package/profile files matched SHA-256. Previous files are backed up in
`tmp/deployments/before-build539-20261010-160401`; user configuration and the prior
release ZIP are preserved. Actual gameplay and host-only multiplayer are untested.
