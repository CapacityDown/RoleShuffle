# Influenza implementation notes

## Behavior

- The role has a 30-second incubation after assignment or infection. Infection
  replaces both the assigned and effective role. The victim loses old abilities.
- After onset, maximum HP is fixed to 75 without changing Health upgrade levels.
  Imposing or removing the cap does not heal. Death and revival do not reset
  incubation; reconnects retain the same state within the stage.
- A uniform interval of 30–90 seconds yields an average of 60 seconds per sneeze.
  The native `Achoo!` TTS cue is separate from ordinary speech infection trials.
  Each sneeze also calls native `EnemyDirector.SetInvestigate` on the host at the
  carrier's head position, with the vanilla voice radius of 5 m and `pathfindOnly: false`.
  Hearing is omnidirectional, independent of the infection fan; native enemy hearing
  multipliers and investigation behavior still apply. The alert does not depend on
  successful TTS playback or notification audio suppression. Disaster inherits it.
- Sneeze: 60% per eligible living target, within 5 m and a horizontal ±20° fan.
- Speech: 30% per eligible living target, within 3 m and a horizontal ±30° fan.
  Distance is measured in 3D between heads. No additional line-of-sight rule.
- Microphone-only loudness excludes TTS; a 0.75-second silence separates utterances.
  Chat uses one trial per submitted message. Commands and host-generated
  announcements are excluded. Incoming chat must belong to the sender's avatar.
- Infection bypasses initial draw uniqueness and balance limits. Default draw
  weight is 20. Influenza is a Danger/Hardship role, excluded from solo draws,
  Superbot capabilities and Imitator copying. Standard, Chaos and Challenge include it.

## Combined role

Disaster (1002) combines Bomber, Stinker, Tuna and Influenza. Its Influenza
ability follows the same incubation, maximum HP and transmission rules.
The carrier keeps Disaster and its other abilities. Victims receive ordinary
Influenza. Players who already carry the ability cannot be reinfected, so
Disaster is not replaced and its incubation timer is not restarted. Leaving
Disaster or ending the stage restores the normal maximum HP without healing.
Public descriptions remain concealed until the hidden role is revealed.

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
the separate neck band, fever patch, sweat, blush and sneeze. Its primary icon
category is Danger, with the established burgundy interior color `#762C37`.
The additional Hardship assignment limit reflects its maximum-health penalty.
Use smooth fills without mosaic-like artifacts and keep the composition.
The selected master and runtime PNG are named `41-Influenza.png` in the existing
asset directories. Built-in image generation was used; no external image API.

### Final category-color prompt

Use case: precise-object-edit. Edit target: the attached approved Influenza role emblem. Change ONLY the colored interior backdrop inside the hexagonal frame from green to a perfectly uniform solid muted burgundy red #762C37, the established Danger category color for RoleShuffle. Preserve the EXACT existing character, pose, proportions, head, separate shallow jaw/neck band below the mouth, short tapered cone arms, eyes, forehead cooling patch, blue sweat, peach blush, cream sneeze puff, orange rays, black outlines, cream hexagonal border, layout and margins. No redesign, no new details, no repositioning, no silhouette changes. Outside the hexagonal badge stays genuinely transparent alpha. Inside the frame must be fully opaque. Background must have NO mosaic, blocks, grain, texture, gradient, speckles or cloudy patches. Crisp smooth edges and flat game-icon rendering. Return only one square finished icon, no text and no comparison board.
