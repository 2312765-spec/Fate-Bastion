using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.UI;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>Slide and Pop transitions of UIScreen end in the rest pose, also while paused (S10).</summary>
    public class UIScreenTransitionPlayModeTests
    {
        private const float Settle = 0.5f;

        private GameObject _routerGo;
        private GameObject _canvas;
        private UIManager _manager;
        private float _timeScaleBefore;

        [SetUp]
        public void SetUp()
        {
            _timeScaleBefore = Time.timeScale;
            _routerGo = new GameObject("InputRouter (test)");
            _routerGo.AddComponent<UiTestInputRouter>();
            _canvas = new GameObject("Menus (test)", typeof(RectTransform));
            _manager = _canvas.AddComponent<UIManager>();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _timeScaleBefore;
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_routerGo);
        }

        private TestScreen CreateUiScreen(ScreenTransition transition)
        {
            var go = new GameObject(transition + "Screen", typeof(RectTransform));
            go.transform.SetParent(_canvas.transform, false);
            go.SetActive(false);
            var screen = go.AddComponent<TestScreen>();
            screen.Configure(transition == ScreenTransition.Pop, true, 0.1f);
            screen.ConfigureTransition(transition, null, 0.15f);
            return screen;
        }

        [UnityTest]
        public IEnumerator Pop_ShowWhilePaused_EndsAtScaleOne()
        {
            TestScreen dialog = CreateUiScreen(ScreenTransition.Pop);
            Time.timeScale = 0f;

            _manager.Push(dialog);
            Assert.Less(dialog.transform.localScale.x, 1f, "Pop starts smaller.");

            yield return new WaitForSecondsRealtime(Settle);

            Assert.AreEqual(1f, dialog.transform.localScale.x, 0.001f);
        }

        [UnityTest]
        public IEnumerator Pop_Close_ResetsScaleAndDeactivates()
        {
            TestScreen dialog = CreateUiScreen(ScreenTransition.Pop);
            _manager.Push(dialog);
            yield return new WaitForSecondsRealtime(Settle);

            _manager.Close(dialog);
            yield return new WaitForSecondsRealtime(Settle);

            Assert.IsFalse(dialog.gameObject.activeSelf);
            Assert.AreEqual(1f, dialog.transform.localScale.x, 0.001f, "Next open must start from the rest pose.");
        }

        [UnityTest]
        public IEnumerator Slide_ShowAndClose_ReturnsToRestPosition()
        {
            TestScreen page = CreateUiScreen(ScreenTransition.Slide);
            var rect = (RectTransform)page.transform;
            Vector2 rest = rect.anchoredPosition;

            _manager.Push(page);
            Assert.AreNotEqual(rest, rect.anchoredPosition, "Slide starts off to the side.");
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(rest.x, rect.anchoredPosition.x, 0.01f);

            _manager.Close(page);
            yield return new WaitForSecondsRealtime(Settle);
            Assert.AreEqual(rest.x, rect.anchoredPosition.x, 0.01f);
        }

        [Test]
        public void Fade_PlainTransformScreen_ShowsWithoutRectTransform()
        {
            // A screen on a plain Transform only fades; it must not throw.
            var go = new GameObject("PlainScreen");
            go.transform.SetParent(_canvas.transform, false);
            go.SetActive(false);
            var screen = go.AddComponent<TestScreen>();
            screen.Configure(false, true, 0.1f);
            screen.ConfigureTransition(ScreenTransition.Pop, null);

            Assert.DoesNotThrow(() => _manager.Push(screen));
            Assert.IsTrue(screen.IsShown);
        }
    }
}
