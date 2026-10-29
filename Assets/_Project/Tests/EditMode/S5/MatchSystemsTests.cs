using NUnit.Framework;
using UnityEngine;
using FateBastion.Enemies;
using FateBastion.Game;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S5
{
    /// <summary>TimeScaleState, CastleHealth, wave-end gold and level unlocks (S5, B-06).</summary>
    public class MatchSystemsTests
    {
        [Test]
        public void PauseThenResumeAtX2_StaysX2()
        {
            var time = new TimeScaleState();
            time.SetSpeed(TimeScaleState.FastSpeed);

            time.Pause();
            Assert.AreEqual(0f, time.Effective);
            Assert.AreEqual(2f, time.Speed);

            time.Resume();
            Assert.AreEqual(2f, time.Effective);
        }

        [Test]
        public void SpeedChangedWhilePaused_AppliesOnResume()
        {
            var time = new TimeScaleState();
            time.Pause();
            time.ToggleFast();

            Assert.AreEqual(0f, time.Effective);
            time.Resume();
            Assert.AreEqual(2f, time.Effective);
            Assert.AreEqual(1f, time.ToggleFast());
        }

        [Test]
        public void TimeScale_RejectsZeroSpeed_ResetGoesBackToX1()
        {
            var time = new TimeScaleState();
            Assert.Throws<System.ArgumentOutOfRangeException>(() => time.SetSpeed(0f));

            time.SetSpeed(5f);
            time.Pause();
            time.Reset();

            Assert.IsFalse(time.Paused);
            Assert.AreEqual(1f, time.Effective);
        }

        [Test]
        public void Castle_DamageToZero_DestroyedOnce()
        {
            var castle = new CastleHealth();
            castle.Reset(20);

            Assert.IsFalse(castle.TakeDamage(5, out bool changed));
            Assert.IsTrue(changed);
            Assert.AreEqual(15, castle.Current);

            Assert.IsTrue(castle.TakeDamage(99, out _));
            Assert.AreEqual(0, castle.Current);
            Assert.IsFalse(castle.IsAlive);

            Assert.IsFalse(castle.TakeDamage(1, out changed), "A destroyed Castle is not destroyed again.");
            Assert.IsFalse(changed);
        }

        [Test]
        public void Castle_Invincible_IgnoresDamage()
        {
            var castle = new CastleHealth { Invincible = true };
            castle.Reset(20);

            Assert.IsFalse(castle.TakeDamage(50, out bool changed));
            Assert.IsFalse(changed);
            Assert.AreEqual(20, castle.Current);
        }

        [TestCase(1, 60)]
        [TestCase(3, 80)]
        [TestCase(15, 200)]
        [TestCase(0, 0)]
        public void WaveClearGold_50Plus10n(int wave, int gold)
        {
            Assert.AreEqual(gold, MatchEconomy.WaveClearGold(50, 10, wave));
        }

        [Test]
        public void WaveClearGold_FromSettings()
        {
            var settings = ScriptableObject.CreateInstance<MatchEconomySettings>();
            Assert.AreEqual(70, MatchEconomy.WaveClearGold(settings, 2));
            Assert.AreEqual(50, settings.SkipGold);
            Object.DestroyImmediate(settings);
        }

        [Test]
        public void Levels_UnlockAfterAStarOnThePreviousOne()
        {
            int[] stars = { 2, 0, 0 };
            Assert.IsTrue(LevelProgress.IsLevelUnlocked(0, stars));
            Assert.IsTrue(LevelProgress.IsLevelUnlocked(1, stars));
            Assert.IsFalse(LevelProgress.IsLevelUnlocked(2, stars));
            Assert.IsFalse(LevelProgress.IsLevelUnlocked(-1, stars));
            Assert.IsTrue(LevelProgress.IsLevelUnlocked(0, new int[0]));
        }

        [Test]
        public void Endless_UnlocksAfterLevel1()
        {
            Assert.IsFalse(LevelProgress.IsEndlessUnlocked(1, new[] { 0, 0, 0 }));
            Assert.IsTrue(LevelProgress.IsEndlessUnlocked(1, new[] { 1, 0, 0 }));
            Assert.IsTrue(LevelProgress.IsEndlessUnlocked(0, new int[0]));
        }

        [Test]
        public void StarsOf_ReadsTheSaveKeyMatchRewardsWrites()
        {
            var level = ScriptableObject.CreateInstance<LevelData>();
            level.name = "level_2";
            level.firstClearGems = 300;
            level.gemsPerNewStar = 50;
            var data = new SaveData();

            Assert.AreEqual(0, LevelProgress.StarsOf(data, level));
            MatchRewards.ApplyCampaign(data, level, 2);
            Assert.AreEqual(2, LevelProgress.StarsOf(data, level));

            Object.DestroyImmediate(level);
        }
    }
}
