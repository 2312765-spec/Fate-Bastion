using System;
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_Result (S5, S10, B-15): Thắng/Thua, stars popping in one by one, gems, Endless record, Chơi lại / Về sảnh.
    /// Esc does not close it (a choice is required).
    /// </summary>
    public sealed class ResultScreen : UIScreen
    {
        private const int MaxStars = 3;

        [Tooltip("Real seconds between two stars popping in.")]
        [SerializeField, Min(0f)] private float _starStep = 0.25f;

        private readonly Transform[] _starsOn = new Transform[MaxStars];
        private TMP_Text _title;
        private Transform _stars;
        private TMP_Text _record;
        private TMP_Text _detail;
        private TMP_Text _gems;
        private Action _retry;
        private Action _lobby;
        private bool _bound;

        public void Bind(Action retry, Action lobby)
        {
            _retry = retry;
            _lobby = lobby;
            if (_bound) return;
            _bound = true;

            _title = UiFind.Get<TMP_Text>(transform, "Panel/ResultTitleText", this);
            _stars = transform.Find("Panel/Stars");
            for (int i = 0; i < MaxStars; i++) _starsOn[i] = transform.Find($"Panel/Stars/Star_{i + 1}/On");
            _record = UiFind.Optional<TMP_Text>(transform, "Panel/RecordText");
            _detail = UiFind.Optional<TMP_Text>(transform, "Panel/DetailText");
            _gems = UiFind.Optional<TMP_Text>(transform, "Panel/GemsText");

            Button retryButton = UiFind.Get<Button>(transform, "Panel/Buttons/RetryButton", this);
            if (retryButton != null) retryButton.onClick.AddListener(() => _retry?.Invoke());
            Button lobbyButton = UiFind.Get<Button>(transform, "Panel/Buttons/LobbyButton", this);
            if (lobbyButton != null) lobbyButton.onClick.AddListener(() => _lobby?.Invoke());
        }

        /// <summary>Esc must not close the result: the player picks Chơi lại or Về sảnh.</summary>
        public override void OnCancelPressed()
        {
        }

        /// <summary>Fills the screen; the caller pushes it.</summary>
        public void Show(in MatchResult result, string levelName, int castleMax, bool newRecord)
        {
            if (_title != null)
            {
                _title.text = result.IsEndless ? "Kết thúc Vô tận" : result.Won ? "Chiến thắng!" : "Thất bại";
            }

            UiFind.SetActive(_stars, !result.IsEndless);
            for (int i = 0; i < MaxStars; i++)
            {
                Transform on = _starsOn[i];
                if (on == null) continue;
                bool lit = i < result.Stars;
                on.gameObject.SetActive(lit);
                if (!lit) continue;

                Tween.StopAll(on);
                on.localScale = Vector3.zero;
                Tween.Scale(on, 1f, 0.35f, Ease.OutBack, startDelay: 0.3f + i * _starStep, useUnscaledTime: true);
            }

            if (_record != null)
            {
                _record.gameObject.SetActive(result.IsEndless);
                _record.text = newRecord ? $"Kỷ lục mới: wave {result.WavesReached}" : $"Đạt wave {result.WavesReached}";
            }

            if (_detail != null)
            {
                _detail.text = result.IsEndless
                    ? $"Vô tận · {result.WavesReached} wave"
                    : $"Thành còn {result.CastleHPLeft}/{castleMax} · {levelName}";
            }

            if (_gems != null) _gems.text = result.GemsAwarded > 0 ? "+" + UiText.Gems(result.GemsAwarded) : "Không nhận Ngọc";
        }
    }
}
