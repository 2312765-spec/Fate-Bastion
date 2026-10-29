using System;
using System.Collections.Generic;

namespace FateBastion.UI
{
    /// <summary>
    /// Plain ordered stack of open screens used by <see cref="UIManager"/> (S10). Index 0 is the bottom.
    /// Kept free of Unity so the push/pop rules are covered by EditMode tests.
    /// </summary>
    public sealed class ScreenStack<T> where T : class
    {
        private readonly List<T> _items;

        public ScreenStack(int capacity = 8)
        {
            _items = new List<T>(capacity);
        }

        public int Count => _items.Count;

        /// <summary>Screen on top, or null when the stack is empty.</summary>
        public T Top => _items.Count == 0 ? null : _items[_items.Count - 1];

        /// <summary>Entry at <paramref name="index"/>, counted from the bottom (0).</summary>
        public T this[int index] => _items[index];

        public bool Contains(T item) => item != null && _items.Contains(item);

        /// <summary>Puts <paramref name="item"/> on top; an item already in the stack is moved to the top.</summary>
        public void Push(T item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            _items.Remove(item);
            _items.Add(item);
        }

        /// <summary>Removes and returns the top item; null when the stack is empty.</summary>
        public T Pop()
        {
            if (_items.Count == 0)
            {
                return null;
            }

            int last = _items.Count - 1;
            T top = _items[last];
            _items.RemoveAt(last);
            return top;
        }

        /// <summary>Removes <paramref name="item"/> wherever it is; false when it was not in the stack.</summary>
        public bool Remove(T item) => item != null && _items.Remove(item);

        public void RemoveAt(int index) => _items.RemoveAt(index);

        public void Clear() => _items.Clear();
    }
}
