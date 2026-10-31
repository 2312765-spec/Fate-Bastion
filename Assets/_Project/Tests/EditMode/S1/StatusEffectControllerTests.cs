using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;
using UnityEngine.TestTools.Constraints;
using Is =UnityEngine.TestTools.Constraints.Is;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests for burn and slow stacking: strongest wins, slow cap 60%, boss 50% effectiveness (S1).</summary>
    public class StatusEffectControllerTests
    {
        private const float SlowCap = 0.6f;
        private const float BossSlowEffectiveness = 0.5f;
        private const float FrameStep = 0.02f;

        private StatusEffectData _burn;
        private StatusEffectData _slow;
        private StatusEffectData _slowOther;
        private StatusEffectData _slowThird;
        private StatusEffectController _controller;

        [SetUp]
        public void SetUp()
        {
            _burn = Create("status_burn", Element.Fire, 3f, 1f, 0.2f);
            _slow = Create("status_slow", Element.Ice, 2f, 0f, 0.3f);
            _slowOther = Create("status_slow_other", Element.Ice, 2f, 0f, 0.2f);
            _slowThird = Create("status_slow_third", Element.Ice, 2f, 0f, 0.3f);
            _controller = new StatusEffectController(SlowCap, BossSlowEffectiveness);
            _controller.ResetState(false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_burn);
            Object.DestroyImmediate(_slow);
            Object.DestroyImmediate(_slowOther);
            Object.DestroyImmediate(_slowThird);
        }

        private static StatusEffectData Create(string id, Element element, float duration, float tickInterval,
            float value)
        {
            var data = ScriptableObject.CreateInstance<StatusEffectData>();
            data.id = id;
            data.element = element;
            data.duration = duration;
            data.tickInterval = tickInterval;
            data.value = value;
            data.stackRule = StackRule.StrongestWins;
            return data;
        }

        private float TickFor(float seconds)
        {
            float total = 0f;
            int frames = Mathf.RoundToInt(seconds / FrameStep);
            for (int i = 0; i < frames; i++) total += _controller.Tick(FrameStep);
            return total;
        }

        // --- Burn ---

        [Test]
        public void Tick_Burn_DealsValueTimesHitPerTick()
        {
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(20f, _controller.Tick(1f), 1e-3f);
        }

        [Test]
        public void Tick_Burn_NoDamageBeforeFirstInterval()
        {
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(0f, _controller.Tick(0.5f), 1e-3f);
        }

        [Test]
        public void Burn_ThreeSeconds_TicksThreeTimesThenStops()
        {
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(60f, TickFor(4f), 1e-2f);
            Assert.IsFalse(_controller.IsBurning);
        }

        [Test]
        public void Burn_LargeDeltaTime_SumsTicks()
        {
            // x2 speed: one frame can cover two ticks.
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(40f, _controller.Tick(2f), 1e-3f);
        }

        [Test]
        public void Burn_StrongerHit_ReplacesWeaker()
        {
            _controller.Apply(_burn, 50f, DamageType.Magic);
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(20f, _controller.Tick(1f), 1e-3f);
        }

        [Test]
        public void Burn_WeakerHit_KeepsStrongerAndRefreshesDuration()
        {
            _controller.Apply(_burn, 100f, DamageType.Magic);
            TickFor(2f);
            _controller.Apply(_burn, 10f, DamageType.Magic);

            // Duration restarted at t = 2 with the stronger value: three more ticks of 20.
            Assert.AreEqual(60f, TickFor(3f), 1e-2f);
            Assert.AreEqual(0f, TickFor(1f), 1e-3f);
        }

        [Test]
        public void Burn_Reapply_DoesNotResetTickPhase()
        {
            // A hero hitting every 0.8 s must not keep pushing the next tick away.
            _controller.Apply(_burn, 100f, DamageType.Magic);
            TickFor(0.8f);
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(20f, TickFor(0.2f), 1e-3f);
        }

        [Test]
        public void Burn_DamageTypeAndElement_FollowHit()
        {
            _controller.Apply(_burn, 100f, DamageType.Physical);
            Assert.AreEqual(DamageType.Physical, _controller.BurnDamageType);
            Assert.AreEqual(Element.Fire, _controller.BurnElement);
        }

        [Test]
        public void Burn_StatusChanged_ReapplyAndExpiryRaiseOnlyStateTransitions()
        {
            int started = 0;
            int stopped = 0;
            _controller.OnStatusChanged += (data, active) =>
            {
                if (!ReferenceEquals(data, _burn)) return;
                if (active) started++;
                else stopped++;
            };

            _controller.Apply(_burn, 100f, DamageType.Magic);
            _controller.Apply(_burn, 100f, DamageType.Magic);
            Assert.AreEqual(1, started);
            Assert.AreEqual(0, stopped);

            TickFor(_burn.duration + FrameStep);
            Assert.AreEqual(1, stopped);
        }

        // --- Slow ---

        [Test]
        public void Slow_ThirtyPercent_SpeedMultiplier07()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            Assert.AreEqual(0.7f, _controller.SpeedMultiplier, 1e-5f);
        }

        [Test]
        public void Slow_SameSourceTwice_StrongestNotSum()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            Assert.AreEqual(0.3f, _controller.SlowAmount, 1e-5f);
        }

        [Test]
        public void Slow_StatusChanged_ReapplyAndExpiryRaiseOnlyStateTransitions()
        {
            int started = 0;
            int stopped = 0;
            _controller.OnStatusChanged += (data, active) =>
            {
                if (!ReferenceEquals(data, _slow)) return;
                if (active) started++;
                else stopped++;
            };

            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            Assert.AreEqual(1, started);
            Assert.AreEqual(0, stopped);

            _controller.Tick(_slow.duration + FrameStep);
            Assert.AreEqual(1, stopped);
        }

        [Test]
        public void Slow_DifferentSources_Add()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.Apply(_slowOther, 100f, DamageType.Magic);
            Assert.AreEqual(0.5f, _controller.SlowAmount, 1e-5f);
        }

        [Test]
        public void Slow_ThreeSources_CappedAt60()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.Apply(_slowOther, 100f, DamageType.Magic);
            _controller.Apply(_slowThird, 100f, DamageType.Magic);
            Assert.AreEqual(0.6f, _controller.SlowAmount, 1e-5f);
            Assert.AreEqual(0.4f, _controller.SpeedMultiplier, 1e-5f);
        }

        [Test]
        public void Slow_Boss_HalfEffect()
        {
            _controller.ResetState(true);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            Assert.AreEqual(0.15f, _controller.SlowAmount, 1e-5f);
        }

        [Test]
        public void Slow_BossOverCap_CapThenHalf()
        {
            _controller.ResetState(true);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.Apply(_slowOther, 100f, DamageType.Magic);
            _controller.Apply(_slowThird, 100f, DamageType.Magic);
            Assert.AreEqual(0.3f, _controller.SlowAmount, 1e-5f);
        }

        [Test]
        public void Slow_Expires_AfterDuration()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            TickFor(2.1f);
            Assert.IsFalse(_controller.IsSlowed);
            Assert.AreEqual(1f, _controller.SpeedMultiplier, 1e-5f);
        }

        [Test]
        public void Slow_Reapply_RefreshesDuration()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            TickFor(1.5f);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            TickFor(1.5f);
            Assert.AreEqual(0.3f, _controller.SlowAmount, 1e-5f);
        }

        [Test]
        public void Slow_OneSourceExpires_OtherStays()
        {
            _controller.Apply(_slow, 100f, DamageType.Magic);
            TickFor(1f);
            _controller.Apply(_slowOther, 100f, DamageType.Magic);
            TickFor(1.5f);
            Assert.AreEqual(0.2f, _controller.SlowAmount, 1e-5f);
        }

        // --- Lifecycle and misc ---

        [Test]
        public void ResetState_RemovesAllEffects()
        {
            _controller.Apply(_burn, 100f, DamageType.Magic);
            _controller.Apply(_slow, 100f, DamageType.Magic);
            _controller.ResetState(false);
            Assert.IsFalse(_controller.IsBurning);
            Assert.IsFalse(_controller.IsSlowed);
            Assert.AreEqual(0f, _controller.Tick(1f), 1e-5f);
        }

        [Test]
        public void Apply_LightningOrNull_NoEffect()
        {
            var chain = Create("status_chain", Element.Lightning, 1f, 0f, 0.3f);
            _controller.Apply(chain, 100f, DamageType.Magic);
            _controller.Apply(null, 100f, DamageType.Magic);
            Assert.IsFalse(_controller.IsBurning);
            Assert.IsFalse(_controller.IsSlowed);
            Object.DestroyImmediate(chain);
        }

        [Test]
        public void ApplyAndTick_HundredEnemiesBurningAndSlowed_NoGCAllocation()
        {
            var controllers = new StatusEffectController[100];
            for (int i = 0; i < controllers.Length; i++)
            {
                controllers[i] = new StatusEffectController(SlowCap, BossSlowEffectiveness);
                controllers[i].ResetState(false);
            }

            TestDelegate frame = () =>
            {
                for (int i = 0; i < controllers.Length; i++)
                {
                    controllers[i].Apply(_burn, 100f, DamageType.Magic);
                    controllers[i].Apply(_slow, 100f, DamageType.Magic);
                    controllers[i].Tick(FrameStep);
                }
            };

            // Warm up once so JIT and first-call static init are not counted as per-frame garbage.
            frame();
            Assert.That(frame, Is.Not.AllocatingGCMemory());
        }
    }
}
