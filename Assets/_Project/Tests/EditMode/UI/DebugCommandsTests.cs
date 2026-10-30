using NUnit.Framework;
using FateBastion.Meta;
using FateBastion.UI;
using FateBastion.UI.DebugTools;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>F1 debug panel save edits (B-16) and the shared lobby number format.</summary>
    public class DebugCommandsTests
    {
        [Test]
        public void AddGems_NeverBelowZero()
        {
            var data = new SaveData { gems = 100 };
            DebugCommands.AddGems(data, 1000);
            Assert.AreEqual(1100, data.gems);

            DebugCommands.AddGems(data, -5000);
            Assert.AreEqual(0, data.gems);
        }

        [Test]
        public void GrantHero_AddsCopyAndMarksOwned()
        {
            var data = new SaveData();
            DebugCommands.GrantHero(data, "hero_long_vuong");
            DebugCommands.GrantHero(data, "hero_long_vuong");

            Assert.AreEqual(2, data.collection["hero_long_vuong"]);
            CollectionAssert.AreEqual(new[] { "hero_long_vuong" }, data.ownedEver);
            Assert.AreEqual(0, data.gems, "A debug gift pays no first-own gems.");
        }

        [Test]
        public void GrantHero_NullCollections_AreCreated()
        {
            var data = new SaveData { collection = null, ownedEver = null };
            DebugCommands.GrantHero(data, "hero_cung_thu");
            Assert.AreEqual(1, data.collection["hero_cung_thu"]);
        }

        [TestCase(1000, "1.000 Ngọc")]
        [TestCase(0, "0 Ngọc")]
        [TestCase(1234567, "1.234.567 Ngọc")]
        public void Gems_VietnameseThousands(int gems, string text)
        {
            Assert.AreEqual(text, UiText.Gems(gems));
        }

        [TestCase(0.085f, "8.5%")]
        [TestCase(0.65f, "65%")]
        public void Percent_Rates(float ratio, string text)
        {
            Assert.AreEqual(text, UiText.Percent(ratio));
        }
    }
}
