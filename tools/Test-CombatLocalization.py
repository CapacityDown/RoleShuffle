"""Validate the four changed roles' templates, translations, glyphs and release Markdown."""
import json,re
from pathlib import Path
from fontTools.ttLib import TTFont
from collect_localization_strings import catalog
from check_release_markdown import validate_markdown
root=Path(__file__).resolve().parents[1]
prefixes=['Supports allies from its Death Head','Uses less weapon battery and earns','Gains temporary enemy damage bonuses','Builds melee damage by repeatedly','Death Head Battery level','Weapon battery use:','An ally dying within','Melee damage starts at']
generated=catalog();keys=[next(k for k in generated if k.startswith(prefix)) for prefix in prefixes]
english=json.loads((root/'Assets/Localization/English.json').read_text(encoding='utf-8'))
fonts={p.stem:set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').iterdir() if p.suffix in ('.otf','.ttf')}
checks=0
for p in (root/'Assets/Localization').glob('*.json'):
 if p.stem=='Japanese':continue
 values=json.loads(p.read_text(encoding='utf-8'));assert set(values)==set(english);checks+=1
 glyphs=fonts['Names']|fonts['European']|fonts.get(p.stem,set())
 for key in keys:
  assert values[key] and sorted(re.findall(r'\{\d+\}',key))==sorted(re.findall(r'\{\d+\}',values[key])),p.name
  assert not(set(map(ord,values[key]))-glyphs-{9,10,13}),(p.name,key)
  assert p.stem=='English' or values[key]!=key,p.name
  checks+=3
for name in ('README.md','CHANGELOG.md'):
 text=validate_markdown(root/'package'/name);assert not re.search(r'## [0-9.]+\n\n',text)
 print(f'{name}: {len(text):,}/100,000 characters.');checks+=1
print(f'PASS: {checks} combat localization, bundled glyph and document checks.')
