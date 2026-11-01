"""Step 1 (C-08): scale the Hunyuan3D mesh to 1.6 m incl. hat, put the feet on the origin, decimate to the
tri budget keeping UVs, shrink the textures to 1024. Opens ChienBinhSam_inspect.blend, writes ChienBinhSam_master.blend:
HIGH = scaled source (kept for baking), LOW = delivery mesh."""
import sys, json, bpy, bmesh
from mathutils import Vector, Matrix

argv = sys.argv[sys.argv.index("--") + 1:]
src_blend, out_blend = argv[0], argv[1]

TARGET_HEIGHT = 1.9      # armoured knight incl. the helmet horns (Epic): a head taller than the Common heroes
TARGET_TRIS = 9000       # hero budget 10k (HeroPrefabAssetTests.MaxTriangles), margin for cleanup
TEXTURE_SIZE = 1024      # enough at the game camera; 4096 from Hunyuan is 16x the memory
FEET_HALF_SPAN = 0.15   # source units (~0.35 m final): both boots, not the sword tip
FEET_BAND = 0.05         # metres above the lowest vertex counted as "feet" when centring

bpy.ops.wm.open_mainfile(filepath=src_blend)
high = bpy.data.collections["HIGH"]
src = next(o for o in high.objects if o.type == 'MESH')
src.name = "CH_ChienBinhSam_HIGH"

# The glTF importer may hang the mesh under a Y-up parent: bake the world transform into the mesh first.
me = src.data
me.transform(src.matrix_world)
src.parent = None
src.matrix_world = Matrix.Identity(4)

# Scale so the full height (hat tip to sole) is TARGET_HEIGHT, then centre the feet on the origin.
zs = [v.co.z for v in me.vertices]
zmin, zmax = min(zs), max(zs)
k = TARGET_HEIGHT / (zmax - zmin)
# the sword tip also rests on the ground far to the side, so the feet band is unreliable here: centre on the
# boots found under the neck (the sword never reaches the neck's column), taken from the helmet/neck band x
neck = [v.co for v in me.vertices if zmin + 0.70 * (zmax - zmin) < v.co.z < zmin + 0.78 * (zmax - zmin)]
ncx = (min(c.x for c in neck) + max(c.x for c in neck)) / 2
feet = [v.co for v in me.vertices if v.co.z < zmin + FEET_BAND and abs(v.co.x - ncx) < FEET_HALF_SPAN]
cx = sum(c.x for c in feet) / len(feet)
cy = sum(c.y for c in feet) / len(feet)
me.transform(Matrix.Translation((-cx, -cy, -zmin)))
me.transform(Matrix.Scale(k, 4))
src.matrix_world = Matrix.Identity(4)

# LOW: a copy, cleaned and decimated (collapse keeps UVs and seams).
low_col = bpy.data.collections.get("LOW") or bpy.data.collections.new("LOW")
if low_col.name not in bpy.context.scene.collection.children:
    bpy.context.scene.collection.children.link(low_col)
for o in list(low_col.objects):
    bpy.data.objects.remove(o, do_unlink=True)
low = src.copy(); low.data = src.data.copy(); low.name = "CH_ChienBinhSam_Body_LOD0"; low.data.name = low.name
low_col.objects.link(low)

bm = bmesh.new(); bm.from_mesh(low.data)
bmesh.ops.remove_doubles(bm, verts=bm.verts, dist=0.0005)
bmesh.ops.dissolve_degenerate(bm, edges=bm.edges, dist=0.0001)
# the source has inward-facing patches (62 faces after decimation); Unity culls back faces, so make them consistent
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
bm.to_mesh(low.data); bm.free()

low.data.calc_loop_triangles()
before = len(low.data.loop_triangles)
mod = low.modifiers.new("Decimate", 'DECIMATE')
mod.decimate_type = 'COLLAPSE'
mod.ratio = TARGET_TRIS / before
mod.use_collapse_triangulate = True
bpy.context.view_layer.objects.active = low
for o in bpy.context.view_layer.objects: o.select_set(o == low)
bpy.ops.object.modifier_apply(modifier=mod.name)
low.data.calc_loop_triangles()
after = len(low.data.loop_triangles)

# Smooth shading with sharp creases kept, so the decimated faces do not look faceted.
for p in low.data.polygons: p.use_smooth = True
try:
    bpy.ops.object.shade_auto_smooth(angle=0.7)
except Exception:
    pass

for im in bpy.data.images:
    if im.size[0] > TEXTURE_SIZE:
        im.scale(TEXTURE_SIZE, TEXTURE_SIZE)
        im.pack()

src.hide_render = True
print("LOWPOLY_JSON " + json.dumps({"scale": k, "feet_offset": [cx, cy, zmin], "tris_before": before, "tris_after": after,
                                    "height": TARGET_HEIGHT}))
bpy.ops.wm.save_as_mainfile(filepath=out_blend)
