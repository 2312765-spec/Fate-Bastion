using NUnit.Framework;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.UI.World
{
    /// <summary>Picks the enemy nearest the Castle among those past the warning threshold (C-14, S10).</summary>
    public class ClosestToCastleSelectorTests
    {
        private const float Threshold = 0.8f;

        [Test]
        public void Select_NoCandidates_ReturnsMinusOne()
        {
            var selector = new ClosestToCastleSelector(Threshold);
            selector.Begin();

            Assert.AreEqual(-1, selector.BestIndex);
        }

        [Test]
        public void Select_AllBelowThreshold_ReturnsMinusOne()
        {
            var selector = new ClosestToCastleSelector(Threshold);
            selector.Begin();
            selector.Consider(0, 0.1f);
            selector.Consider(1, 0.79f);

            Assert.AreEqual(-1, selector.BestIndex);
        }

        [Test]
        public void Select_ExactlyThreshold_IsIncluded()
        {
            var selector = new ClosestToCastleSelector(Threshold);
            selector.Begin();
            selector.Consider(3, Threshold);

            Assert.AreEqual(3, selector.BestIndex);
        }

        [Test]
        public void Select_SeveralPastThreshold_PicksHighestProgress()
        {
            var selector = new ClosestToCastleSelector(Threshold);
            selector.Begin();
            selector.Consider(0, 0.85f);
            selector.Consider(1, 0.95f);
            selector.Consider(2, 0.9f);

            Assert.AreEqual(1, selector.BestIndex);
            Assert.AreEqual(0.95f, selector.BestProgress, 1e-6f);
        }

        [Test]
        public void Begin_AfterPreviousScan_ClearsResult()
        {
            var selector = new ClosestToCastleSelector(Threshold);
            selector.Begin();
            selector.Consider(0, 0.9f);

            selector.Begin();

            Assert.AreEqual(-1, selector.BestIndex);
        }
    }
}
