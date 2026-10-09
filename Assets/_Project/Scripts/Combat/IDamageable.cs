namespace FateBastion.Combat
{
    /// <summary>Anything a hero, skill or the meteor can damage (S1).</summary>
    public interface IDamageable
    {
        /// <summary>False once the target is dead; guards against several sources killing it in one frame (S1).</summary>
        bool IsAlive { get; }

        void TakeDamage(in DamageInfo info);
    }
}
