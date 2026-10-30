using System;
using System.Collections.Generic;

namespace FateBastion.Meta
{
    /// <summary>Outcome of a x1 or x10 pull: the rolls in order, gems spent and first-own gems received (S8).</summary>
    public sealed class GachaPullResult
    {
        private static readonly GachaRoll[] NoRolls = Array.Empty<GachaRoll>();

        private GachaPullResult(GachaError error, IReadOnlyList<GachaRoll> rolls, int gemsSpent, int gemsAwarded, int cost)
        {
            Error = error;
            Rolls = rolls;
            GemsSpent = gemsSpent;
            GemsAwarded = gemsAwarded;
            Cost = cost;
        }

        public bool Success => Error == GachaError.None;
        public GachaError Error { get; }
        public IReadOnlyList<GachaRoll> Rolls { get; }
        public int GemsSpent { get; }

        /// <summary>First-own bonus added after the price was paid.</summary>
        public int GemsAwarded { get; }

        /// <summary>Price of the pull that was asked, also when it was refused.</summary>
        public int Cost { get; }

        public static GachaPullResult Succeeded(IReadOnlyList<GachaRoll> rolls, int cost, int gemsAwarded) =>
            new GachaPullResult(GachaError.None, rolls, cost, gemsAwarded, cost);

        public static GachaPullResult Failed(GachaError error, int cost) =>
            new GachaPullResult(error, NoRolls, 0, 0, cost);
    }
}
