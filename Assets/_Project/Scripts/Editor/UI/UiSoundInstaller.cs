using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Audio;

namespace FateBastion.Editor.UI
{
    /// <summary>
    /// Tools > Audio > Add UI Sounds (C-13): gives every Button in the UI prefabs a click sound and every screen a
    /// open/close sound. Safe to run again after the layout builders or B add buttons; nothing is added twice.
    /// </summary>
    public static class UiSoundInstaller
    {
        // Screen roots that get UIPanelSound; the HUD and small widgets stay quiet.
        private const string ScreenPrefix = "Screen_";

        [MenuItem("Tools/Audio/Add UI Sounds")]
        private static void AddUiSoundsMenu()
        {
            int buttons = 0;
            int panels = 0;
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { HudLayoutBuilder.PrefabFolder });
            // Nested prefabs (screens, cards) first, so their parents only see buttons that already have the sound.
            var paths = new List<string>(guids.Length);
            foreach (string guid in guids) paths.Add(AssetDatabase.GUIDToAssetPath(guid));
            paths.Sort((a, b) => NestingDepth(a).CompareTo(NestingDepth(b)));

            foreach (string path in paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int addedButtons = AddButtonSounds(root);
                    bool isScreen = root.name.StartsWith(ScreenPrefix) || root.name == LobbyLayoutBuilder.ConfirmDialogName;
                    bool addedPanel = isScreen && AddPanelSound(root);
                    if (addedButtons > 0 || addedPanel) PrefabUtility.SaveAsPrefabAsset(root, path);
                    buttons += addedButtons;
                    panels += addedPanel ? 1 : 0;
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            Debug.Log($"[Add UI Sounds] Added {buttons} button sound(s) and {panels} panel sound(s) under {HudLayoutBuilder.PrefabFolder}.");
        }

        /// <summary>Adds <see cref="UIButtonSound"/> to each Button owned by this prefab; returns how many were added.</summary>
        public static int AddButtonSounds(GameObject root)
        {
            int added = 0;
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                // Buttons inside a nested prefab instance belong to that prefab; changing them here would be an override.
                if (PrefabUtility.IsPartOfPrefabInstance(button)) continue;
                if (button.GetComponent<UIButtonSound>() != null) continue;

                var sound = button.gameObject.AddComponent<UIButtonSound>();
                UnityEventTools.AddVoidPersistentListener(button.onClick, sound.Play);
                added++;
            }

            return added;
        }

        /// <summary>Adds <see cref="UIPanelSound"/> to a screen root; false when it already has one.</summary>
        public static bool AddPanelSound(GameObject root)
        {
            if (root.GetComponent<UIPanelSound>() != null) return false;
            root.AddComponent<UIPanelSound>();
            return true;
        }

        private static int NestingDepth(string path)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) return 0;
            int depth = 0;
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
            {
                if (PrefabUtility.IsAnyPrefabInstanceRoot(child.gameObject)) depth++;
            }

            return depth;
        }
    }
}
