using NUnit.Framework;
using UnityEngine;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Buff icons above a hero: which icons show and where they sit (C-09).</summary>
    public class HeroBuffIndicatorTests
    {
        private const float Spacing = 0.45f;

        private GameObject _root;
        private GameObject _damage;
        private GameObject _attackSpeed;
        private HeroBuffIndicator _indicator;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("BuffIcons");
            _damage = new GameObject("FX_Buff_Damage");
            _attackSpeed = new GameObject("FX_Buff_AttackSpeed");
            _damage.transform.SetParent(_root.transform);
            _attackSpeed.transform.SetParent(_root.transform);
            _indicator = _root.AddComponent<HeroBuffIndicator>();
            _indicator.Configure(_damage, _attackSpeed, Spacing);
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_root);

        [Test]
        public void Arrange_NoBuff_NoIcon()
        {
            Assert.AreEqual(0, BuffIconLayout.Arrange(false, false, Spacing, out _, out _));
        }

        [Test]
        public void Arrange_OneBuff_Centred()
        {
            Assert.AreEqual(1, BuffIconLayout.Arrange(false, true, Spacing, out _, out float attackSpeedX));
            Assert.AreEqual(0f, attackSpeedX);
        }

        [Test]
        public void Arrange_BothBuffs_SideBySide()
        {
            Assert.AreEqual(2, BuffIconLayout.Arrange(true, true, Spacing, out float damageX, out float attackSpeedX));
            Assert.AreEqual(-Spacing * 0.5f, damageX, 1e-5f);
            Assert.AreEqual(Spacing * 0.5f, attackSpeedX, 1e-5f);
        }

        [Test]
        public void Refresh_DamageOnly_ShowsDamageCentred()
        {
            _indicator.Refresh(true, false);

            Assert.IsTrue(_indicator.DamageShown);
            Assert.IsFalse(_indicator.AttackSpeedShown);
            Assert.AreEqual(Vector3.zero, _damage.transform.localPosition);
        }

        [Test]
        public void Refresh_Both_ShowsBothApart()
        {
            _indicator.Refresh(true, true);

            Assert.IsTrue(_indicator.DamageShown && _indicator.AttackSpeedShown);
            Assert.Less(_damage.transform.localPosition.x, _attackSpeed.transform.localPosition.x);
        }

        [Test]
        public void Refresh_BuffEnds_HidesIcon()
        {
            _indicator.Refresh(true, true);

            _indicator.Refresh(false, true);

            Assert.IsFalse(_indicator.DamageShown);
            Assert.IsTrue(_indicator.AttackSpeedShown);
            Assert.AreEqual(0f, _attackSpeed.transform.localPosition.x, "Còn một ký hiệu thì phải về giữa.");
        }
    }
}
