using System;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Enemies;
using FateBastion.Game;
using FateBastion.Heroes;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Match menus on MatchMenus_Canvas (S5, S10, B-15): opens Pause from GameManager.PausedChanged, Result from
    /// GameEvents.OnMatchEnded, and the shared ConfirmDialog for skip-wave (with "Không hỏi lại") and for selling a
    /// level-3 hero. Runs after GameManager.Start so GameManager.Instance and its match objects are ready.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(UIManager))]
    public sealed class MatchMenusController : MonoBehaviour
    {
        private UIManager _manager;
        private PauseScreen _pause;
        private SettingsScreen _settings;
        private ResultScreen _result;
        private ConfirmDialog _confirm;
        private GameManager _game;
        private PlacementSystem _placement;
        private DialogCancelEntry _dialogEntry;
        private Action<MatchResult> _onMatchEnded;

        private void Awake()
        {
            _manager = GetComponent<UIManager>();
            _pause = GetComponentInChildren<PauseScreen>(true);
            _settings = GetComponentInChildren<SettingsScreen>(true);
            _result = GetComponentInChildren<ResultScreen>(true);
            _confirm = GetComponentInChildren<ConfirmDialog>(true);
            _onMatchEnded = HandleMatchEnded;
        }

        private void Start()
        {
            _game = GameManager.Instance;
            if (_game == null)
            {
                Debug.LogError($"{name}: MatchMenusController needs a GameManager in the scene.", this);
                return;
            }

            if (_confirm != null) _dialogEntry = new DialogCancelEntry(_confirm);
            if (_pause != null) _pause.Bind(_game.Resume, OpenSettings, _game.ReturnToLobby, _confirm);
            if (_result != null) _result.Bind(_game.Retry, _game.ReturnToLobby);
            if (_settings != null && MetaRuntime.HasSession) _settings.Bind(MetaRuntime.Session);

            _game.PausedChanged += HandlePaused;
            _game.SkipConfirmRequested += AskSkip;
            GameEvents.OnMatchEnded += _onMatchEnded;
            _placement = _game.Placement;
            if (_placement != null) _placement.SellConfirmRequested += AskSell;
        }

        private void OnDestroy()
        {
            GameEvents.OnMatchEnded -= _onMatchEnded;
            if (_game != null)
            {
                _game.PausedChanged -= HandlePaused;
                _game.SkipConfirmRequested -= AskSkip;
            }

            if (_placement != null) _placement.SellConfirmRequested -= AskSell;
            if (_dialogEntry != null) CancelStack.Remove(_dialogEntry);
        }

        private void HandlePaused(bool paused)
        {
            if (paused)
            {
                if (_pause != null) _manager.Push(_pause);
            }
            else
            {
                _manager.PopToRoot();
            }
        }

        private void OpenSettings()
        {
            if (_settings != null) _manager.Push(_settings);
        }

        private void HandleMatchEnded(MatchResult result)
        {
            _manager.PopToRoot();
            if (_result == null) return;

            LevelData level = _game.Level;
            string levelName = level != null && !string.IsNullOrEmpty(level.displayName) ? level.displayName : level != null ? level.name : string.Empty;
            int castleMax = _game.Castle != null ? _game.Castle.MaxHP : 0;
            _result.Show(result, levelName, castleMax, _game.LastResultIsNewRecord);
            _manager.Push(_result);
        }

        private void AskSkip()
        {
            if (_confirm == null)
            {
                _game.ConfirmSkip();
                return;
            }

            bool paysGold = _game.Flow != null && _game.Flow.State == GameState.Resting;
            string message = paysGold ? "Wave sau bắt đầu ngay, bạn nhận thêm Vàng." : "Wave đầu tiên bắt đầu ngay.";
            ShowInMatch("Bỏ qua thời gian chờ?", message, dontAskAgain =>
            {
                if (dontAskAgain) _game.SetAskBeforeSkip(false);
                _game.ConfirmSkip();
            }, true);
        }

        private void AskSell(HeroLevel hero)
        {
            if (_confirm == null || _placement == null)
            {
                _placement?.TrySell(hero);
                return;
            }

            int refund = hero.State.SellRefund(_placement.Rules);
            ShowInMatch("Bán tướng cấp 3?", $"Bạn nhận lại {UiText.Number(refund)} Vàng.", _ => _placement.TrySell(hero), false);
        }

        // During play the Summoner map is active: Esc or a right tap goes through CancelStack, so the dialog
        // registers itself there to be cancelled like the other modes (TEAM_ASSIGNMENT 3.1 #6).
        private void ShowInMatch(string title, string message, Action<bool> onConfirm, bool dontAsk)
        {
            // Push after Show: replacing a pending question runs its cancel callback, which removes the entry.
            _confirm.Show(title, message, answer =>
            {
                CancelStack.Remove(_dialogEntry);
                onConfirm(answer);
            }, () => CancelStack.Remove(_dialogEntry), dontAsk);
            if (_confirm.IsPending) CancelStack.Push(_dialogEntry);
        }

        private sealed class DialogCancelEntry : ICancelable
        {
            private readonly ConfirmDialog _dialog;

            public DialogCancelEntry(ConfirmDialog dialog) => _dialog = dialog;

            public void OnCancel() => _dialog.Cancel();
        }
    }
}
