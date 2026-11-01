"""Step 2 (C-08): Unity Humanoid rig for CH_LinhBang and skin weights, plus a QA pose action.

Run: blender --background --python build/02_rig.py -- --master LinhBang_master.blend
Same method as CungThu/PhuThuyLua 02_rig.py (computed weights on one fused Hunyuan3D island). Rigid parts: crossbow
with its ice limbs (RightHand), helmet and crest (Head), bolt quiver and gourd at the hips (Hips); the front tabard
only takes torso bones so a crouch does not tear it. Adult proportions, 1.8 m incl. the crest (01_lowpoly).
Joint centres from 5 cm slices of the scaled mesh and the front render (belt z 0.86, boot tops z 0.29).
"""
import sys, os, math, argparse
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import crossbow

NAME = "CH_LinhBang"
BODY = NAME + "_Body_LOD0"
RIG = NAME + "_Rig"
QA_ACTION = "QA_Poses"
Y = 0.02  # body centre plane: feet y [-0.13, 0.15]

BONES = [  # arms join the torso at z 1.20, neck z 1.30-1.35, helmet 1.36-1.62, crest to 1.80
    ("Hips", (0, Y, 0.85), (0, Y, 0.98), None),
    ("Spine", (0, Y, 0.98), (0, Y, 1.10), "Hips"),
    ("Chest", (0, Y, 1.10), (0, Y, 1.20), "Spine"),
    ("UpperChest", (0, Y, 1.20), (0, Y, 1.28), "Chest"),
    ("Neck", (0, Y, 1.28), (0, Y, 1.36), "UpperChest"),
    ("Head", (0, Y, 1.36), (0, Y, 1.62), "Neck"),
]
for side, sx in (("Left", 1), ("Right", -1)):
    X = lambda p, sx=sx: (p[0] * sx, p[1], p[2])
    BONES += [
        (side + "Shoulder", X((0.05, Y, 1.22)), X((0.20, Y, 1.22)), "UpperChest"),
        # arms ~60° below horizontal: shoulder (0.20, 1.22) -> wrist (0.44, 0.80); hands x 0.42-0.54, z 0.60-0.85
        (side + "UpperArm", X((0.20, Y, 1.22)), X((0.34, Y, 1.00)), side + "Shoulder"),
        (side + "LowerArm", X((0.34, Y, 1.00)), X((0.44, Y, 0.80)), side + "UpperArm"),
        (side + "Hand", X((0.44, Y, 0.80)), X((0.49, Y, 0.65)), side + "LowerArm"),
        # feet centres x ±0.20 at the sole; knees z 0.45, ankles z 0.10
        (side + "UpperLeg", X((0.10, Y, 0.83)), X((0.15, Y, 0.45)), "Hips"),
        (side + "LowerLeg", X((0.15, Y, 0.45)), X((0.19, Y + 0.01, 0.10)), side + "UpperLeg"),
        (side + "Foot", X((0.19, Y + 0.01, 0.10)), X((0.20, -0.07, 0.03)), side + "LowerLeg"),
        (side + "Toes", X((0.20, -0.07, 0.03)), X((0.20, -0.13, 0.02)), side + "Foot"),
    ]
BONE_NAMES = [b[0] for b in BONES]
TORSO = ("Hips", "Spine", "Chest", "UpperChest")

MAX_INFLUENCES = 4
SMOOTH_ITERATIONS = 6
FALLOFF_POWER = 4
HAT_Z = 1.37           # helmet rim above the neck guard
HAT_RADIUS = 0.30      # helmet and crest reach x ±0.24
LEG_Z = 0.93
ARM_X = 0.16
# Arm line: shoulder (0.20, 1.22) down ~60° to the fingertips (0.49, 0.65): z drops ~1.9 per unit of x.
ARM_LINE_X0 = 0.20
ARM_LINE_Z0 = 1.22
ARM_LINE_SLOPE = 1.9
ARM_LINE_MARGIN = 0.12
HEAD_Z = 1.25
# Crossbow: from the ice head (front, low) to the butt (behind, high) through the right hand.
BOW_A = crossbow.TIP     # scaled in 01b_trim (crossbow.py)
BOW_B = crossbow.BUTT
BOW_RADIUS = 0.12 * crossbow.SCALE      # stock and string; the stock axis sits off the tip-to-butt line by up to 0.1 m
BOW_HEAD_RADIUS = 0.32 * crossbow.SCALE  # ice limbs spread ±0.25 around the front end
BOW_HEAD_T = 0.35
# Tabard: front apron between the legs, belt to the shins.
TABARD_Y = -0.10
# Bolt quiver behind the right hip (y 0.14-0.33, z 0.45-0.87) touches the stock (x -0.46..-0.30); its mouth ends
# at x -0.22, so with the bolt tips trimmed everything outside x -0.27 there is stock.
QUIVER_Y = 0.13
QUIVER_Y_MAX = 0.36
STOCK_X = -0.27
FLETCH_X = -0.22
FLETCH_Z = 0.70
TABARD_X = 0.17


