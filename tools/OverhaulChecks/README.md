# RoleShuffle 4.5 checks

Run `dotnet run --project tools/OverhaulChecks/OverhaulChecks.csproj -c Release`.

This harness links the production growth rules, contract/aura runtime, capped-healing logic, status codec and status transport. Unity, player health and Photon are deterministic substitutes. It checks cap boundaries, idempotent targets, contract delivery and duplicate prevention, grace preservation, dead/replaced avatars, zero-value exclusions, King self-exclusion and shared budget, inactive scan cost, malformed and stale data, copied roles, guest reads, room changes and host-only publication.

`tools/Test-BaseUpgradeEligibility.ps1` additionally extracts the actual production eligibility and target-composition methods, using stubbed base/role inputs. It covers legacy eligibility, one useful Runner upgrade, both upgrades at the cap and Superbot with multiplier 1.

## In-game acceptance checks

- New and existing settings, overhaul on/off, low/high/capped Base Upgrades.
- Tank/Runner/Lifter assignment, Imitator copy, Superbot and stage cleanup.
- Tank: Base Health 100 with minimum 21 and multiplier 1.5 gives level 153 (3160 HP); cap 4100 HP. Runner: Base Speed 6 gives 12; Base Stamina 46 gives 71. Confirm serialized vanilla defaults 5/40 and speed-dependent stamina drain.
- Lifter: default minimum 25, multiplier 1.5 and effective cap 6. Test Base 0→25, 25→200, 50→50 (excluded), 70→70 (rotation protection), 90→156, 198→200. Configured minimum takes priority; growth above it must preserve grip and rotation in both weight classes. Verify copied roles, Superbot, multiplier 1, effective caps, level 200 and legacy mode.
- Schema 32 migration: back up old Base*Bonus settings; preserve minimums and existing new settings; expose four multipliers and four effective caps. Confirm no fractional upgrade grants.
- Jobless: initial grace; different nonzero valuables; no contract after a drop, death, teleport or duplicate delivery; limit retained after reconnect.
- King: self-exclusion, radius, injured/full/dead targets, overlapping Medic/Mage healing, delayed acknowledgements, final budget and reconnect.
- Stinker/Bomber: existing automatic hazards, truck restrictions and stage cleanup. No manual pause command.
- Host with a vanilla guest, host with an installed guest, room changes and old hosts without ability data.
- HUD in English, Japanese and a long-label language; six players, all supported anchors/scales, small screens, fonts, page transitions and editor Save/Cancel.
- Stage Flux and Elite Enemy Variants present and absent, including revival ordering.

Automated checks do not establish actual Unity layout, live networking or compatibility behavior. Keep those results separate in the development record.
