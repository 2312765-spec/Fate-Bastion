using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests that an enemy dies exactly once and overkill is discarded (S1).</summary>
    public class HealthTests
    {
        private Health _health;

        [SetUp]
        public void SetUp()
        {
            _health = new Health();
            _health.Reset(100f);
        }

        [TearDown]
        public void TearDown()
        {
            CombatEvents.ResetAll();
        }

        [Test]
        public void ApplyDamage_NonLethal_ReducesHP()
        {
            Assert.IsFalse(_health.ApplyDamage(30f));
            Assert.AreEqual(70f, _health.Current, 1e-5f);
            Assert.IsTrue(_health.IsAlive);
        }

        [Test]
        public void ApplyDamage_ThreeLethalHitsSameFrame_ReportsDeathOnce()
        {
            int deaths = 0;
            if (_health.ApplyDamage(150f)) deaths++;
            if (_health.ApplyDamage(150f)) deaths++;
            if (_health.ApplyDamage(150f)) deaths++;
            Assert.AreEqual(1, deaths);
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void ApplyDamage_ThreeLethalHitsSameFrame_RaisesEnemyKilledOnce()
        {
            // Mirrors what EnemyController (S2) does: raise the hub event only when ApplyDamage returns true.
            int killedEvents = 0;
            CombatEvents.OnEnemyKilled += _ => killedEvents++;

            for (int i = 0; i < 3; i++)
            {
                if (_health.ApplyDamage(100f))
                    CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, false));
            }

            Assert.AreEqual(1, killedEvents);
        }

        [Test]
        public void ApplyDamage_ExactHP_Kills()
        {
            Assert.IsTrue(_health.ApplyDamage(100f));
            Assert.AreEqual(0f, _health.Current, 1e-5f);
        }

        [Test]
        public void ApplyDamage_Overkill_ClampsToZero()
        {
            _health.ApplyDamage(250f);
            Assert.AreEqual(0f, _health.Current, 1e-5f);
        }

        [Test]
        public void ApplyDamage_AfterDeath_Ignored()
        {
            _health.ApplyDamage(100f);
            Assert.IsFalse(_health.ApplyDamage(10f));
            Assert.AreEqual(0f, _health.Current, 1e-5f);
        }

        [Test]
        public void ApplyDamage_ZeroOrNegative_Ignored()
        {
            Assert.IsFalse(_health.ApplyDamage(0f));
            Assert.IsFalse(_health.ApplyDamage(-5f));
            Assert.AreEqual(100f, _health.Current, 1e-5f);
        }

        [Test]
        public void TryMarkDead_Alive_ReturnsTrueOnce()
        {
            // Used when the enemy reaches the Castle: the caller handles it only if this returns true.
            Assert.IsTrue(_health.TryMarkDead());
            Assert.IsFalse(_health.TryMarkDead());
            Assert.IsFalse(_health.IsAlive);
        }

        [Test]
        public void TryMarkDead_AfterKilledSameFrame_ReturnsFalse()
        {
            // S2: killed and reaching the Castle in one frame is handled once, as "killed".
            _health.ApplyDamage(100f);
            Assert.IsFalse(_health.TryMarkDead());
        }

        [Test]
        public void Reset_AfterDeath_RevivesWithFullHP()
        {
            _health.ApplyDamage(100f);
            _health.Reset(80f);
            Assert.IsTrue(_health.IsAlive);
            Assert.AreEqual(80f, _health.Current, 1e-5f);
            Assert.AreEqual(80f, _health.Max, 1e-5f);
        }
    }
}
