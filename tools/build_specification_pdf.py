"""Build the Japanese implementation specification from a release ZIP and defaults.

Example:
python tools/build_specification_pdf.py --source-ref 8eb5990
The included defaults were exported from StageRolesConfig with a fresh, temporary
BepInEx ConfigFile via the RoleSettingsChecks assembly (no game or user settings).
"""
from __future__ import annotations

import argparse
import hashlib
import html
import io
import json
import re
import subprocess
import zipfile
from collections import OrderedDict
from datetime import date
from pathlib import Path

from reportlab.lib import colors
from reportlab.lib.enums import TA_CENTER
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    BaseDocTemplate, CondPageBreak, Flowable, Frame, Image, KeepTogether,
    PageBreak, PageTemplate, Paragraph, Spacer, Table, TableStyle,
)
from reportlab.platypus.tableofcontents import TableOfContents
from pypdf import PdfReader

ROOT = Path(__file__).resolve().parents[1]
W, H = A4
LEFT = RIGHT = 18 * mm
TOP = 22 * mm
BOTTOM = 18 * mm
CW = W - LEFT - RIGHT
INK = colors.HexColor('#172331')
MUTED = colors.HexColor('#536474')
NAVY = colors.HexColor('#142A3B')
TEAL = colors.HexColor('#147F92')
AMBER = colors.HexColor('#E7A33B')
LINE = colors.HexColor('#D4DFE6')
PALE = colors.HexColor('#F2F6F8')


def git_text(ref: str, path: str) -> str:
    return subprocess.check_output(['git', 'show', f'{ref}:{path}'], cwd=ROOT).decode('utf-8').replace('\r\n', '\n')


def register_fonts():
    entries = [('JP', 'BIZ-UDGothicR.ttc'), ('JPB', 'BIZ-UDGothicB.ttc'),
               ('Latin', 'arial.ttf'), ('KR', 'malgun.ttf'), ('SC', 'msyh.ttc')]
    for name, file in entries:
        pdfmetrics.registerFont(TTFont(name, str(Path('C:/Windows/Fonts') / file), subfontIndex=0))
    pdfmetrics.registerFontFamily('JP', normal='JP', bold='JPB', italic='JP', boldItalic='JPB')


def normalize(text):
    return str(text).replace('\u2011', '-').replace('\u2013', '-').replace('\u2014', '-').replace('\u2212', '-')


def glyph_markup(text):
    pieces = []
    active = None
    run = []
    for ch in normalize(text):
        font = next((name for name in ('JP', 'Latin', 'KR', 'SC')
                     if ord(ch) in pdfmetrics.getFont(name).face.charToGlyph), None)
        if font is None and ch not in '\n\r\t':
            raise ValueError(f'Unsupported PDF glyph U+{ord(ch):04X}: {ch!r}')
        font = font or 'JP'
        if active != font and run:
            escaped = html.escape(''.join(run))
            pieces.append(escaped if active == 'JP' else f'<font name="{active}">{escaped}</font>')
            run = []
        active = font
        run.append(ch)
    if run:
        escaped = html.escape(''.join(run))
        pieces.append(escaped if active == 'JP' else f'<font name="{active}">{escaped}</font>')
    return ''.join(pieces).replace('\n', '<br/>')


def rich(text):
    chunks = []
    cursor = 0
    pattern = re.compile(r'`([^`]+)`|\*\*(.+?)\*\*|\[([^\]]+)\]\((https?://[^)]+)\)')
    for m in pattern.finditer(normalize(text)):
        chunks.append(glyph_markup(normalize(text)[cursor:m.start()]))
        if m.group(1) is not None:
            chunks.append('<font color="#147F92">' + glyph_markup(m.group(1)) + '</font>')
        elif m.group(2) is not None:
            chunks.append('<b>' + glyph_markup(m.group(2)) + '</b>')
        else:
            chunks.append(f'<link href="{html.escape(m.group(4), quote=True)}" color="#147F92">{glyph_markup(m.group(3))}</link>')
        cursor = m.end()
    chunks.append(glyph_markup(normalize(text)[cursor:]))
    return ''.join(chunks)


