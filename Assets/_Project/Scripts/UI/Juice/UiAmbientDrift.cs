using PrimeTween;
using UnityEngine;

namespace FateBastion.UI.Juice
{
    /// <summary>Very slow zoom in and out ("Ken Burns") that keeps a static background alive.</summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Fate Bastion/UI/Ambient Drift")]
    public class UiAmbientDrift : MonoBehaviour
    {
        [SerializeField, Min(1f)] private float _maxScale = 1.06f;
        [SerializeField, Min(0.5f)] private float _halfPeriod = 14f;

        private void OnEnable()
        {
            transform.localScale = Vector3.one;
            Tween.Scale(transform, _maxScale, _halfPeriod, Ease.InOutSine, cycles: -1, cycleMode: CycleMode.Yoyo, useUnscaledTime: true);
        }

        private void OnDisable()
        {
            Tween.StopAll(transform);
            transform.localScale = Vector3.one;
        }
    }
}
