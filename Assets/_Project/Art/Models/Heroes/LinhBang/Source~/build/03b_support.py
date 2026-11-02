"""Step 3b (C-08): two-handed crossbow hold in LinhBang_Attack, the way a soldier shoots one.

Run after 03_anim.py: blender --background --python build/03b_support.py -- --master LinhBang_master.blend
Left arm: upper arm hangs along the ribs, elbow bent at a right angle, forearm level and forward; the palm turns up
under the stock. Right arm: the crossbow is laid on that left hand and points straight ahead, the right hand on the
grip behind it (elbow bent, near the chest). Both arms are solved with IK at the shot frame (the right one also
matches rotation, so the crossbow heading is exact), keyed in and out with the clip's raise and lower, then baked
into plain rotation keys; helpers are deleted so Unity gets an ordinary Humanoid clip. Re-running 03_anim.py
rebuilds the clip; run this again after it.
"""
import sys, os, math, argparse
import bpy
from mathutils import Vector, Quaternion, Matrix
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import crossbow

RIG = "CH_LinhBang_Rig"
ACTION = "LinhBang_Attack"
SHOT_FRAME = 13
ARM_BONES = ("LeftUpperArm", "LeftLowerArm", "LeftHand", "RightUpperArm", "RightLowerArm", "RightHand")
HELPERS = ("IK_RightHand", "IK_RightPole", "IK_LeftWrist", "IK_LeftPole")

# Rest-pose crossbow (Blender metres, character faces -Y): butt (-0.42, 0.54, 0.88), tip (-0.59, -0.89, 0.35),
# right wrist (-0.44, 0.02, 0.80). The left hand carries it at GRIP_ALONG of the wrist-to-tip length.
BUTT = crossbow.BUTT     # scaled in 01b_trim (crossbow.py)
TIP = crossbow.TIP
WRIST_R = crossbow.WRIST_R
GRIP_ALONG = 0.22
STOCK_HALF = 0.03       # stock bottom sits this far under its axis; the palm touches it there
# Real hold: the butt sits in the pocket in front of the right shoulder and the stock rests on the left palm; the
# crossbow lies on the line through those two points (its yaw and pitch follow from the left hand's position).
BUTT_POCKET = Vector((-0.02, -0.14, -0.08))   # from the posed right shoulder joint

# Left arm, from the posed left shoulder: upper arm 0.26 m, forearm 0.22 m (bone lengths). The elbow goes down,
# forward and a little OUT, so the whole arm stays in front of the armour (an inward elbow sinks into the chest
# and the crossbow hides the rest: the arm vanishes from the front). Forearm across under the stock, ~90° elbow.
# The left arm is searched, not hand-set: with these short arms (0.26 + 0.22 m) a right-angle elbow cannot put the
# palm on a straight-ahead crossbow line, so the search finds the arm that comes closest. Upper arm: tilt forward
# from hanging (deg) and swing in (-) / out (+); forearm: heading from straight ahead toward the right (-) and rise.
ELBOW_RANGE = (85.0, 115.0)        # interior elbow angle, degrees
UPPER_FORWARD = range(20, 81, 5)
UPPER_SWING = range(-40, 31, 5)
FORE_HEADING = range(-90, -4, 5)
FORE_RISE = range(-10, 31, 5)
AIM_PITCH_GOAL = -2.0              # degrees: a touch down at the target
# Armour surface in the posed Chest frame (local X = character left, local Z = forward): the chest plate is 0.15 m in
# front of the spine and 0.27 m to each side around z 0.95-1.2 (rest-pose slices). Elbow, mid-forearm and wrist must
# clear it, or the arm sinks into the chest and vanishes from the front.
ARMOUR_FRONT = 0.15
ARMOUR_SIDE = 0.27
CLEARANCE = 0.04
# Readability: the elbow must stick out past the chest's silhouette from the front, or the blue arm armour blends
# into the blue chest plate and the arm reads as missing.
ELBOW_SIDE_MIN = 0.32


