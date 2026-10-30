using FateBastion.Core;
using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Colour and size of a damage number (S11): colour by element, meteor bigger.</summary>
    public static class DamageNumberStyle
    {
        // ASSUMPTION: True damage comes only from the Meteor (no hero deals True; burn ticks keep the hit's type),
        // so True marks a meteor number. DamageDealtInfo has no source field; revisit if a True skill is added.
        public static bool IsMeteor(DamageType type) => type == DamageType.True;

        public static Color GetColor(DamageType type, Element element, WorldUiSettings settings) =>
            IsMeteor(type) ? settings.MeteorColor : settings.GetElementColor(element);

        public static float GetFontSize(DamageType type, WorldUiSettings settings) =>
            IsMeteor(type) ? settings.MeteorFontSize : settings.DamageNumberFontSize;
    }
}
