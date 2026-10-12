using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Combat;

namespace FateBastion.Tests.EditMode.S1
{
    /// <summary>Tests for the single damage formula: D x k x (1 - resist), resist clamped [0; 0.8], floor, min 1 (S1).</summary>
    public class DamageCalculatorTests
    {
        // --- Resistance 0% / 50% / 80% and clamping ---

        [Test]
        public void Calculate_Physical_Armor0_ReturnsFullDamage()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, 0f, 0f));
        }

        [Test]
        public void Calculate_Physical_Armor50_ReturnsHalf()
        {
            Assert.AreEqual(50, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, 0.5f, 0f));
        }

        [Test]
        public void Calculate_Physical_Armor80_Returns20()
        {
            Assert.AreEqual(20, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, 0.8f, 0f));
        }

        [Test]
        public void Calculate_ArmorAbove80_ClampedTo80()
        {
            Assert.AreEqual(20, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, 0.95f, 0f));
        }

        [Test]
        public void Calculate_NegativeArmor_ClampedTo0()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, -0.5f, 0f));
        }

        [Test]
        public void Calculate_Physical_IgnoresMagicResist()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(100f, 1f, DamageType.Physical, 0f, 0.8f));
        }

        [Test]
        public void Calculate_Magic_UsesMagicResistNotArmor()
        {
            Assert.AreEqual(50, DamageCalculator.Calculate(100f, 1f, DamageType.Magic, 0.8f, 0.5f));
        }

        [Test]
        public void Calculate_MagicResistAbove80_ClampedTo80()
        {
            Assert.AreEqual(20, DamageCalculator.Calculate(100f, 1f, DamageType.Magic, 0f, 1f));
        }

        // --- True damage ---

        [Test]
        public void Calculate_True_IgnoresResistance()
        {
            Assert.AreEqual(100, DamageCalculator.Calculate(100f, 1f, DamageType.True, 0.8f, 0.8f));
        }

        [TestCase(DamageType.Physical, 0.3f, 0.6f, 0.3f)]
        [TestCase(DamageType.Magic, 0.3f, 0.6f, 0.6f)]
        [TestCase(DamageType.True, 0.3f, 0.6f, 0f)]
        [TestCase(DamageType.Physical, 2f, 0f, 0.8f)]
        [TestCase(DamageType.Magic, 0f, -1f, 0f)]
        public void GetResistance_ByType_ReturnsClampedResistance(DamageType type, float armor, float magicResist,
            float expected)
        {
            Assert.AreEqual(expected, DamageCalculator.GetResistance(type, armor, magicResist), 1e-6f);
        }

        // --- Skill multiplier ---

        [Test]
        public void Calculate_SkillMultiplier_Applied()
        {
            // 100 x 1.5 x (1 - 0.2) = 120
            Assert.AreEqual(120, DamageCalculator.Calculate(100f, 1.5f, DamageType.Physical, 0.2f, 0f));
        }

        [Test]
        public void Calculate_SkillMultiplierBelowOne_Applied()
        {
            // 100 x 0.5 x (1 - 0) = 50
            Assert.AreEqual(50, DamageCalculator.Calculate(100f, 0.5f, DamageType.Magic, 0f, 0f));
        }

        // --- Rounding and minimum ---

        [Test]
        public void Calculate_FractionalResult_RoundsDown()
        {
            // 33 x 0.5 = 16.5 -> 16
            Assert.AreEqual(16, DamageCalculator.Calculate(33f, 1f, DamageType.Physical, 0.5f, 0f));
        }

        [Test]
        public void Calculate_FractionJustBelowNextInteger_RoundsDown()
        {
            // 19.9 is a real fraction, not float error: must stay 19
            Assert.AreEqual(19, DamageCalculator.Calculate(19.9f, 1f, DamageType.True, 0f, 0f));
        }

        [Test]
        public void Calculate_TinyDamage_MinimumOne()
        {
            // 1 x (1 - 0.8) = 0.2 -> 1
            Assert.AreEqual(1, DamageCalculator.Calculate(1f, 1f, DamageType.Physical, 0.8f, 0f));
        }

        [Test]
        public void Calculate_ZeroDamage_MinimumOne()
        {
            // ASSUMPTION: minimum 1 applies to every call; sources that deal no damage do not call the calculator.
            Assert.AreEqual(1, DamageCalculator.Calculate(0f, 1f, DamageType.True, 0f, 0f));
        }

        // Products that are whole numbers on paper but can land a hair below in float (e.g. 1 - 0.7f = 0.29999998).
        [TestCase(100f, 1f, 0.7f, 30)]
        [TestCase(10f, 2.5f, 0f, 25)]
        [TestCase(14f, 2.5f, 0f, 35)]
        [TestCase(10f, 1.4f, 0f, 14)]
        [TestCase(25f, 1.4f, 0.2f, 28)]
        [TestCase(40f, 1.1f, 0.5f, 22)]
        [TestCase(1000f, 1f, 0.3f, 700)]
        [TestCase(5000f, 1f, 0.7f, 1500)]
        [TestCase(3000f, 1.1f, 0.3f, 2310)]
        public void Calculate_WholeNumberOnPaper_NoFloatUndershoot(float damage, float skillMultiplier, float armor,
            int expected)
        {
            Assert.AreEqual(expected, DamageCalculator.Calculate(damage, skillMultiplier, DamageType.Physical, armor, 0f));
        }

        // --- DamageInfo overload ---

        [Test]
        public void Calculate_DamageInfoOverload_UsesAmountAsAlreadyMultiplied()
        {
            // Amount = D x k already (S6 multiplies before building DamageInfo).
            var info = new DamageInfo(150f, DamageType.Physical, Element.Fire, null, true, null, Vector3.zero);
            Assert.AreEqual(75, DamageCalculator.Calculate(info, 0.5f, 0f));
        }

        [TestCase(DamageType.Physical)]
        [TestCase(DamageType.Magic)]
        [TestCase(DamageType.True)]
        public void Calculate_DamageInfoOverload_MatchesFloatOverload(DamageType type)
        {
            var info = new DamageInfo(87f, type, Element.Ice, null, false, null, Vector3.one);
            int expected = DamageCalculator.Calculate(87f, 1f, type, 0.35f, 0.6f);
            Assert.AreEqual(expected, DamageCalculator.Calculate(info, 0.35f, 0.6f));
        }
    }
}
