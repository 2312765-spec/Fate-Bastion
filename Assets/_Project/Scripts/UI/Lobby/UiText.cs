using System.Globalization;
using UnityEngine;
using FateBastion.Core;

namespace FateBastion.UI
{
    /// <summary>Vietnamese labels, number formats and rarity colours shared by the lobby and match menus.</summary>
    public static class UiText
    {
        private static readonly CultureInfo Vietnamese = CreateCulture();

        // Rarity tints for card frames (Common grey, Rare blue, Epic purple, Legendary gold).
        private static readonly Color[] RarityColors =
        {
            new Color32(0x9A, 0xA3, 0xAD, 0xFF),
            new Color32(0x3C, 0x8C, 0xF0, 0xFF),
            new Color32(0xA8, 0x5C, 0xE8, 0xFF),
            new Color32(0xF6, 0xB8, 0x30, 0xFF)
        };

        /// <summary>1000 → "1.000".</summary>
        public static string Number(int value) => value.ToString("N0", Vietnamese);

        public static string Gems(int value) => Number(value) + " Ngọc";

        public static string RarityName(Rarity rarity) => rarity.ToString();

        public static Color RarityColor(Rarity rarity)
        {
            int index = (int)rarity;
            return index >= 0 && index < RarityColors.Length ? RarityColors[index] : Color.white;
        }

        /// <summary>0.085 → "8.5%".</summary>
        public static string Percent(float ratio) => (ratio * 100f).ToString("0.##", CultureInfo.InvariantCulture) + "%";

        private static CultureInfo CreateCulture()
        {
            try
            {
                return CultureInfo.GetCultureInfo("vi-VN");
            }
            catch (CultureNotFoundException)
            {
                // Invariant groups with ',' – replace it so "1,000" still reads as "1.000".
                var culture = (CultureInfo)CultureInfo.InvariantCulture.Clone();
                culture.NumberFormat.NumberGroupSeparator = ".";
                culture.NumberFormat.NumberDecimalSeparator = ",";
                return culture;
            }
        }
    }
}
