namespace FateBastion.Enemies
{
    /// <summary>One enemy to spawn: produced by WaveSchedule or a boss summon, consumed by EnemyManager (S2).</summary>
    public readonly struct SpawnRequest
    {
        public readonly EnemyData Enemy;
        public readonly int PathIndex;

        /// <summary>Wave the enemy counts toward; 0 or less means no wave tracks it.</summary>
        public readonly int Wave;

        /// <summary>Metres along the path to start at; 0 = the Gate. Boss summons start at the boss's distance.</summary>
        public readonly float StartDistance;

        public SpawnRequest(EnemyData enemy, int pathIndex, int wave, float startDistance = 0f)
        {
            Enemy = enemy;
            PathIndex = pathIndex;
            Wave = wave;
            StartDistance = startDistance;
        }
    }
}
