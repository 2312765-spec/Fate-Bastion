using System;
using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_Pause (S5, S10, B-15): Tiếp tục, Cài đặt, Về sảnh (asks first: leaving mid-match gives no reward).
    /// Esc on this screen resumes. MatchMenusController opens it from GameManager.PausedChanged.
    /// </summary>
    public sealed class PauseScreen : UIScreen
    {
        private Action _resume;
        private Action _openSettings;
        private Action _toLobby;
        private ConfirmDialog _confirm;
        private bool _bound;

        public void Bind(Action resume, Action openSettings, Action toLobby, ConfirmDialog confirm)
        {
            _resume = resume;
            _openSettings = openSettings;
            _toLobby = toLobby;
            _confirm = confirm;
            if (_bound) return;
            _bound = true;

            Hook("Panel/ResumeButton", () => _resume?.Invoke());
            Hook("Panel/SettingsButton", () => _openSettings?.Invoke());
            Hook("Panel/LobbyButton", AskLobby);
        }

        public override void OnCancelPressed() => _resume?.Invoke();

        private void AskLobby()
        {
            if (_confirm == null)
            {
                _toLobby?.Invoke();
                return;
            }

            _confirm.Show("Về sảnh?", "Trận đấu sẽ kết thúc và không được nhận thưởng.", _ => _toLobby?.Invoke());
        }

        private void Hook(string path, UnityEngine.Events.UnityAction action)
        {
            Button button = UiFind.Get<Button>(transform, path, this);
            if (button != null) button.onClick.AddListener(action);
        }
    }
}