def styles():
    base = dict(fontName='JP', textColor=INK, wordWrap='CJK', splitLongWords=True)
    return {
        'body': ParagraphStyle('body', fontSize=9.2, leading=14.7, spaceAfter=7, **base),
        'small': ParagraphStyle('small', fontSize=8, leading=12, spaceAfter=5, **base),
        'table': ParagraphStyle('table', fontSize=7.8, leading=11.6, **base),
        'thead': ParagraphStyle('thead', fontName='JPB', fontSize=8, leading=11.8, textColor=colors.white, wordWrap='CJK'),
        'chapter': ParagraphStyle('chapter', fontName='JPB', fontSize=20, leading=28, textColor=NAVY, wordWrap='CJK', spaceAfter=15, keepWithNext=True),
        'section': ParagraphStyle('section', fontName='JPB', fontSize=11.5, leading=18, textColor=TEAL, spaceBefore=11, spaceAfter=7, keepWithNext=True, wordWrap='CJK'),
        'table_section': ParagraphStyle('table_section', fontName='JPB', fontSize=11.5, leading=18, textColor=TEAL, spaceBefore=11, spaceAfter=7, wordWrap='CJK'),
        'role': ParagraphStyle('role', fontName='JPB', fontSize=13.5, leading=19, textColor=NAVY, spaceAfter=6, keepWithNext=True),
        'bullet': ParagraphStyle('bullet', fontSize=9.1, leading=14.4, spaceAfter=6, leftIndent=11, firstLineIndent=-10, **base),
    }


class StageFlow(Flowable):
    def __init__(self):
        super().__init__()
        self.width, self.height = CW, 82

    def draw(self):
        c = self.canv
        gap = 11
        width = (CW - gap * 3) / 4
        labels = [('ロビー', '設定と準備'), ('ステージ開始', '抽選・強化・通知'), ('ステージ中', '能力・途中参加'), ('ステージ終了', '役職解除・基礎値へ')]
        for i, (title, sub) in enumerate(labels):
            x = i * (width + gap)
            c.setFillColor(NAVY if i in (1, 2) else PALE)
            c.roundRect(x, 12, width, 58, 5, fill=1, stroke=0)
            c.setFillColor(colors.white if i in (1, 2) else INK)
            c.setFont('JPB', 10)
            c.drawCentredString(x + width / 2, 48, title)
            c.setFont('JP', 7.3)
            c.drawCentredString(x + width / 2, 29, sub)
            if i < 3:
                c.setStrokeColor(TEAL)
                c.line(x + width + 2, 40, x + width + gap - 2, 40)
                c.line(x + width + gap - 5, 43, x + width + gap - 2, 40)
                c.line(x + width + gap - 5, 37, x + width + gap - 2, 40)


class SpecDoc(BaseDocTemplate):
    def __init__(self, filename, *, version, build, icon):
        super().__init__(str(filename), pagesize=A4, leftMargin=LEFT, rightMargin=RIGHT,
                         topMargin=TOP, bottomMargin=BOTTOM, title=f'RoleShuffle v{version} 仕様書',
                         author='RoleShuffle', subject='機能・役職・設定・UI・同期・保存・互換性の仕様', pageCompression=1)
        self.version, self.ui_build, self.icon = version, build, icon
        self.section_title = '仕様書'
        self.section_number = 0
        frame = Frame(LEFT, BOTTOM, CW, H - TOP - BOTTOM, leftPadding=0, rightPadding=0, topPadding=0, bottomPadding=0)
        self.addPageTemplates(PageTemplate(id='spec', frames=[frame], onPage=self.page_frame))

    def page_frame(self, c, doc):
        c.saveState()
        if doc.page == 1:
            c.setFillColor(NAVY)
            c.rect(0, 0, W, H, fill=1, stroke=0)
            c.setFillColor(AMBER)
            c.rect(LEFT, H - 106, 45, 5, fill=1, stroke=0)
            c.setFont('Latin', 13)
            c.drawString(LEFT, H - 80, 'FUNCTIONAL & TECHNICAL SPECIFICATION')
            c.setFillColor(colors.white)
            c.setFont('JPB', 33)
            c.drawString(LEFT, H - 170, 'RoleShuffle')
            c.setFont('JPB', 25)
            c.drawString(LEFT, H - 215, '仕様書')
            c.setFont('JP', 13)
            c.drawString(LEFT, H - 252, f'v{self.version} / build {self.ui_build} / 日本語')
            c.drawImage(ImageReader(io.BytesIO(self.icon)), LEFT, 325, 125, 125, mask='auto')
            c.setFont('JPB', 19)
            c.drawString(LEFT + 151, 418, '42役職  /  291設定')
            c.setFont('JP', 10.5)
            for i, line in enumerate(['ホスト中心のゲーム処理', 'Roles UI・HUD・14言語対応', '抽選・保存・同期・MOD互換性']):
                c.drawString(LEFT + 151, 389 - i * 23, line)
            c.setStrokeColor(colors.HexColor('#375165'))
            c.line(LEFT, 273, W - RIGHT, 273)
            c.setFont('JP', 10)
            for i, line in enumerate(['対象: v4.4.7の配布ZIPと対応する実装', '用途: 開発・運用資料（隠し役職の仕様を含む）', f'作成日: {date.today().isoformat()}', '本書は仕様整理であり、実機検証の完了報告ではありません。']):
                c.drawString(LEFT, 242 - i * 23, line)
        else:
            c.setFillColor(TEAL)
            c.rect(LEFT, H - 34, 20, 2.5, fill=1, stroke=0)
            c.setFillColor(MUTED)
            c.setFont('JP', 8)
            c.drawString(LEFT + 29, H - 34, f'RoleShuffle v{self.version}  |  仕様書')
            c.setStrokeColor(LINE)
            c.line(LEFT, BOTTOM - 12, W - RIGHT, BOTTOM - 12)
            c.setFont('JP', 7.4)
            c.drawString(LEFT, BOTTOM - 26, 'リリース時点の実装・初期設定に基づく')
            c.drawRightString(W - RIGHT, BOTTOM - 26, str(doc.page))
        c.restoreState()

    def afterFlowable(self, flowable):
        if isinstance(flowable, Paragraph) and flowable.style.name == 'chapter':
            title = flowable.getPlainText()
            if title == '目次':
                return
            key = getattr(flowable, '_bookmark', title)
            self.canv.bookmarkPage(key)
            self.canv.addOutlineEntry(title, key, 0, False)
            self.notify('TOCEntry', (0, title, self.page, key))


