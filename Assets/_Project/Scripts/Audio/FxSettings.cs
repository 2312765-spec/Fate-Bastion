using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Pooled one-shot effects (C-09). One asset at Data/Settings/FxSettings, owned by C. Values are presentation and
    /// performance limits, tuned by feel and the Profiler (C-15), not balance numbers.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Audio/Fx Settings", fileName = "FxSettings")]
    public class FxSettings : ScriptableObject
    {
        public FxEntry hitFire = new FxEntry(12, 0.03f);
        public FxEntry hitIce = new FxEntry(12, 0.03f);
        public FxEntry hitLightning = new FxEntry(12, 0.03f);
        public FxEntry enemyDeath = new FxEntry(12, 0f);
        public FxEntry bossDeath = new FxEntry(2, 0f);
        public FxEntry meteorExplosion = new FxEntry(2, 0f);

        public FxEntry Get(FxKind kind)
        {
            switch (kind)
            {
                case FxKind.HitFire: return hitFire;
                case FxKind.HitIce: return hitIce;
                case FxKind.HitLightning: return hitLightning;
                case FxKind.EnemyDeath: return enemyDeath;
                case FxKind.BossDeath: return bossDeath;
                case FxKind.MeteorExplosion: return meteorExplosion;
                default: return null;
            }
        }
    }
}
