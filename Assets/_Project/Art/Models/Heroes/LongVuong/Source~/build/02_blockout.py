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

PHASE = "02_blockout"          # change per phase
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
# CH_LongVuong blockout. Units: metres, Z up, faces -Y, origin at ground centre.
# Upper body and robe were measured on the sheets (ref/front.png axis 198, side 150, back 197) in the
# first calibration: 0.005282 m/px, ground row 495. ref/front_full.png later showed the real ground
# (canvas row 565), so those constants stay as measured and OLD_TO_NEW maps them to the final
# calibration (0.004561 m/px, ground 565): p' = 0.8635 * p + (0, 0, 0.3193). Legs and boots are built
# after the remap, directly in final metres from ref/front_full.png (0.0045 m/px, ground row 602).
# Primary masses only; appendages are separate rough objects.

S = 0.004561                       # m/px, final calibration (asset-brief SCALE)
OLD_SCALE = 0.004561 / 0.005282    # 0.8635
OLD_LIFT = 70 * 0.004561           # 0.3193 m: ground moved from canvas row 495 to 565
OLD_TO_NEW = Matrix.Translation((0, 0, OLD_LIFT)) @ Matrix.Scale(OLD_SCALE, 4)


def limb(name, a, b, r1, r2, segments=10, collection="LOW", squash=1.0):
    """Tapered cylinder from a to b (radius r1 -> r2); squash scales the local Y of the section."""
    a, b = Vector(a), Vector(b)
    d = b - a
    bm = bmesh.new()
    bmesh.ops.create_cone(bm, cap_ends=True, cap_tris=False, segments=segments,
                          radius1=r1, radius2=r2, depth=d.length)
    if squash != 1.0:
        bmesh.ops.scale(bm, vec=(1, squash, 1), verts=bm.verts)
    rot = Vector((0, 0, 1)).rotation_difference(d.normalized()).to_matrix().to_4x4()
    bmesh.ops.transform(bm, matrix=Matrix.Translation((a + b) / 2) @ rot, verts=bm.verts)
    return new_mesh_object(name, bm, collection)


def ellipse(z, rx, ry, cy=0.0, n=16):
    return [(rx * math.cos(2 * math.pi * i / n), cy + ry * math.sin(2 * math.pi * i / n), z) for i in range(n)]


def lofted(name, rows, n=16):
    """rows = [(z, rx, ry, cy), ...] top to bottom."""
    return loft(name, [ellipse(z, rx, ry, cy, n) for z, rx, ry, cy in rows], smooth=True)


