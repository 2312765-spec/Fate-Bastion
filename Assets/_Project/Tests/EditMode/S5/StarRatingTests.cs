using NUnit.Framework;
using FateBastion.Game;

namespace FateBastion.Tests.EditMode.S5
{
    /// <summary>StarRating (S5): the spec thresholds 15 / 8 at 20, 15, 14, 8, 7, 1 and 0 Castle HP.</summary>
    public class StarRatingTests
    {
        [TestCase(20, 3)]
        [TestCase(15, 3)]
        [TestCase(14, 2)]
        [TestCase(8, 2)]
        [TestCase(7, 1)]
        [TestCase(1, 1)]
        [TestCase(0, 0)]
        [TestCase(-3, 0)]
        public void Calculate_CastleHP_Stars(int castleHP, int stars)
        {
            Assert.AreEqual(stars, StarRating.Calculate(castleHP, 15, 8));
        }
    }
}
