using System;
using UnityEngine;

namespace FateBastion.Core
{
    /// <summary>
    /// Summoner ability event hub (S7). Raised by MeteorAbility (A); listened to by the meteor HUD slot (B)
    /// and by camera shake and audio (C). See Docs/TEAM_ASSIGNMENT.md section 3.
    /// </summary>
    public static class AbilityEvents
    {
        /// <summary>Remaining cooldown and total cooldown in seconds; remaining 0 means the meteor is ready.</summary>
        public static event Action<float, float> OnMeteorCooldownChanged;

        /// <summary>Impact point and blast radius in metres.</summary>
        public static event Action<Vector3, float> OnMeteorExploded;

        public static void RaiseMeteorCooldownChanged(float remaining, float total) =>
            OnMeteorCooldownChanged?.Invoke(remaining, total);

        public static void RaiseMeteorExploded(Vector3 point, float radius) =>
            OnMeteorExploded?.Invoke(point, radius);

        /// <summary>See <see cref="GameEvents.ResetAll"/>.</summary>
        public static void ResetAll()
        {
            OnMeteorCooldownChanged = null;
            OnMeteorExploded = null;
        }
    }
}
