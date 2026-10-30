using PrimeTween;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Tactile button feedback (S10): grows a little on hover, squashes on press and bounces back on release.
    /// Unscaled time, so it also works on the pause screen. Purely visual: clicks still go through the Button.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Button Juice")]
    public class UiButtonJuice : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Tooltip("What scales. Empty = this object.")]
        [SerializeField] private Transform _target;

        [SerializeField, Min(0.5f)] private float _hoverScale = 1.05f;
        [SerializeField, Min(0.5f)] private float _pressScale = 0.93f;

        [Tooltip("Real seconds of each change.")]
        [SerializeField, Min(0.01f)] private float _duration = 0.12f;

        private Selectable _selectable;
        private Tween _tween;
        // UI elements rest at scale 1. Not read in Awake: a parent UiStaggerIn may already have shrunk this button
        // for its entrance by the time Awake runs.
        private static readonly Vector3 BaseScale = Vector3.one;
        private bool _hovered;
        private bool _pressed;

        private Transform Target => _target != null ? _target : transform;

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        private void OnDisable()
        {
            if (_tween.isAlive)
            {
                _tween.Stop();
            }

            _hovered = false;
            _pressed = false;
            Target.localScale = BaseScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hovered = true;
            Animate(Ease.OutQuad, _duration);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hovered = false;
            _pressed = false;
            Animate(Ease.OutQuad, _duration);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressed = true;
            Animate(Ease.OutQuad, _duration * 0.6f);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left)
            {
                return;
            }

            _pressed = false;
            // A springy release is what makes the press feel physical.
            Animate(Ease.OutBack, _duration * 2.2f);
        }

        private void Animate(Ease ease, float duration)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            bool interactable = _selectable == null || _selectable.IsInteractable();
            float factor = !interactable ? 1f : _pressed ? _pressScale : _hovered ? _hoverScale : 1f;

            // Also stops an entrance animation (UiStaggerIn) still scaling this button, so the two never fight.
            Tween.StopAll(Target);
            Vector3 end = BaseScale * factor;
            if ((Target.localScale - end).sqrMagnitude < 0.000001f)
            {
                return; // Already there; PrimeTween warns about zero-length tweens.
            }

            _tween = Tween.Scale(Target, end, duration, ease, useUnscaledTime: true);
        }
    }
}
