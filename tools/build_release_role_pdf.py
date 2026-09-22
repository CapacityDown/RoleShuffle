"""Create Japanese full/public role catalogs from a release ZIP and matching Git ref.

Example:
  python tools/build_release_role_pdf.py --zip output/release/RoleShuffle-4.5.1.zip \
    --source-ref 31d369ea7a1dc925dd045d7ce7fdc9ff9a68ceaf
"""
from __future__ import annotations

import argparse
import hashlib
import html
import io
import json
import re
import subprocess
from dataclasses import dataclass
from functools import lru_cache
from pathlib import Path
from zipfile import ZipFile

from PIL import Image
from pypdf import PdfReader
from reportlab.lib.colors import HexColor, white
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph, Table, TableStyle

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / 'output/pdf'
QA = ROOT / 'tmp/pdfs/release-catalog'
W, H = A4
M = 38
CW = W - 2 * M
INK, MUTED = HexColor('#182938'), HexColor('#526576')
CYAN, LIGHT = HexColor('#007D9C'), HexColor('#EFF7FA')
LINE, NAVY = HexColor('#D2E1E7'), HexColor('#142C3B')
ISSUES = 'https://github.com/CapacityDown/RoleShuffle/issues'
CATEGORIES = [
    ('Enhancement', '強化'), ('Support', '支援'), ('Combat', '戦闘'),
    ('Special', '特殊'), ('Danger', '危険'), ('Hardship', 'ハンデ'),
]


@dataclass
class Role:
    name: str
    number: str
    category: str
    color: str
    icon: bytes
    effect: str
    limits: str
    settings: str


def markup(value):
    value = html.unescape(value).replace('`', '')
    value = re.sub(r'<br\s*/?>', '\n', value)
    value = value.replace('\u2011', '-').replace('\u2013', '-').replace('\u2014', '-')
    value = html.escape(value).replace('\n', '<br/>')
    return re.sub(r'\*\*(.*?)\*\*', r'<b>\1</b>', value)


def paragraph(value, width, size=9.3, color=INK, bold=False):
    p = Paragraph(markup(value), ParagraphStyle('body', fontName='JPB' if bold else 'JP',
        fontSize=size, leading=size * 1.5, textColor=color, wordWrap='CJK'))
    _, height = p.wrap(width, 2000)
    return p, height


def text(c, value, x, top, width, size=9.3, color=INK, bold=False):
    p, height = paragraph(value, width, size, color, bold)
    p.drawOn(c, x, top - height)
    return top - height


def label(c, value, x, y, size=10, color=INK, bold=True):
    c.setFont('JPB' if bold else 'JP', size)
    c.setFillColor(color)
    c.drawString(x, y, value)


def sections(role):
    return [('能力・既定効果', role.effect, 9.4, INK),
            ('条件・注意', role.limits, 9.0, MUTED),
            ('設定できる項目', role.settings, 8.4, MUTED)]


def card_height(role):
    return max(130, 49 + sum(14 + paragraph(body, CW - 106, size)[1] + 7
                            for _, body, size, _ in sections(role)))


def card(c, role, top):
    height = card_height(role)
    bottom = top - height
    c.setFillColor(LIGHT)
    c.roundRect(M, bottom, CW, height, 7, fill=1, stroke=0)
    c.setFillColor(HexColor(role.color))
    c.roundRect(M, bottom, 4, height, 2, fill=1, stroke=0)
    c.drawImage(ImageReader(io.BytesIO(role.icon)), M + 13, top - 89, 70, 70, mask='auto')
    label(c, role.number, M + 32, top - 108, 8, MUTED)
    x, width = M + 93, CW - 106
    label(c, role.name, x, top - 25, 17)
    cursor = top - 40
    for heading, body, size, color in sections(role):
        cursor = text(c, heading, x, cursor, width, 7.8, CYAN, True) - 2.3
        cursor = text(c, body, x, cursor, width, size, color) - 7
    assert cursor >= bottom + 5, (role.name, cursor, bottom)
    key = 'role-' + role.number
    c.bookmarkPage(key)
    c.addOutlineEntry(role.name, key, level=1)
    return bottom


