"""Current public/internal illustrated catalogs. Reuses the established PDF palette."""
from __future__ import annotations

import html
import json
import re
from functools import lru_cache
from pathlib import Path

from PIL import Image
from reportlab.lib.colors import HexColor, white
from reportlab.lib.styles import ParagraphStyle
from reportlab.lib.utils import ImageReader
from reportlab.pdfgen import canvas
from reportlab.platypus import Paragraph
from pypdf import PdfReader

import build_role_list_pdf as style

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "output/pdf"
ASSETS = ROOT / "Assets/role-emblems-semibot-v1"
VERSION = json.loads((ROOT / "package/manifest.json").read_text(encoding="utf-8-sig"))["version_number"]
W, H = style.PAGE_W, style.PAGE_H
M = style.MARGIN
INK, MUTED, CYAN = style.INK, style.MUTED, style.CYAN
SOURCE = (ROOT / "package/README.md").read_text(encoding="utf-8-sig")
TABLE = SOURCE.split("### 役職一覧", 1)[1].split("### 設定", 1)[0]
ROLES = []
for line in TABLE.splitlines():
    cells = [cell.strip() for cell in line.strip().strip("|").split("|")]
    if len(cells) != 4 or cells[0] == "役職" or cells[0].startswith("---"):
        continue
    # The current settings implementation allows upgrade targets up to 200.
    cells[3] = cells[3].replace("`100`", "`200`")
    ROLES.append(cells)
assert len(ROLES) == 42
LEGACY = {r[0]: (r[1], r[2], r[3]) for r in style.ROLES}
META = {
    "Sniper": ("距離連動攻撃", "攻撃", "medium"),
    "Imitator": ("役職コピー", "特殊", "medium"),
    "Avenger": ("仲間の死亡で強化", "攻撃", "low"),
    "Brawler": ("近接特化", "攻撃", "medium"),
}
SECRET = {
    "???1": (1001, "Superbot",
        "Bomber、Stinker、Werewolf、Jobless、Tuna、King、Diver、Imitator、Sniper、Brawlerを除く役職のアップグレードと能力を併せ持ちます。",
        "RammerはTumble Attackのダメージだけを適用し、Tumble系アップグレードの0固定はありません。Influencerは人数連動強化だけを適用し、物音の増加と定期TTSはありません。通常抽選の候補不足を埋める補完には使用しません。"),
    "???2": (1002, "Disaster",
        "Bomber・Stinker・Tunaの能力と制約を併せ持ちます。移動した場所に起動済みグレネードとウラン雲を残し、一定時間停止すると継続ダメージを受けます。",
        "各能力は対応する役職の設定に従います。動くと停止時間と継続ダメージが止まりますが、失ったHPは戻りません。1人のセッションでは抽選しません。候補不足を埋める補完には使用しません。"),
}


@lru_cache(maxsize=48)
def pdf_image(path: str, max_pixels: int = 420) -> ImageReader:
    # Keep source artwork intact; embed only the resolution needed on paper.
    with Image.open(path) as source:
        raster = source.convert("RGB")
        raster.thumbnail((max_pixels, max_pixels), Image.Resampling.LANCZOS)
    return ImageReader(raster)


def clean(text: str) -> str:
    text = text.replace("`", "").replace("\u2011", "-")
    return html.escape(text)


def block(text: str, width: float, size: float = 8.5, color=INK) -> tuple[Paragraph, float]:
    p = Paragraph(clean(text), ParagraphStyle("catalog", fontName="BIZUDGothicBold",
        fontSize=size, leading=size * 1.48, textColor=color, wordWrap="CJK"))
    _, height = p.wrap(width, 10000)
    return p, height


def text(c, value, x, top, width, size=8.5, color=INK):
    p, height = block(value, width, size, color)
    p.drawOn(c, x, top - height)
    return top - height


