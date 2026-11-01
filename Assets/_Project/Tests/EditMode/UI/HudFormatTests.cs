using System.Text;
using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>HUD texts (B-14): wave line per state, countdown rounding, reasons, labels.</summary>
    public class HudFormatTests
    {
        [TestCase(18.2f, "0:19")]
        [TestCase(15f, "0:15")]
        [TestCase(0.01f, "0:01")]
        [TestCase(0f, "0:00")]
        [TestCase(-3f, "0:00")]
        [TestCase(75f, "1:15")]
        public void Clock_RoundsUp(float seconds, string text)
        {
            Assert.AreEqual(text, HudFormat.Clock(seconds));
        }

        [Test]
        public void Wave_PerState()
        {
            Assert.AreEqual("Chuẩn bị · 0:15", HudFormat.Wave(GameState.Preparing, 0, 15, false, 15f));
            Assert.AreEqual("Wave 3/15", HudFormat.Wave(GameState.InWave, 3, 15, false, 0f));
            Assert.AreEqual("Wave 4/15 · 0:08", HudFormat.Wave(GameState.Resting, 3, 15, false, 7.5f));
            Assert.AreEqual("Wave 23", HudFormat.Wave(GameState.InWave, 23, 15, true, 0f));
            Assert.AreEqual("Wave 15/15", HudFormat.Wave(GameState.Won, 15, 15, false, 0f));
        }

        [Test]
        public void Labels()
        {
            Assert.AreEqual("Thành 17/20", HudFormat.Castle(17, 20));
            Assert.AreEqual("Vàng 1.250", HudFormat.Gold(1250));
            Assert.AreEqual("Quân số 12/15", HudFormat.UnitCount(12, 15));
            Assert.AreEqual("Tốc ×1", HudFormat.Speed(1f));
            Assert.AreEqual("Tốc ×2", HudFormat.Speed(2f));
            Assert.AreEqual("Nâng (U) 525", HudFormat.Upgrade(525));
            Assert.AreEqual("Cấp tối đa", HudFormat.Upgrade(-1));
            Assert.AreEqual("Bán (X) 306", HudFormat.Sell(306));
            Assert.AreEqual("Nộ 60/100", HudFormat.Rage(60.4f, 100f));
            Assert.AreEqual("Sẵn sàng", HudFormat.Cooldown(0f));
            Assert.AreEqual("0:27", HudFormat.Cooldown(26.3f));
            Assert.AreEqual("Lửa", HudFormat.ElementName(Element.Fire));
            Assert.AreEqual("Hỗ trợ", HudFormat.RoleName(Role.Support));
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(999)]
        [TestCase(1000)]
        [TestCase(1250)]
        [TestCase(105000)]
        [TestCase(1000000)]
        public void AppendGold_AnyAmount_MatchesGold(int gold)
        {
            Assert.AreEqual(HudFormat.Gold(gold), HudFormat.AppendGold(new StringBuilder(), gold).ToString());
        }

        [Test]
        public void EveryPlacementError_HasAReason()
        {
            foreach (PlacementError error in System.Enum.GetValues(typeof(PlacementError)))
            {
                string text = HudFormat.Placement(error);
                if (error == PlacementError.None) Assert.IsEmpty(text);
                else Assert.IsFalse(string.IsNullOrEmpty(text) || text == error.ToString(), error.ToString());
            }
        }
    }
}
