using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Tools > Heroes > Setup Hero Models (C-08): for every catalog hero whose Blender export is in place, configures the
    /// FBX (Humanoid, clips named and looped), builds the URP material and the Animator Override Controller, fills the
    /// catalog, then runs Build Hero Variants. Convention per hero folder Art/Models/Heroes/&lt;Name&gt;/:
    /// CH_&lt;Name&gt;.fbx with clips &lt;Name&gt;_Idle/_Attack/_Spawn[/_Ultimate], Textures/T_&lt;Name&gt;_BaseColor.png and
    /// T_&lt;Name&gt;_Normal.png. Heroes without these files (Long Vương keeps its own palette setup) are skipped.
    /// </summary>
    public static class HeroModelSetup
    {
        public const string ModelsFolder = "Assets/_Project/Art/Models/Heroes";
        public const string AnimationsFolder = "Assets/_Project/Art/Animations/Heroes";
        public const string BaseControllerPath = AnimationsFolder + "/AC_Hero_Base.controller";

        // States of AC_Hero_Base; each hero clip is matched by the suffix after its "<Name>_" prefix.
        public static readonly string[] States = { "Idle", "Attack", "Spawn", "Ultimate" };

        private const string LoopState = "Idle";
        private const string FallbackForUltimate = "Attack";   // only Legendary heroes have an Ultimate clip
        private const int MaxTextureSize = 1024;               // matches the Blender export (04_export.py)
        // ASSUMPTION: stylized cloth, wood and straw read as matte non-metal; one value for every Hunyuan3D hero.
        private const float Smoothness = 0.2f;
        private const string LitShader = "Universal Render Pipeline/Lit";

        [MenuItem("Tools/Heroes/Setup Hero Models")]
        public static void SetupAllFromMenu()
        {
            HeroArtCatalog catalog = HeroVariantBuilder.LoadOrCreateCatalog(HeroVariantBuilder.CatalogPath);
            int count = SetupAll(catalog, ModelsFolder, AnimationsFolder);
            Debug.Log($"[Hero Models] Set up {count} hero model(s); building Variants.");
            HeroVariantBuilder.BuildAllFromMenu();
        }

        public static string GetFolder(string modelsFolder, string variantName) => modelsFolder + "/" + variantName;
        public static string GetModelPath(string modelsFolder, string variantName) =>
            GetFolder(modelsFolder, variantName) + "/CH_" + variantName + ".fbx";
        public static string GetBaseColorPath(string modelsFolder, string variantName) =>
            GetFolder(modelsFolder, variantName) + "/Textures/T_" + variantName + "_BaseColor.png";
        public static string GetNormalPath(string modelsFolder, string variantName) =>
            GetFolder(modelsFolder, variantName) + "/Textures/T_" + variantName + "_Normal.png";
        public static string GetMaterialPath(string modelsFolder, string variantName) =>
            GetFolder(modelsFolder, variantName) + "/M_" + variantName + ".mat";
        public static string GetOverridePath(string animationsFolder, string variantName) =>
            animationsFolder + "/AOC_" + variantName + ".overrideController";

        /// <summary>Clip name from an FBX take ("CH_X_Rig|X_Idle" → "X_Idle"): Blender prefixes takes with the rig.</summary>
        public static string ClipNameFromTake(string takeName)
        {
            int bar = takeName.LastIndexOf('|');
            return bar >= 0 ? takeName.Substring(bar + 1) : takeName;
        }

        /// <summary>The AC_Hero_Base state a clip name belongs to ("CungThu_Attack" → "Attack"), or null.</summary>
        public static string StateOf(string clipName)
        {
            for (int i = 0; i < States.Length; i++)
            {
                if (clipName.EndsWith("_" + States[i])) return States[i];
            }

            return null;
        }

        public static int SetupAll(HeroArtCatalog catalog, string modelsFolder, string animationsFolder)
        {
            int count = 0;
            for (int i = 0; i < catalog.heroes.Count; i++)
            {
                HeroArtEntry entry = catalog.heroes[i];
                if (entry == null || string.IsNullOrEmpty(entry.variantName)) continue;
                if (AssetDatabase.LoadAssetAtPath<Texture2D>(GetBaseColorPath(modelsFolder, entry.variantName)) == null) continue;
                if (Setup(entry, modelsFolder, animationsFolder)) count++;
            }

            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            return count;
        }

        /// <summary>Configures one hero's FBX, material and Override Controller and fills its catalog entry.</summary>
        public static bool Setup(HeroArtEntry entry, string modelsFolder, string animationsFolder)
        {
            string name = entry.variantName;
            string modelPath = GetModelPath(modelsFolder, name);
            if (!(AssetImporter.GetAtPath(modelPath) is ModelImporter))
            {
                Debug.LogError($"[Hero Models] {entry.heroId}: {modelPath} not found.");
                return false;
            }

            Material material = SetupMaterial(modelsFolder, name);
            if (material == null) return false;
            if (!SetupModel(modelPath, material)) return false;

            AnimatorOverrideController animations = SetupOverride(modelPath, animationsFolder, name);
            if (animations == null) return false;

            entry.model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);
            entry.animations = animations;
            Debug.Log($"[Hero Models] {entry.heroId}: {modelPath} ready.");
            return true;
        }

        private static Material SetupMaterial(string modelsFolder, string name)
        {
            string basePath = GetBaseColorPath(modelsFolder, name);
            string normalPath = GetNormalPath(modelsFolder, name);
            ConfigureTexture(basePath, false);
            bool hasNormal = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath) != null;
            if (hasNormal) ConfigureTexture(normalPath, true);

            var shader = Shader.Find(LitShader);
            if (shader == null)
            {
                Debug.LogError($"[Hero Models] Shader '{LitShader}' not found (URP missing?).");
                return null;
            }

            string path = GetMaterialPath(modelsFolder, name);
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader) { name = "M_" + name };
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(basePath));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Metallic", 0f);
            material.SetFloat("_Smoothness", Smoothness);
            if (hasNormal)
            {
                material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
                material.EnableKeyword("_NORMALMAP");
            }

            // Many heroes on screen at once (GDD 13): let URP instance them when SRP Batcher cannot.
            material.enableInstancing = true;
            EditorUtility.SetDirty(material);
            return material;
        }

        private static void ConfigureTexture(string path, bool normalMap)
        {
            if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;
            importer.textureType = normalMap ? TextureImporterType.NormalMap : TextureImporterType.Default;
            importer.sRGBTexture = !normalMap;
            importer.maxTextureSize = MaxTextureSize;
            importer.mipmapEnabled = true;
            importer.SaveAndReimport();
        }

        private static bool SetupModel(string modelPath, Material material)
        {
            var importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.importAnimation = true;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), material.name), material);
            importer.SaveAndReimport();

            // Clip list exists only after the first import with animation on.
            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations;
            if (clips.Length == 0)
            {
                Debug.LogError($"[Hero Models] {modelPath} has no animation takes.");
                return false;
            }

            for (int i = 0; i < clips.Length; i++)
            {
                clips[i].name = ClipNameFromTake(clips[i].takeName);
                clips[i].loopTime = StateOf(clips[i].name) == LoopState;
                // Heroes stand still (HeroController owns the transform): bake the Hips motion into the pose.
                clips[i].lockRootHeightY = true;
                clips[i].lockRootPositionXZ = true;
                clips[i].lockRootRotation = true;
                clips[i].keepOriginalPositionY = true;
                clips[i].keepOriginalPositionXZ = true;
                clips[i].keepOriginalOrientation = true;
            }

            importer.clipAnimations = clips;
            importer.SaveAndReimport();

            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(modelPath);
            if (avatar == null || !avatar.isHuman || !avatar.isValid)
            {
                Debug.LogError($"[Hero Models] {modelPath}: Humanoid avatar is not valid; check bone names in Blender.");
                return false;
            }

            return true;
        }

        private static AnimatorOverrideController SetupOverride(string modelPath, string animationsFolder, string name)
        {
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(BaseControllerPath);
            if (controller == null)
            {
                Debug.LogError($"[Hero Models] {BaseControllerPath} not found.");
                return null;
            }

            var clipsByState = new Dictionary<string, AnimationClip>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(modelPath))
            {
                if (asset is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                {
                    string state = StateOf(clip.name);
                    if (state != null) clipsByState[state] = clip;
                }
            }

            if (!clipsByState.ContainsKey(FallbackForUltimate))
            {
                Debug.LogError($"[Hero Models] {modelPath} has no '{name}_{FallbackForUltimate}' clip.");
                return null;
            }

            string path = GetOverridePath(animationsFolder, name);
            var aoc = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(path);
            if (aoc == null)
            {
                aoc = new AnimatorOverrideController(controller) { name = "AOC_" + name };
                AssetDatabase.CreateAsset(aoc, path);
            }

            aoc.runtimeAnimatorController = controller;
            var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            aoc.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; i++)
            {
                AnimationClip original = overrides[i].Key;
                string state = StateOf(original.name);
                if (state == null)
                {
                    Debug.LogWarning($"[Hero Models] AC_Hero_Base clip '{original.name}' has no state suffix; not overridden.");
                    continue;
                }

                if (!clipsByState.TryGetValue(state, out AnimationClip clip)) clip = clipsByState[FallbackForUltimate];
                overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(original, clip);
            }

            aoc.ApplyOverrides(overrides);
            EditorUtility.SetDirty(aoc);
            return aoc;
        }
    }
}
