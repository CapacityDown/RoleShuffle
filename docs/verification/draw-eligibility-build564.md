# Draw eligibility — v5.0.0 build564

Date: 2026-11-01. Source base: e23866cc85349534eedc5b28fef3c8633d1e57e4.
Branch: version/5.0.0.

## User-visible result

Roles → Role Guide → Draw eligibility (also linked from Role Settings) lists
all ordinary roles for the viewer: Eligible, Conditional, Excluded or Not yet
known, with reasons. The viewer's own base levels and role history are used.
The display follows the native game language in all 14 supported catalogs.
Excluded emblems are grayed out. Secret roles retain unrevealed titles/icons
and do not disclose conditional availability.

The preview represents a fresh whole-party draw under current conditions.
It is not an exact probability or a guaranteed result: sequential category
choices, uniqueness, team limits, King conflicts and teammate assignments can
change the outcome. History restrictions may relax when candidates run out;
Twins requires two eligible living members without history relaxation;
Imitator requires a possible copyable teammate. Before stage generation,
item-dependent roles remain unconfirmed. Missing host/player information is
explicitly shown, including older hosts that do not publish this feature.

Bomber remains eligible in singleplayer and one-player rooms, as requested
when the preceding exclusion change was cancelled. Actual draw policy,
settings, base levels, assignment histories and random state are not changed
by reading the preview.

## Implementation notes

- Reuse the production planner's candidate/history rules and controller's
  enabled pool, solo list, context filters and copied reroll history.
- Candidate inspection examines one role at a time rather than rescanning
  every role, and potential Imitator targets stop at the first match.
- Publish host-calculated per-player flags every two seconds only when the
  payload changes; guests never substitute their own settings. Scene/master
  identity, room changes and level generation reject stale snapshots. A failed
  scan clears published data instead of presenting it as current.
- Bounded ASCII codec: at most 24,000 characters, 30 players, 64 role fields
  per player; reject invalid IDs, flags, duplicates, numbers and malformed
  UTF-8/base64. A 30-player/48-role fixture occupied 8,219 bytes.
- A separate defect was found while checking plugin startup: build562's new
  MusicianPatches class had not been registered in the explicit Harmony startup
  list. Added its registration and a compiled startup-IL assertion. The earlier
  Musician runtime tests did not detect that missing startup call.

## Verification

- Main Release build564: zero warnings and zero errors. Auto profile copying
  disabled. Build563 also passed; build564 contains the final inspection-cost
  reduction and UI formatting.
- DrawEligibilityChecks: 2,029 assertions using production planner, preview,
  codec, sync and menu-row sources, plus extracted production pool/history
  methods. Native world/upgrade data, Unity, room and menu primitives are
  deterministic stand-ins. Covers per-player caps, history, paired roles,
  solo Bomber preservation, no RNG/history/assignment mutation, equal seeded
  draws before/after inspection, malformed packets, full-lobby round trip,
  localization, host/client separation, stale rooms/scenes/hosts and failures.
- Secret-role checks: 96.
- Installed unmodified game assembly field references: 622.
- R.E.P.O. v0.5.0 calls, Harmony bindings and reflection contracts: 605.
- Role settings: 362; ability configuration: 374; Lifter migration: 19;
  combat migration: 23; Hunter migration: 72; Courier migration: 491;
  base settings: 128; base save adjustments: 248.
- Localization: 15,156; guide sync/Photon protocol: 2,187.
- Musician runtime/native hook/startup checks: 244.
- Added missing CJK glyphs from the approved font sources; glyph coverage passed.
- README: 99,928 characters; CHANGELOG: 22,428. UTF-8 without BOM, LF;
  changelog headings directly precede their first bullet.
- All 47 GitHub README icon URLs matched the local approved images.
- ZIP contains exactly the seven approved files; every archived file matches
  its package source. Previous ZIP, DLL, profile and configuration preserved.

## Deployment and artifact

RSO_TEST deployed while REPO was stopped. All seven files verified by SHA-256.
Configuration was not changed. Backup:
`tmp/deployments/before-build564-20261101-064535`.

- DLL SHA-256: `2cf673742d9278b06dd9c0b412cd8e18b60dcb523a23b73883328227b483b500`
- ZIP SHA-256: `de45fd3d931319f0df2b6c695a228e91c4e5ef94f5e5d8514c545ebe202383dd`
- ZIP size: 5,339,637 bytes.
- Outputs: `RoleShuffle.zip` and `output/release/RoleShuffle-5.0.0.zip`.

Live Unity visual/layout checks and host-only multiplayer sessions were not
performed. Automated menu checks exercise row content, not actual rendering.
No remote release was published. Git publication status is recorded separately
in the local release manifest after verifying the remote branch.
