"""C-08: prepare the five Quaternius Ultimate Monsters (CC0) used as enemies.

Run from Assets/ThirdParty/Quaternius:
  blender --background --python Source~/build/process_monsters.py
Reads the raw pack in _Download~/ (not committed, Unity ignores ~ folders), keeps the rig, the skin weights and every
clip, decimates each monster to the enemy triangle budget (100 on screen, GDD 13) and writes
Creatures/UltimateMonsters/<Name>.fbx plus the shared Atlas_Monsters.png. Scale is left to Unity (the enemy Variant
tool sizes the model by type); scaling here would not scale the animation's root translation.
"""
import os, json, shutil
import bpy

HERE = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))   # Assets/ThirdParty/Quaternius
SRC = os.path.join(HERE, "_Download~", "Ultimate Monsters")
DST = os.path.join(HERE, "Creatures", "UltimateMonsters")
ATLAS = "Atlas_Monsters.png"

# ASSUMPTION: per-enemy budget so 100 enemies stay within 30 FPS on Intel UHD (EnemyPrefabAssetTests.MaxTriangles).
TRI_BUDGET = 3000

MONSTERS = [  # (pack group, file name, enemy type) – picked from the contact sheet with the user
    ("Big", "Orc", "Normal"),
    ("Big", "Ninja", "Fast"),
    ("Big", "Orc_Skull", "Armor"),
    ("Flying", "Dragon", "Fly"),
    ("Big", "Demon", "Boss"),
]


def log(*a):
    print("[process_monsters]", *a)


def tris(objs):
    n = 0
    for o in objs:
        o.data.calc_loop_triangles()
        n += len(o.data.loop_triangles)
    return n


def process(group, name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=os.path.join(SRC, group, "FBX", name + ".fbx"))
    for o in [o for o in bpy.context.scene.objects if o.type not in ("ARMATURE", "MESH") or o.parent is None and o.type == "MESH"]:
        bpy.data.objects.remove(o, do_unlink=True)   # default cube/light/camera that ship in the FBX
    arm = next(o for o in bpy.context.scene.objects if o.type == "ARMATURE")
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    before = tris(meshes)
    ratio = min(1.0, TRI_BUDGET / max(1, before))
    for o in meshes:
        if ratio < 1.0:
            bpy.context.view_layer.objects.active = o
            for x in bpy.context.view_layer.objects:
                x.select_set(x == o)
            mod = o.modifiers.new("Decimate", "DECIMATE")
            mod.ratio = ratio
            mod.use_collapse_triangulate = True
            # decimate before the armature deforms; collapse keeps vertex groups and UVs
            while o.modifiers.find(mod.name) > 0:
                bpy.ops.object.modifier_move_up(modifier=mod.name)
            bpy.ops.object.modifier_apply(modifier=mod.name)
        for p in o.data.polygons:
            p.use_smooth = True
    after = tris(meshes)

    for x in bpy.context.view_layer.objects:
        x.select_set(x is arm or x in meshes)
    bpy.context.view_layer.objects.active = arm
    arm.animation_data_create()
    arm.animation_data.action = None
    # The pack names actions "CharacterArmature|CharacterArmature|Walk"; the exporter prefixes the rig again and hashes
    # names past 63 characters (Fast_Flying became F_5c614f9). Keep only the clip name.
    for a in bpy.data.actions:
        a.name = a.name.split("|")[-1]
    os.makedirs(DST, exist_ok=True)
    out = os.path.join(DST, name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=out, use_selection=True, apply_unit_scale=True, apply_scale_options="FBX_SCALE_ALL",
        use_mesh_modifiers=True, add_leaf_bones=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_force_startend_keying=True, use_armature_deform_only=False,
        mesh_smooth_type="FACE", path_mode="STRIP", embed_textures=False, axis_forward="-Z", axis_up="Y",
        object_types={"ARMATURE", "MESH"})
    acts = sorted({a.name.split("|")[-1] for a in bpy.data.actions})
    return {"name": name, "tris_before": before, "tris_after": after, "clips": acts}


def main():
    report = [dict(process(g, n), type=t) for g, n, t in MONSTERS]
    shutil.copyfile(os.path.join(SRC, ATLAS), os.path.join(DST, ATLAS))
    for r in report:
        log(r["type"], r["name"], r["tris_before"], "->", r["tris_after"], "clips", ",".join(r["clips"]))
    with open(os.path.join(HERE, "Source~", "monsters_report.json"), "w") as f:
        json.dump(report, f, indent=1)


main()
