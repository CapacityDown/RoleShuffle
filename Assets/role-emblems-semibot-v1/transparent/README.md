# Transparent role emblems

The current emblem design retains the cream hexagonal frame, colored interior,
characters and props. Only the near-black exterior connected to the image
corners is removed. Dark outlines and shadows enclosed by the frame remain
opaque. When a prop crosses the frame and connects its dark outline to the
exterior, narrow channels are sealed in the detection mask before extracting
the background; the artwork's RGB values are not altered.

- `masters/`: 43 full-size RGBA PNGs. RGB pixels are identical to the originals;
  only exterior alpha is changed.
- `runtime/`: 43 RGBA PNGs at 256 x 256, embedded by `StageRoles.csproj` and used
  by the HUD, Current Roles and Role Guide. Alpha-aware downsampling smooths
  the silhouette edges.
- `previews/`: source / light background / dark background comparisons for
  inspection. These are not embedded in the mod.
- `manifest.json`: source and output SHA-256 hashes and transparency checks.

The inputs remain in `../selected/` and `../unrevealed/Unrevealed.png`.
Regenerate with `StageRoles/tools/build_transparent_role_emblems.py` using
Python, Pillow and NumPy. The script checks the transparent perimeter, opaque
center, preserved interior and source hashes, and partial alpha at runtime
edges. Review every comparison sheet after regenerating.

## Courier (v4.5.0)

Role 13 now uses the user-selected Courier concept A. Its selected PNG already
contains native transparency, so it bypasses the near-black removal process.
The generated 1254 px master is retained without alteration; System.Drawing
high-quality bicubic resizing produces the 256 px runtime asset. The central
alpha is 254/255. Do not run background extraction on this native-alpha source.
The active set still contains 42 roles plus Unrevealed. Jobless artwork is
preserved in Git history and the original checkout. See
`../../../docs/COURIER_DESIGN.md` for the adopted design and generation prompt.
