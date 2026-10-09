using UnityEditor;
using UnityEngine;

namespace FateBastion.Editor.Import
{
    /// <summary>Small AssetDatabase helpers shared by the balance importers (S0).</summary>
    public static class AssetFolderUtility
    {
        /// <summary>Creates every missing segment of a project-relative folder path such as "Assets/A/B/C".</summary>
        public static void EnsureFolder(string projectRelativeFolder)
        {
            if (string.IsNullOrEmpty(projectRelativeFolder) || AssetDatabase.IsValidFolder(projectRelativeFolder))
            {
                return;
            }

            string[] parts = projectRelativeFolder.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        /// <summary>
        /// Loads the asset at "folder/assetName.asset" or creates it. Updating in place is what keeps the
        /// references assigned by hand (prefab, icon, ultimate) alive across imports (S0).
        /// </summary>
        public static T LoadOrCreate<T>(string folder, string assetName, out bool created) where T : ScriptableObject
        {
            EnsureFolder(folder);

            string path = folder + "/" + assetName + ".asset";
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            created = false;
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            asset.name = assetName;
            AssetDatabase.CreateAsset(asset, path);
            created = true;
            return asset;
        }

        /// <summary>Asset names of every asset of type T directly inside the folder.</summary>
        public static string[] FindAssetNames<T>(string folder) where T : ScriptableObject
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return new string[0];
            }

            string[] guids = AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { folder });
            var names = new string[guids.Length];
            for (int i = 0; i < guids.Length; i++)
            {
                names[i] = System.IO.Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guids[i]));
            }

            return names;
        }
    }
}
