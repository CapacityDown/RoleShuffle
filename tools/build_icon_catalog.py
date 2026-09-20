"""Build portable galleries from the approved runtime icons, without changing artwork."""
from __future__ import annotations

import base64
import hashlib
import html
import json
import re
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

ROOT = Path(__file__).resolve().parents[1]
ICONS = ROOT / "Assets/role-emblems-semibot-v1/transparent/runtime"
OUT = ROOT / "output/icon-catalog"


def image_uri(path: Path) -> str:
    return "data:image/png;base64," + base64.b64encode(path.read_bytes()).decode("ascii")


def roles() -> list[tuple[int, str, Path]]:
    source = (ROOT / "StageRole.cs").read_text(encoding="utf-8-sig")
    body = source.split("{", 1)[1].split("}", 1)[0]
    result = []
    value = -1
    for part in body.split(","):
        match = re.fullmatch(r"\s*(\w+)(?:\s*=\s*(\d+))?\s*", part)
        if not match:
            if part.strip():
                raise ValueError(f"Unrecognized role: {part}")
            continue
        value = int(match[2]) if match[2] else value + 1
        name = "Courier" if match[1] == "Jobless" else match[1]
        icon_id = value if value >= 1000 else value + 1
        path = ICONS / f"{icon_id:02}-{name}.png"
        if not path.exists():
            raise FileNotFoundError(path)
        result.append((icon_id, name, path))
    return result


def build(public: bool, entries: list[tuple[int, str, Path]], version: str) -> Path:
    cards = []
    for icon_id, name, path in entries:
        secret = icon_id >= 1000
        if public and secret:
            name = f"???{icon_id - 1000}"
            path = ICONS / "Unrevealed.png"
        label = "隠し役職" if secret else f"{icon_id:02}"
        cards.append((name, label, image_uri(path)))
    if not public:
        cards.append(("Unrevealed", "非開示用・共通", image_uri(ICONS / "Unrevealed.png")))

    def card(name: str, label: str, uri: str) -> str:
        search = name.lower() + (" インフルエンザ" if name == "Influenza" else "")
        return (f'<button class="card" data-search="{html.escape(search)}" '
                f'aria-label="{html.escape(name)}を拡大">'
                f'<span class="art"><img src="{uri}" alt="{html.escape(name)}" '
                'width="256" height="256" draggable="false"></span>'
                f'<span class="label">{html.escape(label)}</span>'
                f'<span class="name">{html.escape(name)}</span></button>')

    title = "アイコン一覧 · 公開版" if public else "アイコン一覧 · 全アイコン"
    subtitle = (f"全{len(entries)}役職。隠し役職は共通アイコンで表示しています。" if public else
                f"全{len(entries)}役職＋非開示用の共通アイコン。隠し役職の名前と絵柄を含みます。")
    count = len(cards)
    page = TEMPLATE.replace("__TITLE__", title).replace("__VERSION__", html.escape(version))
    page = page.replace("__SUBTITLE__", subtitle).replace("__COUNT__", str(count))
    page = page.replace("__CARDS__", "\n".join(card(*c) for c in cards))
    suffix = "Public" if public else "Full"
    name = f"RoleShuffle_Icons_v{version}_{suffix}"
    page = page.replace("__EXPORT_NAME__", name)
    destination = OUT / f"{name}.html"
    destination.write_text(page, encoding="utf-8", newline="\n")

    # The public artifact must not contain the secret names or their embedded art.
    if public:
        for icon_id, secret_name, path in entries:
            if icon_id >= 1000:
                assert secret_name not in page, secret_name
                assert image_uri(path) not in page, path
    assert page.count('class="card"') == count
    assert not re.search(r'(?:src|href)="https?://', page)
    print(f"{destination}: {count} cards, {destination.stat().st_size:,} bytes")
    build_png(cards, title, subtitle, version, OUT / f"{name}.png")
    return destination


