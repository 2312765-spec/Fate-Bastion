"""C-08: prepare the Quaternius RPG Wizard (CC0) used as the Triệu Hồi Sư (Summoner).

Run from Assets/ThirdParty/Quaternius:
  blender --background --python Source~/build/process_summoner.py
Reads _Download~/RPG Characters - Nov 2020/FBX/Wizard.fbx, drops the stray default cube the pack ships inside the FBX,
shortens the clip names (the exporter hashes names past 63 characters) and writes Characters/RPGCharacters/Wizard.fbx
with Wizard_Texture.png and Wizard_Staff_Texture.png. One character on screen, so the mesh is not decimated.
The normal rig is used (the pack's "Humanoid Rig Versions" have no clips); Unity imports it as Generic.
"""
import os, shutil
import bpy

HERE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))   # Assets/ThirdParty/Quaternius
SRC = os.path.join(HERE, "_Download~", "RPG Characters - Nov 2020")
DST = os.path.join(HERE, "Characters", "RPGCharacters")
TEXTURES = ("Wizard_Texture.png", "Wizard_Staff_Texture.png")


def main():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, "FBX", "Wizard.fbx"))
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    for o in [o for o in bpy.context.scene.objects if o.type == "MESH" and o.name.startswith("Cube")]:
        bpy.data.objects.remove(o, do_unlink=True)
    for o in [o for o in bpy.context.scene.objects if o.type not in ("ARMATURE", "MESH")]:
        bpy.data.objects.remove(o, do_unlink=True)
    for a in bpy.data.actions:
        a.name = a.name.split("|")[-1]
    arm.animation_data_create()
    arm.animation_data.action = None
    for o in bpy.context.view_layer.objects:
        o.select_set(o.type in ("ARMATURE", "MESH"))
    bpy.context.view_layer.objects.active = arm
    os.makedirs(DST, exist_ok=True)
    bpy.ops.export_scene.fbx(
        filepath=os.path.join(DST, "Wizard.fbx"), use_selection=True, apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL", use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=True,
        bake_anim_use_all_actions=True, bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True,
        use_armature_deform_only=False, mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False,
        axis_forward="-Z", axis_up="Y", object_types={"ARMATURE", "MESH"})
    for t in TEXTURES:
        shutil.copyfile(os.path.join(SRC, "Textures", t), os.path.join(DST, t))
    print("[process_summoner] clips", ",".join(sorted(a.name for a in bpy.data.actions)))


main()
