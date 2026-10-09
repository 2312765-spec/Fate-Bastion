using System.Collections.Generic;

namespace FateBastion.Core
{
    /// <summary>
    /// Decides what one Esc or right mouse tap cancels (S3). Each system pushes itself when it enters a cancelable
    /// mode and removes itself when it leaves on its own; only the top entry is cancelled per press.
    /// Push order matches the Esc priority in practice: placement or meteor aim is always entered after the hero
    /// info panel, so it sits on top. See Docs/TEAM_ASSIGNMENT.md section 3.1.
    /// </summary>
    public static class CancelStack
    {
        // A handful of modes can be open at once; preallocated so Push never allocates in a match.
        private const int InitialCapacity = 8;

        private static readonly List<ICancelable> Entries = new List<ICancelable>(InitialCapacity);

        public static int Count => Entries.Count;

        public static bool Contains(ICancelable entry) => entry != null && Entries.Contains(entry);

        /// <summary>Puts <paramref name="entry"/> on top; an entry already in the stack is moved to the top.</summary>
        public static void Push(ICancelable entry)
        {
            if (entry == null)
            {
                return;
            }

            Entries.Remove(entry);
            Entries.Add(entry);
        }

        /// <summary>Removes <paramref name="entry"/> without calling it; use when a mode ends by itself.</summary>
        public static void Remove(ICancelable entry)
        {
            if (entry != null)
            {
                Entries.Remove(entry);
            }
        }

        /// <summary>Removes the top entry and calls its <see cref="ICancelable.OnCancel"/>; false when nothing was open.</summary>
        public static bool TryCancelTop()
        {
            if (Entries.Count == 0)
            {
                return false;
            }

            int last = Entries.Count - 1;
            ICancelable top = Entries[last];

            // Removed before the call so OnCancel may safely push or remove other entries.
            Entries.RemoveAt(last);
            top.OnCancel();
            return true;
        }

        /// <summary>Static state survives scene loads and tests; call when a match starts and in [TearDown].</summary>
        public static void ResetAll()
        {
            Entries.Clear();
        }
    }
}
