using NUnit.Framework;
using FateBastion.Enemies;

namespace FateBastion.Tests.EditMode.S2
{
    /// <summary>Spawn HP = baseHP x hpGrowth^(wave-1) x hpMultiplier, for waves 1/5/15 of all three levels (S2).</summary>
    public class EnemyHealthFormulaTests
    {
        private const float BaseHP = 40f;      // enemy_thuong
        private const float HpGrowth = 1.12f;
        private const float Tolerance = 1e-3f;

        [TestCase(1, 1.0f, 40.0000f)]
        [TestCase(5, 1.0f, 62.9408f)]
        [TestCase(15, 1.0f, 195.4845f)]
        [TestCase(1, 1.15f, 46.0000f)]
        [TestCase(5, 1.15f, 72.3819f)]
        [TestCase(15, 1.15f, 224.8072f)]
        [TestCase(1, 1.3f, 52.0000f)]
        [TestCase(5, 1.3f, 81.8230f)]
        [TestCase(15, 1.3f, 254.1298f)]
        public void Calculate_NormalEnemy_MatchesFormula(int wave, float hpMultiplier, float expected)
        {
            float hp = EnemyHealthFormula.Calculate(BaseHP, HpGrowth, wave, hpMultiplier);
            Assert.AreEqual(expected, hp, Tolerance);
        }

        [Test]
        public void Calculate_Wave1_IgnoresGrowth()
        {
            Assert.AreEqual(600f, EnemyHealthFormula.Calculate(600f, 5f, 1, 1f), Tolerance);
        }

        [Test]
        public void Calculate_WaveBelow1_TreatedAsWave1()
        {
            Assert.AreEqual(40f, EnemyHealthFormula.Calculate(BaseHP, HpGrowth, 0, 1f), Tolerance);
        }

        [Test]
        public void Calculate_EndlessWave100_StaysFinite()
        {
            float hp = EnemyHealthFormula.Calculate(600f, HpGrowth, 100, 1.3f);
            Assert.IsFalse(float.IsInfinity(hp) || float.IsNaN(hp));
            Assert.Greater(hp, 600f);
        }
    }
}
