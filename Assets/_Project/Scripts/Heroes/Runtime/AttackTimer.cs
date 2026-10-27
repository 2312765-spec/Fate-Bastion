using System;

namespace FateBastion.Heroes
{
    /// <summary>Tracks one hero's scaled-time basic-attack cadence without depending on Unity objects.</summary>
    public sealed class AttackTimer
    {
        private float _elapsed;
        private bool _hasInterval;

        /// <summary>Returns true when one attack is due; a valid target attacks immediately after reset.</summary>
        public bool Tick(float deltaTime, float attacksPerSecond, bool canAttack)
        {
            if (float.IsNaN(deltaTime) || float.IsInfinity(deltaTime) || deltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(deltaTime));

            if (float.IsNaN(attacksPerSecond) || float.IsInfinity(attacksPerSecond) || attacksPerSecond <= 0f)
            {
                Reset();
                return false;
            }

            if (!canAttack)
            {
                Reset();
                return false;
            }

            float interval = 1f / attacksPerSecond;
            if (!_hasInterval)
            {
                _elapsed = interval;
                _hasInterval = true;
            }

            _elapsed += deltaTime;
            if (_elapsed < interval) return false;

            _elapsed -= interval;
            return true;
        }

        public void Reset()
        {
            _elapsed = 0f;
            _hasInterval = false;
        }
    }
}
