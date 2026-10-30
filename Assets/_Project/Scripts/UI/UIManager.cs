using System;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.UI
{
    /// <summary>
    /// Stack navigation for one canvas of screens (S10, B-13): Push opens a screen on top, Pop/Close removes it,
    /// Esc in the UI action map closes the top screen. One UIManager per scene canvas of menus
    /// (Lobby_Canvas in the lobby, MatchMenus_Canvas in a match).
    /// </summary>
    [DisallowMultipleComponent]
    public class UIManager : MonoBehaviour
    {
        // A few screens deep at most (page + settings + dialog); preallocated so Push never allocates.
        private const int InitialCapacity = 8;

        [Tooltip("Bottom screen that is always open and never closed by Esc (Screen_Main in the lobby). Empty in a match.")]
        [SerializeField] private UIScreen _rootScreen;

        // ASSUMPTION: UIManager never switches action maps in a match; GameManager (S5) does it for pause/result.
        // The lobby has no GameManager, so this flag lets the lobby's UIManager enable the UI map once on Start.
        [Tooltip("Enable Action Map UI on Start. On in the lobby only; in a match GameManager switches maps (S5).")]
        [SerializeField] private bool _enableUiActionMapOnStart;

        private readonly ScreenStack<UIScreen> _stack = new ScreenStack<UIScreen>(InitialCapacity);
        private InputRouter _router;
        private Action _onCancel;
        private UIScreen _lastTop;
        private bool _initialized;

        /// <summary>Raised when the top screen changes (null when the stack became empty).</summary>
        public event Action<UIScreen> TopChanged;

        /// <summary>Screen on top, or null when nothing is open.</summary>
        public UIScreen Top => _stack.Top;

        public UIScreen RootScreen => _rootScreen;

        /// <summary>Number of open screens, the root included.</summary>
        public int Count => _stack.Count;

        public bool IsOpen(UIScreen screen) => _stack.Contains(screen);

        /// <summary>For tests and scenes built in code. Call before the GameObject is activated.</summary>
        public void Configure(UIScreen rootScreen, bool enableUiActionMapOnStart)
        {
            _rootScreen = rootScreen;
            _enableUiActionMapOnStart = enableUiActionMapOnStart;
        }

        private void Awake()
        {
            _onCancel = HandleCancel;
            EnsureInitialized();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void Start()
        {
            // InputRouter may wake up after this component; try again once every Awake has run.
            Subscribe();
            if (_router == null)
            {
                Debug.LogWarning($"{nameof(UIManager)} on '{name}': no InputRouter in the scene, Esc will not close screens.", this);
            }
            else if (_enableUiActionMapOnStart)
            {
                _router.SetActionMap(ActionMapId.UI);
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Opens <paramref name="screen"/> on top. A screen already open is brought to the top without being reopened;
        /// pushing the root screen closes everything above it.
        /// </summary>
        public void Push(UIScreen screen)
        {
            if (screen == null)
            {
                Debug.LogError($"{nameof(UIManager)} on '{name}': Push called with a null screen.", this);
                return;
            }

            EnsureInitialized();
            if (screen == _rootScreen)
            {
                PopToRoot();
                return;
            }

            if (_stack.Top == screen)
            {
                return;
            }

            bool wasOpen = _stack.Contains(screen);
            _stack.Push(screen);
            if (!wasOpen)
            {
                screen.NotifyOpened(this);
            }

            RefreshVisibility();
            NotifyTopChanged();
        }

        /// <summary>Closes the top screen; false when only the root (or nothing) is open.</summary>
        public bool Pop()
        {
            UIScreen top = _stack.Top;
            return top != null && Close(top);
        }

        /// <summary>Closes <paramref name="screen"/> wherever it is in the stack; the root cannot be closed.</summary>
        public bool Close(UIScreen screen)
        {
            if (screen == null || screen == _rootScreen || !_stack.Remove(screen))
            {
                return false;
            }

            // Hidden before OnClosed so a callback that opens another screen sees a consistent stack.
            screen.SetVisible(false, false);
            screen.NotifyClosed();
            RefreshVisibility();
            NotifyTopChanged();
            return true;
        }

        /// <summary>Closes every screen above the root (all screens when there is no root).</summary>
        public void PopToRoot()
        {
            // Bounded: an OnClosed that keeps opening screens must not loop forever.
            int guard = _stack.Count + InitialCapacity;
            while (guard-- > 0)
            {
                UIScreen top = _stack.Top;
                if (top == null || top == _rootScreen || !Close(top))
                {
                    break;
                }
            }
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            if (_rootScreen != null)
            {
                _stack.Push(_rootScreen);
                _rootScreen.NotifyOpened(this);
                _rootScreen.SetVisible(true, true);
                _lastTop = _rootScreen;
            }
        }

        // UI map only: in the Summoner map the same raw Cancel is handled by CancelStack (S3, TEAM_ASSIGNMENT 3.1 #6).
        private void HandleCancel()
        {
            if (_router == null || _router.ActiveMap != ActionMapId.UI)
            {
                return;
            }

            UIScreen top = _stack.Top;
            if (top == null || top == _rootScreen)
            {
                return;
            }

            top.OnCancelPressed();
        }

        /// <summary>From the top down, screens stay visible until the first full page; everything under it is hidden.</summary>
        private void RefreshVisibility()
        {
            bool covered = false;
            for (int i = _stack.Count - 1; i >= 0; i--)
            {
                UIScreen screen = _stack[i];
                if (screen == null)
                {
                    // Destroyed while open (scene unload); drop it instead of throwing.
                    _stack.RemoveAt(i);
                    continue;
                }

                screen.SetVisible(!covered, false);
                if (!screen.IsOverlay)
                {
                    covered = true;
                }
            }
        }

        private void NotifyTopChanged()
        {
            UIScreen top = _stack.Top;
            if (top == _lastTop)
            {
                return;
            }

            _lastTop = top;
            TopChanged?.Invoke(top);
        }

        private void Subscribe()
        {
            if (_router != null || InputRouter.Instance == null)
            {
                return;
            }

            _router = InputRouter.Instance;
            _router.Cancel += _onCancel;
        }

        private void Unsubscribe()
        {
            if (_router == null)
            {
                return;
            }

            _router.Cancel -= _onCancel;
            _router = null;
        }
    }
}
