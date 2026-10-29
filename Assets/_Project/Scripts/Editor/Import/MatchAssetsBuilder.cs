using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FateBastion.Enemies;
using FateBastion.Game;
using FateBastion.Heroes;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// Tools > Meta > Build Match Assets (S4, S5, B-02/B-06/B-15): creates or refreshes in Data/Settings the
    /// HeroCatalog (every HeroData), the MatchCatalog (every LevelData by level number + EndlessSettings) and a
    /// MatchEconomySettings with the spec numbers. Run again after Import Balance CSV; numbers edited in the
    /// Inspector are kept.
    /// </summary>
    public static class MatchAssetsBuilder
    {
        public const string DataFolder = "Assets/_Project/Data";
        public const string HeroCatalogPath = MetaAssetsBuilder.SettingsFolder + "/HeroCatalog.asset";
        public const string MatchCatalogPath = MetaAssetsBuilder.SettingsFolder + "/MatchCatalog.asset";
        public const string EconomyPath = MetaAssetsBuilder.SettingsFolder + "/MatchEconomySettings.asset";

        [MenuItem("Tools/Meta/Build Match Assets")]
        public static void BuildFromMenu()
        {
            Build(out string report);
            Debug.Log(report);
        }

        public static bool Build(out string report)
        {
            AssetFolderUtility.EnsureFolder(MetaAssetsBuilder.SettingsFolder);

            var heroes = LoadAll<HeroData>();
            heroes.RemoveAll(h => string.IsNullOrEmpty(h.id));
            heroes.Sort((a, b) => a.rarity != b.rarity ? a.rarity.CompareTo(b.rarity) : string.CompareOrdinal(a.id, b.id));
            HeroCatalog heroCatalog = LoadOrCreate<HeroCatalog>(HeroCatalogPath);
            heroCatalog.SetHeroes(heroes);
            EditorUtility.SetDirty(heroCatalog);

            var levels = LoadAll<LevelData>();
            levels.Sort((a, b) => a.level != b.level ? a.level.CompareTo(b.level) : string.CompareOrdinal(a.name, b.name));
            List<EndlessSettings> endless = LoadAll<EndlessSettings>();
            MatchCatalog matchCatalog = LoadOrCreate<MatchCatalog>(MatchCatalogPath);
            var existingEndlessLevel = new SerializedObject(matchCatalog).FindProperty("_endlessLevel").objectReferenceValue as LevelData;
            matchCatalog.Configure(levels.ToArray(), existingEndlessLevel, endless.Count > 0 ? endless[0] : null,
                matchCatalog.LevelsToUnlockEndless);
            EditorUtility.SetDirty(matchCatalog);

            LoadOrCreate<MatchEconomySettings>(EconomyPath);
            AssetDatabase.SaveAssets();

            bool ok = heroes.Count > 0 && levels.Count > 0 && endless.Count > 0;
            report = $"[Build Match Assets] HeroCatalog: {heroes.Count} heroes · MatchCatalog: {levels.Count} levels, " +
                     $"EndlessSettings {(endless.Count > 0 ? endless[0].name : "THIẾU")} · MatchEconomySettings ok.";
            return ok;
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            var result = new List<T>();
            // Game data only: sandbox or test assets elsewhere are not part of the lobby.
            foreach (string guid in AssetDatabase.FindAssets("t:" + typeof(T).Name, new[] { DataFolder }))
            {
                var asset = AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) result.Add(asset);
            }

            return result;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }
    }
}
