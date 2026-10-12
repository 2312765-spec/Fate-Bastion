using System;
using UnityEngine;

namespace FateBastion.Combat
{
    /// <summary>
    /// Pooled projectile (S1, S6): flies with scaled time toward a target and calls TakeDamage on arrival.
    /// If the target dies mid-flight it flies to the last known point and disables without damage.
    /// The pool (S6) subscribes to <see cref="Finished"/> once and releases the projectile there.
    /// </summary>
    public class Projectile : MonoBehaviour
    {
        private readonly ProjectileFlight _flight = new ProjectileFlight();

        private IDamageable _target;
        private Transform _targetPoint;
        private DamageInfo _damage;

        /// <summary>Raised once per flight, after the hit (or the miss) and after the object is disabled.</summary>
        public event Action<Projectile> Finished;

        /// <summary>Raised on a successful hit, after TakeDamage; S6 uses it for Lightning chains and area hits.</summary>
        public event Action<Projectile, IDamageable> HitTarget;

        public bool IsFlying => _flight.IsFlying;

        /// <summary>Result of the last finished flight: true when damage was applied.</summary>
        public bool LastFlightHitTarget { get; private set; }

        /// <summary>Call when taking the projectile from the pool.</summary>
        public void ResetState()
        {
            _target = null;
            _targetPoint = null;
            _damage = default;
            LastFlightHitTarget = false;
        }

        /// <summary>
        /// Starts a flight. targetPoint is the transform to home on (usually the enemy root); damage.Amount is
        /// D x k before resist, the target applies DamageCalculator itself in TakeDamage.
        /// </summary>
        public void Launch(Vector3 start, IDamageable target, Transform targetPoint, in DamageInfo damage, float speed)
        {
            ResetState();
            _target = target;
            _targetPoint = targetPoint;
            _damage = damage;

            transform.position = start;
            // Pooled projectiles sit inactive; also re-enable the component in case it was unticked in the Inspector.
            enabled = true;
            gameObject.SetActive(true);

            Vector3 aim = targetPoint != null ? targetPoint.position : start;
            _flight.Launch(start, aim, speed);
        }

        private void Update()
        {
            if (!_flight.IsFlying) return;

            // Unity null check covers a destroyed target transform; IsAlive covers death and pool release.
            bool alive = _target != null && _targetPoint != null && _target.IsAlive;
            Vector3 aim = alive ? _targetPoint.position : _flight.TargetPoint;

            ProjectileStepResult result = _flight.Step(Time.deltaTime, alive, aim);
            transform.position = _flight.Position;

            if (result == ProjectileStepResult.HitTarget) Finish(true);
            else if (result == ProjectileStepResult.ArrivedNoTarget) Finish(false);
        }

        private void Finish(bool hit)
        {
            LastFlightHitTarget = hit;

            if (hit)
            {
                // HitPoint is where the projectile actually landed, not where it was fired.
                var info = new DamageInfo(_damage.Amount, _damage.Type, _damage.Element, _damage.Source,
                    _damage.CanHitFlying, _damage.Status, _flight.Position);
                _target.TakeDamage(info);
                HitTarget?.Invoke(this, _target);
            }

            _target = null;
            _targetPoint = null;
            gameObject.SetActive(false);
            Finished?.Invoke(this);
        }

        private void OnDisable()
        {
            // Disabled from outside (pool cleared at match end): stop without damage, no Finished event.
            _flight.Stop();
        }
    }
}
