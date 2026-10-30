using UnityEngine;
using FateBastion.Heroes;

namespace FateBastion.Tests.EditMode.S4
{
    /// <summary>Spec numbers of S4 built in memory (no project asset).</summary>
    public static class PlacementTestData
    {
        public static PlacementRules Rules()
        {
            var rules = ScriptableObject.CreateInstance<PlacementRules>();
            rules.Configure(new[] { 100, 200, 350, 600 }, new[] { 6, 5, 4, 2 }, 15, 0.8f, 0.5f);
            return rules;
        }

        public static HeroLevelTable Levels()
        {
            var table = ScriptableObject.CreateInstance<HeroLevelTable>();
            table.Configure(new[]
            {
                new HeroLevelTable.LevelStep(0.75f, 0.40f, 0.10f, 0.10f),
                new HeroLevelTable.LevelStep(1.50f, 1.00f, 0.20f, 0.15f)
            });
            return table;
        }
    }
}
