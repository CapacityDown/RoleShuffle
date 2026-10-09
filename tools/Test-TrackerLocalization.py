"""Check Tracker's guide templates, locale placeholders and bundled glyphs."""
import json
import re
from pathlib import Path
from fontTools.ttLib import TTFont
from collect_localization_strings import catalog

root = Path(__file__).resolve().parents[1]
generated = catalog()
keys = [next(k for k in generated if k.startswith(prefix)) for prefix in (
    'Automatically senses nearby enemies', 'Automatically reports the nearest enemy within')]
english = json.loads((root/'Assets/Localization/English.json').read_text(encoding='utf-8'))
fonts = {p.stem: set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').iterdir()
         if p.suffix in ('.otf', '.ttf')}
checks = 0
for path in (root/'Assets/Localization').glob('*.json'):
    if path.stem == 'Japanese':
        continue  # Japanese role descriptions are directly authored in RoleGuideCatalog.cs.
    values = json.loads(path.read_text(encoding='utf-8'))
    assert set(values) == set(english), path.name
    checks += 1
    glyphs = fonts['Names'] | fonts['European'] | fonts.get(path.stem, set())
    for key in keys:
        assert key in values and values[key], (path.name, key)
        assert sorted(re.findall(r'\{\d+\}', key)) == sorted(re.findall(r'\{\d+\}', values[key])), path.name
        assert not (set(map(ord, values[key])) - glyphs - {9, 10, 13}), path.name
        assert path.stem == 'English' or values[key] != key, path.name
        checks += 4
print(f'PASS: {checks} Tracker guide template, translation and glyph checks.')
