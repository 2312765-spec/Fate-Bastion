using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Tools > Meta > Build Gacha Table (S8, B-08/B-11): creates GachaTable and StarterSettings in Data/Settings if
    /// missing, fills the gacha pool with the id and rarity of every HeroData, and checks the starter heroes exist.
    /// Run again after Import Balance CSV adds or changes heroes; existing numbers set in the Inspector are kept.
    /// </summary>
    public static class MetaAssetsBuilder
    {
        public const string SettingsFolder = "Assets/_Project/Data/Settings";
        public const string GachaTablePath = SettingsFolder + "/GachaTable.asset";
        public const string StarterSettingsPath = SettingsFolder + "/StarterSettings.asset";

        [MenuItem("Tools/Meta/Build Gacha Table")]
        public static void BuildFromMenu()
        {
            Build(out string report);
            Debug.Log(report);
        }

        /// <summary>Returns false when something needs attention (missing rarity, unknown starter hero).</summary>
        public static bool Build(out string report)
        {
            AssetFolderUtility.EnsureFolder(SettingsFolder);
            GachaTable table = LoadOrCreate<GachaTable>(GachaTablePath);
            StarterSettings starter = LoadOrCreate<StarterSettings>(StarterSettingsPath);

            var entries = new List<GachaTable.HeroEntry>();
            foreach (string guid in AssetDatabase.FindAssets("t:" + nameof(HeroData)))
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guid));
                if (hero != null && !string.IsNullOrEmpty(hero.id))
                {
                    entries.Add(new GachaTable.HeroEntry(hero.id, hero.rarity));
                }
            }

            // Stable order (rarity, then id) keeps the asset diff small between runs.
            entries.Sort((a, b) => a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : string.CompareOrdinal(a.heroId, b.heroId));
            table.SetHeroes(entries);
            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            bool ok = true;
            var text = new StringBuilder("[Build Gacha Table] ");
            text.Append(entries.Count).Append(" heroes:");
            for (int i = 0; i < GachaTable.RarityCount; i++)
            {
                int count = table.HeroesOf((Rarity)i).Count;
                text.Append(' ').Append((Rarity)i).Append('=').Append(count);
                if (count == 0)
                {
                    ok = false;
                    text.Append(" (THIẾU: gacha sẽ báo lỗi)");
                }
            }

            foreach (string heroId in starter.StarterHeroes)
            {
                if (!table.TryGetRarity(heroId, out _))
                {
                    ok = false;
                    text.Append("\nStarterSettings: không có HeroData '").Append(heroId).Append("'.");
                }
            }

            report = text.ToString();
            return ok;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
