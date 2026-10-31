# Mouthless Rigger and Negotiator — v5.0.0 build559

- Removed the facial mouth slit from both selected icons using built-in image_gen.
  Preserved the structural head/neck band contour, eyes, props and composition.
- Full-resolution sources, prompts and normalized masters retained. Runtime icons
  are 256x256 RGBA; README thumbnails are 64x64. Support background is #24563E.
- Visually reviewed both icons at 256/64/32 px on light and dark backgrounds.
  Background normalization preserves source alpha and pixels outside its mask.
- Updated the production guide to keep both roles mouthless. No role behavior changed.
- Refreshed full/public catalogs. Public secret-name and secret-art checks passed.
- README pins both images to artwork commit c90ddb670afc53aac50eb20d85c72b6d4d5e90c3.
- Release build559: zero warnings/errors. Mandatory game field checks: 617 passed.
  Both DLL image resources match the approved runtime files by SHA-256.
- RSO_TEST deployed while the game was stopped; all seven files verified.
  Previous plugin, configuration, logs and package archived under
  tmp/deployments/before-build559-20261031-054127.
- ZIP has exactly seven required files, byte-matched with package; all 47 GitHub
  icon references verified. README is 99,946 characters, UTF-8 without BOM and LF.
- DLL SHA-256: a650f029804b3676e3b44c92f2a1fef8be184b642c1138e41781124d289fbe4a.
- ZIP SHA-256: 9d53cee3df06d4774cd1717aff386927e6ecb3a798ea9cbf0446d4fb46416fda.
- Actual game rendering was not exercised in this turn.
