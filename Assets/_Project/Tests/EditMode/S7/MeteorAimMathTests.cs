using FateBastion.Player;
using NUnit.Framework;
using UnityEngine;

namespace FateBastion.Tests.EditMode.S7
{
    /// <summary>Checks ground-point range on the horizontal XZ plane.</summary>
    public class MeteorAimMathTests
    {
        [Test]
        public void IsWithinRangeXZ_TargetAtRangeAndDifferentHeight_ReturnsTrue()
        {
            Assert.IsTrue(MeteorAimMath.IsWithinRangeXZ(Vector3.zero, new Vector3(20f, 50f, 0f), 20f));
        }

        [Test]
        public void IsWithinRangeXZ_TargetOutsideRange_ReturnsFalse()
        {
            Assert.IsFalse(MeteorAimMath.IsWithinRangeXZ(Vector3.zero, new Vector3(0f, 0f, 20.01f), 20f));
        }
    }
}
