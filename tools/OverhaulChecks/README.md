# RoleShuffle 4.5 checks

Run `dotnet run --project tools/OverhaulChecks/OverhaulChecks.csproj -c Release`.

This harness links the production growth rules, contract/aura runtime, capped-healing logic, status codec and status transport. Unity, player health and Photon are deterministic substitutes. It checks cap boundaries, idempotent targets, contract delivery and duplicate prevention, grace preservation, dead/replaced avatars, zero-value exclusions, King self-exclusion and shared budget, inactive scan cost, malformed and stale data, copied roles, guest reads, room changes and host-only publication.

Courier delivery rewards use fixed, independently configured HP and grace for all seven vanilla size categories. Coverage includes both truck and extraction destinations, held and just-released valuables, item/player volume mismatch, over 100 deliveries, duplicate prevention, zero healing, maximum-health clamping, shorter-grace preservation, local/remote ownership, stale host HP, pending Medic/Mage reservations and no revival. Native transport and room queries remain simulated.

`tools/Test-BaseUpgradeEligibility.ps1` additionally extracts the actual production eligibility and target-composition methods, using stubbed base/role inputs. It covers configured minimums, cap-based exclusion, fixed Lifter targets and Superbot with multiplier 1.

## In-game acceptance checks

- New and existing settings, migration from both old mode-switch values, low/high/capped Base Upgrades.
- Tank/Runner/Lifter assignment, Imitator copy, Superbot and stage cleanup.
- Tank: Base Health 100 with minimum 21 and multiplier 1.5 gives level 153 (3160 HP); cap 4100 HP. Runner: Base Speed 6 gives 12; Base Stamina 46 gives 71. Confirm serialized vanilla defaults 5/40 and speed-dependent stamina drain.
- Lifter: native display remains 200; host grip/rotation coefficients use vanilla level 1 below mass 2, and each curve's maximum across levels 0–200 for heavier objects (143/24 at level 50). Base level 50 is excluded from random assignment because it already reaches the target. Verify copied roles, Superbot, mixed grabbers, stage cleanup and special overrides. See `tools/LifterChecks` for emitted-IL and installed-game checks.
- Schema 37 migration: exact config backup, removal of the old mode switch and unused Lifter/status settings, retention of current role tuning and selection preferences, repeat migration.
- Courier: initial grace; all size categories and both delivery areas; different nonzero valuables; release between scans; no reward after an earlier drop, death, teleport or duplicate delivery; per-item history retained after reconnect. Schema 39 preserves the old Jobless settings and alias while removing the completion cap.
- King: self-exclusion, radius, dead targets, multiple Kings, safe Strength selection, upgrade changes while supported, stage cleanup and reconnect.
- Stinker/Bomber: existing automatic hazards, truck restrictions and stage cleanup. No manual pause command.
- Host with a vanilla guest, host with an installed guest, room changes and old hosts without ability data.
- HUD in English, Japanese and a long-label language; six players, all supported anchors/scales, small screens, fonts, page transitions and editor Save/Cancel.
- Stage Flux and Elite Enemy Variants present and absent, including revival ordering.

Automated checks do not establish actual Unity layout, live networking or compatibility behavior. Keep those results separate in the development record.