def upper_dir(forward, swing):
    f, s = math.radians(forward), math.radians(swing)
    return Vector((math.sin(f) * math.sin(s), -math.sin(f) * math.cos(s), -math.cos(f))).normalized()


def fore_dir(heading, rise):
    h, r = math.radians(heading), math.radians(rise)
    return Vector((math.sin(h) * math.cos(r), -math.cos(h) * math.cos(r), math.sin(r))).normalized()


UPPER_LEN = 0.26
FORE_LEN = 0.22
PALM_FROM_WRIST = 0.07
PALM_THICK = 0.03

# The palm faces the thigh (-X) in the A-pose; turn it up, but cap the twist so the low-poly forearm does not
# wring. Split between forearm and wrist so neither joint takes all of it.
REST_PALM = Vector((-1.0, 0.0, 0.0))
MAX_TWIST_DEG = 70.0
TWIST_SHARE = (("LeftLowerArm", 0.5), ("LeftHand", 0.5))

# IK influence over the clip: idle (1) -> aiming (6) ... recoil (15) -> idle (24).
INFLUENCE = {1: 0.0, 6: 1.0, 17: 1.0, 24: 0.0}
# Recoil: the crossbow kicks up about its grip at the shot, the left hand rides along.
RECOIL = {13: 0.0, 15: 8.0, 18: 0.0}


def log(*a):
    print("[03b_support]", *a)


def fcurve_lists(act):
    """F-curve collections of an action: Blender 5 keeps them in layers/strips/channel bags, 4.x on the action."""
    if hasattr(act, "fcurves"):
        return [act.fcurves]
    out = []
    for layer in act.layers:
        for strip in layer.strips:
            for bag in strip.channelbags:
                out.append(bag.fcurves)
    return out


def influence_at(frame, table):
    keys = sorted(table.items())
    for (f0, v0), (f1, v1) in zip(keys, keys[1:]):
        if f0 <= frame <= f1:
            return v0 + (v1 - v0) * (frame - f0) / (f1 - f0)
    return keys[-1][1] if frame > keys[-1][0] else keys[0][1]


def empty(name, parent=None):
    ob = bpy.data.objects.new(name, None)
    bpy.context.scene.collection.objects.link(ob)
    if parent is not None:
        ob.parent = parent
    return ob


def add_ik(pb, target, pole, chain, rotation=False):
    for c in [c for c in pb.constraints if c.type == "IK"]:
        pb.constraints.remove(c)
    ik = pb.constraints.new("IK")
    ik.target = target
    ik.pole_target = pole
    ik.pole_angle = math.radians(-90.0)   # roll-0 bones
    ik.chain_count = chain
    ik.use_tail = True
    ik.use_rotation = rotation
    return ik


def select_bones(rig, names):
    for pb in rig.pose.bones:
        if hasattr(pb, "select"):   # Blender 5 moved selection from Bone to PoseBone
            pb.select = pb.name in names
        else:
            pb.bone.select = pb.name in names


def solve_twist(scene, rig):
    """Twist about the hand's length (1° steps, capped) that turns the palm closest to straight up at the shot."""
    scene.frame_set(SHOT_FRAME)
    rest = rig.data.bones["LeftHand"].matrix_local.to_3x3()
    palm_local = rest.inverted() @ REST_PALM
    pose = rig.pose.bones["LeftHand"].matrix.to_3x3()
    cap = int(MAX_TWIST_DEG)
    up = lambda d: (pose @ (Quaternion((0, 1, 0), math.radians(d)).to_matrix() @ palm_local)).z
    best = max(range(-cap, cap + 1), key=up)
    log("palm twist", best, "deg, palm up", round(up(best), 3), "(untwisted", round(up(0), 3), ")")
    return best


def read_rotations(scene, rig, bones, start, end):
    """Evaluated rotations of every frame, read before any key is written: writing as we go would let the new keys
    leak into the interpolation of later frames and compound the change (sparse keys, e.g. Hips)."""
    out = {}
    for f in range(start, end + 1):
        scene.frame_set(f)
        out[f] = {b: (rig.pose.bones[b].rotation_quaternion.copy(), rig.pose.bones[b].location.copy()) for b in bones}
    return out


