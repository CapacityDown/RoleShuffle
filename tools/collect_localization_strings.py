"""Generate the English phrase catalog used to validate translated resources."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
TOKEN = r'\$?"((?:\\.|[^"\\])*)"'

def decode(value):
    return json.loads('"' + value + '"')

def catalog():
    result = {}
    source = (ROOT / 'RoleGuideCatalog.cs').read_text(encoding='utf-8-sig')
    # Merge adjacent C# literals so translators receive complete paragraphs.
    for group in re.finditer(TOKEN + r'(?:\s*\+\s*' + TOKEN + r')*', source):
        value = ''.join(decode(match[1]) for match in re.finditer(TOKEN, group[0]))
        if any(ord(c) > 127 for c in value) or len(value) < 15:
            continue
        index = 0
        def parameter(_):
            nonlocal index
            placeholder = '{' + str(index) + '}'
            index += 1
            return placeholder
        value = re.sub(r'\{[^{}]+\}', parameter, value)
        result[value] = value
    for filename in ['RoleMenu.cs', 'RoleHudEditor.cs', 'PlayerUtilitiesRuntime.cs']:
        source = (ROOT / filename).read_text(encoding='utf-8-sig')
        for match in re.finditer(r'(?:Localized|Pick)\(\s*' + TOKEN, source):
            value = decode(match[1])
            result[value] = value
    for value in [
        'CURRENT ROLES', 'ROLE GUIDE', 'BASE UPGRADES', 'DRAW HISTORY', 'TOOLS', 'Back',
        'Current Roles', 'Role Guide', 'Base Upgrades', 'Draw History', 'Tools', 'Bug Report',
        'No roles are currently assigned.', 'Base Upgrade data is not available yet.',
        'Shared targets used when a role does not replace an upgrade.', 'Configured', 'Truck Draw',
        'YOU', 'Player', 'Level', 'All Upgrades', 'No change (zero draw or level limit)',
        'Anchor: ', 'Scale', 'NameOnly', 'IconAndName', 'IconOnly', 'Left', 'Center', 'Right',
        'TopLeft', 'TopCenter', 'TopRight', 'MiddleLeft', 'MiddleCenter', 'MiddleRight',
        'BottomLeft', 'BottomCenter', 'BottomRight', 'ON', 'OFF',
        'Host v{0} / Local v{1}',
    ]:
        result[value] = value
    return result

if __name__ == '__main__':
    output = ROOT / 'Assets' / 'Localization' / 'English.json'
    output.parent.mkdir(parents=True, exist_ok=True)
    values = catalog()
    output.write_text(json.dumps(values, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(f'{len(values)} English phrases: {output}')