def page(c, title, number, internal, subtitle=""):
    style.draw_background(c)
    c.setFillColor(INK)
    c.setFont("BIZUDGothicBold", 21)
    c.drawString(M, H - 45, title)
    if subtitle:
        text(c, subtitle, M, H - 56, W - 2*M, 8, MUTED)
    c.setStrokeColor(HexColor("#27353D"))
    c.line(M, 25, W-M, 25)
    c.setFont("BIZUDGothicBold", 7.2)
    c.setFillColor(MUTED)
    edition = "INTERNAL" if internal else "PUBLIC"
    c.drawString(M, 12, f"ROLE SHUFFLE v{VERSION} / {edition}")
    c.drawRightString(W-M, 12, f"{number:02d}")


def cover(c, internal):
    page(c, "ROLE SHUFFLE", 1, internal, "HOST-ONLY RANDOM PLAYER ROLE MOD")
    c.setFont("BIZUDGothicBold", 26)
    c.setFillColor(white)
    c.drawString(M, H-125, "職業一覧・共通仕様")
    c.setFillColor(CYAN)
    c.setFont("BIZUDGothicBold", 13)
    c.drawString(M, H-155, f"v{VERSION} / 42 ROLES")
    c.drawImage(pdf_image(str(ROOT / "package/icon.png"), 800), (W-185)/2, 370, 185, 185)
    edition = "内部版 - 隠し役職の情報を含みます。公開配布には使用しないでください。" if internal else "公開版 - 隠し役職の名前・能力・固有エンブレムは非開示です。"
    text(c, edition, M, 320, W-2*M, 12, style.MAGENTA if internal else CYAN)
    text(c, "ステージごとに役職が変わるイベント型MOD。各役職の機能、デフォルト効果、主な制限と設定できる項目をまとめています。ゲームプレイはホストのみの導入に対応し、最大30人をサポートします。", M, 265, W-2*M, 11)
    text(c, "数値はデフォルト設定です。実際の効果はホスト設定で変わります。役職のアップグレード値は追加個数ではなく目標レベルを表します。基礎アップグレードの説明は共通仕様にまとめています。", M, 172, W-2*M, 9.5, MUTED)
    text(c, "危険度はプレイ上の注意を示す目安で、抽選カテゴリとは別です。低: 直接的な不利益が少ない / 中: 条件次第で不利益がある / 高: 死亡・味方への事故につながりやすい", M, 99, W-2*M, 8, MUTED)
    c.showPage()


def card(c, row, index, x, y, width, height, internal):
    name, description, limits, settings = row
    hidden = name in SECRET and not internal
    secret = name in SECRET
    if secret:
        number, actual_name, actual_description, actual_limits = SECRET[name]
        if internal:
            name, description, limits = actual_name, actual_description, actual_limits
            settings = "Enabledのみ変更可能。相対Weightは標準役職の1/1000固定（出現確率そのものではありません）。"
        subtitle, category, risk = "希少な複合役職", "特殊", "high" if number == 1002 else "low"
    else:
        number = index + 1
        subtitle, category, risk = META.get(name, LEGACY.get(name, ("固有能力", "特殊", "low")))
    color = MUTED if hidden else style.RISK[risk][1]
    c.setFillColor(style.CARD)
    c.setStrokeColor(color)
    c.setLineWidth(1.2)
    c.roundRect(x, y, width, height, 8, fill=1, stroke=1)
    icon = ASSETS / "unrevealed/Unrevealed.png" if hidden else ASSETS / "selected" / f"{number:02d}-{name}.png"
    assert icon.is_file(), icon
    c.drawImage(pdf_image(str(icon)), x+12, y+height-96, 76, 76)
    text(c, str(number).zfill(2), x+100, y+height-20, width-112, 8, MUTED)
    text(c, name, x+100, y+height-36, width-112, 14)
    text(c, "???" if hidden else subtitle, x+100, y+height-58, width-112, 8, MUTED)
    text(c, "???" if hidden else "危険度 " + style.RISK[risk][0], x+100, y+height-78, width-112, 8, color)
    c.setStrokeColor(HexColor("#2A3941"))
    c.line(x+12, y+height-106, x+width-12, y+height-106)
    sections = [("機能・既定効果", description), ("主な制限・注意", limits), ("調整可能な値", settings)]
    size = 8.3
    available = height - 123
    while True:
        used = sum(block(body, width-24, size)[1] + 22 for _, body in sections)
        if used <= available:
            break
        size -= .15
        if size < 7.3:
            raise RuntimeError(f"Card text overflow: {name} ({used} > {available})")
    top = y+height-119
    for label, body in sections:
        top = text(c, "???" if hidden else label, x+12, top, width-24, 7.5, CYAN) - 4
        top = text(c, body, x+12, top, width-24, size) - 8
    assert top >= y+7, (name, top, y)