def build(scene, args):
    # ---- head (front: crown z 2.34, chin 2.02; side: face front y -0.143)
    sphere("CH_LongVuong_Head_LOD0", 1.0, location=(0, -0.02, 2.18), scale=(0.09, 0.12, 0.16))
    sphere("CH_LongVuong_HairCap_LOD0", 1.0, location=(0, 0.03, 2.24), scale=(0.135, 0.15, 0.115))
    for sx, sd in ((1, "L"), (-1, "R")):
        limb(f"CH_LongVuong_Ear_{sd}_LOD0", (0.085 * sx, 0.0, 2.18), (0.16 * sx, 0.05, 2.27), 0.025, 0.0, 4)
        # antlers: back view tips at x +-0.11, z 2.55
        curve_tube(f"CH_LongVuong_Antler_{sd}_LOD0",
                   [(0.05 * sx, 0.0, 2.30), (0.10 * sx, 0.02, 2.40), (0.13 * sx, 0.03, 2.48), (0.12 * sx, 0.04, 2.55)],
                   0.022, taper_to=0.3)
        curve_tube(f"CH_LongVuong_AntlerTine_{sd}_LOD0",
                   [(0.12 * sx, 0.02, 2.44), (0.16 * sx, 0.02, 2.47), (0.19 * sx, 0.02, 2.50)], 0.012, taper_to=0.3)
        # thin front strands hanging to the belt (front x +-0.11)
        curve_tube(f"CH_LongVuong_Strand_{sd}_LOD0",
                   [(0.09 * sx, -0.11, 2.17), (0.11 * sx, -0.22, 1.85), (0.11 * sx, -0.25, 1.45), (0.11 * sx, -0.26, 1.10)],
                   0.010)
    limb("CH_LongVuong_Neck_LOD0", (0, 0, 1.92), (0, -0.01, 2.06), 0.065, 0.06)

    # ---- long hair mass behind (side: back edge y +0.42 @1.93, +0.60 @1.53, +0.70 @1.14, +0.52 @0.74)
    lofted("CH_LongVuong_HairBack_LOD0", [
        (2.30, 0.13, 0.12, 0.04), (2.10, 0.16, 0.15, 0.12), (1.90, 0.22, 0.18, 0.22),
        (1.60, 0.44, 0.20, 0.36), (1.30, 0.50, 0.20, 0.45), (1.00, 0.40, 0.18, 0.50),
        (0.80, 0.26, 0.12, 0.40), (0.62, 0.10, 0.05, 0.36)])

    # ---- torso (side: chest front -0.217 @1.61, -0.13 @1.93)
    lofted("CH_LongVuong_Torso_LOD0", [
        (1.95, 0.15, 0.13, 0.0), (1.80, 0.27, 0.20, 0.0), (1.65, 0.30, 0.22, 0.0),
        (1.50, 0.28, 0.21, 0.0), (1.33, 0.27, 0.20, 0.0)])
    lofted("CH_LongVuong_Belt_LOD0", [(1.38, 0.31, 0.23, 0.0), (1.29, 0.31, 0.23, 0.0)])

    # ---- robe (front half-widths per row of the matte; side front edge -0.23 .. -0.30)
    lofted("CH_LongVuong_Robe_LOD0", [
        (1.32, 0.30, 0.22, 0.0), (1.14, 0.38, 0.25, 0.0), (0.98, 0.47, 0.27, 0.0), (0.90, 0.53, 0.28, 0.0),
        (0.74, 0.60, 0.31, 0.02), (0.66, 0.63, 0.33, 0.04), (0.58, 0.61, 0.35, 0.07), (0.50, 0.58, 0.36, 0.09),
        (0.44, 0.51, 0.36, 0.10)], n=20)
    lofted("CH_LongVuong_RobeHemFur_LOD0", [(0.56, 0.63, 0.37, 0.08), (0.42, 0.51, 0.37, 0.10)], n=20)
    lofted("CH_LongVuong_RobeLower_LOD0", [(0.44, 0.46, 0.33, 0.08), (0.261, 0.37, 0.32, 0.08)], n=20)
    box("CH_LongVuong_TabardFront_LOD0", (0.40, 0.03, 0.82), location=(0, -0.30, 0.86),
        rotation=(math.radians(-6.4), 0, 0))

    for sx, sd in ((1, "L"), (-1, "R")):
        ry = math.radians(20 * sx)
        # ---- pauldrons: span +-0.776 at z 1.77 (fur tips); crest top z 2.29 at x +-0.24
        box(f"CH_LongVuong_Pauldron_{sd}_LOD0", (0.64, 0.46, 0.12), location=(0.46 * sx, 0.04, 1.85), rotation=(0, ry, 0))
        sphere(f"CH_LongVuong_PauldronFur_{sd}_LOD0", 1.0, location=(0.55 * sx, 0.03, 1.72),
               scale=(0.27, 0.25, 0.045)).rotation_euler = (0, math.radians(22 * sx), 0)
        box(f"CH_LongVuong_PauldronCrest_{sd}_LOD0", (0.17, 0.34, 0.28), location=(0.26 * sx, 0.0, 2.06))
        limb(f"CH_LongVuong_CrestHorn_{sd}_LOD0", (0.27 * sx, -0.05, 2.18), (0.27 * sx, 0.02, 2.29), 0.035, 0.0, 6)

        # ---- arms, A-pose: shoulder (0.36, 1.82) elbow (0.60, 1.45) wrist (0.84, 1.22) claws (1.0, 0.98)
        limb(f"CH_LongVuong_Sleeve_{sd}_LOD0", (0.34 * sx, 0.0, 1.84), (0.62 * sx, 0.02, 1.43), 0.15, 0.10)
        sphere(f"CH_LongVuong_SleeveFur_{sd}_LOD0", 1.0, location=(0.42 * sx, 0.0, 1.57),
               scale=(0.12, 0.14, 0.22)).rotation_euler = (0, math.radians(-30 * sx), 0)
        limb(f"CH_LongVuong_Gauntlet_{sd}_LOD0", (0.62 * sx, 0.02, 1.43), (0.85 * sx, 0.02, 1.17), 0.07, 0.05)
        limb(f"CH_LongVuong_Hand_{sd}_LOD0", (0.85 * sx, 0.02, 1.17), (0.92 * sx, 0.02, 1.06), 0.055, 0.045, 6, squash=0.5)
        for i, y in enumerate((-0.03, -0.01, 0.01, 0.03)):
            limb(f"CH_LongVuong_Claw{i}_{sd}_LOD0", (0.91 * sx, y + 0.02, 1.07), (1.01 * sx, y + 0.02, 0.96), 0.012, 0.0, 4)

        # ---- braided cords from the belt to the tassels (front: tassels x +-0.62, z 0.82-0.90)
        curve_tube(f"CH_LongVuong_Braid_{sd}_LOD0",
                   [(0.22 * sx, -0.20, 1.32), (0.38 * sx, -0.25, 1.17), (0.52 * sx, -0.22, 1.00), (0.62 * sx, -0.14, 0.90)],
                   0.03)
        limb(f"CH_LongVuong_Tassel_{sd}_LOD0", (0.62 * sx, -0.14, 0.90), (0.63 * sx, -0.14, 0.76), 0.025, 0.06, 8)

    remap_to_final_calibration()
    build_legs()
    log("blockout objects", len([o for o in bpy.data.objects if o.get("owner") == OWNER_TAG]))


