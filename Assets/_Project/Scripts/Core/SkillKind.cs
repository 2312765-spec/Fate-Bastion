namespace FateBastion.Core
{
    /// <summary>Skill execution kind; one runner class per kind (S6).</summary>
    public enum SkillKind
    {
        MeleeHit,
        Projectile,
        AreaAtPoint,
        Cone,
        Line,
        Zone,
        Buff
    }
}
