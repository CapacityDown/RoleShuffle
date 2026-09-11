from __future__ import annotations

import html
import re
from pathlib import Path

from reportlab.lib.colors import HexColor, white
from reportlab.lib.enums import TA_CENTER, TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.units import mm
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.platypus import (
    CondPageBreak,
    Flowable,
    ListFlowable,
    ListItem,
    PageBreak,
    Paragraph,
    SimpleDocTemplate,
    Spacer,
    Table,
    TableStyle,
)

from build_role_list_pdf import ROLES


ROOT = Path(__file__).resolve().parents[1]
README = ROOT / "package" / "README.md"
OUTPUT = ROOT / "output" / "pdf" / "RoleShuffle_Full_Design_v4.1.0.pdf"
ICON = ROOT / "package" / "icon.png"
FONT_REGULAR = Path(r"C:\Windows\Fonts\BIZ-UDGothicR.ttc")
FONT_BOLD = Path(r"C:\Windows\Fonts\BIZ-UDGothicB.ttc")

PAGE_W, PAGE_H = A4
MARGIN_X = 18 * mm
TOP_MARGIN = 17 * mm
BOTTOM_MARGIN = 14 * mm
CONTENT_W = PAGE_W - MARGIN_X * 2

BG = HexColor("#071015")
PANEL = HexColor("#101A1F")
PANEL_DARK = HexColor("#0A1216")
INK = HexColor("#E7ECE7")
MUTED = HexColor("#B4BDB7")
GRID = HexColor("#304048")
CYAN = HexColor("#46C6E8")
MAGENTA = HexColor("#E34B9A")
ORANGE = HexColor("#E49A29")
GREEN = HexColor("#70A650")
YELLOW = HexColor("#CF9E35")
RED = HexColor("#B84143")

RISK_COLOR = {"low": GREEN, "medium": YELLOW, "high": RED}
RISK_LABEL = {"low": "低", "medium": "中", "high": "高"}


def register_fonts() -> None:
    pdfmetrics.registerFont(TTFont("BIZUD", str(FONT_REGULAR), subfontIndex=0))
    pdfmetrics.registerFont(TTFont("BIZUDB", str(FONT_BOLD), subfontIndex=0))


STYLES = {
    "body": ParagraphStyle(
        "body",
        fontName="BIZUDB",
        fontSize=9.1,
        leading=14,
        textColor=INK,
        wordWrap="CJK",
        spaceAfter=7,
    ),
    "small": ParagraphStyle(
        "small",
        fontName="BIZUDB",
        fontSize=7.5,
        leading=10.5,
        textColor=INK,
        wordWrap="CJK",
    ),
    "table": ParagraphStyle(
        "table",
        fontName="BIZUDB",
        fontSize=7.0,
        leading=9.8,
        textColor=INK,
        wordWrap="CJK",
    ),
    "table_header": ParagraphStyle(
        "table_header",
        fontName="BIZUDB",
        fontSize=7.1,
        leading=9.8,
        textColor=white,
        wordWrap="CJK",
        alignment=TA_LEFT,
    ),
    "toc_title": ParagraphStyle(
        "toc_title",
        fontName="BIZUDB",
        fontSize=11.5,
        leading=16,
        textColor=CYAN,
    ),
    "toc_body": ParagraphStyle(
        "toc_body",
        fontName="BIZUDB",
        fontSize=8.2,
        leading=12.5,
        textColor=INK,
        wordWrap="CJK",
    ),
}


def normalize(text: str) -> str:
    return (
        text.replace("\u2013", "-")
        .replace("\u2014", "-")
        .replace("\u2011", "-")
        .replace("\u2212", "-")
        .replace("：", ":")
    )


def inline_markup(text: str) -> str:
    text = normalize(text)
    placeholders: list[str] = []

    def stash_code(match: re.Match[str]) -> str:
        value = html.escape(match.group(1))
        placeholders.append(f"<font color='#46C6E8'>{value}</font>")
        return f"@@CODE{len(placeholders) - 1}@@"

    text = re.sub(r"`([^`]+)`", stash_code, text)
    text = html.escape(text)
    text = re.sub(
        r"\*\*(.+?)\*\*",
        r"<font color='#E49A29'>\1</font>",
        text,
    )
    for index, value in enumerate(placeholders):
        text = text.replace(f"@@CODE{index}@@", value)
    return text


class SectionBand(Flowable):
    def __init__(self, title: str, color=CYAN, height: float = 31):
        super().__init__()
        self.title = title
        self.color = color
        self.height = height
        self.width = CONTENT_W

    def draw(self) -> None:
        self.canv.setFillColor(PANEL)
        self.canv.setStrokeColor(self.color)
        self.canv.setLineWidth(1.3)
        self.canv.roundRect(0, 0, self.width, self.height, 6, fill=1, stroke=1)
        self.canv.setFillColor(self.color)
        self.canv.rect(0, 0, 5, self.height, fill=1, stroke=0)
        self.canv.setFont("BIZUDB", 14)
        self.canv.setFillColor(INK)
        self.canv.drawString(14, 9, self.title)


def paragraph(text: str, style: str = "body") -> Paragraph:
    return Paragraph(inline_markup(text), STYLES[style])


def bullet_list(items: list[str], color=CYAN) -> ListFlowable:
    return ListFlowable(
        [
            ListItem(paragraph(item), leftIndent=4)
            for item in items
        ],
        bulletType="bullet",
        start="circle",
        leftIndent=17,
        bulletFontName="BIZUDB",
        bulletFontSize=7,
        bulletColor=color,
        spaceAfter=8,
    )


