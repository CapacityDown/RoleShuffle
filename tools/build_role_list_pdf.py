from __future__ import annotations

from pathlib import Path
from typing import Iterable

from reportlab.lib.colors import Color, HexColor, white
from reportlab.lib.enums import TA_LEFT
from reportlab.lib.pagesizes import A4
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.pdfbase import pdfmetrics
from reportlab.pdfbase.ttfonts import TTFont
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph


ROOT = Path(__file__).resolve().parents[1]
OUTPUT_PUBLIC = (
    ROOT / "output" / "pdf" /
    "RoleShuffle_Role_List_v4.4.0_Public.pdf")
OUTPUT_INTERNAL = (
    ROOT / "output" / "pdf" /
    "RoleShuffle_Role_List_v4.4.0_Internal.pdf")
ICON_PATH = ROOT / "package" / "icon.png"
FONT_REGULAR_PATH = Path(r"C:\Windows\Fonts\BIZ-UDGothicR.ttc")
FONT_BOLD_PATH = Path(r"C:\Windows\Fonts\BIZ-UDGothicB.ttc")

PAGE_W, PAGE_H = A4
MARGIN = 28
CYAN = HexColor("#46C6E8")
MAGENTA = HexColor("#E34B9A")
ORANGE = HexColor("#E49A29")
INK = HexColor("#E4E8E2")
MUTED = HexColor("#AAB1AC")
BACKGROUND = HexColor("#081015")
CARD = HexColor("#111A1F")
CARD_INNER = HexColor("#0A1115")

RISK = {
    "low": ("低", HexColor("#70A650")),
    "medium": ("中", HexColor("#CF9E35")),
    "high": ("高", HexColor("#B84143")),
}

ROLE_WEIGHTS = {
    "Ghost": 80,
    "Bomber": 80,
    "Jobless": 20,
    "Rescuer": 80,
    "Tuna": 50,
    "Musician": 60,
    "Hunter": 80,
    "Stinker": 80,
    "Engineer": 70,
    "Electrician": 80,
    "Rider": 60,
    "Werewolf": 40,
    "Bodyguard": 80,
}

