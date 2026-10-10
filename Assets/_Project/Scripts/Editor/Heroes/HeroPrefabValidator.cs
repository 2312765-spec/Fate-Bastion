using System;
using System.Collections.Generic;
using UnityEngine;
using FateBastion.Heroes;

namespace FateBastion.Editor.Heroes
{
    /// <summary>
    /// Pure checks behind Tools > Validate Hero Prefabs (S0b). Takes the asset path from the caller so it never
    /// touches AssetDatabase and can be unit tested with in-memory objects.
    /// </summary>
    public static class HeroPrefabValidator
    {
        public const string HeroesPrefabFolder = "Assets/_Project/Prefabs/Heroes/";

        private static readonly List<string> MissingBuffer = new List<string>(4);

        /// <summary>Validates every hero, appends findings to <paramref name="issues"/> and returns the totals.</summary>
        public static HeroValidationSummary Validate(IReadOnlyList<HeroData> heroes, Func<GameObject, string> getPrefabPath,
            List<HeroValidationIssue> issues)
        {
            int start = issues.Count;
            int heroCount = 0;
            for (int i = 0; i < heroes.Count; i++)
            {
                HeroData hero = heroes[i];
                if (hero == null)
                {
                    continue;
                }

                heroCount++;
                string path = hero.prefab != null ? getPrefabPath(hero.prefab) : null;
                ValidateHero(hero, path, issues);
            }

            int errors = 0;
            int warnings = 0;
            for (int i = start; i < issues.Count; i++)
            {
                if (issues[i].Severity == HeroValidationSeverity.Error) errors++;
                else warnings++;
            }

            return new HeroValidationSummary(heroCount, errors, warnings);
        }

        /// <summary>Runs the five checks of the S0b table for one hero.</summary>
        public static void ValidateHero(HeroData hero, string prefabPath, List<HeroValidationIssue> issues)
        {
            string id = string.IsNullOrEmpty(hero.id) ? hero.name : hero.id;

            if (hero.icon == null)
            {
                issues.Add(new HeroValidationIssue(id, HeroValidationSeverity.Warning,
                    "HeroData.icon is not assigned (run Tools > Render Hero Icons).", hero));
            }

            if (hero.prefab == null)
            {
                issues.Add(new HeroValidationIssue(id, HeroValidationSeverity.Error,
                    "HeroData.prefab is not assigned.", hero));
                return;
            }

            if (!string.IsNullOrEmpty(prefabPath) &&
                !prefabPath.StartsWith(HeroesPrefabFolder, StringComparison.OrdinalIgnoreCase))
            {
                issues.Add(new HeroValidationIssue(id, HeroValidationSeverity.Warning,
                    $"Prefab '{prefabPath}' is outside {HeroesPrefabFolder}.", hero.prefab));
            }

            // includeInactive: a disabled model must still count as having the contract.
            var visual = hero.prefab.GetComponentInChildren<HeroVisual>(true);
            if (visual == null)
            {
                issues.Add(new HeroValidationIssue(id, HeroValidationSeverity.Error,
                    $"Prefab '{hero.prefab.name}' has no {nameof(HeroVisual)} in its children.", hero.prefab));
                return;
            }

            MissingBuffer.Clear();
            visual.CollectMissingFields(MissingBuffer);
            for (int i = 0; i < MissingBuffer.Count; i++)
            {
                issues.Add(new HeroValidationIssue(id, HeroValidationSeverity.Error,
                    $"{nameof(HeroVisual)}.{MissingBuffer[i]} is empty on '{hero.prefab.name}'.", hero.prefab));
            }
        }
    }
}
