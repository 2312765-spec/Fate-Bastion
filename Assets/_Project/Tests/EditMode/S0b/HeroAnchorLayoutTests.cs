using NUnit.Framework;
using UnityEngine;
using FateBastion.Editor.Heroes;

namespace FateBastion.Tests.EditMode.S0b
{
    /// <summary>Anchor placement used by Build Hero Variants (C-08).</summary>
    public class HeroAnchorLayoutTests
    {
        private const float Tolerance = 0.0001f;

        // A 1.8 m hero, 0.6 m wide and deep, standing on the origin.
        private static readonly Bounds Hero = new Bounds(new Vector3(0f, 0.9f, 0f), new Vector3(0.6f, 1.8f, 0.6f));

        [Test]
        public void Overhead_Margin_SitsAboveTopOfBounds()
        {
            Vector3 overhead = HeroAnchorLayout.Overhead(Hero, 0.3f);

            Assert.That(overhead.y, Is.EqualTo(2.1f).Within(Tolerance));
            Assert.That(overhead.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(overhead.z, Is.EqualTo(0f).Within(Tolerance));
        }

        [Test]
        public void Overhead_OffCentreBounds_FollowsBoundsCentre()
        {
            var bounds = new Bounds(new Vector3(0.2f, 1f, -0.1f), new Vector3(1f, 2f, 1f));

            Vector3 overhead = HeroAnchorLayout.Overhead(bounds, 0f);

            Assert.That(overhead.x, Is.EqualTo(0.2f).Within(Tolerance));
            Assert.That(overhead.z, Is.EqualTo(-0.1f).Within(Tolerance));
        }

        [Test]
        public void AuraAnchor_Always_OnGroundUnderHeroRoot()
        {
            Assert.AreEqual(Vector3.zero, HeroAnchorLayout.AuraAnchor());
        }

        [Test]
        public void Muzzle_WithBone_IsBonePlusOffset()
        {
            Vector3 muzzle = HeroAnchorLayout.Muzzle(Hero, new Vector3(0.3f, 1.2f, 0.1f), new Vector3(0f, 0f, 0.25f), 0.6f);

            Assert.That(Vector3.Distance(muzzle, new Vector3(0.3f, 1.2f, 0.35f)), Is.LessThan(Tolerance));
        }

        [Test]
        public void Muzzle_NoBone_FrontOfBoundsAtFallbackHeight()
        {
            Vector3 muzzle = HeroAnchorLayout.Muzzle(Hero, null, Vector3.zero, 0.6f);

            Assert.That(muzzle.y, Is.EqualTo(1.08f).Within(Tolerance), "60% của 1.8 m.");
            Assert.That(muzzle.z, Is.EqualTo(0.3f).Within(Tolerance), "Mặt trước bounds (hướng +Z).");
        }
    }
}
