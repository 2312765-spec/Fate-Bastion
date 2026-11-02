"""Step 3 (C-08): CH_LinhBang clips – Idle (loop), Attack (raise the crossbow, aim, shoot, recoil), Spawn. 30 fps.

Run: blender --background --python build/03_anim.py -- --master LinhBang_master.blend
Same key convention as CungThu/Source~/build/03_anim.py: {bone: [(world axis, degrees), ...]} in the rest frame,
applied in order, relative to the parent's pose. X = character left, Y = back, Z = up. Arms hang ~60° below
horizontal: right arm Y+ 60 lifts it level, Z+ 90 swings it forward. The crossbow is rigid in the right hand and
points forward, 20° down, at rest.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_LinhBang_Rig"
FPS = 30

IDLE = {"RightLowerArm": [("X", -10)], "LeftUpperArm": [("Y", 8)], "LeftLowerArm": [("X", -12)],
        "Head": [("X", 2)]}


AIM_LIFT = 45     # arm ~15° below level: the crossbow sits at chest height, clear of the face
# Pistol stance: the whole body (Hips) turns left so the right shoulder leads toward the target, the right arm
# reaches straight out with the crossbow, the left arm hangs. 42° is inside the 40-45° asked for.
STANCE_TURN = 42
AIM_SWING = 90 - STANCE_TURN   # relative to the turned body, so the arm itself points at the target
BOW_YAW_FIX = 7   # at rest the crossbow points 7° to the right of the hand line (tip x -0.59 vs butt x -0.42)


def aim(tilt):
    """Right arm lifted and swung at the target, tilted by X (X- raises the muzzle). The hand undoes the lift, the
    swing and the stance turn, and removes the crossbow's own 7° yaw, so it points straight ahead and only follows
    the tilt. -23 brings it level (measured in the pistol stance)."""
    return {"RightUpperArm": [("Y", AIM_LIFT), ("Z", AIM_SWING), ("X", tilt)],
            "RightHand": [("Z", -STANCE_TURN + BOW_YAW_FIX), ("Z", -AIM_SWING), ("Y", -AIM_LIFT)]}


# Side-on body, head turned back to the target, left arm relaxed at the side.
LEFT_DOWN = {"LeftUpperArm": [("Y", 8)], "LeftLowerArm": [("X", -12)]}


# Spawn: a soldier's bow, then stands to attention. No knee bend: the long front tabard follows the torso only and
# any crouch pushes the knees through it.
BOW = {"Spine": [("X", 12)], "Chest": [("X", 6)], "Head": [("X", 16)],   # shallow: the tabard stretches on a deep bow
       "LeftUpperArm": [("Y", 10)], "RightLowerArm": [("X", -20)]}


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


BRACE = merge({"Hips": [("Z", STANCE_TURN)], "Head": [("Z", -STANCE_TURN + 5)]}, LEFT_DOWN)
RECOIL = merge({"Hips": [("Z", STANCE_TURN)], "Spine": [("X", -4)], "Head": [("Z", -STANCE_TURN + 5), ("X", -3)]},
               LEFT_DOWN)


CLIPS = {
    "LinhBang_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -2)], "RightLowerArm": [("X", -14)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "LinhBang_Attack": dict(loop=False, keys={
        1: IDLE,
        8: merge(BRACE, aim(-23)),
        12: merge(BRACE, aim(-24)),
        13: merge(BRACE, aim(-23)),
        15: merge(RECOIL, aim(-32)),
        24: IDLE}, hips={}, markers={"release": 13}),
    "LinhBang_Spawn": dict(loop=False, keys={
        1: BOW,
        10: BOW,
        20: merge({"Chest": [("X", -4)], "Head": [("X", -6)]}, IDLE),
        30: IDLE}, hips={}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim LinhBang]", *a)


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
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "LinhBang_"))]:
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
