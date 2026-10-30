using System;

namespace FateBastion.Game
{
    /// <summary>
    /// Speed and pause, kept apart so resuming restores the chosen speed (S5: pause then resume at ×2 stays ×2).
    /// Pure; TimeController copies <see cref="Effective"/> into Time.timeScale.
    /// </summary>
    public sealed class TimeScaleState
    {
        public const float NormalSpeed = 1f;
        public const float FastSpeed = 2f;

        public float Speed { get; private set; } = NormalSpeed;
        public bool Paused { get; private set; }

        /// <summary>The value for Time.timeScale.</summary>
        public float Effective => Paused ? 0f : Speed;

        /// <summary>Sets the game speed (×1, ×2; the debug panel uses ×5). Kept while paused.</summary>
        public bool SetSpeed(float speed)
        {
            if (float.IsNaN(speed) || speed <= 0f) throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be positive; use Pause.");
            if (Math.Abs(Speed - speed) < 1e-4f) return false;
            Speed = speed;
            return true;
        }

        /// <summary>×1 ↔ ×2 (HUD button).</summary>
        public float ToggleFast()
        {
            SetSpeed(Speed > NormalSpeed ? NormalSpeed : FastSpeed);
            return Speed;
        }

        public bool Pause()
        {
            if (Paused) return false;
            Paused = true;
            return true;
        }

        public bool Resume()
        {
            if (!Paused) return false;
            Paused = false;
            return true;
        }

        /// <summary>New match: ×1, not paused.</summary>
        public void Reset()
        {
            Speed = NormalSpeed;
            Paused = false;
        }
    }
}
