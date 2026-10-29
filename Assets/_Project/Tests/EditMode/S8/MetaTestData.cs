using System.Collections.Generic;
using UnityEngine;
using FateBastion.Core;
using FateBastion.Meta;

namespace FateBastion.Tests.EditMode.S8
{
    /// <summary>Builds GachaTable / StarterSettings / SaveData in memory for the S8 tests (same roster as the game).</summary>
    public static class MetaTestData
    {
        public static readonly string[] Commons = { "hero_cung_thu", "hero_linh_bang", "hero_phu_thuy_lua", "hero_tu_si" };
        public static readonly string[] Rares = { "hero_nguoi_tuyet", "hero_phap_su_sam" };
        public static readonly string[] Epics = { "hero_chien_binh_sam", "hero_thanh_nu" };
        public static readonly string[] Legendaries = { "hero_long_vuong" };

        public static readonly string[] Starters =
        {
            "hero_cung_thu", "hero_phu_thuy_lua", "hero_linh_bang", "hero_tu_si", "hero_nguoi_tuyet"
        };

        /// <summary>Spec numbers: 65/25/8.5/1.5, 100/900 gems, pity 25, +50 first-own, merge 3.</summary>
        public static GachaTable Table(float common = 65f, float rare = 25f, float epic = 8.5f, float legendary = 1.5f,
            int pity = 25)
        {
            var table = ScriptableObject.CreateInstance<GachaTable>();
            table.Configure(common, rare, epic, legendary, 100, 900, pity, 50, 3);
            var entries = new List<GachaTable.HeroEntry>();
            Add(entries, Commons, Rarity.Common);
            Add(entries, Rares, Rarity.Rare);
            Add(entries, Epics, Rarity.Epic);
            Add(entries, Legendaries, Rarity.Legendary);
            table.SetHeroes(entries);
            return table;
        }

        public static StarterSettings Starter()
        {
            var settings = ScriptableObject.CreateInstance<StarterSettings>();
            settings.Configure(Starters, 1000, Starters);
            return settings;
        }

        public static SaveData NewPlayer() => new StarterSaveFactory(Starter()).CreateNew();

        public static SaveData Rich(int gems = 1_000_000)
        {
            SaveData data = NewPlayer();
            data.gems = gems;
            return data;
        }

        private static void Add(List<GachaTable.HeroEntry> entries, string[] ids, Rarity rarity)
        {
            foreach (string id in ids)
            {
                entries.Add(new GachaTable.HeroEntry(id, rarity));
            }
        }
    }
}
