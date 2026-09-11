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
