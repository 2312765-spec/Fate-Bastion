"""Step 3 (C-08): CH_ThanhNu clips – Idle (loop), Attack (raise the sun staff, point it forward), Spawn. 30 fps.

Run: blender --background --python build/03_anim.py -- --master ThanhNu_master.blend
Same key convention as CungThu/Source~/build/03_anim.py: {bone: [(world axis, degrees), ...]} in the rest frame,
applied in order, relative to the parent's pose. X = character left, Y = back, Z = up. She holds the staff in the
LEFT hand: left arm Y- 52 lifts it level, Z- swings it forward, X tilts it (X- up). The hand undoes the lift and
the swing, so the staff keeps standing upright and only follows the tilt.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_ThanhNu_Rig"
FPS = 30
ARM_LIFT = 52

IDLE = {"LeftLowerArm": [("X", -8)], "RightLowerArm": [("X", -10)], "Head": [("X", 2)]}


def staff(swing, tilt):
    """Left arm lifted level, swung forward by swing (90 = straight ahead), tilted by X; staff stays upright and
    follows the tilt (X+ leans the sun disc toward the target)."""
    return {"LeftUpperArm": [("Y", -ARM_LIFT), ("Z", -swing), ("X", tilt)],
            "LeftHand": [("Z", swing), ("Y", ARM_LIFT)]}


# Raise: staff lifted high beside her, right palm open toward the sky; point: staff tilts toward the target,
# right hand opens forward as the blessing goes out.
RAISE = {"Spine": [("X", -4)], "Chest": [("X", -5)], "Head": [("X", -8)],
         "RightUpperArm": [("Y", 20)], "RightLowerArm": [("X", -40)]}
POINT = {"Spine": [("X", 6)], "Chest": [("X", 4)], "Head": [("X", 4)],
         "RightUpperArm": [("Y", 25), ("Z", 40)], "RightLowerArm": [("X", -20)]}

# Spawn: a gentle bow, then stands up. No knee bend: the ankle-length dress tears on any crouch (QA pose 4).
BOW = {"Spine": [("X", 12)], "Chest": [("X", 6)], "Head": [("X", 14)],
       "RightLowerArm": [("X", -25)]}


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


CLIPS = {
    "ThanhNu_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -2)], "LeftLowerArm": [("X", -12)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "ThanhNu_Attack": dict(loop=False, keys={
        1: IDLE,
        9: merge(RAISE, staff(45, -45)),
        14: merge(POINT, staff(90, 25)),
        19: merge(POINT, staff(88, 22)),
        28: IDLE}, hips={}, markers={"release": 14}),
    "ThanhNu_Spawn": dict(loop=False, keys={
        1: BOW,
        10: BOW,
        20: merge({"Chest": [("X", -4)], "Head": [("X", -6)]}, IDLE),
        30: IDLE}, hips={}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim ThanhNu]", *a)


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
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "ThanhNu_"))]:
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
