using UnityEditor;
using UnityEngine;
using FateBastion.UI.World;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Audio > Add Settings Bridge to _Audio: puts SettingsAudioBridge on the _Audio prefab, so the Settings
    /// sliders and the camera shake toggle reach AudioManager and FeedbackService in every scene (C-13, C-18).
    /// </summary>
    public static class SettingsAudioBridgeInstaller
    {
        public const string AudioPrefabPath = "Assets/_Project/Prefabs/Systems/_Audio.prefab";

        [MenuItem("Tools/Audio/Add Settings Bridge to _Audio")]
        public static void InstallFromMenu() => Install(AudioPrefabPath);

        /// <summary>Adds the bridge once; false when the prefab is missing or already has it.</summary>
        public static bool Install(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                Debug.LogError($"[Audio] {prefabPath} not found.");
                return false;
            }

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (root.GetComponent<SettingsAudioBridge>() != null)
                {
                    Debug.Log($"[Audio] {prefabPath} already has {nameof(SettingsAudioBridge)}.");
                    return false;
                }

                root.AddComponent<SettingsAudioBridge>();
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Debug.Log($"[Audio] {nameof(SettingsAudioBridge)} added to {prefabPath}.");
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
