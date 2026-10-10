import json,re
from pathlib import Path
from fontTools.ttLib import TTFont
from collect_localization_strings import catalog
root=Path(__file__).resolve().parents[1]
generated=catalog()
keys=[next(k for k in generated if k.startswith(p)) for p in ['A second melee weapon','Holding a melee weapon alone']]
english=json.loads((root/'Assets/Localization/English.json').read_text(encoding='utf-8'))
fonts={p.stem:set(TTFont(p).getBestCmap()) for p in (root/'Assets/Fonts/Localization').iterdir() if p.suffix in ('.otf','.ttf')}
count=0
for p in (root/'Assets/Localization').glob('*.json'):
    if p.stem=='Japanese':continue
    d=json.loads(p.read_text(encoding='utf-8'));assert set(d)==set(english),p
    glyphs=fonts['Names']|fonts['European']|fonts.get(p.stem,set())
    for key in keys:
        value=d[key];assert sorted(re.findall(r'\{\d+\}',key))==sorted(re.findall(r'\{\d+\}',value)),p
        assert p.stem=='English' or value!=key,(p,key)
        assert not(set(map(ord,value))-glyphs-{9,10,13}),(p,key)
        count+=3
print(f'PASS: {count} DualWielder translation, parameter and bundled glyph checks.')