def compact_bullet_list(items: list[str], color=CYAN) -> ListFlowable:
    return ListFlowable(
        [
            ListItem(paragraph(item, "small"), leftIndent=3)
            for item in items
        ],
        bulletType="bullet",
        start="circle",
        leftIndent=15,
        bulletFontName="BIZUDB",
        bulletFontSize=6,
        bulletColor=color,
        spaceAfter=0,
    )


def make_table(
    rows: list[list[str]],
    widths: list[float] | None = None,
    header_color=HexColor("#1B5968"),
    font_size: float = 7.0,
) -> Table:
    count = len(rows[0])
    if widths is None:
        if count == 2:
            widths = [40 * mm, CONTENT_W - 40 * mm]
        elif count == 3:
            widths = [32 * mm, 68 * mm, CONTENT_W - 100 * mm]
        elif count == 4:
            widths = [24 * mm, 31 * mm, 56 * mm, CONTENT_W - 111 * mm]
        elif count == 5:
            widths = [23 * mm, 19 * mm, 25 * mm, 38 * mm, CONTENT_W - 105 * mm]
        else:
            widths = [CONTENT_W / count] * count

    cell_style = ParagraphStyle(
        f"table_{font_size}",
        parent=STYLES["table"],
        fontSize=font_size,
        leading=font_size * 1.4,
    )
    formatted: list[list[Paragraph]] = []
    for row_index, row in enumerate(rows):
        style = STYLES["table_header"] if row_index == 0 else cell_style
        formatted.append([Paragraph(inline_markup(cell), style) for cell in row])

    table = Table(
        formatted,
        colWidths=widths,
        repeatRows=1,
        splitByRow=1,
        hAlign="LEFT",
    )
    commands = [
        ("BACKGROUND", (0, 0), (-1, 0), header_color),
        ("TEXTCOLOR", (0, 0), (-1, -1), INK),
        ("GRID", (0, 0), (-1, -1), 0.55, GRID),
        ("VALIGN", (0, 0), (-1, -1), "TOP"),
        ("LEFTPADDING", (0, 0), (-1, -1), 5),
        ("RIGHTPADDING", (0, 0), (-1, -1), 5),
        ("TOPPADDING", (0, 0), (-1, -1), 5),
        ("BOTTOMPADDING", (0, 0), (-1, -1), 5),
    ]
    for row_index in range(1, len(rows)):
        commands.append((
            "BACKGROUND",
            (0, row_index),
            (-1, row_index),
            PANEL_DARK if row_index % 2 == 0 else PANEL,
        ))
    table.setStyle(TableStyle(commands))
    return table


def chapter(title: str, color=CYAN) -> list:
    return [PageBreak(), SectionBand(title, color, height=36), Spacer(1, 10)]


def subheading(title: str, color=ORANGE) -> list:
    return [
        CondPageBreak(115),
        SectionBand(title, color, height=27),
        Spacer(1, 7),
    ]


def contents_page() -> list:
    groups = [
        ("01  基本方針", "目的 / 対象範囲 / 必須MOD / ホスト専用モデル"),
        ("02  システム構成", "コンポーネント / 権限 / データ / 同期境界"),
        ("03  ライフサイクル", "ステージ開始 / 抽選 / 実行 / 退出 / 終了"),
        ("04  役職・強化", "全23職業 / 基礎アップグレード / ショップ"),
        ("05  特殊効果", "危険 / 回復 / 復活 / 戦闘 / Valuable制御"),
        ("06  通知とUI", "チャット・TTS / HUD / Rolesメニュー / 同期更新"),
        ("07  互換性・安全", "StageFlux / バニラ参加者 / 制約 / 復旧"),
        ("08  設定・検証", "REPOConfig / 全設定表 / テスト観点 / リリース"),
    ]
    result: list = [
        SectionBand("CONTENTS / 収録内容", CYAN, height=36),
        Spacer(1, 15),
    ]
    for index, (title, body) in enumerate(groups):
        color = (CYAN, MAGENTA, ORANGE)[index % 3]
        card = Table(
            [[
                Paragraph(title, ParagraphStyle(
                    f"toc_title_{index}",
                    parent=STYLES["toc_title"],
                    textColor=color,
                )),
                Paragraph(body, STYLES["toc_body"]),
            ]],
            colWidths=[48 * mm, CONTENT_W - 48 * mm],
        )
        card.setStyle(TableStyle([
            ("BACKGROUND", (0, 0), (-1, -1), PANEL),
            ("BOX", (0, 0), (-1, -1), 1.0, color),
            ("LINEBEFORE", (0, 0), (0, 0), 4, color),
            ("VALIGN", (0, 0), (-1, -1), "MIDDLE"),
            ("LEFTPADDING", (0, 0), (-1, -1), 10),
            ("RIGHTPADDING", (0, 0), (-1, -1), 10),
            ("TOPPADDING", (0, 0), (-1, -1), 11),
            ("BOTTOMPADDING", (0, 0), (-1, -1), 11),
        ]))
        result.extend([card, Spacer(1, 7)])
    result.append(PageBreak())
    return result


