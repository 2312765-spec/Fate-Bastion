using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Editor.Import;
using FateBastion.Enemies;
using FateBastion.Heroes;
using FateBastion.Skills;

namespace FateBastion.Tests.EditMode.S0
{
    /// <summary>
    /// Import tests for Tools &gt; Import Balance CSV (S0 completion criteria). They read the real CSVs in
    /// Data/Balance but write into a throwaway folder so the project assets are never touched.
    /// </summary>
    public class BalanceImporterTests
    {
        private const string TempRootPrefix = "Assets/_TestTemp_Balance_";

        /// <summary>
        /// Every test writes into its own folder. Reusing one path across tests inside a single Editor session
        /// makes AssetDatabase hand back the instance of an already deleted asset, which showed up as assets
        /// that looked like they still held their default values.
        /// </summary>
        private string _tempRoot;

        private string _tempCsvFolder;

        [SetUp]
        public void SetUp()
        {
            _tempRoot = TempRootPrefix + System.Guid.NewGuid().ToString("N").Substring(0, 8);
            _tempCsvFolder = Path.Combine(Path.GetTempPath(), "FateBastionCsvTests_" + Path.GetRandomFileName());
            Directory.CreateDirectory(_tempCsvFolder);
        }

        [TearDown]
        public void TearDown()
        {
            if (AssetDatabase.IsValidFolder(_tempRoot))
            {
                AssetDatabase.DeleteAsset(_tempRoot);
                AssetDatabase.Refresh();
            }

            if (Directory.Exists(_tempCsvFolder))
            {
                Directory.Delete(_tempCsvFolder, true);
            }
        }

        private BalanceImportRequest RealCsvRequest()
        {
            BalanceImportRequest request = BalanceImportRequest.CreateDefault();
            request.OutputRoot = _tempRoot;
            return request;
        }

        private static void AssertCsvFilesExist(BalanceImportRequest request)
        {
            Assert.IsTrue(File.Exists(request.HeroesCsvPath), "heroes.csv not found at " + request.HeroesCsvPath);
            Assert.IsTrue(File.Exists(request.EnemiesCsvPath), "enemies.csv not found at " + request.EnemiesCsvPath);
            Assert.IsTrue(File.Exists(request.WavesCsvPath), "waves.csv not found at " + request.WavesCsvPath);
        }

        private static T Load<T>(string folder, string assetName) where T : ScriptableObject
        {
            return AssetDatabase.LoadAssetAtPath<T>(folder + "/" + assetName + ".asset");
        }

        [Test]
        public void Import_SampleCsv_Creates9Heroes9PassiveSkills5Enemies45Waves3Levels()
        {
            BalanceImportRequest request = RealCsvRequest();
            AssertCsvFilesExist(request);

            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.IsFalse(report.HasErrors, "Import reported errors:\n" + report.ToSummary());
            Assert.AreEqual(9, report.HeroTotal, "HeroData count");
            Assert.AreEqual(9, report.SkillTotal, "passive SkillData count");
            Assert.AreEqual(5, report.EnemyTotal, "EnemyData count");
            Assert.AreEqual(45, report.WaveTotal, "WaveData count");
            Assert.AreEqual(3, report.LevelTotal, "LevelData count");

            Assert.AreEqual(9, AssetFolderUtility.FindAssetNames<HeroData>(request.HeroesFolder).Length);
            Assert.AreEqual(9, AssetFolderUtility.FindAssetNames<SkillData>(request.SkillsFolder).Length);
            Assert.AreEqual(5, AssetFolderUtility.FindAssetNames<EnemyData>(request.EnemiesFolder).Length);
            Assert.AreEqual(45, AssetFolderUtility.FindAssetNames<WaveData>(request.WavesFolder).Length);
            Assert.AreEqual(3, AssetFolderUtility.FindAssetNames<LevelData>(request.LevelsFolder).Length);
        }

