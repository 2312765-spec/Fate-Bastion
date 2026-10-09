using System.Collections.Generic;
using UnityEditor;
using FateBastion.Enemies;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// waves.csv to WaveData "L&lt;level&gt;_W&lt;wave&gt;" and LevelData "level_&lt;level&gt;" (S0 mapping table).
    /// Every enemy column with a count above zero becomes one SpawnGroup, in the order
    /// normal, fast, armored, flying, boss.
    /// </summary>
    public static class WaveCsvImporter
    {
        /// <summary>Seconds added per group so the groups of a wave do not all start at once (S0).</summary>
        private const float StartDelayStep = 2f;

        private const int GroundPathIndex = 0;
        private const int FlyingPathIndex = 1;

        /// <summary>
        /// ASSUMPTION (Q2): waves.csv holds counts per column, not enemy ids, so the column name maps to the
        /// EnemyData id from enemies.csv. The order of this table is also the SpawnGroup order.
        /// </summary>
        private static readonly (string Column, string EnemyId, bool Flying)[] ColumnToEnemy =
        {
            ("normal", "enemy_thuong", false),
            ("fast", "enemy_nhanh", false),
            ("armored", "enemy_giap", false),
            ("flying", "enemy_bay", true),
            ("boss", "enemy_boss", false)
        };

        public static void Import(CsvTable table, BalanceImportRequest request, BalanceImportReport report)
        {
            const string file = "waves.csv";

            Dictionary<string, EnemyData> enemiesById = LoadEnemies(request);
            var wavesByLevel = new Dictionary<int, SortedDictionary<int, WaveData>>();
            var seenWaveAssets = new List<string>();

            foreach (CsvRow row in table.Rows)
            {
                if (!row.TryGetInt("level", out int level) || !row.TryGetInt("wave", out int wave))
                {
                    report.AddError(file, row.LineNumber, "column 'level' or 'wave' is missing or not a number; row skipped.");
                    continue;
                }

                if (!row.TryGetFloat("spawnInterval", out float spawnInterval))
                {
                    report.AddError(file, row.LineNumber,
                        $"L{level}_W{wave}: 'spawnInterval' cannot be parsed; row skipped.");
                    continue;
                }

                var groups = new List<SpawnGroup>(ColumnToEnemy.Length);
                bool rowFailed = false;
                bool isBossWave = false;

                foreach ((string column, string enemyId, bool flying) in ColumnToEnemy)
                {
                    if (!row.TryGetInt(column, out int count))
                    {
                        // An empty cell means zero; only a non-numeric cell is an error.
                        if (row.TryGetString(column, out _))
                        {
                            report.AddError(file, row.LineNumber,
                                $"L{level}_W{wave}: column '{column}' is not a number; row skipped.");
                            rowFailed = true;
                            break;
                        }

                        continue;
                    }

                    if (count <= 0)
                    {
                        continue;
                    }

                    if (!enemiesById.TryGetValue(enemyId, out EnemyData enemy))
                    {
                        report.AddError(file, row.LineNumber,
                            $"L{level}_W{wave}: EnemyData '{enemyId}' for column '{column}' not found in {request.EnemiesFolder}; row skipped. Import enemies.csv first.");
                        rowFailed = true;
                        break;
                    }

                    groups.Add(new SpawnGroup
                    {
                        enemy = enemy,
                        count = count,
                        interval = spawnInterval,

                        // ASSUMPTION (Q1): the group index counts only the groups actually created, so a wave
                        // with normal/fast/boss gets 0 s, 2 s and 4 s rather than 0 s, 2 s and 8 s.
                        startDelay = groups.Count * StartDelayStep,
                        pathIndex = flying ? FlyingPathIndex : GroundPathIndex
                    });

                    if (enemy.boss)
                    {
                        isBossWave = true;
                    }
                }

                if (rowFailed)
                {
                    continue;
                }

                if (groups.Count == 0)
                {
                    report.AddWarning($"{file} line {row.LineNumber}: L{level}_W{wave} has no enemies; an empty wave asset was written.");
                }

                string assetName = $"L{level}_W{wave}";
                var waveData = AssetFolderUtility.LoadOrCreate<WaveData>(request.WavesFolder, assetName, out bool created);
                waveData.level = level;
                waveData.wave = wave;
                waveData.groups = groups.ToArray();
                waveData.isBossWave = isBossWave;

                EditorUtility.SetDirty(waveData);
                if (created)
                {
                    report.WavesCreated++;
                }
                else
                {
                    report.WavesUpdated++;
                }

                seenWaveAssets.Add(assetName);

                if (!wavesByLevel.TryGetValue(level, out SortedDictionary<int, WaveData> levelWaves))
                {
                    levelWaves = new SortedDictionary<int, WaveData>();
                    wavesByLevel.Add(level, levelWaves);
                }

                levelWaves[wave] = waveData;
            }

            ImportLevels(request, report, wavesByLevel);
            WarnAboutOrphans(request, report, seenWaveAssets, wavesByLevel);
        }

        private static void ImportLevels(BalanceImportRequest request, BalanceImportReport report,
            Dictionary<int, SortedDictionary<int, WaveData>> wavesByLevel)
        {
            foreach (KeyValuePair<int, SortedDictionary<int, WaveData>> pair in wavesByLevel)
            {
                int level = pair.Key;
                var ordered = new List<WaveData>(pair.Value.Count);
                foreach (KeyValuePair<int, WaveData> wave in pair.Value)
                {
                    ordered.Add(wave.Value);
                }

                string assetName = "level_" + level;
                var levelData = AssetFolderUtility.LoadOrCreate<LevelData>(request.LevelsFolder, assetName, out bool created);
                levelData.level = level;
                levelData.waves = ordered.ToArray();

                EditorUtility.SetDirty(levelData);
                if (created)
                {
                    report.LevelsCreated++;
                }
                else
                {
                    report.LevelsUpdated++;
                }
            }
        }

        private static Dictionary<string, EnemyData> LoadEnemies(BalanceImportRequest request)
        {
            var map = new Dictionary<string, EnemyData>();
            if (!AssetDatabase.IsValidFolder(request.EnemiesFolder))
            {
                return map;
            }

            string[] guids = AssetDatabase.FindAssets("t:EnemyData", new[] { request.EnemiesFolder });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var enemy = AssetDatabase.LoadAssetAtPath<EnemyData>(path);
                if (enemy == null)
                {
                    continue;
                }

                // Match on the asset name: the importer writes '<id>.asset', and the name survives even if
                // the id field was cleared by hand.
                string name = System.IO.Path.GetFileNameWithoutExtension(path);
                map[name] = enemy;
            }

            return map;
        }

        private static void WarnAboutOrphans(BalanceImportRequest request, BalanceImportReport report,
            List<string> seenWaveAssets, Dictionary<int, SortedDictionary<int, WaveData>> wavesByLevel)
        {
            foreach (string name in AssetFolderUtility.FindAssetNames<WaveData>(request.WavesFolder))
            {
                if (!seenWaveAssets.Contains(name))
                {
                    report.AddWarning($"WaveData '{name}' exists in the project but not in waves.csv; kept untouched.");
                }
            }

            foreach (string name in AssetFolderUtility.FindAssetNames<LevelData>(request.LevelsFolder))
            {
                bool found = false;
                foreach (int level in wavesByLevel.Keys)
                {
                    if (name == "level_" + level)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    report.AddWarning($"LevelData '{name}' exists in the project but not in waves.csv; kept untouched.");
                }
            }
        }
    }
}
