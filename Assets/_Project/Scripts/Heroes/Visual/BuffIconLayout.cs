namespace FateBastion.Heroes
{
    /// <summary>
    /// Where the buff icons sit above a hero's head (C-09): one icon centred, two side by side. Plain C# so the layout
    /// is unit-tested; <see cref="HeroBuffIndicator"/> applies it.
    /// </summary>
    public static class BuffIconLayout
    {
        /// <summary>
        /// Local X of each shown icon; returns how many are shown. A hidden icon keeps X = 0.
        /// </summary>
        public static int Arrange(bool damage, bool attackSpeed, float spacing, out float damageX, out float attackSpeedX)
        {
            damageX = 0f;
            attackSpeedX = 0f;
            if (damage && attackSpeed)
            {
                damageX = -spacing * 0.5f;
                attackSpeedX = spacing * 0.5f;
                return 2;
            }

            return damage || attackSpeed ? 1 : 0;
        }
    }
}
