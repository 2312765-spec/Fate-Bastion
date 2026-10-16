"""
BUILD PHASE TEMPLATE
Copy this file to <asset>/build/NN_<phase>.py, fill in build(), run:

  blender --background --python build/02_blockout.py -- --master <asset>_master.blend

Every phase script is re-runnable: it opens the master, deletes what it owns
(objects whose names carry OWNER_TAG in a custom property), rebuilds them, saves.
Nothing else in the file is touched, so earlier phases and manual edits survive.

Coordinate convention: metres, Z up, subject faces -Y, origin at the ground
contact point (characters, props, architecture) or the ground centre (vehicles).
"""
import bpy, bmesh, sys, os, math, argparse
from mathutils import Vector, Matrix, Euler

PHASE = "03_forms"
OWNER_TAG = "phase:" + PHASE   # objects created here get ob["owner"] = OWNER_TAG


# ARGS AND FILE HANDLING

def argv_after_dashes():
    a = sys.argv
    return a[a.index("--") + 1:] if "--" in a else []


def open_master(path):
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(path))
    return bpy.context.scene


def save_master(path):
    bpy.context.preferences.filepaths.save_version = 0   # no .blend1 backups
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(path))


def col(name):
    c = bpy.data.collections.get(name)
    if c is None:
        c = bpy.data.collections.new(name)
        bpy.context.scene.collection.children.link(c)
    return c


def clear_owned(tag=None):
    """Delete every object this phase created last time, so the script is idempotent."""
    tag = tag or OWNER_TAG
    for ob in [o for o in bpy.data.objects if o.get("owner") == tag]:
        data = ob.data
        bpy.data.objects.remove(ob, do_unlink=True)
        if data is not None and data.users == 0:
            if isinstance(data, bpy.types.Mesh):
                bpy.data.meshes.remove(data)
            elif isinstance(data, bpy.types.Curve):
                bpy.data.curves.remove(data)
            elif isinstance(data, bpy.types.Armature):
                bpy.data.armatures.remove(data)


def own(ob):
    ob["owner"] = OWNER_TAG
    return ob


def log(*a):
    print("[" + PHASE + "]", *a)


# MESH PRIMITIVES (bmesh based, no operators, safe in background mode)

def new_mesh_object(name, bm, collection, location=(0, 0, 0), rotation=(0, 0, 0), scale=(1, 1, 1)):
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    ob.location = location
    ob.rotation_euler = rotation
    ob.scale = scale
    col(collection).objects.link(ob)
    return own(ob)


def box(name, size, collection="LOW", location=(0, 0, 0), rotation=(0, 0, 0), base_at_origin=False):
    """size = (x, y, z). base_at_origin puts the bottom face on z=0."""
    bm = bmesh.new()
    bmesh.ops.create_cube(bm, size=1.0)
    bmesh.ops.scale(bm, vec=size, verts=bm.verts)
    if base_at_origin:
        bmesh.ops.translate(bm, vec=(0, 0, size[2] / 2), verts=bm.verts)
    return new_mesh_object(name, bm, collection, location, rotation)


def cylinder(name, radius, height, collection="LOW", segments=32, axis="Z", location=(0, 0, 0),
             radius_top=None, base_at_origin=False):
    """Cylinder or truncated cone along axis. radius_top defaults to radius."""
    bm = bmesh.new()
    r2 = radius if radius_top is None else radius_top
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments,
                          radius1=radius, radius2=r2, depth=height)
    if base_at_origin:
        bmesh.ops.translate(bm, vec=(0, 0, height / 2), verts=bm.verts)
    if axis == "X":
        bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(90), 3, "Y"), verts=bm.verts)
    elif axis == "Y":
        bmesh.ops.rotate(bm, cent=(0, 0, 0), matrix=Matrix.Rotation(math.radians(-90), 3, "X"), verts=bm.verts)
    return new_mesh_object(name, bm, collection, location)


def sphere(name, radius, collection="LOW", location=(0, 0, 0), u=24, v=12, scale=(1, 1, 1)):
    bm = bmesh.new()
    bmesh.ops.create_uvsphere(bm, u_segments=u, v_segments=v, radius=radius)
    bmesh.ops.scale(bm, vec=scale, verts=bm.verts)
    return new_mesh_object(name, bm, collection, location)


def grid(name, size_x, size_y, cuts_x, cuts_y, collection="LOW", location=(0, 0, 0), rotation=(0, 0, 0)):
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=cuts_x, y_segments=cuts_y, size=0.5)
    bmesh.ops.scale(bm, vec=(size_x, size_y, 1), verts=bm.verts)
    return new_mesh_object(name, bm, collection, location, rotation)


def loft(name, sections, collection="LOW", close_sections=True, cap_ends=True, smooth=True):
    """Bridge a list of cross sections (each a list of Vector/tuple, same point count) into a hull.
    Use it for vehicle bodies, boat hulls, torsos: read section widths/heights off the top and side
    reference views at fixed stations along the length."""
    bm = bmesh.new()
    rings = []
    for sec in sections:
        vs = [bm.verts.new(Vector(p)) for p in sec]
        ring = []
        for i in range(len(vs)):
            if i == len(vs) - 1 and not close_sections:
                break
            ring.append(bm.edges.new((vs[i], vs[(i + 1) % len(vs)])))
        rings.append(ring)
    for a, b in zip(rings, rings[1:]):
        bmesh.ops.bridge_loops(bm, edges=a + b)
    if cap_ends and close_sections:
        bmesh.ops.contextual_create(bm, geom=rings[0])
        bmesh.ops.contextual_create(bm, geom=rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = new_mesh_object(name, bm, collection)
    if smooth:
        shade_smooth(ob, math.radians(35))
    return ob


def profile_spin(name, profile_xz, collection="LOW", steps=32, location=(0, 0, 0), angle=360):
    """Lathe a 2D profile (list of (radius, z)) around Z: vases, columns, wheel rims, bolts."""
    bm = bmesh.new()
    verts = [bm.verts.new((r, 0, z)) for r, z in profile_xz]
    edges = [bm.edges.new((verts[i], verts[i + 1])) for i in range(len(verts) - 1)]
    bmesh.ops.spin(bm, geom=verts + edges, cent=(0, 0, 0), axis=(0, 0, 1),
                   angle=math.radians(angle), steps=steps, use_merge=(angle >= 360))
    bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=1e-5)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return new_mesh_object(name, bm, collection, location)


def profile_extrude(name, profile_xy, length, collection="LOW", location=(0, 0, 0), rotation=(0, 0, 0)):
    """Extrude a closed 2D outline (list of (x, y)) along Z: arches, mouldings, I-beams, tracks, trims."""
    bm = bmesh.new()
    verts = [bm.verts.new((x, y, 0)) for x, y in profile_xy]
    face = bm.faces.new(verts)
    res = bmesh.ops.extrude_face_region(bm, geom=[face])
    top = [g for g in res["geom"] if isinstance(g, bmesh.types.BMVert)]
    bmesh.ops.translate(bm, vec=(0, 0, length), verts=top)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    return new_mesh_object(name, bm, collection, location, rotation)


def curve_tube(name, points, radius, collection="LOW", resolution=12, taper_to=None, bevel_res=6):
    """Poly/bezier tube through points: cables, horns, tails, pipes, tentacles, railings, roots."""
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    sp = cu.splines.new("NURBS")
    sp.points.add(len(points) - 1)
    for i, p in enumerate(points):
        sp.points[i].co = (p[0], p[1], p[2], 1.0)
        if taper_to is not None:
            t = i / max(1, len(points) - 1)
            sp.points[i].radius = 1.0 + (taper_to - 1.0) * t
    sp.use_endpoint_u = True
    sp.order_u = min(4, len(points))
    cu.resolution_u = resolution
    cu.bevel_depth = radius
    cu.bevel_resolution = bevel_res
    cu.use_fill_caps = True
    ob = bpy.data.objects.new(name, cu)
    col(collection).objects.link(ob)
    return own(ob)


