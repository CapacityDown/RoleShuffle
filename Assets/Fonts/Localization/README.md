# Localization fonts

Subset fonts derived from Noto Sans and Noto Sans CJK under the SIL Open Font
License 1.1. Copyright notices and licenses are in `OFL.txt` and `OFL-NotoSans.txt`,
also embedded in the DLL and included under the package's `LICENSES` directory.
The subset families are renamed to `RoleShuffle Names`, `RoleShuffle European`,
`RoleShuffle Korean`, `RoleShuffle ChineseSimplified`, and `RoleShuffle ChineseTraditional`.
Japanese role text continues to use the existing Checkpoint Revenge font.
At runtime, all fallback font faces use the same 0.72 scale correction as Japanese
to match the game's Latin glyph size. This also covers native language names in
REPOConfig; the primary game font keeps its original scale.

Sources:
- https://github.com/notofonts/noto-cjk (revision in `source-commit.txt`)
- https://github.com/notofonts/noto-fonts/tree/ffebf8c1ee449e544955a7e813c54f9b73848eac/hinted/ttf/NotoSans

To regenerate, install fontTools in a development environment, obtain the regular
SC/TC/KR CJK OTFs and `NotoSans-Regular.ttf` from those revisions, and place them with
the CJK `OFL.txt` and `noto-commit.txt` in a source directory. Then run:

```powershell
python tools/build_localization_fonts.py --source-root <source-directory>
```

The DLL extracts fonts to `BepInEx/cache/RoleShuffle/Fonts` using content-hashed
filenames. TMP generates glyph atlases dynamically. The Names and European fonts
also provide fallback glyphs for REPOConfig's language choices. No OS font install
or font download is required by players.
