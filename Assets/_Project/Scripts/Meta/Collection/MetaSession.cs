using System;
using System.Collections.Generic;
using FateBastion.Core;

namespace FateBastion.Meta
{
    /// <summary>
    /// Out-of-match commands for the lobby UI (S8): every successful pull, merge or deck change is saved right
    /// away through SaveService (S9), so quitting never loses gems without the hero. The UI calls these only when
    /// the player presses a button and refreshes from <see cref="Changed"/>.
    /// </summary>
    public sealed class MetaSession
    {
        private readonly SaveService _save;
        private readonly GachaService _gacha;
        private readonly CollectionService _collection;
        private readonly DeckService _deck;

        public MetaSession(SaveService save, GachaService gacha, CollectionService collection, DeckService deck)
        {
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _gacha = gacha ?? throw new ArgumentNullException(nameof(gacha));
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _deck = deck ?? throw new ArgumentNullException(nameof(deck));
        }

        /// <summary>Raised after a change was applied and saved (gems, bag or deck changed).</summary>
        public event Action<SaveData> Changed;

        /// <summary>The loaded save; SaveService.Load() must have been called.</summary>
        public SaveData Data => _save.Data ?? throw new InvalidOperationException("SaveService chưa có dữ liệu; gọi Load() trước.");

        public GachaService Gacha => _gacha;
        public CollectionService Collection => _collection;
        public DeckService Deck => _deck;

        public GachaPullResult PullOne() => Commit(_gacha.PullOne(Data), r => r.Success);

        public GachaPullResult PullTen() => Commit(_gacha.PullTen(Data), r => r.Success);

        public MergeResult Merge(Rarity rarity, IReadOnlyList<string> selection) =>
            Commit(_collection.Merge(Data, rarity, selection), r => r.Success);

        public MergeResult MergeAuto(Rarity rarity) => Commit(_collection.MergeAuto(Data, rarity), r => r.Success);

        public DeckError SetDeck(IReadOnlyList<string> heroIds) =>
            Commit(_deck.TrySetDeck(Data, heroIds), e => e == DeckError.None);

        public DeckError SetDeckSlot(int slot, string heroId) =>
            Commit(_deck.TrySetSlot(Data, slot, heroId), e => e == DeckError.None);

        private T Commit<T>(T result, Func<T, bool> succeeded)
        {
            if (succeeded(result))
            {
                _save.Save();
                Changed?.Invoke(_save.Data);
            }

            return result;
        }
    }
}
