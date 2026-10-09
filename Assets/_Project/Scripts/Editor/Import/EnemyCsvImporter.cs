using System.Collections.Generic;
using UnityEditor;
using FateBastion.Enemies;

namespace FateBastion.Editor.Import
{
    /// <summary>enemies.csv to EnemyData "&lt;id&gt;"; all ten columns are written straight through (S0 mapping table).</summary>
    public static class EnemyCsvImporter
    {
        public static void Import(CsvTable table, BalanceImportRequest request, BalanceImportReport report)
        {
            const string file = "enemies.csv";
            var seenIds = new List<string>(table.Rows.Count);

            foreach (CsvRow row in table.Rows)
            {
                if (!row.TryGetString("id", out string id))
                {
                    report.AddError(file, row.LineNumber, "column 'id' is missing or empty; row skipped.");
                    continue;
                }

                if (!row.TryGetString("displayName", out string displayName))
                {
                    report.AddError(file, row.LineNumber, "column 'displayName' is missing or empty; row skipped.");
                    continue;
                }

                if (!row.TryGetFloat("baseHP", out float baseHP)
                    || !row.TryGetFloat("moveSpeed", out float moveSpeed)
                    || !row.TryGetFloat("armor", out float armor)
                    || !row.TryGetFloat("magicResist", out float magicResist))
                {
                    report.AddError(file, row.LineNumber,
                        $"enemy '{id}' has a number that cannot be parsed in baseHP/moveSpeed/armor/magicResist; row skipped.");
                    continue;
                }

                if (!row.TryGetBool("flying", out bool flying) || !row.TryGetBool("boss", out bool boss))
                {
                    report.AddError(file, row.LineNumber,
                        $"enemy '{id}' has a flying/boss value that is not TRUE or FALSE; row skipped.");
                    continue;
                }

                if (!row.TryGetInt("goldReward", out int goldReward) || !row.TryGetInt("castleDamage", out int castleDamage))
                {
                    report.AddError(file, row.LineNumber,
                        $"enemy '{id}' has a number that cannot be parsed in goldReward/castleDamage; row skipped.");
                    continue;
                }

                var enemy = AssetFolderUtility.LoadOrCreate<EnemyData>(request.EnemiesFolder, id, out bool created);
                enemy.id = id;
                enemy.displayName = displayName;
                enemy.baseHP = baseHP;
                enemy.moveSpeed = moveSpeed;
                enemy.armor = armor;
                enemy.magicResist = magicResist;
                enemy.flying = flying;
                enemy.boss = boss;
                enemy.goldReward = goldReward;
                enemy.castleDamage = castleDamage;

                EditorUtility.SetDirty(enemy);
                if (created)
                {
                    report.EnemiesCreated++;
                }
                else
                {
                    report.EnemiesUpdated++;
                }

                seenIds.Add(id);
            }

            foreach (string name in AssetFolderUtility.FindAssetNames<EnemyData>(request.EnemiesFolder))
            {
                if (!seenIds.Contains(name))
                {
                    report.AddWarning($"EnemyData '{name}' exists in the project but not in enemies.csv; kept untouched.");
                }
            }
        }
    }
}
