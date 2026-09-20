# RoleShuffle Icon Production Guide

## Current approved style

Follow the approved role emblems in `Assets/role-emblems-semibot-v1/transparent/runtime/`.
The user's instructions in RoleShuffle-S001 (`019fbdb5-6974-7f70-b783-b426be968fe3`)
and RoleShuffle-S002 (`01a091a3-dc1e-7463-bad9-ef2e5a5fd3d9`), reconfirmed on
2026-09-21, take precedence over the earlier frameless concept.

- Preserve the hexagonal frame, cream edging and interior illustration.
- Make only the exterior of the hexagon transparent.
- Match the flat colors, simple shading and dark outlines of the existing set.
- Use a clear role-specific action or prop; avoid unrelated decoration.
- Fill the badge well enough to remain recognizable in the HUD.
- Keep actual Semibot anatomy: dome head, protruding eyes above the mouth slit,
  a distinct shallow cylindrical jaw/neck band, cylindrical torso and cone legs.
- Arms attach high on the torso, just below the jaw band. Keep them short and
  tapered. Do not add human elbows, wrists, fingers or extra joints.
- Reference approved original icons such as Tank and Medic for anatomy. Do not
  derive the whole set's anatomy from a later role-specific costume variant.
- Keep role names and other text out of the artwork.

## Category backgrounds

Use the role's primary icon category for the interior background. Assignment
balance groups may overlap; they do not require multiple background colors.

| Category | Interior color |
| --- | --- |
| Enhancement | `#174665` |
| Support | `#24563E` |
| Combat | `#80451F` |
| Special | `#50356E` |
| Danger | `#762C37` |
| Hardship | `#514E49` |
| Secret | `#756025` |

Influenza uses Danger because it can spread to teammates. It also remains in
the Hardship assignment group because of its health penalty. Courier uses
Hardship because delivery pauses its automatic health loss. Preserve the
existing character and props when correcting a category background.

## Delivery and review

Keep a full-resolution master and its generation prompt. Export a 256 × 256 RGBA
runtime PNG named with the existing role ID convention. Preserve true alpha.
Inspect the neck band, mouth location, arm attachment and silhouette at full size
and at HUD size. Existing icons remain unchanged when adding a new role.

Secret-role icons remain concealed in public documents and unrevealed UI.
