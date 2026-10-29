using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using FateBastion.Core;
using FateBastion.UI;

namespace FateBastion.Tests.PlayMode.UI
{
    /// <summary>ConfirmDialog answers each question once and Esc counts as Cancel (S10, B-13).</summary>
    public class ConfirmDialogPlayModeTests
    {
        private GameObject _routerGo;
        private UiTestInputRouter _router;
        private GameObject _canvas;
        private UIManager _manager;
        private TestScreen _root;
        private ConfirmDialog _dialog;

        [SetUp]
        public void SetUp()
        {
            _routerGo = new GameObject("InputRouter (test)");
            _router = _routerGo.AddComponent<UiTestInputRouter>();

            _canvas = new GameObject("Menus (test)");
            _canvas.SetActive(false);
            _manager = _canvas.AddComponent<UIManager>();

            var rootGo = new GameObject("Root");
            rootGo.transform.SetParent(_canvas.transform, false);
            _root = rootGo.AddComponent<TestScreen>();
            _root.Configure(false, true, 0f);

            var dialogGo = new GameObject("ConfirmDialog");
            dialogGo.transform.SetParent(_canvas.transform, false);
            dialogGo.SetActive(false);
            _dialog = dialogGo.AddComponent<ConfirmDialog>();
            _dialog.Configure(true, true, 0f);

            _manager.Configure(_root, false);
            _canvas.SetActive(true);

            // This test assembly does not reference uGUI/TMP, so the texts and buttons stay empty;
            // the dialog reports that once (on its first Awake) and still works through Confirm()/Cancel().
            // LogAssert is checked at the end of SetUp, so the error must be logged here, not in the test body.
            LogAssert.Expect(LogType.Error, new Regex("ConfirmDialog.*missing references"));
            dialogGo.SetActive(true);
            dialogGo.SetActive(false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_canvas);
            Object.DestroyImmediate(_routerGo);
        }

        [Test]
        public void Show_Question_OpensOnTopAndKeepsRootVisible()
        {
            _dialog.Show("Title", "Message", _ => { });

            Assert.AreSame(_dialog, _manager.Top);
            Assert.IsTrue(_dialog.IsPending);
            Assert.IsTrue(_dialog.IsShown);
            Assert.IsTrue(_root.IsShown);
        }

        [Test]
        public void Confirm_ClickedTwice_InvokesOnConfirmOnceAndCloses()
        {
            int confirmed = 0;
            bool? dontAsk = null;
            _dialog.Show("Title", "Message", flag =>
            {
                confirmed++;
                dontAsk = flag;
            });

            _dialog.Confirm();
            _dialog.Confirm();

            Assert.AreEqual(1, confirmed);
            Assert.AreEqual(false, dontAsk);
            Assert.IsFalse(_manager.IsOpen(_dialog));
            Assert.IsFalse(_dialog.IsPending);
        }

        [Test]
        public void Cancel_Clicked_InvokesOnCancelNotOnConfirm()
        {
            int confirmed = 0;
            int cancelled = 0;
            _dialog.Show("Title", "Message", _ => confirmed++, () => cancelled++);

            _dialog.Cancel();

            Assert.AreEqual(0, confirmed);
            Assert.AreEqual(1, cancelled);
            Assert.IsFalse(_manager.IsOpen(_dialog));
        }

        [Test]
        public void Esc_InUiMap_CountsAsCancel()
        {
            int cancelled = 0;
            _dialog.Show("Title", "Message", _ => { }, () => cancelled++);
            _router.SetActionMap(ActionMapId.UI);

            _router.SendCancel();

            Assert.AreEqual(1, cancelled);
            Assert.AreSame(_root, _manager.Top);
        }

        [Test]
        public void Show_WhilePending_CancelsPreviousQuestion()
        {
            int firstCancelled = 0;
            int secondConfirmed = 0;
            _dialog.Show("First", "Message", _ => { }, () => firstCancelled++);

            _dialog.Show("Second", "Message", _ => secondConfirmed++);
            _dialog.Confirm();

            Assert.AreEqual(1, firstCancelled);
            Assert.AreEqual(1, secondConfirmed);
        }

        [Test]
        public void PopToRoot_WhilePending_InvokesOnCancel()
        {
            int cancelled = 0;
            _dialog.Show("Title", "Message", _ => { }, () => cancelled++);

            _manager.PopToRoot();

            Assert.AreEqual(1, cancelled);
            Assert.IsFalse(_dialog.IsPending);
        }

        [Test]
        public void Confirm_CallbackShowsNewQuestion_NewQuestionStaysOpen()
        {
            int secondConfirmed = 0;
            _dialog.Show("First", "Message", _ => _dialog.Show("Second", "Message", __ => secondConfirmed++));

            _dialog.Confirm();

            Assert.IsTrue(_dialog.IsPending);
            Assert.AreSame(_dialog, _manager.Top);

            _dialog.Confirm();
            Assert.AreEqual(1, secondConfirmed);
        }
    }
}
