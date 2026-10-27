#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace FateBastion.Meta.Sandbox
{
    /// <summary>
    /// Sandbox/A_Save driver for the S9 handoff: Load/Save/Sanitize/Delete against a folder inside
    /// persistentDataPath (never the real save), and a "corrupt save.json" button to show the backup path.
    /// Uses OnGUI buttons, not keys, so it does not bypass InputRouter.
    /// </summary>
    public class SaveSandboxDemo : MonoBehaviour
    {
        private static readonly string[] StarterHeroIds =
        {
            "hero_chien_binh_sam", "hero_cung_thu", "hero_linh_bang", "hero_long_vuong", "hero_nguoi_tuyet"
        };

        [Tooltip("Folder inside Application.persistentDataPath; keep a sandbox name so the real save is untouched.")]
        [SerializeField] private string _folderName = "SandboxSave";

        private SaveService _service;
        private SaveLoadResult _lastLoad;
        private List<string> _warnings = new List<string>();
        private string _lastAction = "(chưa làm gì)";

        private string SaveDirectory => Path.Combine(Application.persistentDataPath, _folderName);

        private void Awake()
        {
            _service = new SaveService(SaveDirectory, new DemoStarterSaveFactory());
        }

        private void Start()
        {
            Load();
        }

        private void Load()
        {
            _lastLoad = _service.Load();
            _warnings.Clear();
            _lastAction = $"Load() -> {_lastLoad.Kind}";
        }

        private void Save()
        {
            if (_service.Data == null)
            {
                _lastAction = "Chưa có Data: bấm Load trước.";
                return;
            }

            _service.Save();
            _lastAction = "Save() -> save.tmp -> save.json (+ save.bak nếu đã có)";
        }

        private void GainGems()
        {
            if (_service.Data == null) return;
            _service.Data.gems += 100;
            Save();
            _lastAction = "gems +100 rồi Save()";
        }

        private void Sanitize()
        {
            if (_service.Data == null) return;
            _warnings = SaveSanitizer.Sanitize(_service.Data, StarterHeroIds, StarterHeroIds);
            _service.Save();
            _lastAction = $"Sanitize(roster 5 tướng) -> {_warnings.Count} cảnh báo, đã Save()";
        }

        private void Delete()
        {
            _service.Delete();
            _lastLoad = null;
            _warnings.Clear();
            _lastAction = "Delete() -> xoá save.json / save.bak / save.tmp";
        }

        private void CorruptSave()
        {
            Directory.CreateDirectory(SaveDirectory);
            File.WriteAllText(_service.SaveFilePath, "{ file hỏng cố ý");
            _lastAction = "Đã ghi đè save.json bằng JSON hỏng";
            Load();
        }

        private void OnGUI()
        {
            SaveData data = _service.Data;
            GUILayout.BeginArea(new Rect(10f, 10f, 500f, 460f), GUI.skin.box);
            GUILayout.Label($"SaveService (S9) - folder: {SaveDirectory}");
            GUILayout.Label($"Exists: {_service.Exists}   Last action: {_lastAction}");
            if (_lastLoad != null)
            {
                GUILayout.Label($"Load(): {_lastLoad.Kind}   Recovered: {_lastLoad.Recovered}");
                GUILayout.Label($"Error: {_lastLoad.Error ?? "(không có)"}");
            }
            else
            {
                GUILayout.Label("Load(): chưa gọi");
            }

            if (data == null)
            {
                GUILayout.Label("Data: null (bấm Load hoặc Delete xong)");
            }
            else
            {
                GUILayout.Label($"gems: {data.gems}   pity: {data.pityCounter}   version: {data.version}");
                GUILayout.Label($"collection: {data.collection.Count}   deck: {data.deck.Count}/{SaveSanitizer.DeckSize}" +
                                $"   ownedEver: {data.ownedEver.Count}");
                GUILayout.Label($"settings.master: {data.settings.master:0.00}   tutorialDone: {data.tutorialDone}");
            }

            for (int i = 0; i < _warnings.Count; i++) GUILayout.Label($"- {_warnings[i]}");

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Load")) Load();
            if (GUILayout.Button("Save")) Save();
            if (GUILayout.Button("+100 gems")) GainGems();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Sanitize")) Sanitize();
            if (GUILayout.Button("Delete")) Delete();
            if (GUILayout.Button("Corrupt save.json")) CorruptSave();
            GUILayout.EndHorizontal();

            GUILayout.Label($"save.json: {_service.SaveFilePath}");
            GUILayout.Label($"save.bak : {_service.BackupFilePath}");
            GUILayout.Label("Kỳ vọng: Corrupt -> Load = RecoveredFromBackup (hoặc CreatedNew nếu chưa có bak).");
            GUILayout.EndArea();
        }

        /// <summary>Placeholder for S8: the real starter values live in StarterSettings (B, B-11).</summary>
        private sealed class DemoStarterSaveFactory : IStarterSaveFactory
        {
            public SaveData CreateNew()
            {
                var data = new SaveData { gems = 1000 };
                for (int i = 0; i < StarterHeroIds.Length; i++)
                {
                    data.collection[StarterHeroIds[i]] = 1;
                    data.deck.Add(StarterHeroIds[i]);
                    data.ownedEver.Add(StarterHeroIds[i]);
                }
                return data;
            }
        }
    }
}
#endif
