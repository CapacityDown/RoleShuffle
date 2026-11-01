# RoleShuffle working methods

The user instructed this task to inherit all working methods from
[RoleShuffle-S001](codex://threads/019fbdb5-6974-7f70-b783-b426be968fe3) and
[RoleShuffle-S002](codex://threads/01a091a3-dc1e-7463-bad9-ef2e5a5fd3d9).
These methods are retained below. Newer user instructions take precedence over
historical version numbers, deployment profiles and superseded design choices.

## Source and Git

- Work in `RoleShuffle`; preserve the original `StageRoles` checkout and previous
  release artifacts so the stable version can be restored.
- Keep the MOD name RoleShuffle. Continue the current development version (5.0.0)
  unless the user changes it. Do not revive the old overhaul setting.
- On 2026-10-03 the user requested a complete rollback of the Tuna changes in
  build503. Retain Tuna's build502 behavior: per-frame movement checks, stage-start
  grace only, and inactivity damage inside the truck. Do not reapply those safety
  changes without a new request. The Trickster cancellation fix remains in place.
- The user's standing authorization for Git operations is permanent for
  `https://github.com/CapacityDown/RoleShuffle.git` and is not limited to a
  branch. It includes local commits, merges, normal pushes (including `main`),
  and organizing branches with recoverable history. Do not ask again for each
  ordinary operation. Do not discard unrelated work or use destructive force
  pushes. This authorization does not override platform approval enforcement.
- On 2026-09-30 the user explicitly requested integration of the current work
  into `main`, including the role-safety audit, its diagnostic results and
  hashes, and the verification tools committed as `d4c2698`.
- Perform development in `RoleShuffle` on a branch named `version/<version>`;
  the current working branch is `version/5.0.0`. Commit and push ongoing work
  to that version's branch instead of directly to `main`.
- When the user changes the version, complete the relevant verification and
  merge the outgoing version's branch into `main`, then push and verify `main`.
  Create the incoming version's branch from the updated `main`, switch to it,
  and make the version-number changes there. Do not merge unfinished work into
  `main` on every ordinary commit; version changes are the integration point.
- On 2026-10-10 the user moved the unpublished v4.5.4 contents to v4.6.0.
  The existing CHANGELOG section is now v4.6.0; do not create a separate v4.5.4
  release entry. Keep historical verification records and the old branch intact.
- The original `StageRoles` checkout remains on
  `archive/main-before-4.5.3` at its preserved v4.4.8 state. Keep the separate
  v4.4.7 PDF worktree and release branch unless later instructed otherwise.
- Before bulk branch cleanup, save and verify a complete Git bundle and the
  branch/worktree inventory. Remove only branches already contained in `main`
  and not in use by a worktree. Keep unmerged and explicit archive/release
  branches. Publish and verify `main` before deleting integrated remote branches.

- On 2026-10-18 the user removed Signalman and requested compact role numbers.
  Keep ordinary role/test/icon numbers contiguous; secret IDs remain 1001/1002.
  The role guide opens with Overview and offers Details with host-configured values.

- On 2026-10-18 the user requested substantially easier Courier play, prioritizing
  longer grace periods. Defaults: initial/revival 120 seconds; delivery sizes
  Tiny/Small/Medium/Big/Wide/Tall/VeryTall 90/120/180/240/240/240/300 seconds.
  Keep the existing attrition rate, fixed healing amounts and delivery conditions.

- On 2026-10-19 the user assigned the Courier difficulty easing and later work
  to v4.6.1. The v4.6.0 feature boundary is build540; its source tree is integrated
  into main at `7525180`. Those changes were originally developed in
  `version/4.6.1`; the 2026-10-30 version migration below supersedes that release name.

- After final extraction, Courier automatic HP loss stays disabled until stage end,
  including after role reassignment or revival. Other damage remains unchanged.

- On 2026-10-30 the user moved the v4.6.1 contents to v5.0.0 and requested
  R.E.P.O. v0.5.0 compatibility. Develop on `version/5.0.0`; the previous
  v4.6.1 branch and release archives remain recoverable. The v5.0.0 changelog
  includes Courier easing and final-extraction protection.

- On 2026-10-30 the user approved upgrade-based Lifter Strength. Grant a
  configurable Strength level (default 200) and use native heavy-object physics.
  Keep light/shop item handling at its configured level (default 1), except
  carts/bikes, and keep absolute Strength restoration after role changes.
  Retire the heavy grip/rotation multipliers with a configuration backup.
- Apply the upgrade-based Strength policy to every role. King uses direct
  temporary upgrade grants; Twins converts its configured carry multiplier into
  temporary Strength levels, capped at 200. Track their combined grant once so
  overlapping effects cannot compound, and preserve purchased levels on expiry.
  Influencer and Berserker already grant ordinary upgrades; inherited/copied Lifter
  abilities use the same Lifter settings and restoration.

- On 2026-10-31 the user requested native game-language following. Roles menus
  and roulette labels read the local R.E.P.O. language, never a separate saved
  MOD override. The roulette uses vanilla upgrade display-name translations and
  keeps canonical IDs on the network. Support all 21 installed v0.5.0 locales,
  including separate pt-BR/pt-PT and es-ES/es-419 catalogs. Unknown future locales
  fall back to English. Retire UI.GuideLanguage with a backup.
- On 2026-11-01 the user requested every native game language. Use bundled
  Arabic/Thai fonts and the game TMP text shaper for joined Arabic, bidi and Thai
  marks. Apply direction per MOD text and reset it on language changes. Preserve
  logical text order, Thai grapheme clusters, canonical role names and compact
  notification tokens; do not change global TMP settings.
- Repair the legacy Japanese font's missing Unity 6 material from its original
  atlas and the current game SDF shader before it can become a fallback font.
  Keep glyph metrics intact and avoid inserting MOD fonts into global TMP settings.

- On 2026-10-31 the user approved Musician support for Pan Flute, Harmonica and
  Mouth Harp. All old/new instruments share a per-performer healing cooldown
  (default one second) and a combined self/allies stage budget (default 1000 HP).
  Continuous instruments require audible play while attached at the mouth.
  Preserve the budget/cooldown across instrument swaps, role changes and revival;
  reset on stage end. Both settings are configurable; show remaining budget on
  the resource HUD and notify exhaustion with `MusicEmpty`.

- On 2026-11-01 the user requested current draw eligibility in Roles menus.
  Preview the viewer's own fresh whole-party draw using host conditions and the
  production candidate/history rules. Keep the preview read-only and RNG-free;
  distinguish conditional and unconfirmed roles, and conceal secret conditions.
  The user cancelled the preceding Bomber solo-exclusion request: Bomber remains
  eligible in singleplayer and one-player rooms.

## Builds and deployment

- Compile against the installed R.E.P.O. v0.5.0 managed assemblies. Set
  `REPO_GAME_DIR` or local `Directory.Repo.props`; do not commit game DLLs.
  Require MenuLib 2.6.0 and REPOConfig 1.2.7 from REPOModding.
- Increment the Role UI build number before each main build attempt. Keep the
  build number in the Role UI, and include it when reporting a build result.
- Build Release with automatic profile copying disabled:
  `dotnet build StageRoles.csproj -c Release --no-restore -p:CopyFilesToPluginOutputDirectoryOnBuild=false -p:CopyFilesToPluginOutputDirectoryOnRun=false`.
- Require zero warnings/errors and run `tools/Test-GameFieldAccess.ps1` after the
  main build. Run other checks appropriate to the change; distinguish automated
  checks from actual game and host-only multiplayer testing.
- Back up the previous package DLL, update `package/RoleShuffle.dll`, and verify
  SHA-256 equality with the build output. Deploy to **RSO_TEST**, following the
  current task's override of the earlier Default profile. Verify the deployed
  DLL against the same hash.
- Do not stop a running game or replace its loaded DLL. Prepare the files and
  state that deployment is pending until the game exits.

## In-game notifications

- Only initial role announcements use a shared sequence. Later notifications,
  replies and automatic speech must avoid overlap per player; one player's
  message must not block a different player. Respect that player's native TTS,
  including text chat and other mods, while retaining enemy hearing for abilities.
- Countdown speech takes priority: do not skip counts because that player is
  already speaking. Counts may interrupt speech; ordinary notices wait until
  the countdown's speaking window ends. Above 10 seconds, announce remaining
  multiples of 15 (for example 30, 15); from 10 through 0, announce every second.
  Follow the remaining-time deadline and interrupt the preceding utterance.
- Keep role notices short and without spaces (for example `CannotCopy`). The
  user reported that spaces interrupt notifications. Do not include role names
  or explanations when a concise status is sufficient.

## Artwork

- Follow [ROLE_ICON_GUIDE.md](ROLE_ICON_GUIDE.md) for the approved hexagonal
  Semibot emblems, anatomy, category colors and secret-role handling.
- Retain selected source PNGs and generation prompts. New illustration work
  uses image generation; deterministic preservation work may use Python,
  Pillow and NumPy as approved in S002 (the user's response to the exterior
  transparency method was “進めてください”). The current instruction inherits
  that method for category-background normalization.
- Change only the requested region; preserve the cream frame, character, props,
  dark contours and true exterior transparency. Keep full-resolution masters,
  256 × 256 runtime PNGs, source/output hashes and visual comparison sheets.
- Review every affected icon on light and dark backgrounds and at HUD size.
  Check the embedded DLL resources against the approved runtime PNGs.
- Produce full and public icon catalogs in parallel. The public version hides
  secret role names and artwork behind the common unrevealed icon.

## Documents and ZIPs

Follow [RELEASE_DOCUMENT_GUIDE.md](RELEASE_DOCUMENT_GUIDE.md), including:

- Explain current player-facing behavior, without technical details or changes
  made within the same unpublished version.
- README is English followed by Japanese, UTF-8 without BOM, at most 100,000
  decoded characters. CHANGELOG is English only, with no blank line between a
  version heading and its first bullet.
- Document only `/roles` as a public role command; conceal secret role details
  in public documents. Keep a separate detailed version when requested.
- `RoleShuffle.zip` contains the five required root files and font licenses,
  without PDFs, source, configuration, logs or an enclosing directory. Preserve
  the earlier ZIP and compare every archived entry with its validated source.
- Render and inspect all pages of generated PDFs. Report automated validation
  and actual game verification separately and accurately.
