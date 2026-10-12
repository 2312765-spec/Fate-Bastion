using UnityEngine;

namespace FateBastion.Combat
{
    /// <summary>
    /// Pure homing logic of a projectile (S1, S6). Follows the target while it lives; once it dies the
    /// projectile locks the last known point, flies there and ends without damage. Moves in 3D so arrows
    /// reach flyers 2 m up.
    /// </summary>
    public sealed class ProjectileFlight
    {
        public Vector3 Position { get; private set; }
        public Vector3 TargetPoint { get; private set; }
        public float Speed { get; private set; }
        public bool IsFlying { get; private set; }

        /// <summary>
        /// True once the target was seen dead. Never goes back to false during a flight, because a pooled
        /// enemy can be reused as a new enemy while the projectile is still in the air.
        /// </summary>
        public bool TargetLost { get; private set; }

        public void Launch(Vector3 start, Vector3 targetPoint, float speed)
        {
            Position = start;
            TargetPoint = targetPoint;
            Speed = speed;
            IsFlying = true;
            TargetLost = false;
        }

        /// <summary>Ends the flight with no result, e.g. when the projectile is disabled from outside.</summary>
        public void Stop()
        {
            IsFlying = false;
        }

        /// <summary>Advances by deltaTime (scaled). targetPoint is read only while the target is alive.</summary>
        public ProjectileStepResult Step(float deltaTime, bool targetAlive, Vector3 targetPoint)
        {
            if (!IsFlying) return ProjectileStepResult.Idle;

            if (!TargetLost)
            {
                if (targetAlive) TargetPoint = targetPoint;
                else TargetLost = true;
            }

            Position = Vector3.MoveTowards(Position, TargetPoint, Speed * deltaTime);
            if (Position != TargetPoint) return ProjectileStepResult.Flying;

            IsFlying = false;
            return TargetLost ? ProjectileStepResult.ArrivedNoTarget : ProjectileStepResult.HitTarget;
        }
    }
}
