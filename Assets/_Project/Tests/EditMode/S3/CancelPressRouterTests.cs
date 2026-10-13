using NUnit.Framework;
using FateBastion.Core;
using FateBastion.Player;

namespace FateBastion.Tests.EditMode.S3
{
    /// <summary>Esc and right mouse tap priority through CancelStack (S3, TEAM_ASSIGNMENT 3.1 #6).</summary>
    public class CancelPressRouterTests
    {
        private sealed class FakeMode : ICancelable
        {
            public int CancelCount;

            public void OnCancel() => CancelCount++;
        }

        private FakeMode _heroPanel;
        private FakeMode _meteorAim;

        [SetUp]
        public void SetUp()
        {
            CancelStack.ResetAll();
            _heroPanel = new FakeMode();
            _meteorAim = new FakeMode();
        }

        [TearDown]
        public void TearDown() => CancelStack.ResetAll();

        [Test]
        public void Route_EscWithPanelAndMeteorAim_CancelsOnlyMeteorAim()
        {
            CancelStack.Push(_heroPanel);
            CancelStack.Push(_meteorAim);

            CancelPressResult result = CancelPressRouter.Route(isEscape: true);

            Assert.AreEqual(CancelPressResult.Cancelled, result);
            Assert.AreEqual(1, _meteorAim.CancelCount);
            Assert.AreEqual(0, _heroPanel.CancelCount);
            Assert.AreEqual(1, CancelStack.Count);
        }

        [Test]
        public void Route_EscWithPanelOnly_ClosesPanelWithoutPause()
        {
            CancelStack.Push(_heroPanel);

            CancelPressResult result = CancelPressRouter.Route(isEscape: true);

            Assert.AreEqual(CancelPressResult.Cancelled, result);
            Assert.AreEqual(1, _heroPanel.CancelCount);
        }

        [Test]
        public void Route_EscWithEmptyStack_RequestsPause()
        {
            Assert.AreEqual(CancelPressResult.PauseRequested, CancelPressRouter.Route(isEscape: true));
        }

        [Test]
        public void Route_EscThreePresses_MeteorThenPanelThenPause()
        {
            CancelStack.Push(_heroPanel);
            CancelStack.Push(_meteorAim);

            Assert.AreEqual(CancelPressResult.Cancelled, CancelPressRouter.Route(true));
            Assert.AreEqual(CancelPressResult.Cancelled, CancelPressRouter.Route(true));
            Assert.AreEqual(CancelPressResult.PauseRequested, CancelPressRouter.Route(true));

            Assert.AreEqual(1, _meteorAim.CancelCount);
            Assert.AreEqual(1, _heroPanel.CancelCount);
        }

        [Test]
        public void Route_RightTapWithMeteorAim_CancelsMeteorAim()
        {
            CancelStack.Push(_meteorAim);

            Assert.AreEqual(CancelPressResult.Cancelled, CancelPressRouter.Route(isEscape: false));
            Assert.AreEqual(1, _meteorAim.CancelCount);
        }

        [Test]
        public void Route_RightTapWithEmptyStack_IsIgnoredAndDoesNotPause()
        {
            Assert.AreEqual(CancelPressResult.Ignored, CancelPressRouter.Route(isEscape: false));
        }
    }
}
