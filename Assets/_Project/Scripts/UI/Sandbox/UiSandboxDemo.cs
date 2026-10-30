#if UNITY_EDITOR || DEVELOPMENT_BUILD
using PrimeTween;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.Sandbox
{
    /// <summary>
    /// Sandbox/B_UI driver for B-13: wires the lobby buttons of C's layout to <see cref="UIManager"/> so the stack
    /// navigation, transitions and <see cref="ConfirmDialog"/> can be clicked through. Uses buttons and OnGUI, never
    /// reads keys (Esc comes from InputRouter). Not a real lobby view: the lobby views (B-14+) replace it.
    /// References are filled by Tools > Sandbox > Build B_UI Scene.
    /// </summary>
    public class UiSandboxDemo : MonoBehaviour
    {
        [SerializeField] private UIManager _manager;
        [SerializeField] private UIScreen _levelSelect;
        [SerializeField] private UIScreen _gacha;
        [SerializeField] private UIScreen _gachaResult;
        [SerializeField] private UIScreen _collection;
        [SerializeField] private UIScreen _settings;
        [SerializeField] private ConfirmDialog _confirmDialog;

        [Header("Lobby_Canvas buttons and top bar")]
        [SerializeField] private Button _playButton;
        [SerializeField] private Button _gachaButton;
        [SerializeField] private Button _collectionButton;
        [SerializeField] private Button _settingsButton;
        [SerializeField] private Button _backButton;
        [SerializeField] private Button _settingsCloseButton;
        [SerializeField] private Button _startButton;
        [SerializeField] private Button _pull1Button;
        [SerializeField] private Button _pull10Button;
        [SerializeField] private Button _continueButton;
        [SerializeField] private Button[] _mergeButtons;
        [SerializeField] private TMP_Text _titleText;

        [Tooltip("Show the on-screen sandbox panel (stack state).")]
        [SerializeField] private bool _showPanel = true;

        private string _lastAnswer = "(chưa có)";
        private GUIStyle _labelStyle;
        private GUIStyle _buttonStyle;

        private void Awake()
        {
            if (_manager == null)
            {
                Debug.LogError($"{nameof(UiSandboxDemo)}: UIManager is not assigned; run Tools > Sandbox > Build B_UI Scene again.", this);
                enabled = false;
                return;
            }

            Hook(_playButton, () => _manager.Push(_levelSelect));
            Hook(_gachaButton, () => _manager.Push(_gacha));
            Hook(_collectionButton, () => _manager.Push(_collection));
            Hook(_settingsButton, () => _manager.Push(_settings));
            Hook(_backButton, Back);
            Hook(_settingsCloseButton, Back);
            Hook(_pull1Button, () => _manager.Push(_gachaResult));
            Hook(_pull10Button, () => _manager.Push(_gachaResult));
            Hook(_continueButton, Back);
            Hook(_startButton, () => Ask("Vào trận?", "Deck 5 tướng đã sẵn sàng. Bắt đầu Màn 1?", false));
            if (_mergeButtons != null)
            {
                for (int i = 0; i < _mergeButtons.Length; i++)
                {
                    Hook(_mergeButtons[i], () => Ask("Ghép tướng?", "3 bản thừa cùng độ hiếm sẽ đổi thành 1 tướng ngẫu nhiên bậc kế.", true));
                }
            }

            _manager.TopChanged += OnTopChanged;
        }

        private void Start()
        {
            OnTopChanged(_manager.Top);
        }

        private void OnDestroy()
        {
            if (_manager != null)
            {
                _manager.TopChanged -= OnTopChanged;
            }
        }

        private void Back() => _manager.Pop();

        private void Ask(string title, string message, bool showDontAskAgain)
        {
            _confirmDialog.Show(title, message,
                dontAskAgain => _lastAnswer = dontAskAgain ? "Xác nhận + Không hỏi lại" : "Xác nhận",
                () => _lastAnswer = "Hủy",
                showDontAskAgain);
        }

        private void OnTopChanged(UIScreen top)
        {
            // Overlays (dialogs, gacha result) keep the page title underneath.
            if (top != null && top.IsOverlay)
            {
                return;
            }

            bool atRoot = top == null || top == _manager.RootScreen;
            if (_backButton != null)
            {
                _backButton.gameObject.SetActive(!atRoot);
            }

            if (_titleText == null)
            {
                return;
            }

            string title = TitleFor(top);
            if (_titleText.text == title)
            {
                return;
            }

            _titleText.text = title;
            Transform t = _titleText.transform;
            Tween.StopAll(t);
            t.localScale = Vector3.one;
            Tween.PunchScale(t, new Vector3(0.18f, 0.18f, 0f), 0.4f, frequency: 6f, useUnscaledTime: true);
        }

        private string TitleFor(UIScreen top)
        {
            if (top == _levelSelect) return "Chọn màn";
            if (top == _gacha) return "Triệu hồi";
            if (top == _collection) return "Túi & Deck";
            return "Sảnh";
        }

        private static void Hook(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button != null)
            {
                button.onClick.AddListener(action);
            }
        }

        // Sandbox-only panel: state of the stack.
        private void OnGUI()
        {
            if (!_showPanel)
            {
                return;
            }

            if (_labelStyle == null)
            {
                _labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 15 };
                _buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            }

            UIScreen top = _manager.Top;
            GUILayout.BeginArea(new Rect(10f, Screen.height - 120f, 430f, 110f), GUI.skin.box);
            GUILayout.Label($"Sandbox B_UI · Top: {(top != null ? top.name : "(trống)")} · Số màn: {_manager.Count}", _labelStyle);
            GUILayout.Label($"ConfirmDialog trả lời: {_lastAnswer}", _labelStyle);
            if (GUILayout.Button("Mở ConfirmDialog thử", _buttonStyle))
            {
                Ask("Bỏ qua thời gian chờ?", "Wave sau bắt đầu ngay, bạn nhận +50 Vàng.", true);
            }

            GUILayout.EndArea();
        }
    }
}
#endif
