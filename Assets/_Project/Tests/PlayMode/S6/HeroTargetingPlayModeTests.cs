using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Heroes;
using FateBastion.Tests.PlayMode.S2;

namespace FateBastion.Tests.PlayMode.S6
{
    /// <summary>Targeting honors front-of-line, boss priority, melee flight and horizontal range rules.</summary>
    public class HeroTargetingPlayModeTests
    {
        private EnemyTestRig _rig;
        private HeroData _hero;
        private TargetingModule _targeting;
        private EnemyData _normal;
        private EnemyData _boss;
        private EnemyData _flying;

        [SetUp]
        public void SetUp()
        {
            CombatEvents.ResetAll();
            _rig = new EnemyTestRig(100f);
            _normal = _rig.CreateEnemy("enemy_normal");
            _boss = _rig.CreateEnemy("enemy_boss", boss: true);
            _flying = _rig.CreateEnemy("enemy_fly", flying: true);

            _hero = ScriptableObject.CreateInstance<HeroData>();
            _hero.range = 40f;
            _hero.attackType = AttackType.Projectile;
            _targeting = new TargetingModule();
            _targeting.Initialize(_hero);
        }

        [TearDown]
        public void TearDown()
        {
            _rig.Dispose();
            Object.Destroy(_hero);
        }

        [Test]
        public void FindTarget_NonSingleDps_SelectsFrontOfLine()
        {
            _hero.role = Role.Mage;
            EnemyController rear = Spawn(_normal, 10f);
            EnemyController front = Spawn(_boss, 25f);

            EnemyController target = _targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies);

            Assert.AreSame(front, target);
            Assert.AreNotSame(rear, target);
        }

        [Test]
        public void FindTarget_SingleDps_PrioritizesBossWithinRange()
        {
            _hero.role = Role.SingleDps;
            EnemyController frontNormal = Spawn(_normal, 30f);
            EnemyController boss = Spawn(_boss, 15f);

            EnemyController target = _targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies);

            Assert.AreSame(boss, target);
            Assert.AreNotSame(frontNormal, target);
        }

        [Test]
        public void FindTarget_SingleDps_BossOutsideRange_SelectsFrontNormal()
        {
            _hero.role = Role.SingleDps;
            _hero.range = 20f;
            EnemyController normal = Spawn(_normal, 18f);
            Spawn(_boss, 35f);

            EnemyController target = _targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies);

            Assert.AreSame(normal, target);
        }

        [Test]
        public void FindTarget_Melee_SkipsFlyingEnemy()
        {
            _hero.attackType = AttackType.Melee;
            EnemyController ground = Spawn(_normal, 10f);
            Spawn(_flying, 30f);

            EnemyController target = _targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies);

            Assert.AreSame(ground, target);
        }

        [Test]
        public void FindTarget_RangeUsesHorizontalDistance()
        {
            _hero.range = 20f;
            EnemyController target = Spawn(_normal, 20f);

            Assert.AreSame(target, _targeting.FindTarget(new Vector3(0f, 100f, 0f), _rig.Manager.ActiveEnemies));
        }

        [Test]
        public void FindTarget_UsesEffectiveRangeAfterUpgrade()
        {
            _hero.range = 5f;
            EnemyController target = Spawn(_normal, 8f);

            Assert.IsNull(_targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies));
            Assert.AreSame(target, _targeting.FindTarget(Vector3.zero, _rig.Manager.ActiveEnemies, 10f));
        }

        private EnemyController Spawn(EnemyData data, float distance)
        {
            _rig.Manager.Prewarm(data, 1);
            _rig.Manager.RequestSpawn(data, 0, 1, distance);
            return _rig.Manager.ActiveEnemies[_rig.Manager.ActiveEnemies.Count - 1];
        }
    }
}
