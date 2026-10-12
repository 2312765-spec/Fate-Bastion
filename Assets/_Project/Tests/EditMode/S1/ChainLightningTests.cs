using NUnit.Framework;
using UnityEngine;
using FateBastion.Combat;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests for Lightning chains: up to 2 enemies within 3 m (XZ), no repeats, -30% per bounce (S1).</summary>
    public class ChainLightningTests
    {
        private const int MaxBounces = 2;
        private const float Radius = 3f;
        private const float Falloff = 0.3f;

        private readonly int[] _results = new int[MaxBounces];

        private int Select(int first, Vector3[] positions, bool[] alive)
        {
            return ChainLightning.SelectTargets(first, positions, alive, positions.Length, MaxBounces, Radius,
                _results);
        }

        private static bool[] AllAlive(int count)
        {
            var alive = new bool[count];
            for (int i = 0; i < count; i++) alive[i] = true;
            return alive;
        }

        [Test]
        public void SelectTargets_TwoNearby_BouncesToBoth()
        {
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            Assert.AreEqual(2, Select(0, positions, AllAlive(3)));
            Assert.AreEqual(1, _results[0]);
            Assert.AreEqual(2, _results[1]);
        }

        [Test]
        public void SelectTargets_FourNearby_StopsAtTwo()
        {
            var positions = new[]
            {
                Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(-1f, 0f, 0f),
                new Vector3(0f, 0f, -1f)
            };
            Assert.AreEqual(2, Select(0, positions, AllAlive(5)));
        }

        [Test]
        public void SelectTargets_OutOfRadius_Ignored()
        {
            var positions = new[] { Vector3.zero, new Vector3(3.5f, 0f, 0f) };
            Assert.AreEqual(0, Select(0, positions, AllAlive(2)));
        }

        [Test]
        public void SelectTargets_ExactlyOnRadius_Included()
        {
            var positions = new[] { Vector3.zero, new Vector3(3f, 0f, 0f) };
            Assert.AreEqual(1, Select(0, positions, AllAlive(2)));
        }

        [Test]
        public void SelectTargets_FlyingWithinXZ_Included()
        {
            // Flying enemies are 2 m up; range is measured on the XZ plane.
            var positions = new[] { Vector3.zero, new Vector3(2.9f, 2f, 0f) };
            Assert.AreEqual(1, Select(0, positions, AllAlive(2)));
        }

        [Test]
        public void SelectTargets_DeadEnemy_Skipped()
        {
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            var alive = new[] { true, false, true };
            Assert.AreEqual(1, Select(0, positions, alive));
            Assert.AreEqual(2, _results[0]);
        }

        [Test]
        public void SelectTargets_OnlyOneOther_NeverBouncesBackToFirst()
        {
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f) };
            Assert.AreEqual(1, Select(0, positions, AllAlive(2)));
            Assert.AreEqual(1, _results[0]);
        }

        [Test]
        public void SelectTargets_HopsFromLastHit()
        {
            // C is 5 m from A but 2.5 m from B, so A -> B -> C.
            var positions = new[] { Vector3.zero, new Vector3(2.5f, 0f, 0f), new Vector3(5f, 0f, 0f) };
            Assert.AreEqual(2, Select(0, positions, AllAlive(3)));
            Assert.AreEqual(1, _results[0]);
            Assert.AreEqual(2, _results[1]);
        }

        [Test]
        public void SelectTargets_PicksNearestFirst()
        {
            var positions = new[] { Vector3.zero, new Vector3(2.5f, 0f, 0f), new Vector3(0f, 0f, 1f) };
            Select(0, positions, AllAlive(3));
            Assert.AreEqual(2, _results[0]);
        }

        [Test]
        public void SelectTargets_ResultBufferSmallerThanBounces_RespectsBuffer()
        {
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            var small = new int[1];
            int count = ChainLightning.SelectTargets(0, positions, AllAlive(3), positions.Length, MaxBounces, Radius,
                small);
            Assert.AreEqual(1, count);
        }

        [Test]
        public void SelectTargets_CountSmallerThanArray_IgnoresTail()
        {
            // Callers reuse a big prebuilt array and pass how many slots are filled this frame.
            var positions = new[] { Vector3.zero, new Vector3(1f, 0f, 0f), new Vector3(2f, 0f, 0f) };
            int count = ChainLightning.SelectTargets(0, positions, AllAlive(3), 2, MaxBounces, Radius, _results);
            Assert.AreEqual(1, count);
        }

        [TestCase(1, 70f)]
        [TestCase(2, 49f)]
        public void BounceDamage_Multiplicative(int bounce, float expected)
        {
            Assert.AreEqual(expected, ChainLightning.BounceDamage(100f, Falloff, bounce), 1e-3f);
        }

        [Test]
        public void BounceDamage_ZeroBounce_IsFullDamage()
        {
            Assert.AreEqual(100f, ChainLightning.BounceDamage(100f, Falloff, 0), 1e-5f);
        }
    }
}
