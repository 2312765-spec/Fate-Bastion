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
        private HeroRageSettings _rageSettings;
        private SkillData _passive;
        private SkillData _ultimate;
        private StatusEffectData _status;
        private GameObject _heroObject;
        private HeroController _hero;
        private GameObject _projectileTemplate;
        private GameObject _buffRecipientObject;
        private HeroController _buffRecipient;
        private HeroData _buffRecipientData;

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
            _rageSettings = ScriptableObject.CreateInstance<HeroRageSettings>();

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

            _status = ScriptableObject.CreateInstance<StatusEffectData>();
            _status.id = "burn_test";
            _status.element = Element.Fire;
            _status.duration = 3f;
            _status.tickInterval = 1f;
            _status.value = 0.2f;

            _projectileTemplate = new GameObject("ProjectileTemplate");
            _projectileTemplate.SetActive(false);
            _projectileTemplate.AddComponent<Projectile>();

            _heroObject = new GameObject("AttackModuleTestHero");
            _heroObject.SetActive(false);
            _heroObject.transform.position = new Vector3(2f, 1f, 1f);
            HeroStats stats = _heroObject.AddComponent<HeroStats>();
            SetField(stats, "_settings", _statsSettings);
            _hero = _heroObject.AddComponent<HeroController>();
            SetField(_hero, "_rageSettings", _rageSettings);
            _heroObject.AddComponent<AttackModule>();
            SkillRunner runner = _heroObject.GetComponent<SkillRunner>();
            Assert.IsNotNull(runner, "AttackModule should require SkillRunner.");
            SetField(runner, "_projectilePrefab", _projectileTemplate.GetComponent<Projectile>());
            SetField(runner, "_projectilePrewarmCount", 2);

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
            if (_ultimate != null) Object.Destroy(_ultimate);
            Object.Destroy(_status);
            Object.Destroy(_statsSettings);
            Object.Destroy(_rageSettings);
            if (_projectileTemplate != null) Object.Destroy(_projectileTemplate);
            if (_buffRecipientObject != null) Object.Destroy(_buffRecipientObject);
            if (_buffRecipientData != null) Object.Destroy(_buffRecipientData);
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
        public IEnumerator Attack_LegendaryRageFull_UltimateRequiresTargetAndConsumesRageOnSuccess()
        {
            _heroData.rarity = Rarity.Legendary;
            _heroData.attacksPerSecond = 1000f;
            _enemyData.baseHP = 100000f;
            _ultimate = ScriptableObject.CreateInstance<SkillData>();
            _ultimate.kind = SkillKind.AreaAtPoint;
            _ultimate.radius = 2f;
            _ultimate.damageMultiplier = 1f;
            _heroData.ultimateSkill = _ultimate;
            _hero.Init(_heroData);
            SpawnEnemy();

            int rageEvents = 0;
            _hero.OnRageChanged += (current, maximum) => rageEvents++;
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            for (int i = 0; i < 24 && _hero.CurrentRage < _hero.MaximumRage; i++)
                yield return null;

            Assert.AreEqual(100f, _hero.CurrentRage, 1e-5f);
            Assert.AreEqual(20, rageEvents, "Each successful attack should add rage exactly once.");

            enemy.transform.position = _heroObject.transform.position + Vector3.right * 20f;
            Assert.IsFalse(_hero.TryActivateUltimate());
            Assert.AreEqual(100f, _hero.CurrentRage, 1e-5f);

            enemy.transform.position = _heroObject.transform.position + Vector3.right * 4f;
            Assert.IsTrue(_hero.TryActivateUltimate());
            Assert.AreEqual(0f, _hero.CurrentRage, 1e-5f);
            Assert.AreEqual(21, rageEvents, "Successful activation should publish the consumed rage value.");
        }

        [UnityTest]
        public IEnumerator Update_MeleePassiveWithModifierAndStatus_AppliesBothOnce()
        {
            _passive.kind = SkillKind.MeleeHit;
            _passive.trigger = PassiveTrigger.OnEveryHit;
            _passive.damageMultiplier = 2f;
            _passive.status = _status;
            SpawnEnemy();

            yield return null;

            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            Assert.AreEqual(70f, enemy.CurrentHP, 1e-5f);
            Assert.IsTrue(enemy.Status.IsBurning);
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

        [Test]
        public void ExecuteBasicAttack_AreaAtPoint_HitsLivingEnemiesInXZRadiusIncludingFlying()
        {
            _heroData.attackType = AttackType.Area;
            _passive.kind = SkillKind.AreaAtPoint;
            _passive.trigger = PassiveTrigger.OnEveryHit;
            _passive.damageMultiplier = 2f;
            _passive.radius = 2f;
            _hero.Init(_heroData);

            _enemyData.flying = false;
            _rig.Manager.Prewarm(_enemyData, 1);
            _rig.Manager.RequestSpawn(_enemyData, 0, 1, 6f);

            EnemyData flyingData = _rig.CreateEnemy("attack_flying_target", flying: true);
            _rig.Manager.Prewarm(flyingData, 1);
            _rig.Manager.RequestSpawn(flyingData, 0, 1, 6f);

            EnemyData outsideData = _rig.CreateEnemy("attack_outside_target");
            _rig.Manager.Prewarm(outsideData, 1);
            _rig.Manager.RequestSpawn(outsideData, 0, 1, 8.1f);

            EnemyController center = _rig.Manager.ActiveEnemies[0];
            EnemyController flying = _rig.Manager.ActiveEnemies[1];
            EnemyController outside = _rig.Manager.ActiveEnemies[2];
            center.transform.position = new Vector3(6f, 0f, 0f);
            flying.transform.position = new Vector3(8f, 2f, 0f);
            outside.transform.position = new Vector3(8.1f, 0f, 0f);

            Assert.IsTrue(_heroObject.GetComponent<SkillRunner>().ExecuteBasicAttack(center));

            Assert.AreEqual(70f, center.CurrentHP, 1e-5f);
            Assert.AreEqual(70f, flying.CurrentHP, 1e-5f);
            Assert.AreEqual(100f, outside.CurrentHP, 1e-5f);
        }

        [Test]
        public void ExecuteBasicAttack_Cone_UsesConfiguredReachAndFarEdgeWidth()
        {
            _heroData.attackType = AttackType.Cone;
            _passive.kind = SkillKind.Cone;
            _passive.trigger = PassiveTrigger.OnEveryHit;
            _passive.damageMultiplier = 2f;
            _passive.radius = 2f;
            _heroObject.transform.position = Vector3.zero;
            _hero.Init(_heroData);

            SpawnEnemies(3);
            EnemyController center = _rig.Manager.ActiveEnemies[0];
            EnemyController outsideWidth = _rig.Manager.ActiveEnemies[1];
            EnemyController behind = _rig.Manager.ActiveEnemies[2];
            center.transform.position = new Vector3(0f, 0f, 5f);
            outsideWidth.transform.position = new Vector3(2f, 0f, 5f);
            behind.transform.position = new Vector3(0f, 0f, -1f);

            Assert.IsTrue(_heroObject.GetComponent<SkillRunner>().ExecuteBasicAttack(center));

            Assert.AreEqual(70f, center.CurrentHP, 1e-5f);
            Assert.AreEqual(100f, outsideWidth.CurrentHP, 1e-5f);
            Assert.AreEqual(100f, behind.CurrentHP, 1e-5f);
        }

        [Test]
        public void TryCast_AreaCooldown_ExpiresAfterScaledTimeAdvances()
        {
            _hero.Init(_heroData);
            _passive.kind = SkillKind.AreaAtPoint;
            _passive.radius = 2f;
            _passive.cooldown = 2f;
            SpawnEnemy();

            SkillRunner runner = _heroObject.GetComponent<SkillRunner>();
            Vector3 aimPoint = _rig.Manager.ActiveEnemies[0].transform.position;
            Assert.IsTrue(runner.TryCast(_passive, aimPoint));
            Assert.IsFalse(runner.TryCast(_passive, aimPoint));

            runner.TickCooldown(1f);
            Assert.IsFalse(runner.TryCast(_passive, aimPoint));
            runner.TickCooldown(1f);
            Assert.IsTrue(runner.TryCast(_passive, aimPoint));
        }

        [UnityTest]
        public IEnumerator TryCast_LineDelayed_UsesCastStartOriginAndDamagesEachEnemyOnce()
        {
            _heroObject.transform.position = Vector3.zero;
            _hero.Init(_heroData);
            _passive.kind = SkillKind.Line;
            _passive.damageMultiplier = 2f;
            _passive.length = 6f;
            _passive.width = 2f;
            _passive.duration = 0.5f;
            _passive.castDelay = 0.1f;

            SpawnEnemies(2);
            _rig.Manager.enabled = false;
            EnemyController inside = _rig.Manager.ActiveEnemies[0];
            EnemyController outside = _rig.Manager.ActiveEnemies[1];
            inside.transform.position = new Vector3(0f, 0f, 3f);
            outside.transform.position = new Vector3(2f, 0f, 3f);

            Assert.IsTrue(_heroObject.GetComponent<SkillRunner>().TryCast(_passive, new Vector3(0f, 0f, 6f)));
            _heroObject.transform.position = new Vector3(20f, 0f, 0f);
            yield return new UnityEngine.WaitForSeconds(0.15f);
            yield return null;

            Assert.AreEqual(70f, inside.CurrentHP, 1e-5f);
            Assert.AreEqual(100f, outside.CurrentHP, 1e-5f);
        }

        [UnityTest]
        public IEnumerator TryCast_Zone_UsesXZRadiusAndOwnerPosition()
        {
            _heroObject.transform.position = Vector3.zero;
            _hero.Init(_heroData);
            _passive.kind = SkillKind.Zone;
            _passive.damageMultiplier = 2f;
            _passive.radius = 3f;
            _passive.duration = 0.5f;

            SpawnEnemies(3);
            _rig.Manager.enabled = false;
            EnemyController inside = _rig.Manager.ActiveEnemies[0];
            EnemyController flyingInside = _rig.Manager.ActiveEnemies[1];
            EnemyController outside = _rig.Manager.ActiveEnemies[2];
            inside.transform.position = new Vector3(2f, 0f, 0f);
            flyingInside.transform.position = new Vector3(2f, 2f, 0f);
            outside.transform.position = new Vector3(3.1f, 0f, 0f);
            flyingInside.Data.flying = true;

            Assert.IsTrue(_heroObject.GetComponent<SkillRunner>().TryCastAuto(_passive));
            yield return null;

            Assert.AreEqual(70f, inside.CurrentHP, 1e-5f);
            Assert.AreEqual(70f, flyingInside.CurrentHP, 1e-5f);
            Assert.AreEqual(100f, outside.CurrentHP, 1e-5f);
        }

        [UnityTest]
        public IEnumerator TryCast_ZoneSlow_RefreshesWhileEnemyRemainsInside()
        {
            _heroObject.transform.position = new Vector3(6f, 0f, 0f);
            _hero.Init(_heroData);
            _passive.kind = SkillKind.Zone;
            _passive.damageMultiplier = 1f;
            _passive.radius = 3f;
            _passive.duration = 1f;
            _passive.status = _status;
            _status.element = Element.Ice;
            _status.duration = 0.25f;
            _status.tickInterval = 0f;
            _status.value = 0.3f;

            SpawnEnemy();
            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            enemy.Data.moveSpeed = 0f;
            _heroObject.GetComponent<AttackModule>().enabled = false;

            SkillRunner runner = _heroObject.GetComponent<SkillRunner>();
            Assert.IsTrue(runner.TryCast(_passive, _heroObject.transform.position));
            yield return new UnityEngine.WaitForSeconds(0.6f);

            Assert.AreEqual(85f, enemy.CurrentHP, 1e-5f, "Zone status refresh must not repeat base damage.");
            Assert.IsTrue(enemy.Status.IsSlowed, "Slow should remain active while the enemy is inside the Zone.");
        }

        [UnityTest]
        public IEnumerator Buff_SupportWithinRadius_AppliesAndRemovesDamageBonus()
        {
            _heroObject.transform.position = Vector3.zero;
            _passive.kind = SkillKind.Buff;
            _passive.buffType = BuffType.Damage;
            _passive.buffRadius = 5f;
            _passive.buffValue = 0.15f;
            _hero.Init(_heroData);

            _buffRecipientData = ScriptableObject.CreateInstance<HeroData>();
            _buffRecipientData.id = "buff_recipient";
            _buffRecipientData.damage = 10f;
            _buffRecipientData.attacksPerSecond = 1f;
            _buffRecipientData.range = 5f;
            _buffRecipientData.rarity = Rarity.Common;

            _buffRecipientObject = new GameObject("BuffRecipient");
            _buffRecipientObject.SetActive(false);
            _buffRecipientObject.transform.position = new Vector3(4f, 0f, 0f);
            HeroStats recipientStats = _buffRecipientObject.AddComponent<HeroStats>();
            SetField(recipientStats, "_settings", _statsSettings);
            _buffRecipient = _buffRecipientObject.AddComponent<HeroController>();
            SetField(_buffRecipient, "_rageSettings", _rageSettings);
            LogAssert.Expect(LogType.Error,
                "BuffRecipient: no HeroVisual found under this hero; using the hero root as the visual anchor.");
            _buffRecipientObject.SetActive(true);
            _buffRecipient.Init(_buffRecipientData);

            yield return new UnityEngine.WaitForSeconds(0.25f);
            Assert.AreEqual(11.5f, _buffRecipient.Stats.Damage, 1e-5f);

            _buffRecipientObject.transform.position = new Vector3(6f, 0f, 0f);
            yield return new UnityEngine.WaitForSeconds(0.25f);
            Assert.AreEqual(10f, _buffRecipient.Stats.Damage, 1e-5f);
        }

        private void SpawnEnemies(int count)
        {
            _enemyData.flying = false;
            _rig.Manager.Prewarm(_enemyData, count);
            for (int i = 0; i < count; i++)
                _rig.Manager.RequestSpawn(_enemyData, 0, 1, 6f);
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
