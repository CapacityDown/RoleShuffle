# RoleShuffle Icon Production Guide

## 1. Purpose

This document defines the visual production policy for RoleShuffle role icons. It is based on the lessons learned while producing the Stage Flux event icons, but RoleShuffle must not use medals, medallions, circular rings, or circular frames.

The goals are:

- Make every role recognizable without reading its label.
- Match the dark, industrial, slightly playful visual language of R.E.P.O.
- Remain readable in the HUD, Roles page, README, and PDF catalog.
- Keep the icon artwork independent from UI frames, rarity colors, selection states, and text.
- Maintain one consistent visual system across all roles.

## 2. Stage Flux icon production policy

### 2.1 Source of truth

The current Stage Flux runtime assets in `../StagePhysicsEvents/Assets/EventIcons/Runtime/` are the primary reference.

The older `256/`, `masters/`, and contact-sheet assets may contain circular gunmetal medallions, colored rings, indicator nodes, or presentation frames. These are historical source and catalog assets, not the preferred structure for new runtime icons.

### 2.2 Separation between artwork and UI

Stage Flux runtime icons follow this division of responsibility:

- The PNG contains only the event motif.
- The HUD supplies the slot background and danger-colored border.
- The HUD supplies labels, lock states, timers, highlights, and animation.
- The same icon can therefore be reused in vertical and horizontal layouts without carrying an incompatible frame.

Do not embed any of the following in a runtime icon:

- Circular rings or medallions
- Square or rectangular borders
- Risk-colored frames
- Lock marks or selection highlights
- Event names, initials, numbers, or other text
- A background panel intended to fill the entire square

### 2.3 Stage Flux visual characteristics

- Canvas: square RGBA PNG with transparent corners and background.
- Runtime size: `256 x 256` pixels.
- Recommended master size: at least `1024 x 1024` pixels.
- Composition: one dominant central motif with a clear silhouette.
- Safe margin: approximately 10% on all sides.
- Surface: matte painted metal, rubber, cloth, glass, electricity, smoke, or other tactile game-like material.
- Outline: thick dark outline or strong dark edge separation.
- Lighting: restrained cyan, amber, green, magenta, or red accent light on a dark-valued subject.
- Contrast: readable at 64 pixels and still identifiable at approximately 32 pixels.
- Text: never embedded in the image.

### 2.4 Motif rules

- Express one event with one primary object or action.
- Use a secondary effect only when needed to explain direction or behavior, such as arrows, impact lines, electricity, cracks, or motion trails.
- Avoid multiple equally important subjects; small icons become visual noise quickly.
- Do not rely only on color. The silhouette must distinguish the event in grayscale.
- Directional artwork must be stored upright. Do not compensate for a runtime vertical flip inside the asset.
- Hinges, doors, tools, limbs, and other mechanical structures must have physically plausible axes and joints.
- When using an enemy, reference the actual R.E.P.O. enemy anatomy and silhouette. Use a variety of enemies rather than repeating one enemy for every icon.
- When using a Semibot, keep its eyes and mouth in their correct visual positions. Never place the eyes inside the mouth opening.
- Remove generation artifacts such as mosaic patches, false eyes, duplicated limbs, accidental frames, and unreadable micro-details.

### 2.5 Color policy

Color communicates the effect, but does not replace the UI danger frame.

- Cyan or blue: movement, gravity, energy, warp, or cold.
- Green: healing, regeneration, protection, or positive recovery.
- Amber or orange: impact, value increase, unstable motion, or warning.
- Red: damage, destruction, danger, or value loss.
- Purple or magenta: void, hypnosis, supernatural control, or unusual effects.
- Neutral metal and off-white: waiting, physical objects, or non-elemental effects.

These are tendencies, not mandatory category colors. Preserve natural object colors when they improve recognition.

## 3. Recommended RoleShuffle format

### 3.1 Primary recommendation: frameless character vignette

