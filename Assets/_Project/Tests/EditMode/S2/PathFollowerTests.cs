using NUnit.Framework;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>Distance along the path, progress and the "reached the Castle" check by distance, not collision (S2).</summary>
    public class PathFollowerTests
    {
        private const float Tolerance = 1e-4f;

        private PathFollower _follower;

        [SetUp]
        public void SetUp()
        {
            _follower = new PathFollower();
            _follower.Reset(new StraightTestPath(10f), 0f);
        }

        [Test]
        public void Step_ShortOfEnd_NotReached()
        {
            bool reached = _follower.Step(2.5f, 1f, 1f);
            Assert.IsFalse(reached);
            Assert.AreEqual(2.5f, _follower.Distance, Tolerance);
            Assert.AreEqual(0.25f, _follower.Progress, Tolerance);
        }

        [Test]
        public void Step_ExactlyAtEnd_Reached()
        {
            Assert.IsTrue(_follower.Step(5f, 1f, 2f));
            Assert.IsTrue(_follower.HasReachedEnd);
            Assert.AreEqual(1f, _follower.Progress, Tolerance);
        }

        [Test]
        public void Step_LargeStepPastEnd_ReachedAndProgressClamped()
        {
            // At x2 a single frame can jump well past the end; the check is distance >= length.
            Assert.IsTrue(_follower.Step(4.5f, 1f, 5f));
            Assert.AreEqual(1f, _follower.Progress, Tolerance);
        }

        [Test]
        public void Step_Slowed30Percent_Moves70Percent()
        {
            _follower.Step(2f, 0.7f, 1f);
            Assert.AreEqual(1.4f, _follower.Distance, Tolerance);
        }

        [Test]
        public void Step_ZeroDeltaTime_DoesNotMove()
        {
            _follower.Step(2.5f, 1f, 0f);
            Assert.AreEqual(0f, _follower.Distance, Tolerance);
        }

        [Test]
        public void Reset_WithStartDistance_StartsThere()
        {
            _follower.Reset(new StraightTestPath(10f), 4f);
            Assert.AreEqual(4f, _follower.Distance, Tolerance);
            Assert.AreEqual(0.4f, _follower.Progress, Tolerance);
        }

        [Test]
        public void Reset_ZeroLengthPath_ReachedImmediately()
        {
            _follower.Reset(new StraightTestPath(0f), 0f);
            Assert.IsTrue(_follower.HasReachedEnd);
            Assert.AreEqual(1f, _follower.Progress, Tolerance);
        }

        [Test]
        public void Reset_AfterReachingEnd_ClearsState()
        {
            _follower.Step(100f, 1f, 1f);
            _follower.Reset(new StraightTestPath(10f), 0f);
            Assert.IsFalse(_follower.HasReachedEnd);
            Assert.AreEqual(0f, _follower.Progress, Tolerance);
        }
    }
}
