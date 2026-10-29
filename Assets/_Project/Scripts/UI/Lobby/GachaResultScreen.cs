using System.Collections.Generic;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Gacha result overlay (Screen_Gacha/ResultPanel, S10 "hiệu ứng ra tướng"): the heroes flip in one by one, Epic and
    /// Legendary cards punch, new heroes carry the "Mới" badge. Unscaled-time tweens; "Tiếp tục" or Esc closes.
    /// </summary>
    public sealed class GachaResultScreen : LobbyScreen
    {
        [Tooltip("Real seconds before the first card flips.")]
        [SerializeField, Min(0f)] private float _firstDelay = 0.2f;

        [Tooltip("Real seconds between two cards.")]
        [SerializeField, Min(0f)] private float _step = 0.09f;

        [SerializeField, Min(0.01f)] private float _flipDuration = 0.35f;

        private readonly List<HeroCardView> _cards = new List<HeroCardView>(GachaService.TenPullCount);
        private TMP_Text _title;
        private GachaPullResult _pending;

        public override string Title => "Kết quả triệu hồi";

        protected override void OnBind()
        {
            _title = UiFind.Optional<TMP_Text>(transform, "Panel/ResultTitle");
            Transform grid = UiFind.Node(transform, "Panel/Grid", this);
            if (grid != null)
            {
                for (int i = 0; i < grid.childCount; i++) _cards.Add(new HeroCardView(grid.GetChild(i)));
            }

            Button next = UiFind.Get<Button>(transform, "Panel/Buttons/ContinueButton", this);
            if (next != null) next.onClick.AddListener(Close);
        }

        /// <summary>Opens the overlay for a successful pull.</summary>
        public void Show(GachaPullResult result)
        {
            _pending = result;
            Context.Open(this);
            Reveal();
        }

        protected override void Refresh()
        {
        }

        private void Reveal()
        {
            if (_pending == null) return;
            IReadOnlyList<GachaRoll> rolls = _pending.Rolls;

            if (_title != null)
            {
                _title.text = _pending.GemsAwarded > 0
                    ? $"Kết quả triệu hồi · Tướng mới +{UiText.Gems(_pending.GemsAwarded)}"
                    : "Kết quả triệu hồi";
            }

            if (rolls.Count > _cards.Count)
            {
                Debug.LogError($"{name}: {rolls.Count} heroes but only {_cards.Count} cards in the result grid.", this);
            }

            for (int i = 0; i < _cards.Count; i++)
            {
                HeroCardView card = _cards[i];
                if (i >= rolls.Count)
                {
                    card.Hide();
                    continue;
                }

                GachaRoll roll = rolls[i];
                card.Set(roll.HeroId, Context.HeroData(roll.HeroId), roll.Rarity, -1, false, roll.IsNew, false);
                Animate(card, _firstDelay + i * _step, roll.Rarity);
            }
        }

        private void Animate(HeroCardView card, float delay, Rarity rarity)
        {
            RectTransform rect = card.Root;
            CanvasGroup group = rect.GetComponent<CanvasGroup>();
            Tween.StopAll(rect);
            if (group != null) Tween.StopAll(group);

            rect.localScale = Vector3.one * 0.6f;
            rect.localRotation = Quaternion.Euler(0f, 90f, 0f);
            if (group != null)
            {
                group.alpha = 0f;
                Tween.Alpha(group, 1f, _flipDuration * 0.6f, Ease.OutQuad, startDelay: delay, useUnscaledTime: true);
            }

            Tween.Scale(rect, 1f, _flipDuration, Ease.OutBack, startDelay: delay, useUnscaledTime: true);
            Tween.LocalRotation(rect, Quaternion.identity, _flipDuration, Ease.OutCubic, startDelay: delay, useUnscaledTime: true);

            if (rarity >= Rarity.Epic)
            {
                float strength = rarity == Rarity.Legendary ? 0.22f : 0.12f;
                Tween.PunchScale(rect, new Vector3(strength, strength, 0f), 0.5f, frequency: 7f,
                    startDelay: delay + _flipDuration, useUnscaledTime: true);
            }
        }

        protected override void OnClosed()
        {
            base.OnClosed();
            foreach (HeroCardView card in _cards)
            {
                Tween.StopAll(card.Root);
                card.Root.localScale = Vector3.one;
                card.Root.localRotation = Quaternion.identity;
            }

            _pending = null;
        }
    }
}
