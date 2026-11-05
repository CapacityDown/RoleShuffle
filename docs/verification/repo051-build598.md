# R.E.P.O. v0.5.1 compatibility — RoleShuffle v5.0.1

Date: 2026-11-05. UI build: 598. Branch: `version/5.0.1`.

## Installed game

- `PlayerSettings.bundleVersion`: `0.5.1`, extracted from the installed
  `globalgamemanagers` asset; Unity build `6000.3.17f1`.
- Steam public build `26407089`; depot 3241661 manifest `3071300821150093080`.
- `Assembly-CSharp.dll`: 6,074,880 bytes; SHA-256
  `7df0ff1d03755a71a6755d01fc4c58c3cc044f8aa740f3d1c1379a6d6bd66724`.
- Official [v0.5.1 announcement](https://steamstore-a.akamaihd.net/news/externalpost/steam_community_announcements/1848664643340421)
  was retrieved through Steam's `ISteamNews/GetNewsForApp` API. Checked its
  translation, TTS, optional input binding, melee grab and cart-related changes
  against installed assemblies rather than relying on older search results.

## Changes

- Rebuilt against installed v0.5.1 assemblies. MOD version stays 5.0.1.
- Upgrade-name caching now observes native localization-change callbacks, so
  changing or reverting a custom translation in the same locale updates the
  roulette. A visible result refreshes without rerolling or changing network IDs.
- Cache and subscriptions follow manager/table replacements. Plugin shutdown
  detaches the callback. Missing startup tables retain the existing fallback/retry.
- Porter native verification now accepts cargo-volume initialization in Awake
  or Start and verifies that ObjectsInCart still reads that volume. v0.5.1 moved
  initialization to Awake; the production cart adapter required no change.
- Current build/install instructions and player-facing requirements now name
  v0.5.1. Compatibility appears first in the 5.0.1 changelog; historical release
  entries and release archives remain unchanged.

## Verification

- Release build 598: zero warnings/errors. Automatic profile copying disabled.
- Built DLL: 670 installed game calls, Harmony and reflection contracts;
  723 game field references; 15 v0.5.1 localization/compiled refresh contracts.
- Porter native: 39; DualWielder native: 118; Courier native: 20 checks passed.
- MenuLib 2.6.0 reference check: 150; deployed REPOConfig 1.2.7: 123 checks passed.
  Dependency versions unchanged. These counts include the shared reflection
  inventory and are not counts of unique dependency calls.
- GameUiChecks: 787, including same-language override enable/revert, cache reuse,
  replacement manager/table, startup fallback, callback cleanup and resubscription.
- Lifecycle runner: all 12 suites passed. Lifter 253,050; Review 2,704;
  Overhaul 84,371; Influenza 154; Twins 58; Porter 13,053; Negotiator 140;
  Musician 244; DualWielder 7,377; MageBeam 17,228; UtilityRuntime 74;
  UpgradeItem 89. Local logs: `tmp/repo051-lifecycle`.
- Additional targeted suites passed: Engineer 359; Scroll 243; Expression 111;
  HunterBattery 85; Rigger 157. Native IL-sensitive suites used installed v0.5.1.
- Documentation: 1,590 bilingual setting/default/range/table checks passed.
  README: 49,801 characters, below 100,000. CHANGELOG: 24,005 characters.
  Both UTF-8 without BOM; no blank line after a version heading.
- `git diff --check` passed before commit.

## Deployment and restoration

- RSO_TEST deployed at `2026-11-05T14:01:23.8852990Z` while R.E.P.O. was closed.
  All seven package files matched their deployed hashes. No config values changed.
- Previous DLL/profile/config backups: `tmp/deployments/before-build598-20261105-140123`.
- Build/package/deployed DLL SHA-256:
  `1566c7ed78444db40ba5daaaf62d468aa10f89e352e4b5e43989373882723a54`.
- Existing `RoleShuffle.zip` and `output/release/RoleShuffle-5.0.0.zip` preserved:
  SHA-256 `8922f890529184dd953bc488f767d4b41a9256201f6a042bc168f93260211a32`.
  A new release ZIP was not requested or generated.

## Limits

Gameplay regression suites mostly use production C# with simulated Unity/network
adapters; native IL/assembly checks are separate. No live game launch, rendered UI,
physics session, or host-only multiplayer session was performed. Passing assembly
and regression checks does not establish those live behaviors. This update keeps
existing role balance and settings; game DLLs and decompiled sources remain local
and untracked.