RoleShuffle should use a **frameless character vignette** instead of a medal or circular badge.

Each icon consists of:

1. One Semibot shown from the chest or waist up, or a compact full-body pose.
2. One role-defining prop, effect, pose, or action.
3. One asymmetrical accent shape behind the character, such as a diagonal light wedge, paint slash, hazard stripe fragment, or soft rectangular glow.
4. Transparent space around the composition.

The accent shape is not a frame. It must not enclose the character on all sides and must not form a complete circle, shield, medal, or card border.

Examples:

- Medic: Semibot extending a hand toward a green medical pulse.
- Rescuer: Semibot lifting or reaching toward a Death Head with an upward recovery light.
- Phoenix: Semibot rising through angular orange flame wings; no circular halo.
- Runner: leaning Semibot with long horizontal speed streaks.
- Lifter: Semibot bracing beneath a heavy object.
- Gambler: Semibot presenting a valuable beside split win/loss lighting.
- Ninja: low-contrast Semibot silhouette cut by one sharp cyan edge light.
- Door- or tool-related roles: keep the mechanical action physically plausible and immediately readable.

### 3.2 Why this format is recommended

- A Semibot makes the set immediately recognizable as RoleShuffle rather than Stage Flux.
- A pose communicates a role more clearly than a generic emblem.
- The asymmetrical backdrop creates consistency without resembling a medal.
- Transparent edges allow the same asset to work in the HUD, guide page, README, and PDF.
- Role color can be changed by the UI without redrawing the icon.

### 3.3 Secondary format: frameless object-and-silhouette glyph

Use this only when a character pose becomes too busy at small sizes.

- Combine one role-defining object with a small Semibot silhouette.
- Keep the object dominant and the Semibot secondary.
- Use an angled shadow, light beam, or incomplete hazard-stripe patch as the shared background language.
- Do not place the glyph inside a circle, crest, shield, coin, or fully enclosed tile.

Suitable examples include Tracker, King, Engineer, Electrician, Warden, and Hunter.

### 3.4 Optional UI backplate

If the HUD requires stronger separation from the game scene, create the backplate in UI code or as a shared UI asset, not inside every role icon.

Recommended backplate:

- Rounded or chamfered rectangle
- Wider than it is tall when paired with a player name
- Dark charcoal with subtle industrial texture
- One short role-colored edge bar or corner notch
- No circular center and no medal-like rim
- Consistent across every role

This preserves a unified list while allowing the icon PNGs to remain reusable.

## 4. RoleShuffle composition specification

### 4.1 File format

- Master: `1024 x 1024` or larger, lossless PNG.
- Runtime: `256 x 256`, RGBA PNG.
- Color space: sRGB.
- Background: transparent.
- Naming: exactly match the `StageRole` identifier, for example `Phoenix.png` and `EnemySpeedUp.png`-style PascalCase.
- One file per role; do not create language-specific icon variants.

### 4.2 Layout

- Keep the focal point near the visual center, not necessarily the mathematical center.
- Use approximately 10% safe margin.
- Fill roughly 70% to 82% of the canvas with the meaningful silhouette.
- Keep the face, prop, and primary effect inside the central 80% so HUD cropping cannot remove them.
- Avoid thin lines near the canvas edge.
- Do not add labels to compensate for an unclear composition; simplify the composition instead.

### 4.3 Semibot depiction

- Preserve the recognizable cylindrical body, face arrangement, limbs, and game-world proportions.
- Eyes sit in the upper face area; the mouth opening remains below them.
- Expressions may be exaggerated, but facial anatomy must remain structurally correct.
- Prefer readable poses and props over anime-style facial rendering.
- Smooth obvious generation artifacts, while retaining the slightly rough, toy-like industrial character of R.E.P.O.
- Do not make every role the same standing pose with a recolor.

### 4.4 Category accents

Role category may be communicated with a small accent rather than a frame:

