namespace FateBastion.Skills
{
    /// <summary>When a hero's passive skill fires (S6).</summary>
    public enum PassiveTrigger
    {
        None,

        /// <summary>Fires on every basic attack.</summary>
        OnEveryHit,

        /// <summary>Fires on every Nth basic attack; see SkillData.nthHit.</summary>
        OnNthHit,

        /// <summary>Always on while the hero is alive (buff auras).</summary>
        Aura
    }
}
