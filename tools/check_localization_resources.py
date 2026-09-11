"""Verify catalog keys, placeholders and bundled Unicode glyph coverage."""
import json
import re
from pathlib import Path
from fontTools.ttLib import TTFont
from collect_localization_strings import catalog

root=Path(__file__).resolve().parents[1]
folder=root/'Assets/Localization'
english=json.loads((folder/'English.json').read_text(encoding='utf-8'))
assert english==catalog(), 'Regenerate the English catalog after editing source strings'
fonts={p.stem:set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').glob('*') if p.suffix in ['.otf','.ttf']}
common=fonts['Names']|fonts['European']
checked=0
for path in folder.glob('*.json'):
    values=json.loads(path.read_text(encoding='utf-8'))
    if path.stem=='Japanese':
        # Existing Japanese role descriptions use the pre-existing bundle.
        assert set(list(english)[84:])==set(values), 'Japanese UI keys'
        continue
    assert set(values)==set(english),path.name
    glyphs=common|fonts.get(path.stem,set())
    for key,text in values.items():
        assert sorted(re.findall(r'\{\d+\}',key))==sorted(re.findall(r'\{\d+\}',text)), (path.name,key)
        missing=set(map(ord,text)) - glyphs - {10,13,9}
        assert not missing,(path.name,key,missing)
        checked+=1
native=''.join(re.findall(r'=> "([^"]+)"',(root/'RoleLanguage.cs').read_text(encoding='utf-8')))
assert not(set(map(ord,native+native.upper()))-common),'Native setting names missing glyphs'
print(f'Validated {checked} phrases, Japanese UI coverage, and native setting-name glyphs.')
