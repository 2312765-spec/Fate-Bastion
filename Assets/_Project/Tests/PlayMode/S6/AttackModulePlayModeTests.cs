using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.Combat;
using FateBastion.Enemies;
using FateBastion.Heroes;
using FateBastion.Skills;
using FateBastion.Tests.PlayMode.S2;

namespace FateBastion.Tests.PlayMode.S6
{
    /// <summary>Basic attacks use the hero timer and muzzle while respecting target validity.</summary>
    public class AttackModulePlayModeTests
    {
        private EnemyTestRig _rig;
        private EnemyData _enemyData;
        private HeroData _heroData;
        private HeroStatsSettings _statsSettings;
        private SkillData _passive;
        private GameObject _heroObject;
        private HeroController _hero;
        private GameObject _projectileTemplate;

        [SetUp]
        public void SetUp()
        {
            CombatEvents.ResetAll();
            _rig = new EnemyTestRig(100f);
            _enemyData = _rig.CreateEnemy("attack_target", baseHP: 100f);

            _statsSettings = ScriptableObject.CreateInstance<HeroStatsSettings>();
            SetField(_statsSettings, "_commonDamageMultiplier", 1f);
            SetField(_statsSettings, "_rareDamageMultiplier", 1.6f);
            SetField(_statsSettings, "_epicDamageMultiplier", 2.5f);
            SetField(_statsSettings, "_legendaryDamageMultiplier", 4f);

            _heroData = ScriptableObject.CreateInstance<HeroData>();
            _heroData.id = "hero_attack_test";
            _heroData.damage = 15f;
            _heroData.attacksPerSecond = 1f;
            _heroData.range = 10f;
            _heroData.rarity = Rarity.Common;
            _heroData.attackType = AttackType.Melee;

            _passive = ScriptableObject.CreateInstance<SkillData>();
            _passive.projectileSpeed = 0.01f;
            _heroData.passiveSkill = _passive;

            _projectileTemplate = new GameObject("ProjectileTemplate");
            _projectileTemplate.SetActive(false);
            _projectileTemplate.AddComponent<Projectile>();

            _heroObject = new GameObject("AttackModuleTestHero");
            _heroObject.SetActive(false);
            _heroObject.transform.position = new Vector3(2f, 1f, 1f);
            HeroStats stats = _heroObject.AddComponent<HeroStats>();
            SetField(stats, "_settings", _statsSettings);
            _hero = _heroObject.AddComponent<HeroController>();
            AttackModule attack = _heroObject.AddComponent<AttackModule>();
            SetField(attack, "_projectilePrefab", _projectileTemplate.GetComponent<Projectile>());
            SetField(attack, "_projectilePrewarmCount", 2);

            LogAssert.Expect(LogType.Error,
                "AttackModuleTestHero: no HeroVisual found under this hero; using the hero root as the visual anchor.");
            _heroObject.SetActive(true);
            _hero.SetEnemyManager(_rig.Manager);
            _hero.Init(_heroData);
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ResetAll();
            Object.Destroy(_heroObject);
            Object.Destroy(_heroData);
            Object.Destroy(_passive);
            Object.Destroy(_statsSettings);
            if (_projectileTemplate != null) Object.Destroy(_projectileTemplate);
            _rig.Dispose();
        }

        [UnityTest]
        public IEnumerator Update_MeleeTargetInRange_AppliesDamageOnFirstAttack()
        {
            SpawnEnemy();
            yield return null;

            Assert.AreEqual(85f, _rig.Manager.ActiveEnemies[0].CurrentHP, 1e-5f);
        }

        [UnityTest]
        public IEnumerator Update_MeleeTargetFlying_DoesNotDamageEnemy()
        {
            SpawnEnemy(flying: true);

            yield return null;

            Assert.AreEqual(100f, _rig.Manager.ActiveEnemies[0].CurrentHP, 1e-5f);
        }

        [UnityTest]
        public IEnumerator Update_ProjectileTargetInRange_LaunchesFromMuzzle()
        {
            _heroData.attackType = AttackType.Projectile;
            SpawnEnemy();
            _hero.ResetState();
            _hero.Init(_heroData);

            yield return null;

            Projectile launched = null;
            Projectile[] projectiles = _heroObject.GetComponentsInChildren<Projectile>(true);
            for (int i = 0; i < projectiles.Length; i++)
                if (projectiles[i].IsFlying) launched = projectiles[i];

            Assert.IsNotNull(launched, "Expected the attack timer to launch a pooled projectile.");
            Assert.That(Vector3.Distance(_hero.Muzzle.position, launched.transform.position), Is.LessThan(0.02f));
        }

        private void SpawnEnemy(bool flying = false)
        {
            _enemyData.flying = flying;
            _rig.Manager.Prewarm(_enemyData, 1);
            _rig.Manager.RequestSpawn(_enemyData, 0, 1, 6f);
            _hero.SetEnemyManager(_rig.Manager);
        }

        private static void SetField<T>(object instance, string name, T value)
        {
            FieldInfo field = instance.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, $"Missing serialized field {name}.");
            field.SetValue(instance, value);
        }
    }
}
