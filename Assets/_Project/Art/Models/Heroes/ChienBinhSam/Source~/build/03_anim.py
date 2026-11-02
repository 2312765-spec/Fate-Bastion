"""Step 3 (C-08): CH_ChienBinhSam clips – Idle (loop), Attack (overhead diagonal slash: Melee), Spawn. 30 fps.

Run: blender --background --python build/03_anim.py -- --master ChienBinhSam_master.blend
Same key convention as CungThu/Source~/build/03_anim.py: {bone: [(world axis, degrees), ...]} in the rest frame,
applied in order, relative to the parent's pose. X = character left, Y = back, Z = up. Arms hang ~48° below
horizontal: right arm Y+ raises it (48 = level, ~140 = overhead), Z+ swings it forward. The sword is rigid in the
right hand and simply follows the arm, which is what a slash needs. Spine Z+ brings the right shoulder forward.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_ChienBinhSam_Rig"
FPS = 30

IDLE = {"RightLowerArm": [("X", -8)], "LeftLowerArm": [("X", -10)], "Head": [("X", 2)]}

# Wind-up: sword raised high over the right shoulder, body coiled back (right shoulder away), left arm forward.
WIND_UP = {"Spine": [("Z", -18)], "Chest": [("Z", -8), ("X", -4)], "Head": [("Z", 15)],
           "RightUpperArm": [("Y", 135), ("Z", -15)], "RightLowerArm": [("X", -60)],
           "LeftUpperArm": [("Y", -30), ("Z", -45)], "LeftLowerArm": [("X", -30)]}
# Slash: arm driven forward and down across the body, the blade ends low in front of the left knee.
SLASH = {"Spine": [("Z", 22), ("X", 8)], "Chest": [("Z", 10), ("X", 6)], "Head": [("Z", -15), ("X", 4)],
         "RightUpperArm": [("Y", 55), ("Z", 110), ("X", 25)], "RightLowerArm": [("X", -10)],
         "LeftUpperArm": [("Y", -10), ("Z", 20)], "LeftLowerArm": [("X", -15)]}
FOLLOW = {"Spine": [("Z", 20), ("X", 6)], "Chest": [("Z", 8), ("X", 5)], "Head": [("Z", -12), ("X", 3)],
          "RightUpperArm": [("Y", 45), ("Z", 115), ("X", 32)], "RightLowerArm": [("X", -8)],
          "LeftUpperArm": [("Y", -10), ("Z", 20)], "LeftLowerArm": [("X", -15)]}

# Spawn: a knight's salute – sword raised in front of the face, then back to guard. No crouch: the tabard panels
# between the legs follow the torso only.
SALUTE = {"Spine": [("X", 6)], "Head": [("X", 8)],
          "RightUpperArm": [("Y", 60), ("Z", 75)], "RightLowerArm": [("Y", -60), ("Z", 110), ("Y", 60)]}


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


CLIPS = {
    "ChienBinhSam_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -2)], "RightLowerArm": [("X", -12)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "ChienBinhSam_Attack": dict(loop=False, keys={
        1: IDLE,
        9: WIND_UP,
        13: SLASH,
        18: FOLLOW,
        27: IDLE}, hips={}, markers={"release": 13}),
    "ChienBinhSam_Spawn": dict(loop=False, keys={
        1: SALUTE,
        12: SALUTE,
        22: merge({"Chest": [("X", -3)], "Head": [("X", -5)]}, IDLE),
        30: IDLE}, hips={}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim ChienBinhSam]", *a)


def rot(rest, rots):
    R = Matrix.Identity(3)
    for axis, deg in rots:
        R = Matrix.Rotation(math.radians(deg), 3, axis) @ R
    L = rest.to_3x3()
    return (L.inverted() @ R @ L).to_quaternion()


def build():
    arm = bpy.data.objects[RIG]
    bpy.context.scene.render.fps = FPS
    arm.animation_data_create()
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "ChienBinhSam_"))]:
        bpy.data.actions.remove(act)
    for name, clip in CLIPS.items():
        act = bpy.data.actions.new(name)
        act.use_fake_user = True
        arm.animation_data.action = act
        frames = sorted(clip["keys"])
        for f in frames:
            pose = clip["keys"][f]
            for pb in arm.pose.bones:
                pb.rotation_mode = "QUATERNION"
                pb.rotation_quaternion = rot(pb.bone.matrix_local, pose.get(pb.name, []))
                pb.keyframe_insert("rotation_quaternion", frame=f)
            hips = arm.pose.bones["Hips"]
            # Hips points up, so its local Y is world Z: a drop is a negative local Y.
            hips.location = (0.0, -clip["hips"].get(f, 0.0), 0.0)
            hips.keyframe_insert("location", frame=f)
        act.use_frame_range = True
        act.frame_start, act.frame_end = frames[0], frames[-1]
        act.use_cyclic = clip["loop"]
        act["loop"] = clip["loop"]
        for m, f in clip["markers"].items():
            act.pose_markers.new(m).frame = f
        log(name, "frames", frames[0], "-", frames[-1], "(%.2f s)" % ((frames[-1] - frames[0]) / FPS),
            "markers", clip["markers"])
    arm.animation_data.action = None
    for pb in arm.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(a.master))
    build()
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(a.master))


main()
