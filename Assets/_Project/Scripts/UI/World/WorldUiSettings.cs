using FateBastion.Core;
using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Numbers of the world UI (C-14, S10, S11): health bars, off-screen warning, damage numbers and hit flash. One asset at Data/Settings, owned by C.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/World UI", fileName = "WorldUiSettings")]
    public class WorldUiSettings : ScriptableObject
    {
        [Header("Health bars")]
        [Tooltip("Bars created when the match loads; matches the 100-enemy cap so no bar is instantiated during a match.")]
        [SerializeField, Min(0)] private int _healthBarPrewarm = 100;

        [Tooltip("Initial capacity of the enemy → bar lookup, so it does not grow during a match.")]
        [SerializeField, Min(1)] private int _bindingCapacity = 128;

        [Tooltip("Scale of the boss health bar relative to a normal one.")]
        [SerializeField, Min(0.1f)] private float _bossBarScale = 1.8f;

        [Header("Off-screen warning")]
        [Tooltip("Path progress (0 = Gate, 1 = Castle) from which an enemy can trigger the warning (S10: 80%).")]
        [SerializeField, Range(0f, 1f)] private float _warningProgressThreshold = 0.8f;

        [Tooltip("How often the enemy list is scanned for the one nearest the Castle (CLAUDE.md: 5–10 per second).")]
        [SerializeField, Min(1f)] private float _warningScansPerSecond = 10f;

        [Tooltip("Distance in screen pixels between the arrow and the screen edge.")]
        [SerializeField, Min(0f)] private float _warningEdgeMargin = 48f;

        [Header("Damage numbers")]
        [Tooltip("Numbers on screen at once (S11: 60). When full, a smaller new number is dropped, a larger one replaces the smallest.")]
        [SerializeField, Min(1)] private int _damageNumberMax = 60;

        [Tooltip("Seconds (real time) a number stays: rises and fades out.")]
        [SerializeField, Min(0.05f)] private float _damageNumberLifetime = 0.8f;

        [Tooltip("Screen pixels a number rises over its lifetime.")]
        [SerializeField] private float _damageNumberRise = 60f;

        [Tooltip("Metres above the hit point where a number starts.")]
        [SerializeField] private float _damageNumberHeight = 0.6f;

        [SerializeField, Min(1f)] private float _damageNumberFontSize = 28f;

        [Tooltip("Meteor (True damage) numbers are bigger (S11).")]
        [SerializeField, Min(1f)] private float _meteorFontSize = 46f;

        [SerializeField] private Color _fireColor = new Color32(0xFF, 0x7A, 0x2E, 0xFF);
        [SerializeField] private Color _iceColor = new Color32(0x7F, 0xD8, 0xFF, 0xFF);
        [SerializeField] private Color _lightningColor = new Color32(0xFF, 0xE4, 0x4D, 0xFF);
        [SerializeField] private Color _meteorColor = new Color32(0xFF, 0x3B, 0x3B, 0xFF);

        [Header("Hit flash")]
        [Tooltip("Seconds (real time) an enemy glows after taking damage.")]
        [SerializeField, Min(0.01f)] private float _hitFlashDuration = 0.1f;

        [Tooltip("Emission colour at the start of the flash; the enemy materials need Emission enabled (black).")]
        [SerializeField, ColorUsage(false, true)] private Color _hitFlashColor = new Color(1.5f, 1.5f, 1.5f, 1f);

        public int HealthBarPrewarm => _healthBarPrewarm;
        public int BindingCapacity => _bindingCapacity;
        public float BossBarScale => _bossBarScale;
        public float WarningProgressThreshold => _warningProgressThreshold;
        public float WarningScansPerSecond => _warningScansPerSecond;
        public float WarningEdgeMargin => _warningEdgeMargin;
        public int DamageNumberMax => _damageNumberMax;
        public float DamageNumberLifetime => _damageNumberLifetime;
        public float DamageNumberRise => _damageNumberRise;
        public float DamageNumberHeight => _damageNumberHeight;
        public float DamageNumberFontSize => _damageNumberFontSize;
        public float MeteorFontSize => _meteorFontSize;
        public Color MeteorColor => _meteorColor;
        public float HitFlashDuration => _hitFlashDuration;
        public Color HitFlashColor => _hitFlashColor;

        public Color GetElementColor(Element element)
        {
            switch (element)
            {
                case Element.Fire: return _fireColor;
                case Element.Ice: return _iceColor;
                default: return _lightningColor;
            }
        }
    }
}