def skin_chain(name, joints, collection="HIGH", subdiv=2, root_index=0):
    """Organic blockout in one call. joints = [(x, y, z, radius), ...] connected in order, or a dict
    {"name": [(x, y, z, r), ...], ...} of chains sharing their first point with another chain by position.
    Skin modifier + Subdivision gives a smooth mass with quad loops at every joint, which is a usable
    LOW base for stylized characters after shrinkwrapping to the sculpt."""
    me = bpy.data.meshes.new(name)
    bm = bmesh.new()
    chains = joints if isinstance(joints, dict) else {"main": joints}
    vert_by_pos = {}
    order = []
    for cname, pts in chains.items():
        prev = None
        for (x, y, z, r) in pts:
            key = (round(x, 5), round(y, 5), round(z, 5))
            v = vert_by_pos.get(key)
            if v is None:
                v = bm.verts.new((x, y, z))
                vert_by_pos[key] = v
                order.append((v, r))
            if prev is not None and prev is not v:
                if not bm.edges.get((prev, v)):
                    bm.edges.new((prev, v))
            prev = v
    bm.verts.index_update()
    bm.to_mesh(me)
    bm.free()
    ob = bpy.data.objects.new(name, me)
    col(collection).objects.link(ob)
    skin = ob.modifiers.new("Skin", "SKIN")
    skin.use_smooth_shade = True
    # skin_vertices layer exists once the modifier is present
    layer = me.skin_vertices[0].data
    for i, (v, r) in enumerate(order):
        layer[i].radius = (r, r)
        layer[i].use_root = (i == root_index)
    sub = ob.modifiers.new("Subdivision", "SUBSURF")
    sub.levels = subdiv
    sub.render_levels = subdiv
    return own(ob)


# MODIFIERS AND EDITS

def mirror_origin():
    """Shared empty at the world origin so mirrors work across the asset's centre line,
    not across each object's own origin (the classic Blender mirror mistake)."""
    ob = bpy.data.objects.get("REF_mirror_origin")
    if ob is None:
        ob = bpy.data.objects.new("REF_mirror_origin", None)
        ob.empty_display_type = "PLAIN_AXES"
        ob.empty_display_size = 0.05
        col("REF").objects.link(ob)
    return ob


def mirror(ob, axis="X", merge=0.001, clip=True, about_world=True):
    """Mirror across the world centre plane by default. Pass about_world=False to mirror
    across the object's own origin (only right when the origin sits on the symmetry plane)."""
    m = ob.modifiers.new("Mirror", "MIRROR")
    m.use_axis = (axis == "X", axis == "Y", axis == "Z")
    m.use_mirror_merge = merge > 0
    m.merge_threshold = merge
    m.use_clip = clip
    if about_world:
        m.mirror_object = mirror_origin()
    return m


def bevel(ob, width=0.01, segments=3, angle_limit=30, harden=True):
    b = ob.modifiers.new("Bevel", "BEVEL")
    b.width = width
    b.segments = segments
    b.limit_method = "ANGLE"
    b.angle_limit = math.radians(angle_limit)
    b.harden_normals = harden
    return b


def subsurf(ob, levels=2):
    s = ob.modifiers.new("Subdivision", "SUBSURF")
    s.levels = levels
    s.render_levels = levels
    return s


def solidify(ob, thickness=0.01, offset=-1.0, rim=True):
    s = ob.modifiers.new("Solidify", "SOLIDIFY")
    s.thickness = thickness
    s.offset = offset
    s.use_rim = rim
    return s


def boolean(ob, cutter, op="DIFFERENCE", hide_cutter=True, solver="EXACT"):
    b = ob.modifiers.new("Bool_" + cutter.name, "BOOLEAN")
    b.operation = op
    b.object = cutter
    b.solver = solver
    if hide_cutter:
        cutter.hide_render = True
        cutter.hide_viewport = True
        cutter.display_type = "WIRE"
    return b


def array(ob, count, offset=(1, 0, 0), relative=True, curve=None):
    a = ob.modifiers.new("Array", "ARRAY")
    a.count = count
    if relative:
        a.relative_offset_displace = offset
    else:
        a.use_relative_offset = False
        a.use_constant_offset = True
        a.constant_offset_displace = offset
    if curve is not None:
        a.fit_type = "FIT_CURVE"
        a.curve = curve
        c = ob.modifiers.new("Curve", "CURVE")
        c.object = curve
    return a


def shrinkwrap(ob, target, offset=0.0, mode="NEAREST_SURFACEPOINT"):
    s = ob.modifiers.new("Shrinkwrap", "SHRINKWRAP")
    s.target = target
    s.offset = offset
    s.wrap_method = mode
    return s


def remesh_voxel(ob, voxel_size=0.02, smooth_normals=True):
    r = ob.modifiers.new("Remesh", "REMESH")
    r.mode = "VOXEL"
    r.voxel_size = voxel_size
    r.use_smooth_shade = smooth_normals
    return r


def decimate(ob, ratio):
    d = ob.modifiers.new("Decimate", "DECIMATE")
    d.ratio = ratio
    return d


def apply_all_modifiers(ob):
    """Bake the modifier stack into the mesh without operators (safe in background)."""
    dg = bpy.context.evaluated_depsgraph_get()
    eo = ob.evaluated_get(dg)
    me = bpy.data.meshes.new_from_object(eo)
    old = ob.data
    name = old.name
    ob.data = me
    ob.modifiers.clear()
    if old.users == 0:
        bpy.data.meshes.remove(old)
    me.name = name                      # keep the mesh name free of .001
    return ob


def apply_transform(ob):
    """Freeze location/rotation/scale into the mesh. Only for unparented objects."""
    if ob.parent is not None or ob.type != "MESH":
        return ob
    ob.data.transform(ob.matrix_basis)
    ob.matrix_basis = Matrix.Identity(4)
    return ob


def set_origin(ob, world_point):
    """Move the object origin to world_point without moving the geometry."""
    p = Vector(world_point)
    local = ob.matrix_world.inverted() @ p
    ob.data.transform(Matrix.Translation(-local))
    ob.matrix_world = ob.matrix_world @ Matrix.Translation(local)
    return ob


def join_meshes(name, objects, collection="LOW"):
    """Merge several mesh objects into one new object in world space. Sources are left alone."""
    bm = bmesh.new()
    dg = bpy.context.evaluated_depsgraph_get()
    for ob in objects:
        eo = ob.evaluated_get(dg)
        me = eo.to_mesh()
        tmp = bmesh.new()
        tmp.from_mesh(me)
        bmesh.ops.transform(tmp, matrix=eo.matrix_world, verts=tmp.verts)
        tmp_me = bpy.data.meshes.new("_tmp_join")
        tmp.to_mesh(tmp_me)
        tmp.free()
        bm.from_mesh(tmp_me)
        bpy.data.meshes.remove(tmp_me)
        eo.to_mesh_clear()
    return new_mesh_object(name, bm, collection)


def duplicate(ob, name, collection=None):
    new = ob.copy()
    new.data = ob.data.copy()
    new.name = name
    new.data.name = name
    col(collection or ob.users_collection[0].name).objects.link(new)
    return own(new)


