using System.Collections.Generic;
using UnityEditor;
using FateBastion.Core;
using FateBastion.Heroes;
using FateBastion.Skills;

namespace FateBastion.Editor.Import
{
    /// <summary>
    /// heroes.csv to HeroData "&lt;id&gt;" plus the passive SkillData "&lt;id&gt;_passive" (S0 mapping table).
    /// Columns id..range go to HeroData; areaRadius becomes passive.radius; buffType other than None makes the
    /// passive a Buff with buffValue and buffRadius. notes and calc_* columns are ignored.
    /// </summary>
    public static class HeroCsvImporter
    {
        public const string PassiveSuffix = "_passive";

        public static void Import(CsvTable table, BalanceImportRequest request, BalanceImportReport report)
        {
            const string file = "heroes.csv";
            var seenIds = new List<string>(table.Rows.Count);

            foreach (CsvRow row in table.Rows)
            {
                if (!row.TryGetString("id", out string id))
                {
                    report.AddError(file, row.LineNumber, "column 'id' is missing or empty; row skipped.");
                    continue;
                }

                if (!row.TryGetString("displayName", out string displayName))
                {
                    report.AddError(file, row.LineNumber, "column 'displayName' is missing or empty; row skipped.");
                    continue;
                }

                if (!row.TryGetEnum("rarity", out Rarity rarity)
                    || !row.TryGetEnum("element", out Element element)
                    || !row.TryGetEnum("role", out Role role)
                    || !row.TryGetEnum("damageType", out DamageType damageType)
                    || !row.TryGetEnum("attackType", out AttackType attackType)
                    || !row.TryGetEnum("buffType", out BuffType buffType))
                {
                    report.AddError(file, row.LineNumber,
                        $"hero '{id}' has an unknown value in rarity/element/role/damageType/attackType/buffType; row skipped.");
                    continue;
                }

                if (!row.TryGetFloat("damage", out float damage)
                    || !row.TryGetFloat("attacksPerSecond", out float attacksPerSecond)
                    || !row.TryGetFloat("range", out float range)
                    || !row.TryGetFloat("areaRadius", out float areaRadius)
                    || !row.TryGetFloat("buffValue", out float buffValue)
                    || !row.TryGetFloat("buffRadius", out float buffRadius))
                {
                    report.AddError(file, row.LineNumber,
                        $"hero '{id}' has a number that cannot be parsed in damage/attacksPerSecond/range/areaRadius/buffValue/buffRadius; row skipped.");
                    continue;
                }

                SkillData passive = ImportPassive(request, report, id, displayName, damageType, attackType,
                    areaRadius, buffType, buffValue, buffRadius);

                var hero = AssetFolderUtility.LoadOrCreate<HeroData>(request.HeroesFolder, id, out bool created);
                hero.id = id;
                hero.displayName = displayName;
                hero.rarity = rarity;
                hero.element = element;
                hero.role = role;
                hero.damageType = damageType;
                hero.attackType = attackType;
                hero.damage = damage;
                hero.attacksPerSecond = attacksPerSecond;
                hero.range = range;

                // The passive is created by this importer, so linking it is safe; icon, prefab and
                // ultimateSkill are assigned by hand and must never be touched here.
                hero.passiveSkill = passive;

                EditorUtility.SetDirty(hero);
                if (created)
                {
                    report.HeroesCreated++;
                }
                else
                {
                    report.HeroesUpdated++;
                }

                seenIds.Add(id);
            }

            WarnAboutOrphans(request, report, seenIds);
        }

        private static SkillData ImportPassive(BalanceImportRequest request, BalanceImportReport report,
            string heroId, string heroName, DamageType damageType, AttackType attackType,
            float areaRadius, BuffType buffType, float buffValue, float buffRadius)
        {
            string assetName = heroId + PassiveSuffix;
            var passive = AssetFolderUtility.LoadOrCreate<SkillData>(request.SkillsFolder, assetName, out bool created);

            passive.id = assetName;
            passive.displayName = heroName;
            passive.damageType = damageType;
            passive.damageMultiplier = 1f;
            passive.aimMode = AimMode.Auto;
            passive.radius = areaRadius;
            passive.buffType = buffType;
            passive.buffValue = buffValue;
            passive.buffRadius = buffRadius;

            if (buffType != BuffType.None)
            {
                passive.kind = SkillKind.Buff;
                passive.trigger = PassiveTrigger.Aura;
            }
            else
            {
                // ASSUMPTION (Q3): the CSV has no kind column, so a non-buff passive takes its kind from
                // attackType and fires on every basic attack.
                passive.kind = KindFromAttackType(attackType);
                passive.trigger = PassiveTrigger.OnEveryHit;
            }

            EditorUtility.SetDirty(passive);
            if (created)
            {
                report.SkillsCreated++;
            }
            else
            {
                report.SkillsUpdated++;
            }

            return passive;
        }

        private static SkillKind KindFromAttackType(AttackType attackType)
        {
            switch (attackType)
            {
                case AttackType.Melee: return SkillKind.MeleeHit;
                case AttackType.Area: return SkillKind.AreaAtPoint;
                case AttackType.Cone: return SkillKind.Cone;
                default: return SkillKind.Projectile;
            }
        }

        /// <summary>Assets present in the project but absent from the CSV are never deleted, only reported (S0).</summary>
        private static void WarnAboutOrphans(BalanceImportRequest request, BalanceImportReport report, List<string> seenIds)
        {
            foreach (string name in AssetFolderUtility.FindAssetNames<HeroData>(request.HeroesFolder))
            {
                if (!seenIds.Contains(name))
                {
                    report.AddWarning($"HeroData '{name}' exists in the project but not in heroes.csv; kept untouched.");
                }
            }

            foreach (string name in AssetFolderUtility.FindAssetNames<SkillData>(request.SkillsFolder))
            {
                if (!name.EndsWith(PassiveSuffix))
                {
                    continue;
                }

                string heroId = name.Substring(0, name.Length - PassiveSuffix.Length);
                if (!seenIds.Contains(heroId))
                {
                    report.AddWarning($"SkillData '{name}' exists in the project but its hero is not in heroes.csv; kept untouched.");
                }
            }
        }
    }
}
