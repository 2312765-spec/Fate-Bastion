using NUnit.Framework;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>Wallet (S4, B-03): spending, OnGoldChanged, kill rewards from CombatEvents (TEAM_ASSIGNMENT 3.1 #8).</summary>
    public class WalletTests
    {
        [TearDown]
        public void TearDown() => CombatEvents.ResetAll();

        [Test]
        public void TrySpend_NotEnough_FalseAndUnchanged()
        {
            var wallet = new Wallet(300);
            int events = 0;
            wallet.OnGoldChanged += _ => events++;

            Assert.IsFalse(wallet.TrySpend(301));
            Assert.AreEqual(300, wallet.Gold);
            Assert.AreEqual(0, events);
        }

        [Test]
        public void TrySpend_Enough_PaysAndRaisesNewValue()
        {
            var wallet = new Wallet(300);
            int last = -1;
            wallet.OnGoldChanged += gold => last = gold;

            Assert.IsTrue(wallet.TrySpend(100));
            Assert.AreEqual(200, wallet.Gold);
            Assert.AreEqual(200, last);
        }

        [Test]
        public void EnemyKilled_Listening_AddsGoldReward()
        {
            var wallet = new Wallet(0);
            wallet.ListenToKills();

            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, false));
            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_boss", Vector3.zero, 150, true));

            Assert.AreEqual(155, wallet.Gold);
        }

        [Test]
        public void EnemyKilled_AfterStopListening_Ignored()
        {
            var wallet = new Wallet(0);
            wallet.ListenToKills();
            wallet.ListenToKills(); // Twice must not double the reward.
            wallet.StopListeningToKills();

            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, false));

            Assert.AreEqual(0, wallet.Gold);
        }

        [Test]
        public void ListenTwice_OneKill_AddedOnce()
        {
            var wallet = new Wallet(0);
            wallet.ListenToKills();
            wallet.ListenToKills();

            CombatEvents.RaiseEnemyKilled(new EnemyKilledInfo("enemy_normal", Vector3.zero, 5, false));

            Assert.AreEqual(5, wallet.Gold);
        }

        [Test]
        public void Reset_SameValue_NoEvent()
        {
            var wallet = new Wallet(300);
            int events = 0;
            wallet.OnGoldChanged += _ => events++;

            wallet.Reset(300);
            wallet.Reset(0);

            Assert.AreEqual(1, events);
            Assert.AreEqual(0, wallet.Gold);
        }
    }
}
