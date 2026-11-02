"""Crossbow geometry shared by 01b_trim.py, 02_rig.py and 03b_support.py (CH_LinhBang, C-08).

The Hunyuan3D crossbow is ~1.5 m long with 0.5 m ice limbs, against ~0.5 m arms: held in front it hides the left
arm and its butt runs through the chest. 01b_trim scales it down about the right-hand grip; the rig and the hold
read the scaled points from here so all three steps agree.
"""
from mathutils import Vector

# Rest-pose points of the source crossbow (Blender metres, character faces -Y), measured on the 01_lowpoly mesh.
SRC_TIP = Vector((-0.59, -0.89, 0.35))
SRC_BUTT = Vector((-0.42, 0.54, 0.88))
WRIST_R = Vector((-0.44, 0.02, 0.80))      # right wrist (RightHand bone head)

PIVOT = Vector((-0.47, -0.08, 0.70))       # where the right hand closes on the stock
SCALE = 1.0    # user decision: keep the source size (pistol stance, one hand); was 0.72 for the two-handed hold


def scaled(p):
    return PIVOT + (Vector(p) - PIVOT) * SCALE


TIP = scaled(SRC_TIP)
BUTT = scaled(SRC_BUTT)
