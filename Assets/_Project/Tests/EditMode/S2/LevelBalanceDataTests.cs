using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>
    /// Verifies the imported balance assets the HP formula reads (S2/A-06): three levels, 15 waves each and the
    /// hpMultiplier 1.0 / 1.15 / 1.3. Lives in EditMode because it reads the project through AssetDatabase.
    /// </summary>
    public class LevelBalanceDataTests
    {
        private const int WaveCount = 15;

        private const string LevelsPath = "Assets/_Project/Data/Levels/";
        private const string EnemiesPath = "Assets/_Project/Data/Enemies/";

        private static LevelData LoadLevel(int level)
        {
            string path = $"{LevelsPath}level_{level}.asset";
            var data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
            Assert.IsNotNull(data, $"thiếu {path} (chạy Tools > Import Balance CSV)");
            return data;
        }

        [TestCase(1, 1.0f)]
        [TestCase(2, 1.15f)]
        [TestCase(3, 1.3f)]
        public void Level_HasFifteenWavesAndExpectedHpMultiplier(int level, float expectedMultiplier)
        {
            LevelData data = LoadLevel(level);

            Assert.AreEqual(1.12f, data.hpGrowth, 1e-4f, "hpGrowth phải là 1.12");
            Assert.AreEqual(expectedMultiplier, data.hpMultiplier, 1e-4f);
            Assert.AreEqual(WaveCount, data.waves.Length, "mỗi màn 15 wave");

            foreach (WaveData wave in data.waves)
            {
                Assert.IsNotNull(wave, $"level_{level} thiếu WaveData trong danh sách");
                Assert.IsNotNull(wave.groups, $"{wave.name} thiếu groups");
            }
        }

        [Test]
        public void Level3_Wave15_NormalEnemyHp_MatchesSheetGrowthAndMultiplier()
        {
            LevelData data = LoadLevel(3);
            var normal = AssetDatabase.LoadAssetAtPath<EnemyData>($"{EnemiesPath}enemy_normal.asset");
            Assert.IsNotNull(normal, "thiếu enemy_normal.asset");

            Assert.AreEqual(40f, normal.baseHP, 1e-4f, "enemy_normal.baseHP theo enemies.csv");
            float expected = 40f * Mathf.Pow(1.12f, WaveCount - 1) * 1.3f;
            Assert.AreEqual(expected, EnemyHealthFormula.Calculate(normal.baseHP, data.hpGrowth, WaveCount, data.hpMultiplier), 1e-1f);
        }

        [TestCase(1, 1, 280f)]
        [TestCase(1, 5, 1353.2266f)]
        [TestCase(1, 15, 5669.0503f)]
        [TestCase(2, 1, 322f)]
        [TestCase(2, 5, 1664.7835f)]
        [TestCase(2, 15, 7531.0400f)]
        [TestCase(3, 1, 312f)]
        [TestCase(3, 5, 1865.5646f)]
        [TestCase(3, 15, 8869.1314f)]
        public void ImportedWave_TotalEnemyHp_MatchesBalanceSheet(int level, int waveNumber, float expectedTotalHp)
        {
            LevelData data = LoadLevel(level);
            WaveData wave = data.waves[waveNumber - 1];

            Assert.IsNotNull(wave, $"level_{level} thiếu WaveData wave {waveNumber}");
            Assert.AreEqual(level, wave.level);
            Assert.AreEqual(waveNumber, wave.wave);

            float totalHp = 0f;
            foreach (SpawnGroup group in wave.groups)
            {
                Assert.IsNotNull(group, $"L{level}_W{waveNumber} có SpawnGroup null");
                Assert.IsNotNull(group.enemy, $"L{level}_W{waveNumber} có EnemyData null");

                float enemyHp = EnemyHealthFormula.Calculate(
                    group.enemy.baseHP,
                    data.hpGrowth,
                    waveNumber,
                    data.hpMultiplier);
                totalHp += enemyHp * group.count;
            }

            Assert.AreEqual(expectedTotalHp, totalHp, 1e-1f,
                $"L{level}_W{waveNumber} tổng HP phải khớp calc_totalHP trong workbook cân bằng");
        }
    }
}
