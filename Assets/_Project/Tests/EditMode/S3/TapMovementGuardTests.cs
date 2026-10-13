using NUnit.Framework;
using UnityEngine;
using FateBastion.Player;

namespace FateBastion.Tests.EditMode.S3
{
    /// <summary>A right mouse tap only cancels when the mouse moved less than 5 px while pressed (S3).</summary>
    public class TapMovementGuardTests
    {
        private const float MaxPixels = 5f;

        private TapMovementGuard _guard;

        [SetUp]
        public void SetUp()
        {
            _guard = new TapMovementGuard();
        }

        [Test]
        public void IsTap_NoMovement_True()
        {
            _guard.Begin();

            Assert.IsTrue(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void IsTap_SmallJitter_True()
        {
            _guard.Begin();
            _guard.AddDelta(new Vector2(1f, 1f));
            _guard.AddDelta(new Vector2(-1f, 1f));

            Assert.IsTrue(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void IsTap_MovedFivePixels_False()
        {
            _guard.Begin();
            _guard.AddDelta(new Vector2(3f, 0f));
            _guard.AddDelta(new Vector2(2f, 0f));

            Assert.IsFalse(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void IsTap_MovedAwayAndBack_False()
        {
            // Path length counts, not net displacement: a drag that returns is still a camera drag.
            _guard.Begin();
            _guard.AddDelta(new Vector2(10f, 0f));
            _guard.AddDelta(new Vector2(-10f, 0f));

            Assert.IsFalse(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void AddDelta_NotPressed_IsIgnored()
        {
            _guard.AddDelta(new Vector2(50f, 0f));
            _guard.Begin();

            Assert.IsTrue(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void IsTap_NeverBegun_False()
        {
            Assert.IsFalse(_guard.IsTap(MaxPixels));
        }

        [Test]
        public void End_AfterPress_StopsCounting()
        {
            _guard.Begin();
            _guard.End();
            _guard.AddDelta(new Vector2(50f, 0f));

            Assert.IsFalse(_guard.IsPressed);
        }
    }
}