        [Test]
        public void Import_RunTwice_DoesNotCreateAdditionalAssets()
        {
            BalanceImportRequest request = RealCsvRequest();
            AssertCsvFilesExist(request);

            BalanceImporter.Import(request);
            BalanceImportReport second = BalanceImporter.Import(request);

            Assert.IsFalse(second.HasErrors, "Second import reported errors:\n" + second.ToSummary());
            Assert.AreEqual(0, second.HeroesCreated, "no new HeroData on the second run");
            Assert.AreEqual(0, second.SkillsCreated, "no new SkillData on the second run");
            Assert.AreEqual(0, second.EnemiesCreated, "no new EnemyData on the second run");
            Assert.AreEqual(0, second.WavesCreated, "no new WaveData on the second run");
            Assert.AreEqual(0, second.LevelsCreated, "no new LevelData on the second run");
            Assert.AreEqual(0, second.Warnings.Count, "no orphan warnings:\n" + second.ToSummary());

            Assert.AreEqual(9, AssetFolderUtility.FindAssetNames<HeroData>(request.HeroesFolder).Length);
            Assert.AreEqual(9, AssetFolderUtility.FindAssetNames<SkillData>(request.SkillsFolder).Length);
            Assert.AreEqual(5, AssetFolderUtility.FindAssetNames<EnemyData>(request.EnemiesFolder).Length);
            Assert.AreEqual(45, AssetFolderUtility.FindAssetNames<WaveData>(request.WavesFolder).Length);
            Assert.AreEqual(3, AssetFolderUtility.FindAssetNames<LevelData>(request.LevelsFolder).Length);
        }

        [Test]
        public void Import_RunTwice_KeepsManuallyAssignedUltimateSkill()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var dragon = Load<HeroData>(request.HeroesFolder, "hero_long_vuong");
            Assert.IsNotNull(dragon);

            // Stands in for the prefab, icon and ultimate that are wired up by hand in the Inspector.
            var ultimate = ScriptableObject.CreateInstance<SkillData>();
            ultimate.id = "skill_rong_thieng";
            AssetDatabase.CreateAsset(ultimate, request.SkillsFolder + "/skill_rong_thieng.asset");
            dragon.ultimateSkill = ultimate;
            EditorUtility.SetDirty(dragon);
            AssetDatabase.SaveAssets();

            BalanceImporter.Import(request);

            dragon = Load<HeroData>(request.HeroesFolder, "hero_long_vuong");
            Assert.IsNotNull(dragon.ultimateSkill, "the importer must not clear ultimateSkill");
            Assert.AreEqual("skill_rong_thieng", dragon.ultimateSkill.id);
        }

        [Test]
        public void ImportHeroes_LinksPassiveSkillAndCopiesStats()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var archer = Load<HeroData>(request.HeroesFolder, "hero_cung_thu");
            Assert.IsNotNull(archer);
            Assert.AreEqual("hero_cung_thu", archer.id);
            Assert.AreEqual(Rarity.Common, archer.rarity);
            Assert.AreEqual(Element.Lightning, archer.element);
            Assert.AreEqual(Role.SingleDps, archer.role);
            Assert.AreEqual(DamageType.Physical, archer.damageType);
            Assert.AreEqual(AttackType.Projectile, archer.attackType);
            Assert.AreEqual(12f, archer.damage, 0.001f);
            Assert.AreEqual(1.25f, archer.attacksPerSecond, 0.001f);
            Assert.AreEqual(9f, archer.range, 0.001f);

