using FateBastion.Enemies;

namespace FateBastion.Game
{
    /// <summary>
    /// Stars of a finished level from the Castle HP left (S5): 3 stars at star3CastleHP or more, 2 at star2CastleHP
    /// or more, 1 while the Castle survives, 0 when it fell.
    /// </summary>
    public static class StarRating
    {
        public const int MaxStars = 3;

        public static int Calculate(int castleHPLeft, int star3CastleHP, int star2CastleHP)
        {
            if (castleHPLeft <= 0) return 0;
            if (castleHPLeft >= star3CastleHP) return 3;
            if (castleHPLeft >= star2CastleHP) return 2;
            return 1;
        }

        public static int Calculate(int castleHPLeft, LevelData level) =>
            Calculate(castleHPLeft, level.star3CastleHP, level.star2CastleHP);
    }
}
