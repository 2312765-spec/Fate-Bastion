using NUnit.Framework;
using FateBastion.Audio;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Slider 0–1 → mixer decibels (S11).</summary>
    public class VolumeMathTests
    {
        private const float Tolerance = 0.01f;

        [Test]
        public void ToDecibels_Zero_ReturnsMinus80() =>
            Assert.AreEqual(VolumeMath.SilentDecibels, VolumeMath.ToDecibels(0f), Tolerance);

        [Test]
        public void ToDecibels_One_Returns0() =>
            Assert.AreEqual(0f, VolumeMath.ToDecibels(1f), Tolerance);

        [Test]
        public void ToDecibels_Half_ReturnsMinus6() =>
            Assert.AreEqual(-6.02f, VolumeMath.ToDecibels(0.5f), Tolerance);

        [Test]
        public void ToDecibels_OutOfRange_IsClamped()
        {
            Assert.AreEqual(0f, VolumeMath.ToDecibels(2f), Tolerance);
            Assert.AreEqual(VolumeMath.SilentDecibels, VolumeMath.ToDecibels(-1f), Tolerance);
        }
    }
}