ROLES = [
    ("Tank", "耐久特化", "アップグレード", "low",
     "ステージ中、バニラのHealthアップグレードを21レベル付与します。",
     "Health 21へ置換 / 設定範囲 0-100"),
    ("Runner", "移動特化", "アップグレード", "low",
     "ステージ中、SpeedとStaminaのバニラアップグレードを同時に付与します。",
     "Base Health +1 / Speed 6・Stamina 46へ置換 / 各0-100"),
    ("Jumper", "跳躍特化", "アップグレード", "low",
     "ステージ中、バニラのExtra Jumpアップグレードを10レベル付与します。",
     "Base Health +1 / Extra Jump 10へ置換 / 設定範囲 0-100"),
    ("Lifter", "運搬特化", "アップグレード", "low",
     "ステージ中、バニラのStrengthアップグレードを25レベル付与します。",
     "Base Health +1 / Strength 25へ置換 / 設定範囲 0-100"),
    ("Launcher", "射出特化", "アップグレード", "low",
     "ステージ中、バニラのLaunchアップグレードを10レベル付与します。",
     "Base Health +1 / Launch 10へ置換 / 設定範囲 0-100"),
    ("Climber", "登攀・射程特化", "アップグレード", "low",
     "Tumble ClimbとRangeのバニラアップグレードを同時に付与します。",
     "Base Health +1 / Tumble Climb 50・Range 20へ置換 / 各0-100"),
    ("Flyer", "飛行特化", "アップグレード", "low",
     "ステージ中、バニラのTumble Wingsアップグレードを10レベル付与します。",
     "Base Health +1 / Tumble Wings 10へ置換 / 設定範囲 0-100"),
    ("Tracker", "索敵特化", "アップグレード", "low",
     "ステージ中、Healthを3、Map Player Countを1へ設定します。",
     "Health 3へ置換 / Map Player Count 1へ置換 / Map Player Countは固定"),
    ("Ghost", "Death Head特化", "アップグレード", "low",
     "ステージ中、バニラのDeath Head Batteryアップグレードを50レベル付与します。",
     "Base Health +1 / Death Head Battery 50へ置換 / 設定範囲 0-100"),
    ("Bomber", "グレネード設置", "危険効果", "high",
     "8m移動するごとに、4種類の起動済みグレネードから1個をランダム設置します。",
     "Base Health +1 / 4種から抽選 / 同時最大30個 / トラック内OFF"),
    ("Medic", "周囲回復", "回復・支援", "low",
     "5m以内の生存している仲間を2秒ごとに5HP回復します。本人は対象外です。",
     "Base Health +1 / 5HP / 2秒 / 5m / 1ステージ合計150HPまで"),
    ("Phoenix", "自己復活", "復活", "low",
     "1ステージにつき1回だけ自分自身を復活させます。",
     "Base Health +1 / 復活後25HP（1-1000・最大HP上限）/ 失敗時猶予5秒"),
    ("Jobless", "トラック外ダメージ", "危険効果", "high",
     "トラック外にいる間、0.1秒ごとに1ダメージを受けます。死亡する可能性があります。",
     "Base Health +1 / 1ダメージ / 0.1秒 / 1人プレイ時は抽選対象外"),
    ("Rescuer", "仲間の復活", "復活・支援", "low",
     "3m以内で最も近い死亡中の仲間を復活させます。",
     "Base Health +1 / 復活後25HP（1-1000・最大HP上限）/ 最大2回 / 3m"),
    ("Vampire", "敵死亡時回復", "回復", "low",
     "10m以内で敵が死亡すると、敵のバニラDanger Levelに応じて自分を回復します。",
     "Base Health +1 / Tier 1: 5HP / Tier 2: 10HP / Tier 3: 50HP"),
    ("King", "Crown付与", "特殊効果", "low",
     "ステージ中、バニラのCrownを受け取ります。Kingは同時に最大1人です。",
     "Base Health +1 / Crown付与 / 役職終了時に以前の所有者へ復元"),
    ("Tuna", "停止時ダメージ", "危険効果", "high",
     "開始猶予後に3秒以上停止すると、移動するまで0.1秒ごとに1ダメージを受けます。",
     "Base Health +1 / 開始猶予5秒 / 停止3秒 / 1ダメージ / 0.1秒"),
    ("Musician", "演奏時回復", "回復・支援", "low",
     "楽器で音を鳴らすたびに、本人を含む10m以内の生存プレイヤーを回復します。",
     "Base Health +1 / 1音ごとに5HP / 半径10m / 本人を含む"),
    ("Mage", "チャット魔法攻撃", "危険効果", "high",
     "完全一致のチャットキーワードで5種類のバニラ魔法を選択して発射します。発動後にHPを消費し、10秒間ダメージを受けなければ2秒ごとに1HP回復します。",
     "Base Health +1 / star 10HP・gravity 10HP・roll 15HP・void 30HP・laser 50HP / 間隔5秒 / 自動回復1HP・2秒ごと・待機10秒"),
    ("Gambler", "回復式の価格抽選", "経済効果", "medium",
     "最初に直接つかんだValuableを50%で2倍、それ以外では破壊します。Gambitは緑50HP回復、赤100ダメージ、黒死亡、白全回復＋ステージ中Health +5になります。黄色はバニラ挙動です。",
     "Base Health +1 / 勝率50% / 勝利2倍 / 敗北時は破壊 / 使用済み効果は納品で回復"),
    ("Hunter", "武器・戦利品特化", "戦闘・経済", "low",
     "武器のバッテリー消費を抑え、最後にダメージを与えて倒した敵のオーブ数を抽選します。",
     "Base Health +1 / 消費75% / 0.5%で10個・外れ後10%で2倍 / バー消費は平均75%"),
    ("Stinker", "ウラン雲の足跡", "危険効果", "high",
     "通った経路を2mごとに記録し、ウラン食器破壊時の雲を1つずつ順番に発生させます。",
     "Base Health +1 / 間隔2m / 最新地点のみ安全距離2m / トラック内OFF"),
    ("Engineer", "効果付きValuable抑止", "特殊効果", "low",
     "保持している効果付きValuableの罠、更新処理、専用発動処理をホスト側で抑止します。",
     "Base Health +1 / 保持中と離してから0.25秒間抑止 / 効果強度設定なし"),
    ("Trickster", "固定デコイ設置", "誘導・支援", "medium",
     "チャットでdecoyと入力すると、前方2mに固定したScream Dollを置き、25秒間敵を繰り返し誘導します。有効中は残り時間を維持して再配置できます。",
     "Base Health +1 / 半径40m / 誘導1秒ごと / 終了後45秒・空振り時10秒 / トラック外のみ"),
    ("Mechanic", "Valuable修復", "経済・支援", "low",
     "破損したValuableを直接つかんでいる間、元の価格に対する合計2%を毎秒修復します。",
     "Base Health +1 / 1ステージ合計50ポイントまで / 速度0-100・上限0-100"),
    ("Electrician", "バッテリー回復", "装備・支援", "low",
     "未使用の充電可能アイテムを直接つかんでいる間、合計10%のバッテリーを毎秒回復します。",
     "Base Health +1 / 1ステージ合計100ポイントまで / 速度0-100・上限0-1000"),
    ("Warden", "スタン延長", "戦闘・支援", "low",
     "自分の武器、グレネード、保持中の攻撃用オブジェクトが発生させた敵のスタンを3秒延長します。",
     "Base Health +1 / 元の攻撃にスタンがある場合のみ / 追加時間0-30秒"),
    ("Ninja", "自身の物音抑制", "隠密", "low",
     "自身の足音、着地音、VC、チャットTTSによる敵の調査行動を防ぎ、敵が視覚で認識するまでの時間を既定2倍にします。",
     "Base Health +1 / Huntsmanの音調査も抑止 / 視認倍率1-10 / 武器・Valuable・爆発などの音は通常どおり"),
    ("Executioner", "スタン中ダメージ", "戦闘", "low",
     "攻撃前からスタンしている敵への直接攻撃ダメージを2倍にします。",
     "Base Health +1 / 初回スタン攻撃・物理衝突のみのダメージは対象外 / 倍率1-10"),
    ("Rider", "車両衝突強化", "戦闘・イベント", "medium",
     "バニラ車両の運転中、敵への衝突ダメージと、元から発生する対プレイヤーTumbleノックバックを強化します。",
     "Base Health +1 / 敵ダメージ3倍 / プレイヤーノックバック2倍 / プレイヤーダメージは通常どおり"),
    ("Influencer", "人数連動強化", "イベント", "medium",
     "20m以内の生存中の仲間が多いほど、複数アップグレードが5段階で強化され、定期的に英語TTSを発します。",
     "Base Health +1 / 1-5人で強化 / 6人以上は5人段階 / 物音範囲2倍 / TTS 20-40秒"),
    ("Werewolf", "対プレイヤー攻撃", "危険効果", "high",
     "Werewolfが攻撃者と特定できる、他のプレイヤーへのダメージを2倍にします。",
     "Base Health +1 / 倍率2倍 / 自傷・環境・攻撃者不明は対象外 / 1人プレイ時は抽選対象外"),
    ("Berserker", "低HP強化", "戦闘", "medium",
     "残りHPが少なくなるほど、Strength、Speed、Stamina、Extra Jumpが段階的に強化されます。",
     "Base Health +1 / 境界80・60・40・20・10% / Health・Tumble Wings強化なし / HP回復で段階低下"),
    ("Bodyguard", "ダメージ肩代わり", "支援", "low",
     "15m以内の仲間が敵から受けたダメージの50%を、最も近いBodyguardがHPを1残して肩代わりします。",
     "Health 11へ置換 / 半径15m / 肩代わり50% / 1人プレイ時は抽選対象外"),
    ("Rammer", "タンブル攻撃特化", "戦闘", "medium",
     "Tumble Attackで敵へ150ダメージを与え、ステージ中はTumble系アップグレード3種を0へ固定します。",
     "Base Health +1 / 敵ダメージ150 / Launch・Tumble Climb・Tumble Wings 0固定 / ダメージ0-100000"),
    ("Diver", "床下潜航", "移動・イベント", "high",
     "下向きのTumble Attackで固定床を抜け、最大30秒間、床下を自由に移動します。残り時間と再使用可能状態はTTSで通知されます。",
     "Base Health +1 / 30秒で死亡 / 床上へ戻ると潜航時間と同じクールダウン / 制限1-120秒 / 移動力1-30"),
    ("???", "???", "???", "medium",
     "???",
     "???"),
]

