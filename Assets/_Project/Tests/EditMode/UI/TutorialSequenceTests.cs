using System.Collections.Generic;
using NUnit.Framework;
using FateBastion.UI;

namespace FateBastion.Tests.EditMode.UI
{
    /// <summary>Level-1 tutorial order (B-17): one step at a time, only its own trigger advances, skip ends it.</summary>
    public class TutorialSequenceTests
    {
        [Test]
        public void FourSteps_InOrder_ThenDone()
        {
            var sequence = new TutorialSequence();
            var seen = new List<TutorialStep>();
            sequence.StepChanged += seen.Add;

            Assert.IsTrue(sequence.Notify(TutorialTrigger.HeroPlaced));
            Assert.IsTrue(sequence.Notify(TutorialTrigger.HeroUpgraded));
            Assert.IsTrue(sequence.Notify(TutorialTrigger.HeroEnteredAura));
            Assert.IsTrue(sequence.Notify(TutorialTrigger.MeteorExploded));

            Assert.IsTrue(sequence.IsDone);
            CollectionAssert.AreEqual(
                new[] { TutorialStep.UpgradeHero, TutorialStep.CommandAura, TutorialStep.Meteor, TutorialStep.Done }, seen);
        }

        [Test]
        public void WrongTrigger_DoesNotSkipAStep()
        {
            var sequence = new TutorialSequence();

            Assert.IsFalse(sequence.Notify(TutorialTrigger.HeroUpgraded));
            Assert.IsFalse(sequence.Notify(TutorialTrigger.MeteorExploded));
            Assert.AreEqual(TutorialStep.PlaceHero, sequence.Current);

            sequence.Notify(TutorialTrigger.HeroPlaced);
            Assert.IsFalse(sequence.Notify(TutorialTrigger.HeroPlaced), "Placing again is not an upgrade.");
            Assert.AreEqual(TutorialStep.UpgradeHero, sequence.Current);
        }

        [Test]
        public void Skip_EndsAtOnce_AndOnlyOnce()
        {
            var sequence = new TutorialSequence();
            int events = 0;
            sequence.StepChanged += _ => events++;

            sequence.Skip();
            sequence.Skip();

            Assert.IsTrue(sequence.IsDone);
            Assert.AreEqual(1, events);
            Assert.IsFalse(sequence.Notify(TutorialTrigger.HeroPlaced));
        }

        [Test]
        public void EveryStep_HasHintAndProgress()
        {
            Assert.AreEqual("Bước 1/4", TutorialSequence.Progress(TutorialStep.PlaceHero));
            Assert.AreEqual("Bước 4/4", TutorialSequence.Progress(TutorialStep.Meteor));
            foreach (TutorialStep step in System.Enum.GetValues(typeof(TutorialStep)))
            {
                Assert.IsFalse(string.IsNullOrEmpty(TutorialSequence.Hint(step)), step.ToString());
            }
        }
    }
}
