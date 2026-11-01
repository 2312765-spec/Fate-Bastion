using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Core;

namespace FateBastion.UI
{
    /// <summary>
    /// Meteor slot of the HUD (S10 CooldownSlot, B-14): radial overlay and seconds left from
    /// <see cref="AbilityEvents.OnMeteorCooldownChanged"/> (A, S7). Event-driven; the text is only rewritten when the
    /// whole second shown changes. Nodes of C's CooldownSlot prefab: Overlay, TimerText, KeyText.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CooldownSlot : MonoBehaviour
    {
        private Image _overlay;
        private TMP_Text _timer;
        private Action<float, float> _onCooldown;
        private int _shownSeconds = -1;
        private bool _bound;

        private void Awake() => Bind();

        private void OnEnable()
        {
            Bind();
            AbilityEvents.OnMeteorCooldownChanged += _onCooldown;
        }

        private void OnDisable() => AbilityEvents.OnMeteorCooldownChanged -= _onCooldown;

        private void Bind()
        {
            if (_bound) return;
            _bound = true;
            _onCooldown = Show;
            _overlay = UiFind.Get<Image>(transform, "Overlay", this);
            _timer = UiFind.Optional<TMP_Text>(transform, "TimerText");
            Show(0f, 1f);
        }

        /// <summary>For tests and for the HUD when the ability is not in the scene.</summary>
        public void Show(float remaining, float total)
        {
            if (_overlay != null) _overlay.fillAmount = total > 0f ? Mathf.Clamp01(remaining / total) : 0f;

            int seconds = HudFormat.ClockSeconds(remaining);
            if (seconds == _shownSeconds || _timer == null) return;
            _shownSeconds = seconds;
            _timer.text = HudFormat.Cooldown(remaining);
        }
    }
}
