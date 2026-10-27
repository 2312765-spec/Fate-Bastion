using NUnit.Framework;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S6
{
    /// <summary>Attack cadence is deterministic and independent of Unity scene state.</summary>
    public class AttackTimerTests
    {
        [Test]
        public void Tick_FirstValidTarget_AttacksImmediately()
        {
            var timer = new AttackTimer();

            Assert.IsTrue(timer.Tick(0f, 1f, true));
        }

        [Test]
        public void Tick_OneAttackPerSecond_WaitsForScaledInterval()
        {
            var timer = new AttackTimer();
            Assert.IsTrue(timer.Tick(0f, 1f, true));

            Assert.IsFalse(timer.Tick(0.99f, 1f, true));
            Assert.IsTrue(timer.Tick(0.01f, 1f, true));
        }

        [Test]
        public void Tick_NoValidTarget_ResetsForImmediateAttackOnReacquire()
        {
            var timer = new AttackTimer();
            Assert.IsTrue(timer.Tick(0f, 1f, true));
            Assert.IsFalse(timer.Tick(0.5f, 1f, true));

            Assert.IsFalse(timer.Tick(0f, 1f, false));
            Assert.IsTrue(timer.Tick(0f, 1f, true));
        }

        [Test]
        public void Tick_AttackSpeedMultiplier_ShortensInterval()
        {
            var timer = new AttackTimer();
            Assert.IsTrue(timer.Tick(0f, 2f, true));

            Assert.IsFalse(timer.Tick(0.49f, 2f, true));
            Assert.IsTrue(timer.Tick(0.01f, 2f, true));
        }

        [Test]
        public void Tick_InvalidAttackSpeed_DoesNotAttack()
        {
            var timer = new AttackTimer();

            Assert.IsFalse(timer.Tick(0.1f, 0f, true));
            Assert.IsFalse(timer.Tick(0.1f, float.NaN, true));
        }
    }
}
