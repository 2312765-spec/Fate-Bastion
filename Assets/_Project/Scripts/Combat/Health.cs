namespace FateBastion.Combat
{
    /// <summary>
    /// HP of one enemy. Reports death exactly once even when several sources kill it in the same frame,
    /// so OnEnemyKilled is raised once (S1). Overkill is discarded, never carried to another enemy.
    /// </summary>
    public sealed class Health
    {
        public float Current { get; private set; }
        public float Max { get; private set; }
        public bool IsAlive { get; private set; }

        /// <summary>Call when the enemy is taken from the pool. HP is float so Endless waves cannot overflow (S2).</summary>
        public void Reset(float max)
        {
            Max = max;
            Current = max;
            IsAlive = true;
        }

        /// <summary>Applies already-resisted damage. Returns true only on the hit that kills.</summary>
        public bool ApplyDamage(float amount)
        {
            if (!IsAlive || amount <= 0f) return false;

            Current -= amount;
            if (Current > 0f) return false;

            Current = 0f;
            IsAlive = false;
            return true;
        }

        /// <summary>
        /// Marks the enemy dead without damage (it reached the Castle). Returns false if it was already dead,
        /// so "killed and reached the Castle in the same frame" is handled once, as killed (S2).
        /// </summary>
        public bool TryMarkDead()
        {
            if (!IsAlive) return false;
            IsAlive = false;
            return true;
        }
    }
}
