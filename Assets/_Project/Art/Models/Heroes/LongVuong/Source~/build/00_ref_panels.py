"""
Phase 0 helper (plain Python, not Blender): crop the two reference sheets into separate views that share
one canvas, one scale and one ground line, and write clean silhouette mattes (alpha) for the gates.

    python build/00_ref_panels.py

Sheets (711 x 431 px, near-orthographic studio renders on a flat grey background):
  sheet_front_threequarter.png : front view (x 0-400) + three-quarter concept (x 400-711)
  sheet_side_back.png          : left-side view (x 0-330) + back view (x 310-711), framed 24 px lower

Canvas: 580 px tall. Sheet A row r -> canvas r + 30, sheet B row r -> canvas r + 6 (crowns line up).
Ground row 565 comes from front_full_src.png (the only view that shows the ground): there the gap from
the bottom of the lower robe to the soles is 0.316 x (crown -> lower-robe bottom); applied to the sheets
(crown 52, lower-robe bottom 442) it puts the ground at 565.

front_full_src.png (491 x 617, dark studio background, game-model render, mild perspective) is kept in
its own scale as front_full.png / front_full_matte.png: crown row 80, ground row 600, axis col 258.
"""
import os
import numpy as np
from PIL import Image
from scipy import ndimage

HERE = os.path.dirname(os.path.abspath(__file__))
REF = os.path.join(os.path.dirname(HERE), "ref")
CANVAS_H = 580
GROUND_ROW = 565          # derived from front_full_src.png, see docstring
BG = 126                  # measured background grey (corners of both sheets: 124-129)

# (output name, sheet, x0, x1, row offset, title boxes cleared outright, caption boxes over the subject)
# Boxes are (x0, y0, x1, y1) in sheet px. Captions crossing the costume are rebuilt column by column
# from the mask just above and below the box (OR), which keeps the arm and torso and drops the gap.
PANELS = [
    ("front", "sheet_front_threequarter.png", 0, 400, 30, [(18, 4, 172, 34)], [(64, 199, 230, 231)]),
    ("threequarter", "sheet_front_threequarter.png", 400, 711, 30, [], []),
    ("side", "sheet_side_back.png", 0, 312, 6, [(18, 4, 182, 31)], [(64, 199, 230, 231)]),
    ("back", "sheet_side_back.png", 310, 710, 6, [], []),
]


def subject_mask(rgb, titles, captions):
    a = rgb.astype(np.float32)
    lum = a.mean(axis=2)
    chroma = a.max(axis=2) - a.min(axis=2)
    # The background is flat but not one grey (panels of slightly different value, ruler lines);
    # the costume is textured or tinted. Local standard deviation + chroma separate them.
    # 3 px window: a 1-2 px ruler line then stays a thin band the opening below removes
    # (measured: flat background std 0.5, costume std 15-25, costume chroma 11-18).
    mean = ndimage.uniform_filter(lum, 3)
    std = np.sqrt(np.maximum(ndimage.uniform_filter(lum * lum, 3) - mean * mean, 0))
    m = (std > 8) | (chroma > 9) | (np.abs(lum - BG) > 20)   # last term: smooth white hair and fur, dark gauntlets
    for x0, y0, x1, y1 in titles:
        m[y0:y1, x0:x1] = False
    m = ndimage.binary_opening(m, structure=np.ones((5, 5), bool))   # ruler lines, panel edges (<= 4 px), noise
    for x0, y0, x1, y1 in captions:
        above = m[y0 - 3, x0:x1]
        below = m[y1 + 3, x0:x1]
        m[y0:y1, x0:x1] = (above | below)[None, :]
    m = ndimage.binary_closing(m, iterations=3)
    m = ndimage.binary_fill_holes(m)
    lab, n = ndimage.label(m)
    if n:
        sizes = ndimage.sum(m, lab, range(1, n + 1))
        keep = [i + 1 for i, s in enumerate(sizes) if s > 0.02 * sizes.max()]
        m = np.isin(lab, keep)
    return m


def dark_studio_mask(rgb):
    """front_full_src.png: subject on a dark gradient (24-40) with a soft floor shadow."""
    a = rgb.astype(np.float32)
    lum = a.mean(axis=2)
    mean = ndimage.uniform_filter(lum, 3)
    std = np.sqrt(np.maximum(ndimage.uniform_filter(lum * lum, 3) - mean * mean, 0))
    m = (lum > 58) | (std > 9)
    m = ndimage.binary_opening(m, structure=np.ones((3, 3), bool))
    m = ndimage.binary_closing(m, iterations=3)
    m = ndimage.binary_fill_holes(m)
    lab, n = ndimage.label(m)
    sizes = ndimage.sum(m, lab, range(1, n + 1))
    return np.isin(lab, [i + 1 for i, sz in enumerate(sizes) if sz > 0.02 * sizes.max()])


def main():
    rgb = np.array(Image.open(os.path.join(REF, "front_full_src.png")).convert("RGB"))
    mask = dark_studio_mask(rgb)
    Image.fromarray(rgb).save(os.path.join(REF, "front_full.png"))
    Image.fromarray(np.dstack([rgb, (mask * 255).astype(np.uint8)])).save(os.path.join(REF, "front_full_matte.png"))
    ys, xs = np.nonzero(mask)
    print(f"front_full: {rgb.shape[1]}x{rgb.shape[0]}, subject x[{xs.min()},{xs.max()}] y[{ys.min()},{ys.max()}]")

    for name, sheet, x0, x1, off, titles, captions in PANELS:
        rgb = np.array(Image.open(os.path.join(REF, sheet)).convert("RGB"))
        mask = subject_mask(rgb, titles, captions)[:, x0:x1]
        crop = rgb[:, x0:x1]
        h = crop.shape[0]
        canvas = np.full((CANVAS_H, x1 - x0, 3), BG, np.uint8)
        alpha = np.zeros((CANVAS_H, x1 - x0), np.uint8)
        rows = slice(off, min(CANVAS_H, off + h))
        canvas[rows] = crop[: rows.stop - rows.start]
        alpha[rows] = (mask[: rows.stop - rows.start] * 255).astype(np.uint8)
        Image.fromarray(canvas).save(os.path.join(REF, name + ".png"))
        Image.fromarray(np.dstack([canvas, alpha])).save(os.path.join(REF, name + "_matte.png"))
        ys, xs = np.nonzero(alpha)
        print(f"{name}: canvas {x1 - x0}x{CANVAS_H}, subject x[{xs.min()},{xs.max()}] y[{ys.min()},{ys.max()}]")


if __name__ == "__main__":
    main()