def log(*a):
    print("[02_rig LinhBang]", *a)


def col(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def clear_previous(body):
    rig = bpy.data.objects.get(RIG)
    if rig:
        arm = rig.data
        bpy.data.objects.remove(rig, do_unlink=True)
        bpy.data.armatures.remove(arm)
    act = bpy.data.actions.get(QA_ACTION)
    if act:
        bpy.data.actions.remove(act)
    body.parent = None
    for m in [m for m in body.modifiers if m.type == "ARMATURE"]:
        body.modifiers.remove(m)
    body.vertex_groups.clear()


def build_armature():
    arm = bpy.data.armatures.new(RIG)
    ob = bpy.data.objects.new(RIG, arm)
    col("RIG_DEF").objects.link(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    for name, head, tail, parent in BONES:
        eb = arm.edit_bones.new(name)
        eb.head, eb.tail = Vector(head), Vector(tail)
        eb.roll = 0.0
        if parent:
            eb.parent = arm.edit_bones[parent]
            eb.use_connect = (eb.head - eb.parent.tail).length < 1e-5
    bpy.ops.object.mode_set(mode="OBJECT")
    arm.display_type = "STICK"
    return ob


def seg_dist(p, a, b):
    ab = b - a
    t = max(0.0, min(1.0, (p - a).dot(ab) / ab.length_squared))
    return (a + ab * t - p).length


def is_tabard(co):
    return co.y < TABARD_Y and abs(co.x) < TABARD_X and 0.25 < co.z < 0.86


def allowed(name, co):
    if is_tabard(co):
        return name in TORSO
    if "Leg" in name or "Foot" in name or "Toes" in name:
        return co.z < LEG_Z and (co.x > -0.02 if name.startswith("Left") else co.x < 0.02)
    if "Arm" in name or "Hand" in name or "Shoulder" in name:
        # armour skirt and hip gear sit close to the hanging hands; keep everything under the arm line off the arm
        below_arm = co.z < ARM_LINE_Z0 - ARM_LINE_SLOPE * (abs(co.x) - ARM_LINE_X0) - ARM_LINE_MARGIN
        return abs(co.x) > ARM_X and not below_arm and (co.x > 0 if name.startswith("Left") else co.x < 0)
    if name in ("Neck", "Head"):
        return co.z > HEAD_Z
    return True


def vertex_colours(body):
    """Base colour sampled at each vertex's first loop UV, used to tell the wooden bow from the blue sleeve."""
    me = body.data
    img = None
    for n in body.active_material.node_tree.nodes:
        if n.type == "TEX_IMAGE" and n.image and n.image.colorspace_settings.name == "sRGB":
            img = n.image
    w, h = img.size
    px = img.pixels[:]
    uv = me.uv_layers.active.data
    cols = [None] * len(me.vertices)
    for loop in me.loops:
        if cols[loop.vertex_index] is None:
            u, v = uv[loop.index].uv
            x = min(w - 1, max(0, int((u % 1.0) * w)))
            y = min(h - 1, max(0, int((v % 1.0) * h)))
            i = (y * w + x) * 4
            cols[loop.vertex_index] = (px[i], px[i + 1], px[i + 2])
    return cols


def is_blue(c):
    # sleeve/shirt blue (b clearly above r); the yellow cuff trim sits on the sleeve, so it counts as cloth too
    return c[2] > c[0] + 0.04 or (c[0] > 0.45 and c[1] > 0.4 and c[2] < 0.25 and c[0] - c[2] > 0.3 and c[1] > c[2] + 0.2)


def is_wood(c):
    # crossbow stock: dark brown (r clearly above b); the bolts beside it are neutral grey (r ~ g ~ b)
    return c[0] - c[2] > 0.05


def rigid_bone(co, colour):
    """Parts that must not bend. Returns the bone or None."""
    ab = BOW_B - BOW_A
    t = max(0.0, min(1.0, (co - BOW_A).dot(ab) / ab.length_squared))
    radius = BOW_HEAD_RADIUS if t < BOW_HEAD_T else BOW_RADIUS
    if (BOW_A + ab * t - co).length < radius:
        # the ice head is far in front of the body: all of it is crossbow
        if t < BOW_HEAD_T:
            return "RightHand"
        # behind the hip the stock lies on the quiver (bolt tips removed in 01b_trim): outside STOCK_X is stock
        if co.x < STOCK_X:
            return "RightHand"
        # bluish fletching scraps left on the stock's underside (x -0.24..-0.32, z 0.74-0.78) ride with it
        if co.x < FLETCH_X and co.z > FLETCH_Z:
            return "RightHand"
    if co.z > HAT_Z and abs(co.x) < HAT_RADIUS:
        return "Head"
    # bolt quiver across the back of the hips (bolts reach the stock at x -0.46) and the gourd on the left hip
    if co.y > QUIVER_Y and 0.38 < co.z < 0.88 and -0.47 < co.x < 0.42:
        return "Hips"
    if 0.20 < co.x < 0.40 and 0.55 < co.z < 0.76 and abs(co.y) < 0.12:
        return "Hips"
    return None


def compute_weights(body, rig):
    me = body.data
    bones = {b.name: (rig.matrix_world @ b.head_local, rig.matrix_world @ b.tail_local) for b in rig.data.bones}
    colours = [None] * len(me.vertices)
    n = len(me.vertices)
    weights = [dict() for _ in range(n)]
    locked = [False] * n
    for v in me.vertices:
        co = body.matrix_world @ v.co
        rb = rigid_bone(co, colours[v.index])
        if rb:
            weights[v.index] = {rb: 1.0}
            locked[v.index] = True
            continue
        cand = []
        for name, (a, b) in bones.items():
            if allowed(name, co):
                cand.append((seg_dist(co, a, b), name))
        cand.sort()
        cand = cand[:3]
        ws = {name: 1.0 / (d ** FALLOFF_POWER + 1e-8) for d, name in cand}
        s = sum(ws.values())
        weights[v.index] = {k: w / s for k, w in ws.items()}

    # Laplacian smoothing over edges so joints blend instead of stepping; rigid parts stay fixed.
    nbrs = [[] for _ in range(n)]
    for e in me.edges:
        a, b = e.vertices
        nbrs[a].append(b)
        nbrs[b].append(a)
    for _ in range(SMOOTH_ITERATIONS):
        new = []
        for i in range(n):
            if locked[i] or not nbrs[i]:
                new.append(weights[i])
                continue
            acc = dict(weights[i])
            for j in nbrs[i]:
                for k, w in weights[j].items():
                    acc[k] = acc.get(k, 0.0) + w
            s = sum(acc.values())
            new.append({k: w / s for k, w in acc.items()})
        weights = new

    groups = {name: body.vertex_groups.new(name=name) for name in BONE_NAMES}
    for i, ws in enumerate(weights):
        top = sorted(ws.items(), key=lambda kv: -kv[1])[:MAX_INFLUENCES]
        top = [(k, w) for k, w in top if w >= 0.03] or top[:1]
        s = sum(w for _, w in top)
        for k, w in top:
            groups[k].add([i], w / s, "REPLACE")
    return sum(locked)


def bind(body, rig):
    mw = body.matrix_world.copy()
    body.parent = rig
    body.matrix_world = mw
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    mod.use_vertex_groups = True


def rot_world(rig, bone_name, axis, deg):
    """Pose rotation expressed as a world-axis rotation about the bone head (QA only)."""
    b = rig.data.bones[bone_name]
    rest = (rig.matrix_world @ b.matrix_local).to_quaternion()
    q = Quaternion(Vector(axis), math.radians(deg))
    pb = rig.pose.bones[bone_name]
    pb.rotation_mode = "QUATERNION"
    pb.rotation_quaternion = rest.inverted() @ q @ rest


POSES = {  # frame: [(bone, world axis, degrees)] – the extremes Idle/Attack/Spawn must survive
    1: [],
    2: [("LeftUpperArm", (0, 1, 0), -60), ("RightUpperArm", (0, 1, 0), 60)],          # arms raised
    3: [("LeftUpperArm", (1, 0, 0), 70), ("LeftLowerArm", (1, 0, 0), 20),             # bow raised forward
        ("RightUpperArm", (1, 0, 0), 80), ("RightLowerArm", (0, 0, 1), -90)],          # drawing hand back
    4: [("LeftUpperLeg", (1, 0, 0), 70), ("LeftLowerLeg", (1, 0, 0), -90),            # knee up / crouch
        ("RightUpperLeg", (1, 0, 0), -25), ("Spine", (1, 0, 0), 20)],
    5: [("Spine", (0, 0, 1), 30), ("Chest", (0, 0, 1), 15), ("Head", (0, 0, 1), -30)],  # twist
}


def build_qa_action(rig):
    rig.animation_data_create()
    act = bpy.data.actions.new(QA_ACTION)
    rig.animation_data.action = act
    for frame, rots in POSES.items():
        for pb in rig.pose.bones:
            pb.rotation_mode = "QUATERNION"
            pb.rotation_quaternion = Quaternion()
        for bone, axis, deg in rots:
            rot_world(rig, bone, axis, deg)
        for pb in rig.pose.bones:
            pb.keyframe_insert("rotation_quaternion", frame=frame)
    for pb in rig.pose.bones:
        pb.rotation_quaternion = Quaternion()
    act.use_fake_user = True


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(a.master))
    body = bpy.data.objects[BODY]
    clear_previous(body)
    rig = build_armature()
    locked = compute_weights(body, rig)
    bind(body, rig)
    build_qa_action(rig)
    log("bones", len(rig.data.bones), "verts", len(body.data.vertices), "rigid", locked)
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(a.master))


main()
