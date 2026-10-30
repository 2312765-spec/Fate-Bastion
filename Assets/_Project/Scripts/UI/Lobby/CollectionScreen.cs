using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_Collection "Túi &amp; Deck" (S8, S10, B-15): every hero with its copies, the five deck slots, and Merge per
    /// rarity behind the shared ConfirmDialog. Deck editing by clicks: pick a slot then a hero, or a hero then a slot;
    /// a hero already in the deck swaps places (DeckService.TrySetSlot).
    /// </summary>
    public sealed class CollectionScreen : LobbyScreen
    {
        private static readonly Rarity[] MergeRarities = { Rarity.Common, Rarity.Rare, Rarity.Epic };

        private readonly List<HeroCardView> _deckCards = new List<HeroCardView>(DeckService.DeckSize);
        private readonly List<HeroCardView> _gridCards = new List<HeroCardView>(16);
        private readonly List<string> _order = new List<string>(16);
        private readonly Button[] _mergeButtons = new Button[3];
        private readonly TMP_Text[] _spareTexts = new TMP_Text[3];
        private Transform _gridContent;
        private TMP_Text _deckStatus;
        private int _selectedSlot = -1;
        private string _selectedHero;

        public override string Title => "Túi & Deck";

        protected override void OnBind()
        {
            for (int i = 0; i < DeckService.DeckSize; i++)
            {
                Transform node = UiFind.Node(transform, $"DeckBar/DeckCard_{i + 1}", this);
                if (node == null) continue;
                var card = new HeroCardView(node);
                int slot = i;
                card.OnClick(_ => ClickSlot(slot));
                _deckCards.Add(card);
            }

            _deckStatus = UiFind.Optional<TMP_Text>(transform, "DeckBar/DeckInfo/DeckStatusText");
            var hint = UiFind.Optional<TMP_Text>(transform, "DeckBar/DeckInfo/DeckHintText");
            if (hint != null) hint.text = "Chọn ô deck rồi chọn tướng trong túi (hoặc ngược lại)";

            _gridContent = UiFind.Node(transform, "HeroGrid/Viewport/Content", this);
            if (_gridContent != null)
            {
                for (int i = 0; i < _gridContent.childCount; i++) AddGridCard(_gridContent.GetChild(i));
            }

            for (int i = 0; i < MergeRarities.Length; i++)
            {
                Rarity rarity = MergeRarities[i];
                string row = $"MergePanel/MergeRow_{rarity}";
                _mergeButtons[i] = UiFind.Get<Button>(transform, row + "/MergeButton", this);
                _spareTexts[i] = UiFind.Optional<TMP_Text>(transform, row + "/SpareText");
                if (_mergeButtons[i] != null) _mergeButtons[i].onClick.AddListener(() => AskMerge(rarity));
            }
        }

        private void AddGridCard(Transform node)
        {
            var card = new HeroCardView(node);
            card.OnClick(ClickHero);
            _gridCards.Add(card);
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            _selectedSlot = -1;
            _selectedHero = null;
        }

        protected override void Refresh()
        {
            SaveData data = Context.Data;
            BuildOrder(data);
            EnsureGridCards(_order.Count);

            for (int i = 0; i < _gridCards.Count; i++)
            {
                if (i >= _order.Count)
                {
                    _gridCards[i].Hide();
                    continue;
                }

                string id = _order[i];
                int copies = CollectionService.CopiesOf(data, id);
                _gridCards[i].Set(id, Context.HeroData(id), Context.RarityOf(id), copies, data.deck != null && data.deck.Contains(id), false, id == _selectedHero);
            }

            for (int i = 0; i < _deckCards.Count; i++)
            {
                string id = data.deck != null && i < data.deck.Count ? data.deck[i] : null;
                if (id == null)
                {
                    _deckCards[i].Set(null, null, Rarity.Common, 0, false, false, i == _selectedSlot);
                    continue;
                }

                _deckCards[i].Set(id, Context.HeroData(id), Context.RarityOf(id), -1, false, false, i == _selectedSlot);
            }

            if (_deckStatus != null) _deckStatus.text = DeckStatus(Context.Session.Deck.Validate(data));

            CollectionService collection = Context.Session.Collection;
            for (int i = 0; i < MergeRarities.Length; i++)
            {
                int spare = collection.SpareCopies(data, MergeRarities[i]);
                if (_spareTexts[i] != null) _spareTexts[i].text = $"Thừa: {spare}";
                if (_mergeButtons[i] != null) _mergeButtons[i].interactable = collection.CanMerge(data, MergeRarities[i]);
            }
        }

        // Owned heroes first, then by rarity (Legendary first), then by name.
        private void BuildOrder(SaveData data)
        {
            _order.Clear();
            if (Context.Heroes != null)
            {
                foreach (HeroData hero in Context.Heroes.Heroes)
                {
                    if (hero != null && !string.IsNullOrEmpty(hero.id)) _order.Add(hero.id);
                }
            }

            // Owned ids missing from the catalog are still shown, so nothing the player owns disappears.
            if (data.collection != null)
            {
                foreach (string id in data.collection.Keys)
                {
                    if (!_order.Contains(id)) _order.Add(id);
                }
            }

            _order.Sort((a, b) =>
            {
                int owned = (CollectionService.CopiesOf(data, b) > 0).CompareTo(CollectionService.CopiesOf(data, a) > 0);
                if (owned != 0) return owned;
                int rarity = Context.RarityOf(b).CompareTo(Context.RarityOf(a));
                return rarity != 0 ? rarity : string.CompareOrdinal(Context.HeroName(a), Context.HeroName(b));
            });
        }

        // The layout ships a few sample cards; more heroes clone the first one (lobby only, never in a match).
        private void EnsureGridCards(int count)
        {
            if (_gridContent == null || _gridCards.Count == 0) return;
            while (_gridCards.Count < count)
            {
                Transform clone = Instantiate(_gridCards[0].Root, _gridContent, false);
                clone.name = $"HeroCard_{_gridCards.Count + 1}";
                AddGridCard(clone);
            }
        }

        private void ClickSlot(int slot)
        {
            if (_selectedHero != null)
            {
                Assign(slot, _selectedHero);
                return;
            }

            _selectedSlot = _selectedSlot == slot ? -1 : slot;
            Refresh();
        }

        private void ClickHero(HeroCardView card)
        {
            string id = card.HeroId;
            if (id == null) return;

            if (CollectionService.CopiesOf(Context.Data, id) < 1)
            {
                Context.Message("Chưa sở hữu", $"Bạn chưa có {Context.HeroName(id)}. Quay Gacha để nhận.");
                return;
            }

            if (_selectedSlot >= 0)
            {
                Assign(_selectedSlot, id);
                return;
            }

            _selectedHero = _selectedHero == id ? null : id;
            Refresh();
        }

        private void Assign(int slot, string heroId)
        {
            _selectedSlot = -1;
            _selectedHero = null;
            DeckError error = Context.Session.SetDeckSlot(slot, heroId); // Saves and raises Changed → Refresh.
            if (error != DeckError.None)
            {
                Context.Message("Không đổi được deck", DeckStatus(error));
                Refresh();
            }
        }

        private void AskMerge(Rarity rarity)
        {
            CollectionService collection = Context.Session.Collection;
            if (!collection.CanMerge(Context.Data, rarity)) return;

            Rarity next = rarity + 1;
            string message = $"Dùng {collection.MergeCost} bản {UiText.RarityName(rarity)} thừa để nhận 1 tướng {UiText.RarityName(next)} " +
                             "ngẫu nhiên. Mỗi tướng luôn giữ lại 1 bản.";
            if (Context.Confirm == null)
            {
                Merge(rarity);
                return;
            }

            Context.Confirm.Show("Ghép tướng?", message, _ => Merge(rarity));
        }

        private void Merge(Rarity rarity)
        {
            MergeResult result = Context.Session.MergeAuto(rarity);
            if (!result.Success)
            {
                Context.Message("Không ghép được", result.Error == MergeError.NotEnoughSpare
                    ? $"Còn thiếu {result.Missing} bản thừa."
                    : $"Lỗi: {result.Error}.");
                return;
            }

            string bonus = result.GemsAwarded > 0 ? $" Tướng mới: +{UiText.Gems(result.GemsAwarded)}." : string.Empty;
            Context.Message("Ghép thành công", $"Bạn nhận được {Context.HeroName(result.HeroId)} ({UiText.RarityName(result.Rarity)}).{bonus}");
        }

        private static string DeckStatus(DeckError error)
        {
            switch (error)
            {
                case DeckError.None: return $"{DeckService.DeckSize}/{DeckService.DeckSize} tướng khác nhau";
                case DeckError.WrongSize: return $"Deck cần đúng {DeckService.DeckSize} tướng";
                case DeckError.Duplicate: return "Deck có tướng bị trùng";
                case DeckError.NotOwned: return "Deck có tướng chưa sở hữu";
                default: return "Ô deck không hợp lệ";
            }
        }
    }
}
