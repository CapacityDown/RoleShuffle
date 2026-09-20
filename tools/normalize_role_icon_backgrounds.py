"""Normalize the colored interiors of the approved raster emblem set.

The selected artwork is immutable. Work on an RGBA copy after exterior-alpha
extraction; never color-key the whole image or regenerate characters/props.
"""
from __future__ import annotations

import json
from pathlib import Path

import numpy as np
from PIL import Image, ImageFilter


def category_palette(assets: Path) -> dict[str, tuple[str, str]]:
    data = json.loads((assets / "selected/prompts.json").read_text(encoding="utf-8"))
    categories = {role: (name, color) for name, color, roles in data["categories"] for role in roles}
    result = {}
    for asset in data["assets"]:
        category, color = categories[asset["role"]]
        assert (asset["category"], asset["background"]) == (category, color)
        result[asset["file"]] = (category, color)
    assert len(result) == data["count"]
    return result


def normalize(pixels: np.ndarray, category: str, color: str, name: str = "") -> tuple[np.ndarray, dict, Image.Image]:
    rgb = pixels[:, :, :3].astype(np.int16)
    high, low = rgb.max(axis=2), rgb.min(axis=2)
    eligible = (pixels[:, :, 3] > 240) & (high > 40) & (high < 180) & (high - low > 20)
    if category == "Hardship":
        eligible = (pixels[:, :, 3] > 240) & (high > 42) & (high < 120) & (high - low < 20)
    # The largest quantized color cluster is the flat interior, excluding the
    # black contours, cream frame/robot and bright orange ability illustrations.
    samples = rgb[eligible]
    quantized = samples // 8
    keys = quantized[:, 0] * 1024 + quantized[:, 1] * 32 + quantized[:, 2]
    mode = np.bincount(keys).argmax()
    original = np.median(samples[keys == mode], axis=0).astype(np.int16)
    distance = np.abs(rgb - original).max(axis=2)
    # Courier's generated gray ground has a wider range of background shading.
    # Its black outlines and cream/tan artwork remain outside this color range.
    tolerance = 42 if name == "13-Courier" else 24
    candidate = (distance <= tolerance) & (pixels[:, :, 3] > 240)

    # Remove isolated pixel noise without joining regions across dark contours.
    # Separate background pockets (for example between legs) remain eligible.
    regions = Image.fromarray(np.where(candidate, 255, 0).astype(np.uint8))
    opened = regions.filter(ImageFilter.MinFilter(3)).filter(ImageFilter.MaxFilter(3))
    retained = candidate & (np.asarray(opened) == 255)

    assert 0.08 < retained.mean() < 0.50, (category, original, retained.mean())
    # A subpixel transition at full resolution preserves antialiased contours.
    mask = Image.fromarray(retained.astype(np.uint8) * 255).filter(ImageFilter.GaussianBlur(0.55))
    weights = np.asarray(mask).astype(np.float32) / 255.0
    weights[pixels[:, :, 3] == 0] = 0
    target = np.array([int(color[i:i + 2], 16) for i in (1, 3, 5)], dtype=np.int16)
    result = pixels.copy()
    result[:, :, :3] = np.rint(rgb * (1 - weights[:, :, None]) + target * weights[:, :, None]).astype(np.uint8)
    assert np.array_equal(result[:, :, 3], pixels[:, :, 3])
    assert np.array_equal(result[weights == 0], pixels[weights == 0])
    assert np.all(result[weights == 1, :3] == target)
    return result, {
        "category": category,
        "background_color": color,
        "detected_background_rgb": original.tolist(),
        "background_color_tolerance": tolerance,
        "background_core_pixels": int(np.count_nonzero(weights == 1)),
        "background_mask_pixels": int(np.count_nonzero(weights)),
        "outside_mask_pixels_unchanged": True,
        "alpha_preserved_during_recolor": True,
    }, Image.fromarray(np.rint(weights * 255).astype(np.uint8))
