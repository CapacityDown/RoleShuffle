# RoleShuffle 4.5 checks

Run `dotnet run --project tools/OverhaulChecks/OverhaulChecks.csproj -c Release`.

This harness links the production growth rules, contract/aura runtime, capped-healing logic, status codec and status transport. Unity, player health and Photon are deterministic substitutes. It checks cap boundaries, idempotent targets, contract delivery and duplicate prevention, grace preservation, dead/replaced avatars, zero-value exclusions, King self-exclusion and shared budget, inactive scan cost, malformed and stale data, copied roles, guest reads, room changes and host-only publication.

`tools/Test-BaseUpgradeEligibility.ps1` additionally extracts the actual production eligibility and target-composition methods, using stubbed base/role inputs. It covers legacy eligibility, one useful Runner upgrade, both upgrades at the cap and Superbot with zero bonus.

## In-game acceptance checks

- New and existing settings, overhaul on/off, low/high/capped Base Upgrades.
- Tank/Runner/Lifter assignment, Imitator copy, Superbot and stage cleanup.
- Lifter: with configured minimum 25, test Base Strength 15, 20 and 24 (target 25, eligible), 25 and 50 (no extra gain, excluded), and 90 (target 95). The configured minimum takes priority even if normal light/heavy grip or rotation weakens. Only growth above max(Base, minimum) must preserve those forces. Copied/Superbot abilities share the rule. Verify legacy mode still uses the fixed configured level.
- At Base Strength 70, configured minimum 200 must apply 200. By contrast, minimum 25 plus bonus 130 must preserve 70 because this extra growth would weaken heavy-object rotation.
- Jobless: initial grace; different nonzero valuables; no contract after a drop, death, teleport or duplicate delivery; limit retained after reconnect.
- King: self-exclusion, radius, injured/full/dead targets, overlapping Medic/Mage healing, delayed acknowledgements, final budget and reconnect.
- Stinker/Bomber: existing automatic hazards, truck restrictions and stage cleanup. No manual pause command.
- Host with a vanilla guest, host with an installed guest, room changes and old hosts without ability data.
- HUD in English, Japanese and a long-label language; six players, all supported anchors/scales, small screens, fonts, page transitions and editor Save/Cancel.
- Stage Flux and Elite Enemy Variants present and absent, including revival ordering.

Automated checks do not establish actual Unity layout, live networking or compatibility behavior. Keep those results separate in the development record.
