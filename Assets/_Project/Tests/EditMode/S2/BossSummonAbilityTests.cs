using NUnit.Framework;
using UnityEngine;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>BossSummonAbility timer: one batch every IntervalSeconds, invalid settings never summon (S2/A-05).</summary>
    public class BossSummonAbilityTests
    {
        private EnemyData _enemy;
        private BossSummonSettings _settings;
        private BossSummonAbility _ability;

        [SetUp]
        public void SetUp()
        {
            _enemy = ScriptableObject.CreateInstance<EnemyData>();
            _enemy.id = "enemy_nhanh";
            _settings = ScriptableObject.CreateInstance<BossSummonSettings>();
            _settings.Configure(_enemy, 3, 1f);
            _ability = new BossSummonAbility();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_enemy);
            Object.DestroyImmediate(_settings);
        }

        [Test]
        public void Begin_ValidSettings_Active()
        {
            _ability.Begin(_settings);
            Assert.IsTrue(_ability.IsActive);
            Assert.AreSame(_enemy, _ability.SummonEnemy);
        }

        [Test]
        public void Begin_NullOrInvalidSettings_NotActive()
        {
            _ability.Begin(null);
            Assert.IsFalse(_ability.IsActive);

            _settings.Configure(null, 3, 1f);
            _ability.Begin(_settings);
            Assert.IsFalse(_ability.IsActive, "thiếu EnemyData thì không gọi được quái");
        }

        [Test]
        public void Tick_BeforeInterval_ReturnsZero()
        {
            _ability.Begin(_settings);
            Assert.AreEqual(0, _ability.Tick(0.9f));
        }

        [Test]
        public void Tick_AtInterval_ReturnsCount()
        {
            _ability.Begin(_settings);
            Assert.AreEqual(3, _ability.Tick(1f), "12 s (đây 1 s) gọi 3 quái");
        }

        [Test]
        public void Tick_EveryInterval_ReturnsCountEachBatch()
        {
            _ability.Begin(_settings);
            Assert.AreEqual(3, _ability.Tick(1f));
            Assert.AreEqual(0, _ability.Tick(0.5f));
            Assert.AreEqual(3, _ability.Tick(0.6f));
        }

        [Test]
        public void Tick_LargeDelta_CatchesUpInWholeBatches()
        {
            _ability.Begin(_settings);
            // 3 whole batches now, the ~0.5 s leftover stays on the timer.
            Assert.AreEqual(9, _ability.Tick(3.5f));
            Assert.AreEqual(3, _ability.Tick(0.5f));
            Assert.AreEqual(0, _ability.Tick(0.4f));
            Assert.AreEqual(3, _ability.Tick(0.7f));
        }

        [Test]
        public void Tick_ZeroDelta_DoesNotAdvance()
        {
            _ability.Begin(_settings);
            Assert.AreEqual(0, _ability.Tick(0f));
            Assert.AreEqual(0, _ability.Tick(0.5f));
            Assert.AreEqual(3, _ability.Tick(0.6f));
        }

        [Test]
        public void Clear_StopsSummoning()
        {
            _ability.Begin(_settings);
            _ability.Clear();
            Assert.IsFalse(_ability.IsActive);
            Assert.AreEqual(0, _ability.Tick(100f));
        }
    }
}
