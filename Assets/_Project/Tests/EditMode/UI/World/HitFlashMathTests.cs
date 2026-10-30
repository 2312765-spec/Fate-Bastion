using NUnit.Framework;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Hit flash brightness: full at the hit, linear fade (S11).</summary>
    public class HitFlashMathTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Intensity_AtStart_IsOne() => Assert.AreEqual(1f, HitFlashMath.Intensity(0.1f, 0.1f), Tolerance);

        [Test]
        public void Intensity_Halfway_IsHalf() => Assert.AreEqual(0.5f, HitFlashMath.Intensity(0.05f, 0.1f), Tolerance);

        [Test]
        public void Flash_AfterDuration_Zero() => Assert.AreEqual(0f, HitFlashMath.Intensity(-0.01f, 0.1f), Tolerance);

        [Test]
        public void Intensity_ZeroDuration_Zero() => Assert.AreEqual(0f, HitFlashMath.Intensity(0.1f, 0f), Tolerance);
    }
}