def basic_design() -> list:
    topology = [
        ["参加形態", "必要な導入", "受け取る内容"],
        ["ホスト", "RoleShuffle / REPOConfig / MenuLib", "役職抽選、全ゲーム効果、設定、同期公開、通知を担当します。"],
        ["バニラ参加者", "導入不要", "役職効果、アップグレード、バニラチャット・TTS通知を受け取ります。専用HUDは表示しません。"],
        ["MOD導入参加者", "RoleShuffle / MenuLib", "バニラ参加者の内容に加え、同期されたHUDとRolesメニューを利用できます。"],
        ["シングルプレイ", "ホストと同じ", "同一の権限経路を使用し、Joblessだけランダム抽選から除外します。"],
    ]
    return [
        SectionBand("基本方針", CYAN, height=36),
        Spacer(1, 10),
        paragraph(
            "RoleShuffleは、プレイ可能なステージ開始時に各プレイヤーへ1つの職業を割り当て、"
            "バニラのアップグレードまたはゲーム効果によって職業ごとの専門性を作るホスト専用MODです。"
            "ゲーム状態の決定はホストへ集約し、参加者側の導入をゲームプレイ成立の条件にしません。"
        ),
        *subheading("設計目標", CYAN),
        bullet_list([
            "各職業は1つの明確なテーマへ特化し、役割名だけで効果を想像しやすくします。",
            "ゲーム効果にはバニラ実装を使用し、ホストだけの導入で参加者へ反映できる構成を優先します。",
            "役職の抽選、アップグレード目標値、危険効果、回復、復活、Valuable制御をREPOConfigから調整可能にします。",
            "ステージ終了時は役職固有状態を確実に解除し、管理対象アップグレードを基礎値へ戻します。",
            "StageFluxと同時導入しても、復活判定、終了処理、通知、内部オブジェクトが互いに干渉しないようにします。",
        ]),
        *subheading("対象範囲と非対象", MAGENTA),
        make_table([
            ["区分", "内容"],
            ["対象", "プレイ可能ステージの開始から実際のレベル遷移まで。役職、強化、効果、通知、HUD、退出処理を含みます。"],
            ["対象外", "ホスト移譲への継続対応、独自ネットワークプロトコル、バニラに存在しない攻撃・アップグレードの追加。"],
            ["途中参加", "ステージ開始時の割り当て対象には含まれません。新しい割り当ては次のステージ開始時に行います。"],
            ["ゲーム内説明", "ゲーム内には職業説明を表示せず、英語の職業名と一覧UIだけを表示します。"],
        ]),
        *subheading("必須MODと参加形態", ORANGE),
        make_table(topology, widths=[30 * mm, 45 * mm, CONTENT_W - 75 * mm]),
    ]


def architecture_design() -> list:
    components = [
        ["コンポーネント", "責務"],
        ["StageRolesPlugin", "設定、常駐Controller、HUD、Rolesメニューを生成し、Harmonyパッチとシーン変更監視を登録します。"],
        ["StageRoleController", "ホスト権限、ステージ状態、割り当て、役職別更新、復活、敵死亡、退出、終了処理を統括します。"],
        ["StageRolesConfig / RoleCatalog", "REPOConfigの値、全23職業、基礎アップグレード、役職別の目標アップグレードを定義します。"],
        ["UpgradeService", "Steam ID単位でバニラのアップグレード目標値を読み書きします。Throwは管理しません。"],
        ["RoleAssignmentSync", "役職一覧をPhoton Room Custom Propertiesへ公開し、導入クライアントのHUDとRolesメニューへ供給します。"],
        ["RoleHud / RoleMenu", "1列ページ切替HUDと、Escapeメニューから開く全員一覧をローカル表示します。"],
        ["RoleNotifier / Guard", "英語の役職名をバニラチャット・TTSで通知し、MOD通知に対する敵の音声反応だけを抑止します。"],
        ["VanillaRolePrefabResolver", "シーンやリソースからグレネード、魔法、オーブなどのバニラPrefabを解決します。"],
        ["役職Runtime", "Bomber、Medic、Mage、Gambler、Stinkerの継続状態と生成物を役職ごとに管理します。"],
        ["Lifecycle / Shop / Hunter / Engineer patches", "バニライベントを検出し、終了順序、ショップ、武器消費、Valuable効果抑止へ必要なフックを提供します。"],
    ]
    authority = [
        ["状態・処理", "ホスト", "導入参加者", "バニラ参加者"],
        ["役職抽選", "決定", "受信", "通知と効果のみ"],
        ["アップグレード・危険効果", "実行", "結果を受信", "結果を受信"],
        ["Room role map", "公開・更新・削除", "読取", "未使用"],
        ["HUD / Rolesページ", "ローカル表示", "ローカル表示", "なし"],
        ["設定", "ゲーム設定を管理", "HUD設定だけローカル", "なし"],
    ]
    data = [
        ["データ", "キー・識別", "寿命"],
        ["RoleAssignment", "Steam ID / PlayerAvatar / StageRole", "ステージ開始から終了または退出まで"],
        ["同期RoleMap", "`RS.RoleMap`", "ステージ中。退出時に再公開し、終了時に削除"],
        ["表示名", "UTF-8をBase64化", "RoleMapの1レコード内"],
        ["役職値", "StageRole enumの整数", "RoleMapの1レコード内"],
        ["役職固有状態", "Steam ID、Instance ID、Photon View ID", "各Runtimeが終了・退出時に破棄"],
        ["HUDプレビュー", "ローカルのみ1-30人", "テスト表示中。ネットワークへ公開しない"],
    ]
    return [
        *chapter("システム構成", MAGENTA),
        paragraph(
            "常駐プラグインは、ゲーム処理を担当するホスト側Controllerと、導入者だけが使用する表示層を分離します。"
            "同期データは役職名の表示に必要な最小情報だけとし、効果の正しさはホスト権限で保証します。"
        ),
        *subheading("コンポーネント責務", CYAN),
        make_table(components, widths=[43 * mm, CONTENT_W - 43 * mm]),
        *subheading("権限と同期境界", MAGENTA),
        make_table(authority, widths=[39 * mm, 39 * mm, 39 * mm, CONTENT_W - 117 * mm]),
        *subheading("主要データモデル", ORANGE),
        make_table(data, widths=[38 * mm, 48 * mm, CONTENT_W - 86 * mm]),
    ]


