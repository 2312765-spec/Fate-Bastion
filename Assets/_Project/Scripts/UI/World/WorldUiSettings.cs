using UnityEngine;

namespace FateBastion.UI.World
{
    /// <summary>Numbers of the world UI (C-14, S10): health bars and the off-screen warning. One asset at Data/Settings, owned by C.</summary>
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

        public int HealthBarPrewarm => _healthBarPrewarm;
        public int BindingCapacity => _bindingCapacity;
        public float BossBarScale => _bossBarScale;
        public float WarningProgressThreshold => _warningProgressThreshold;
        public float WarningScansPerSecond => _warningScansPerSecond;
        public float WarningEdgeMargin => _warningEdgeMargin;
    }
}
