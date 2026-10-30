using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI
{
    /// <summary>Screen_Main (S10): Chơi, Gacha, Túi &amp; Deck, Cài đặt, Thoát. The lobby's root screen.</summary>
    public sealed class MainMenuScreen : LobbyScreen
    {
        public override string Title => "Sảnh";

        protected override void OnBind()
        {
            Hook("Menu/PlayButton", () => Context.Open(Context.LevelSelect));
            Hook("Menu/GachaButton", () => Context.Open(Context.GachaScreen));
            Hook("Menu/CollectionButton", () => Context.Open(Context.Collection));
            Hook("Menu/SettingsButton", () => Context.Open(Context.Settings));
            Hook("Menu/QuitButton", AskQuit);
        }

        protected override void Refresh()
        {
        }

        public override void OnCancelPressed()
        {
            // Root screen: Esc never closes it (UIManager ignores the root anyway).
        }

        private void AskQuit()
        {
            if (Context.Confirm == null)
            {
                Quit();
                return;
            }

            Context.Confirm.Show("Thoát game?", "Tiến trình đã được lưu.", _ => Quit());
        }

        private static void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Hook(string path, UnityEngine.Events.UnityAction action)
        {
            Button button = UiFind.Get<Button>(transform, path, this);
            if (button == null) return;
            button.onClick.RemoveListener(action);
            button.onClick.AddListener(action);
        }
    }
}
