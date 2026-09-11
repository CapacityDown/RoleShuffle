"""Extract the black exterior of the approved hexagonal role emblems.

Artwork, the cream frame and the colored interior stay intact; only alpha is
edited in the full-size output. The original selected PNGs are never overwritten.
"""
from __future__ import annotations

import hashlib
import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageOps

ROOT = Path(__file__).resolve().parents[1]
ASSETS = ROOT / "Assets/role-emblems-semibot-v1"
OUT = ASSETS / "transparent"
MASTERS = OUT / "masters"
RUNTIME = OUT / "runtime"
PREVIEWS = OUT / "previews"
BLACK_LIMIT = 64
RUNTIME_SIZE = 256


def outside_mask(bright: np.ndarray) -> np.ndarray:
    height, width = bright.shape
    candidates = Image.fromarray(np.where(bright, 0, 255).astype(np.uint8)).copy()
    for corner in ((0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)):
        if candidates.getpixel(corner) == 255:
            ImageDraw.floodfill(candidates, corner, 128, thresh=0)
    return np.array(candidates) == 128


def sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def extract(path: Path) -> dict:
    source_hash = sha256(path)
    with Image.open(path) as source:
        pixels = np.array(source.convert("RGBA"))
    height, width = pixels.shape[:2]
    assert height == width, path

    # Do not color-key the entire image: dark details inside the closed frame
    # have the same colors as the background and must remain fully opaque.
    bright = pixels[:, :, :3].max(axis=2) > BLACK_LIMIT
    background = outside_mask(bright)
    interior = (slice(int(height * .25), int(height * .75)),
                slice(int(width * .20), int(width * .80)))
    closing_size = 0
    if background[interior].any():
        # Props can cross the cream border and connect their black outline to
        # the exterior. Seal these narrow channels before locating the outside.
        closing_size = 31
        barrier = Image.fromarray(np.where(bright, 255, 0).astype(np.uint8))
        barrier = ImageOps.expand(barrier, border=closing_size, fill=0)
        barrier = barrier.filter(ImageFilter.MaxFilter(closing_size)).filter(ImageFilter.MinFilter(closing_size))
        barrier = barrier.crop((closing_size, closing_size, width + closing_size, height + closing_size))
        background = outside_mask(np.array(barrier) != 0)
    assert not background[interior].any(), (path.name, "interior outline lost")
    removed_fraction = float(background.mean())
    assert 0.20 < removed_fraction < 0.55, (path.name, removed_fraction)
    assert not background[height // 2, width // 2], path

    rgba = pixels.copy()
    rgba[background, 3] = 0
    # Preserve all RGB values and every interior alpha value exactly.
    assert np.array_equal(rgba[:, :, :3], pixels[:, :, :3])
    assert np.array_equal(rgba[~background], pixels[~background])
    border = np.concatenate((rgba[0, :, 3], rgba[-1, :, 3], rgba[:, 0, 3], rgba[:, -1, 3]))
    assert not border.any(), (path.name, "non-transparent outer edge")

    master = Image.fromarray(rgba)
    master_path = MASTERS / path.name
    runtime_path = RUNTIME / path.name
    master.save(master_path, optimize=True)
    # Pillow resamples RGBA through premultiplied alpha. Downscaling therefore
    # produces smooth silhouette edges without bleeding the black matte inward.
    runtime = master.resize((RUNTIME_SIZE, RUNTIME_SIZE), Image.Resampling.LANCZOS)
    runtime.save(runtime_path, optimize=True)

    with Image.open(runtime_path) as saved:
        assert saved.mode == "RGBA" and saved.size == (RUNTIME_SIZE, RUNTIME_SIZE)
        alpha = np.array(saved.getchannel("A"))
        assert alpha.min() == 0 and alpha.max() == 255
        assert np.any((alpha > 0) & (alpha < 255)), path
        assert alpha[RUNTIME_SIZE // 2, RUNTIME_SIZE // 2] == 255
    assert sha256(path) == source_hash, path
    return {
        "name": path.stem,
        "source": str(path.relative_to(ROOT)),
        "source_sha256": source_hash,
        "master_sha256": sha256(master_path),
        "runtime_sha256": sha256(runtime_path),
        "source_size": [width, height],
        "runtime_size": [RUNTIME_SIZE, RUNTIME_SIZE],
        "transparent_fraction": round(removed_fraction, 6),
        "outline_channel_closing": closing_size,
        "runtime_edge_pixels": int(np.count_nonzero((alpha > 0) & (alpha < 255))),
        "rgb_pixels_unchanged": True,
        "central_interior_opaque": True,
    }


def previews(paths: list[Path]) -> None:
    # Each row compares the untouched source with the runtime PNG over two
    # backgrounds. These composites are review artifacts, never runtime assets.
    icon_size, cell_w, cell_h = 112, 360, 160
    for start in range(0, len(paths), 12):
        page = paths[start : start + 12]
        rows = (len(page) + 3) // 4
        sheet = Image.new("RGB", (4 * cell_w, rows * cell_h), "#727782")
        draw = ImageDraw.Draw(sheet)
        for index, path in enumerate(page):
            x, y = (index % 4) * cell_w, (index // 4) * cell_h
            draw.text((x + 7, y + 5), path.stem, fill="white")
            with Image.open(path) as source:
                original = source.convert("RGB").resize((icon_size, icon_size), Image.Resampling.LANCZOS)
            sheet.paste(original, (x + 4, y + 25))
            with Image.open(RUNTIME / path.name) as saved:
                icon = saved.resize((icon_size, icon_size), Image.Resampling.LANCZOS)
            for column, color in ((1, "#f4f2ed"), (2, "#182d43")):
                canvas = Image.new("RGBA", (icon_size, icon_size), color)
                canvas.alpha_composite(icon)
                sheet.paste(canvas.convert("RGB"), (x + 4 + column * 118, y + 25))
            draw.text((x + 5, y + 140), "source         light           dark", fill="white")
        sheet.save(PREVIEWS / f"comparison-{start // 12 + 1:02}.png")


def main() -> None:
    paths = sorted((ASSETS / "selected").glob("*.png"), key=lambda p: int(p.stem.split("-", 1)[0]))
    paths.append(ASSETS / "unrevealed/Unrevealed.png")
    assert len(paths) == 43 and len({p.name for p in paths}) == 43
    for directory in (MASTERS, RUNTIME, PREVIEWS):
        directory.mkdir(parents=True, exist_ok=True)
    records = []
    for path in paths:
        record = extract(path)
        records.append(record)
        print(f"{path.stem}: exterior {record['transparent_fraction']:.1%}, RGB preserved", flush=True)
    report = {
        "method": "Perimeter-connected near-black exterior to transparent alpha",
        "generator": "Python / Pillow / NumPy",
        "black_limit": BLACK_LIMIT,
        "count": len(records),
        "assets": records,
    }
    (OUT / "manifest.json").write_text(json.dumps(report, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    previews(paths)
    print(f"Validated {len(records)} transparent masters and runtime PNGs. Output: {OUT}", flush=True)


if __name__ == "__main__":
    main()
