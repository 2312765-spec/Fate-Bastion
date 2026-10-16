"""
Re-score a world_gate silhouette inside the rows the reference actually covers.

The sheets cut off at the boots (front/side/back mattes end at canvas row 461/437), so everything the
model has below that row counts as "model-only" in world_gate even when it is right. This recomputes
IoU and the 10 band widths above CUT_ROW only. Plain Python (Pillow + numpy).

    python build/gate_clip.py review/02_blockout/world_front/sil_az000.png ref/front_matte.png 461
"""
import json, sys
import numpy as np
from PIL import Image


def main(sil_path, matte_path, cut_row, top_row=0):
    matte = np.array(Image.open(matte_path).getchannel("A")) > 16
    sil_img = Image.open(sil_path).getchannel("A").resize((matte.shape[1], matte.shape[0]), Image.BOX)
    sil = np.array(sil_img) > 127
    m, s = matte[top_row:cut_row], sil[top_row:cut_row]   # rows the reference covers
    inter, union = (m & s).sum(), (m | s).sum()
    rows = np.nonzero(m.any(axis=1))[0]
    top, bot = rows.min(), rows.max()
    bands = []
    for i in range(10):
        r0 = top + (bot - top) * i // 10
        r1 = top + (bot - top) * (i + 1) // 10
        wm = m[r0:r1].sum(axis=1).mean()
        ws = s[r0:r1].sum(axis=1).mean()
        bands.append(round(float((ws - wm) / (bot - top)), 4))
    out = {"rows": [top_row, cut_row], "iou": round(float(inter / union), 4),
           "ref_only_pct": round(float((m & ~s).sum() / union * 100), 2),
           "model_only_pct": round(float((s & ~m).sum() / union * 100), 2), "bands": bands}
    print(json.dumps(out))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]) if len(sys.argv) > 4 else 0)
