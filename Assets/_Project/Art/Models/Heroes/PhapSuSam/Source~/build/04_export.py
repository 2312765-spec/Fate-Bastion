"""Step 4 (C-08): export CH_PhapSuSam for Unity.

Run: blender --background --python build/04_export.py -- --master PhapSuSam_master.blend --out-dir <PhapSuSam folder>
Writes <out-dir>/CH_PhapSuSam.fbx (mesh + Humanoid rig + 3 clips, textures NOT embedded) and
<out-dir>/Textures/T_PhapSuSam_BaseColor.png, T_PhapSuSam_Normal.png. Unity builds the URP material from these
(Tools > Heroes > Setup Hero Models). The metallic-roughness map is dropped: the stylized cloth/wood/straw reads
as non-metal with a fixed smoothness, and URP packs those channels differently from glTF anyway.
Same FBX settings as the skill's export_delivery.py (Unity: -Z forward, Y up, no leaf bones, all actions).
"""
import sys, os, argparse
import bpy

NAME = "CH_PhapSuSam"
PREFIX = "PhapSuSam"


def log(*a):
    print("[04_export]", *a)


def save_texture(img, path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    copy = img.copy()
    copy.filepath_raw = path
    copy.file_format = "PNG"
    copy.save()
    bpy.data.images.remove(copy)
    log("texture", path, tuple(img.size))


def main():
    p = argparse.ArgumentParser()
    p.add_argument("--master", required=True)
    p.add_argument("--out-dir", required=True)
    a = p.parse_args(sys.argv[sys.argv.index("--") + 1:])
    bpy.ops.wm.open_mainfile(filepath=os.path.abspath(a.master))

    body = bpy.data.objects[NAME + "_Body_LOD0"]
    rig = bpy.data.objects[NAME + "_Rig"]
    mat = body.active_material
    mat.name = "M_" + PREFIX   # Unity remaps this material name to the URP material

    base = normal = None
    for n in mat.node_tree.nodes:
        if n.type != "TEX_IMAGE" or not n.image:
            continue
        if "normal" in n.image.name.lower():
            normal = n.image
        elif n.image.colorspace_settings.name == "sRGB":
            base = n.image
    tex_dir = os.path.join(a.out_dir, "Textures")
    save_texture(base, os.path.join(tex_dir, "T_%s_BaseColor.png" % PREFIX))
    save_texture(normal, os.path.join(tex_dir, "T_%s_Normal.png" % PREFIX))

    for o in bpy.context.view_layer.objects:
        o.select_set(o in (body, rig))
    bpy.context.view_layer.objects.active = rig
    rig.animation_data.action = None
    for pb in rig.pose.bones:
        pb.rotation_quaternion = (1, 0, 0, 0)
        pb.location = (0, 0, 0)

    path = os.path.join(a.out_dir, NAME + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, bake_anim_simplify_factor=0.0,
        use_armature_deform_only=True, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False,
        axis_forward="-Z", axis_up="Y", object_types={"ARMATURE", "MESH"})
    log("fbx", path, "%.1f MB" % (os.path.getsize(path) / 1e6))


main()