SPECS = [
    ("共通仕様・画面表示", [
        ("ホストのみ導入 / 最大30人", "役職・アップグレード・ゲームプレイ効果とバニラTTSはMOD未導入の参加者にも適用されます。HUD、Rolesメニュー、エンブレムはMOD導入済みプレイヤーだけに表示します。"),
        ("Rolesメニュー", "CURRENT ROLESではプレイヤーの行から説明を開閉できます。ROLE GUIDEは設定で有効な役職を表示し、英語・日本語を切り替えられます。未開示の隠し役職は共通の「?」アイコンを使用し、割り当てられると名前・説明・固有エンブレムを開示します。BASE UPGRADESでは現在値とConfigured / Truck Drawの内訳を確認できます。"),
        ("開始と終了", "プレイ可能なステージの開始時に各プレイヤーへ役職を割り当てます。ステージが実際に終了したときだけ役職と固有効果を解除します。復活効果で失敗時の移行が中断された場合は役職を維持します。"),
        ("通知と連携", "役職通知は英語のチャット・TTSで行います。Stage Flux導入時は通知が重ならないよう調整します。Influencerの定期TTSは敵を引き付けます。Elite Enemy Variantsとの報酬連携は設定で切り替えられ、Enhanced個体をVampire回復量とHunter追加オーブ品質で最大Tier 3まで1段階上として扱います。"),
    ]),
    ("基礎アップグレード・抽選", [
        ("基礎アップグレード", "デフォルトはHealth 1、その他0です。ランレベル1-999999に対する目標値を「レベル:設定値」で指定できます。アップグレード値は0-200、Map Player Countだけ0-1、Throwは対象外です。範囲外の数値は上下限へ補正します。基礎値はステージ外でも維持します。"),
        ("トラック抽選", "ショップ後の準備フェーズで、1種類のアップグレードまたはAllを抽選し、増減値を適用する機能をON/OFFできます。増減の既定Weightは -1:10、0:15、1:60、2:15。各種類のWeight、増減値:Weight、抽選上限を変更できます。減少できない候補は除外されるため、実際の確率は状態で変わります。"),
        ("上限に近づいたとき", "現在レベルが抽選上限へ近づくほど抽選Weightを低下させます。デフォルトは上限時0.25倍、低下カーブ2です。抽選の追加値はセーブデータごとに引き継ぎます。導入済み参加者にも抽選結果と現在の共有基礎値を表示します。"),
        ("役職の目標値との関係", "通常の特化役は対象アップグレードを役職設定値へ置き換えます。Tank、Runner、Jumper、Lifter、Launcher、Climber、Flyer、Tracker、Ghostは、対応する基礎目標値のいずれかが役職設定値以上なら通常抽選から除外します。InfluencerとBerserkerは役職付与前のレベルを下げずに強化します。"),
    ]),
    ("役職の選ばれ方", [
        ("イベント・支援役", "人数に応じてイベント性の高い役職と支援役を優先します。Showcaseの最低人数は3/7/14/24人で1/2/3/4人、Supportは4/8/14/24人で1/2/3/4人です。これらの保証と危険な組み合わせの上限は設定できます。"),
        ("個人ごとの履歴", "直近5回に割り当てられた役職は、そのプレイヤーの抽選でWeightを半分にします。通常は前回と同じ役職を避け、JoblessまたはTunaの後は2ステージ、両方を避けます。必要時には制約を段階的に緩和します。"),
        ("使い道のない役職を避ける", "1人のときは仲間が必要な役職などを除外します。対象物がないMusician、Engineer、Electrician、Riderも通常は除外します。Brawlerには近接武器、Sniperには近接武器または銃が必要で、武器として使える貴重品と杖はSniperの判定に含めません。Imitatorにはコピー可能な役職の仲間が必要です。"),
        ("設定と詳細確認", "設定はREPOConfigで変更できます。本書はデフォルトの機能案内です。実際の参加中の数値はRolesメニューに反映されるホスト設定を確認してください。お問い合わせ: https://github.com/CapacityDown/RoleShuffle/issues"),
    ]),
]


