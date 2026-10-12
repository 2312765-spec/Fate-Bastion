using NUnit.Framework;
using UnityEngine;
using FateBastion.Combat;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests that range and blast checks use the XZ plane, so flying enemies 2 m up stay in range (S1).</summary>
    public class CombatMathTests
    {
        [Test]
        public void DistanceXZ_IgnoresHeight()
        {
            Assert.AreEqual(3f, CombatMath.DistanceXZ(Vector3.zero, new Vector3(3f, 2f, 0f)), 1e-5f);
        }

        [Test]
        public void DistanceXZ_UsesXAndZ()
        {
            Assert.AreEqual(5f, CombatMath.DistanceXZ(Vector3.zero, new Vector3(3f, 10f, 4f)), 1e-5f);
        }

        [Test]
        public void SqrDistanceXZ_IgnoresHeight()
        {
            Assert.AreEqual(25f, CombatMath.SqrDistanceXZ(new Vector3(0f, 5f, 0f), new Vector3(3f, 0f, 4f)), 1e-5f);
        }

        [Test]
        public void InRangeXZ_FlyingAboveRange_StillInRange()
        {
            // 3D distance is sqrt(9 + 4) = 3.6 > 3.5, but XZ distance is 3.
            Assert.IsTrue(CombatMath.InRangeXZ(Vector3.zero, new Vector3(3f, 2f, 0f), 3.5f));
        }

        [Test]
        public void InRangeXZ_OnBoundary_Included()
        {
            Assert.IsTrue(CombatMath.InRangeXZ(Vector3.zero, new Vector3(0f, 0f, 4f), 4f));
        }

        [Test]
        public void InRangeXZ_Outside_Excluded()
        {
            Assert.IsFalse(CombatMath.InRangeXZ(Vector3.zero, new Vector3(4.01f, 0f, 0f), 4f));
        }
    }
}