SPEC_PAGES = [
    (
        "基本動作仕様",
        "STAGE LIFECYCLE & ROLE ASSIGNMENT",
        [
            ("ステージ開始",
             "プレイ可能なステージの開始時、ホストが実在する各プレイヤーへ役職を1つ割り当てます。役職、対象アップグレード、ランタイム効果、英語名による通知、同期用の役職一覧を同じ割り当てから初期化します。",
             CYAN),
            ("抽選規則",
             "Enabledが有効でWeightが1以上の通常役職を相対確率で抽選します。UniqueRolesがONの場合は候補を一巡するまで重複を避け、Kingは最大1人です。1人プレイではTracker、Ghost、Medic、Jobless、Rescuer、Influencer、Werewolf、Bodyguardを除外します。候補不足時は制約を段階的に緩和し、最大30人へ割り当てます。",
             MAGENTA),
            ("アップグレード",
             "基礎アップグレードはHealth 1、その他0が既定です。ランレベルごとの目標値を設定でき、役職の対象値は加算ではなくステージ中の絶対値として基礎値を置き換えます。Throwは対象外、Map Player Countだけ0-1、その他の基礎値は0-9999です。",
             ORANGE),
            ("ステージ終了",
             "役職、役職固有の効果、生成物、復活状態、非Healthの役職用アップグレードを解除し、設定した基礎値へ戻します。Healthは現在HPへの影響を避けるためゼロへリセットせず、次の割り当て時にレベル差分で更新します。",
             HexColor("#70A650")),
        ],
    ),
    (
        "マルチプレイ・画面表示",
        "HOST-ONLY, NOTIFICATIONS & UI",
        [
            ("ホストのみ導入",
             "ゲームプレイ効果はホストだけの導入で動作し、MOD未導入の参加者にも役職、アップグレード、回復、ダメージ、復活などを適用します。抽選と効果設定はホストが管理し、参加者側への追加導入は必須ではありません。",
             CYAN),
            ("通知",
             "ステージ開始時、各プレイヤーが割り当てられた英語の役職名をバニラのチャットとTTSで発言します。RoleShuffleが強制する通知TTSでは敵が反応しません。通常のマイク入力、他人の発声、ワールド音は変更しません。",
             MAGENTA),
            ("HUD",
             "MOD導入済みプレイヤーには1列のROLES HUDを左下へ表示します。既定では自分を固定し、ほかのプレイヤーを最大7人ずつ5秒ごとに切り替えます。表示人数、間隔、倍率、位置、整列は各導入者のローカル設定です。",
             ORANGE),
            ("Rolesメニュー・役職確認",
             "Escメニュー右下のROLESからCURRENT ROLES、ROLE GUIDE、BASE UPGRADESを切り替えられ、最初は現在の割り当てを表示します。CURRENT ROLESのプレイヤー行から説明を確認でき、導入済み参加者のガイドにはホスト設定を同期します。秘密の役職は誰かへ割り当てられた後だけガイドと説明を開示します。",
             HexColor("#70A650")),
        ],
    ),
    (
        "効果判定と共通ルール",
        "GAMEPLAY EFFECT RULES",
        [
            ("復活",
             "Phoenixは1ステージに1回だけ自己復活し、Rescuerは3m以内で最も近いDeath Headのある仲間を既定2回まで復活させます。復活後HPは各25が既定で、対象の最大HPを超えません。復活役職には復活直後の追加無敵を付与しません。",
             CYAN),
            ("危険物とダメージ",
             "Bomber、Stinker、Mageが作る攻撃や効果はバニラ実体を使用し、ほかのプレイヤーやValuableへ影響する場合があります。JoblessとTunaの継続ダメージは死亡可能で、失ったHPを自動回復しません。",
             MAGENTA),
            ("保持・攻撃者の判定",
             "MechanicとElectricianは本人が直接つかんでいる対象だけを処理します。Hunterは本人が装備・保持する武器だけを軽減します。WardenとExecutionerはバニラの命中判定が本人を攻撃者として識別できた直接攻撃へ適用します。",
             ORANGE),
            ("回数・合計上限",
             "Medicは実際に回復した合計150HP、Mechanicは元価格に対する合計50ポイント、Electricianは合計100バッテリーポイントが1人・1ステージの既定上限です。Gamblerの使用済み効果はチームの納品完了で復帰します。",
             HexColor("#70A650")),
        ],
    ),
    (
        "設定・互換性・制約",
        "CONFIGURATION, COMPATIBILITY & LIMITS",
        [
            ("REPOConfig",
             "通常役職にはEnabledとWeightを用意し、役職固有の数値もホストが変更できます。秘密の役職はEnabledだけを変更でき、Weightは固定です。HUD項目だけは各導入者のローカル設定です。旧設定は対応する現行項目へ移行します。",
             CYAN),
            ("ショップと基礎値",
             "ショップのアップグレード出現数は既定0で、0-30の範囲で変更できます。基礎アップグレードはランレベル別に設定でき、ステージ外でも維持します。任意のトラック抽選では1種類または全種類を増減し、結果をスロットUIとTTSで共有します。",
             MAGENTA),
            ("Stage Flux互換",
             "Stage Fluxは任意です。失敗時の復活判定、通知予約、内部処理、価値変更を連携し、どちらかがレベル遷移を止めた場合は役職状態を維持します。Stage Fluxがない場合もRoleShuffle単体の処理へ安全に戻ります。",
             ORANGE),
            ("任意MODとの連携",
             "Elite Enemy Variantsは任意です。有効な場合、Enhanced個体をVampire回復量とHunter追加オーブ品質の計算時だけ最大Tier 3まで1段階上として扱います。通常オーブ数やUI表示は変更しません。",
            HexColor("#70A650")),
        ],
    ),
    (
        "抽選バランス",
        "ROLE BALANCE & RANDOM SELECTION",
        [
            ("イベント役の最低保証",
             "ShowcaseはBomber、Stinker、Mage、Gambler、Trickster、King、Rider、Influencer、Diverです。既定では3-6人で1人、7-13人で2人、14-23人で3人、24-30人で4人を最低保証し、イベント型MODとして目立つ役職が一定数登場するようにします。",
             CYAN),
            ("支援役の最低保証",
             "SupportはMedic、Rescuer、Mechanic、Electrician、Warden、Bodyguardです。既定では4-7人で1人、8-13人で2人、14-23人で3人、24-30人で4人を最低保証します。候補が足りない場合は保証を無理に成立させません。",
             MAGENTA),
            ("危険な組み合わせ",
             "DangerはBomber、Stinker、Werewolfで、上限は1-7人で1、8-15人で2、16-30人で3です。HardshipはJoblessとTunaで、上限は1-11人で1、12-30人で2です。さらに4人以下では両グループを合わせて1人までに制限します。",
             ORANGE),
            ("履歴とフォールバック",
             "同じプレイヤーへの同役職の連続割り当てを既定で避けます。JoblessまたはTunaの後は、次の2ステージだけ両方の候補から通常除外します。役職未割り当てを防ぐため、候補が尽きた場合は履歴、重複、基礎値除外、状況依存除外、危険上限の順に制約を段階的に緩和します。",
             HexColor("#70A650")),
        ],
    ),
]


