"""Validate the new radio artwork and translations without dropping legacy phrases."""
import hashlib
import json
import re
from pathlib import Path
from PIL import Image
from fontTools.ttLib import TTFont

ROOT = Path(__file__).resolve().parents[1]
catalogs = ROOT / 'Assets/Localization'
english = json.loads((catalogs / 'English.json').read_text(encoding='utf-8'))
keys = [k for k in english if k.startswith(('Relays ordinary chat', 'Relays your ordinary chat'))]
assert len(keys) == 2
fonts = {p.stem: set(TTFont(p).getBestCmap()) for p in (ROOT/'Assets/Fonts/Localization').glob('*')
         if p.suffix in ('.otf', '.ttf')}
common = fonts['Names'] | fonts['European']
checks = 0
for path in catalogs.glob('*.json'):
    if path.stem == 'Japanese':
        continue  # Japanese role descriptions are authored directly in RoleGuideCatalog.
    values = json.loads(path.read_text(encoding='utf-8'))
    assert set(values) == set(english), path.name
    for key in keys:
        text = values[key]
        assert sorted(re.findall(r'\{\d+\}', key)) == sorted(re.findall(r'\{\d+\}', text)), path.name
        assert path.stem == 'English' or text != key, path.name
        missing = set(map(ord, text)) - common - fonts.get(path.stem, set()) - {10, 13, 9}
        assert not missing, (path.name, ''.join(map(chr, sorted(missing))))
        checks += 1

assets = ROOT/'Assets/role-emblems-semibot-v1'
manifest = json.loads((assets/'transparent/manifest.json').read_text(encoding='utf-8'))
record = next(x for x in manifest['assets'] if x['name'] == '42-Signalman')
for kind, directory in [('source', 'selected'), ('master', 'transparent/masters'), ('runtime', 'transparent/runtime')]:
    path = assets/directory/'42-Signalman.png'
    assert hashlib.sha256(path.read_bytes()).hexdigest() == record[kind+'_sha256'], path
    checks += 1
with Image.open(assets/'transparent/runtime/42-Signalman.png') as icon:
    assert icon.mode == 'RGBA' and icon.size == (256, 256)
    assert icon.getpixel((0, 0))[3] == 0
    assert record['background_color'] == '#24563E' and record['category'] == 'Support'
    assert record['outside_mask_pixels_unchanged'] and record['alpha_preserved_during_recolor']
    checks += 4
with Image.open(ROOT/'docs/icons/42.png') as thumb:
    assert thumb.mode == 'RGBA' and thumb.size == (64, 64)
    checks += 1
print(f'Signalman artwork and localization checks passed: {checks}')
