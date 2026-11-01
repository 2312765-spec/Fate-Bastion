"""Step 2 (C-08): Unity Humanoid rig for CH_CungThu and skin weights, plus a QA pose action.

Run: blender --background --python build/02_rig.py -- --master CungThu_master.blend
Re-runnable: deletes the rig, vertex groups and QA action it made last time.

The Hunyuan3D mesh is one fused island (bow welded to the hand, quiver to the back), so weights are computed,
not painted: distance to bone segments with side/height masks, smoothed over mesh edges, then rigid overrides
for bow (LeftHand), hat (Head) and quiver (UpperChest). Joint centres were measured from horizontal slices of
the scaled mesh (01_lowpoly, 1.6 m incl. hat; character faces -Y, character left = +X, bow in the left hand).
"""
import sys, os, math, argparse
import bpy, bmesh
from mathutils import Vector, Matrix, Quaternion

NAME = "CH_CungThu"
BODY = NAME + "_Body_LOD0"
RIG = NAME + "_Rig"
QA_ACTION = "QA_Poses"
Y = 0.02  # body centre plane: feet y [-0.12, 0.13], torso y [-0.10, 0.12] without the quiver

BONES = [  # name, head, tail, parent – slices of the mesh every 5 cm (crotch z 0.50, armpit z 0.95, chin z 1.12)
    ("Hips", (0, Y, 0.56), (0, Y, 0.66), None),
    ("Spine", (0, Y, 0.66), (0, Y, 0.78), "Hips"),
    ("Chest", (0, Y, 0.78), (0, Y, 0.90), "Spine"),
    ("UpperChest", (0, Y, 0.90), (0, Y, 1.02), "Chest"),
    ("Neck", (0, Y, 1.02), (0, Y, 1.10), "UpperChest"),
    ("Head", (0, Y, 1.10), (0, Y, 1.36), "Neck"),
]
for side, sx in (("Left", 1), ("Right", -1)):
    X = lambda p, sx=sx: (p[0] * sx, p[1], p[2])
    BONES += [
        (side + "Shoulder", X((0.04, Y, 0.99)), X((0.15, Y, 1.01)), "UpperChest"),
        # A-pose ~45° down: shoulder (0.15, 1.01) -> wrist (0.46, 0.72); hand to the fingertips (0.53, 0.62)
        (side + "UpperArm", X((0.15, Y, 1.01)), X((0.31, Y, 0.87)), side + "Shoulder"),
        (side + "LowerArm", X((0.31, Y, 0.87)), X((0.46, Y, 0.72)), side + "UpperArm"),
        (side + "Hand", X((0.46, Y, 0.72)), X((0.52, Y, 0.63)), side + "LowerArm"),
        # feet centres x ±0.18 at the sole, knees at z 0.30, ankles at z 0.10
        (side + "UpperLeg", X((0.09, Y, 0.56)), X((0.14, Y, 0.31)), "Hips"),
        (side + "LowerLeg", X((0.14, Y, 0.31)), X((0.17, Y + 0.01, 0.10)), side + "UpperLeg"),
        (side + "Foot", X((0.17, Y + 0.01, 0.10)), X((0.17, -0.07, 0.03)), side + "LowerLeg"),
        (side + "Toes", X((0.17, -0.07, 0.03)), X((0.17, -0.12, 0.02)), side + "Foot"),
    ]
BONE_NAMES = [b[0] for b in BONES]

MAX_INFLUENCES = 4
SMOOTH_ITERATIONS = 6
FALLOFF_POWER = 4
HAT_Z = 1.335          # hat brim underside; above it everything near the head follows the head
HAT_RADIUS = 0.33      # brim reaches x ±0.30 (slice z 1.35-1.40)
LEG_Z = 0.66           # leg bones may only take vertices below the hip line
ARM_X = 0.12           # arm bones may only take vertices this far from the centre
HEAD_Z = 1.0           # Neck/Head may only take vertices above the shoulders


def log(*a):
    print("[02_rig]", *a)


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


def allowed(name, co):
    if "Leg" in name or "Foot" in name or "Toes" in name:
        return co.z < LEG_Z and (co.x > -0.02 if name.startswith("Left") else co.x < 0.02)
    if "Arm" in name or "Hand" in name or "Shoulder" in name:
        return abs(co.x) > ARM_X and (co.x > 0 if name.startswith("Left") else co.x < 0)
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
    """Parts that must not bend: the bow and its string with the left hand, the hat with the head, the quiver
    with the upper chest. Returns the bone name or None."""
    # the bow tip also rises above the brim (x 0.53-0.57), so the hat is limited to its own radius
    if co.z > HAT_Z and abs(co.x) < HAT_RADIUS:
        return "Head"
    # bow: lower limb below the hand, upper limb above the sleeve, grip zone unless it is sleeve cloth
    if co.x > 0.255 and co.z < 0.60:
        return "LeftHand"
    if co.x > 0.44 and co.z > 0.78 and not is_blue(colour):
        return "LeftHand"
    if co.x > 0.40 and 0.60 <= co.z <= 0.78 and not is_blue(colour):
        return "LeftHand"
    # quiver on the back (+Y) and the arrow fletchings above the right shoulder
    if co.y > 0.12 and 0.80 < co.z < 1.10:
        return "UpperChest"
    if co.x < -0.17 and 1.05 < co.z < 1.30:
        return "UpperChest"
    return None


def compute_weights(body, rig):
    me = body.data
    bones = {b.name: (rig.matrix_world @ b.head_local, rig.matrix_world @ b.tail_local) for b in rig.data.bones}
    colours = vertex_colours(body)
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