def register_fonts() -> None:
    pdfmetrics.registerFont(
        TTFont("BIZUDGothic", str(FONT_REGULAR_PATH), subfontIndex=0))
    pdfmetrics.registerFont(
        TTFont("BIZUDGothicBold", str(FONT_BOLD_PATH), subfontIndex=0))


def paragraph(
    c: canvas.Canvas,
    text: str,
    x: float,
    y_top: float,
    width: float,
    height: float,
    *,
    size: float = 8.5,
    color: Color = INK,
    leading: float | None = None,
) -> None:
    style = ParagraphStyle(
        "body",
        fontName="BIZUDGothicBold",
        fontSize=size,
        leading=leading or size * 1.45,
        textColor=color,
        alignment=TA_LEFT,
        wordWrap="CJK",
        spaceAfter=0,
        spaceBefore=0,
    )
    block = Paragraph(text, style)
    _, used_h = block.wrap(width, height)
    block.drawOn(c, x, y_top - used_h)


def draw_background(c: canvas.Canvas) -> None:
    c.setFillColor(BACKGROUND)
    c.rect(0, 0, PAGE_W, PAGE_H, fill=1, stroke=0)
    c.setFillColor(CYAN)
    c.rect(0, PAGE_H - 5, PAGE_W * 0.42, 5, fill=1, stroke=0)
    c.setFillColor(MAGENTA)
    c.rect(PAGE_W * 0.42, PAGE_H - 5, PAGE_W * 0.33, 5, fill=1, stroke=0)
    c.setFillColor(ORANGE)
    c.rect(PAGE_W * 0.75, PAGE_H - 5, PAGE_W * 0.25, 5, fill=1, stroke=0)