def lifecycle_design() -> list:
    phases = [
        ["フェーズ", "トリガー", "処理"],
        ["準備", "`LevelGenerator.GenerateDone` Postfix", "プレイ可能ステージとホスト権限を確認し、既存状態を終了して新しい世代番号を発行します。"],
        ["待機", "開始後0.75秒、その後0.25秒間隔", "UpgradeService、GameDirector、PlayerAvatar、Steam IDの準備を最大10秒待ちます。"],
        ["抽選", "準備完了", "有効かつWeightが正の職業からプレイヤーごとに重み付き抽選し、目標アップグレードを適用します。"],
        ["開始", "抽選完了", "役職Runtime、King Crown、RoleMap同期、通知を開始します。"],
        ["実行", "毎フレーム / 0.5秒周期", "役職効果を更新し、Photon参加者一覧から退出者を検出します。"],
        ["終了判定", "`RunManager.ChangeLevel`", "PrefixはPhoenixによる失敗遷移中断だけを判断します。実処理が走った場合だけPostfixで終了します。"],
        ["終了", "実レベル遷移 / シーン変更 / Plugin破棄", "生成物、回復オーブ、役職状態、同期RoleMapを解除し、アップグレードを基礎値へ戻します。"],
    ]
    assignment = [
        ["手順", "規則"],
        ["候補作成", "`Enabled = true`かつ`Weight > 0`の職業だけを使用します。1人プレイではJoblessを除外します。"],
        ["重み付き選択", "候補のWeight合計から整数抽選し、累積Weightへ到達した職業を選択します。"],
        ["重複制御", "UniqueRolesがONなら候補を一巡するまで選択済み職業を除外します。候補が空になると再構成します。"],
        ["King", "UniqueRolesの設定に関係なく一度選ばれたら候補から除外し、1ステージ最大1人にします。"],
        ["不足時", "King以外の再利用可能職業がなければ、その追加プレイヤーには基礎アップグレードだけを適用します。"],
    ]
    departure = [
        ["更新対象", "退出時の処理"],
        ["割り当て", "Photon PlayerListのUserIdに存在しないSteam IDを0.5秒周期で削除します。死亡・非表示だけでは削除しません。"],
        ["生成物", "退出したBomberのグレネード、Medicの追従オーブ、Stinkerの保留地点を破棄します。"],
        ["補助状態", "Rescuer、通知応答、King Crownなど、Steam IDに紐づく状態を解除します。"],
        ["表示", "RoleMapを再公開し、HUDと開いたままのRolesページが0.25秒周期で新しい一覧へ更新されます。"],
        ["安全策", "接続者IDが一時的に0件の場合は全削除せず、遷移中の誤判定を避けます。"],
    ]
    return [
        *chapter("ステージ・ライフサイクル", ORANGE),
        make_table(phases, widths=[25 * mm, 46 * mm, CONTENT_W - 71 * mm]),
        *subheading("役職抽選アルゴリズム", CYAN),
        make_table(assignment, widths=[33 * mm, CONTENT_W - 33 * mm]),
        *subheading("途中退出の整合性", MAGENTA),
        make_table(departure, widths=[35 * mm, CONTENT_W - 35 * mm]),
        *subheading("終了処理の不変条件", ORANGE),
        bullet_list([
            "元のChangeLevelが中断された場合、RoleShuffleはStageEndingを実行しません。",
            "役職固有の生成物と追跡状態は次のステージへ持ち越しません。",
            "役職が上書きしたアップグレードは設定された基礎値へ戻します。Healthを含む基礎値そのものはステージ外でも維持します。",
            "Room role mapとHUDプレビューを消去し、導入クライアントへ終了状態を伝えます。",
        ]),
    ]


def upgrade_design() -> list:
    upgrade_rows = [
        ["管理対象", "基礎値", "役職上書き"],
        ["Health", "1", "Tank 21"],
        ["Stamina", "0", "Runner 46"],
        ["Extra Jump", "0", "Jumper 10"],
        ["Speed", "0", "Runner 6"],
        ["Strength", "0", "Lifter 25"],
        ["Range", "0", "Climber 20"],
        ["Launch", "0", "Launcher 10"],
        ["Tumble Climb", "0", "Climber 50"],
        ["Tumble Wings", "0", "Flyer 10"],
        ["Crouch Rest", "0", "なし"],
        ["Map Player Count", "0", "Tracker 1固定"],
        ["Death Head Battery", "0", "Ghost 50"],
        ["Throw", "管理対象外", "なし"],
    ]
    shop_rows = [
        ["項目", "設計"],
        ["ShopUpgradeItemCount", "デフォルト0。ショップのアップグレード出現要求数を0-30で指定します。"],
        ["目的", "一時的な役職強化とショップ購入による強化が混在しないよう、デフォルトではアップグレード商品を除外します。"],
        ["基礎値", "ショップ在庫とは独立してSteam ID単位で適用します。"],
        ["更新頻度", "役職付与、役職変更、ステージ遷移など必要な時点だけ更新し、常時監視しません。"],
    ]
    return [
        *chapter("アップグレードとショップ", CYAN),
        paragraph(
            "基礎アップグレードは、役職が専門化しない項目へ常に使う絶対目標値です。"
            "役職用設定は加算値ではなく、該当ステージ中だけ基礎値を置き換える絶対目標値です。"
            "役職終了後は基礎値へ戻します。"
        ),
        make_table(upgrade_rows, widths=[54 * mm, 35 * mm, CONTENT_W - 89 * mm]),
        *subheading("適用規則", MAGENTA),
        bullet_list([
            "TankだけはHealthを21へ置き換えます。ほかの全職業はデフォルトのBase Health 1を維持します。",
            "Trackerの役職値はMap Player Count 1固定で、役職用の強度設定を持ちません。",
            "各アップグレードは0-100、Base Map Player Countだけ0-1で設定します。",
            "Throwアップグレードには読み書きを行いません。",
            "役職が無効でも基礎アップグレードは有効です。",
        ]),
        *subheading("ショップ制御", ORANGE),
        make_table(shop_rows, widths=[46 * mm, CONTENT_W - 46 * mm]),
    ]


