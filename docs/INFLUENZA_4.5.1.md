# Influenza implementation notes

## Behavior

- The role has a 30-second incubation after assignment or infection. Infection
  replaces both the assigned and effective role. The victim loses old abilities.
- After onset, maximum HP is fixed to 75 without changing Health upgrade levels.
  Imposing or removing the cap does not heal. Death and revival do not reset
  incubation; reconnects retain the same state within the stage.
- A uniform interval of 30–90 seconds yields an average of 60 seconds per sneeze.
  The native `Achoo!` TTS cue is separate from ordinary speech infection trials.
- Sneeze: 60% per eligible living target, within 5 m and a horizontal ±20° fan.
- Speech: 30% per eligible living target, within 3 m and a horizontal ±30° fan.
  Distance is measured in 3D between heads. No additional line-of-sight rule.
- Microphone-only loudness excludes TTS; a 0.75-second silence separates utterances.
  Chat uses one trial per submitted message. Commands and host-generated
  announcements are excluded. Incoming chat must belong to the sender's avatar.
- Infection bypasses initial draw uniqueness and balance limits. Default draw
  weight is 20. Influenza is a Danger/Hardship role, excluded from solo draws,
  Superbot capabilities and Imitator copying. Standard, Chaos and Challenge include it.

## Host-only integration

The host owns infection state and uses the game's existing `UpdateHealthRPC`
and `ChatMessageSendRPC`; no custom RPC is required on vanilla clients. Native
`UpdateHealthRPC` accepts the master and does not clamp the maximum to 100.
Microphone samples are read from `clipLoudnessNoTTS`, including vanilla guests.
The health override is checked again after upgrades/revival and sent only when
the current maximum or health violates the cap. At cleanup, the normal maximum
is restored with any Health upgrade changes, before normal role/base resets.

Only the infected player's role effects are removed. Other players' ability
budgets and hazards continue. Existing emitted attacks may finish naturally.

Automated checks exercise production infection rules/runtime, including forward
fans, exact probability boundaries, speech bursts, authenticated chat, role
replacement, incubation, reconnects, upgrade changes and stage cleanup. Native
game field access is checked against the installed game DLL. A live multiplayer
test with an unmodded guest is still required; automated checks do not prove
audio timing or network behavior in an actual lobby.

## Icon source of truth

The user reconfirmed the icon instructions in RoleShuffle-S001
(`019fbdb5-6974-7f70-b783-b426be968fe3`) and RoleShuffle-S002
(`01a091a3-dc1e-7463-bad9-ef2e5a5fd3d9`). Keep the cream hexagonal frame and
interior illustration; make only the exterior transparent. Semibot has short
single-piece cone arms attached below a distinct jaw/neck band, eyes above the
mouth slit, and no human elbows, hands or unrelated decoration.

References: the existing Tank and Medic runtime icons. The final artwork keeps
the separate neck band, fever patch, sweat, blush and sneeze. The last edit
removes mosaic-like background artifacts and keeps the established composition.
The selected master and runtime PNG are named `41-Influenza.png` in the existing
asset directories. Built-in image generation was used; no external image API.

### Final cleanup prompt

Precise cleanup of this EXACT Influenza emblem. Keep every shape, character anatomy, collar band below mouth, cooling patch, sweat droplets, peach blush, sneeze puff, outlines, orange accents, hexagon border, composition and sizing identical. ONLY REMOVE ALL mosaic-like mottling, square/block artifacts, noise and texture. Make the entire green interior a perfectly UNIFORM SOLID flat color #365D33: no gradient, no cloudy shading, no grain, no patchwork blocks whatsoever. Make the cream body and border uniformly clean #FFF2D8 except existing small intentional hard-edged shadow shapes; remove subtle speckled shading. Keep peach fever blush as two smooth intentional small patches. Crisp smooth dark vector-like lines. Clean anti-aliased edges, genuinely transparent exterior of hexagon. No checkered transparency pattern painted into the image, no added text. This is a flat color game UI emblem, never a textured print.