def remap_to_final_calibration():
    """Bake OLD_TO_NEW into every object built so far (curves: their control points)."""
    bpy.context.view_layer.update()
    for ob in [o for o in bpy.data.objects if o.get("owner") == OWNER_TAG]:
        M = OLD_TO_NEW @ ob.matrix_world
        if ob.type == "MESH":
            ob.data.transform(M)
        elif ob.type == "CURVE":
            for sp in ob.data.splines:
                for pt in sp.points:
                    co = M @ Vector(pt.co[:3])
                    pt.co = (co.x, co.y, co.z, pt.co[3])
            ob.data.bevel_depth *= OLD_SCALE
        ob.matrix_world = Matrix.Identity(4)


def build_legs():
    """ref/front_full.png, 0.0045 m/px, ground row 602, axis 258: boot shafts at x +-0.25 (rows 500-560,
    0.135-0.16 m wide), boot tops hidden under the robe at z 0.55, feet splay out to x +-0.31 at the sole."""
    for sx, sd in ((1, "L"), (-1, "R")):
        limb(f"CH_LongVuong_Thigh_{sd}_LOD0", (0.09 * sx, 0.0, 1.30), (0.22 * sx, -0.01, 0.64), 0.085, 0.065)
        limb(f"CH_LongVuong_Shin_{sd}_LOD0", (0.22 * sx, -0.01, 0.64), (0.25 * sx, 0.0, 0.12), 0.065, 0.05)
        limb(f"CH_LongVuong_BootShaft_{sd}_LOD0", (0.25 * sx, 0.0, 0.56), (0.255 * sx, 0.0, 0.10), 0.085, 0.072)
        box(f"CH_LongVuong_BootFoot_{sd}_LOD0", (0.17, 0.30, 0.11), location=(0.29 * sx, -0.06, 0.055),
            rotation=(0, 0, math.radians(-10 * sx)))


if __name__ == "__main__":
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(argv_after_dashes())
    scene = open_master(a.master)
    clear_owned()
    build(scene, a)
    save_master(a.master)
    log("saved", a.master)
