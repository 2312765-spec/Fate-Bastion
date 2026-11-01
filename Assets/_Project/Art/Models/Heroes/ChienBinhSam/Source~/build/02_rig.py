"""Step 2 (C-08): Unity Humanoid rig for CH_ChienBinhSam and skin weights, plus a QA pose action.

Run: blender --background --python build/02_rig.py -- --master ChienBinhSam_master.blend
Same method as the other Hunyuan3D heroes (computed weights on one fused island). Rigid parts: lightning sword and
hilt (RightHand), horned helmet (Head), pauldrons (each UpperArm). The front and back tabard panels between the legs
only take torso bones so a step does not tear them. Joint centres from 5 cm slices of the scaled mesh (01_lowpoly,
1.9 m incl. the horns).
"""
import sys, os, math, argparse
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion

NAME = "CH_ChienBinhSam"
BODY = NAME + "_Body_LOD0"
RIG = NAME + "_Rig"
QA_ACTION = "QA_Poses"
Y = 0.0  # body centre plane: boots y [-0.20, 0.15]

BONES = [  # boots to z 0.15, knee guards z 0.40, belt z 0.95, shoulders z 1.22 (x ±0.26), helmet 1.40-1.62, horns to 1.9
    ("Hips", (0, Y, 0.86), (0, Y, 0.98), None),
    ("Spine", (0, Y, 0.98), (0, Y, 1.10), "Hips"),
    ("Chest", (0, Y, 1.10), (0, Y, 1.22), "Spine"),
    ("UpperChest", (0, Y, 1.22), (0, Y, 1.32), "Chest"),
    ("Neck", (0, Y, 1.32), (0, Y, 1.42), "UpperChest"),
    ("Head", (0, Y, 1.42), (0, Y, 1.66), "Neck"),
]
for side, sx in (("Left", 1), ("Right", -1)):
    X = lambda p, sx=sx: (p[0] * sx, p[1], p[2])
    BONES += [
        (side + "Shoulder", X((0.07, Y, 1.24)), X((0.26, Y, 1.22)), "UpperChest"),
        # arms ~48° below horizontal: shoulder (0.26, 1.22) -> wrist (0.53, 0.92); fist to (0.62, 0.78)
        (side + "UpperArm", X((0.26, Y, 1.22)), X((0.40, Y, 1.07)), side + "Shoulder"),
        (side + "LowerArm", X((0.40, Y, 1.07)), X((0.53, Y, 0.92)), side + "UpperArm"),
        (side + "Hand", X((0.53, Y, 0.92)), X((0.62, Y, 0.78)), side + "LowerArm"),
        # boots centred x ±0.25
        (side + "UpperLeg", X((0.12, Y, 0.86)), X((0.20, Y, 0.44)), "Hips"),
        (side + "LowerLeg", X((0.20, Y, 0.44)), X((0.25, Y + 0.01, 0.11)), side + "UpperLeg"),
        (side + "Foot", X((0.25, Y + 0.01, 0.11)), X((0.25, -0.09, 0.03)), side + "LowerLeg"),
        (side + "Toes", X((0.25, -0.09, 0.03)), X((0.25, -0.17, 0.02)), side + "Foot"),
    ]
BONE_NAMES = [b[0] for b in BONES]
TORSO = ("Hips", "Spine", "Chest", "UpperChest")

MAX_INFLUENCES = 4
SMOOTH_ITERATIONS = 6
FALLOFF_POWER = 4
LEG_Z = 0.94
ARM_X = 0.22
# Arm line: shoulder (0.26, 1.22) down ~48° to the fist (0.62, 0.78): z drops 1.1 per unit of x.
ARM_LINE_X0 = 0.26
ARM_LINE_Z0 = 1.22
ARM_LINE_SLOPE = 1.1
ARM_LINE_MARGIN = 0.12
TORSO_HALF_X = 0.45    # armour and thigh guards reach x ±0.35; the fists start at ±0.50
HEAD_Z = 1.32
HELMET_Z = 1.40
HELMET_RADIUS = 0.32   # horns reach x ±0.28
PAULDRON_X = 0.30
PAULDRON_Z = 1.12
# Sword: grip at the right fist, blade down and out to the tip on the ground.
SWORD_A = Vector((-0.55, 0.0, 0.95))
SWORD_B = Vector((-1.41, 0.0, 0.02))
SWORD_RADIUS = 0.13
# Tabard panels between the legs, front and back.
TABARD_X = 0.12
TABARD_Y = 0.10


def log(*a):
    print("[02_rig ChienBinhSam]", *a)


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
    return abs(co.x) < TABARD_X and abs(co.y) > TABARD_Y and 0.30 < co.z < 0.92


def allowed(name, co):
    if is_tabard(co):
        return name in TORSO
    if "Leg" in name or "Foot" in name or "Toes" in name:
        return co.z < LEG_Z and (co.x > -0.02 if name.startswith("Left") else co.x < 0.02)
    if "Arm" in name or "Hand" in name or "Shoulder" in name:
        below_arm = (abs(co.x) < TORSO_HALF_X and
                     co.z < ARM_LINE_Z0 - ARM_LINE_SLOPE * (abs(co.x) - ARM_LINE_X0) - ARM_LINE_MARGIN)
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


def rigid_bone(co, colour):
    """Parts that must not bend. Returns the bone or None."""
    ab = SWORD_B - SWORD_A
    t = max(0.0, min(1.0, (co - SWORD_A).dot(ab) / ab.length_squared))
    if (SWORD_A + ab * t - co).length < SWORD_RADIUS and co.x < -0.52:
        return "RightHand"
    if co.z > HELMET_Z and abs(co.x) < HELMET_RADIUS:
        return "Head"
    if abs(co.x) > PAULDRON_X and co.z > PAULDRON_Z:
        return "LeftUpperArm" if co.x > 0 else "RightUpperArm"
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
