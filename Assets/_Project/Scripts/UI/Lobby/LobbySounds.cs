using System.Collections.Generic;
using FateBastion.Audio;
using FateBastion.Core;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Lobby sound cues (S11 hooks C-13 left for B): gacha reveal by best rarity, merge, error. Silent when the scene has
    /// no _Audio (AudioManager), e.g. UI sandboxes and tests.
    /// </summary>
    public static class LobbySounds
    {
        /// <summary>One reveal sound for the best rarity of the pull, so a ×10 does not stack ten clips.</summary>
        public static void GachaReveal(GachaPullResult result)
        {
            if (result == null || AudioManager.Instance == null) return;
            IReadOnlyList<GachaRoll> rolls = result.Rolls;
            if (rolls == null || rolls.Count == 0) return;

            Rarity best = rolls[0].Rarity;
            for (int i = 1; i < rolls.Count; i++)
            {
                if (rolls[i].Rarity > best) best = rolls[i].Rarity;
            }

            AudioManager.Instance.PlayGachaReveal(best);
        }

        public static void Merge() => Play(UiSound.Merge);

        public static void Error() => Play(UiSound.Error);

        private static void Play(UiSound cue)
        {
            if (AudioManager.Instance != null) AudioManager.Instance.PlayUi(cue);
        }
    }
}
