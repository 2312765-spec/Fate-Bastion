using System;
using System.Collections.Generic;

namespace FateBastion.Meta
{
    /// <summary>
    /// Battle deck rules (S8): exactly DeckSize different heroes, each with at least one copy. Checked before every
    /// match and before every change, so the saved deck is always valid.
    /// </summary>
    public sealed class DeckService
    {
        /// <summary>Same constant as SaveSanitizer (S9) so both sides agree on the deck size.</summary>
        public const int DeckSize = SaveSanitizer.DeckSize;

        public DeckError Validate(SaveData data)
        {
            return data == null ? DeckError.WrongSize : Validate(data.deck, data);
        }

        /// <summary>Validates <paramref name="heroIds"/> as a deck for the heroes owned in <paramref name="data"/>.</summary>
        public DeckError Validate(IReadOnlyList<string> heroIds, SaveData data)
        {
            if (heroIds == null || heroIds.Count != DeckSize)
            {
                return DeckError.WrongSize;
            }

            var seen = new HashSet<string>();
            for (int i = 0; i < heroIds.Count; i++)
            {
                string heroId = heroIds[i];
                if (SaveDataGuard.CopiesOf(data, heroId) < 1)
                {
                    return DeckError.NotOwned;
                }

                if (!seen.Add(heroId))
                {
                    return DeckError.Duplicate;
                }
            }

            return DeckError.None;
        }

        /// <summary>"Vào trận" is allowed only with a valid deck.</summary>
        public bool CanStartMatch(SaveData data) => Validate(data) == DeckError.None;

        /// <summary>Replaces the whole deck when <paramref name="heroIds"/> is valid; otherwise changes nothing.</summary>
        public DeckError TrySetDeck(SaveData data, IReadOnlyList<string> heroIds)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            DeckError error = Validate(heroIds, data);
            if (error != DeckError.None)
            {
                return error;
            }

            SaveDataGuard.EnsureCollections(data);
            data.deck.Clear();
            for (int i = 0; i < heroIds.Count; i++)
            {
                data.deck.Add(heroIds[i]);
            }

            return DeckError.None;
        }

        /// <summary>
        /// Puts <paramref name="heroId"/> in <paramref name="slot"/> (drag from the bag). A hero already in another
        /// slot swaps places, so the deck stays 5 different heroes.
        /// </summary>
        public DeckError TrySetSlot(SaveData data, int slot, string heroId)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            SaveDataGuard.EnsureCollections(data);
            if (data.deck.Count != DeckSize)
            {
                return DeckError.WrongSize;
            }

            if (slot < 0 || slot >= DeckSize)
            {
                return DeckError.SlotOutOfRange;
            }

            if (SaveDataGuard.CopiesOf(data, heroId) < 1)
            {
                return DeckError.NotOwned;
            }

            int current = data.deck.IndexOf(heroId);
            if (current >= 0)
            {
                data.deck[current] = data.deck[slot];
            }

            data.deck[slot] = heroId;
            return DeckError.None;
        }
    }
}