def shade_smooth(ob, sharp_angle=math.radians(30)):
    """Smooth shading with sharp edges marked by angle. Marked sharp edges always split normals in 4.1+."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    for f in bm.faces:
        f.smooth = True
    for e in bm.edges:
        if len(e.link_faces) == 2:
            e.smooth = e.calc_face_angle(0.0) <= sharp_angle
    bm.to_mesh(me)
    bm.free()
    if hasattr(me, "use_auto_smooth"):      # 4.0 only; removed in 4.1
        me.use_auto_smooth = True
        me.auto_smooth_angle = math.pi
    return ob


def push(ob, center, radius, strength, direction=None, falloff="smooth"):
    """Code sculpting: move vertices within radius of center along their normal (or direction)."""
    me = ob.data
    c = ob.matrix_world.inverted() @ Vector(center)
    bm = bmesh.new()
    bm.from_mesh(me)
    bm.normal_update()
    d = None if direction is None else (ob.matrix_world.to_3x3().inverted() @ Vector(direction)).normalized()
    for v in bm.verts:
        t = (v.co - c).length / radius
        if t >= 1.0:
            continue
        w = (1 - t * t) ** 2 if falloff == "smooth" else (1 - t)
        v.co += (d if d is not None else v.normal) * strength * w
    bm.to_mesh(me)
    bm.free()
    return ob


def taper(ob, axis="Z", top_scale=0.5, bottom=None, top=None):
    """Scale cross sections linearly along an axis (limb tapering, spires, hulls)."""
    me = ob.data
    idx = "XYZ".index(axis)
    cos = [v.co[idx] for v in me.vertices]
    lo = min(cos) if bottom is None else bottom
    hi = max(cos) if top is None else top
    for v in me.vertices:
        t = 0 if hi == lo else max(0.0, min(1.0, (v.co[idx] - lo) / (hi - lo)))
        s = 1 + (top_scale - 1) * t
        for j in range(3):
            if j != idx:
                v.co[j] *= s
    return ob


# UV

def smart_uv(ob, angle=66.0, margin=0.02, seams_from_sharp=True):
    """Quick unwrap for blockouts, props and hard surface: Smart UV Project through the operator,
    with seams taken from marked sharp edges when asked. Hero deforming parts get seams placed
    by hand in the phase script (mark_seams) and unwrap_by_seams instead."""
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    if seams_from_sharp:
        bpy.ops.mesh.edges_select_sharp(sharpness=math.radians(angle))
        bpy.ops.mesh.mark_seam(clear=False)
        bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(angle), island_margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def mark_seams(ob, edge_filter):
    """edge_filter(bmesh_edge) -> bool. Marks seams by rule, e.g. lambda e: abs(e.verts[0].co.x) < 1e-4."""
    me = ob.data
    bm = bmesh.new()
    bm.from_mesh(me)
    n = 0
    for e in bm.edges:
        if edge_filter(e):
            e.seam = True
            n += 1
    bm.to_mesh(me)
    bm.free()
    log("seams marked on", ob.name, n)
    return ob


def unwrap_by_seams(ob, margin=0.02, method="ANGLE_BASED"):
    """Unwrap along marked seams, then pack islands."""
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    ob.select_set(True)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.unwrap(method=method, margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


# MATERIALS

def material(name, color=(0.5, 0.5, 0.5, 1), roughness=0.5, metallic=0.0):
    m = bpy.data.materials.get(name) or bpy.data.materials.new(name)
    if bpy.app.version < (5, 0, 0) and not m.use_nodes:   # 5.0+ materials always use nodes
        m.use_nodes = True
    # look nodes up by type: display names are translated in non-English Blender UIs
    bsdf = next((n for n in m.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
    if bsdf:
        bsdf.inputs["Base Color"].default_value = color
        bsdf.inputs["Roughness"].default_value = roughness
        bsdf.inputs["Metallic"].default_value = metallic
    return m


def assign(ob, mat):
    if ob.data.materials:
        ob.data.materials[0] = mat
    else:
        ob.data.materials.append(mat)
    return ob


# RIG HELPERS

def armature(name, bones, collection="RIG_DEF"):
    """bones = [(name, head, tail, parent_name_or_None, roll_deg, deform_bool), ...]"""
    arm = bpy.data.armatures.new(name)
    ob = bpy.data.objects.new(name, arm)
    col(collection).objects.link(ob)
    own(ob)
    bpy.context.view_layer.objects.active = ob
    bpy.ops.object.mode_set(mode="EDIT")
    for bname, head, tail, parent, roll, deform in bones:
        eb = arm.edit_bones.new(bname)
        eb.head, eb.tail = Vector(head), Vector(tail)
        eb.roll = math.radians(roll)
        eb.use_deform = deform
        if parent:
            eb.parent = arm.edit_bones[parent]
            eb.use_connect = (Vector(head) - arm.edit_bones[parent].tail).length < 1e-5
    bpy.ops.object.mode_set(mode="OBJECT")
    return ob


def bind(mesh_ob, arm_ob, method="ARMATURE_AUTO"):
    """Parent with automatic weights. Weight painting corrections come after, in a later phase."""
    for o in bpy.context.view_layer.objects:
        o.select_set(False)
    mesh_ob.select_set(True)
    arm_ob.select_set(True)
    bpy.context.view_layer.objects.active = arm_ob
    bpy.ops.object.parent_set(type=method)
    return mesh_ob


def socket(name, location, parent=None, parent_bone=None, rotation=(0, 0, 0), size=0.1, **props):
    """Semantic attachment point. Local +Y = forward of whatever attaches, +Z = its up."""
    ob = bpy.data.objects.new(name, None)
    ob.empty_display_type = "ARROWS"
    ob.empty_display_size = size
    col("SOCKETS").objects.link(ob)
    own(ob)
    if parent is not None:
        ob.parent = parent
        if parent_bone:
            ob.parent_type = "BONE"
            ob.parent_bone = parent_bone
    ob.matrix_world = Matrix.Translation(Vector(location)) @ Euler(rotation).to_matrix().to_4x4()
    for k, v in props.items():
        ob[k] = v
    return ob


def lod_copy(ob, level, ratio):
    """Make <name>_LOD<level> from a LOD0 object with a Decimate at ratio. Rework silhouettes by hand after."""
    base = ob.name.replace("_LOD0", "")
    new = duplicate(ob, base + "_LOD" + str(level))
    decimate(new, ratio)
    return new


# BUILD
#
# CH_LongVuong forms (HIGH). Same calibration as build/02_blockout.py: upper body, robe, hair and
# appendages are written in the first-pass sheet units (0.005282 m/px, ground row 495) and mapped by
# OLD_TO_NEW at the end; legs and boots are written afterwards in final metres (ref/front_full.png).
# Order: body -> head/hair -> undersuit -> armour -> robe -> pauldrons -> arms -> ornaments -> legs.
# DESIGN DELTA items (brief) are marked "# delta".
import random

OLD_SCALE = 0.004561 / 0.005282
OLD_LIFT = 70 * 0.004561
OLD_TO_NEW = Matrix.Translation((0, 0, OLD_LIFT)) @ Matrix.Scale(OLD_SCALE, 4)
RNG = random.Random(7)          # fixed seed: fur and hair clump jitter is reproducible

# blockout profiles (sheet units), reused so forms stay on the approved silhouette
TORSO = [(1.95, 0.15, 0.13, 0.0), (1.80, 0.27, 0.20, 0.0), (1.65, 0.30, 0.22, 0.0),
         (1.50, 0.28, 0.21, 0.0), (1.33, 0.27, 0.20, 0.0)]
ROBE = [(1.32, 0.30, 0.22, 0.0), (1.14, 0.38, 0.25, 0.0), (0.98, 0.47, 0.27, 0.0), (0.90, 0.53, 0.28, 0.0),
        (0.74, 0.60, 0.31, 0.02), (0.66, 0.63, 0.33, 0.04), (0.58, 0.61, 0.35, 0.07), (0.50, 0.58, 0.36, 0.09),
        (0.44, 0.51, 0.36, 0.10)]
HAIR = [(2.30, 0.13, 0.12, 0.04), (2.10, 0.16, 0.15, 0.12), (1.90, 0.22, 0.18, 0.22), (1.60, 0.44, 0.20, 0.36),
        (1.30, 0.50, 0.20, 0.45), (1.00, 0.40, 0.18, 0.50), (0.80, 0.26, 0.12, 0.40), (0.62, 0.10, 0.05, 0.36)]


def srgb(h):
    def lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return tuple(lin(int(h[i:i + 2], 16) / 255) for i in (1, 3, 5)) + (1.0,)


def make_materials():
    m = {
        "silver": material("M_LV_Silver", srgb("#C8CCD3"), 0.35, 0.8),
        "steel": material("M_LV_Steel", srgb("#4A4F59"), 0.45, 0.85),
        "fur": material("M_LV_Fur", srgb("#EEEAE4"), 0.9, 0.0),
        "cloth": material("M_LV_Cloth", srgb("#E6E4E0"), 0.7, 0.0),
        "hair": material("M_LV_Hair", srgb("#E9E7E6"), 0.5, 0.0),
        "skin": material("M_LV_Skin", srgb("#E8D2C6"), 0.5, 0.0),
        "ember": material("M_LV_Ember", srgb("#A3201A"), 0.55, 0.2),        # delta: fire red in place of pink
        "glow": material("M_LV_Glow", srgb("#FF7A1F"), 0.4, 0.0),           # delta: emissive fire
        "antler": material("M_LV_Antler", srgb("#E9D8CF"), 0.5, 0.0),
    }
    g = m["glow"]
    bsdf = next(n for n in g.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
    bsdf.inputs["Emission Color"].default_value = srgb("#FF7A1F")
    bsdf.inputs["Emission Strength"].default_value = 3.0
    return m


MAT = {}


def put(ob, key):
    assign(ob, MAT[key])
    return ob


def interp(rows, z):
    """rows (z, rx, ry, cy) top to bottom -> (rx, ry, cy) at z, clamped."""
    if z >= rows[0][0]:
        return rows[0][1:]
    for a, b in zip(rows, rows[1:]):
        if a[0] >= z >= b[0]:
            t = (a[0] - z) / (a[0] - b[0])
            return tuple(a[i] + (b[i] - a[i]) * t for i in (1, 2, 3))
    return rows[-1][1:]


def ring_at(rows, z, theta, grow=0.0):
    rx, ry, cy = interp(rows, z)
    return Vector(((rx + grow) * math.sin(theta), cy - (ry + grow) * math.cos(theta), z))


def frame(t):
    ref = Vector((1, 0, 0)) if abs(t.x) < 0.9 else Vector((0, 0, 1))
    s = t.cross(ref).normalized()
    return s, t.cross(s).normalized()


def tube(name, pts, radii, key, n=12, collection="HIGH", wobble=None):
    """Lofted tube through pts; wobble(i, k) -> radial factor for folds/flutes."""
    pts = [Vector(p) for p in pts]
    bm = bmesh.new()
    rings = []
    for i, p in enumerate(pts):
        t = (pts[min(i + 1, len(pts) - 1)] - pts[max(i - 1, 0)]).normalized()
        s, u = frame(t)
        ring = []
        for k in range(n):
            a = 2 * math.pi * k / n
            r = radii[i] * (wobble(i, k) if wobble else 1.0)
            ring.append(bm.verts.new(p + (s * math.cos(a) + u * math.sin(a)) * r))
        rings.append(ring)
    for ra, rb in zip(rings, rings[1:]):
        for k in range(n):
            bm.faces.new((ra[k], ra[(k + 1) % n], rb[(k + 1) % n], rb[k]))
    bm.faces.new(list(reversed(rings[0])))
    bm.faces.new(rings[-1])
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = new_mesh_object(name, bm, collection)
    shade_smooth(ob, math.radians(50))
    return put(ob, key)


def rings_loft(name, rows, key, n=24, wobble=None, collection="HIGH"):
    """Closed hull from (z, rx, ry, cy) rows; wobble(z, theta) -> radial factor."""
    secs = []
    for z, rx, ry, cy in rows:
        sec = []
        for k in range(n):
            th = 2 * math.pi * k / n
            f = wobble(z, th) if wobble else 1.0
            sec.append((rx * f * math.sin(th), cy - ry * f * math.cos(th), z))
        secs.append(sec)
    ob = loft(name, secs, collection, smooth=True)
    return put(ob, key)


def surface(name, fn, nu, nv, thickness, key, collection="HIGH"):
    """Quad sheet from fn(u, v) -> (x, y, z), u, v in [0, 1], solidified (armour plates, panels)."""
    bm = bmesh.new()
    grid_v = [[bm.verts.new(fn(i / nu, j / nv)) for j in range(nv + 1)] for i in range(nu + 1)]
    for i in range(nu):
        for j in range(nv):
            bm.faces.new((grid_v[i][j], grid_v[i + 1][j], grid_v[i + 1][j + 1], grid_v[i][j + 1]))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = new_mesh_object(name, bm, collection)
    solidify(ob, thickness, offset=0.0)
    shade_smooth(ob, math.radians(40))
    return put(ob, key)


def spikes(name, roots, dirs, lengths, radius, key, sides=4, collection="HIGH"):
    """Many thin cones in one object: fur fringe, tassels, flame licks, mane."""
    bm = bmesh.new()
    for p, d, L in zip(roots, dirs, lengths):
        p, d = Vector(p), Vector(d).normalized()
        s, u = frame(d)
        base = [bm.verts.new(p + (s * math.cos(a) + u * math.sin(a)) * radius)
                for a in (2 * math.pi * k / sides for k in range(sides))]
        tip = bm.verts.new(p + d * L)
        for k in range(sides):
            bm.faces.new((base[k], base[(k + 1) % sides], tip))
        bm.faces.new(list(reversed(base)))
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    ob = new_mesh_object(name, bm, collection)
    return put(ob, key)


def ellipsoid(name, center, scale, key, u=20, v=12, collection="HIGH"):
    ob = sphere(name, 1.0, collection, center, u, v, scale)
    shade_smooth(ob, math.radians(60))
    return put(ob, key)


def jitter(a, b):
    return RNG.uniform(a, b)


# ---------------------------------------------------------------- head and hair
def build_head():
    head = ellipsoid("CH_LongVuong_Head_HIGH", (0, -0.02, 2.18), (0.085, 0.105, 0.15), "skin", 24, 16)
    me = head.data
    for vtx in me.vertices:            # local coords (centre 0, -0.02, 2.18): jaw narrows, face front fuller
        c = vtx.co
        t = min(1.0, max(0.0, -c.z / 0.15))
        c.x *= 1.0 - 0.38 * t
        if c.y < 0.0:
            c.y -= 0.012 * max(0.0, 1 - abs(c.x) / 0.085) * max(0.0, 1 - abs(c.z + 0.03) / 0.15)
    tube("CH_LongVuong_Nose_HIGH", [(0, -0.118, 2.215), (0, -0.13, 2.18)], [0.012, 0.009], "skin", 6)
    for sx, sd in ((1, "L"), (-1, "R")):
        put(sphere(f"CH_LongVuong_Eye_{sd}_HIGH", 1.0, "HIGH", (0.034 * sx, -0.112, 2.21), 12, 8, (0.013, 0.005, 0.007)),
            "glow")                                                    # delta: ember eyes
        tube(f"CH_LongVuong_Brow_{sd}_HIGH", [(0.012 * sx, -0.118, 2.232), (0.05 * sx, -0.112, 2.238)],
             [0.006, 0.004], "hair", 5)
        tube(f"CH_LongVuong_Ear_{sd}_HIGH", [(0.075 * sx, 0.0, 2.18), (0.12 * sx, 0.03, 2.23), (0.165 * sx, 0.055, 2.28)],
             [0.028, 0.018, 0.002], "skin", 6)
    spikes("CH_LongVuong_FlameMark_HIGH", [(0, -0.118, 2.26)], [(0, -0.2, 1)], [0.03], 0.008, "glow")   # delta

    cap = ellipsoid("CH_LongVuong_HairCap_HIGH", (0, 0.03, 2.235), (0.102, 0.132, 0.125), "hair", 24, 16)
    for vtx in cap.data.vertices:      # local coords (centre 0, 0.03, 2.235): tuck the lower front inside the skull
        c = vtx.co
        if c.y < -0.01 and c.z < -0.02:
            k = min(1.0, (-0.02 - c.z) / 0.05)
            c.y += (-0.01 - c.y) * 0.9 * k
    put(sphere("CH_LongVuong_HairCrown_HIGH", 1.0, "HIGH", (0, -0.02, 2.355), 12, 8, (0.035, 0.03, 0.02)), "silver")

    # long back hair (pass 2): smoother core, then two layers of thin clumps with wavy paths and
    # staggered tips so the mass reads as hair, not segments
    rings_loft("CH_LongVuong_HairCore_HIGH", [(z, rx * 0.84, ry * 0.78, cy) for z, rx, ry, cy in HAIR], "hair", 32)
    layers = ((23, 0.92, 0.55, 0.11, "a"), (15, 0.75, 0.75, 0.15, "b"))   # count, spread, depth, radius frac, tag
    for count, spread, depth, rfrac, tag in layers:
        for i in range(count):
            f = (i / (count - 1)) * 2 - 1
            phase = jitter(0, math.tau)
            pts, rad = [], []
            for r, (z, rx, ry, cy) in enumerate(HAIR[1:]):
                wave = 0.012 * math.sin(phase + r * 1.3)
                pts.append((f * rx * spread + wave, cy + ry * depth * (1 - f * f) ** 0.5 + jitter(-0.008, 0.008), z))
                rad.append(max(0.008, rx * rfrac * (1.15 - 0.45 * abs(f)) * jitter(0.85, 1.15)))
            tip_z = HAIR[-1][0] - jitter(-0.06, 0.16) - 0.12 * (1 - abs(f))
            pts.append((f * 0.06 + jitter(-0.02, 0.02), HAIR[-1][3] + 0.02, tip_z))
            rad.append(0.003)
            tube(f"CH_LongVuong_HairClump{tag}{i:02d}_HIGH", pts, rad, "hair", 7)
    for sx, sd in ((1, "L"), (-1, "R")):   # face-framing locks to the shoulders, then the long front strands
        tube(f"CH_LongVuong_SideLock_{sd}_HIGH",
             [(0.085 * sx, -0.06, 2.27), (0.105 * sx, -0.07, 2.12), (0.13 * sx, -0.05, 1.98), (0.16 * sx, -0.02, 1.88)],
             [0.03, 0.028, 0.02, 0.004], "hair", 8)
        tube(f"CH_LongVuong_Strand_{sd}_HIGH",
             [(0.09 * sx, -0.11, 2.17), (0.11 * sx, -0.22, 1.85), (0.11 * sx, -0.25, 1.45), (0.11 * sx, -0.26, 1.10)],
             [0.011, 0.01, 0.009, 0.004], "hair", 6)


def build_antlers():
    """Own branching (delta): main beam, a forward and an outward tine, flame tips."""
    for sx, sd in ((1, "L"), (-1, "R")):
        beam = [(0.05 * sx, 0.0, 2.30), (0.10 * sx, 0.02, 2.40), (0.13 * sx, 0.03, 2.48), (0.12 * sx, 0.04, 2.55)]
        tube(f"CH_LongVuong_Antler_{sd}_HIGH", beam, [0.022, 0.018, 0.012, 0.005], "antler", 8)
        tube(f"CH_LongVuong_AntlerTineOut_{sd}_HIGH", [(0.12 * sx, 0.02, 2.44), (0.16 * sx, 0.02, 2.47), (0.19 * sx, 0.02, 2.50)],
             [0.011, 0.008, 0.003], "antler", 6)
        tube(f"CH_LongVuong_AntlerTineFwd_{sd}_HIGH", [(0.10 * sx, 0.02, 2.40), (0.11 * sx, -0.03, 2.44), (0.11 * sx, -0.05, 2.48)],
             [0.010, 0.007, 0.003], "antler", 6)
        spikes(f"CH_LongVuong_AntlerFlame_{sd}_HIGH",
               [beam[-1], (0.19 * sx, 0.02, 2.50), (0.11 * sx, -0.05, 2.48)],
               [(-0.1 * sx, 0.05, 1), (0.3 * sx, 0, 1), (0, -0.3, 1)], [0.05, 0.035, 0.03], 0.008, "glow")


# ---------------------------------------------------------------- torso, belt, robe
def build_torso():
    rings_loft("CH_LongVuong_Undersuit_HIGH", TORSO, "cloth", 24)
    rings_loft("CH_LongVuong_Collar_HIGH", [(2.03, 0.095, 0.095, -0.005), (1.92, 0.125, 0.115, 0.0)], "silver", 20)

    def breast(u, v):
        z = 1.52 + v * (1.93 - 1.52)
        th = math.radians(-105 + 210 * u)
        p = ring_at(TORSO, z, th, 0.02)
        p.y -= 0.03 * math.exp(-(math.degrees(th) / 22.0) ** 2) * (0.4 + 0.6 * v)    # central ridge
        return p
    surface("CH_LongVuong_Breastplate_HIGH", breast, 24, 10, 0.018, "silver")

    def back(u, v):
        z = 1.52 + v * (1.90 - 1.52)
        return ring_at(TORSO, z, math.radians(75 + 210 * u), 0.02)
    surface("CH_LongVuong_Backplate_HIGH", back, 20, 8, 0.018, "silver")
    rings_loft("CH_LongVuong_Corset_HIGH", [(1.53, 0.30, 0.225, 0.0), (1.38, 0.30, 0.225, 0.0)], "steel", 24)

    # chest emblem (delta): flame-wreathed dragon pearl
    put(sphere("CH_LongVuong_Pearl_HIGH", 1.0, "HIGH", (0, -0.245, 1.60), 16, 10, (0.034, 0.025, 0.034)), "glow")
    roots, dirs = [], []
    for k in range(7):
        a = math.radians(-90 + k * 30)
        roots.append((0.03 * math.cos(a), -0.24, 1.60 + 0.03 * math.sin(a)))
        dirs.append((math.cos(a), -0.2, math.sin(a) + 0.6))
    spikes("CH_LongVuong_PearlFlames_HIGH", roots, dirs, [0.045] * 7, 0.01, "glow")
    surface("CH_LongVuong_PearlFrame_HIGH",
            lambda u, v: (0.075 * (2 * u - 1), -0.232 - 0.01 * (1 - (2 * u - 1) ** 2), 1.54 + 0.13 * v), 6, 4, 0.01, "silver")


def build_robe():
    flute = lambda z, th: 1.0 + (0.015 * math.sin(10 * th) if z < 1.0 else 0.0)
    rings_loft("CH_LongVuong_Robe_HIGH", ROBE, "cloth", 40, flute)
    rings_loft("CH_LongVuong_RobeLower_HIGH", [(0.44, 0.46, 0.33, 0.08), (0.35, 0.42, 0.325, 0.08), (0.261, 0.37, 0.32, 0.08)],
               "cloth", 40, lambda z, th: 1.0 + 0.03 * math.sin(12 * th))
    rings_loft("CH_LongVuong_Belt_HIGH", [(1.38, 0.31, 0.23, 0.0), (1.29, 0.31, 0.23, 0.0)], "silver", 24)
    put(box("CH_LongVuong_Buckle_HIGH", (0.11, 0.03, 0.09), "HIGH", (0, -0.245, 1.335)), "silver")
    put(sphere("CH_LongVuong_BuckleGem_HIGH", 1.0, "HIGH", (0, -0.262, 1.335), 10, 6, (0.018, 0.008, 0.018)), "glow")

    def tabard(u, v, sy=-1):
        z = 1.29 - v * (1.29 - 0.47)
        half = 0.16 + 0.11 * v
        x = (2 * u - 1) * half
        rx, ry, cy = interp(ROBE, z)
        t = min(0.98, abs(x) / (rx + 0.03))
        y = cy + sy * ((ry + 0.025) * math.sqrt(1 - t * t))
        z -= 0.05 * (1 - (2 * u - 1) ** 2) * v ** 3          # pointed lower edge
        return (x, y, z)
    surface("CH_LongVuong_TabardFront_HIGH", tabard, 10, 16, 0.014, "silver")
    surface("CH_LongVuong_TabardBack_HIGH", lambda u, v: tabard(u, v, 1), 10, 16, 0.014, "silver")
    for sx, sd in ((1, "L"), (-1, "R")):
        def side(u, v, sx=sx, grow=0.015):
            z = 1.24 - v * (1.24 - 0.53)
            th = math.radians(40 + 46 * u) * sx
            p = ring_at(ROBE, z, th, grow)
            p.z -= 0.04 * math.sin(math.pi * u) * v ** 2
            return tuple(p)
        surface(f"CH_LongVuong_SidePanel_{sd}_HIGH", side, 8, 14, 0.012, "silver")
        surface(f"CH_LongVuong_SidePattern_{sd}_HIGH", lambda u, v, side=side: side(0.15 + 0.7 * u, 0.15 + 0.6 * v, grow=0.027),
                6, 8, 0.004, "ember")                       # delta: fire-red motif where the reference is pink

    # fur hem: band + fringe of hanging tufts
    rings_loft("CH_LongVuong_HemFur_HIGH", [(0.56, 0.615, 0.375, 0.08), (0.46, 0.525, 0.375, 0.10)], "fur", 40,
               lambda z, th: 1.0 + 0.04 * math.sin(17 * th))
    roots, dirs, lens = [], [], []
    for k in range(72):
        th = 2 * math.pi * k / 72 + jitter(-0.03, 0.03)
        z = jitter(0.46, 0.52)
        p = ring_at(ROBE, z, th, 0.015)
        roots.append(p)
        dirs.append((math.sin(th) * 0.2, -math.cos(th) * 0.2, -1))
        lens.append(jitter(0.06, 0.11))
    spikes("CH_LongVuong_HemFringe_HIGH", roots, dirs, lens, 0.022, "fur", 5)



def build_robe_borders():
    """Pass 2: raised silver borders around the tabards and side panels; scalloped tabard tip."""
    def tabard_edge(sy):
        def at(u, v):
            z = 1.29 - v * (1.29 - 0.47)
            half = 0.16 + 0.11 * v
            x = (2 * u - 1) * half
            rx, ry, cy = interp(ROBE, z)
            t = min(0.98, abs(x) / (rx + 0.03))
            y = cy + sy * ((ry + 0.04) * math.sqrt(1 - t * t))
            z -= 0.05 * (1 - (2 * u - 1) ** 2) * v ** 3
            return Vector((x, y, z))
        return at
    for sy, tag in ((-1, "Front"), (1, "Back")):
        at = tabard_edge(sy)
        edge = [at(0.0, k / 12) for k in range(13)] + [at(k / 10, 1.0) for k in range(1, 10)] + \
               [at(1.0, 1 - k / 12) for k in range(13)]
        tube(f"CH_LongVuong_TabardBorder{tag}_HIGH", edge, [0.012] * len(edge), "silver", 6)
        # scalloped ornament where the tabard ends
        for k in range(5):
            c = at(0.1 + 0.2 * k, 0.97)
            tube(f"CH_LongVuong_TabardScallop{tag}{k}_HIGH", [c + Vector((-0.02, 0, 0)), c + Vector((0, 0, -0.025)),
                 c + Vector((0.02, 0, 0))], [0.008, 0.01, 0.008], "silver", 6)
    for sx, sd in ((1, "L"), (-1, "R")):
        def side(u, v, sx=sx):
            z = 1.24 - v * (1.24 - 0.53)
            p = ring_at(ROBE, z, math.radians(40 + 46 * u) * sx, 0.026)
            p.z -= 0.04 * math.sin(math.pi * u) * v ** 2
            return p
        edge = [side(0.0, k / 12) for k in range(13)] + [side(k / 8, 1.0) for k in range(1, 8)] + \
               [side(1.0, 1 - k / 12) for k in range(13)]
        tube(f"CH_LongVuong_SideBorder_{sd}_HIGH", edge, [0.011] * len(edge), "silver", 6)


# ---------------------------------------------------------------- pauldrons
def build_pauldrons():
    """Pass 2: each tier is an arched shell that wraps over the shoulder (front/back edges roll down),
    slopes out and down, then sweeps up at the tip; steel trim on the edges; fur fringe under the last."""
    tiers = [  # x0, x1, z inner, drop, y0, y1, arch, lift, material
        (0.14, 0.58, 1.965, 0.13, -0.20, 0.24, 0.04, 0.07, "silver"),
        (0.19, 0.66, 1.915, 0.15, -0.225, 0.265, 0.035, 0.08, "silver"),
        (0.24, 0.73, 1.86, 0.17, -0.245, 0.285, 0.03, 0.10, "silver"),
    ]
    for sx, sd in ((1, "L"), (-1, "R")):
        for i, (x0, x1, zin, drop, y0, y1, arch, lift, key) in enumerate(tiers):
            def fn(u, v, x0=x0, x1=x1, zin=zin, drop=drop, y0=y0, y1=y1, arch=arch, lift=lift):
                x = x0 + (x1 - x0) * u
                w = 2 * v - 1
                # outer end widens a little and its corners flick up
                half = 0.5 * (y1 - y0) * (1 + 0.12 * u)
                y = 0.5 * (y0 + y1) + w * half
                z = (zin - drop * u + lift * u ** 3.5
                     + arch * (1 - w * w) * (1 - 0.35 * u)       # arched over the shoulder
                     - 0.10 * w ** 4 * (1 - 0.6 * u ** 3))        # front/back edges roll down
                return (x * sx, y, z)
            surface(f"CH_LongVuong_Pauldron{i}_{sd}_HIGH", fn, 20, 14, 0.018, key)
            # steel trim along front, outer and back edges
            edge = [fn(k / 12, 0.0) for k in range(13)] + [fn(1.0, k / 10) for k in range(1, 10)] + \
                   [fn(1 - k / 12, 1.0) for k in range(13)]
            tube(f"CH_LongVuong_PauldronTrim{i}_{sd}_HIGH", edge, [0.011] * len(edge), "steel", 6)
            if i == 0:   # ember edge (delta)
                tube(f"CH_LongVuong_PauldronEmber_{sd}_HIGH",
                     [(Vector(fn(k / 10, 0.02)) + Vector((0, -0.012, 0.008))) for k in range(11)], [0.006] * 11, "glow", 5)
            # curls at the outer corners of the lowest tier only
            for j, v in enumerate((0.0, 1.0) if i == 2 else ()):
                c = Vector(fn(1.0, v))
                dy = -1 if v == 0 else 1
                tube(f"CH_LongVuong_PauldronCurl{i}{j}_{sd}_HIGH",
                     [c, c + Vector((0.035 * sx, 0.015 * dy, 0.02)), c + Vector((0.045 * sx, 0.01 * dy, 0.05)),
                      c + Vector((0.025 * sx, 0.0, 0.065))], [0.016, 0.013, 0.009, 0.002], "silver", 6)
        # fur flare from under the lowest tier
        x0, x1, zin, drop, y0, y1, arch, lift = tiers[2][:8]
        last = lambda u, v: Vector((x0 + (x1 - x0) * u, 0.5 * (y0 + y1) + (2 * v - 1) * 0.5 * (y1 - y0) * (1 + 0.12 * u),
                                    zin - drop * u + lift * u ** 3.5 + arch * (1 - (2 * v - 1) ** 2) * (1 - 0.35 * u)
                                    - 0.10 * (2 * v - 1) ** 4 * (1 - 0.6 * u ** 3)))
        roots, dirs, lens = [], [], []
        for k in range(30):
            u = k / 29
            for v, dy in ((0.0, -0.5), (1.0, 0.5)):
                p = last(u, v) - Vector((0, 0, 0.012))
                roots.append((p.x * sx, p.y, p.z))
                dirs.append((0.2 * sx, dy * 0.6, -1))
                lens.append(jitter(0.05, 0.10))
        for k in range(16):
            p = last(0.97, k / 15) - Vector((0, 0, 0.015))
            roots.append((p.x * sx, p.y, p.z))
            dirs.append((0.8 * sx, 0, -0.7))
            lens.append(jitter(0.06, 0.11))
        spikes(f"CH_LongVuong_PauldronFur_{sd}_HIGH", roots, dirs, lens, 0.02, "fur", 5)
        build_crest(sx, sd)


def build_crest(sx, sd):
    """Pass 2 dragon head on the pauldron (delta: open jaw, flame tongue): bigger skull, brow ridges,
    swept-back horns, mane of blades down the back of the neck."""
    X = lambda x: 0.25 * sx + x * sx
    tube(f"CH_LongVuong_CrestNeck_{sd}_HIGH", [(X(0), 0.16, 1.96), (X(0), 0.10, 2.04), (X(0), 0.04, 2.11)],
         [0.07, 0.075, 0.07], "silver", 12)
    tube(f"CH_LongVuong_CrestSkull_{sd}_HIGH",
         [(X(0), 0.07, 2.10), (X(0), 0.0, 2.17), (X(0), -0.08, 2.19), (X(0), -0.15, 2.17), (X(0), -0.19, 2.15)],
         [0.07, 0.085, 0.07, 0.045, 0.02], "silver", 12)
    tube(f"CH_LongVuong_CrestJaw_{sd}_HIGH", [(X(0), 0.02, 2.09), (X(0), -0.08, 2.08), (X(0), -0.16, 2.10)],
         [0.042, 0.034, 0.012], "silver", 10)
    for b in (-1, 1):
        tube(f"CH_LongVuong_CrestBrow{b + 1}_{sd}_HIGH",
             [(X(0.035 * b), -0.03, 2.23), (X(0.05 * b), 0.03, 2.25), (X(0.04 * b), 0.08, 2.24)], [0.016, 0.014, 0.004],
             "silver", 6)
        put(sphere(f"CH_LongVuong_CrestEye{b + 1}_{sd}_HIGH", 1.0, "HIGH", (X(0.045 * b), -0.07, 2.205), 8, 6,
                   (0.012, 0.016, 0.009)), "glow")
    spikes(f"CH_LongVuong_CrestTongue_{sd}_HIGH", [(X(0), -0.13, 2.12), (X(0), -0.13, 2.11)],
           [(0, -1, 0.25), (0.25 * sx, -1, -0.1)], [0.11, 0.07], 0.016, "glow")
    for j, (dx, h, back) in enumerate(((0.035, 0.12, 0.17), (-0.035, 0.10, 0.14))):
        tube(f"CH_LongVuong_CrestHorn{j}_{sd}_HIGH",
             [(X(dx), 0.02, 2.22), (X(dx * 1.4), 0.07, 2.22 + h * 0.6), (X(dx * 1.8), back, 2.22 + h),
              (X(dx * 1.6), back + 0.05, 2.22 + h * 1.05)], [0.02, 0.015, 0.008, 0.002], "silver", 6)
    roots = [(X(0), 0.12, 2.06), (X(0), 0.07, 2.12), (X(0), 0.16, 2.0), (X(0.03), 0.10, 2.08), (X(-0.03), 0.10, 2.08)]
    dirs = [(0, 1, 0.9), (0, 0.7, 1), (0, 1, 0.3), (0.5 * sx, 0.8, 0.7), (-0.5 * sx, 0.8, 0.7)]
    spikes(f"CH_LongVuong_CrestMane_{sd}_HIGH", roots, dirs, [0.12, 0.13, 0.09, 0.09, 0.09], 0.022, "silver")


# ---------------------------------------------------------------- arms
def build_arms():
    for sx, sd in ((1, "L"), (-1, "R")):
        sh, el, wr = Vector((0.30 * sx, 0.0, 1.86)), Vector((0.62 * sx, 0.02, 1.43)), Vector((0.85 * sx, 0.02, 1.17))
        pts = [sh.lerp(el, t) for t in (0, 0.25, 0.5, 0.75, 1.0)]
        tube(f"CH_LongVuong_Sleeve_{sd}_HIGH", pts, [0.12, 0.155, 0.16, 0.135, 0.095], "cloth", 16,
             wobble=lambda i, k: 1.0 + 0.04 * math.sin(6 * 2 * math.pi * k / 16))
        # fur trim hanging from the sleeve head under the pauldron
        roots, dirs, lens = [], [], []
        for k in range(18):
            a = 2 * math.pi * k / 18
            s, u = frame((el - sh).normalized())
            p = sh.lerp(el, 0.2) + (s * math.cos(a) + u * math.sin(a)) * 0.15
            roots.append(p)
            dirs.append((0.2 * sx, 0, -1))
            lens.append(jitter(0.06, 0.12))
        spikes(f"CH_LongVuong_SleeveFur_{sd}_HIGH", roots, dirs, lens, 0.022, "fur", 5)

        gpts = [el.lerp(wr, t) for t in (0, 0.15, 0.5, 0.85, 1.0)]
        tube(f"CH_LongVuong_Gauntlet_{sd}_HIGH", gpts, [0.09, 0.075, 0.068, 0.058, 0.055], "steel", 14)
        for j, t in enumerate((0.2, 0.55, 0.9)):
            p = el.lerp(wr, t)
            d = (wr - el).normalized()
            tube(f"CH_LongVuong_GauntletBand{j}_{sd}_HIGH", [p - d * 0.012, p + d * 0.012], [0.075 - 0.012 * t] * 2, "silver", 14)
        d = (wr - el).normalized()
        out = Vector((sx, 0, 0.3)).normalized()
        spikes(f"CH_LongVuong_GauntletFins_{sd}_HIGH",
               [el.lerp(wr, t) + out * 0.06 for t in (0.25, 0.45, 0.65)], [(-d + out * 0.6)] * 3, [0.09, 0.08, 0.07], 0.018,
               "silver", 4)
        tube(f"CH_LongVuong_GauntletEmber_{sd}_HIGH", [el.lerp(wr, 0.25) + Vector((0, -0.07, 0)), wr + Vector((0, -0.058, 0))],
             [0.006, 0.006], "glow", 5)                                                     # delta
        build_hand(sx, sd, wr, d)


def build_hand(sx, sd, wr, d):
    inward = Vector((-sx, 0, 0))
    palm_end = wr + d * 0.07
    tube(f"CH_LongVuong_Palm_{sd}_HIGH", [wr, wr + d * 0.035, palm_end], [0.04, 0.042, 0.038], "steel", 10)
    for j, yo in enumerate((-0.03, -0.01, 0.01, 0.03)):
        base = palm_end + Vector((0, yo, 0))
        d1 = (d + inward * 0.15).normalized()
        d2 = (d + inward * 0.45).normalized()
        d3 = (d + inward * 0.35).normalized()
        p1 = base + d1 * 0.035
        p2 = p1 + d2 * 0.03
        tube(f"CH_LongVuong_Finger{j}_{sd}_HIGH", [base, p1, p2], [0.011, 0.01, 0.009], "steel", 6)
        spikes(f"CH_LongVuong_Claw{j}_{sd}_HIGH", [p2], [d3], [0.085], 0.009, "steel", 5)
    tb = wr + d * 0.03 + Vector((0, -0.04, 0))
    tube(f"CH_LongVuong_Thumb_{sd}_HIGH", [tb, tb + Vector((0.01 * sx, -0.03, -0.02))], [0.012, 0.01], "steel", 6)
    spikes(f"CH_LongVuong_ThumbClaw_{sd}_HIGH", [tb + Vector((0.01 * sx, -0.03, -0.02))], [(0, -0.6, -1)], [0.04], 0.009, "steel")


# ---------------------------------------------------------------- ornaments
def build_braids():
    for sx, sd in ((1, "L"), (-1, "R")):
        path = [Vector(p) for p in ((0.22 * sx, -0.20, 1.32), (0.38 * sx, -0.25, 1.17), (0.52 * sx, -0.22, 1.00),
                                    (0.62 * sx, -0.14, 0.90))]
        bm = bmesh.new()
        n = 0
        for a, b in zip(path, path[1:]):
            for k in range(6):          # braid = chained beads alternating sides
                t = (k + 0.5) / 6
                p = a.lerp(b, t)
                s, u = frame((b - a).normalized())
                p = p + s * (0.012 if (n % 2) else -0.012)
                bmesh.ops.create_uvsphere(bm, u_segments=8, v_segments=5, radius=0.024,
                                          matrix=Matrix.Translation(p) @ Matrix.Diagonal((1, 1, 1.4, 1)))
                n += 1
        ob = new_mesh_object(f"CH_LongVuong_Braid_{sd}_HIGH", bm, "HIGH")
        shade_smooth(ob, math.radians(60))
        put(ob, "cloth")
        top = path[-1]
        tube(f"CH_LongVuong_TasselKnot_{sd}_HIGH", [top, top + Vector((0, 0, -0.03))], [0.03, 0.03], "silver", 10)
        roots, dirs, lens = [], [], []
        for k in range(16):
            a = 2 * math.pi * k / 16
            roots.append(top + Vector((0.025 * math.cos(a), 0.025 * math.sin(a), -0.03)))
            dirs.append((0.25 * math.cos(a), 0.25 * math.sin(a), -1))
            lens.append(jitter(0.10, 0.13))
        spikes(f"CH_LongVuong_Tassel_{sd}_HIGH", roots, dirs, lens, 0.014, "fur", 4)
        spikes(f"CH_LongVuong_TasselFlame_{sd}_HIGH", [top + Vector((0, 0, -0.10))], [(0, 0, -1)], [0.07], 0.022, "glow")  # delta


# ---------------------------------------------------------------- remap, legs (final metres)
def remap_to_final_calibration():
    bpy.context.view_layer.update()
    for ob in [o for o in bpy.data.objects if o.get("owner") == OWNER_TAG]:
        M = OLD_TO_NEW @ ob.matrix_world
        if ob.type == "MESH":
            ob.data.transform(M)
            for md in ob.modifiers:
                if md.type == "SOLIDIFY":
                    md.thickness *= OLD_SCALE
        ob.matrix_world = Matrix.Identity(4)


def build_legs():
    """Pass 2 boots, ref/front_full.png (0.0045 m/px, ground row 602): flared cuff, knee cop pointing up,
    two-segment ridged shin guard, ankle ring, pointed toe cap. Shafts 0.14-0.16 m, soles splay to +-0.31."""
    for sx, sd in ((1, "L"), (-1, "R")):
        cx = 0.25 * sx
        tube(f"CH_LongVuong_Thigh_{sd}_HIGH", [(0.09 * sx, 0.0, 1.30), (0.16 * sx, -0.005, 0.95), (0.22 * sx, -0.01, 0.64)],
             [0.085, 0.075, 0.065], "cloth", 12)
        tube(f"CH_LongVuong_BootShaft_{sd}_HIGH",
             [(cx, 0.0, 0.57), (cx, 0.0, 0.48), (cx * 1.008, 0.0, 0.30), (cx * 1.02, 0.0, 0.17), (cx * 1.04, -0.01, 0.09)],
             [0.088, 0.078, 0.073, 0.066, 0.072], "steel", 16)
        # flared cuff: open cone rising to a point in front
        def cuff(u, v):
            a = 2 * math.pi * u
            r = 0.09 + 0.035 * v
            z = 0.53 + 0.07 * v + 0.05 * v * max(0.0, math.cos(a)) ** 4
            return (cx + r * math.sin(a), -r * math.cos(a), z)
        surface(f"CH_LongVuong_BootCuff_{sd}_HIGH", cuff, 24, 4, 0.012, "steel")

        def plate(z0, z1, r, peak):
            def f(u, v):
                a = math.radians(-70 + 140 * u)
                z = z0 + v * (z1 - z0)
                rr = r + 0.012 * math.exp(-((u - 0.5) / 0.12) ** 2)           # central ridge
                z += peak * (1 - (2 * u - 1) ** 2) * v ** 5                     # pointed top
                return (cx + rr * math.sin(a), -rr * math.cos(a), z)
            return f
        surface(f"CH_LongVuong_ShinGuardLow_{sd}_HIGH", plate(0.11, 0.31, 0.077, 0.04), 12, 8, 0.012, "silver")
        surface(f"CH_LongVuong_ShinGuardHigh_{sd}_HIGH", plate(0.29, 0.50, 0.082, 0.05), 12, 8, 0.012, "silver")
        surface(f"CH_LongVuong_KneeCop_{sd}_HIGH", plate(0.47, 0.60, 0.092, 0.09), 12, 8, 0.014, "silver")
        tube(f"CH_LongVuong_AnkleRing_{sd}_HIGH", [(cx * 1.04, -0.01, 0.105), (cx * 1.04, -0.01, 0.125)], [0.08, 0.08],
             "silver", 16)
        foot = [Vector((0.265 * sx, 0.06, 0.06)), Vector((0.28 * sx, -0.04, 0.06)), Vector((0.30 * sx, -0.14, 0.045)),
                Vector((0.315 * sx, -0.22, 0.025))]
        tube(f"CH_LongVuong_BootFoot_{sd}_HIGH", foot, [0.062, 0.068, 0.052, 0.008], "steel", 12)
        tube(f"CH_LongVuong_ToeCap_{sd}_HIGH", [foot[1] + Vector((0, 0, 0.008)), foot[2] + Vector((0, 0, 0.006)), foot[3]],
             [0.072, 0.058, 0.01], "silver", 12)
        put(box(f"CH_LongVuong_BootSole_{sd}_HIGH", (0.13, 0.29, 0.022), "HIGH", (0.287 * sx, -0.07, 0.011),
                (0, 0, math.radians(-8 * sx))), "steel")
        put(box(f"CH_LongVuong_BootHeel_{sd}_HIGH", (0.09, 0.07, 0.04), "HIGH", (0.265 * sx, 0.05, 0.02)), "steel")


def build(scene, args):
    MAT.update(make_materials())
    build_head()
    build_antlers()
    build_torso()
    build_robe()
    build_robe_borders()
    build_pauldrons()
    build_arms()
    build_braids()
    remap_to_final_calibration()
    build_legs()
    owned = [o for o in bpy.data.objects if o.get("owner") == OWNER_TAG]
    log("forms objects", len(owned))


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(argv_after_dashes())
    scene = open_master(a.master)
    clear_owned()
    build(scene, a)
    save_master(a.master)
    log("saved", a.master)
