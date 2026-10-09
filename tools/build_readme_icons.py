"""Build public 64px README thumbnails from the approved runtime role icons.
The game assets are read-only inputs; secret artwork is never exported here.
"""
from pathlib import Path
from PIL import Image
import hashlib

ROOT = Path(__file__).resolve().parents[1]
SOURCE = ROOT / "Assets/role-emblems-semibot-v1/transparent/runtime"
OUTPUT = ROOT / "docs/icons"


def main():
    public = sorted(p for p in SOURCE.glob("[0-9]*-*.png")
                    if int(p.stem.split("-", 1)[0]) < 1000)
    from build_icon_catalog import roles
    assert len(public) == sum(role_id < 1000 for role_id, _, _ in roles())
    images = [(p, f"{int(p.stem.split('-', 1)[0]):02}.png") for p in public]
    images.append((SOURCE / "Unrevealed.png", "unknown.png"))
    OUTPUT.mkdir(parents=True, exist_ok=True)
    for source, name in images:
        before = hashlib.sha256(source.read_bytes()).digest()
        with Image.open(source) as image:
            assert image.size == (256, 256)
            # Premultiplied alpha keeps the transparent badge edges clean.
            thumbnail = image.convert("RGBA").convert("RGBa").resize(
                (64, 64), Image.Resampling.LANCZOS).convert("RGBA")
            thumbnail.save(OUTPUT / name, optimize=True)
        assert hashlib.sha256(source.read_bytes()).digest() == before
        with Image.open(OUTPUT / name) as saved:
            assert saved.size == (64, 64) and saved.mode == "RGBA"
    assert set(p.name for p in OUTPUT.glob("*.png")) == {name for _, name in images}
    print(f"Verified {len(images)} public README thumbnails; original game artwork unchanged.")


if __name__ == "__main__":
    main()