def role_catalog() -> list:
    def build_role_table(start: int, end: int) -> Table:
        selected = ROLES[start:end]
        rows = [["No.", "Role", "危険度", "専門内容", "デフォルト・主な制限"]]
        for index, role in enumerate(selected, start=start + 1):
            name, subtitle, _, risk, description, settings = role
            rows.append([
                f"{index:02d}",
                name,
                RISK_LABEL[risk],
                f"{subtitle}: {description}",
                settings,
            ])

        table = make_table(
            rows,
            widths=[11 * mm, 25 * mm, 14 * mm, 61 * mm, CONTENT_W - 111 * mm],
            font_size=7.0,
        )
        table.setStyle(TableStyle([
            (
                "LINEBEFORE",
                (0, row_index),
                (0, row_index),
                3,
                RISK_COLOR[role[3]],
            )
            for row_index, role in enumerate(selected, start=1)
        ]))
        return table

    return [
        *chapter("全23職業カタログ", MAGENTA),
        paragraph(
            "危険度はRoleShuffle自身が生む直接的な不利益を基準にします。"
            "低は直接的不利益なし、中は抽選による資産変動、高はダメージまたは実体ハザードを発生させる職業です。"
        ),
        build_role_table(0, 12),
        PageBreak(),
        SectionBand("全23職業カタログ（続き）", MAGENTA, height=36),
        Spacer(1, 10),
        build_role_table(12, len(ROLES)),
    ]


def special_role_design() -> list:
    hazard = [
        ["Role", "発動・判定", "安全・制限"],
        ["Bomber", "8m移動ごとに4種の起動済みグレネードから1個を設置。各Bomber最大30親グレネード。", "トラック内OFF。自動生成品は現在Bomberの全員だけが保持可能。バニラ参加者側では拒否まで一瞬保持表示される場合があります。"],
        ["Jobless", "トラック外で0.1秒ごとに1ダメージ。", "死亡可能。1人プレイでは抽選しません。"],
        ["Tuna", "ステージ開始から5秒猶予後、3秒停止すると0.1秒ごとに1ダメージ。", "移動で停止カウントと継続ダメージを即時リセット。受けたHPは戻しません。"],
        ["Mage", "完全一致の英語キーワードで5種のバニラ魔法。全魔法で5秒の共通間隔。", "HPがコスト以下なら不発。成功後だけ10/10/15/30/50HPを消費し、魔法による自死を防ぎます。"],
        ["Stinker", "移動経路を1mごとに記録し、ウラン食器破壊時の雲を順次発生。", "最新地点だけ本人から1m離れるまで待機。過去地点は順番に処理。戻れば本人も被害を受ける可能性があります。"],
    ]
    support = [
        ["Role", "対象", "デフォルト処理"],
        ["Medic", "本人以外の5m以内の生存仲間", "2秒ごとに5HP。Medic1人につき実回復量合計150HPで停止。"],
        ["Phoenix", "本人", "死亡から最低2秒後に1回だけ復活。25HP、最大HP上限。"],
        ["Rescuer", "3m以内で最も近いDeath Head付きの死亡仲間", "死亡から2秒後、1ステージ最大2回、25HPで復活。本人は対象外。"],
        ["Vampire", "本人", "10m以内の敵死亡時、Danger Level 1/2/3に応じて5/10/50HP回復。"],
        ["Musician", "本人を含む10m以内の生存プレイヤー", "楽器の各ノートRPCを検出し、1音ごとに5HP回復。"],
    ]
    economy = [
        ["Role", "実装ポイント", "制約"],
        ["King", "既存Crown所有者を保存し、Kingへ同期付与。役職終了時に以前の所有者へ復元。", "1ステージ最大1人。"],
        ["Gambler", "効果が使用可能なとき、最初に直接つかんだValuableの現在価格を50%で2倍、外れでは破壊。", "傷ついている場合は低下後の現在価格を基準にします。使用済みの効果は納品完了ごとに回復します。"],
        ["Hunter", "武器消費を通常の75%。最後に敵へダメージを与えたHunterを追跡し、オーブを抽選。", "0.5%で10個、それ以外は10%で2倍。保持・装備中の武器だけ対象。"],
        ["Engineer", "効果付きValuableの罠、Update/FixedUpdate、専用発動経路を抑止し、発動用grab情報をホスト内だけに保持。", "保持中と離してから0.25秒間リセット。未導入参加者のgrabbedLocalだけで動く表示は残る可能性があります。"],
    ]
    prefab = [
        ["対象", "バニラ利用方針"],
        ["グレネード", "Explosive / Stun / Shockwave / Duct TapedのPrefabを解決し、Photon経由で生成します。"],
        ["魔法", "Star Wand / Zero Gravity Staff / Roll Staff / Void Staff / Wizard Staff beamの既存発射・衝突効果を利用します。"],
        ["回復オーブ", "追従表示に使いますが、物理干渉を無効化してプレイヤーの移動・接地へ影響させません。"],
        ["Stinker雲", "ウラン食器破壊時の既存エフェクトを使用し、発生順と本人からの距離だけをRoleShuffleが制御します。"],
    ]
    return [
        *chapter("特殊役職の実行設計", ORANGE),
        *subheading("危険・継続効果", RED),
        make_table(hazard, widths=[25 * mm, 70 * mm, CONTENT_W - 95 * mm], font_size=6.8),
        *subheading("回復・復活", GREEN),
        make_table(support, widths=[25 * mm, 63 * mm, CONTENT_W - 88 * mm], font_size=6.8),
        *subheading("戦闘・経済・Valuable", YELLOW),
        make_table(economy, widths=[25 * mm, 75 * mm, CONTENT_W - 100 * mm], font_size=6.8),
        *subheading("バニラPrefabと内部オブジェクト", CYAN),
        make_table(prefab, widths=[34 * mm, CONTENT_W - 34 * mm]),
    ]