            Assert.IsNotNull(archer.passiveSkill, "the importer links <id>_passive");
            Assert.AreEqual("hero_cung_thu_passive", archer.passiveSkill.id);
            Assert.AreEqual(SkillKind.Projectile, archer.passiveSkill.kind);
            Assert.AreEqual(PassiveTrigger.OnEveryHit, archer.passiveSkill.trigger);
        }

        [Test]
        public void ImportHeroes_ThanhNu_PassiveIsBuffWithValue030AndRadius5()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var passive = Load<SkillData>(request.SkillsFolder, "hero_thanh_nu_passive");
            Assert.IsNotNull(passive);
            Assert.AreEqual(SkillKind.Buff, passive.kind);
            Assert.AreEqual(PassiveTrigger.Aura, passive.trigger);
            Assert.AreEqual(BuffType.AttackSpeed, passive.buffType);
            Assert.AreEqual(0.3f, passive.buffValue, 0.001f);
            Assert.AreEqual(5f, passive.buffRadius, 0.001f);
        }

        [Test]
        public void ImportHeroes_PhuThuyLua_PassiveRadiusEqualsAreaRadius()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var passive = Load<SkillData>(request.SkillsFolder, "hero_phu_thuy_lua_passive");
            Assert.IsNotNull(passive);
            Assert.AreEqual(2f, passive.radius, 0.001f, "areaRadius maps to passive.radius");
            Assert.AreEqual(SkillKind.AreaAtPoint, passive.kind, "attackType Area maps to AreaAtPoint");
            Assert.AreEqual(BuffType.None, passive.buffType);
        }

        [Test]
        public void ImportHeroes_MeleeHero_PassiveKindIsMeleeHit()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var passive = Load<SkillData>(request.SkillsFolder, "hero_chien_binh_sam_passive");
            Assert.IsNotNull(passive);
            Assert.AreEqual(SkillKind.MeleeHit, passive.kind);
        }

        [Test]
        public void ImportEnemies_Boss_ReadsAllTenColumns()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var boss = Load<EnemyData>(request.EnemiesFolder, "enemy_boss");
            Assert.IsNotNull(boss);
            Assert.AreEqual("Boss", boss.displayName);
            Assert.AreEqual(600f, boss.baseHP, 0.001f);
            Assert.AreEqual(1.5f, boss.moveSpeed, 0.001f);
            Assert.AreEqual(0.2f, boss.armor, 0.001f);
            Assert.AreEqual(0.2f, boss.magicResist, 0.001f);
            Assert.IsFalse(boss.flying);
            Assert.IsTrue(boss.boss);
            Assert.AreEqual(150, boss.goldReward);
            Assert.AreEqual(10, boss.castleDamage);
        }

        [Test]
        public void ImportEnemies_Flying_ReadsTrueIgnoringCase()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var flyer = Load<EnemyData>(request.EnemiesFolder, "enemy_bay");
            Assert.IsNotNull(flyer);
            Assert.IsTrue(flyer.flying);
            Assert.IsFalse(flyer.boss);
        }

        [Test]
        public void ImportWaves_L1W5_IsBossWaveWithGroupsInOrderAndStartDelays()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            // waves.csv row "1,5,6,1,0,0,1,1.2": normal 6, fast 1, boss 1.
            var wave = Load<WaveData>(request.WavesFolder, "L1_W5");
            Assert.IsNotNull(wave);
            Assert.IsTrue(wave.isBossWave);
            Assert.AreEqual(1, wave.level);
            Assert.AreEqual(5, wave.wave);
            Assert.AreEqual(3, wave.groups.Length, "only columns above zero become groups");

            Assert.AreEqual("enemy_thuong", wave.groups[0].enemy.id);
            Assert.AreEqual(6, wave.groups[0].count);
            Assert.AreEqual(0f, wave.groups[0].startDelay, 0.001f);
            Assert.AreEqual(1.2f, wave.groups[0].interval, 0.001f);
            Assert.AreEqual(0, wave.groups[0].pathIndex);

            Assert.AreEqual("enemy_nhanh", wave.groups[1].enemy.id);
            Assert.AreEqual(1, wave.groups[1].count);
            Assert.AreEqual(2f, wave.groups[1].startDelay, 0.001f);

            Assert.AreEqual("enemy_boss", wave.groups[2].enemy.id);
            Assert.AreEqual(1, wave.groups[2].count);
            Assert.AreEqual(4f, wave.groups[2].startDelay, 0.001f);
        }

        [Test]
        public void ImportWaves_NonBossWave_IsBossWaveIsFalse()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var wave = Load<WaveData>(request.WavesFolder, "L1_W1");
            Assert.IsNotNull(wave);
            Assert.IsFalse(wave.isBossWave);
            Assert.AreEqual(1, wave.groups.Length);
            Assert.AreEqual(7, wave.groups[0].count);
        }

        [Test]
        public void ImportWaves_FlyingGroup_UsesPathIndex1()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var wave = Load<WaveData>(request.WavesFolder, "L3_W13");
            Assert.IsNotNull(wave);

            bool foundFlying = false;
            foreach (SpawnGroup group in wave.groups)
            {
                if (group.enemy.flying)
                {
                    foundFlying = true;
                    Assert.AreEqual(1, group.pathIndex, "flying groups use the flying path");
                }
                else
                {
                    Assert.AreEqual(0, group.pathIndex);
                }
            }

            Assert.IsTrue(foundFlying, "L3_W13 should contain a flying group");
        }

        [Test]
        public void ImportWaves_Level1_Has15WavesInWaveOrder()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            var level = Load<LevelData>(request.LevelsFolder, "level_1");
            Assert.IsNotNull(level);
            Assert.AreEqual(1, level.level);
            Assert.AreEqual(15, level.waves.Length);

            for (int i = 0; i < level.waves.Length; i++)
            {
                Assert.IsNotNull(level.waves[i], "wave slot " + i);
                Assert.AreEqual(i + 1, level.waves[i].wave, "waves are stored in wave order");
                Assert.AreEqual(1, level.waves[i].level);
            }
        }

        [Test]
        public void ImportWaves_BossWaves_AreWave5And10And15()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            for (int level = 1; level <= 3; level++)
            {
                for (int wave = 1; wave <= 15; wave++)
                {
                    var data = Load<WaveData>(request.WavesFolder, $"L{level}_W{wave}");
                    Assert.IsNotNull(data, $"L{level}_W{wave}");
                    bool expectBoss = wave == 5 || wave == 10 || wave == 15;
                    Assert.AreEqual(expectBoss, data.isBossWave, $"L{level}_W{wave} isBossWave");
                }
            }
        }

        [Test]
        public void Import_RowWithInvalidEnum_SkipsRowAndReportsErrorWithLineNumber()
        {
            string path = Path.Combine(_tempCsvFolder, "heroes.csv");
            File.WriteAllText(path,
                "id,displayName,rarity,element,role,damageType,attackType,damage,attacksPerSecond,range,areaRadius,buffType,buffValue,buffRadius,notes\n" +
                "# description row\n" +
                "hero_ok,Hero OK,Common,Fire,Mage,Magic,Area,10,1,7,2,None,0,0,ghi chu\n" +
                "hero_bad,Hero Bad,Mythic,Fire,Mage,Magic,Area,10,1,7,2,None,0,0,ghi chu\n");

            var request = new BalanceImportRequest { HeroesCsvPath = path, OutputRoot = _tempRoot };
            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.AreEqual(1, report.HeroTotal, "only the valid row is imported");
            Assert.AreEqual(1, report.Errors.Count, report.ToSummary());
            StringAssert.Contains("heroes.csv", report.Errors[0]);
            StringAssert.Contains("line 4", report.Errors[0]);
            StringAssert.Contains("hero_bad", report.Errors[0]);
            Assert.IsNotNull(Load<HeroData>(request.HeroesFolder, "hero_ok"));
            Assert.IsNull(Load<HeroData>(request.HeroesFolder, "hero_bad"));
        }

        [Test]
        public void Import_RowWithInvalidNumber_SkipsRowAndKeepsImporting()
        {
            string path = Path.Combine(_tempCsvFolder, "enemies.csv");
            File.WriteAllText(path,
                "id,displayName,baseHP,moveSpeed,armor,magicResist,flying,boss,goldReward,castleDamage\n" +
                "enemy_bad,Bad,bon muoi,2.5,0,0,FALSE,FALSE,5,1\n" +
                "enemy_ok,OK,40,2.5,0,0,FALSE,FALSE,5,1\n");

            var request = new BalanceImportRequest { EnemiesCsvPath = path, OutputRoot = _tempRoot };
            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.AreEqual(1, report.EnemyTotal);
            Assert.AreEqual(1, report.Errors.Count, report.ToSummary());
            StringAssert.Contains("line 2", report.Errors[0]);
            Assert.IsNotNull(Load<EnemyData>(request.EnemiesFolder, "enemy_ok"));
            Assert.IsNull(Load<EnemyData>(request.EnemiesFolder, "enemy_bad"));
        }

        [Test]
        public void Import_UnknownColumn_IsIgnoredWithoutError()
        {
            string path = Path.Combine(_tempCsvFolder, "enemies.csv");
            File.WriteAllText(path,
                "id,displayName,baseHP,moveSpeed,armor,magicResist,flying,boss,goldReward,castleDamage,calc_totalHP,notes\n" +
                "enemy_ok,OK,40,2.5,0,0,FALSE,FALSE,5,1,1234,\"ghi chu, co dau phay\"\n");

            var request = new BalanceImportRequest { EnemiesCsvPath = path, OutputRoot = _tempRoot };
            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.IsFalse(report.HasErrors, report.ToSummary());
            Assert.AreEqual(1, report.EnemyTotal);
            Assert.AreEqual(40f, Load<EnemyData>(request.EnemiesFolder, "enemy_ok").baseHP, 0.001f);
        }

        [Test]
        public void ImportWaves_WithoutEnemyAssets_ReportsErrorPerRow()
        {
            string path = Path.Combine(_tempCsvFolder, "waves.csv");
            File.WriteAllText(path,
                "level,wave,normal,fast,armored,flying,boss,spawnInterval\n" +
                "1,1,7,0,0,0,0,1.2\n");

            var request = new BalanceImportRequest { WavesCsvPath = path, OutputRoot = _tempRoot };
            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.AreEqual(0, report.WaveTotal);
            Assert.AreEqual(1, report.Errors.Count, report.ToSummary());
            StringAssert.Contains("enemy_thuong", report.Errors[0]);
        }

        [Test]
        public void Import_MissingCsvFile_ReportsErrorAndDoesNotThrow()
        {
            var request = new BalanceImportRequest
            {
                HeroesCsvPath = Path.Combine(_tempCsvFolder, "does_not_exist.csv"),
                OutputRoot = _tempRoot
            };

            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.AreEqual(1, report.Errors.Count);
            StringAssert.Contains("not found", report.Errors[0]);
        }

        [Test]
        public void Import_AssetPresentInProjectButNotInCsv_IsKeptAndWarned()
        {
            BalanceImportRequest request = RealCsvRequest();
            BalanceImporter.Import(request);

            // A hero somebody added by hand, or left over from an older CSV.
            var orphan = ScriptableObject.CreateInstance<HeroData>();
            orphan.id = "hero_cu";
            AssetDatabase.CreateAsset(orphan, request.HeroesFolder + "/hero_cu.asset");
            AssetDatabase.SaveAssets();

            BalanceImportReport report = BalanceImporter.Import(request);

            Assert.IsNotNull(Load<HeroData>(request.HeroesFolder, "hero_cu"), "orphan assets are never deleted");
            bool warned = false;
            foreach (string warning in report.Warnings)
            {
                if (warning.Contains("hero_cu"))
                {
                    warned = true;
                }
            }

            Assert.IsTrue(warned, "expected a warning about hero_cu:\n" + report.ToSummary());
        }
    }
}
