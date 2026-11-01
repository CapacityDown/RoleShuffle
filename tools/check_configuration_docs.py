"""Compare player-facing configuration tables with the live BepInEx inventory."""
import argparse, json, re
from pathlib import Path

parser=argparse.ArgumentParser()
parser.add_argument('inventory',type=Path)
args=parser.parse_args()
root=Path(__file__).resolve().parents[1]
entries=json.loads(args.inventory.read_text(encoding='utf-8'))
text=(root/'package/README.md').read_text(encoding='utf-8')
assert len(text)<=100000, len(text)
checks=0
number=r'-?\d+(?:\.\d+)?'
for language,part in zip(['English','Japanese'],text.split('## 日本語',1)):
    expanded=part
    for size in ['Tiny','Small','Medium','Big','Wide','Tall','VeryTall']:
        expanded+=part.replace('<Size>',size)
    for upgrade in ['Health','Speed','Stamina','Strength','Range','ExtraJump','Launch','TumbleClimb','TumbleWings','CrouchRest','MapPlayerCount','DeathHeadBattery']:
        expanded+=part.replace('<Upgrade>',upgrade)
    for entry in entries:
        if entry['section'].startswith('???'): continue
        assert entry['key'] in expanded,(language,'missing key',entry['section'],entry['key'])
        checks+=1
    expected=None
    for line in part.splitlines():
        if not line.startswith('|'):
            expected=None
            continue
        cells=line.split('|')[1:-1]
        if expected is None: expected=len(cells)
        assert len(cells)==expected,(language,'table columns',line)
        keys=re.findall(r'`([^`]+)`',cells[0])
        if not keys or len(cells)<4: continue
        defaults=[v.strip(' `') for v in cells[1].split(' / ')]
        ranges=[v.strip(' `') for v in cells[2].split(' / ')]
        section=''
        for index,key in enumerate(keys):
            if '.' in key: section,key=key.rsplit('.',1)
            matches=[e for e in entries if e['section']==section and e['key']==key]
            if not matches: continue  # documented placeholders and grouped lists
            entry=matches[0]
            if entry['type'] not in ['Int32','Single']:continue
            if len(defaults)==len(keys) and re.fullmatch(number+r'(?: [a-zA-Z%]+)?',defaults[index]):
                found=float(re.match(number,defaults[index])[0])
                assert abs(found-float(entry['defaultValue']))<0.0001,(language,'default',section,key,found,entry['defaultValue'])
                checks+=1
            cell=ranges[index] if len(ranges)==len(keys) else ranges[0] if len(ranges)==1 else ''
            m=re.match('('+number+r')[–～-]('+number+')',cell)
            native=re.search(r'From ('+number+r') to ('+number+')',entry['range'] or '')
            if m and native:
                assert [float(x) for x in m.groups()]==[float(x) for x in native.groups()],(language,'range',section,key,m.groups(),native.groups())
                checks+=1
    for section in ['Influencer','Berserker']:
        for entry in entries:
            if entry['section']==section and entry['key'].endswith('UpgradeScaling') and entry['defaultValue']:
                assert entry['defaultValue'] in part,(language,'dynamic default',section,entry['key'])
                checks+=1
print(f'PASS: {checks} bilingual setting-key/default/range/table checks; README {len(text)} characters.')
