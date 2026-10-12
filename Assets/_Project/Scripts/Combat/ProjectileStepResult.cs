namespace FateBastion.Combat
{
    /// <summary>Outcome of one <see cref="ProjectileFlight.Step"/>.</summary>
    public enum ProjectileStepResult
    {
        /// <summary>Not launched, or already finished.</summary>
        Idle,
        Flying,

        /// <summary>Reached a target that was alive for the whole flight: apply damage.</summary>
        HitTarget,

        /// <summary>Target died mid-flight: reached its last known point, deal no damage, then disable.</summary>
        ArrivedNoTarget
    }
}
