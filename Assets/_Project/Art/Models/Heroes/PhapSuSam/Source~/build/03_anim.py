"""Step 3 (C-08): CH_PhapSuSam clips – Idle (loop), Attack (raise the staff, thrust it: Area cast), Spawn. 30 fps.

Run: blender --background --python build/03_anim.py -- --master PhapSuSam_master.blend
Same key convention as CungThu/Source~/build/03_anim.py: {bone: [(world axis, degrees), ...]} in the rest frame,
applied in order, relative to the parent's pose. X = character left, Y = back, Z = up. Right arm: Y+ raises it,
then Z+ swings it forward. The staff is rigid in the right hand and points forward, 18° up, at rest.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_PhapSuSam_Rig"
FPS = 30

IDLE = {"RightLowerArm": [("X", -12)], "LeftUpperArm": [("Y", 12)], "LeftLowerArm": [("X", -15)],
        "Head": [("X", 2)]}

# Right arm horizontal and forward (Y 45, Z 90), then tilted by X. The hand undoes the Y/Z part so the staff keeps
# its rest heading (forward) and only follows the tilt: X- raises the flame, X+ lowers it toward the target.
def staff_arm(tilt, swing=90):
    """Right arm raised to horizontal, swung forward by swing (90 = straight ahead), then tilted; the hand undoes
    the raise and the swing so the staff keeps pointing forward and only follows the tilt."""
    return {"RightUpperArm": [("Y", 51), ("Z", swing), ("X", tilt)], "RightHand": [("Z", -swing), ("Y", -51)]}


WIND_UP = {"Spine": [("Z", 10)], "Chest": [("X", -6)], "Head": [("X", -5)],
           "LeftUpperArm": [("Y", -20)], "LeftLowerArm": [("X", -20)]}
THRUST = {"Spine": [("Z", 15)], "Chest": [("X", 10)], "Head": [("X", 4)],
          "LeftUpperArm": [("Y", -35), ("Z", -50)], "LeftLowerArm": [("X", -10)]}

# Spawn: a bow, then stands up. No knee bend: the long robe follows the hips only and a crouch pushes the knees
# through its front (same as Tu sĩ and Lính Băng).
BOW = {"Spine": [("X", 14)], "Chest": [("X", 8)], "Head": [("X", 14)],
       "LeftLowerArm": [("X", -20)], "RightLowerArm": [("X", -15)]}


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


CLIPS = {
    "PhapSuSam_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -2)], "RightLowerArm": [("X", -16)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "PhapSuSam_Attack": dict(loop=False, keys={
        1: IDLE,
        9: merge(WIND_UP, staff_arm(-40, swing=60)),
        13: merge(THRUST, staff_arm(20)),
        18: merge(THRUST, staff_arm(18)),
        28: IDLE}, hips={}, markers={"release": 13}),
    "PhapSuSam_Spawn": dict(loop=False, keys={
        1: BOW,
        10: BOW,
        20: merge({"Chest": [("X", -4)], "Head": [("X", -6)]}, IDLE),
        30: IDLE}, hips={}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim PhapSuSam]", *a)


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
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "PhapSuSam_"))]:
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
