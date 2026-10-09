using UnityEngine;

namespace FateBastion.Core
{
    /// <summary>Payload of <see cref="CombatEvents.OnDamageDealt"/>; drives floating damage numbers and hit SFX.</summary>
    public readonly struct DamageDealtInfo
    {
        public readonly int Amount;
        public readonly DamageType Type;
        public readonly Element Element;
        public readonly Vector3 HitPoint;
        public readonly bool TargetWasKilled;

        public DamageDealtInfo(int amount, DamageType type, Element element, Vector3 hitPoint, bool targetWasKilled)
        {
            Amount = amount;
            Type = type;
            Element = element;
            HitPoint = hitPoint;
            TargetWasKilled = targetWasKilled;
        }
    }
}
