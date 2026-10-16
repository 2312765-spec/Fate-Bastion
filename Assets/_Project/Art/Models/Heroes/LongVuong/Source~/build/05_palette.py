"""
Phase 5 (low-poly revision): write the 4 x 4 colour palette that every face of CH_LongVuong_Body samples.

    python build/05_palette.py

Replaces the 2k bake: in the low-poly style each face is one flat colour, so the hero needs three
64 x 64 PNGs (16 px swatches, sampled with nearest/point filtering):
  CH_LongVuong_Palette_basecolor.png  sRGB
  CH_LongVuong_Palette_metallic.png   Non-Color, R = metallic, A = smoothness (Unity URP Lit layout)
  CH_LongVuong_Palette_emission.png   sRGB, black except the glow swatch
Swatch indices must match SWATCH / FLAME_SWATCH in build/04_topology.py.
"""
import os
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(os.path.dirname(HERE), "textures")
GRID, CELL = 4, 16

# index: (name, sRGB hex, metallic, smoothness, emissive)
SWATCHES = {
    0: ("silver armour", "#C9CED6", 0.75, 0.62, False),
    1: ("dark steel", "#3B4049", 0.75, 0.50, False),
    2: ("ember motif", "#9C1E17", 0.30, 0.45, False),      # delta: fire red where the reference is pink
    3: ("flame tongues", "#C8361A", 0.20, 0.40, False),    # delta: faceted flames on the scale plates
    4: ("antler", "#EADAD2", 0.0, 0.40, False),
    5: ("skin", "#E8D2C6", 0.0, 0.40, False),
    6: ("cloth", "#E7E4DF", 0.0, 0.25, False),
    7: ("hair", "#EDEBEA", 0.0, 0.45, False),
    8: ("fur", "#F0ECE6", 0.0, 0.10, False),
    9: ("glow", "#FF7A1F", 0.0, 0.50, True),
}


def rgb(h):
    return tuple(int(h[i:i + 2], 16) for i in (1, 3, 5))


def main():
    os.makedirs(OUT, exist_ok=True)
    size = GRID * CELL
    base = Image.new("RGB", (size, size), (255, 0, 255))        # unused swatches: magenta, easy to spot
    metal = Image.new("RGBA", (size, size), (0, 0, 0, 128))
    emit = Image.new("RGB", (size, size), (0, 0, 0))
    for i, (name, hx, m, s, e) in SWATCHES.items():
        x, y = (i % GRID) * CELL, (i // GRID) * CELL
        box = (x, y, x + CELL, y + CELL)
        base.paste(rgb(hx), box)
        metal.paste((int(m * 255), 0, 0, int(s * 255)), box)
        if e:
            emit.paste(rgb(hx), box)
    base.save(os.path.join(OUT, "CH_LongVuong_Palette_basecolor.png"))
    metal.save(os.path.join(OUT, "CH_LongVuong_Palette_metallic.png"))
    emit.save(os.path.join(OUT, "CH_LongVuong_Palette_emission.png"))
    print("[05_palette] wrote", OUT)


if __name__ == "__main__":
    main()