def draw_footer(
    c: canvas.Canvas,
    page_number: int,
    edition: str,
    label: str = "ROLE CATALOG",
) -> None:
    c.setStrokeColor(HexColor("#27353D"))
    c.setLineWidth(0.6)
    c.line(MARGIN, 23, PAGE_W - MARGIN, 23)
    c.setFont("BIZUDGothicBold", 7.5)
    c.setFillColor(MUTED)
    c.drawString(
        MARGIN,
        11,
        f"ROLE SHUFFLE v4.4.0  /  {edition}  /  {label}")
    c.drawRightString(PAGE_W - MARGIN, 11, f"{page_number:02d}")


def draw_cover(c: canvas.Canvas, internal: bool) -> None:
    draw_background(c)
    c.setFont("BIZUDGothicBold", 13)
    c.setFillColor(CYAN)
    c.drawString(MARGIN, PAGE_H - 52, "HOST-ONLY RANDOM PLAYER ROLE MOD")

    c.setFont("BIZUDGothicBold", 39)
    c.setFillColor(white)
    c.drawString(MARGIN, PAGE_H - 117, "ROLE SHUFFLE")
    c.setFont("BIZUDGothicBold", 24)
    c.setFillColor(INK)
    c.drawString(MARGIN, PAGE_H - 158, "職業一覧")

    c.setFont("BIZUDGothicBold", 12)
    c.setFillColor(MUTED)
    c.drawString(
        MARGIN,
        PAGE_H - 189,
        f"v4.4.0  /  全{len(ROLES)}職業・動作仕様収録")
    c.setFont("BIZUDGothicBold", 9)
    c.setFillColor(MAGENTA if internal else CYAN)
    c.drawRightString(
        PAGE_W - MARGIN,
        PAGE_H - 189,
        "INTERNAL EDITION" if internal else "PUBLIC EDITION")

    panel_size = 190
    panel_x = (PAGE_W - panel_size) / 2
    panel_y = 370
    c.setFillColor(CARD)
    c.setStrokeColor(CYAN)
    c.setLineWidth(2)
    c.roundRect(panel_x, panel_y, panel_size, panel_size, 12, fill=1, stroke=1)
    c.drawImage(
        ImageReader(str(ICON_PATH)),
        panel_x + 9,
        panel_y + 9,
        panel_size - 18,
        panel_size - 18,
        preserveAspectRatio=True,
        mask="auto",
    )
    c.setFont("BIZUDGothicBold", 14)
    c.setFillColor(CYAN)
    c.drawCentredString(
        PAGE_W / 2,
        panel_y - 25,
        f"{len(ROLES)} PLAYER ROLES")

    paragraph(
        c,
        "ステージ開始時、プレイヤーごとに職業を抽選するホスト専用MOD。"
        "本書では、各職業のデフォルト効果、付与されるアップグレード量、"
        "主な制限と危険性に加え、抽選、マルチプレイ、効果判定、互換性の"
        "共通仕様をまとめています。",
        MARGIN,
        310,
        PAGE_W - MARGIN * 2,
        90,
        size=12,
        leading=18,
    )

    for index, risk in enumerate(("low", "medium", "high")):
        label, color = RISK[risk]
        x = MARGIN + index * 116
        c.setFillColor(color)
        c.roundRect(x, 118, 100, 26, 5, fill=1, stroke=0)
        c.setFont("BIZUDGothicBold", 8.5)
        c.setFillColor(white)
        c.drawCentredString(x + 50, 127, f"危険度 {label}")

    paragraph(
        c,
        "低: 直接的な不利益を生まない / 中: 抽選結果により資産などを失う / "
        "高: ダメージや危険物を自動発生させ、死亡・事故につながる",
        MARGIN,
        98,
        PAGE_W - MARGIN * 2,
        30,
        size=7.6,
        color=MUTED,
        leading=11,
    )
    draw_footer(
        c,
        1,
        "INTERNAL" if internal else "PUBLIC",
        "ROLE CATALOG / COVER")
    c.showPage()


