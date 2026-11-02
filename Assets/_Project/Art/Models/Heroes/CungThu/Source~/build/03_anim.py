"""Step 3 (C-08): CH_CungThu clips – Idle (loop), Attack (draw and loose), Spawn (lands, stands up). 30 fps.

Run: blender --background --python build/03_anim.py -- --master CungThu_master.blend
Re-runnable: rebuilds the three actions and removes the QA_Poses rig test.

Gameplay timing is the attack timer (CLAUDE.md 7); clips only carry event markers and Unity speeds Attack up
with attack speed. Same key convention as LongVuong 08_anim.py: a key is {bone: [(world axis, degrees), ...]},
rotations about WORLD axes (X = character left, Y = back, Z = up) applied in order, relative to the parent's
pose. Rest = modelled A-pose (arms ~45° down). Left arm: Y- raises it. Right arm: Y+ raises it.
Legs/arms pointing down: X- swings them forward. Z- turns the left shoulder forward.
"""
import sys, os, math, argparse
import bpy
from mathutils import Matrix

RIG = "CH_CungThu_Rig"
FPS = 30

# The bow hangs from the left hand; lowering the left arm would push it into the leg, so the arm stays at rest
# and the forearm swings slightly forward instead.
IDLE = {"LeftLowerArm": [("X", -12)], "RightUpperArm": [("Y", -15)], "RightLowerArm": [("X", -15)],
        "Head": [("X", 2)]}

# Aim: torso turns the left shoulder 30° forward; left arm horizontal and forward; the hand undoes the arm's
# rotation and turns the bow 90° so it stands upright in the Y-Z plane with the string toward the archer.
TWIST = {"Spine": [("Z", -15)], "Chest": [("Z", -15)], "Head": [("Z", 25)]}
BOW_ARM = {"LeftUpperArm": [("Y", -45), ("Z", -60)], "LeftHand": [("Y", 45)]}
# Keys rotate in the rest frame, before the parent's pose. To bend the elbow about the world vertical once the
# upper arm is raised (Y 45), conjugate: undo the raise, turn about Z, raise again.
def elbow(z_deg):
    return [("Y", 45), ("Z", z_deg), ("Y", -45)]


DRAW_REACH = {"RightUpperArm": [("Y", 45), ("Z", 60)], "RightLowerArm": elbow(20)}   # hand to the string
FULL_DRAW = {"RightUpperArm": [("Y", 45)], "RightLowerArm": elbow(150)}              # hand at the chin
LOOSE = {"RightUpperArm": [("Y", 50), ("Z", -35)], "RightLowerArm": elbow(60)}       # hand flies back

# Landing crouch: thigh 35° forward, knee 70°, foot back to flat; the hips drop by the leg shortening
# (thigh 0.25 m, shin 0.21 m: 0.46 - 0.25 cos35 - 0.21 cos35 = 0.083 m) so the feet stay on the ground.
CROUCH_LEGS = {s + b: r for s in ("Left", "Right") for b, r in
               (("UpperLeg", [("X", -35)]), ("LowerLeg", [("X", 70)]), ("Foot", [("X", -35)]))}
CROUCH_DROP = 0.083


def merge(*poses):
    out = {}
    for p in poses:
        for b, r in p.items():
            out[b] = list(r)
    return out


CLIPS = {
    "CungThu_Idle": dict(loop=True, keys={
        1: IDLE,
        31: merge(IDLE, {"Chest": [("X", -2)], "LeftLowerArm": [("X", -16)], "Head": [("X", 0)]}),
        61: IDLE}, hips={}, markers={}),
    "CungThu_Attack": dict(loop=False, keys={
        1: IDLE,
        7: merge(TWIST, BOW_ARM, DRAW_REACH),
        13: merge(TWIST, BOW_ARM, FULL_DRAW),
        15: merge(TWIST, BOW_ARM, LOOSE),
        19: merge(TWIST, BOW_ARM, LOOSE),
        26: IDLE}, hips={}, markers={"release": 14}),
    "CungThu_Spawn": dict(loop=False, keys={
        1: merge(CROUCH_LEGS, {"Spine": [("X", -20)], "Head": [("X", 10)],
                               "LeftUpperArm": [("Y", -20)], "RightUpperArm": [("Y", 20)]}),
        16: merge({"Chest": [("X", 5)], "Head": [("X", -6)]}, IDLE),
        30: IDLE}, hips={1: CROUCH_DROP, 16: 0.0, 30: 0.0}, markers={"landed": 1}),
}


def log(*a):
    print("[03_anim]", *a)


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
    for act in [a for a in bpy.data.actions if a.name.startswith(("QA_", "CungThu_"))]:
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
