# Player notifications and countdowns — v4.5.4 build514

## Behavior

- Initial role announcements retain the shared sequential queue and Stage Flux
  quiet period. Later notices and replies wait only for their own player's speech.
  One player's queue does not delay a different player.
- Native chat/TTS activity, including speech started by other mods, is observed
  per avatar. Influencer and Influenza use that player's availability and retain
  their enemy investigation effects.
- Diver announces remaining multiples of 15 above 10 seconds, then every integer
  from 10 through 0. A 60-second dive announces `60,45,30,15,10,9,...,1,0`;
  the default 10-second dive announces `10,9,...,1,0`.
- Counts bypass the normal queue and replace ongoing speech. A 1.25-second
  priority window keeps ordinary notices out of the gaps between final counts.
  Different players remain independent. Notification settings are respected.
- Count deadlines follow the dive's remaining game time, with one frame of
  scheduling precision. A stalled frame sends only the current eligible count,
  without rapidly replaying expired numbers or shifting the dive's deadline.
- Returning above the floor cancels further counts. Disabled notifications or
  a failed zero-count transport do not block existing timeout handling.

## Verification

- `tools/Test-NotificationQueues.py` generates a harness from production
  `RoleNotifier`, `PlayerMessageActivity`, `StageRole`, Influencer `LateTick`,
  and Diver `TickCountdown`: **51 checks passed**.
  Coverage includes per-player ordering/concurrency, delayed TTS, native chat,
  initial shared announcements, Stage Flux, cancellation, queue capacity,
  disconnected avatars, transport failure and per-player ability speech.
- The extracted production countdown was run over a 60-second virtual dive.
  Counts occurred at the expected deadlines within one simulated frame, and
  every final count interrupted the preceding 2.5-second utterance. The default
  10-second dive, short utterances, queued notices, frame stalls, dive cancellation
  and zero-count failure were also checked.
- `tools/InfluenzaChecks`: **152 checks passed**, including independent sneeze
  waiting and preserved enemy investigation/infection behavior.
- Inspected the installed, unmodified `Assembly-CSharp.dll` using Mono.Cecil:
  `PlayerAvatar.ChatMessageSend` reaches native `ChatMessageSpeak` locally or
  through the authorized `ChatMessageSendRPC` path. `TTSSpeakNow` calls
  `StartSpeakingWithHighlight`, whose first call is `StopAndClearVoice`.
  That method stops both voice instances and clears their cache/schedule before
  new speech is scheduled. The countdown therefore uses vanilla interruption.
- Release build: **0 warnings, 0 errors**, UI build **514**.
- `tools/Test-GameFieldAccess.ps1`: **527 compiled game field references passed**
  against the installed unmodified game assembly.
- Release Markdown: README **99,695 / 100,000 characters**, CHANGELOG **18,163**;
  UTF-8 without BOM, no blank line after a changelog version heading.
- No fresh in-game or host-only multiplayer test was performed. The virtual
  timing checks do not measure network latency or actual client audio timing.

## Deployment

- Build, package and RSO_TEST DLL SHA-256:
  `982e41aacfdbb15de6f44c379908bb4373881f41f524dca97e1ae0f466f493a0`.
- All seven profile package files verified; deployment performed with R.E.P.O.
  closed. Configuration unchanged; published release ZIPs were not rebuilt.
- Previous build/package: `tmp/versions/before-build514-20261009-111012`.
- Previous profile/package: `tmp/deployments/before-build514-20261009-111115`.
