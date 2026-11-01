"""Step 3 (C-08): CH_NguoiTuyet clips – Idle (loop), Attack (club overhead, slam the ground: Area), Spawn. 30 fps.

Run: blender --background --python build/03_anim.py -- --master NguoiTuyet_master.blend
Same key convention as CungThu/Source~/build/03_anim.py: {bone: [(world axis, degrees), ...]} in the rest frame,
applied in order, relative to the parent's pose. X = character left, Y = back, Z = up. Arms hang ~62° below
horizontal: right arm Y+ 62 lifts it level, Z+ 90 swings it forward, then X tilts it (X- up). The ice club is rigid
in the right hand and points straight ahead at rest.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_NguoiTuyet_Rig"
FPS = 30

IDLE = {"RightLowerArm": [("X", -8)], "LeftLowerArm": [("X", -8)], "Head": [("X", 2)]}
ARM_LIFT = 62


def club(arm_tilt, club_tilt):
    """Right arm level and forward, tilted by arm_tilt (X- raises it); the hand undoes the lift and the swing, so the
    club keeps its rest heading and follows the arm tilt plus club_tilt (X- = club head up/back)."""
    return {"RightUpperArm": [("Y", ARM_LIFT), ("Z", 90), ("X", arm_tilt)],
            "RightHand": [("X", club_tilt), ("Z", -90), ("Y", -ARM_LIFT)]}


# Wind-up: club over the head and behind it, body leans back; the slam folds the body forward over the impact.
WIND_UP = {"Spine": [("X", -8)], "Chest": [("X", -6)], "Head": [("X", -6)],
           "LeftUpperArm": [("Y", -25)], "LeftLowerArm": [("X", -20)]}
SLAM = {"Spine": [("X", 14)], "Chest": [("X", 8)], "Head": [("X", 6)],
        "LeftUpperArm": [("Y", -10), ("X", -20)], "LeftLowerArm": [("X", -15)]}

# Landing crouch: thigh 0.23 m, shin 0.17 m: 0.40 - 0.40 cos35 = 0.072 m hip drop keeps the feet on the ground.
CROUCH_LEGS = {s + b: r for s in ("Left", "Right") for b, r in
               (("UpperLeg", [("X", -35)]), ("LowerLeg", [("X", 70)]), ("Foot", [("X", -35)]))}
CROUCH_DROP = 0.072


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


CLIPS = {
    "NguoiTuyet_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -3)], "Spine": [("X", -1)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "NguoiTuyet_Attack": dict(loop=False, keys={
        1: IDLE,
        10: merge(WIND_UP, club(-110, -25)),
        15: merge(SLAM, club(25, 30)),
        20: merge(SLAM, club(22, 28)),
        30: IDLE}, hips={}, markers={"release": 15}),
    "NguoiTuyet_Spawn": dict(loop=False, keys={
        1: merge(CROUCH_LEGS, {"Spine": [("X", -15)], "Head": [("X", 8)],
                               "LeftUpperArm": [("Y", -20)], "RightUpperArm": [("Y", 20)]}),
        16: merge({"Chest": [("X", -5)], "Head": [("X", -6)]}, IDLE),
        30: IDLE}, hips={1: CROUCH_DROP, 16: 0.0, 30: 0.0}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim NguoiTuyet]", *a)


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
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "NguoiTuyet_"))]:
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
