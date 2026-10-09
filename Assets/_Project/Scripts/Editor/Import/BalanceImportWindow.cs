using System.IO;
using UnityEditor;
using UnityEngine;

namespace FateBastion.Editor.Import
{
    /// <summary>Tools &gt; Import Balance CSV window: pick the three CSV files and run the import (S0).</summary>
    public class BalanceImportWindow : EditorWindow
    {
        private const string PrefKeyPrefix = "FateBastion.BalanceImport.";

        private string _heroesPath;
        private string _enemiesPath;
        private string _wavesPath;
        private string _outputRoot;
        private string _log = string.Empty;
        private Vector2 _logScroll;

        [MenuItem("Tools/Import Balance CSV")]
        public static void Open()
        {
            var window = GetWindow<BalanceImportWindow>(true, "Import Balance CSV", true);
            window.minSize = new Vector2(560f, 360f);
            window.Show();
        }

        private void OnEnable()
        {
            BalanceImportRequest defaults = BalanceImportRequest.CreateDefault();
            _heroesPath = EditorPrefs.GetString(PrefKeyPrefix + "heroes", defaults.HeroesCsvPath);
            _enemiesPath = EditorPrefs.GetString(PrefKeyPrefix + "enemies", defaults.EnemiesCsvPath);
            _wavesPath = EditorPrefs.GetString(PrefKeyPrefix + "waves", defaults.WavesCsvPath);
            _outputRoot = EditorPrefs.GetString(PrefKeyPrefix + "outputRoot", defaults.OutputRoot);
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Edit the numbers in Docs/Can bang - Fate Bastion.xlsx, export to CSV UTF-8 into Data/Balance, " +
                "then import here. Never edit the generated assets by hand.",
                MessageType.Info);

            _heroesPath = PathField("heroes.csv", _heroesPath);
            _enemiesPath = PathField("enemies.csv", _enemiesPath);
            _wavesPath = PathField("waves.csv", _wavesPath);

            EditorGUILayout.Space();
            _outputRoot = EditorGUILayout.TextField("Output root", _outputRoot);

            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Import All", GUILayout.Height(28f)))
                {
                    Run(heroes: true, enemies: true, waves: true);
                }

                if (GUILayout.Button("Enemies only", GUILayout.Height(28f)))
                {
                    Run(heroes: false, enemies: true, waves: false);
                }

                if (GUILayout.Button("Heroes only", GUILayout.Height(28f)))
                {
                    Run(heroes: true, enemies: false, waves: false);
                }

                if (GUILayout.Button("Waves only", GUILayout.Height(28f)))
                {
                    Run(heroes: false, enemies: false, waves: true);
                }
            }

            if (GUILayout.Button("Reset paths to default"))
            {
                BalanceImportRequest defaults = BalanceImportRequest.CreateDefault();
                _heroesPath = defaults.HeroesCsvPath;
                _enemiesPath = defaults.EnemiesCsvPath;
                _wavesPath = defaults.WavesCsvPath;
                _outputRoot = defaults.OutputRoot;
                SavePrefs();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Result", EditorStyles.boldLabel);
            using (var scroll = new EditorGUILayout.ScrollViewScope(_logScroll))
            {
                _logScroll = scroll.scrollPosition;
                EditorGUILayout.TextArea(_log, GUILayout.ExpandHeight(true));
            }
        }

        private string PathField(string label, string value)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string result = EditorGUILayout.TextField(label, value);
                if (GUILayout.Button("...", GUILayout.Width(30f)))
                {
                    string start = string.IsNullOrEmpty(result) ? BalanceImportRequest.DefaultCsvFolder : Path.GetDirectoryName(result);
                    string picked = EditorUtility.OpenFilePanel("Select " + label, start, "csv");
                    if (!string.IsNullOrEmpty(picked))
                    {
                        result = picked;
                    }
                }

                return result;
            }
        }

        private void Run(bool heroes, bool enemies, bool waves)
        {
            SavePrefs();

            var request = new BalanceImportRequest
            {
                HeroesCsvPath = heroes ? _heroesPath : null,
                EnemiesCsvPath = enemies ? _enemiesPath : null,
                WavesCsvPath = waves ? _wavesPath : null,
                OutputRoot = string.IsNullOrEmpty(_outputRoot) ? BalanceImportRequest.DefaultOutputRoot : _outputRoot
            };

            BalanceImportReport report = BalanceImporter.Import(request);
            _log = report.ToSummary();

            if (report.HasErrors)
            {
                Debug.LogError("Import Balance CSV finished with errors:\n" + _log);
            }
            else
            {
                Debug.Log("Import Balance CSV finished:\n" + _log);
            }
        }

        private void SavePrefs()
        {
            EditorPrefs.SetString(PrefKeyPrefix + "heroes", _heroesPath);
            EditorPrefs.SetString(PrefKeyPrefix + "enemies", _enemiesPath);
            EditorPrefs.SetString(PrefKeyPrefix + "waves", _wavesPath);
            EditorPrefs.SetString(PrefKeyPrefix + "outputRoot", _outputRoot);
        }
    }
}
