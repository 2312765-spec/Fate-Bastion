namespace FateBastion.Heroes
{
    /// <summary>Stat multipliers of a hero level, passed to HeroStats.SetLevelMultipliers (TEAM_ASSIGNMENT 3.1 #4).</summary>
    public readonly struct LevelMultipliers
    {
        public static readonly LevelMultipliers One = new LevelMultipliers(1f, 1f, 1f);

        public readonly float Damage;
        public readonly float AttackSpeed;
        public readonly float Range;

        public LevelMultipliers(float damage, float attackSpeed, float range)
        {
            Damage = damage;
            AttackSpeed = attackSpeed;
            Range = range;
        }
    }
}
