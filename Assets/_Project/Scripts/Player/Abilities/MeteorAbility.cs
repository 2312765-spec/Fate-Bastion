using System;
using FateBastion.Core;
using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Controls meteor aiming, impact timing, and its scaled-time cooldown (S7).</summary>
    public sealed class MeteorAbility : ICancelable, IDisposable
    {
        public enum State
        {
            Ready,
            Aiming,
            Falling,
            Cooldown
        }

        private readonly InputRouter _inputRouter;
        private readonly float _cooldownDuration;
        private readonly float _fallDuration;
        private float _cooldownRemaining;
        private float _fallRemaining;
        private Vector3 _impactPoint;
        private bool _disposed;

        public State CurrentState { get; private set; } = State.Ready;
        public float CooldownRemaining => _cooldownRemaining;
        public float CooldownDuration => _cooldownDuration;
        public Vector3 ImpactPoint => _impactPoint;

        /// <summary>Raised when the state changes.</summary>
        public event Action<State> OnStateChanged;

        /// <summary>Raised when the falling delay elapses and the meteor should impact.</summary>
        public event Action<Vector3> OnImpactReady;

        public MeteorAbility(InputRouter inputRouter, float cooldownDuration, float fallDuration)
        {
            if (inputRouter == null) throw new ArgumentNullException(nameof(inputRouter));
            if (float.IsNaN(cooldownDuration) || float.IsInfinity(cooldownDuration) || cooldownDuration <= 0f)
                throw new ArgumentOutOfRangeException(nameof(cooldownDuration), "Cooldown duration must be finite and positive.");
            if (float.IsNaN(fallDuration) || float.IsInfinity(fallDuration) || fallDuration < 0f)
                throw new ArgumentOutOfRangeException(nameof(fallDuration), "Fall duration must be finite and non-negative.");

            _inputRouter = inputRouter;
            _cooldownDuration = cooldownDuration;
            _fallDuration = fallDuration;

            _inputRouter.Meteor += OnMeteorPressed;
            _inputRouter.DeckSlot += OnDeckSlotSelected;
            AbilityEvents.RaiseMeteorCooldownChanged(0f, _cooldownDuration);
        }

        public bool BeginAiming()
        {
            if (_disposed || CurrentState != State.Ready) return false;

            CancelStack.Push(this);
            SetState(State.Aiming);
            return true;
        }

        /// <summary>Commits the selected point and starts the cooldown before the falling delay.</summary>
        public bool ConfirmAim(Vector3 point)
        {
            if (_disposed || CurrentState != State.Aiming) return false;

            CancelStack.Remove(this);
            _impactPoint = point;
            _cooldownRemaining = _cooldownDuration;
            _fallRemaining = _fallDuration;
            SetState(State.Falling);
            AbilityEvents.RaiseMeteorCooldownChanged(_cooldownRemaining, _cooldownDuration);

            if (_fallRemaining <= 0f)
                ResolveImpact();

            return true;
        }

        /// <summary>Advances the falling delay and cooldown using the caller's scaled delta time.</summary>
        public void Tick(float scaledDeltaTime)
        {
            if (_disposed) return;
            if (float.IsNaN(scaledDeltaTime) || float.IsInfinity(scaledDeltaTime) || scaledDeltaTime < 0f)
                throw new ArgumentOutOfRangeException(nameof(scaledDeltaTime), "Delta time must be finite and non-negative.");
            if (CurrentState != State.Falling && CurrentState != State.Cooldown) return;

            if (_cooldownRemaining > 0f)
            {
                _cooldownRemaining = Mathf.Max(0f, _cooldownRemaining - scaledDeltaTime);
                AbilityEvents.RaiseMeteorCooldownChanged(_cooldownRemaining, _cooldownDuration);
            }

            if (CurrentState == State.Falling)
            {
                _fallRemaining -= scaledDeltaTime;
                if (_fallRemaining <= 0f)
                    ResolveImpact();
            }
            else if (_cooldownRemaining <= 0f)
            {
                SetState(State.Ready);
            }
        }

        public void OnCancel()
        {
            CancelAiming();
        }

        public void Dispose()
        {
            if (_disposed) return;

            CancelStack.Remove(this);
            _inputRouter.Meteor -= OnMeteorPressed;
            _inputRouter.DeckSlot -= OnDeckSlotSelected;
            _disposed = true;
        }

        private bool CancelAiming()
        {
            if (_disposed || CurrentState != State.Aiming) return false;

            CancelStack.Remove(this);
            SetState(State.Ready);
            AbilityEvents.RaiseMeteorCooldownChanged(0f, _cooldownDuration);
            return true;
        }

        private void OnMeteorPressed()
        {
            if (CurrentState == State.Ready)
                BeginAiming();
            else if (CurrentState == State.Aiming)
                CancelAiming();
        }

        private void OnDeckSlotSelected(int slot)
        {
            CancelAiming();
        }

        private void ResolveImpact()
        {
            SetState(_cooldownRemaining > 0f ? State.Cooldown : State.Ready);
            OnImpactReady?.Invoke(_impactPoint);
        }

        private void SetState(State state)
        {
            if (CurrentState == state) return;

            CurrentState = state;
            OnStateChanged?.Invoke(state);
        }
    }
}
