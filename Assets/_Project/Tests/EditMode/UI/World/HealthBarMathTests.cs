using NUnit.Framework;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Fill ratio and the "hidden at full HP" rule of the enemy health bar (C-14, S10).</summary>
    public class HealthBarMathTests
    {
        [Test]
        public void FillRatio_HalfHealth_ReturnsHalf()
        {
            Assert.AreEqual(0.5f, HealthBarMath.FillRatio(50f, 100f), 1e-6f);
        }

        [Test]
        public void FillRatio_ZeroMax_ReturnsZero()
        {
            Assert.AreEqual(0f, HealthBarMath.FillRatio(10f, 0f));
        }

        [Test]
        public void FillRatio_CurrentAboveMax_ClampsToOne()
        {
            Assert.AreEqual(1f, HealthBarMath.FillRatio(120f, 100f));
        }

        [Test]
        public void FillRatio_NegativeCurrent_ClampsToZero()
        {
            Assert.AreEqual(0f, HealthBarMath.FillRatio(-5f, 100f));
        }

        [Test]
        public void ShouldShow_FullHealth_ReturnsFalse()
        {
            Assert.IsFalse(HealthBarMath.ShouldShow(100f, 100f));
        }

        [Test]
        public void ShouldShow_Damaged_ReturnsTrue()
        {
            Assert.IsTrue(HealthBarMath.ShouldShow(99f, 100f));
        }

        [Test]
        public void ShouldShow_Dead_ReturnsFalse()
        {
            Assert.IsFalse(HealthBarMath.ShouldShow(0f, 100f));
        }
    }
}