def draw_role_card(
    c: canvas.Canvas,
    role: tuple[str, str, str, str, str, str],
    role_number: int,
    x: float,
    y: float,
    w: float,
    h: float,
    internal: bool,
) -> None:
    name, subtitle, category, risk, description, settings = role
    is_secret = name == "???"
    is_hidden = is_secret and not internal
    if is_secret and internal:
        name = "Superbot"
        subtitle = "選別した能力の統合"
        category = "特殊・希少"
        description = (
            "Diverを含む危険・不利効果を除いた役職能力を統合します。Rammerは対敵"
            "Tumble Attackダメージだけ、Influencerは人数連動強化だけが"
            "適用されます。")
        settings = (
            "ID 999 / Enabledのみ設定可能 / "
            "固定Weightは標準役職の1/1000")
    risk_label, risk_color = ("???", MUTED) if is_hidden else RISK[risk]

    c.setFillColor(CARD)
    c.setStrokeColor(risk_color)
    c.setLineWidth(1.8)
    c.roundRect(x, y, w, h, 8, fill=1, stroke=1)

    c.setFillColor(risk_color)
    c.roundRect(x + 9, y + h - 23, 62, 15, 4, fill=1, stroke=0)
    c.setFont("BIZUDGothicBold", 7.2)
    c.setFillColor(white)
    c.drawCentredString(x + 40, y + h - 18.2, f"危険度 {risk_label}")

    c.setFont("BIZUDGothicBold", 7.2)
    c.setFillColor(MUTED)
    c.drawRightString(x + w - 9, y + h - 18.2,
                      "???" if is_hidden else category)

    display_name = f"999  {name}" if is_secret else f"{role_number:02d}  {name}"
    title_size = 17 if len(display_name) <= 13 else 15
    paragraph(c, display_name, x + 12, y + h - 43, w - 24, 27,
              size=title_size, leading=19)
    paragraph(c, subtitle, x + 12, y + h - 70, w - 24, 18,
              size=9.2, color=MUTED)

    c.setFillColor(HexColor("#70A650"))
    c.roundRect(x + 12, y + h - 98, 67, 17, 4, fill=1, stroke=0)
    c.setFont("BIZUDGothicBold", 7)
    c.setFillColor(white)
    c.drawCentredString(x + 45.5, y + h - 92.3,
                        "???" if is_hidden else "既定 ON")
    c.setFillColor(HexColor("#1D5969"))
    c.roundRect(x + 85, y + h - 98, 67, 17, 4, fill=1, stroke=0)
    c.setFillColor(white)
    default_weight = ROLE_WEIGHTS.get(name, 100)
    c.drawCentredString(
        x + 118.5,
        y + h - 92.3,
        ("???" if is_hidden else
         ("固定Weight" if is_secret else f"Weight {default_weight}")))

    c.setStrokeColor(HexColor("#2A3941"))
    c.setLineWidth(0.5)
    c.line(x + 10, y + h - 113, x + w - 10, y + h - 113)
    paragraph(c, description, x + 12, y + h - 128, w - 24, 61,
              size=9, leading=13)

    c.setFillColor(CARD_INNER)
    c.roundRect(x + 10, y + 12, w - 20, 45, 5, fill=1, stroke=0)
    paragraph(
        c,
        f"<font color='#46C6E8'>既定・注意</font>  {settings}",
        x + 16,
        y + 49,
        w - 32,
        34,
        size=7.8,
        leading=10.8,
    )