def ui_notification_design() -> list:
    notification = [
        ["段階", "仕様"],
        ["通常遅延", "ステージ役職割り当て完了後、デフォルト2.5秒待って通知します。"],
        ["StageFlux併用", "最低5秒待ち、StageFluxのTTS終了後さらに1.5秒の無音を確認します。"],
        ["通知内容", "プレイヤー本人が英語の職業名をバニラチャット・TTSで発言します。職業説明は表示しません。"],
        ["敵反応", "RoleShuffleが生成すると予告した通知だけVoice Chat更新時に抑止し、通常のマイクやワールド音を変更しません。"],
    ]
    hud = [
        ["要素", "デフォルトと動作"],
        ["配置", "画面左下、左揃え、70%。Anchor、Alignment、Offset、Scaleをローカル設定できます。"],
        ["形式", "1列。自分を各ページへ固定し、最大8人（自分+ほか7人）を同時表示します。"],
        ["切替", "5秒ごとにページを送り、0.2秒でフェードします。2列表示は使用しません。"],
        ["人数", "PlayersPerPageは2-20。ページ切替によって約20人規模のセッションを想定します。"],
        ["表示停止", "Rolesページが開いている間はHUDのページ送りを止め、重複した視覚更新を避けます。"],
        ["同期更新", "RoleMapを0.25秒周期で読み、署名が変化した場合だけ表示内容を再構築します。"],
    ]
    menu = [
        ["要素", "仕様"],
        ["ROLESボタン", "Escapeメニュー全体の右下へ固定します。"],
        ["Rolesページ", "同期中の全プレイヤーを1列のスクロール一覧で表示し、自分の行を強調します。"],
        ["ライブ更新", "ページを開いたままでも0.25秒周期でRoleMapを確認し、退出者の行を非表示にして再配置します。"],
        ["未割り当て", "RoleMapが空なら英語の未割り当てメッセージを表示します。"],
    ]
    return [
        *chapter("通知・HUD・Rolesメニュー", CYAN),
        *subheading("通知シーケンス", ORANGE),
        make_table(notification, widths=[40 * mm, CONTENT_W - 40 * mm]),
        *subheading("HUD", MAGENTA),
        make_table(hud, widths=[34 * mm, CONTENT_W - 34 * mm]),
        *subheading("Escapeメニュー", CYAN),
        make_table(menu, widths=[34 * mm, CONTENT_W - 34 * mm]),
        *subheading("同期レコード", ORANGE),
        paragraph(
            "RoleMapは、各プレイヤーを`SteamId,RoleNumber,Base64(PlayerName)`の形式で連結します。"
            "表示名の区切り文字衝突をBase64で避け、不正なレコード、未定義のRole番号、デコード失敗を読取側で無視または空名へフォールバックします。"
        ),
    ]


def compatibility_safety() -> list:
    stageflux = [
        ["競合点", "RoleShuffleの設計"],
        ["ChangeLevel Prefix", "`HarmonyAfter(StageFluxGuid)`でStageFluxのSecond Chanceを先に判定します。RoleShuffle PrefixはPhoenixによる中断判定だけを行います。"],
        ["StageEnding", "元のChangeLevelが実行された場合だけPostfixの`__runOriginal`を確認して終了します。どちらかの復活が遷移を止めた場合は役職を維持します。"],
        ["通知", "StageFlux用の待機時間とTTS完了後の無音時間を使い、通知の重なりを避けます。"],
        ["Orb", "内部オーブをStageFlux互換の非干渉オブジェクトとして扱い、移動・接地・物理判定へ影響させません。"],
    ]
    safeguards = [
        ["領域", "安全策 / フォールバック"],
        ["権限", "ゲーム状態変更前にホストまたはシングルプレイ権限を確認します。"],
        ["ID", "Steam IDが空のプレイヤーは割り当てず、準備時間内にIDが揃うまで待機します。"],
        ["Prefab", "解決できないPrefabや同期対象はログへ記録し、その効果だけをスキップします。"],
        ["魔法HP", "HP不足、死亡状態、クールダウン中、発射失敗時はHPを消費しません。"],
        ["復活", "最大HPで復活HPを上限化し、座標はDeath Headと復活後PlayerAvatarの状態に沿って復元します。"],
        ["生成物", "ステージ終了、役職変更、退出、Plugin破棄時に追跡中の生成物を停止または削除します。"],
        ["例外", "役職別処理は失敗箇所をログへ残し、可能な限りバニラ処理またはほかの役職処理を継続します。"],
    ]
    limitations = [
        ["制約", "内容"],
        ["ホスト移譲", "設計対象外です。現在のホストが退出した後の役職状態継続は保証しません。"],
        ["途中参加", "進行中ステージでは新しい役職を割り当てません。次ステージから抽選対象になります。"],
        ["バニラUI", "未導入参加者にはRoleShuffle専用HUDとRolesページを表示できません。"],
        ["クライアント演出", "ホストが拒否・抑止する直前に、未導入参加者側で保持やValuable効果が一瞬表示される場合があります。"],
        ["実体ハザード", "Bomber、Mage、Stinkerはバニラの実体効果を生成するため、プレイヤー、敵、Valuableへ本来の影響を与えます。"],
        ["全役職無効", "EnabledまたはWeightにより候補が0件なら、役職を割り当てず基礎アップグレードだけを適用します。"],
    ]
    return [
        *chapter("互換性・安全設計・制約", MAGENTA),
        *subheading("StageFlux互換性", CYAN),
        make_table(stageflux, widths=[40 * mm, CONTENT_W - 40 * mm]),
        *subheading("安全策", GREEN),
        make_table(safeguards, widths=[34 * mm, CONTENT_W - 34 * mm]),
        *subheading("既知の制約", ORANGE),
        make_table(limitations, widths=[36 * mm, CONTENT_W - 36 * mm]),
    ]


