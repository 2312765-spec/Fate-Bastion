"""Imports the Hunyuan3D GLB into a HIGH collection, prints mesh/material/texture stats, saves an inspect .blend."""
import sys, os, json, bpy
from mathutils import Vector

argv = sys.argv[sys.argv.index("--") + 1:]
src, out_blend = argv[0], argv[1]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.context.scene.unit_settings.system = 'METRIC'
high = bpy.data.collections.new("HIGH")
bpy.context.scene.collection.children.link(high)
bpy.ops.import_scene.gltf(filepath=src)
for ob in list(bpy.context.selected_objects):
    for c in ob.users_collection:
        c.objects.unlink(ob)
    high.objects.link(ob)

meshes = [o for o in high.objects if o.type == 'MESH']
mn = Vector((1e9,) * 3); mx = Vector((-1e9,) * 3)
info = {"objects": [], "materials": [], "images": []}
for o in high.objects:
    entry = {"name": o.name, "type": o.type, "loc": list(o.location), "rot": list(o.rotation_euler), "scale": list(o.scale)}
    if o.type == 'MESH':
        me = o.data
        me.calc_loop_triangles()
        entry.update(verts=len(me.vertices), tris=len(me.loop_triangles), uv_layers=[u.name for u in me.uv_layers],
                     color_attrs=[a.name for a in me.color_attributes], mats=[m.name for m in me.materials if m])
        for v in o.bound_box:
            w = o.matrix_world @ Vector(v)
            mn = Vector(map(min, mn, w)); mx = Vector(map(max, mx, w))
    info["objects"].append(entry)
for m in bpy.data.materials:
    tex = [n.image.name for n in m.node_tree.nodes if n.type == 'TEX_IMAGE' and n.image] if m.use_nodes else []
    info["materials"].append({"name": m.name, "textures": tex})
for im in bpy.data.images:
    info["images"].append({"name": im.name, "size": list(im.size), "colorspace": im.colorspace_settings.name})
info["bounds_min"] = list(mn); info["bounds_max"] = list(mx); info["size"] = list(mx - mn)
print("INSPECT_JSON " + json.dumps(info))
os.makedirs(os.path.dirname(out_blend), exist_ok=True)
bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=out_blend)
