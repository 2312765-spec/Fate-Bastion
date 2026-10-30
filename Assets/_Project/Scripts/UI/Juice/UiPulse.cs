using PrimeTween;
using UnityEngine;
using UnityEngine.UI;

namespace FateBastion.UI.Juice
{
    /// <summary>Breathing glow: the Graphic's alpha goes back and forth while enabled (highlight behind a key button).</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Graphic))]
    [AddComponentMenu("Fate Bastion/UI/Pulse")]
    public class UiPulse : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] private float _minAlpha = 0.15f;
        [SerializeField, Range(0f, 1f)] private float _maxAlpha = 0.55f;
        [SerializeField, Min(0.1f)] private float _halfPeriod = 1.1f;

        private Graphic _graphic;

        private void Awake()
        {
            _graphic = GetComponent<Graphic>();
        }

        private void OnEnable()
        {
            Color color = _graphic.color;
            color.a = _minAlpha;
            _graphic.color = color;
            Tween.Alpha(_graphic, _maxAlpha, _halfPeriod, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }

        private void OnDisable()
        {
            Tween.StopAll(_graphic);
        }
    }
}
