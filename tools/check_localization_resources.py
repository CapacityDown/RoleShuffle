"""Verify catalog keys, placeholders and bundled Unicode glyph coverage."""
import argparse
import json
import re
from pathlib import Path
from fontTools.ttLib import TTFont
from collect_localization_strings import catalog

parser = argparse.ArgumentParser()
parser.add_argument('--shaped-glyphs', type=Path, help='Output from Test-NativeTextShaping.ps1')
args = parser.parse_args()
root=Path(__file__).resolve().parents[1]
folder=root/'Assets/Localization'
english=json.loads((folder/'English.json').read_text(encoding='utf-8'))
assert set(catalog()) <= set(english), 'Add new source phrases to the English catalog; retain legacy host phrases'
assert all(key == value for key, value in english.items()), 'English catalog must retain canonical keys'
assert len(list(folder.glob('*.json'))) == 21, 'All native game locales require a catalog'
fonts={p.stem:set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').glob('*') if p.suffix in ['.otf','.ttf']}
common=fonts['Names']|fonts['European']
checked=0
for path in folder.glob('*.json'):
    values=json.loads(path.read_text(encoding='utf-8'))
    if path.stem=='Japanese':
        # Existing Japanese role descriptions use the pre-existing bundle.
        assert set(values) <= set(english) and all(values.values()), 'Japanese UI keys'
        for key, text in values.items():
            assert sorted(re.findall(r'\{\d+\}',key)) == sorted(re.findall(r'\{\d+\}',text)), ('Japanese',key)
        continue
    assert set(values)==set(english),path.name
    glyphs=common|fonts.get(path.stem,set())
    for key,text in values.items():
        assert sorted(re.findall(r'\{\d+\}',key))==sorted(re.findall(r'\{\d+\}',text)), (path.name,key)
        missing=set(map(ord,text + text.upper())) - glyphs - {10,13,9}
        assert not missing,(path.name,key,missing)
        checked+=1
native=''.join(re.findall(r'=> "([^"]+)"',(root/'RoleLanguage.cs').read_text(encoding='utf-8')))
assert not(set(map(ord,native+native.upper()))-set.union(*fonts.values())),'Native names missing glyphs'
print(f'Validated {checked} phrases, Japanese UI coverage, and native setting-name glyphs.')

if args.shaped_glyphs:
    shaped = json.loads(args.shaped_glyphs.read_text(encoding='utf-8-sig'))
    assert set(shaped) == {'Arabic','Thai'}
    for language, codepoints in shaped.items():
        missing = set(codepoints) - (fonts[language] | common) - {9,10,13}
        assert not missing, ('Native shaped glyphs',language,missing)
    print('Validated native Arabic joining forms and Thai shaped glyphs against bundled fonts.')