def table(c, rows, top, widths, size=9):
    cells = [[paragraph(str(cell), widths[col] - 16, size,
                        white if row == 0 else INK, row == 0)[0]
              for col, cell in enumerate(values)] for row, values in enumerate(rows)]
    t = Table(cells, colWidths=widths)
    t.setStyle(TableStyle([
        ('BACKGROUND', (0, 0), (-1, 0), NAVY),
        ('ROWBACKGROUNDS', (0, 1), (-1, -1), [LIGHT, white]),
        ('VALIGN', (0, 0), (-1, -1), 'MIDDLE'),
        ('LEFTPADDING', (0, 0), (-1, -1), 8), ('RIGHTPADDING', (0, 0), (-1, -1), 8),
        ('TOPPADDING', (0, 0), (-1, -1), 6), ('BOTTOMPADDING', (0, 0), (-1, -1), 6),
    ]))
    _, height = t.wrap(CW, H)
    t.drawOn(c, M, top - height)
    return top - height


class Catalog:
    def __init__(self, archive, source_ref):
        self.source_ref = source_ref
        self.archive = archive
        with ZipFile(archive) as z:
            self.version = json.loads(z.read('manifest.json'))['version_number']
            self.readme = z.read('README.md').decode('utf-8-sig')
        source_version = json.loads(self.source('package/manifest.json'))['version_number']
        assert source_version == self.version, (source_version, self.version)
        assert self.source('package/README.md').decode('utf-8-sig') == self.readme
        self.assets = json.loads(self.source('Assets/role-emblems-semibot-v1/transparent/manifest.json'))['assets']
        self.asset_by_name = {a['name'].split('-', 1)[-1]: a for a in self.assets}
        self.rows = []
        body = self.readme.split('### 役職一覧', 1)[1].split('\n### ', 1)[0]
        for line in body.splitlines():
            if not line.startswith('|'):
                continue
            cells = [cell.strip() for cell in line.strip('|').split('|')]
            if cells[0] == '役職' or cells[0].startswith('---'):
                continue
            assert len(cells) == 4
            assert cells[0].isascii(), cells[0]
            self.rows.append(cells)
        assert len(self.rows) == 43 and len({r[0] for r in self.rows}) == 43
        # This catalog is explicitly reviewed against the v4.5.1 release.
        assert self.version == '4.5.1', 'Review common rules and appendix defaults for the new release.'
        self.guides = self.source('RoleGuideCatalog.cs').decode('utf-8-sig')
        self.roles = [self.role_from_row(r) for r in self.rows if not r[0].startswith('???')]
        self.drawn_icon_hashes = set()

    @lru_cache(maxsize=64)
    def source(self, name):
        return subprocess.check_output(['git', 'show', f'{self.source_ref}:{name}'], cwd=ROOT)

    def icon(self, name):
        item = self.asset_by_name[name]
        raw = self.source('Assets/role-emblems-semibot-v1/transparent/runtime/' + item['name'] + '.png')
        assert hashlib.sha256(raw).hexdigest() == item['runtime_sha256']
        with Image.open(io.BytesIO(raw)) as img:
            assert img.size == (256, 256)
        return raw

    def role_from_row(self, row):
        name, effect, limits, settings = row
        asset = self.asset_by_name[name]
        number = asset['name'].split('-', 1)[0]
        # Upgrade configuration upper bounds are 200 in the matching release source.
        if name in {'Jumper', 'Launcher', 'Climber', 'Flyer', 'Tracker'}:
            assert 'MaximumUpgradeLevel = 200' in self.source('RoleUpgradeScaling.cs').decode()
            settings = settings.replace('`100`', '`200`')
        if name in {'Bomber', 'Stinker'}:
            limits += ' 任意に停止する操作はありません。'
        if name == 'Courier':
            effect = effect.replace('（下表）', '。サイズ別の報酬は巻末の早見表を参照')
        if name == 'Phoenix':
            limits += ' Stage FluxのSecond Chanceが先に発動した場合は回数を温存します。'
        return Role(name, number, asset['category'], asset['background_color'], self.icon(name), effect, limits, settings)

    def secrets(self, public):
        if public:
            icon = self.source('Assets/role-emblems-semibot-v1/transparent/runtime/Unrevealed.png')
            return [Role(f'???{n}', f'100{n}', 'Secret', '#526576', icon,
                         '???', '名前・能力・専用アイコンは公開版では伏せています。', '???') for n in (1, 2)]
        super_text = re.search(r'private static string RevealedSuperbotText.*?\? "((?:\\.|[^"\\])*)"', self.guides, re.S)[1]
        disaster_text = re.search(r'private static string RevealedSecretText.*?\? "((?:\\.|[^"\\])*)"', self.guides, re.S)[1]
        super_text = json.loads('"' + super_text + '"').replace('\n', '')
        disaster_text = json.loads('"' + disaster_text + '"')
        result = []
        for name, number, effect, limits in [
            ('Superbot', '1001', super_text,
             '各能力の回復量・回数などは、対応する役職の設定に従います。通常役職の候補不足を埋める補完には使用しません。'),
            ('Disaster', '1002', disaster_text,
             'Influenzaは30秒後に発症し、最大HPが75に固定。くしゃみ・VC・チャットで感染を広げ、くしゃみは敵にも聞こえます。感染条件はInfluenzaのページと同じです。1人では抽選対象外。通常役職の候補不足を埋める補完には使用しません。'),
        ]:
            asset = self.asset_by_name[name]
            result.append(Role(name, number, 'Secret', asset['background_color'], self.icon(name), effect, limits,
                '役職の有効／無効。個々の能力は対応する役職の設定を使用します。'))
        return result

    def plan(self, public):
        groups = [(key, title, [r for r in self.roles if r.category == key]) for key, title in CATEGORIES]
        groups.append(('Secret', '隠し役職' + ('（非公開）' if public else ''), self.secrets(public)))
        pages = []
        for key, title, roles in groups:
            page, used = [], 0
            for role in roles:
                height = card_height(role)
                assert height < H - 163, role.name
                if page and used + 10 + height > H - 163:
                    pages.append((key, title, page))
                    page, used = [], 0
                page.append(role)
                used += height + (10 if used else 0)
            if page:
                pages.append((key, title, page))
        return pages

    def frame(self, c, title, subtitle, page, count, public):
        c.setFillColor(CYAN)
        c.rect(M, H - 40, 24, 3, fill=1, stroke=0)
        label(c, 'ROLESHUFFLE / ROLE CATALOG', M + 34, H - 41, 8, CYAN)
        label(c, title, M, H - 75, 23)
        text(c, subtitle, M, H - 86, CW, 9, MUTED)
        c.setStrokeColor(LINE)
        c.setLineWidth(.6)
        c.line(M, 34, W - M, 34)
        label(c, f'RoleShuffle v{self.version} / ' + ('公開版' if public else '詳細版') + ' / 既定設定', M, 20, 7.5, MUTED, False)
        c.setFont('JP', 8)
        c.drawRightString(W - M, 20, f'{page:02d} / {count:02d}')

    def cover(self, c, pages, count, public):
        self.frame(c, '', '', 1, count, public)
        c.bookmarkPage('cover')
        c.addOutlineEntry('はじめに・目次', 'cover', level=0)
        c.setFillColor(NAVY)
        c.rect(0, H - 218, W, 218, fill=1, stroke=0)
        label(c, 'ROLESHUFFLE', M, H - 50, 14, HexColor('#69DAF4'))
        label(c, '役職一覧', M, H - 108, 39, white)
        label(c, f'v{self.version}  日本語ガイド / ' + ('公開版' if public else '詳細版'), M, H - 143, 14, white, False)
        text(c, '通常41役職 + 隠し役職2種\n能力・条件・使用上限・設定の早見表', M, H - 166, CW, 10.5, HexColor('#C8DBE5'))
        top = H - 246
        top = text(c, '数値はv4.5.1の既定設定です。実際の倍率・回数・抽選対象はホスト設定で変わります。役職名はゲーム内と同じ英語表記です。', M, top, CW, 10.2) - 10
        top = text(c, '役職はステージ開始時に割り当てられ、ステージ終了で解除されます。Baseは役職を除いた基礎アップグレードです。ゲームプレイはホストのみの導入に対応し、RolesメニューとHUDの表示には各自の導入が必要です。', M, top, CW, 9.5, MUTED) - 12
        top = text(c, '隠し役職の名前・能力・専用アイコンを伏せています。' if public else '隠し役職の名前・能力・専用アイコンを含みます。公開配布には公開版を使用してください。', M, top, CW, 9.5, CYAN, True) - 20
        label(c, '目次', M, top, 13)
        top -= 20
        for key, title in CATEGORIES + [('Secret', '隠し役職' + ('（非公開）' if public else ''))]:
            indexes = [i + 2 for i, p in enumerate(pages) if p[0] == key]
            roles = [r for k, _, rows in pages if k == key for r in rows]
            label(c, title, M, top, 10)
            c.setFont('JPB', 10)
            c.setFillColor(CYAN)
            c.drawRightString(W - M, top, str(indexes[0]) if len(indexes) == 1 else f'{indexes[0]} - {indexes[-1]}')
            c.linkRect('', 'group-' + key, (M, top - 26, W - M, top + 12), relative=0, thickness=0)
            top = text(c, ' / '.join(r.name for r in roles), M, top - 5, CW - 44, 8.3, MUTED) - 15
        for offset, title in enumerate(['使い方・HUD・設定', '配達報酬・能力の操作', '人数・HP連動の強化']):
            label(c, title, M, top, 9.5)
            c.setFont('JPB', 9.5)
            c.setFillColor(CYAN)
            c.drawRightString(W - M, top, str(len(pages) + 2 + offset))
            c.linkRect('', f'ref-{offset}', (M, top - 5, W - M, top + 12), relative=0, thickness=0)
            top -= 22
        assert top > 42, top
        c.showPage()

    def references(self, c, first, count, public):
        titles = ['使い方・HUD・設定', '配達報酬・能力の操作', '人数・HP連動の強化']
        for offset, title in enumerate(titles):
            self.frame(c, title, '数値・操作は既定設定です。ホストが変更している場合があります。', first + offset, count, public)
            c.bookmarkPage(f'ref-{offset}')
            c.addOutlineEntry(title, f'ref-{offset}', level=0)
            top = H - 126
            if offset == 0:
                blocks = [
                    ('役職の確認', 'チャットに /roles と入力すると、割り当てられた役職名を通知します。MOD導入済みなら、ロビー・EscメニューのROLESから役職ガイドを開けます。CURRENT ROLESでは自分の役職を先頭に表示します。'),
                    ('能力残量HUD', '左上のHP・スタミナの下に、水色のアイコンと「残量 / 上限」を縦1列で表示します。使い切った項目は赤色です。Medicの150 / 150は回復できるHP、Mechanicの50 / 50は修理できる割合の残量です。各自の生存中に表示されます。'),
                    ('ホストが変更する設定', 'REPOConfig → RoleShuffle → 役職名で、役職の有効／無効、抽選Weight、能力の数値を調整できます。Tank・Runner・Lifter・Courier・King・Stinker・Influenzaの能力も変更できます。役職のON/OFFとプリセットはROLESからも操作できます。'),
                    ('自分の画面に使う設定', 'HUDの配置・文字サイズ・アイコン・表示のON/OFFは各自で設定します。TOOLS → HUD編集モードで調整し、「保存」で反映します。「取消」またはEscでは編集を破棄します。言語選択も保存され、再起動後に復元されます。'),
                    ('Baseと役職の関係', 'Baseにはホスト設定・手動調整・トラック抽選の結果が含まれます。役職効果が終了すると基礎値へ戻ります。RammerのTumble系0固定など、例外は各役職欄に記載しています。手動調整とトラック抽選の結果はセーブごとに保持します。'),
                    ('相性・注意', 'Stage FluxのSecond ChanceはPhoenixより先に判定され、発動した場合はPhoenixの回数を温存します。MageとTricksterは表情の解除では発動しませんが、参加者がメニューを閉じて表情が戻る際に能力が発動する場合があります。'),
                ]
                for heading, body in blocks:
                    top = text(c, heading, M, top, CW, 12, CYAN, True) - 5
                    top = text(c, body, M, top, CW, 10) - 19
                top = text(c, 'お問い合わせ: ' + ISSUES, M, top, CW, 8.5, MUTED)
                c.linkURL(ISSUES, (M, top - 3, W - M, top + 15), relative=0, thickness=0)
            elif offset == 1:
                top = text(c, 'Courier / サイズ別の配達報酬', M, top, CW, 13, CYAN, True) - 10
                rows = [['貴重品のサイズ', 'HP回復', '自動HP減少の停止'],
                        ['極小 / Tiny', '10', '30秒'], ['小 / Small', '25', '30秒'],
                        ['中 / Medium', '50', '60秒'], ['大 / Big', '100', '90秒'],
                        ['横長 / Wide', '100', '90秒'], ['縦長 / Tall', '100', '90秒'],
                        ['超縦長 / VeryTall', '100', '120秒']]
                top = table(c, rows, top, [CW * .44, CW * .22, CW * .34], 9.5) - 12
                top = text(c, '価値のある貴重品を掴んだまま搬入先の外で5m運び、トラックか納品所に入れると達成。回復は最大HPまで。同じ品の報酬は各自1ステージ1回、配達回数は無制限です。敵の攻撃などを防ぐ無敵効果ではありません。', M, top, CW, 9.5) - 23
                top = text(c, 'Mage / チャット・表情・消費HP', M, top, CW, 13, CYAN, True) - 10
                top = table(c, [['チャット', '消費HP', '表情'], ['star', 10, 'Angry'],
                    ['gravity', 10, 'Suspicious'], ['roll', 15, 'Sad'], ['void', 30, 'EyesClosed'],
                    ['laser', 50, 'Scared']], top, [CW * .32, CW * .23, CW * .45], 9.5) - 13
                top = text(c, '共通クールダウンは3秒。HP消費で死亡する場合は発動しません。Tricksterは decoy またはHappyの表情で発動します。能力のチャット入力は大文字・小文字を問いません。', M, top, CW, 9.5)
            else:
                source = self.source('RoleUpgradeScaling.cs').decode('utf-8-sig')
                def rules(role):
                    body = source.split('string ' + role + 'Default', 1)[1].split('};', 1)[0]
                    return {name: [(int(a), int(b)) for a, b in (p.split(':') for p in expr.split(','))]
                            for name, expr in re.findall(r'"(\w+)" => "([0-9:,]+)"', body)}
                inf, ber = rules('Influencer'), rules('Berserker')
                def value(mapping, name, condition, is_influencer):
                    matches = [val for threshold, val in mapping.get(name, [])
                               if (condition >= threshold if is_influencer else condition <= threshold)]
                    return matches[-1] if matches else 0
                top = text(c, 'Influencer / 20m以内の生存中の味方', M, top, CW, 12, CYAN, True) - 10
                names = ['Strength', 'Speed', 'Stamina', 'ExtraJump', 'TumbleWings', 'Health']
                rows = [['人数', 'Strength', 'Speed', 'Stamina', 'ExtraJump', 'Wings', 'Health']]
                for n in range(1, 6):
                    rows.append([f'{n}人' + ('以上' if n == 5 else '')] + [value(inf, name, n, True) for name in names])
                top = table(c, rows, top, [66] + [(CW - 66) / 6] * 6, 8) - 12
                top = text(c, '0人では人数連動の追加強化なし。WingsはTumble Wingsの略です。表は目標レベルで、高いBaseレベルを下げる効果ではありません。', M, top, CW, 9) - 25
                top = text(c, 'Berserker / 残りHPの割合', M, top, CW, 12, CYAN, True) - 10
                rows = [['残りHP', 'Strength', 'Speed', 'Stamina', 'ExtraJump']]
                for n, label_text in [(100, '80%超'), (80, '60%超 - 80%以下'), (60, '40%超 - 60%以下'),
                                      (40, '20%超 - 40%以下'), (20, '10%超 - 20%以下'), (10, '10%以下')]:
                    rows.append([label_text] + [value(ber, name, n, False) for name in names[:4]])
                top = table(c, rows, top, [151] + [(CW - 151) / 4] * 4, 8.5) - 13
                top = text(c, 'HPを回復すると、条件に応じて追加の強化も戻ります。HealthとTumble Wingsへの追加強化はありません。対象アップグレードごとの条件・目標はホスト設定で変更できます。', M, top, CW, 9.5)
            assert top > 46, (title, top)
            c.showPage()

    def build(self, public):
        pages = self.plan(public)
        count = 1 + len(pages) + 3
        suffix = f'Public_v{self.version}_JA' if public else f'v{self.version}_JA'
        path = OUT / f'RoleShuffle_Role_List_{suffix}.pdf'
        assert not path.exists(), f'Preserve or move the earlier PDF before replacing: {path}'
        c = canvas.Canvas(str(path), pagesize=A4, pageCompression=1)
        c.setTitle(f'RoleShuffle v{self.version} 役職一覧 ' + ('公開版' if public else '詳細版'))
        c.setAuthor('RoleShuffle')
        c.setSubject('日本語ガイド / 既定設定 / 通常41役職・隠し役職2種')
        self.cover(c, pages, count, public)
        seen = set()
        for page_no, (key, title, roles) in enumerate(pages, start=2):
            self.frame(c, title, ' / '.join(r.name for r in roles), page_no, count, public)
            if key not in seen:
                c.bookmarkPage('group-' + key)
                c.addOutlineEntry(title, 'group-' + key, level=0)
                seen.add(key)
            top = H - 116
            for role in roles:
                top = card(c, role, top) - 10
                self.drawn_icon_hashes.add(hashlib.sha256(role.icon).hexdigest())
            assert top > 36
            c.showPage()
        self.references(c, len(pages) + 2, count, public)
        c.save()
        reader = PdfReader(path)
        assert len(reader.pages) == count
        extracted = '\n'.join(p.extract_text() for p in reader.pages)
        expected = self.roles + self.secrets(public)
        for role in expected:
            assert role.name in extracted, role.name
        assert '\ufffd' not in extracted and '<br' not in extracted and '**' not in extracted
        assert '4.5.2' not in extracted and 'Jobless' not in extracted
        if public:
            searchable = extracted + str(reader.metadata) + str(reader.outline)
            for name in ['Superbot', 'Disaster']:
                assert name.casefold() not in searchable.casefold()
                assert hashlib.sha256(self.icon(name)).hexdigest() not in self.drawn_icon_hashes
                for p in reader.pages:
                    assert name.encode() not in p.get_contents().get_data()
        for p in reader.pages:
            for ref in p['/Resources']['/Font'].values():
                font = ref.get_object()
                if font.get('/BaseFont') == '/Helvetica':
                    continue  # ReportLab's unused initial font resource.
                desc = font['/FontDescriptor'].get_object()
                assert '/FontFile2' in desc or '/FontFile3' in desc
        QA.mkdir(parents=True, exist_ok=True)
        (QA / (path.stem + '.txt')).write_text(extracted, encoding='utf-8')
        report = {'file': str(path), 'pages': count, 'roles': len(expected), 'bytes': path.stat().st_size,
                  'sha256': hashlib.sha256(path.read_bytes()).hexdigest(), 'source_ref': self.source_ref,
                  'source_zip_sha256': hashlib.sha256(self.archive.read_bytes()).hexdigest(),
                  'secret_names_and_artwork_masked': public}
        (QA / (path.stem + '.json')).write_text(json.dumps(report, ensure_ascii=False, indent=2), encoding='utf-8')
        print(json.dumps(report, ensure_ascii=False))


def main():
    args = argparse.ArgumentParser(description=__doc__)
    args.add_argument('--zip', type=Path, required=True)
    args.add_argument('--source-ref', required=True)
    ns = args.parse_args()
    OUT.mkdir(parents=True, exist_ok=True)
    pdfmetrics.registerFont(TTFont('JP', 'C:/Windows/Fonts/BIZ-UDGothicR.ttc', subfontIndex=0))
    pdfmetrics.registerFont(TTFont('JPB', 'C:/Windows/Fonts/BIZ-UDGothicB.ttc', subfontIndex=0))
    pdfmetrics.registerFontFamily('JP', normal='JP', bold='JPB')
    for public in (True, False):
        Catalog(ns.zip, ns.source_ref).build(public)


if __name__ == '__main__':
    main()
