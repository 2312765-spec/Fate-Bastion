using NUnit.Framework;
using FateBastion.Audio;
using FateBastion.Core;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Which effect a damage or kill event plays (C-09).</summary>
    public class FxKindMapTests
    {
        [TestCase(Element.Fire, FxKind.HitFire)]
        [TestCase(Element.Ice, FxKind.HitIce)]
        [TestCase(Element.Lightning, FxKind.HitLightning)]
        public void TryGetHit_MagicByElement_PicksElementSpark(Element element, FxKind expected)
        {
            Assert.IsTrue(FxKindMap.TryGetHit(DamageType.Magic, element, out FxKind kind));
            Assert.AreEqual(expected, kind);
        }

        [Test]
        public void TryGetHit_Physical_StillUsesElement()
        {
            Assert.IsTrue(FxKindMap.TryGetHit(DamageType.Physical, Element.Lightning, out FxKind kind));
            Assert.AreEqual(FxKind.HitLightning, kind);
        }

        [Test]
        public void TryGetHit_TrueDamage_NoSpark() =>
            Assert.IsFalse(FxKindMap.TryGetHit(DamageType.True, Element.Fire, out _));

        [Test]
        public void ForDeath_Boss_PicksBossDeath()
        {
            Assert.AreEqual(FxKind.BossDeath, FxKindMap.ForDeath(isBoss: true));
            Assert.AreEqual(FxKind.EnemyDeath, FxKindMap.ForDeath(isBoss: false));
        }
    }
}
