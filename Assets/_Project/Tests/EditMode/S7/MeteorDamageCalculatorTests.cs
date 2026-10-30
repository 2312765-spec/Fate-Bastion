using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Player;
using FateBastion.Skills;
using NUnit.Framework;
using UnityEngine;

namespace FateBastion.Tests.EditMode.S7
{
    /// <summary>Checks meteor True damage against the S7 wave and level formula.</summary>
    public class MeteorDamageCalculatorTests
    {
        private EnemyData _normalEnemy;
        private LevelData _level;
        private SkillData _skill;

        [SetUp]
        public void SetUp()
        {
            _normalEnemy = ScriptableObject.CreateInstance<EnemyData>();
            _normalEnemy.baseHP = 40f;
            _level = ScriptableObject.CreateInstance<LevelData>();
            _level.hpGrowth = 1.12f;
            _level.hpMultiplier = 1.15f;
            _skill = ScriptableObject.CreateInstance<SkillData>();
            _skill.kind = SkillKind.AreaAtPoint;
            _skill.aimMode = AimMode.GroundPoint;
            _skill.damageType = DamageType.True;
            _skill.damageMultiplier = 3f;
            _skill.radius = 4f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_normalEnemy);
            Object.DestroyImmediate(_level);
            Object.DestroyImmediate(_skill);
        }

        [Test]
        public void Calculate_Wave1_MatchesBaseNormalHpTimesMeteorMultiplier()
        {
            Assert.AreEqual(138f, MeteorDamageCalculator.Calculate(_normalEnemy, _level, _skill, 1), 0.001f);
        }

        [Test]
        public void Calculate_Wave5_AppliesFourGrowthStepsAndLevelMultiplier()
        {
            Assert.AreEqual(217.14567f, MeteorDamageCalculator.Calculate(_normalEnemy, _level, _skill, 5), 0.001f);
        }

        [Test]
        public void Calculate_Wave15_AppliesFourteenGrowthSteps()
        {
            _level.hpMultiplier = 1.3f;

            Assert.AreEqual(762.38952f, MeteorDamageCalculator.Calculate(_normalEnemy, _level, _skill, 15), 0.001f);
        }
    }
}
