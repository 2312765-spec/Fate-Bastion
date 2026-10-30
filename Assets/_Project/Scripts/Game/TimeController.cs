using System;
using UnityEngine;

namespace FateBastion.Game
{
    /// <summary>
    /// The only place that writes Time.timeScale (S5, TEAM_ASSIGNMENT 3): speed ×1/×2 and pause. Anyone may call
    /// <see cref="SetSpeed"/>, <see cref="Pause"/>, <see cref="Resume"/>; only B edits this file. UI tweens use
    /// unscaled time, so menus keep animating while paused.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class TimeController : MonoBehaviour
    {
        private readonly TimeScaleState _state = new TimeScaleState();

        public static TimeController Instance { get; private set; }

        /// <summary>New speed (pause does not change it).</summary>
        public event Action<float> SpeedChanged;

        public event Action<bool> PausedChanged;

        public float Speed => _state.Speed;
        public bool IsPaused => _state.Paused;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogError($"{nameof(TimeController)}: a second instance on '{name}'; destroying it.", this);
                Destroy(this);
                return;
            }

            Instance = this;
            Apply();
        }

        private void OnDestroy()
        {
            if (Instance != this) return;
            Instance = null;
            Time.timeScale = 1f; // Never leave the next scene frozen or fast.
        }

        public void SetSpeed(float speed)
        {
            if (!_state.SetSpeed(speed)) return;
            Apply();
            SpeedChanged?.Invoke(_state.Speed);
        }

        /// <summary>HUD ×1/×2 button.</summary>
        public void ToggleFast()
        {
            float before = _state.Speed;
            _state.ToggleFast();
            if (Mathf.Approximately(before, _state.Speed)) return;
            Apply();
            SpeedChanged?.Invoke(_state.Speed);
        }

        public void Pause()
        {
            if (!_state.Pause()) return;
            Apply();
            PausedChanged?.Invoke(true);
        }

        public void Resume()
        {
            if (!_state.Resume()) return;
            Apply();
            PausedChanged?.Invoke(false);
        }

        /// <summary>New match: ×1 and running.</summary>
        public void ResetForMatch()
        {
            bool wasPaused = _state.Paused;
            float before = _state.Speed;
            _state.Reset();
            Apply();
            if (!Mathf.Approximately(before, _state.Speed)) SpeedChanged?.Invoke(_state.Speed);
            if (wasPaused) PausedChanged?.Invoke(false);
        }

        private void Apply() => Time.timeScale = _state.Effective;
    }
}
