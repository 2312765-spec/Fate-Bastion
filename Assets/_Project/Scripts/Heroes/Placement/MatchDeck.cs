using System;
using System.Collections.Generic;

namespace FateBastion.Heroes
{
    /// <summary>One of the five deck slots of a match (S4 "DeckSlot { heroData, placedCount }").</summary>
    public sealed class DeckSlot
    {
        public DeckSlot(HeroData heroData)
        {
            HeroData = heroData;
        }

        /// <summary>Null for an empty slot (sandbox decks may have fewer than five heroes).</summary>
        public HeroData HeroData { get; }

        /// <summary>Copies of this hero on the field now.</summary>
        public int PlacedCount { get; internal set; }

        public bool IsEmpty => HeroData == null;
    }

    /// <summary>
    /// The deck of one match (S4): five slots with their placed counts and the army size. Pure, so the caps of
    /// PlacementValidator are tested without physics. The HUD (B-14) listens to <see cref="SlotChanged"/>.
    /// </summary>
    public sealed class MatchDeck
    {
        public const int Size = 5;

        private readonly DeckSlot[] _slots;

        public MatchDeck(IReadOnlyList<HeroData> heroes)
        {
            _slots = new DeckSlot[Size];
            for (int i = 0; i < Size; i++)
            {
                HeroData data = heroes != null && i < heroes.Count ? heroes[i] : null;
                _slots[i] = new DeckSlot(data);
            }
        }

        /// <summary>A slot's placed count changed (slot index).</summary>
        public event Action<int> SlotChanged;

        /// <summary>The army size changed (new total).</summary>
        public event Action<int> TotalChanged;

        public int Count => _slots.Length;

        /// <summary>Heroes of every slot on the field.</summary>
        public int TotalPlaced { get; private set; }

        public DeckSlot this[int slot] => _slots[slot];

        public bool IsValidSlot(int slot) => slot >= 0 && slot < _slots.Length && !_slots[slot].IsEmpty;

        /// <summary>The distinct heroes of the deck (empty slots skipped), e.g. to prewarm the pools.</summary>
        public List<HeroData> DistinctHeroes()
        {
            var heroes = new List<HeroData>(Size);
            foreach (DeckSlot slot in _slots)
            {
                if (!slot.IsEmpty && !heroes.Contains(slot.HeroData)) heroes.Add(slot.HeroData);
            }

            return heroes;
        }

        /// <summary>Gold, per-hero cap and army cap for <paramref name="slot"/> (S4 checks 1–3).</summary>
        public PlacementError CheckEconomy(PlacementRules rules, int slot, int gold)
        {
            if (!IsValidSlot(slot)) throw new ArgumentOutOfRangeException(nameof(slot), slot, "Empty or unknown deck slot.");
            DeckSlot deckSlot = _slots[slot];
            return PlacementValidator.ValidateEconomy(rules, gold, deckSlot.HeroData.rarity, deckSlot.PlacedCount, TotalPlaced);
        }

        public void RecordPlaced(int slot)
        {
            if (!IsValidSlot(slot)) throw new ArgumentOutOfRangeException(nameof(slot), slot, "Empty or unknown deck slot.");
            _slots[slot].PlacedCount++;
            TotalPlaced++;
            SlotChanged?.Invoke(slot);
            TotalChanged?.Invoke(TotalPlaced);
        }

        /// <summary>A hero of <paramref name="slot"/> left the field (sold).</summary>
        public void RecordRemoved(int slot)
        {
            if (!IsValidSlot(slot)) throw new ArgumentOutOfRangeException(nameof(slot), slot, "Empty or unknown deck slot.");
            if (_slots[slot].PlacedCount <= 0) throw new InvalidOperationException($"Deck slot {slot} has no hero on the field.");

            _slots[slot].PlacedCount--;
            TotalPlaced--;
            SlotChanged?.Invoke(slot);
            TotalChanged?.Invoke(TotalPlaced);
        }

        /// <summary>End of the match: every hero went back to the pool.</summary>
        public void Reset()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i].PlacedCount == 0) continue;
                _slots[i].PlacedCount = 0;
                SlotChanged?.Invoke(i);
            }

            if (TotalPlaced == 0) return;
            TotalPlaced = 0;
            TotalChanged?.Invoke(0);
        }
    }
}
