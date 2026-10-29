using System.Collections;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Player;
using FateBastion.Skills;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Tests.PlayMode.S2;

namespace FateBastion.Tests.PlayMode.S7
{
    /// <summary>Checks meteor radius damage on flying enemies and duplicate colliders.</summary>
    public class MeteorDamageApplierPlayModeTests
    {
        private EnemyTestRig _rig;
        private SkillData _skill;
        private MeteorSettings _settings;

        [SetUp]
        public void SetUp()
        {
            _rig = new EnemyTestRig(1000f);
            _skill = ScriptableObject.CreateInstance<SkillData>();
            _skill.kind = SkillKind.AreaAtPoint;
            _skill.aimMode = AimMode.GroundPoint;
            _skill.damageType = DamageType.True;
            _skill.damageMultiplier = 3f;
            _skill.radius = 4f;
            _settings = ScriptableObject.CreateInstance<MeteorSettings>();
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
            Object.Destroy(_settings);
            Object.Destroy(_skill);
        }

        [UnityTest]
        public IEnumerator Apply_FlyingEnemyInsideRadius_DamagesOnce()
        {
            EnemyData normal = _rig.CreateEnemy("enemy_normal", baseHP: 40f);
            EnemyData flying = _rig.CreateEnemy("enemy_flying", baseHP: 1000f, flying: true);
            flying.prefab.AddComponent<BoxCollider>();
            flying.prefab.AddComponent<SphereCollider>();
            _settings.Configure(_skill, normal, 20f, 2f);

            LevelData level = _rig.CreateLevel();
            level.hpGrowth = 1f;
            level.hpMultiplier = 1f;
            _rig.Manager.RequestSpawn(flying, 1, 1);
            yield return null;

            EnemyController enemy = _rig.Manager.ActiveEnemies[0];
            enemy.transform.position = new Vector3(3f, 2f, 0f);
            Physics.SyncTransforms();
            var applier = new MeteorDamageApplier(_settings, _rig.Manager);

            int hitCount = applier.Apply(Vector3.zero, 1, level, null);

            Assert.AreEqual(1, hitCount);
            Assert.AreEqual(880f, enemy.CurrentHP, 0.001f);
            Assert.IsTrue(enemy.IsFlying);
        }
    }
}
