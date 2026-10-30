# Strength upgrades — v5.0.0 build550

## Scope and behavior

- Lifter grants `Lifter.StrengthLevel` (default 200, range 0–200). Heavy valuables, carts and bikes use the native v0.5.0 Strength calculation. The former absolute heavy grip/rotation override is removed.
- Light valuables and purchased equipment retain the configured holding level (default 1) to preserve the shaking mitigation. Carts and bikes are excluded. Native temporary overrides, tumbling and attack behavior remain in force.
- King grants configured temporary Strength levels directly, capped at total level 200. The obsolete non-weakening level search is removed.
- Twins co-carrying converts its configured multiplier (default 1.5) into temporary native upgrade levels, rounded up, capped at 200. This targets the raw native Strength value `1 + 0.2 × level`; the resulting grip/torque still follows native physics and is not guaranteed to be exactly 1.5 times the final force. Native heavy-object force plateaus around level 50 through 200, so higher upgrade levels may provide no further heavy-object force.
- King and Twins share ownership of their combined temporary grant. Repeated ticks cannot compound the grant. Leaving range, releasing the shared valuable, death, disconnect, absolute resets and stage cleanup remove only the tracked temporary portion. Purchases during the effect survive expiry.
- Superbot and copied Lifter abilities inherit the configured Lifter upgrade and light-item handling. Strength reset cache reconciliation remains to prevent the prior Lifter-to-Runner carryover.
- Full source audit: Influencer and Berserker already use native Strength upgrade grants. Avenger modifies attack damage, not Strength, so its damage rules remain unchanged. No post-helper role force multiplier remains.
- Lifter random eligibility compares native heavy-object benefit at the configured target. Twins no longer depends on the Lifter item-handling patch.
- Configuration schema 45 backs up schema-44 settings as `.pre-v5.0.0-lifter-upgrades.bak`, removes obsolete heavy grip/rotation multipliers and preserves light handling and unrelated preferences.

## Automated verification

Release build: zero warnings and zero errors against installed R.E.P.O. v0.5.0 assemblies.

| Check | Passed |
|---|---:|
| Lifter installed-game IL, emitted IL, native handling and host runtime | 252090 |
| Twins production co-carry/runtime | 32 |
| King/Twins ledger, role growth, Courier and ability synchronization | 83986 |
| Combat and Ghost carrier aura | 73 |
| Base eligibility and upgrade targets | 15364 |
| Lifter configuration migration | 19 |
| Ability configuration | 344 |
| Combat / Hunter / Courier migration | 23 / 72 / 425 |
| Role / Base settings / save adjustments | 349 / 128 / 248 |
| Localization | 13842 |
| Guide synchronization and native Protocol18 serialization | 2100 |
| Upgrade item retention | 89 |
| Installed game field references | 604 |
| Installed v0.5.0 calls and patch bindings | 545 |

The new ledger regressions cover different player baselines, repeated ticks, King overlap and expiry order, purchases, absolute reassignment, death, disconnect, levels above 200, client authority and send failure after local application.

## Package and deployment

- README: 98,451 characters; CHANGELOG: 21,113 characters. UTF-8 without BOM, LF; no blank lines between version headings and first bullets.
- All 14 role-guide catalogs updated; 45 public GitHub icon downloads byte-compared with local approved thumbnails.
- ZIP: exactly seven approved entries, each byte-compared with package sources.
- RSO_TEST received all seven package files with the game stopped; each deployed file matched its source. Settings are migrated by the MOD on the next startup.
- Previous DLLs, ZIP and profile settings preserved at `tmp/deployments/before-build550-20261030-041825` (the last build548 ZIP and intermediate build549 DLL remain recoverable).
- Build/package/deployed DLL SHA-256: `abfff50fe3abdcaa40e5f5d66a31af6fb238c2fd116fb1f871b0e93957ddfd16`.
- Release ZIP SHA-256: `a41c9012de17806931db859eea8b558cee36216d937c186d2e45c8ce6b58c6ff`.

## Limits

No live Unity gameplay or vanilla-client multiplayer session was performed. Physics behavior and host networking were checked with production-code tests, simulated recipients and the installed game assembly; actual in-game operation still needs confirmation. No Thunderstore or GitHub release was published.