def parse_settings() -> list:
    source = README.read_text(encoding="utf-8")
    japanese = source.split("## 日本語", 1)[1]
    settings = japanese.split("### 設定", 1)[1].split("### 通知とHUD", 1)[0]
    lines = settings.splitlines()
    story: list = [
        *chapter("REPOConfig設定一覧", ORANGE),
        paragraph(
            "以下はv4.1.0の公開設定です。ホスト制御の値はセッション全体へ、HUDのローカル設定は導入者本人だけへ反映します。"
        ),
    ]
    paragraph_buffer: list[str] = []
    index = 0

    def flush() -> None:
        if paragraph_buffer:
            story.append(paragraph(" ".join(paragraph_buffer)))
            paragraph_buffer.clear()

    while index < len(lines):
        stripped = lines[index].strip()
        if stripped.startswith("#### "):
            flush()
            story.extend(subheading(stripped[5:].strip(), CYAN))
            index += 1
            continue
        if stripped.startswith("|"):
            flush()
            table_lines: list[str] = []
            while index < len(lines) and lines[index].strip().startswith("|"):
                table_lines.append(lines[index].strip())
                index += 1
            rows = [
                [cell.strip() for cell in row.strip("|").split("|")]
                for row in table_lines
            ]
            rows = [
                row for row in rows
                if not all(re.fullmatch(r":?-{3,}:?", cell) for cell in row)
            ]
            if len(rows[0]) == 5:
                widths = [49 * mm, 18 * mm, 31 * mm, CONTENT_W - 116 * mm, 18 * mm]
            else:
                widths = None
            story.append(make_table(rows, widths=widths, font_size=6.35))
            story.append(Spacer(1, 9))
            continue
        if stripped.startswith("- "):
            flush()
            items: list[str] = []
            while index < len(lines) and lines[index].strip().startswith("- "):
                items.append(lines[index].strip()[2:])
                index += 1
            story.append(bullet_list(items))
            continue
        if not stripped:
            flush()
            index += 1
            continue
        paragraph_buffer.append(stripped)
        index += 1
    flush()
    return story


def quality_and_release() -> list:
    tests = [
        ["領域", "確認観点"],
        ["ビルド", "Release / netstandard2.1、警告0、エラー0。参照DLLとMenuLib APIの互換性を確認します。"],
        ["抽選", "全役職ON/OFF、Weight 0、UniqueRoles、23人超、1人プレイ、King一意性を確認します。"],
        ["アップグレード", "基礎値、役職上書き、役職変更、ステージ終了、Throw非変更、Tracker固定値を確認します。"],
        ["ライフサイクル", "成功遷移、全滅、Phoenix、StageFlux Second Chance、シーン変更、退出を確認します。"],
        ["マルチプレイ", "ホストのみ、MOD導入参加者、バニラ参加者、20人HUD、開いたRolesページの退出更新を確認します。"],
        ["役職効果", "生成物、回復上限、復活HP、HP不足魔法、Hunterドロップ、Engineer対象Valuableを個別確認します。"],
        ["ログ", "例外、Prefab未解決、非公開メンバー直接参照、Harmonyパッチ失敗がないことを確認します。"],
        ["成果物", "DLL、manifest、README、CHANGELOG、iconをZIP直下へ置き、DLLハッシュとDefaultプロファイルを照合します。"],
    ]
    release = [
        ["項目", "値"],
        ["MOD名", "RoleShuffle"],
        ["Plugin GUID", "REPOJP.RoleShuffle"],
        ["DLL", "RoleShuffle.dll"],
        ["バージョン", "4.1.0"],
        ["対象", "R.E.P.O. / BepInEx 5 / netstandard2.1"],
        ["必須依存", "BepInExPack 5.4.2305+ / REPOConfig 1.2.6+ / MenuLib 2.5.2+"],
        ["任意互換", "StageFlux"],
        ["公開ZIP", "CHANGELOG.md / icon.png / manifest.json / README.md / RoleShuffle.dll"],
    ]
    return [
        CondPageBreak(290),
        SectionBand("検証・運用・リリース", CYAN, height=36),
        Spacer(1, 10),
        *subheading("検証マトリクス", MAGENTA),
        make_table(tests, widths=[35 * mm, CONTENT_W - 35 * mm]),
        *subheading("リリース構成", ORANGE),
        make_table(release, widths=[42 * mm, CONTENT_W - 42 * mm]),
        Spacer(1, 8),
        SectionBand("ログ方針", CYAN, height=27),
        Spacer(1, 7),
        compact_bullet_list([
            "割り当て、King、復活、退出削除など重要な状態遷移はInfoへ記録します。",
            "効果を安全にスキップできる解決失敗はDebug、復活や割り当ての実行失敗はWarningへ記録します。",
            "Harmony登録に失敗した場合はErrorを記録し、自身のパッチを解除してControllerを停止します。",
            "プレイヤー名ではなくSteam IDを内部キーとして使用し、同名プレイヤーによる状態衝突を避けます。",
        ]),
    ]


