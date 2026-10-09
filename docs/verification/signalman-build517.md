# Signalman implementation — v4.5.4 build517

## Behavior

- Signalman (public role ID 42) relays the original ordinary chat payload to living
  teammates through each recipient's own vanilla chat/TTS. Distance does not
  limit delivery. The original speaker keeps their ordinary chat; normal VC is
  unchanged. No custom client RPC or guest installation is required by this path.
- Default cooldown is 20 seconds, starting when a transmission enters at least
  one recipient queue. Cooldown posts remain ordinary chat and are never replayed.
- Slash-prefixed inputs, exact built-in spell commands and automatic notices are
  excluded. Additional complete inputs and prefixes are configurable; `!` is the
  default additional prefix. Text containing a spell word remains ordinary chat.
- Authority, owner identity, active assignment and living-state checks prevent
  spoofed or recursive transmission. Role changes, departure, death and stage end
  invalidate queued transmissions. A newer ordinary utterance is never stopped by
  an older queued transmission.
- Per-recipient queues respect their native speech and countdown priority. Queued
  transmissions expire after 5 seconds by default (configurable 1–30 seconds).
  The configurable radio wait also works above the ordinary 15-second queue limit.
- A short private `Down:PlayerName` notice informs Signalman once per teammate
  death. Revival rearms it. Both Signalman.DeathAlerts and Notifications.Enabled
  must be enabled. Radio chat itself does not depend on generated notice settings.
- Support draw group; at least two players; at most one random Signalman even
  with duplicate roles enabled or fallback selection. Imitator can copy it and
  Superbot includes the capability. Standard/Beginner/Cooperative contain 44/20/17
  roles. Existing role identifiers and secret IDs remain unchanged.
- Modded players receive the cyan radio cooldown HUD. REPOConfig exposes role
  enable/weight, cooldown, maximum delay, death alerts and extra command exclusions.

## Automated validation

| Check | Result |
| --- | --- |
| Main Release build517 | 0 warnings, 0 errors |
| Native game field access | 538 references passed |
| Notification and Signalman production-source harness | 97 checks passed |
| Overhaul/HUD/codec/synchronization checks | 75,943 passed |
| Localization checks | 13,215 passed |
| Existing Influenza runtime checks | 152 passed |
| Ability configuration | 126 passed |
| Role settings / Base Upgrade settings / saved adjustments | 339 / 128 / 248 passed |
| Catalog/planner/secret-role checks | 78 passed |
| Signalman artwork, placeholders and bundled glyph coverage | 34 passed |
| Embedded role PNGs | all 45 match runtime source hashes |
| README | 99,941 characters; UTF-8 without BOM, LF |
| CHANGELOG | 18,402 characters; no blank line after version heading |

The installed game's ChatMessageSendRPC accepts the owner or master client and
uses native ChatMessageSpeak during gameplay. PlayerDeathRPC sets deadSet before
the notification postfix. These paths were inspected in the installed game DLL.
The initial build515 passed compilation but the field-access check rejected four
accesses to private game fields; build516 replaced them with existing public
identity/PUN access. Build517 also verifies the 30-second configured queue case.

The legacy phrase collector does not cover the maintained RoleOverhaulDescriptions
catalog. Existing translations were retained, with only Signalman's two phrases
added. The C# localization suite validates the complete maintained catalogs;
Test-SignalmanAssets.py additionally checks the new bundled glyph coverage.

## Artwork

- Mode: built-in image_gen, using existing Tank/Medic emblems as references.
- Motif: cream Semibot with headset, microphone, backpack antenna and radio waves;
  no text, hands, wrists or elbow joints. Support interior is exactly `#24563E`.
- Selected original: `Assets/role-emblems-semibot-v1/selected/42-Signalman.png`.
- Full prompt history: `Assets/role-emblems-semibot-v1/selected/prompts.json`,
  Signalman entry. The final edit makes both arms straight downward cones.
- Full-resolution master: `Assets/role-emblems-semibot-v1/transparent/masters/42-Signalman.png`.
- Runtime: `Assets/role-emblems-semibot-v1/transparent/runtime/42-Signalman.png`.
- GitHub thumbnail: `docs/icons/42.png`, pinned in README to asset commit
  `2207c9a37f217fcea8399ef3efdc0945ce62a04e`.
- Native alpha is preserved, including the nearly opaque central alpha 253.
  Only the background color mask is normalized. Source/master/runtime hashes
  are recorded in `Assets/role-emblems-semibot-v1/transparent/manifest.json`.
- Reviewed at 256 px and 64 px against light and dark backgrounds. Existing
  selected images, masters, runtime icons and thumbnails were left unchanged.
- Full/public HTML and PNG catalogs regenerated in `output/icon-catalog`.
  The public catalog continues to conceal secret names and artwork.

## Deployment

- RSO_TEST updated on 2026-10-09 at 11:58 UTC while R.E.P.O. was closed.
- Build, package and deployed DLL SHA-256:
  `1e14e6a4f7a20556cd26a66db2aacb19b572d728a157b4af43ec09482c651962`.
- Seven deployed files match the package. Existing configuration was not changed.
- Backup: `tmp/deployments/before-build517-20261009-115831`.
- Pre-build backup: `tmp/versions/before-build515-20261009-114604`.
- Existing release ZIPs were preserved; this request did not create a new ZIP.

## Remaining live verification

No live gameplay, multiplayer delivery or UI rendering was exercised. Check a
host Signalman and a vanilla guest Signalman, a receiver beyond normal hearing
range, text with spaces, command exclusions and the 20-second cooldown boundary.

Vanilla targeted playback has no completion acknowledgement. The host reserves
a conservative text-length-based window for remote listeners; exact TTS duration
and unrelated private speech from other mods cannot be observed. Native chat and
countdowns can interrupt a received message. Suppressing its original proximity
copy can have a short network-delay overlap. These are live-test limitations,
not claims of complete remote speech synchronization.
