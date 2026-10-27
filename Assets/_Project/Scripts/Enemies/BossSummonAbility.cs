using UnityEngine;

namespace FateBastion.Enemies
{
    /// <summary>
    /// Timer that turns a boss's presence into summon requests (S2/A-05). Plain class, no Unity dependency beyond
    /// Mathf, so it is unit-tested in EditMode; EnemyController owns one and EnemyManager routes the spawns.
    /// </summary>
    public sealed class BossSummonAbility
    {
        // Guards against a pathologically large deltaTime producing a huge spawn burst in one step.
        private const int MaxBurstsPerTick = 100;

        private EnemyData _summonEnemy;
        private int _count;
        private float _interval;
        private float _timer;

        public EnemyData SummonEnemy => _summonEnemy;
        public bool IsActive { get; private set; }

        /// <summary>Starts the timer. Never active with a null or invalid settings asset.</summary>
        public void Begin(BossSummonSettings settings)
        {
            if (settings == null || !settings.IsValid)
            {
                Clear();
                return;
            }

            _summonEnemy = settings.SummonEnemy;
            _count = settings.Count;
            _interval = Mathf.Max(0.01f, settings.IntervalSeconds);
            _timer = _interval;
            IsActive = true;
        }

        public void Clear()
        {
            _summonEnemy = null;
            _count = 0;
            _timer = 0f;
            IsActive = false;
        }

        /// <summary>Advances scaled time; returns how many enemies to summon this step (0 most steps).</summary>
        public int Tick(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f) return 0;

            _timer -= deltaTime;
            if (_timer > 0f) return 0;

            int bursts = Mathf.FloorToInt(-_timer / _interval) + 1;
            if (bursts > MaxBurstsPerTick) bursts = MaxBurstsPerTick;
            _timer += bursts * _interval;
            return bursts * _count;
        }
    }
}