def build(args):
    register_fonts()
    sty = styles()
    release = Path(args.release_zip)
    with zipfile.ZipFile(release) as z:
        manifest = json.loads(z.read('manifest.json'))
        readme = z.read('README.md').decode('utf-8').replace('\r\n', '\n')
        icon = z.read('icon.png')
        dll_hash = hashlib.sha256(z.read('RoleShuffle.dll')).hexdigest().upper()
        assert z.testzip() is None
    version = manifest['version_number']
    assert version == '4.4.7', 'This edition documents the requested v4.4.7 release.'
    assert readme == git_text(args.source_ref, 'package/README.md')
    assert f'PluginVersion = "{version}"' in git_text(args.source_ref, 'StageRolesPlugin.cs')
    source_menu = git_text(args.source_ref, 'RoleMenu.cs')
    build_number = int(re.search(r'RoleUiBuildNumber = (\d+)', source_menu).group(1))
    snapshot = json.loads(Path(args.settings_json).read_text(encoding='utf-8-sig'))
    assert snapshot['source_revision'] == subprocess.check_output(['git', 'rev-parse', args.source_ref], cwd=ROOT).decode().strip()
    for file, digest in snapshot['source_files'].items():
        assert hashlib.sha256(git_text(args.source_ref, file).encode('utf-8')).hexdigest() == digest, f'Default-export source drift: {file}'
    defaults = snapshot['entries']
    assert len(defaults) == 291
    assert 'MaximumUpgradeLevel = 200;' in git_text(args.source_ref, 'RoleUpgradeScaling.cs')
    jp = readme.split('## 日本語', 1)[1]
    blocks = OrderedDict()
    for m in re.finditer(r'^(#{3,4}) (.+)\n', jp, re.M):
        next_m = re.search(r'^#{3,4} ', jp[m.end():], re.M)
        end = m.end() + next_m.start() if next_m else len(jp)
        blocks[m.group(2)] = jp[m.end():end].strip()
    roles = [[c.strip() for c in line.strip('|').split('|')] for line in blocks['役職一覧'].splitlines()
             if line.startswith('| ') and not line.startswith('| 役職')]
    assert len(roles) == 42
    setting_docs = {}
    for line in jp.splitlines():
        if line.startswith('| `'):
            cells = [c.strip() for c in line.strip('|').split('|')]
            if len(cells) >= 4:
                setting_docs[cells[0].strip('`')] = cells
    out = Path(args.output) if args.output else ROOT / 'output/pdf' / f'RoleShuffle_Specification_v{version}_JA.pdf'
    out.parent.mkdir(parents=True, exist_ok=True)
    story = []
    def p(text, style='body'):
        return Paragraph(rich(text), sty[style])
    def add(text, style='body'):
        story.append(p(text, style))
    def section(title, allow_table_split=False):
        if allow_table_split:
            story.append(CondPageBreak(110))
        story.append(Paragraph(glyph_markup(title), sty['table_section' if allow_table_split else 'section']))
    def chapter(title):
        story.append(PageBreak())
        para = Paragraph(glyph_markup(title), sty['chapter'])
        para._bookmark = 'chapter-' + title.split()[0]
        story.append(para)
    def table(rows, widths=None):
        widths = widths or [CW / len(rows[0])] * len(rows[0])
        cells = [[p(v, 'thead' if i == 0 else 'table') for v in row] for i, row in enumerate(rows)]
        t = Table(cells, colWidths=widths, repeatRows=1, hAlign='LEFT')
        t.setStyle(TableStyle([
            ('BACKGROUND', (0, 0), (-1, 0), NAVY), ('ROWBACKGROUNDS', (0, 1), (-1, -1), [colors.white, PALE]),
            ('VALIGN', (0, 0), (-1, -1), 'TOP'), ('LEFTPADDING', (0, 0), (-1, -1), 7),
            ('RIGHTPADDING', (0, 0), (-1, -1), 7), ('TOPPADDING', (0, 0), (-1, -1), 6),
            ('BOTTOMPADDING', (0, 0), (-1, -1), 6), ('LINEBELOW', (0, 0), (-1, 0), 1, TEAL),
            ('LINEBELOW', (0, 1), (-1, -1), .3, LINE),
        ]))
        story.extend([t, Spacer(1, 9)])
    def markdown_block(title):
        body = blocks[title]
        lines = body.splitlines()
        paragraph = []
        def flush():
            if paragraph:
                add(' '.join(paragraph))
                paragraph.clear()
        index = 0
        while index < len(lines):
            line = lines[index]
            if line.startswith('|'):
                flush()
                rows = []
                while index < len(lines) and lines[index].startswith('|'):
                    row = [c.strip() for c in lines[index].strip('|').split('|')]
                    if not all(re.fullmatch(r'[-: ]+', cell) for cell in row):
                        rows.append(row)
                    index += 1
                table(rows)
                continue
            if line.startswith('- '):
                flush()
                add('・' + line[2:], 'bullet')
            elif not line.strip():
                flush()
            else:
                paragraph.append(line)
            index += 1
        flush()

    story.extend([Spacer(1, 2), PageBreak()])
    add('目次', 'chapter')
    toc = TableOfContents()
    toc.levelStyles = [ParagraphStyle('toc', fontName='JP', fontSize=10, leading=24,
                                     leftIndent=0, firstLineIndent=0, textColor=INK)]
    story.extend([toc, Spacer(1, 19)])
    add('読み方', 'section')
    add('機能の概要は第1-5章、同期・保存と互換性は第6-7章、正確な設定キー・初期値・範囲は第8章にまとめています。隠し役職の具体的な効果も含む開発・運用向け資料です。')
    add('本文は配布用READMEを基に整理し、設定値は対応する実装の設定定義を優先しています。一般アップグレードの上限は200、Map Player Countは1です。')
    add(f'対象リリース: v{version} / UI build {build_number}。以後の開発バージョンの変更は含みません。')

    chapter('01  製品概要・導入条件')
    markdown_block('概要')
    table([['項目', '仕様'], ['ゲーム', 'R.E.P.O.'], ['パッケージ / DLL', 'RoleShuffle / RoleShuffle.dll'],
           ['プラグインGUID', 'REPOJP.RoleShuffle'], ['対象バージョン', f'{version} / build {build_number}'],
           ['役職 / 人数', '通常40役職＋隠し2役職 / 最大30人'], ['実装基盤', 'C# / netstandard2.1 / BepInEx / Harmony / Photon'],
           ['言語', '英語を初期値とする14言語'], ['必須依存', '\n'.join(manifest['dependencies'])]], [CW*.27,CW*.73])
    section('導入とマルチプレイ')
    markdown_block('導入方法')
    markdown_block('マルチプレイ')
    section('問い合わせ')
    markdown_block('お問い合わせ')

    chapter('02  進行・役職抽選・プリセット')
    story.append(StageFlow())
    add('ホストが役職と効果を決定し、参加者への付与・通知・表示データ配信を行います。ステージ終了が実際に成立した時点で役職を解除します。復活により失敗遷移が取り消された場合は、そのステージを継続します。')
    section('抽選規則')
    markdown_block('役職の抽選')
    section('ロールのON/OFFとプリセット')
    markdown_block('ロール選択とプリセット')
    preset_source = git_text(args.source_ref, 'RolePresets.cs')
    groups = re.findall(r'new RolePresetDefinition\(RolePreset\.(\w+),.*?(?=new RolePresetDefinition|\n    };)', preset_source, re.S)
    assert len(groups) == 5
    rows = [['プリセット', '有効な役職']]
    for name in groups:
        m = re.search(r'new RolePresetDefinition\(RolePreset\.'+name+r',(.+?)(?=new RolePresetDefinition|\n    };)',preset_source,re.S)
        selected = re.findall(r'StageRole\.(\w+)',m.group(1))
        if name == 'Standard': selected = ['全42役職']
        rows.append([name, '、'.join(selected)])
    table(rows,[CW*.22,CW*.78])
    section('隠し役職の抽選上の扱い')
    add('???1（Superbot）と???2（Disaster）のEnabledは通常役職と同じ設定画面で切り替えます。Weightの設定項目はありません。抽選内部では通常役職の設定Weightを1000倍した値、隠し役職は100を使用するため、Weight 100の通常役職に対する相対重みは1/1000です。履歴や候補制限も適用されるため、固定の当選確率を表す値ではありません。')
    add('Disasterは1人のセッションでは候補にならず、DangerとHardshipの両グループに属します。通常役職の候補がない場合は候補集合を空にし、隠し役職だけで不足を埋めません。')

    chapter('03  役職仕様')
    add('以下は初期設定での効果です。実際の値はホスト設定に従います。設定キーの全一覧は第8章を参照してください。通常の役職アップグレードは加算値ではなく、ステージ中の目標レベルです。')
    for index, row in enumerate(roles):
        name, effect, limitation, tuning = row
        tuning = tuning.replace('`0`～`100`','`0`～`200`')
        asset_name = name
        if name == '???1':
            asset_name = 'Superbot'
            name = '???1 / Superbot'
            effect = 'Bomber、Stinker、Werewolf、Jobless、Tuna、King、Diver、Imitator、Sniper、Brawlerを除く役職の強化と能力を併せ持ちます。RammerはTumble Attackのダメージのみを使用し、Tumble系の0固定はありません。Influencerは人数連動強化だけを使用し、物音の増加と定期TTSはありません。'
            limitation = '各能力の制約・設定は対応する役職に従います。未開示時の一般表示は???1です。通常役職の候補不足を補うためだけには選ばれません。'
            tuning = '???1.Enabled（初期true）。個別Weightは設定不可。'
        elif name == '???2':
            asset_name = 'Disaster'
            name = '???2 / Disaster'
            effect = 'Bomber・Stinker・Tunaの能力と制約を併せ持ち、移動跡に起動済みグレネードとウラン雲を残します。一定時間停止すると継続ダメージを受け、動くと停止時間と追加ダメージが止まります。'
            limitation = '失ったHPは戻りません。1人のセッションでは抽選されません。各能力は対応する役職の設定に従います。未開示時の一般表示は???2です。'
            tuning = '???2.Enabled（初期true）。個別Weightは設定不可。'
        if asset_name in ('Superbot','Disaster'):
            asset = ROOT/'Assets/role-emblems-semibot-v1/transparent/runtime'/f'{1001 if asset_name=="Superbot" else 1002}-{asset_name}.png'
        else:
            asset = ROOT/'Assets/role-emblems-semibot-v1/transparent/runtime'/f'{index+1:02d}-{asset_name}.png'
        right = [Paragraph(glyph_markup(f'{index+1:02d}  {name}'),sty['role']),p(effect),p('**制限・注意**  '+limitation,'small'),p('**調整項目**  '+tuning,'small')]
        card = Table([[Image(str(asset),width=55,height=55),right]],colWidths=[67,CW-67],hAlign='LEFT')
        card.setStyle(TableStyle([('VALIGN',(0,0),(-1,-1),'TOP'),('LINEABOVE',(0,0),(-1,0),.8,LINE),('TOPPADDING',(0,0),(-1,-1),12),('BOTTOMPADDING',(0,0),(-1,-1),10),('LEFTPADDING',(0,0),(-1,-1),0),('RIGHTPADDING',(0,0),(-1,-1),8)]))
        story.extend([KeepTogether([card]),Spacer(1,5)])

    chapter('04  Base Upgrade・手動調整・履歴')
    base_body = blocks['基礎アップグレード'].split('| キー',1)[0]
    for paragraph in base_body.split('\n\n'):
        if 'この表はすべて' not in paragraph: add(paragraph)
    section('合計値と役職の上書き')
    table([['値', '定義'],['設定値','現在のランレベルに対応する設定式の値'],['手動調整','セーブ別に保存する加減値。設定がONの間だけ有効'],['抽選値','トラック抽選で獲得した累積値'],['合計','clamp（設定値＋有効な手動調整＋抽選値, 0, 上限）'],['役職中','役職の指定があるアップグレードは役職目標値で置き換え。それ以外は合計を使用']], [CW*.22,CW*.78])
    add('上限は一般アップグレード200、Map Player Countは1。デフォルトの基礎値はHealthのみランレベル1から1、その他は0です。設定の1:1,5:3に手動+1を保存した場合、抽選分が0ならレベル5以降の合計は4です。')
    add('固定役職の変更は割当や状態の変化時に行います。InfluencerとBerserkerは条件を再評価し、役職付与時に記録したレベル未満へ下げません。RammerのLaunch・Tumble Climb・Tumble Wingsはステージ中0固定です。Throwは管理対象外です。')
    section('トラック抽選の初期値')
    table([['増減量','-1','0','+1','+2'],['相対重み','10','15','60','15']], [CW*.24]+[CW*.19]*4)
    add('対象ごとの初期Weightは、Stamina 40、Speed・Range・Crouch Rest 30、Health・Strength 20、その他の通常対象10、All Upgrades 1です。上限に近づくと通常対象の重みが低下し、上限では初期倍率0.25になります。All Upgradesはこの低下の対象外です。')
    section('手動調整の操作条件')
    table([['条件','動作'],['設定OFF','±ボタン・手動内訳・手動の案内を非表示。保存済み手動値を除外'],['設定ON＋ホスト＋保存可能＋ロビー/トラック/ショップ','±を操作可能。上限・下限側のボタンは操作不可'],['設定ON＋ステージ中','加減不可。設定値の閲覧は可能'],['参加者','ホストの値を表示。加減不可'],['別セーブ・シーンへの移動後の古い操作','保存識別子や現在の条件を再確認し、不正な変更を拒否']], [CW*.43,CW*.57])
    add('Base Upgradeのプリセット機能はありません。ロールのプリセットはBase Upgradeを変更しません。All Upgradesは表示言語によらず英語名を使用します。')

    chapter('05  Roles UI・HUD・プレイヤーツール')
    section('画面とホスト権限')
    table([['画面 / 操作','ホスト','導入済み参加者'],['Current Roles / Role Guide','閲覧','ホスト由来の内容を閲覧'],['Role Settings / ロールプリセット','変更可（次回以降の抽選へ反映）','閲覧のみ'],['Base Upgrade Settings','変更可（以後の抽選へ反映）','閲覧のみ'],['手動±調整','設定ONかつ許可シーンで可能','不可'],['HUD編集 / 言語 / レポート','自分の端末で利用','自分の端末で利用'],['DRAW HISTORY / 同期状態','閲覧・再取得','閲覧・再取得']], [CW*.33,CW*.335,CW*.335])
    markdown_block('通知とHUD')
    section('マウスホイール')
    add('1目盛りの移動量は本文3行分を基準とし、ロビーとステージ内で揃えます。履歴件数やコンテンツ全体の高さによって1目盛りの移動量を変えません。複数行で構成された項目では、移動する項目数と本文行数は一致しない場合があります。')
    section('ツール・言語・同期・不具合報告')
    markdown_block('プレイヤー向けツール')
    section('自分の役職を確認するコマンド')
    markdown_block('役職確認コマンド')

    chapter('06  同期・保存・実装の責務')
    add('ゲームプレイ効果はホスト側で決定します。RoleShuffleを導入した参加者には表示用データを配信し、未導入の参加者にはバニラのゲーム処理と通知を通じて効果を提供します。参加者のローカル設定でホストの共有値を再計算しません。')
    table([['担当','主要な責務 / 実装'],['起動・ライフサイクル','StageRolesPlugin / StageRoleController / LifecyclePatches: 初期化、ステージ開始・終了、参加状態の更新'],['役職抽選・能力','RoleAssignmentPlanner / RoleModels / 各RoleRuntime: 候補制限、履歴、役職の能力と効果'],['表示','RoleMenu / RoleHud / RoleGuideCatalog: 画面、説明、HUD、言語、エンブレム'],['設定・保存','StageRolesConfig / RoleSelectionSettings / BaseUpgradeSelectionSettings / GameSaveState'],['同期・ツール','RoleAssignmentSync / RoleGuideSync / BaseUpgradeSync / PlayerUtilitiesCore / PlayerUtilitiesRuntime']], [CW*.23,CW*.77])
    section('主な表示データ')
    table([['識別キー','用途'],['RS.RoleMap','役職割当一覧'],['RoleShuffleGuideV2 / RoleShuffleGuideEnabledV1','ホストの役職説明・表示対象'],['RS.BaseUpgrades','旧形式互換の共有Base Upgrade値'],['RS.BaseUpgrades.Detail / RS.BaseUpgrades.ManualState','手動内訳を含む詳細値とホストの手動設定状態'],['RS.BaseUpgradeSettingsV1','Base Upgrade抽選スイッチ'],['RS.BUD.History','直近の抽選履歴'],['RS.Sync.Status / RS.Sync.Request','同期状態の比較と表示データ再配信要求']], [CW*.47,CW*.53])
    add('詳細Base Upgradeと手動設定状態は、現在のホスト識別子を照合して受信します。互換形式を維持し、旧ホストのデータが不足する場合も、参加者に編集権限を与えません。同期状態は対象表示データの一致を示し、すべての通信やゲーム状態の完全一致を保証するものではありません。')
    section('保存先と寿命')
    table([['データ','保存先 / 寿命'],['MOD設定','BepInEx/config/REPOJP.RoleShuffle.cfg。ホスト設定とローカルUI設定を保持'],['手動調整','runStats: RoleShuffle.BaseUpgradeManual.<upgrade dictionary name>。セーブ別に保持'],['トラック抽選加算','runStats: RoleShuffle.BaseUpgradeBonus.<upgrade dictionary name>。当該ランの保存に従う'],['抽選履歴','ホストのセーブに直近50件。導入済み参加者へ表示用に共有'],['HUD/履歴テスト表示','ローカルの一時データ。シーン・部屋・セーブ変更等で解除'],['不具合レポート','BepInEx/RoleShuffleReports。コピー/開く操作の都度生成']], [CW*.27,CW*.73])
    add('ゲームオーバー後、古いセーブ名が残る状態では新しいランの識別子で編集可否を判定します。再開ロビーで最初に編集した時点でゲームの保存準備処理を通し、新しいランへ調整を保存します。古い画面操作を別セーブへ適用せず、保存失敗時は変更を戻します。')

    chapter('07  互換性・制限')
    markdown_block('互換性')
    section('ゲームプレイ上の制限')
    markdown_block('ゲームプレイ上の注意')
    add('本書の数値は初期設定です。導入済みの設定、ホストの変更、ゲーム本体や他MODの影響によって実際の挙動は変わります。設定項目は保存されますが、現在の役職や既に開始した抽選へ即時に全設定を適用する意味ではありません。')

    chapter('08  設定リファレンス')
    add('実装の設定定義から取得した全291項目です。表のキーはセクション名を含む完全名です。UI・HUDは端末ごとの設定、Testingはその端末のテストコマンドの有効化、それ以外はホストがゲーム内容を決定する設定です。')
    add('空欄は空文字列を表します。基礎値の設定はランレベル:値、Influencerは人数:レベル、Berserkerは残りHP%:レベルです。一般アップグレードのレベル範囲は0-200、Map Player Countは0-1です。詳細な適用規則は第4章を参照してください。')
    groupings = OrderedDict()
    system_sections = {'General','UI','Compatibility','Role Balance - Guarantees','Role Balance - Limits','Role Balance - Variety','Notifications','HUD','Testing'}
    for entry in defaults:
        group = '共通・UI・バランス' if entry['section'] in system_sections else 'Base Upgrade' if entry['section'].startswith('Base Upgrade') else '役職ごとの設定'
        groupings.setdefault(group,[]).append(entry)
    def doc_entry(entry):
        name, key, sec = entry['name'],entry['key'],entry['section']
        if name in setting_docs:
            effect = setting_docs[name][3]
        elif key == 'Enabled':
            effect = 'この役職を抽選候補に含める。' if sec not in ('General','Testing') else '役職抽選を有効化。' if sec=='General' else 'ローカルのテストコマンドを有効化。'
        elif key == 'Weight': effect = 'この役職の相対抽選重み。0で候補から除外。'
        elif name == 'UI.GuideLanguage': effect = 'メニュー言語。言語トグルと同じ値を保存。'
        elif name == 'HUD.FontSize': effect = '全体倍率を適用する前のHUD文字サイズ。'
        elif key.endswith('UpgradeScaling'):
            upgrade = key.removesuffix('UpgradeScaling')
            effect = f'{upgrade}の' + ('近くの仲間の人数に応じた目標レベル。' if sec=='Influencer' else '残りHP割合に応じた目標レベル。')
        else: effect = entry['description']
        if entry['choices']:
            value_range = ' / '.join(entry['choices'])
        elif entry['range']:
            value_range = entry['range']
        elif key.endswith('UpgradeLevels') and sec=='Base Upgrades':
            value_range = 'ランレベル:値\n条件1-999999 / 値0-' + ('1' if 'MapPlayerCount' in key else '200')
        elif key.endswith('UpgradeScaling'):
            value_range = ('人数:レベル' if sec=='Influencer' else 'HP%:レベル')+'\nレベル0-'+('1' if 'MapPlayerCount' in key else '200')
        elif name in setting_docs:
            value_range = setting_docs[name][2].replace('`0`～`100`','`0`～`200`')
        else: value_range = '文字列（説明の書式に従う）'
        return [f'`{name}`', entry['value'] or '空欄', value_range, effect]
    for group, entries in groupings.items():
        section(group, allow_table_split=True)
        rows = [['設定キー','初期値','範囲・形式','内容']]+[doc_entry(entry) for entry in entries]
        table(rows,[CW*.285,CW*.16,CW*.225,CW*.33])

    chapter('09  検証手順・参照資料')
    section('確認すべき受け入れ条件')
    table([['領域','確認内容'],['ホスト/参加者','未導入参加者に役職効果が届く。導入済み参加者は共有値を閲覧し、ホスト設定を書き換えられない'],['ライフサイクル','開始時の抽選、途中参加、再参加、終了時解除、復活による遷移中止'],['Base Upgrade','REPOConfigとの一致、各ON/OFF、上限・下限、手動OFF時の非表示と無効化、セーブ再開、ゲームオーバー後の新規ラン'],['画面','全14言語、6人の多バイト名、各HUD形式、現在の役職の自己先頭/説明展開'],['スクロール','ロビー/ステージで1目盛り3本文行。履歴件数0/1/10/50でも同じ移動基準'],['レポート','コピー/開く都度の生成、500件上限、マスク、本文非表示、外部サイト確認'],['互換性','Stage Fluxあり/なし、Elite Enemy Variantsあり/なし、ホスト変更と旧バージョンの表示データ']], [CW*.22,CW*.78])
    section('ローカル表示確認コマンド')
    add('Testing.Enabledを有効にすると、次の表示確認を行えます。サンプルはローカル表示専用で、実際の役職・保存値・共有履歴を変更しません。ゲーム内容を変更するコマンドには別途ホスト権限が必要です。')
    table([['コマンド','内容'],['/hudmultibyte または /hmb','自分を含む6人の多言語名でHUDを確認'],['/hmb 12','1-30人の範囲で表示人数を指定'],['/hmb reset','通常のHUD表示へ戻す'],['/drawhistory または /dh','50件の抽選サンプルを表示'],['/dh 10 または /dh 0','0-50件の範囲で表示件数を指定'],['/dh reset','実際の保存履歴へ戻す']], [CW*.44,CW*.56])
    section('仕様書の作成根拠')
    add('配布ZIPのREADMEとmanifestを起点に、該当リビジョンの設定定義・役職定義・同期・保存処理を照合しました。初期値の抽出はゲームを起動せず設定クラスを使用しています。これは実機の描画やマルチプレイ通信の検証結果ではありません。')
    sources = [
        ['機能・導入・役職','package/README.md / package/manifest.json'],
        ['初期値・型・範囲','StageRolesConfig.cs / RoleUpgradeScaling.cs / RoleLanguage.cs'],
        ['候補・能力・隠し役職','StageRole.cs / RoleModels.cs / RoleAssignmentPlanner.cs / RoleGuideCatalog.cs / RolePresets.cs'],
        ['UI・表示','RoleMenu*.cs / RoleHud*.cs / RoleGuide*.cs'],
        ['Base Upgrade・保存','BaseUpgradeSelectionSettings.cs / BaseUpgradeSaveAdjustments.cs / GameSaveState.cs / BaseUpgradeDrawRuntime.cs'],
        ['同期・レポート','RoleAssignmentSync.cs / RoleGuideSync.cs / BaseUpgradeSync.cs / BaseUpgradeSettingsSync.cs / PlayerUtilities*.cs'],
        ['検証の仕様','tools/RoleSettingsChecks / tools/ScrollChecks / tools/UtilityChecks / tools/ExpressionChecks'],
    ]
    table([['対象','参照元']]+sources,[CW*.28,CW*.72])
    section('対象リリースの識別')
    table([['項目','値'],['バージョン / UI build',f'{version} / {build_number}'],['ソースの参照',subprocess.check_output(['git','rev-parse',args.source_ref],cwd=ROOT).decode().strip()],['ZIP SHA-256',hashlib.sha256(release.read_bytes()).hexdigest().upper()],['DLL SHA-256',dll_hash]], [CW*.27,CW*.73])
    add('同バージョン内の開発経緯は本書に列挙せず、v4.4.7時点の最終的な仕様を記載しています。アイコンは現行の配布用デザインです。')
    doc = SpecDoc(out,version=version,build=build_number,icon=icon)
    doc.multiBuild(story)
    reader = PdfReader(out)
    text = '\n'.join(page.extract_text() or '' for page in reader.pages)
    assert 'v4.4.7' in text and 'v4.4.8' not in text
    assert all(role[0] in text for role in roles)
    compact_text = re.sub(r'\s+', '', text)
    assert all(re.sub(r'\s+', '', entry['name']) in compact_text for entry in defaults)
    assert '291' in text and 'Superbot' in text and 'Disaster' in text
    print(f'Created: {out}\nPages: {len(reader.pages)}\nSettings: {len(defaults)}\nRoles: {len(roles)}')


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source-ref',default='8eb5990')
    parser.add_argument('--release-zip',default=str(ROOT/'RoleShuffle.zip'))
    parser.add_argument('--settings-json',default=str(ROOT/'tools/specification/v4.4.7-defaults.json'))
    parser.add_argument('--output')
    build(parser.parse_args())
