using System;
using NUnit.Framework;
using FateBastion.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Push/pop rules of the UIManager stack (S10, B-13).</summary>
    public class ScreenStackTests
    {
        private ScreenStack<string> _stack;

        [SetUp]
        public void SetUp()
        {
            _stack = new ScreenStack<string>();
        }

        [Test]
        public void Top_Empty_ReturnsNull()
        {
            Assert.IsNull(_stack.Top);
            Assert.AreEqual(0, _stack.Count);
        }

        [Test]
        public void Push_NewItem_BecomesTop()
        {
            _stack.Push("main");
            _stack.Push("settings");

            Assert.AreEqual("settings", _stack.Top);
            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual("main", _stack[0]);
        }

        [Test]
        public void Push_ItemAlreadyInStack_MovesToTopWithoutDuplicate()
        {
            _stack.Push("main");
            _stack.Push("gacha");
            _stack.Push("settings");

            _stack.Push("gacha");

            Assert.AreEqual(3, _stack.Count);
            Assert.AreEqual("gacha", _stack.Top);
            Assert.AreEqual("settings", _stack[1]);
        }

        [Test]
        public void Push_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _stack.Push(null));
        }

        [Test]
        public void Pop_Empty_ReturnsNull()
        {
            Assert.IsNull(_stack.Pop());
        }

        [Test]
        public void Pop_TwoItems_ReturnsLastPushedAndKeepsFirst()
        {
            _stack.Push("main");
            _stack.Push("settings");

            Assert.AreEqual("settings", _stack.Pop());
            Assert.AreEqual("main", _stack.Top);
        }

        [Test]
        public void Remove_MiddleItem_KeepsOrderOfOthers()
        {
            _stack.Push("a");
            _stack.Push("b");
            _stack.Push("c");

            Assert.IsTrue(_stack.Remove("b"));

            Assert.AreEqual(2, _stack.Count);
            Assert.AreEqual("a", _stack[0]);
            Assert.AreEqual("c", _stack.Top);
        }

        [Test]
        public void Remove_ItemNotInStack_ReturnsFalse()
        {
            _stack.Push("a");

            Assert.IsFalse(_stack.Remove("b"));
            Assert.IsFalse(_stack.Remove(null));
            Assert.AreEqual(1, _stack.Count);
        }

        [Test]
        public void Contains_AfterClear_ReturnsFalse()
        {
            _stack.Push("a");
            _stack.Clear();

            Assert.IsFalse(_stack.Contains("a"));
            Assert.AreEqual(0, _stack.Count);
        }
    }
}