def draw_note_card(
    c: canvas.Canvas,
    title: str,
    body: str,
    color: Color,
    x: float,
    y: float,
    w: float,
    h: float,
) -> None:
    c.setFillColor(CARD)
    c.setStrokeColor(color)
    c.setLineWidth(1.4)
    c.roundRect(x, y, w, h, 8, fill=1, stroke=1)
    c.setFont("BIZUDGothicBold", 12)
    c.setFillColor(color)
    c.drawString(x + 14, y + h - 34, title)
    c.setStrokeColor(HexColor("#2A3941"))
    c.setLineWidth(0.5)
    c.line(x + 14, y + h - 48, x + w - 14, y + h - 48)
    paragraph(c, body, x + 14, y + h - 67, w - 28, h - 83,
              size=9.1, leading=14)


def draw_spec_section(
    c: canvas.Canvas,
    title: str,
    body: str,
    color: Color,
    x: float,
    y: float,
    w: float,
    h: float,
    section_number: int,
) -> None:
    c.setFillColor(CARD)
    c.setStrokeColor(color)
    c.setLineWidth(1.6)
    c.roundRect(x, y, w, h, 8, fill=1, stroke=1)

    c.setFillColor(color)
    c.roundRect(x + 13, y + h - 36, 31, 21, 5, fill=1, stroke=0)
    c.setFont("BIZUDGothicBold", 9)
    c.setFillColor(white)
    c.drawCentredString(x + 28.5, y + h - 29, f"{section_number:02d}")

    c.setFont("BIZUDGothicBold", 13)
    c.setFillColor(INK)
    c.drawString(x + 55, y + h - 31, title)
    c.setStrokeColor(HexColor("#2A3941"))
    c.setLineWidth(0.5)
    c.line(x + 14, y + h - 49, x + w - 14, y + h - 49)

    paragraph(
        c,
        body,
        x + 15,
        y + h - 67,
        w - 30,
        h - 80,
        size=9.4,
        leading=15,
    )


def chunks(items: list, size: int) -> Iterable[list]:
    for start in range(0, len(items), size):
        yield items[start:start + size]


