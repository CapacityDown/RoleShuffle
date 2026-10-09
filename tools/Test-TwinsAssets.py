"""Check new artwork, every Twins translation placeholder and required glyphs."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image
from fontTools.ttLib import TTFont

root = Path(__file__).resolve().parents[1]
catalogs = root / 'Assets/Localization'
english = json.loads((catalogs / 'English.json').read_text(encoding='utf-8'))
keys = [k for k in english if k.startswith(('Two teammates share', 'At least two players; at most one pair.')) or k in ('Rest together', 'Joint delivery')]
assert len(keys) == 4
fonts = {p.stem: set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').glob('*') if p.suffix in ('.otf', '.ttf')}
checks = 0
for path in catalogs.glob('*.json'):
    if path.stem == 'Japanese': continue
    values = json.loads(path.read_text(encoding='utf-8'))
    assert set(values) == set(english), path.name
    for key in keys:
        text = values[key]
        assert sorted(re.findall(r'\{\d+\}', key)) == sorted(re.findall(r'\{\d+\}', text)), path.name
        assert path.stem == 'English' or text != key, path.name
        missing = set(map(ord, text)) - fonts['Names'] - fonts['European'] - fonts.get(path.stem, set()) - {10, 13, 9}
        assert not missing, (path.name, ''.join(map(chr, sorted(missing))))
        checks += 1
assets = root/'Assets/role-emblems-semibot-v1'
manifest = json.loads((assets/'transparent/manifest.json').read_text(encoding='utf-8'))
record = next(x for x in manifest['assets'] if x['name'] == '43-Twins')
for kind, directory in [('source', 'selected'), ('master', 'transparent/masters'), ('runtime', 'transparent/runtime')]:
    path = assets/directory/'43-Twins.png'
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record[kind+'_sha256'], path
    checks += 1
with Image.open(assets/'transparent/runtime/43-Twins.png') as icon:
    assert icon.mode == 'RGBA' and icon.size == (256, 256) and icon.getpixel((0, 0))[3] == 0
    assert record['background_color'] == '#50356E' and record['category'] == 'Special'
    assert record['outside_mask_pixels_unchanged'] and record['alpha_preserved_during_recolor']
    checks += 3
with Image.open(root/'docs/icons/43.png') as thumb:
    assert thumb.mode == 'RGBA' and thumb.size == (64, 64)
    checks += 1
print(f'Twins artwork and localization checks passed: {checks}')
