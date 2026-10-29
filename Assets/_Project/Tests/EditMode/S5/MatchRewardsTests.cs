using NUnit.Framework;
using UnityEngine;
using FateBastion.Game;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S5
{
    /// <summary>MatchRewards (S5, B-07): first clear, only new stars on replay, Endless gems and record.</summary>
    public class MatchRewardsTests
    {
        private const string Level = "level_1";

        private static int Apply(SaveData data, int stars) => MatchRewards.ApplyCampaign(data, Level, stars, 300, 50);

        [Test]
        public void ApplyCampaign_FirstClearTwoStars_Gets400AndSavesStars()
        {
            var data = new SaveData();

            Assert.AreEqual(400, Apply(data, 2));
            Assert.AreEqual(400, data.gems);
            Assert.AreEqual(2, data.levelProgress[Level]);
        }

        [Test]
        public void ApplyCampaign_ReplayWithThreeStarsAfterTwo_OnlyPlus50()
        {
            var data = new SaveData();
            Apply(data, 2);

            Assert.AreEqual(50, Apply(data, 3));
            Assert.AreEqual(450, data.gems);
            Assert.AreEqual(3, data.levelProgress[Level]);
        }

        [Test]
        public void ApplyCampaign_ReplayWithFewerStars_NothingAndBestKept()
        {
            var data = new SaveData();
            Apply(data, 3);

            Assert.AreEqual(0, Apply(data, 1));
            Assert.AreEqual(3, data.levelProgress[Level]);
        }

        [Test]
        public void ApplyCampaign_Lost_NoGemsNoProgress()
        {
            var data = new SaveData();

            Assert.AreEqual(0, Apply(data, 0));
            Assert.AreEqual(0, data.gems);
            Assert.IsFalse(data.levelProgress.ContainsKey(Level));
        }

        [TestCase(0, 0)]
        [TestCase(4, 0)]
        [TestCase(5, 10)]
        [TestCase(14, 20)]
        [TestCase(23, 40)]
        public void EndlessGems_TenPerFiveWaves(int waves, int gems)
        {
            Assert.AreEqual(gems, MatchRewards.EndlessGems(waves, 10, 5));
        }

        [Test]
        public void ApplyEndless_NewAndOldRecord_BestKept()
        {
            var settings = ScriptableObject.CreateInstance<EndlessRewardSettings>();
            settings.Configure(10, 5);
            try
            {
                var data = new SaveData();
                Assert.AreEqual(40, MatchRewards.ApplyEndless(data, 23, settings));
                Assert.AreEqual(23, data.endlessBest);

                Assert.AreEqual(20, MatchRewards.ApplyEndless(data, 12, settings));
                Assert.AreEqual(23, data.endlessBest, "A shorter run keeps the record.");
                Assert.AreEqual(60, data.gems);
            }
            finally
            {
                Object.DestroyImmediate(settings);
            }
        }
    }
}
