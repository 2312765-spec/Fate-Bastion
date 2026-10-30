using PrimeTween;
using UnityEngine;

namespace FateBastion.UI
{
    /// <summary>
    /// Base of every screen or dialog opened through <see cref="UIManager"/> (S10). Shows and hides itself with its
    /// CanvasGroup plus an optional slide or pop of its content; every tween uses unscaled time so it also runs
    /// while the game is paused.
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIScreen : MonoBehaviour
    {
        // Pop starts slightly smaller and overshoots back to 1 (OutBack); hiding shrinks a little less.
        private const float PopFromScale = 0.85f;
        private const float PopToScaleOnHide = 0.92f;

        // The exit runs faster than the entrance so closing never feels sluggish.
        private const float HideSpeedFactor = 0.75f;

        [Tooltip("Overlay: the screen below stays visible (dialogs, pop-ups). Off: a full page that hides the screens below.")]
        [SerializeField] private bool _isOverlay;

        [Tooltip("Esc (Action Map UI) closes this screen when it is on top. Off for screens that need a button, e.g. Result.")]
        [SerializeField] private bool _closeOnCancel = true;

        // Technical UI timing, not a balance value. 0 = no animation at all.
        [Tooltip("Fade in/out time in real seconds (unaffected by pause and x2). 0 = instant, no transition.")]
        [SerializeField, Min(0f)] private float _fadeDuration = 0.15f;

        [Tooltip("Fade only, Slide (pages) or Pop (dialogs).")]
        [SerializeField] private ScreenTransition _transition = ScreenTransition.Fade;

        [Tooltip("Part that slides or pops, e.g. the dialog Panel. Empty = this screen's root.")]
        [SerializeField] private RectTransform _content;

        [Tooltip("Slide transition: real seconds of the movement (the fade keeps its own duration).")]
        [SerializeField, Min(0f)] private float _motionDuration = 0.32f;

        [Tooltip("Slide transition: horizontal distance in canvas units.")]
        [SerializeField] private float _slideDistance = 90f;

        private CanvasGroup _canvasGroup;
        private Tween _fade;
        private Tween _motion;
        private bool _isShown;
        private bool _hasRestPose;
        private Vector2 _restPosition;

        /// <summary>Overlays keep the screen below visible; full pages hide it.</summary>
        public virtual bool IsOverlay => _isOverlay;

        public bool CloseOnCancel => _closeOnCancel;

        public ScreenTransition Transition => _transition;

        /// <summary>Logical visibility; the GameObject is turned off only once the fade-out has finished.</summary>
        public bool IsShown => _isShown;

        /// <summary>Manager that last opened this screen; null if it was never opened.</summary>
        public UIManager Manager { get; private set; }

        /// <summary>True while this screen is in its manager's stack (visible or covered by another page).</summary>
        public bool IsOpen => Manager != null && Manager.IsOpen(this);

        private CanvasGroup Group
        {
            get
            {
                // Lazy: a screen that starts inactive gets shown before its own Awake has ever run.
                if (_canvasGroup == null)
                {
                    _canvasGroup = GetComponent<CanvasGroup>();
                }

                return _canvasGroup;
            }
        }

        // Null when the screen is not a UI element (plain Transform): it then only fades.
        private RectTransform Content => _content != null ? _content : transform as RectTransform;

        /// <summary>For tests and screens built in code. Call before the screen is opened.</summary>
        public void Configure(bool isOverlay, bool closeOnCancel, float fadeDuration)
        {
            _isOverlay = isOverlay;
            _closeOnCancel = closeOnCancel;
            _fadeDuration = Mathf.Max(0f, fadeDuration);
        }

        /// <summary>For screens built in code: transition style and the part it moves (null = the root).</summary>
        public void ConfigureTransition(ScreenTransition transition, RectTransform content, float motionDuration = 0.32f)
        {
            _transition = transition;
            _content = content;
            _motionDuration = Mathf.Max(0f, motionDuration);
            _hasRestPose = false;
        }

        /// <summary>Closes this screen through its manager (button "Back", "Close"…).</summary>
        public void Close()
        {
            if (Manager != null)
            {
                Manager.Close(this);
            }
        }

        /// <summary>
        /// Called by <see cref="UIManager"/> when Esc is pressed in the UI action map and this screen is on top.
        /// Default: close when <see cref="CloseOnCancel"/> is on. Override for screens whose Esc means something else.
        /// </summary>
        public virtual void OnCancelPressed()
        {
            if (_closeOnCancel)
            {
                Close();
            }
        }

        /// <summary>Called once when the screen enters the stack (not again when it only becomes visible).</summary>
        protected virtual void OnOpened()
        {
        }

        /// <summary>Called once when the screen leaves the stack; it is already hiding.</summary>
        protected virtual void OnClosed()
        {
        }

        internal void NotifyOpened(UIManager manager)
        {
            Manager = manager;
            OnOpened();
        }

        internal void NotifyClosed()
        {
            OnClosed();
        }

        /// <summary>Shows or hides the screen; idempotent. <paramref name="instant"/> skips every animation.</summary>
        internal void SetVisible(bool visible, bool instant)
        {
            if (_isShown == visible)
            {
                return;
            }

            _isShown = visible;
            CanvasGroup group = Group;
            CaptureRestPose();
            StopTweens();

            bool animate = !instant && _fadeDuration > 0f;
            if (visible)
            {
                bool wasActive = gameObject.activeSelf;
                gameObject.SetActive(true);
                group.interactable = true;
                group.blocksRaycasts = true;

                if (!animate)
                {
                    group.alpha = 1f;
                    ResetPose();
                    return;
                }

                // Coming from inactive: start transparent. Interrupted fade-out: continue from the current alpha.
                if (!wasActive)
                {
                    group.alpha = 0f;
                }

                _fade = Tween.Alpha(group, 1f, _fadeDuration, useUnscaledTime: true);
                PlayEnterMotion(wasActive);
                return;
            }

            // Stop clicks immediately; the fade-out is only visual.
            group.interactable = false;
            group.blocksRaycasts = false;

            if (!animate || !gameObject.activeInHierarchy)
            {
                group.alpha = 0f;
                ResetPose();
                gameObject.SetActive(false);
                return;
            }

            float hideDuration = _fadeDuration * HideSpeedFactor;
            _fade = Tween.Alpha(group, 0f, hideDuration, useUnscaledTime: true)
                .OnComplete(this, screen => screen.FinishHide());
            PlayExitMotion(hideDuration);
        }

        private void PlayEnterMotion(bool wasActive)
        {
            RectTransform content = Content;
            if (_motionDuration <= 0f || content == null)
            {
                return;
            }

            switch (_transition)
            {
                case ScreenTransition.Slide:
                    if (!wasActive)
                    {
                        content.anchoredPosition = _restPosition + new Vector2(_slideDistance, 0f);
                    }

                    _motion = Tween.UIAnchoredPosition(content, _restPosition, _motionDuration, Ease.OutCubic, useUnscaledTime: true);
                    break;

                case ScreenTransition.Pop:
                    if (!wasActive)
                    {
                        content.localScale = Vector3.one * PopFromScale;
                    }

                    _motion = Tween.Scale(content, 1f, _motionDuration, Ease.OutBack, useUnscaledTime: true);
                    break;
            }
        }

        private void PlayExitMotion(float duration)
        {
            RectTransform content = Content;
            if (content == null)
            {
                return;
            }

            switch (_transition)
            {
                case ScreenTransition.Slide:
                    // Leave toward the left so "back" and "forward" read as opposite directions.
                    _motion = Tween.UIAnchoredPosition(content, _restPosition - new Vector2(_slideDistance * 0.5f, 0f), duration,
                        Ease.InCubic, useUnscaledTime: true);
                    break;

                case ScreenTransition.Pop:
                    _motion = Tween.Scale(content, PopToScaleOnHide, duration, Ease.InCubic, useUnscaledTime: true);
                    break;
            }
        }

        private void FinishHide()
        {
            // Shown again during the fade: the new fade owns the state.
            if (_isShown)
            {
                return;
            }

            if (_motion.isAlive)
            {
                _motion.Stop();
            }

            ResetPose();
            gameObject.SetActive(false);
        }

        // The pose the content returns to; read once so an interrupted slide never drifts.
        private void CaptureRestPose()
        {
            if (_hasRestPose || _transition != ScreenTransition.Slide || Content == null)
            {
                return;
            }

            _restPosition = Content.anchoredPosition;
            _hasRestPose = true;
        }

        private void ResetPose()
        {
            if (Content == null)
            {
                return;
            }

            switch (_transition)
            {
                case ScreenTransition.Slide:
                    if (_hasRestPose)
                    {
                        Content.anchoredPosition = _restPosition;
                    }

                    break;

                case ScreenTransition.Pop:
                    Content.localScale = Vector3.one;
                    break;
            }
        }

        private void StopTweens()
        {
            if (_fade.isAlive)
            {
                _fade.Stop();
            }

            if (_motion.isAlive)
            {
                _motion.Stop();
            }
        }

        protected virtual void OnDestroy()
        {
            StopTweens();
        }
    }
}
