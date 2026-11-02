"""Step 1b (C-08): remove the bolt tips that poke out of the hip quiver into the crossbow stock (CH_LinhBang).

Run after 01_lowpoly.py, before 02_rig.py: blender --background --python build/01b_trim.py -- --master LinhBang_master.blend
Why: the stock rests on the quiver behind the right hip and, after decimation, its long faces share vertices with the
grey bolt tips. The stock follows the right hand and the quiver the hips, so any split there tears a sheet across the
gap when the arm moves. Without the bolt tips everything outside x -0.35 in that box is stock. The quiver tube stays.
Then the crossbow (stock, string, ice limbs) is scaled to crossbow.SCALE about the right-hand grip: at source size
it hides the left arm when held and its butt runs through the chest (see crossbow.py).
Only the decimated LOD0 is trimmed; HIGH keeps the full source.
"""
import sys, os, argparse
import bpy, bmesh
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import crossbow

BODY = "CH_LinhBang_Body_LOD0"
# Bolt tips: behind the right hip, between the quiver mouth (x -0.18) and the stock; grey (r ~ g ~ b).
BOX_X = (-0.50, -0.12)
BOX_Y = (0.13, 0.36)
BOX_Z = (0.50, 0.88)   # down to the quiver mouth: the bolt shafts reach it
GREY_TOLERANCE = 0.05
# Crossbow selection on the source geometry (same shape as the rig's rigid rule): the ice head is wide, the stock
# narrow and outside the quiver mouth. The right forearm above the grip is left alone.
HEAD_T = 0.35
HEAD_RADIUS = 0.32
STOCK_RADIUS = 0.12
STOCK_X = -0.27
FOREARM_Z = 0.78
FOREARM_Y = 0.15


def is_crossbow(c):
    a, b = crossbow.SRC_TIP, crossbow.SRC_BUTT
    ab = b - a
    t = max(0.0, min(1.0, (c - a).dot(ab) / ab.length_squared))
    d = (a + ab * t - c).length
    if t < HEAD_T:
        return d < HEAD_RADIUS
    if c.z > FOREARM_Z and abs(c.y) < FOREARM_Y:
        return False
    return d < STOCK_RADIUS and c.x < STOCK_X


def colours(ob):
    me = ob.data
    img = next(n.image for n in ob.active_material.node_tree.nodes
               if n.type == "TEX_IMAGE" and n.image and n.image.colorspace_settings.name == "sRGB")
    w, h = img.size
    px = img.pixels[:]
    uv = me.uv_layers.active.data
    out = {}
    for loop in me.loops:
        if loop.vertex_index not in out:
            u, v = uv[loop.index].uv
            x = min(w - 1, int((u % 1.0) * w))
            y = min(h - 1, int((v % 1.0) * h))
            i = (y * w + x) * 4
            out[loop.vertex_index] = (px[i], px[i + 1], px[i + 2])
    return out


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(a.master))
    ob = bpy.data.objects[BODY]
    cols = colours(ob)
    bm = bmesh.new()
    bm.from_mesh(ob.data)
    bm.verts.ensure_lookup_table()
    doomed = []
    for v in bm.verts:
        c = ob.matrix_world @ v.co
        if not (BOX_X[0] < c.x < BOX_X[1] and BOX_Y[0] < c.y < BOX_Y[1] and BOX_Z[0] < c.z < BOX_Z[1]):
            continue
        r, g, b = cols[v.index]
        if max(r, g, b) - min(r, g, b) < GREY_TOLERANCE:
            doomed.append(v)
    bmesh.ops.delete(bm, geom=doomed, context="VERTS")
    # drop slivers left behind by the cut
    loose = [v for v in bm.verts if not v.link_faces]
    bmesh.ops.delete(bm, geom=loose, context="VERTS")
    wires = [e for e in bm.edges if not e.link_faces]
    bmesh.ops.delete(bm, geom=wires, context="EDGES")
    inv = ob.matrix_world.inverted()
    moved = 0
    for v in bm.verts:
        c = ob.matrix_world @ v.co
        if is_crossbow(c):
            v.co = inv @ crossbow.scaled(c)
            moved += 1
    bm.to_mesh(ob.data)
    bm.free()
    ob.data.calc_loop_triangles()
    print("[01b_trim] removed", len(doomed), "bolt-tip vertices; scaled", moved, "crossbow vertices to",
          crossbow.SCALE, "; tris now", len(ob.data.loop_triangles))
    bpy.context.preferences.filepaths.save_version = 0
    bpy.ops.wm.save_as_mainfile(filepath=os.path.abspath(a.master))


main()
