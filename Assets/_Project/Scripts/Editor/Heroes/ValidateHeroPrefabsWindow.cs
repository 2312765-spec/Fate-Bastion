using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using FateBastion.Heroes;

namespace FateBastion.Editor.Heroes
{
    /// <summary>Tools > Validate Hero Prefabs: lists missing HeroVisual references per hero; click a row to select the asset (S0b).</summary>
    public class ValidateHeroPrefabsWindow : EditorWindow
    {
        public const string HeroDataFolder = "Assets/_Project/Data/Heroes";

        private readonly List<HeroValidationIssue> _issues = new List<HeroValidationIssue>();
        private HeroValidationSummary _summary;
        private bool _hasRun;
        private Vector2 _scroll;

        [MenuItem("Tools/Validate Hero Prefabs")]
        public static void Open()
        {
            var window = GetWindow<ValidateHeroPrefabsWindow>("Validate Hero Prefabs");
            window.Run();
        }

        /// <summary>Loads every HeroData under Data/Heroes, validates it and logs each finding to the Console.</summary>
        public void Run()
        {
            var heroes = new List<HeroData>();
            string[] guids = AssetDatabase.FindAssets("t:" + nameof(HeroData), new[] { HeroDataFolder });
            for (int i = 0; i < guids.Length; i++)
            {
                var hero = AssetDatabase.LoadAssetAtPath<HeroData>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (hero != null)
                {
                    heroes.Add(hero);
                }
            }

            heroes.Sort((a, b) => string.CompareOrdinal(a.id, b.id));

            _issues.Clear();
            _summary = HeroPrefabValidator.Validate(heroes, AssetDatabase.GetAssetPath, _issues);
            _hasRun = true;

            for (int i = 0; i < _issues.Count; i++)
            {
                HeroValidationIssue issue = _issues[i];
                string line = $"[Validate Hero Prefabs] {issue.HeroId}: {issue.Message}";
                if (issue.Severity == HeroValidationSeverity.Error) Debug.LogError(line, issue.Context);
                else Debug.LogWarning(line, issue.Context);
            }

            Debug.Log($"[Validate Hero Prefabs] {_summary.HeroCount} heroes, {_summary.ErrorCount} errors, {_summary.WarningCount} warnings.");
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Validate again", GUILayout.Width(120)))
            {
                Run();
            }

            if (_hasRun)
            {
                GUILayout.Label($"{_summary.HeroCount} heroes · {_summary.ErrorCount} errors · {_summary.WarningCount} warnings",
                    EditorStyles.boldLabel);
            }

            EditorGUILayout.EndHorizontal();

            if (!_hasRun)
            {
                return;
            }

            if (_issues.Count == 0)
            {
                EditorGUILayout.HelpBox("All hero prefabs are valid.", MessageType.Info);
                return;
            }

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            for (int i = 0; i < _issues.Count; i++)
            {
                HeroValidationIssue issue = _issues[i];
                MessageType type = issue.Severity == HeroValidationSeverity.Error ? MessageType.Error : MessageType.Warning;
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.HelpBox($"{issue.HeroId}: {issue.Message}", type);
                if (issue.Context != null && GUILayout.Button("Select", GUILayout.Width(60), GUILayout.Height(38)))
                {
                    Selection.activeObject = issue.Context;
                    EditorGUIUtility.PingObject(issue.Context);
                }

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
        }
    }
}
