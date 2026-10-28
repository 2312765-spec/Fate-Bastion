using PrimeTween;
using UnityEngine;

namespace FateBastion.UI.Juice
{
    /// <summary>Attention grabber played each time the object is enabled: pops in from zero, then wiggles (dialog "!" icon).</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Wiggle")]
    public class UiWiggle : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float _delay = 0.12f;
        [SerializeField] private float _angle = 16f;

        private void OnEnable()
        {
            transform.localScale = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            Tween.Scale(transform, 1f, 0.35f, Ease.OutBack, startDelay: _delay, useUnscaledTime: true);
            Tween.PunchLocalRotation(transform, new Vector3(0f, 0f, _angle), 0.7f, frequency: 6f, startDelay: _delay + 0.2f,
                useUnscaledTime: true);
        }

        private void OnDisable()
        {
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
            transform.localRotation = Quaternion.identity;
        }
    }
}