def specs(c, page_number, internal):
    for title, sections in SPECS:
        page(c, title, page_number, internal)
        top = H-98
        for label, body in sections:
            c.setFillColor(style.CARD)
            c.roundRect(M, top-155, W-2*M, 155, 8, fill=1, stroke=0)
            end = text(c, label, M+15, top-14, W-2*M-30, 12, CYAN)
            end = text(c, body, M+15, end-12, W-2*M-30, 9.2)
            assert end > top-145, label
            top -= 167
        c.showPage()
        page_number += 1
    page(c, "人数・HP連動の強化", page_number, internal, "デフォルトの最低目標レベル。該当しない項目は追加強化なし。")
    source = (ROOT / "RoleUpgradeScaling.cs").read_text(encoding="utf-8-sig")
    top = H-102
    for role, suffix in [("Influencer", "周囲の仲間の人数:レベル"), ("Berserker", "残りHP割合以下:レベル")]:
        top = text(c, role + " / " + suffix, M, top, W-2*M, 14, CYAN) - 14
        body = source.split(f"string {role}Default", 1)[1].split("};", 1)[0]
        for name, expression in re.findall(r'"(\w+)" => "([0-9:,]+)"', body):
            top = text(c, f"{name}: {expression}", M+10, top, W-2*M-20, 10) - 10
        top -= 18
    text(c, "Influencerは5人以降同じ値です。BerserkerのHealthは対象外、Tumble Wingsの既定強化は0です。対象のアップグレードごとに記述式で調整でき、設定済みの基礎値を下げる強化は行いません。", M, top, W-2*M, 9.5, MUTED)
    c.showPage()


def build(internal):
    output = OUT / ("RoleShuffle_Role_List_Internal.pdf" if internal else "RoleShuffle_Role_List_Public.pdf")
    output.parent.mkdir(parents=True, exist_ok=True)
    c = canvas.Canvas(str(output), pagesize=(W,H), pageCompression=1)
    c.setTitle(f"RoleShuffle v{VERSION} - {'Internal' if internal else 'Public'} Role Catalog")
    c.setAuthor("RoleShuffle")
    cover(c, internal)
    cw, ch = (W-2*M-12)/2, (H-122)/2
    for first in range(0, len(ROLES), 4):
        page(c, "職業カタログ", 2+first//4, internal, "DEFAULT ROLE EFFECTS / " + ("内部版" if internal else "公開版"))
        for index in range(first, min(first+4, len(ROLES))):
            cell = index-first
            card(c, ROLES[index], index, M+(cell%2)*(cw+12), H-82-(cell//2+1)*ch-(cell//2)*10, cw, ch, internal)
        c.showPage()
    specs(c, 2+(len(ROLES)+3)//4, internal)
    c.save()
    reader = PdfReader(str(output))
    joined = "\n".join(p.extract_text() for p in reader.pages)
    for role in ROLES:
        assert role[0] in joined or (internal and role[0] in SECRET), role[0]
    if not internal:
        assert "Superbot" not in joined and "Disaster" not in joined
    print(f"{output} ({len(reader.pages)} pages)")


if __name__ == "__main__":
    style.register_fonts()
    build(False)
    build(True)