def draw_page_frame(c, doc) -> None:
    c.saveState()
    c.setFillColor(BG)
    c.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    c.setFillColor(CYAN)
    c.rect(0, PAGE_H - 5, PAGE_W * 0.42, 5, fill=1, stroke=0)
    c.setFillColor(MAGENTA)
    c.rect(PAGE_W * 0.42, PAGE_H - 5, PAGE_W * 0.33, 5, fill=1, stroke=0)
    c.setFillColor(ORANGE)
    c.rect(PAGE_W * 0.75, PAGE_H - 5, PAGE_W * 0.25, 5, fill=1, stroke=0)

    if doc.page == 1:
        draw_cover(c)
        c.restoreState()
        return

    c.setStrokeColor(GRID)
    c.setLineWidth(0.5)
    c.line(MARGIN_X, 25, PAGE_W - MARGIN_X, 25)
    c.setFont("BIZUDB", 7)
    c.setFillColor(MUTED)
    c.drawString(MARGIN_X, 12, "ROLE SHUFFLE v4.1.0  /  FULL DESIGN")
    c.drawRightString(PAGE_W - MARGIN_X, 12, f"{doc.page:02d}")
    c.restoreState()


def draw_cover(c) -> None:
    c.setFillColor(CYAN)
    c.setFont("BIZUDB", 12)
    c.drawString(MARGIN_X, PAGE_H - 64, "HOST-ONLY RANDOM PLAYER ROLE MOD")
    c.setFillColor(white)
    c.setFont("BIZUDB", 34)
    c.drawString(MARGIN_X, PAGE_H - 120, "ROLE SHUFFLE")
    c.setFillColor(INK)
    c.setFont("BIZUDB", 23)
    c.drawString(MARGIN_X, PAGE_H - 160, "全体設計書")
    c.setFillColor(MUTED)
    c.setFont("BIZUDB", 10)
    c.drawString(MARGIN_X, PAGE_H - 187, "v4.1.0  /  2026-08-05  /  日本語版")

    icon_size = 190
    icon_x = (PAGE_W - icon_size) / 2
    icon_y = 405
    c.setFillColor(PANEL)
    c.setStrokeColor(CYAN)
    c.setLineWidth(2.2)
    c.roundRect(
        icon_x - 8,
        icon_y - 8,
        icon_size + 16,
        icon_size + 16,
        12,
        fill=1,
        stroke=1,
    )
    c.drawImage(
        ImageReader(str(ICON)),
        icon_x,
        icon_y,
        icon_size,
        icon_size,
        preserveAspectRatio=True,
        mask="auto",
    )
    c.setFont("BIZUDB", 9)
    c.setFillColor(CYAN)
    c.drawCentredString(PAGE_W / 2, 377, "MOD本体アイコン")

    c.setFillColor(PANEL)
    c.roundRect(MARGIN_X, 103, CONTENT_W, 205, 8, fill=1, stroke=0)
    c.setFont("BIZUDB", 9)
    c.setFillColor(INK)
    lines = [
        "Thunderstore名: RoleShuffle",
        "プラグイン: RoleShuffle.dll",
        "バージョン: 4.1.0",
        "ホスト専用ゲーム処理 / REPOConfig・MenuLib対応",
        "",
        "全23職業 / 抽選・基礎アップグレード・ショップ制御",
        "危険・回復・復活・戦闘・経済・Valuable効果",
        "役職同期 / 通知 / HUD・Rolesメニュー / 途中退出更新",
        "StageFlux互換性 / 安全設計 / 全公開設定 / 検証項目",
    ]
    for index, line in enumerate(lines):
        c.drawString(MARGIN_X + 15, 286 - index * 18, line)


def build() -> None:
    register_fonts()
    OUTPUT.parent.mkdir(parents=True, exist_ok=True)
    doc = SimpleDocTemplate(
        str(OUTPUT),
        pagesize=A4,
        leftMargin=MARGIN_X,
        rightMargin=MARGIN_X,
        topMargin=TOP_MARGIN,
        bottomMargin=BOTTOM_MARGIN,
        title="RoleShuffle v4.1.0 - Full Design",
        author="RoleShuffle",
        subject="Architecture, lifecycle, roles, configuration, UI, compatibility, and validation",
        pageCompression=1,
    )
    story: list = [
        Spacer(1, PAGE_H - TOP_MARGIN - BOTTOM_MARGIN - 28),
        PageBreak(),
        *contents_page(),
        *basic_design(),
        *architecture_design(),
        *lifecycle_design(),
        *upgrade_design(),
        *role_catalog(),
        *special_role_design(),
        *ui_notification_design(),
        *compatibility_safety(),
        *parse_settings(),
        *quality_and_release(),
    ]
    doc.build(story, onFirstPage=draw_page_frame, onLaterPages=draw_page_frame)
    print(OUTPUT)


if __name__ == "__main__":
    build()
