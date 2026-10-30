using System;
using NUnit.Framework;
using FateBastion.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>A ConfirmDialog question is answered at most once (S10, B-13).</summary>
    public class ConfirmPromptTests
    {
        private ConfirmPrompt _prompt;

        [SetUp]
        public void SetUp()
        {
            _prompt = new ConfirmPrompt();
        }

        [Test]
        public void Begin_Valid_IsPending()
        {
            _prompt.Begin(_ => { }, null);

            Assert.IsTrue(_prompt.IsPending);
        }

        [Test]
        public void Begin_NullConfirm_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _prompt.Begin(null, null));
        }

        [Test]
        public void TakeConfirm_Pending_ReturnsConfirmCallbackAndClears()
        {
            Action<bool> onConfirm = _ => { };
            _prompt.Begin(onConfirm, () => { });

            Assert.AreSame(onConfirm, _prompt.TakeConfirm());
            Assert.IsFalse(_prompt.IsPending);
        }

        [Test]
        public void TakeConfirm_CalledTwice_SecondReturnsNull()
        {
            _prompt.Begin(_ => { }, null);
            _prompt.TakeConfirm();

            Assert.IsNull(_prompt.TakeConfirm());
        }

        [Test]
        public void TakeCancel_AfterTakeConfirm_ReturnsNull()
        {
            _prompt.Begin(_ => { }, () => { });
            _prompt.TakeConfirm();

            Assert.IsNull(_prompt.TakeCancel());
        }

        [Test]
        public void TakeCancel_Pending_ReturnsCancelCallbackAndClears()
        {
            Action onCancel = () => { };
            _prompt.Begin(_ => { }, onCancel);

            Assert.AreSame(onCancel, _prompt.TakeCancel());
            Assert.IsFalse(_prompt.IsPending);
        }

        [Test]
        public void Begin_WhilePending_ReturnsPreviousCancelAndKeepsNewQuestion()
        {
            Action firstCancel = () => { };
            Action<bool> secondConfirm = _ => { };
            _prompt.Begin(_ => { }, firstCancel);

            Action replaced = _prompt.Begin(secondConfirm, null);

            Assert.AreSame(firstCancel, replaced);
            Assert.IsTrue(_prompt.IsPending);
            Assert.AreSame(secondConfirm, _prompt.TakeConfirm());
        }

        [Test]
        public void Begin_NothingPending_ReturnsNull()
        {
            Assert.IsNull(_prompt.Begin(_ => { }, () => { }));
        }
    }
}
