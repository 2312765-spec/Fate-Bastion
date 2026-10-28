using PrimeTween;
using UnityEngine;

namespace FateBastion.UI.Juice
{
    /// <summary>
    /// Title entrance: drops in from above with a bounce and fades in, then floats gently up and down while enabled.
    /// Works on any RectTransform; fades through a CanvasGroup if one is present, else through the Graphic.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Title Intro")]
    public class UiTitleIntro : MonoBehaviour
    {
        [SerializeField] private float _dropDistance = 140f;
        [SerializeField, Min(0.01f)] private float _dropDuration = 0.75f;
        [SerializeField, Min(0f)] private float _delay = 0.05f;

        [Tooltip("Height of the idle float in canvas units (0 = no float).")]
        [SerializeField, Min(0f)] private float _floatAmplitude = 7f;
        [SerializeField, Min(0.1f)] private float _floatPeriod = 2.4f;

        private RectTransform _rect;
        private UnityEngine.UI.Graphic _graphic;
        private Vector2 _rest;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            _graphic = GetComponent<UnityEngine.UI.Graphic>();
            _rest = _rect.anchoredPosition;
        }

        private void OnEnable()
        {
            _rect.anchoredPosition = _rest + new Vector2(0f, _dropDistance);
            Tween.UIAnchoredPosition(_rect, _rest, _dropDuration, Ease.OutBounce, startDelay: _delay, useUnscaledTime: true)
                .OnComplete(this, intro => intro.StartFloat());

            if (_graphic != null)
            {
                Color color = _graphic.color;
                color.a = 0f;
                _graphic.color = color;
                Tween.Alpha(_graphic, 1f, _dropDuration * 0.5f, Ease.OutQuad, startDelay: _delay, useUnscaledTime: true);
            }
        }

        private void OnDisable()
        {
            Tween.StopAll(_rect);
            if (_graphic != null)
            {
                Tween.StopAll(_graphic);
                Color color = _graphic.color;
                color.a = 1f;
                _graphic.color = color;
            }

            _rect.anchoredPosition = _rest;
        }

        private void StartFloat()
        {
            if (_floatAmplitude <= 0f || !isActiveAndEnabled)
            {
                return;
            }

            Tween.UIAnchoredPositionY(_rect, _rest.y + _floatAmplitude, _floatPeriod * 0.5f, Ease.InOutSine,
                cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }
    }
}
