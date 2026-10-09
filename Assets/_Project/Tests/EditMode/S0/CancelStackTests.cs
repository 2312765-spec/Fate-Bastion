using NUnit.Framework;
using FateBastion.Core;

namespace FateBastion.Tests.EditMode.S0
{
    /// <summary>Tests for the shared Esc / right mouse tap contract (S3, TEAM_ASSIGNMENT 3.1).</summary>
    public class CancelStackTests
    {
        private sealed class FakeMode : ICancelable
        {
            public int CancelCount;
            public System.Action OnCancelAction;

            public void OnCancel()
            {
                CancelCount++;
                OnCancelAction?.Invoke();
            }
        }

        [SetUp]
        public void SetUp() => CancelStack.ResetAll();

        [TearDown]
        public void TearDown() => CancelStack.ResetAll();

        [Test]
        public void TryCancelTop_Empty_ReturnsFalse()
        {
            Assert.IsFalse(CancelStack.TryCancelTop());
        }

        [Test]
        public void TryCancelTop_PanelThenPlacement_CancelsOnlyPlacement()
        {
            var panel = new FakeMode();
            var placement = new FakeMode();
            CancelStack.Push(panel);
            CancelStack.Push(placement);

            Assert.IsTrue(CancelStack.TryCancelTop());

            Assert.AreEqual(1, placement.CancelCount);
            Assert.AreEqual(0, panel.CancelCount);
            Assert.AreEqual(1, CancelStack.Count);
        }

        [Test]
        public void TryCancelTop_ThreePresses_PlacementThenPanelThenNothing()
        {
            var panel = new FakeMode();
            var placement = new FakeMode();
            CancelStack.Push(panel);
            CancelStack.Push(placement);

            Assert.IsTrue(CancelStack.TryCancelTop());
            Assert.IsTrue(CancelStack.TryCancelTop());
            Assert.IsFalse(CancelStack.TryCancelTop());

            Assert.AreEqual(1, placement.CancelCount);
            Assert.AreEqual(1, panel.CancelCount);
        }

        [Test]
        public void Remove_ModeEndedByItself_IsNotCancelled()
        {
            var placement = new FakeMode();
            CancelStack.Push(placement);

            CancelStack.Remove(placement);

            Assert.IsFalse(CancelStack.TryCancelTop());
            Assert.AreEqual(0, placement.CancelCount);
        }

        [Test]
        public void Push_SameEntryTwice_MovesToTopWithoutDuplicate()
        {
            var a = new FakeMode();
            var b = new FakeMode();
            CancelStack.Push(a);
            CancelStack.Push(b);

            CancelStack.Push(a);

            Assert.AreEqual(2, CancelStack.Count);
            CancelStack.TryCancelTop();
            Assert.AreEqual(1, a.CancelCount);
            Assert.AreEqual(0, b.CancelCount);
        }

        [Test]
        public void TryCancelTop_OnCancelPushesAnother_DoesNotThrow()
        {
            var meteor = new FakeMode();
            var placement = new FakeMode { OnCancelAction = () => CancelStack.Push(meteor) };
            CancelStack.Push(placement);

            Assert.DoesNotThrow(() => CancelStack.TryCancelTop());
            Assert.IsTrue(CancelStack.Contains(meteor));
        }

        [Test]
        public void Push_Null_IsIgnored()
        {
            CancelStack.Push(null);

            Assert.AreEqual(0, CancelStack.Count);
        }
    }
}
