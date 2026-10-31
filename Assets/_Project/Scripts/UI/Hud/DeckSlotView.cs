using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Heroes;

namespace FateBastion.UI
{
    /// <summary>
    /// One DeckSlot of C's HUD (S10, B-14): icon, name, price, placed/max, rarity frame, selected frame while previewing,
    /// faded when the slot cannot place now (gold, per-hero cap, army cap).
    /// </summary>
    public sealed class DeckSlotView
    {
        private const float DisabledAlpha = 0.45f;

        private readonly Image _icon;
        private readonly TMP_Text _name;
        private readonly TMP_Text _price;
        private readonly TMP_Text _count;
        private readonly Image _rarityFrame;
        private readonly Transform _selected;
        private readonly CanvasGroup _group;
        private bool _available = true;

        public DeckSlotView(Transform root)
        {
            Root = root;
            Button = root.GetComponent<Button>();
            _icon = UiFind.Optional<Image>(root, "Icon");
            _name = UiFind.Optional<TMP_Text>(root, "NameText");
            _price = UiFind.Optional<TMP_Text>(root, "PriceText");
            _count = UiFind.Optional<TMP_Text>(root, "CountText");
            _rarityFrame = UiFind.Optional<Image>(root, "RarityFrame");
            _selected = root.Find("SelectedFrame");
            _group = root.GetComponent<CanvasGroup>();
            if (_group == null) _group = root.gameObject.AddComponent<CanvasGroup>();
        }

        public Transform Root { get; }
        public Button Button { get; }

        /// <summary>Static part, set once when the match starts.</summary>
        public void SetHero(HeroData hero, int placeCost)
        {
            Root.gameObject.SetActive(hero != null);
            if (hero == null) return;

            if (_name != null) _name.text = string.IsNullOrEmpty(hero.displayName) ? hero.id : hero.displayName;
            if (_price != null) _price.text = UiText.Number(placeCost);
            if (_rarityFrame != null) _rarityFrame.color = UiText.RarityColor(hero.rarity);
            if (_icon != null && hero.icon != null)
            {
                _icon.sprite = hero.icon;
                _icon.color = Color.white;
            }
        }

        public void SetCount(int placed, int max)
        {
            if (_count != null) _count.text = $"{placed}/{max}";
        }

        public void SetAvailable(bool available)
        {
            if (_available == available) return;
            _available = available;
            _group.alpha = available ? 1f : DisabledAlpha;
        }

        public void SetSelected(bool selected) => UiFind.SetActive(_selected, selected);
    }
}
