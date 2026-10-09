using UnityEngine;
using FateBastion.Core;

namespace FateBastion.Combat
{
    /// <summary>One damage application. Struct passed by 'in' so no garbage is produced per hit (S1).</summary>
    public readonly struct DamageInfo
    {
        /// <summary>Damage before resistances; DamageCalculator (S1) applies armor or magic resist.</summary>
        public readonly float Amount;

        public readonly DamageType Type;
        public readonly Element Element;

        /// <summary>Who caused the hit (hero, meteor, status effect); may be null for world sources.</summary>
        public readonly Object Source;

        /// <summary>False for melee heroes, so flying enemies cannot be hit (S1).</summary>
        public readonly bool CanHitFlying;

        /// <summary>Status effect applied on hit; null when the hit applies none.</summary>
        public readonly StatusEffectData Status;

        public readonly Vector3 HitPoint;

        public DamageInfo(float amount, DamageType type, Element element, Object source,
            bool canHitFlying, StatusEffectData status, Vector3 hitPoint)
        {
            Amount = amount;
            Type = type;
            Element = element;
            Source = source;
            CanHitFlying = canHitFlying;
            Status = status;
            HitPoint = hitPoint;
        }
    }
}
