using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.UI
{
    /// <summary>
    /// One HeroCard of C's layout (Icon, NameText, RarityText, RarityFrame, CopyBadge/CopyText, InDeckMark, NewBadge,
    /// SelectedFrame, root Button). Plain class: the gacha, bag and deck screens each hold a few of them.
    /// </summary>
    public sealed class HeroCardView
    {
        private static readonly Color Missing = new Color(1f, 1f, 1f, 0.35f);

        // The skin may hide SelectedFrame behind the card frame, so a selected card is also lifted a little.
        private static readonly Vector3 SelectedScale = new Vector3(1.08f, 1.08f, 1f);

        private readonly Image _icon;
        private readonly TMP_Text _name;
        private readonly TMP_Text _rarity;
        private readonly Image _rarityFrame;
        private readonly Transform _copyBadge;
        private readonly TMP_Text _copyText;
        private readonly Transform _inDeck;
        private readonly Transform _newBadge;
        private readonly Transform _selected;
        private readonly CanvasGroup _group;
        private Color _iconDefault;

        public HeroCardView(Transform root)
        {
            Root = root as RectTransform;
            Button = root.GetComponent<Button>();
            _icon = UiFind.Optional<Image>(root, "Icon");
            _name = UiFind.Optional<TMP_Text>(root, "NameText");
            _rarity = UiFind.Optional<TMP_Text>(root, "RarityText");
            _rarityFrame = UiFind.Optional<Image>(root, "RarityFrame");
            _copyBadge = root.Find("CopyBadge");
            _copyText = UiFind.Optional<TMP_Text>(root, "CopyBadge/CopyText");
            _inDeck = root.Find("InDeckMark");
            _newBadge = root.Find("NewBadge");
            _selected = root.Find("SelectedFrame");
            _group = root.GetComponent<CanvasGroup>();
            if (_group == null) _group = root.gameObject.AddComponent<CanvasGroup>();
            if (_icon != null) _iconDefault = _icon.color;
        }

        public RectTransform Root { get; }
        public Button Button { get; }

        /// <summary>The hero id shown, null when the card is empty.</summary>
        public string HeroId { get; private set; }

        public void OnClick(Action<HeroCardView> handler)
        {
            if (Button == null) return;
            Button.onClick.RemoveAllListeners();
            Button.onClick.AddListener(() => handler(this));
        }

        /// <summary>
        /// Shows <paramref name="heroId"/>. <paramref name="data"/> may be null for an id missing from the catalog;
        /// <paramref name="copies"/> &lt; 0 hides the copy badge, 0 shows "Chưa có" and dims the card.
        /// </summary>
        public void Set(string heroId, HeroData data, Rarity rarity, int copies, bool inDeck, bool isNew, bool selected)
        {
            HeroId = heroId;
            Root.gameObject.SetActive(true);

            if (_name != null) _name.text = data != null && !string.IsNullOrEmpty(data.displayName) ? data.displayName : heroId;
            if (_rarity != null) _rarity.text = UiText.RarityName(rarity);
            if (_rarityFrame != null) _rarityFrame.color = UiText.RarityColor(rarity);
            if (_icon != null)
            {
                Sprite icon = data != null ? data.icon : null;
                if (icon != null) _icon.sprite = icon;
                _icon.color = icon != null ? Color.white : _iconDefault;
            }

            bool showCopies = copies >= 0;
            UiFind.SetActive(_copyBadge, showCopies);
            if (showCopies && _copyText != null) _copyText.text = copies > 0 ? "×" + copies : "Chưa có";

            UiFind.SetActive(_inDeck, inDeck);
            UiFind.SetActive(_newBadge, isNew);
            SetSelected(selected);
            _group.alpha = copies == 0 ? Missing.a : 1f;
        }

        public void SetSelected(bool selected)
        {
            UiFind.SetActive(_selected, selected);
            Root.localScale = selected ? SelectedScale : Vector3.one;
        }

        public void Hide()
        {
            HeroId = null;
            Root.gameObject.SetActive(false);
        }
    }
}
