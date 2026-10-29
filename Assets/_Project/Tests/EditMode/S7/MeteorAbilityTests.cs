using FateBastion.Core;
using FateBastion.Player;
using NUnit.Framework;
using UnityEngine;

namespace FateBastion.Tests.EditMode.S7
{
    /// <summary>Tests the meteor state machine and its InputRouter/CancelStack connections.</summary>
    public class MeteorAbilityTests
    {
        private GameObject _routerObject;
        private MeteorTestInputRouter _router;
        private MeteorAbility _ability;

        [SetUp]
        public void SetUp()
        {
            CancelStack.ResetAll();
            AbilityEvents.ResetAll();
            _routerObject = new GameObject("TestInputRouter");
            _router = _routerObject.AddComponent<MeteorTestInputRouter>();
            _ability = new MeteorAbility(_router, 45f, 0.8f);
        }

        [TearDown]
        public void TearDown()
        {
            _ability?.Dispose();
            Object.DestroyImmediate(_routerObject);
            CancelStack.ResetAll();
            AbilityEvents.ResetAll();
        }

        [Test]
        public void MeteorInput_ReadyThenPressedAgain_EntersAndCancelsAimWithoutCooldown()
        {
            _router.PressMeteor();
            Assert.AreEqual(MeteorAbility.State.Aiming, _ability.CurrentState);
            Assert.IsTrue(CancelStack.Contains(_ability));

            _router.PressMeteor();

            Assert.AreEqual(MeteorAbility.State.Ready, _ability.CurrentState);
            Assert.IsFalse(CancelStack.Contains(_ability));
            Assert.AreEqual(0f, _ability.CooldownRemaining);
        }

        [Test]
        public void DeckSlotSelectedWhileAiming_CancelsWithoutCooldown()
        {
            _ability.BeginAiming();
            _router.SelectDeckSlot(2);

            Assert.AreEqual(MeteorAbility.State.Ready, _ability.CurrentState);
            Assert.AreEqual(0f, _ability.CooldownRemaining);
            Assert.AreEqual(0, CancelStack.Count);
        }

        [Test]
        public void CancelStackTopWhileAiming_CancelsWithoutCooldown()
        {
            _ability.BeginAiming();

            Assert.IsTrue(CancelStack.TryCancelTop());

            Assert.AreEqual(MeteorAbility.State.Ready, _ability.CurrentState);
            Assert.AreEqual(0f, _ability.CooldownRemaining);
        }

        [Test]
        public void ConfirmAim_TickAtOneTimescale_ImpactsThenReadiesAfter45GameSeconds()
        {
            Vector3 point = new Vector3(3f, 0f, 7f);
            Vector3 impactPoint = Vector3.zero;
            int impactCount = 0;
            _ability.OnImpactReady += value =>
            {
                impactPoint = value;
                impactCount++;
            };
            _ability.BeginAiming();

            Assert.IsTrue(_ability.ConfirmAim(point));
            Assert.AreEqual(MeteorAbility.State.Falling, _ability.CurrentState);
            Assert.AreEqual(45f, _ability.CooldownRemaining);

            _ability.Tick(0.8f);
            Assert.AreEqual(1, impactCount);
            Assert.AreEqual(point, impactPoint);
            Assert.AreEqual(MeteorAbility.State.Cooldown, _ability.CurrentState);
            Assert.AreEqual(44.2f, _ability.CooldownRemaining, 0.001f);

            _ability.Tick(44.2f);

            Assert.AreEqual(MeteorAbility.State.Ready, _ability.CurrentState);
            Assert.AreEqual(0f, _ability.CooldownRemaining);
        }

        [Test]
        public void Cooldown_TwoTimescale_ReachesReadyAfter22Point5RealSeconds()
        {
            _ability.BeginAiming();
            _ability.ConfirmAim(Vector3.one);

            for (int elapsedRealSeconds = 0; elapsedRealSeconds < 22; elapsedRealSeconds++)
                _ability.Tick(2f);
            _ability.Tick(1f);

            Assert.AreEqual(MeteorAbility.State.Ready, _ability.CurrentState);
            Assert.AreEqual(0f, _ability.CooldownRemaining);
        }
    }
}