def build_png(cards: list[tuple[str, str, str]], title: str, subtitle: str,
              version: str, destination: Path) -> None:
    """Compose the original runtime artwork at native size onto a contact sheet."""
    from io import BytesIO

    fonts = Path("C:/Windows/Fonts")
    regular = fonts / "meiryo.ttc"
    bold = fonts / "meiryob.ttc"
    if not regular.exists() or not bold.exists():
        raise FileNotFoundError("The icon catalog requires the Meiryo Japanese fonts.")
    title_font = ImageFont.truetype(str(bold), 42)
    subtitle_font = ImageFont.truetype(str(regular), 23)
    name_font = ImageFont.truetype(str(bold), 23)
    label_font = ImageFont.truetype(str(regular), 17)
    columns, margin, gap, cell_width, cell_height, top = 6, 48, 18, 316, 352, 198
    width = margin * 2 + columns * cell_width + (columns - 1) * gap
    rows = (len(cards) + columns - 1) // columns
    height = top + rows * cell_height + (rows - 1) * gap + 72
    sheet = Image.new("RGB", (width, height), "#10191d")
    draw = ImageDraw.Draw(sheet)
    draw.text((margin, 28), f"ROLESHUFFLE  /  v{version}", font=label_font, fill="#7fdfcf")
    draw.text((margin, 65), title, font=title_font, fill="#f8f0dc")
    draw.text((margin, 133), subtitle, font=subtitle_font, fill="#a8bcc2")
    for index, (name, label, uri) in enumerate(cards):
        x = margin + (index % columns) * (cell_width + gap)
        y = top + (index // columns) * (cell_height + gap)
        draw.rounded_rectangle((x, y, x + cell_width, y + cell_height),
                               radius=14, fill="#1a272c", outline="#3b5058", width=1)
        with Image.open(BytesIO(base64.b64decode(uri.split(",", 1)[1]))) as image:
            assert image.size == (256, 256), (name, image.size)
            icon = image.convert("RGBA")
            sheet.paste(icon, (x + (cell_width - 256) // 2, y + 16), icon)
        assert draw.textlength(name, font=name_font) <= cell_width - 20, name
        draw.text((x + cell_width / 2, y + 283), label, font=label_font,
                  fill="#a8bcc2", anchor="mt")
        draw.text((x + cell_width / 2, y + 313), name, font=name_font,
                  fill="#f8f0dc", anchor="mt")
    draw.text((margin, height - 43), f"RoleShuffle  /  {len(cards)} icons", font=label_font,
              fill="#a8bcc2")
    sheet.save(destination, optimize=True)
    print(f"{destination}: {width} x {height}, {len(cards)} icons")


TEMPLATE = r'''<!doctype html>
<html lang="ja">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width,initial-scale=1">
<title>RoleShuffle — __TITLE__</title>
<style>
:root{color-scheme:dark;--bg:#10191d;--panel:#1a272c;--art:#23363c;--text:#f8f0dc;--muted:#a8bcc2;--line:#3b5058;--accent:#7fdfcf;--size:160px}
*{box-sizing:border-box}body{margin:0;background:var(--bg);color:var(--text);font-family:Inter,"Yu Gothic UI",Meiryo,sans-serif}
main{max-width:1460px;margin:auto;padding:40px 32px 56px}.eyebrow{color:var(--accent);letter-spacing:.22em;font-size:12px;font-weight:700}
h1{margin:9px 0 8px;font-size:clamp(27px,4vw,42px);letter-spacing:-.02em}p{color:var(--muted);line-height:1.65;margin:8px 0}.version{float:right;color:var(--muted);font-size:13px}
.toolbar{display:flex;flex-wrap:wrap;gap:12px;align-items:center;padding:18px 0 24px;margin-top:14px;border-top:1px solid var(--line)}
input,select,.control{font:inherit;color:var(--text);background:var(--panel);border:1px solid var(--line);border-radius:8px;padding:9px 12px}
input[type=search]{width:230px}label{display:flex;align-items:center;gap:8px;font-size:13px}input[type=range]{width:110px;padding:0;accent-color:var(--accent)}
button{cursor:pointer}button:hover{border-color:var(--accent)}button:focus-visible,input:focus-visible,select:focus-visible{outline:3px solid var(--accent);outline-offset:4px}
.toolbar .note{margin-left:auto;font-size:13px;color:var(--muted)}.grid{display:grid;grid-template-columns:repeat(auto-fill,minmax(calc(var(--size) + 28px),1fr));gap:15px}
.card{display:flex;min-width:0;flex-direction:column;align-items:center;color:var(--text);background:var(--panel);border:1px solid var(--line);border-radius:14px;padding:12px 10px 16px;transition:border-color .12s}
.card[hidden]{display:none}.art{display:grid;place-items:center;width:100%;min-height:calc(var(--size) + 8px);border-radius:9px;background:var(--art)}
.art img{width:var(--size);height:var(--size);object-fit:contain}.label{color:var(--muted);font:11px monospace;letter-spacing:.06em;margin-top:12px}.name{font-size:16px;font-weight:700;line-height:1.5;margin-top:2px;overflow-wrap:anywhere}
body[data-background=light]{--art:#e9e5dc}body[data-background=checker] .art{background-color:#d5d8d8;background-image:linear-gradient(45deg,#b7c0c2 25%,transparent 25%),linear-gradient(-45deg,#b7c0c2 25%,transparent 25%),linear-gradient(45deg,transparent 75%,#b7c0c2 75%),linear-gradient(-45deg,transparent 75%,#b7c0c2 75%);background-size:24px 24px;background-position:0 0,0 12px,12px -12px,-12px 0}
.empty{text-align:center;padding:50px}footer{border-top:1px solid var(--line);margin-top:28px;padding-top:16px;font-size:12px;color:var(--muted)}
dialog{border:1px solid var(--line);border-radius:18px;background:var(--panel);color:var(--text);padding:24px;max-width:calc(100vw - 24px);width:470px;text-align:center}dialog::backdrop{background:#050c10d9}
dialog .art{height:300px}dialog img{width:256px!important;height:256px!important}dialog h2{margin:16px 0 6px}dialog p{font-size:13px}dialog .control{margin-top:12px}
@media(max-width:600px){main{padding:24px 16px}.toolbar .note{width:100%;margin:0}.grid{grid-template-columns:repeat(2,minmax(0,1fr));gap:10px}.art img{width:min(100%,var(--size));height:auto;max-height:var(--size)}.art{min-height:100px}.name{font-size:14px}.version{float:none;display:block;margin-top:8px}}
@media print{:root{color-scheme:light;--bg:white;--panel:white;--text:#16272d;--muted:#51636b;--line:#b9c4c8;--size:112px}main{padding:0}.toolbar,.hint{display:none}.grid{grid-template-columns:repeat(5,1fr);gap:8px}.card{break-inside:avoid;padding:6px}.name{font-size:12px}.label{margin-top:5px}.art{background:transparent!important}.version{float:right}}
</style>
</head>
<body data-background="dark"><main>
<header><span class="eyebrow">ROLESHUFFLE / ROLE EMBLEMS</span><span class="version">v__VERSION__</span>
<h1>__TITLE__</h1><p>__SUBTITLE__</p><p class="hint">アイコンを選ぶと拡大表示できます。</p></header>
<section class="toolbar" aria-label="表示設定">
<input id="search" type="search" placeholder="役職名で検索" aria-label="役職名で検索">
<label>背景 <select id="background"><option value="dark">暗い背景</option><option value="light">明るい背景</option><option value="checker">透過を確認</option></select></label>
<label>サイズ <input id="size" type="range" min="96" max="224" step="16" value="160" aria-label="アイコンサイズ"></label>
<button id="save" class="control">一覧PNGを保存</button><span class="note" id="count">__COUNT__ / __COUNT__</span>
</section>
<section class="grid" aria-label="役職アイコン">__CARDS__</section>
<p id="empty" class="empty" hidden>一致する役職がありません。</p>
<footer>RoleShuffle · __TITLE__ · v__VERSION__</footer>
</main>
<dialog id="preview" aria-labelledby="preview-name"><div class="art"><img id="preview-image" width="256" height="256" alt=""></div><h2 id="preview-name"></h2><p id="preview-label"></p><button class="control" id="close">閉じる</button></dialog>
<script>
const cards=[...document.querySelectorAll('.card')],search=document.querySelector('#search'),dialog=document.querySelector('#preview');
search.addEventListener('input',()=>{const q=search.value.trim().toLowerCase();let visible=0;for(const card of cards){card.hidden=!card.dataset.search.includes(q);if(!card.hidden)visible++;}document.querySelector('#count').textContent=`${visible} / ${cards.length}`;document.querySelector('#empty').hidden=visible>0;});
document.querySelector('#background').addEventListener('change',e=>document.body.dataset.background=e.target.value);
document.querySelector('#size').addEventListener('input',e=>document.documentElement.style.setProperty('--size',e.target.value+'px'));
for(const card of cards)card.addEventListener('click',()=>{const source=card.querySelector('img'),image=document.querySelector('#preview-image');image.src=source.src;image.alt=source.alt;document.querySelector('#preview-name').textContent=source.alt;document.querySelector('#preview-label').textContent=card.querySelector('.label').textContent;dialog.showModal();});
document.querySelector('#close').addEventListener('click',()=>dialog.close());dialog.addEventListener('click',e=>{if(e.target===dialog){const r=dialog.getBoundingClientRect();if(e.clientX<r.left||e.clientX>r.right||e.clientY<r.top||e.clientY>r.bottom)dialog.close();}});
async function createSheet(){
 const selected=cards.filter(c=>!c.hidden),columns=6,width=1800,margin=48,gap=18,cellWidth=(width-margin*2-gap*(columns-1))/columns,cellHeight=278,top=176;
 const canvas=document.createElement('canvas');canvas.width=width;canvas.height=top+Math.ceil(selected.length/columns)*(cellHeight+gap)+50;const ctx=canvas.getContext('2d');
 ctx.fillStyle='#10191d';ctx.fillRect(0,0,canvas.width,canvas.height);ctx.fillStyle='#7fdfcf';ctx.font='bold 18px sans-serif';ctx.fillText('ROLESHUFFLE  /  v__VERSION__',margin,45);
 ctx.fillStyle='#f8f0dc';ctx.font='bold 42px sans-serif';ctx.fillText('__TITLE__',margin,100);ctx.fillStyle='#a8bcc2';ctx.font='22px sans-serif';ctx.fillText(`__SUBTITLE__   ${selected.length}点`,margin,140);
 for(let i=0;i<selected.length;i++){const card=selected[i],img=card.querySelector('img');await img.decode();const x=margin+(i%columns)*(cellWidth+gap),y=top+Math.floor(i/columns)*(cellHeight+gap);ctx.fillStyle='#1a272c';ctx.fillRect(x,y,cellWidth,cellHeight);ctx.drawImage(img,x+(cellWidth-200)/2,y+12,200,200);ctx.fillStyle='#a8bcc2';ctx.font='15px sans-serif';ctx.textAlign='center';ctx.fillText(card.querySelector('.label').textContent,x+cellWidth/2,y+237);ctx.fillStyle='#f8f0dc';ctx.font='bold 21px sans-serif';ctx.fillText(img.alt,x+cellWidth/2,y+263);}
 return canvas;
}
document.querySelector('#save').addEventListener('click',async()=>{const button=document.querySelector('#save');button.disabled=true;button.textContent='作成中…';try{const canvas=await createSheet();canvas.toBlob(blob=>{if(!blob)return;const url=URL.createObjectURL(blob),a=document.createElement('a');a.href=url;a.download='__EXPORT_NAME__.png';a.click();setTimeout(()=>URL.revokeObjectURL(url),10000);},'image/png');}finally{button.disabled=false;button.textContent='一覧PNGを保存';}});
</script>
</body></html>
'''


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    entries = roles()
    version = json.loads((ROOT / "package/manifest.json").read_text(encoding="utf-8-sig"))["version_number"]
    sources = [p for _, _, p in entries] + [ICONS / "Unrevealed.png"]
    before = {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
    build(False, entries, version)
    build(True, entries, version)
    assert before == {p: hashlib.sha256(p.read_bytes()).hexdigest() for p in sources}
    print("Approved artwork unchanged; public secret-name and secret-art checks passed.")


if __name__ == "__main__":
    main()