def draw_role_pages(c: canvas.Canvas, internal: bool) -> None:
    card_gap_x = 10
    card_gap_y = 10
    card_w = (PAGE_W - MARGIN * 2 - card_gap_x) / 2
    top = PAGE_H - 69
    bottom = 33
    card_h = (top - bottom - card_gap_y * 2) / 3
    pages = list(chunks(ROLES, 6))

    page_number = 2
    for page_index, page_roles in enumerate(pages):
        draw_background(c)
        c.setFont("BIZUDGothicBold", 18)
        c.setFillColor(INK)
        c.drawString(MARGIN, PAGE_H - 37, "職業カタログ")
        c.setFont("BIZUDGothicBold", 8)
        c.setFillColor(MUTED)
        start = page_index * 6 + 1
        end = min((page_index + 1) * 6, len(ROLES))
        range_end = "999" if page_index == len(pages) - 1 else f"{end:02d}"
        c.drawRightString(PAGE_W - MARGIN, PAGE_H - 34,
                          f"{start:02d} - {range_end} / {len(ROLES):02d}")

        for index, role in enumerate(page_roles):
            col = index % 2
            row = index // 2
            x = MARGIN + col * (card_w + card_gap_x)
            y = top - (row + 1) * card_h - row * card_gap_y
            draw_role_card(
                c,
                role,
                page_index * 6 + index + 1,
                x,
                y,
                card_w,
                card_h,
                internal)

        if page_index == len(pages) - 1 and len(page_roles) < 6:
            notes = [
                ("抽選", "通常役職はEnabledとWeightで抽選対象を調整できます。秘密の役職はEnabledのみ変更でき、Weightは固定です。UniqueRolesがONの場合は可能な範囲で重複を避けます。", MAGENTA),
                ("ホストのみ", "ゲームプレイ効果はホストだけの導入で動作します。MOD導入者には同期された職業HUDも表示されます。", CYAN),
                ("基礎アップグレード", "全職業に共通する基礎値をREPOConfigで変更できます。既定はHealth 1、その他0です。役職固有の対象値はステージ中だけ基礎値を置き換えます。", ORANGE),
            ]
            for cell_index, (title, body, color) in zip(
                    range(len(page_roles), 6), notes):
                col = cell_index % 2
                row = cell_index // 2
                x = MARGIN + col * (card_w + card_gap_x)
                y = top - (row + 1) * card_h - row * card_gap_y
                draw_note_card(c, title, body, color, x, y, card_w, card_h)

        draw_footer(
            c,
            page_number,
            "INTERNAL" if internal else "PUBLIC")
        c.showPage()
        page_number += 1


def draw_spec_pages(c: canvas.Canvas, internal: bool) -> None:
    page_number = 2 + ((len(ROLES) + 5) // 6)
    section_number = 1
    section_gap = 11
    section_h = 155
    top = PAGE_H - 102

    for title, subtitle, sections in SPEC_PAGES:
        draw_background(c)
        c.setFont("BIZUDGothicBold", 8)
        c.setFillColor(CYAN)
        c.drawString(MARGIN, PAGE_H - 32, "ROLE SHUFFLE SPECIFICATION")
        c.setFont("BIZUDGothicBold", 20)
        c.setFillColor(INK)
        c.drawString(MARGIN, PAGE_H - 60, title)
        c.setFont("BIZUDGothicBold", 8)
        c.setFillColor(MUTED)
        c.drawRightString(PAGE_W - MARGIN, PAGE_H - 57, subtitle)

        for index, (section_title, body, color) in enumerate(sections):
            y = top - (index + 1) * section_h - index * section_gap
            draw_spec_section(
                c,
                section_title,
                body,
                color,
                MARGIN,
                y,
                PAGE_W - MARGIN * 2,
                section_h,
                section_number,
            )
            section_number += 1

        draw_footer(
            c,
            page_number,
            "INTERNAL" if internal else "PUBLIC",
            "SPECIFICATION")
        c.showPage()
        page_number += 1


def build_pdf(output: Path, internal: bool) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(output), pagesize=A4, pageCompression=1)
    edition = "Internal" if internal else "Public"
    c.setTitle(f"RoleShuffle v4.4.0 - Role List - {edition}")
    c.setAuthor("RoleShuffle")
    c.setSubject("RoleShuffle role catalog and gameplay specifications")
    draw_cover(c, internal)
    draw_role_pages(c, internal)
    draw_spec_pages(c, internal)
    c.save()
    print(output)


def main() -> None:
    register_fonts()
    build_pdf(OUTPUT_PUBLIC, internal=False)
    build_pdf(OUTPUT_INTERNAL, internal=True)


if __name__ == "__main__":
    main()
