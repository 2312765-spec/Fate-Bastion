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
