using FateBastion.Core;

namespace FateBastion.Audio
{
    /// <summary>Picks the hit effect for a damage event (C-09). Plain C# so the mapping is unit-tested.</summary>
    public static class FxKindMap
    {
        /// <summary>
        /// Hit spark by element. False for True damage: only the meteor deals it and its explosion already covers the hit.
        /// </summary>
        // ASSUMPTION: burn ticks also raise OnDamageDealt and get a small spark; FxSettings.minInterval keeps them sparse.
        public static bool TryGetHit(DamageType type, Element element, out FxKind kind)
        {
            switch (element)
            {
                case Element.Fire: kind = FxKind.HitFire; break;
                case Element.Ice: kind = FxKind.HitIce; break;
                default: kind = FxKind.HitLightning; break;
            }

            return type != DamageType.True;
        }

        public static FxKind ForDeath(bool isBoss) => isBoss ? FxKind.BossDeath : FxKind.EnemyDeath;
    }
}