def turn_body(scene, rig, start, end, yaw_deg):
    """Side-on stance: turn the whole body (Hips, about the vertical) by -yaw while aiming, so the crossbow, which
    lies yaw degrees left of the chest, points straight at the target. Hips' local Y is world up."""
    base = read_rotations(scene, rig, ("Hips",), start, end)
    pb = rig.pose.bones["Hips"]
    for f in range(start, end + 1):
        q, loc = base[f]["Hips"]
        pb.rotation_quaternion = Quaternion((0.0, 1.0, 0.0), math.radians(-yaw_deg * influence_at(f, INFLUENCE))) @ q
        pb.location = loc
        pb.keyframe_insert("rotation_quaternion", frame=f)
        pb.keyframe_insert("location", frame=f)


def twist_palm(scene, rig, start, end, degrees):
    bones = [b for b, _ in TWIST_SHARE]
    base = read_rotations(scene, rig, bones, start, end)
    for f in range(start, end + 1):
        w = influence_at(f, INFLUENCE)
        for bone, share in TWIST_SHARE:
            pb = rig.pose.bones[bone]
            q, _ = base[f][bone]
            pb.rotation_quaternion = q @ Quaternion((0.0, 1.0, 0.0), math.radians(degrees * share * w))
            pb.keyframe_insert("rotation_quaternion", frame=f)


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(a.master))
    scene = bpy.context.scene
    rig = bpy.data.objects[RIG]
    act = bpy.data.actions[ACTION]
    for name in HELPERS:
        old = bpy.data.objects.get(name)
        if old:
            bpy.data.objects.remove(old, do_unlink=True)

    # posed shoulders at the shot frame (the torso turn comes from 03_anim)
    rig.animation_data.action = act
    scene.frame_set(SHOT_FRAME)
    mw = rig.matrix_world
    shoulder_l = mw @ rig.pose.bones["LeftUpperArm"].head
    shoulder_r = mw @ rig.pose.bones["RightUpperArm"].head

    chest = rig.pose.bones["Chest"]
    to_chest = (mw @ chest.matrix).inverted()
    centre = to_chest @ (mw @ chest.head)
    pocket = shoulder_r + BUTT_POCKET
    rest_dir = (TIP - BUTT).normalized()
    grip_rest = WRIST_R + (TIP - WRIST_R) * GRIP_ALONG

    def clears(pt):
        # chest bone: local Y runs up the spine, X to the character's left, Z forward
        q = to_chest @ pt - centre
        return q.z > ARMOUR_FRONT + CLEARANCE or abs(q.x) > ARMOUR_SIDE + CLEARANCE

    best = None
    for fw in UPPER_FORWARD:
        for sw in UPPER_SWING:
            u = upper_dir(fw, sw)
            elbow = shoulder_l + u * UPPER_LEN
            if not clears(elbow) or (to_chest @ elbow - centre).x < ELBOW_SIDE_MIN:
                continue
            for hd in FORE_HEADING:
                for rs in FORE_RISE:
                    f = fore_dir(hd, rs)
                    angle = math.degrees(u.angle(-f))
                    if not ELBOW_RANGE[0] <= angle <= ELBOW_RANGE[1]:
                        continue
                    wrist = elbow + f * FORE_LEN
                    if not (clears(elbow + f * FORE_LEN * 0.5) and clears(wrist)):
                        continue
                    palm = wrist + f * PALM_FROM_WRIST + Vector((0, 0, PALM_THICK + STOCK_HALF))
                    aim = (palm - pocket).normalized()
                    yaw = math.degrees(math.atan2(aim.x, -aim.y))
                    pitch = math.degrees(math.asin(max(-1.0, min(1.0, aim.z))))
                    if aim.y > -0.5:
                        continue
                    cost = abs(yaw) + abs(pitch - AIM_PITCH_GOAL) + 0.05 * abs(angle - 90.0)
                    if best is None or cost < best[0]:
                        best = (cost, u, f, elbow, wrist, palm, aim, yaw, pitch, angle)
    if best is None:
        raise SystemExit("[03b_support] no left-arm pose clears the armour; widen the search ranges")
    _, UPPER, FORE, elbow_l, wrist_l, stock_on_palm, aim, yaw, pitch, angle = best
    turn = rest_dir.rotation_difference(aim)
    hand_bone = rig.data.bones["RightHand"]
    tail_rest = mw @ hand_bone.tail_local
    tail_target = stock_on_palm - turn @ (grip_rest - tail_rest)
    rot_target = turn.to_matrix().to_4x4() @ (mw @ hand_bone.matrix_local).to_3x3().to_4x4()
    log("left elbow", round(angle, 1), "deg | crossbow yaw", round(yaw, 1), "deg (+ = tip left), pitch",
        round(pitch, 1), "deg | right reach", round((tail_target - shoulder_r).length, 3),
        "| elbow", [round(x, 2) for x in elbow_l], "wrist", [round(x, 2) for x in wrist_l])

    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    bpy.context.view_layer.update()

    hand_t = empty("IK_RightHand", rig)
    hand_t.matrix_world = Matrix.Translation(tail_target) @ rot_target
    pole_r = empty("IK_RightPole", rig)
    pole_r.location = shoulder_r + Vector((-0.35, 0.10, -0.40))     # right elbow down and out
    # the left wrist rides on the crossbow, so the recoil carries it
    wrist_t = empty("IK_LeftWrist")
    wrist_t.location = wrist_l
    bpy.context.view_layer.update()
    wrist_t.parent = hand_t
    wrist_t.matrix_parent_inverse = hand_t.matrix_world.inverted()
    pole_l = empty("IK_LeftPole", rig)
    pole_l.location = elbow_l + UPPER * 0.0 + Vector((0.10, 0.05, -0.30))          # left elbow down, tucked to the ribs

    ik_r = add_ik(rig.pose.bones["RightHand"], hand_t, pole_r, 3, rotation=True)
    ik_l = add_ik(rig.pose.bones["LeftLowerArm"], wrist_t, pole_l, 2)

    rig.animation_data.action = act
    for f, v in INFLUENCE.items():
        for ik in (ik_r, ik_l):
            ik.influence = v
            ik.keyframe_insert("influence", frame=f)
    base = hand_t.matrix_world.copy()
    for f, deg in RECOIL.items():
        # pitch up about the palm (X axis), so the stock lifts off the left hand together with it
        kick = (Matrix.Translation(stock_on_palm) @ Matrix.Rotation(math.radians(-deg), 4, "X")
                @ Matrix.Translation(-stock_on_palm))
        hand_t.matrix_world = kick @ base
        hand_t.keyframe_insert("location", frame=f)
        hand_t.keyframe_insert("rotation_euler", frame=f)

    bpy.context.view_layer.objects.active = rig
    for o in bpy.context.view_layer.objects:
        o.select_set(o == rig)
    bpy.ops.object.mode_set(mode="POSE")
    select_bones(rig, ARM_BONES)
    start, end = int(act.frame_range[0]), int(act.frame_range[1])
    bpy.ops.nla.bake(frame_start=start, frame_end=end, only_selected=True, visual_keying=True,
                     clear_constraints=True, use_current_action=True, bake_types={"POSE"})
    bpy.ops.object.mode_set(mode="OBJECT")
    twist_palm(scene, rig, start, end, solve_twist(scene, rig))
    turn_body(scene, rig, start, end, yaw)
    log("body turned", round(-yaw, 1), "deg so the crossbow points at the target")

    for fcurves in fcurve_lists(act):
        for fc in [fc for fc in fcurves if "constraints" in fc.data_path]:
            fcurves.remove(fc)
    for name in HELPERS:
        ob = bpy.data.objects.get(name)
        if ob:
            bpy.data.objects.remove(ob, do_unlink=True)
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)
    log("baked two-handed hold into", ACTION, "frames", start, "-", end)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(a.master))


main()
