# Transparent role emblems

The active set contains 43 role icons and the shared Unrevealed icon. The cream
hexagonal frame, character, props and dark outlines follow the approved source
artwork. Only the exterior is transparent. Role interiors use the exact primary
category palette in `../selected/prompts.json` and `../../../ROLE_ICON_GUIDE.md`.
The neutral Unrevealed icon is excluded from category normalization.

## Rebuild

Run `python tools/build_transparent_role_emblems.py` from the RoleShuffle root
with Pillow and NumPy installed. The script calls
`tools/normalize_role_icon_backgrounds.py` for the background mask and palette.
It never overwrites selected source artwork or generation prompts.

- Opaque sources: remove only near-black exterior connected to image corners.
  When a prop crosses the cream frame, seal narrow channels in the detection
  mask to preserve its interior dark outline.
- Native-alpha sources (Courier and Influenza): retain source alpha directly;
  never color-key them. Their central alpha can be 254/255.
- Detect the dominant background color region, exclude the black/cream/orange
  artwork, and apply the category color with a narrow antialiased transition.
  Pixels outside the background mask remain unchanged in the full-size master.
- Export 256 × 256 RGBA runtime images using alpha-aware resampling. Correct
  background-core RGB after resizing so partial source alpha does not introduce
  one-channel rounding differences between icons of the same category.

## Outputs and review

- `masters/`: 44 full-size RGBA PNGs, generated locally and ignored by Git.
- `runtime/`: 44 runtime PNGs, embedded by `StageRoles.csproj` and shared by the
  HUD, Current Roles and Role Guide.
- `previews/`: background masks and source/light/dark comparison sheets. These
  are local review artifacts and are not embedded in the MOD.
- `manifest.json`: source, master and runtime SHA-256 hashes; transparency,
  background-core and preservation checks. Recolored role masters have
  `rgb_pixels_unchanged: false`; their outside-mask pixels are unchanged.

Review all comparison sheets after regeneration, including dark contours,
small props, the neck band and transparent edges. Verify the embedded resources
against the runtime PNGs. Rebuild the full/public icon catalogs with
`python tools/build_icon_catalog.py`; public artifacts retain secret-role masks.

The original inputs remain in `../selected/` and
`../unrevealed/Unrevealed.png`. See `../../../WORKFLOW.md` for the inherited
source-preservation, review and delivery procedure.
