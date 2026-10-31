using System;

namespace FateBastion.UI
{
    /// <summary>The four steps of the level-1 tutorial (S10, B-17), in order.</summary>
    public enum TutorialStep
    {
        PlaceHero,
        UpgradeHero,
        CommandAura,
        Meteor,
        Done
    }

    /// <summary>Game events that can complete a step.</summary>
    public enum TutorialTrigger
    {
        HeroPlaced,
        HeroUpgraded,
        HeroEnteredAura,
        MeteorExploded
    }

    /// <summary>
    /// Order of the level-1 tutorial (B-17), pure so it is unit-tested: only the trigger of the current step advances,
    /// so upgrading before placing does not skip a box. Skip ends it at once.
    /// </summary>
    public sealed class TutorialSequence
    {
        public TutorialStep Current { get; private set; } = TutorialStep.PlaceHero;
        public bool IsDone => Current == TutorialStep.Done;

        /// <summary>New step (Done included).</summary>
        public event Action<TutorialStep> StepChanged;

        /// <summary>The trigger that completes <paramref name="step"/>.</summary>
        public static TutorialTrigger TriggerOf(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.PlaceHero: return TutorialTrigger.HeroPlaced;
                case TutorialStep.UpgradeHero: return TutorialTrigger.HeroUpgraded;
                case TutorialStep.CommandAura: return TutorialTrigger.HeroEnteredAura;
                case TutorialStep.Meteor: return TutorialTrigger.MeteorExploded;
                default: throw new ArgumentOutOfRangeException(nameof(step), step, "Done has no trigger.");
            }
        }

        /// <summary>Advances when <paramref name="trigger"/> is the current step's; returns whether it did.</summary>
        public bool Notify(TutorialTrigger trigger)
        {
            if (IsDone || TriggerOf(Current) != trigger) return false;
            Advance(Current + 1);
            return true;
        }

        /// <summary>"Bỏ qua" button.</summary>
        public void Skip()
        {
            if (!IsDone) Advance(TutorialStep.Done);
        }

        private void Advance(TutorialStep next)
        {
            Current = next;
            StepChanged?.Invoke(next);
        }

        /// <summary>Hint shown for each step.</summary>
        public static string Hint(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.PlaceHero:
                    return "Chọn một ô tướng ở dưới (phím 1–5), rồi bấm chuột trái lên bãi cỏ để đặt tướng.";
                case TutorialStep.UpgradeHero:
                    return "Bấm vào tướng vừa đặt rồi nhấn U (hoặc nút Nâng) để nâng cấp tướng.";
                case TutorialStep.CommandAura:
                    return "Đi Triệu Hồi Sư (W A S D) lại gần tướng: Hào quang chỉ huy giúp tướng đánh nhanh hơn.";
                case TutorialStep.Meteor:
                    return "Nhấn Q để ngắm Thiên thạch, rồi bấm chuột trái vào đám quái để thả.";
                default:
                    return "Xong hướng dẫn. Chúc bạn giữ Thành an toàn!";
            }
        }

        /// <summary>"Bước 2/4".</summary>
        public static string Progress(TutorialStep step) =>
            step == TutorialStep.Done ? "Hoàn thành" : $"Bước {(int)step + 1}/{(int)TutorialStep.Done}";
    }
}