- Support: green or cyan corner light.
- Showcase or special: amber, purple, or high-contrast spotlight.
- Danger: red-orange hazard slash.
- Hardship: desaturated amber, gray, or damaged texture.
- Standard upgrade role: cyan, blue, or neutral industrial light.

The role-specific motif must remain understandable without these colors. Do not use color as the only category indicator.

## 5. Consistency rules

All RoleShuffle icons must share:

- The same camera distance family.
- The same outline thickness at runtime size.
- Similar material roughness and lighting contrast.
- The same transparent-edge treatment.
- Comparable subject scale.
- A consistent shadow direction.
- A consistent maximum number of accent colors.

Variation should come from pose, prop, enemy, effect, and silhouette—not from changing the entire rendering style.

## 6. Generation and cleanup workflow

1. Define the role in one short visual sentence: subject, action, prop, and accent color.
2. Generate or illustrate at `1024 x 1024` or larger.
3. Remove all full-canvas backgrounds, frames, circles, badges, text, and unrelated props.
4. Correct Semibot and enemy anatomy against R.E.P.O. visual references.
5. Correct mechanical axes, object orientation, and motion direction.
6. Normalize subject scale and safe margins against the rest of the set.
7. Export a transparent master PNG.
8. Downscale to `256 x 256` with a high-quality filter.
9. Inspect at 256, 64, and 32 pixels.
10. Test over both a dark background and a bright/noisy gameplay screenshot.

Do not enlarge a low-resolution icon and treat it as a new master. Regenerate or reconstruct the source when detail is insufficient.

## 7. Image-generation prompt template

Use the following as a starting point, adapting the role-specific section:

```text
Create a square transparent-background game UI icon for the R.E.P.O. RoleShuffle role "[ROLE]". Show one recognizable R.E.P.O.-style Semibot [POSE/ACTION] with [PRIMARY PROP OR EFFECT]. Use a compact, readable silhouette, matte industrial materials, thick dark edge separation, restrained [ACCENT COLOR] lighting, and slightly rough toy-like game rendering. Add only an incomplete asymmetrical accent behind the character, such as [DIAGONAL LIGHT WEDGE / PAINT SLASH / HAZARD STRIPE FRAGMENT]. No text, no letters, no numbers, no medal, no medallion, no circle, no circular ring, no shield, no badge, no complete border, no full square background. Keep approximately 10% transparent safe margin and ensure the icon remains recognizable at 32 pixels. Preserve correct Semibot anatomy: eyes above the mouth opening.
```

For enemy-related roles, add:

```text
Use the visual anatomy and silhouette of the appropriate R.E.P.O. enemy as reference. Do not invent extra eyes or limbs, and do not obscure the face with mosaic-like artifacts.
```

## 8. Review checklist

Before accepting an icon, verify:

- [ ] The role is identifiable without its text label.
- [ ] No medal, circle, circular ring, shield, badge, or complete frame remains.
- [ ] The background and corners are truly transparent.
- [ ] The subject does not touch the canvas edge.
- [ ] The silhouette remains clear at 32 pixels.
- [ ] Semibot eyes are above the mouth and no face parts are misplaced.
- [ ] Enemy anatomy matches the referenced R.E.P.O. enemy.
- [ ] Mechanical axes and movement direction are plausible.
- [ ] No duplicated limbs, false eyes, mosaic patches, accidental text, or generation artifacts remain.
- [ ] Subject scale, outline, lighting, and shadow direction match the rest of the set.
- [ ] The icon works on both dark and bright gameplay backgrounds.
- [ ] The runtime file is `256 x 256` RGBA PNG and the master remains available separately.

## 9. Recommended folder structure

```text
StageRoles/Assets/RoleIcons/
  masters/
    Tank.png
    Runner.png
    ...
  Runtime/
    Tank.png
    Runner.png
    ...
  contact-sheet.png
  README.md
```

The contact sheet may add labels and a neutral catalog background for review. Those presentation elements must not be copied into the runtime PNGs.
