using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.UI;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>UIManager stack navigation and Esc handling (S10, B-13).</summary>
    public class UIManagerPlayModeTests
    {
        private GameObject _routerGo;
        private UiTestInputRouter _router;
        private GameObject _canvas;
        private UIManager _manager;
        private TestScreen _root;
        private float _timeScaleBefore;

        [SetUp]
        public void SetUp()
        {
            _timeScaleBefore = Time.timeScale;
            _routerGo = new GameObject("InputRouter (test)");
            _router = _routerGo.AddComponent<UiTestInputRouter>();

            // Inactive while wiring, like a prefab: Awake runs on activation with the root already set.
            _canvas = new GameObject("Menus (test)");
            _canvas.SetActive(false);
            _manager = _canvas.AddComponent<UIManager>();
            _root = CreateScreen("Root", isOverlay: false);
            _manager.Configure(_root, false);
            _canvas.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScaleBefore;
            // Immediate, so InputRouter.Instance is cleared before the next test creates a router.
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_routerGo);
        }

        private TestScreen CreateScreen(string screenName, bool isOverlay, bool closeOnCancel = true, float fade = 0f)
        {
            var go = new GameObject(screenName);
            go.transform.SetParent(_canvas.transform, false);
            go.SetActive(false);
            var screen = go.AddComponent<TestScreen>();
            screen.Configure(isOverlay, closeOnCancel, fade);
            return screen;
        }

        [Test]
        public void Awake_WithRoot_RootIsOpenTopAndShown()
        {
            Assert.AreSame(_root, _manager.Top);
            Assert.AreEqual(1, _manager.Count);
            Assert.IsTrue(_root.IsShown);
            Assert.IsTrue(_root.gameObject.activeSelf);
            Assert.AreEqual(1, _root.OpenedCount);
        }

        [Test]
        public void Push_FullScreen_HidesPrevious()
        {
            TestScreen settings = CreateScreen("Settings", isOverlay: false);

            _manager.Push(settings);

            Assert.AreSame(settings, _manager.Top);
            Assert.IsTrue(settings.gameObject.activeSelf);
            Assert.IsFalse(_root.IsShown);
            Assert.IsFalse(_root.gameObject.activeSelf);
            Assert.AreEqual(1, settings.OpenedCount);
        }

        [Test]
        public void Push_Overlay_KeepsPreviousShown()
        {
            TestScreen dialog = CreateScreen("Dialog", isOverlay: true);

            _manager.Push(dialog);

            Assert.AreSame(dialog, _manager.Top);
            Assert.IsTrue(dialog.IsShown);
            Assert.IsTrue(_root.IsShown);
        }

        [Test]
        public void Push_AlreadyOpenScreen_MovesToTopWithoutReopening()
        {
            TestScreen a = CreateScreen("A", isOverlay: false);
            TestScreen b = CreateScreen("B", isOverlay: false);
            _manager.Push(a);
            _manager.Push(b);

            _manager.Push(a);

            Assert.AreSame(a, _manager.Top);
            Assert.AreEqual(3, _manager.Count);
            Assert.AreEqual(1, a.OpenedCount);
            Assert.IsTrue(a.IsShown);
            Assert.IsFalse(b.IsShown);
        }

        [Test]
        public void Pop_OnlyRootOpen_ReturnsFalseAndKeepsRoot()
        {
            Assert.IsFalse(_manager.Pop());

            Assert.AreSame(_root, _manager.Top);
            Assert.IsTrue(_root.IsShown);
            Assert.AreEqual(0, _root.ClosedCount);
        }

        [Test]
        public void Pop_FullScreenOverRoot_ShowsRootAgain()
        {
            TestScreen settings = CreateScreen("Settings", isOverlay: false);
            _manager.Push(settings);

            Assert.IsTrue(_manager.Pop());

            Assert.AreSame(_root, _manager.Top);
            Assert.IsTrue(_root.IsShown);
            Assert.IsFalse(settings.gameObject.activeSelf);
            Assert.AreEqual(1, settings.ClosedCount);
        }

        [Test]
        public void Close_MiddleScreen_RemovesItAndKeepsTop()
        {
            TestScreen a = CreateScreen("A", isOverlay: false);
            TestScreen b = CreateScreen("B", isOverlay: true);
            _manager.Push(a);
            _manager.Push(b);

            Assert.IsTrue(_manager.Close(a));

            Assert.AreSame(b, _manager.Top);
            Assert.IsFalse(_manager.IsOpen(a));
            // A full page was removed, so the overlay now sits directly on the root.
            Assert.IsTrue(_root.IsShown);
        }

        [Test]
        public void Close_Root_ReturnsFalse()
        {
            Assert.IsFalse(_manager.Close(_root));
            Assert.IsTrue(_manager.IsOpen(_root));
        }

        [Test]
        public void PushRoot_WithScreensAbove_ClosesEverythingAbove()
        {
            TestScreen a = CreateScreen("A", isOverlay: false);
            TestScreen b = CreateScreen("B", isOverlay: true);
            _manager.Push(a);
            _manager.Push(b);

            _manager.Push(_root);

            Assert.AreEqual(1, _manager.Count);
            Assert.AreSame(_root, _manager.Top);
            Assert.AreEqual(1, a.ClosedCount);
            Assert.AreEqual(1, b.ClosedCount);
        }

        [Test]
        public void PopToRoot_ThreeScreens_LeavesOnlyRoot()
        {
            _manager.Push(CreateScreen("A", isOverlay: false));
            _manager.Push(CreateScreen("B", isOverlay: false));
            _manager.Push(CreateScreen("C", isOverlay: true));

            _manager.PopToRoot();

            Assert.AreEqual(1, _manager.Count);
            Assert.IsTrue(_root.IsShown);
        }

        [Test]
        public void Cancel_InUiMap_ClosesTop()
        {
            TestScreen settings = CreateScreen("Settings", isOverlay: false);
            _manager.Push(settings);
            _router.SetActionMap(ActionMapId.UI);

            _router.SendCancel();

            Assert.IsFalse(_manager.IsOpen(settings));
            Assert.AreSame(_root, _manager.Top);
        }

        [Test]
        public void Cancel_InSummonerMap_DoesNothing()
        {
            // In the Summoner map the same raw Cancel belongs to CancelStack (S3).
            TestScreen hud = CreateScreen("Panel", isOverlay: false);
            _manager.Push(hud);
            Assert.AreEqual(ActionMapId.Summoner, _router.ActiveMap);

            _router.SendCancel();

            Assert.IsTrue(_manager.IsOpen(hud));
        }

        [Test]
        public void Cancel_CloseOnCancelOff_StaysOpen()
        {
            TestScreen result = CreateScreen("Result", isOverlay: false, closeOnCancel: false);
            _manager.Push(result);
            _router.SetActionMap(ActionMapId.UI);

            _router.SendCancel();

            Assert.IsTrue(_manager.IsOpen(result));
        }

        [Test]
        public void Cancel_OnlyRootOpen_KeepsRoot()
        {
            _router.SetActionMap(ActionMapId.UI);

            _router.SendCancel();

            Assert.AreSame(_root, _manager.Top);
            Assert.AreEqual(0, _root.ClosedCount);
        }

        [Test]
        public void Cancel_TwoScreens_ClosesOneStepPerPress()
        {
            TestScreen a = CreateScreen("A", isOverlay: false);
            TestScreen b = CreateScreen("B", isOverlay: true);
            _manager.Push(a);
            _manager.Push(b);
            _router.SetActionMap(ActionMapId.UI);

            _router.SendCancel();

            Assert.AreSame(a, _manager.Top);
            Assert.IsTrue(_manager.IsOpen(a));
        }

        [Test]
        public void TopChanged_PushAndPop_RaisedWithNewTop()
        {
            TestScreen settings = CreateScreen("Settings", isOverlay: false);
            UIScreen lastTop = null;
            int raised = 0;
            _manager.TopChanged += top =>
            {
                lastTop = top;
                raised++;
            };

            _manager.Push(settings);
            Assert.AreSame(settings, lastTop);

            _manager.Pop();
            Assert.AreSame(_root, lastTop);
            Assert.AreEqual(2, raised);
        }

        [UnityTest]
        public IEnumerator Close_WhilePaused_FadeStillFinishes()
        {
            TestScreen settings = CreateScreen("Settings", isOverlay: false, fade: 0.1f);
            _manager.Push(settings);
            Time.timeScale = 0f;

            _manager.Close(settings);
            Assert.IsFalse(settings.IsShown);
            Assert.IsTrue(settings.gameObject.activeSelf, "Fading out: still active this frame.");

            // Unscaled fade (S10): must finish even with timeScale = 0.
            yield return new WaitForSecondsRealtime(0.4f);

            Assert.IsFalse(settings.gameObject.activeSelf);
        }
    }
}
