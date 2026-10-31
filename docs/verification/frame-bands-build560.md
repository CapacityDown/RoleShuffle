# Original-style frames and Semibot bands — v5.0.0 build560

- Replaced the narrow pointed frames of Rigger and Negotiator with broad rounded
  hexagons based on the original Medic/Tank emblems.
- Rigger now uses the original simple cylinder/cone construction, with a visible
  shallow neck band. Negotiator has a wider lower waist band above its legs.
  Both faces remain mouthless; the orange cable and downward-arrow tag remain.
- Used built-in image_gen for illustration edits; preserved the selected sources
  and all edit prompts in selected/prompts.json. Applied the approved background
  mask normalization to Support #24563E while preserving alpha and other pixels.
- Masters are 1254x1254, runtime icons 256x256 RGBA, README thumbnails 64x64.
  Reviewed both on light/dark backgrounds at 256-class, 64 and 32 px alongside
  original Tank/Medic icons. Restored background continuity after rejecting the
  first candidates with transparent holes. Source/output hashes are in the manifest.
- Full/public catalogs refreshed. Public secret-name and secret-art checks passed.
- README pins the two revised images to artwork commit
  5e797dc79e6475a412ef1cf507c17b7287ae962b. All 47 remote image references match
  the corresponding local thumbnails. README is 99,946 characters, UTF-8, no BOM, LF.
- Build560: zero warnings/errors; mandatory game-field verification: 617 passed.
  Both revised embedded resources match the runtime PNGs; 49 emblem resources
  and UI build560 confirmed in the compiled DLL. No role behavior changed.
- RSO_TEST deployed while R.E.P.O. was stopped; seven files verified. Previous
  package/plugin/configuration/logs/ZIP preserved under
  tmp/deployments/before-build560-20261031-063547.
- ZIP contains exactly seven required entries, byte-matched with package; root
  and versioned ZIP hashes match. Prior build559 remains recoverable.
- DLL SHA-256: b17960718f1157d6739da25f3fa7b1e52087b815c238dabd8d7161ac45950ec9.
- ZIP SHA-256: 9d16a6ab59dd68a4502602a622ef8fe407d2a68177c318c37c676d12a020cfe5.
- Actual game rendering and host-only multiplayer testing were not performed.
