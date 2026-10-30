using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Damage number colour by element, meteor (True damage) bigger (S11).</summary>
    public class DamageNumberStyleTests
    {
        private WorldUiSettings _settings;

        [SetUp]
        public void SetUp() => _settings = ScriptableObject.CreateInstance<WorldUiSettings>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_settings);

        [Test]
        public void GetColor_Ice_UsesIceColor() =>
            Assert.AreEqual(_settings.GetElementColor(Element.Ice), DamageNumberStyle.GetColor(DamageType.Magic, Element.Ice, _settings));

        [Test]
        public void GetColor_ElementsDiffer()
        {
            Color fire = _settings.GetElementColor(Element.Fire);
            Assert.AreNotEqual(fire, _settings.GetElementColor(Element.Ice));
            Assert.AreNotEqual(fire, _settings.GetElementColor(Element.Lightning));
        }

        [Test]
        public void Style_TrueDamage_UsesMeteorSizeAndColor()
        {
            Assert.AreEqual(_settings.MeteorColor, DamageNumberStyle.GetColor(DamageType.True, Element.Fire, _settings));
            Assert.AreEqual(_settings.MeteorFontSize / _settings.DamageNumberFontSize,
                DamageNumberStyle.GetScale(DamageType.True, _settings), 1e-4f);
        }

        [Test]
        public void GetScale_Physical_IsOne() =>
            Assert.AreEqual(1f, DamageNumberStyle.GetScale(DamageType.Physical, _settings));

        [Test]
        public void Defaults_MeteorBiggerThanNormal() =>
            Assert.Greater(_settings.MeteorFontSize, _settings.DamageNumberFontSize);
    }
}
