using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Map > Add Post Processing to Map_Level1: a global URP Volume (Art/Rendering/PP_Match, Bloom) under the
    /// map's Visuals/, so every scene that has the map – Game and the sandboxes – gets the same glow on fire, meteor and
    /// rarity auras. Before this only Sandbox/C_Main had a Volume, so effects looked flat in Game.unity.
    /// </summary>
    public static class PostProcessingInstaller
    {
        public const string MapPath = "Assets/_Project/Prefabs/Map/Map_Level1.prefab";
        public const string ProfilePath = "Assets/_Project/Art/Rendering/PP_Match.asset";
        public const string VolumeName = "PostProcessVolume";
        private const string VisualsName = "Visuals";

        // The Editor asmdef does not reference the render pipeline assemblies; the Volume type is found by name instead.
        public const string VolumeTypeName = "UnityEngine.Rendering.Volume, Unity.RenderPipelines.Core.Runtime";

        public const string ProfileTypeName = "UnityEngine.Rendering.VolumeProfile, Unity.RenderPipelines.Core.Runtime";
        public const string TonemappingTypeName = "UnityEngine.Rendering.Universal.Tonemapping, Unity.RenderPipelines.Universal.Runtime";
        public const string VignetteTypeName = "UnityEngine.Rendering.Universal.Vignette, Unity.RenderPipelines.Universal.Runtime";

        // C-17 light grading on top of Bloom. Tonemapping mode 1 = Neutral (keeps Kenney's flat colours, unlike ACES).
        private const int NeutralTonemapping = 1;
        private const float VignetteIntensity = 0.2f;
        private const float VignetteSmoothness = 0.4f;

        /// <summary>
        /// Adds Neutral Tonemapping and a light Vignette to the profile if missing (C-17). Existing overrides are left
        /// as tuned in the Inspector. False when URP types or the profile cannot be found.
        /// </summary>
        public static bool EnsureGrading(string profilePath)
        {
            Type profileType = Type.GetType(ProfileTypeName);
            Type tonemapping = Type.GetType(TonemappingTypeName);
            Type vignette = Type.GetType(VignetteTypeName);
            Object profile = profileType != null ? AssetDatabase.LoadAssetAtPath(profilePath, profileType) : null;
            if (profile == null || tonemapping == null || vignette == null)
            {
                Debug.LogError($"[Map] {profilePath} or the URP Tonemapping/Vignette types not found.");
                return false;
            }

            AddIfMissing(profile, profileType, tonemapping, data =>
            {
                SetParameter(data, "mode", p => p.intValue = NeutralTonemapping);
            });
            AddIfMissing(profile, profileType, vignette, data =>
            {
                SetParameter(data, "intensity", p => p.floatValue = VignetteIntensity);
                SetParameter(data, "smoothness", p => p.floatValue = VignetteSmoothness);
            });
            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return true;
        }

        private static void AddIfMissing(Object profile, Type profileType, Type componentType, Action<SerializedObject> setup)
        {
            bool has = (bool)profileType.GetMethod("Has", new[] { typeof(Type) }).Invoke(profile, new object[] { componentType });
            if (has) return;

            var component = (ScriptableObject)profileType.GetMethod("Add", new[] { typeof(Type), typeof(bool) })
                .Invoke(profile, new object[] { componentType, false });
            component.name = componentType.Name;
            component.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(component, profile);
            var data = new SerializedObject(component);
            setup(data);
            data.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetParameter(SerializedObject data, string parameter, Action<SerializedProperty> setValue)
        {
            data.FindProperty(parameter + ".m_OverrideState").boolValue = true;
            setValue(data.FindProperty(parameter + ".m_Value"));
        }

        [MenuItem("Tools/Map/Add Post Processing to Map_Level1")]
        public static void InstallFromMenu() => Install(MapPath, ProfilePath);

        /// <summary>Adds or refreshes the Volume; false when the map, profile or Volume type is missing.</summary>
        public static bool Install(string mapPath, string profilePath)
        {
            Type volumeType = Type.GetType(VolumeTypeName);
            Object profile = AssetDatabase.LoadMainAssetAtPath(profilePath);
            if (volumeType == null || profile == null || AssetDatabase.LoadAssetAtPath<GameObject>(mapPath) == null)
            {
                Debug.LogError($"[Map] Volume type, {profilePath} or {mapPath} not found.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(mapPath);
            try
            {
                Transform visuals = root.transform.Find(VisualsName);
                if (visuals == null)
                {
                    Debug.LogError($"[Map] {mapPath} has no '{VisualsName}' (C-06 hierarchy contract).");
                    return false;
                }

                Transform holder = visuals.Find(VolumeName);
                if (holder == null)
                {
                    holder = new GameObject(VolumeName).transform;
                    holder.SetParent(visuals, false);
                }

                Component volume = holder.GetComponent(volumeType);
                if (volume == null) volume = holder.gameObject.AddComponent(volumeType);
                var data = new SerializedObject(volume);
                data.FindProperty("m_IsGlobal").boolValue = true;
                data.FindProperty("sharedProfile").objectReferenceValue = profile;
                data.FindProperty("weight").floatValue = 1f;
                data.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, mapPath);
                Debug.Log($"[Map] Global Volume ({profile.name}) set under {VisualsName}/{VolumeName} in {mapPath}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
