using UnityEngine;
using FateBastion.Enemies;
using FateBastion.Skills;

namespace FateBastion.Player
{
    /// <summary>Summoner meteor settings not already represented by its reusable SkillData (S7).</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Meteor Settings", fileName = "meteor_settings")]
    public sealed class MeteorSettings : ScriptableObject
    {
        [SerializeField] private SkillData _skill;
        [SerializeField] private EnemyData _normalEnemy;
        [Tooltip("Maximum horizontal distance from the Summoner to a valid ground point.")]
        [SerializeField, Min(0f)] private float _range = 20f;
        [Tooltip("Extra vertical search distance for the collider query; final blast membership is checked on XZ.")]
        [SerializeField, Min(0f)] private float _verticalQueryPadding = 2f;

        public SkillData Skill => _skill;
        public EnemyData NormalEnemy => _normalEnemy;
        public float Range => _range;
        public float VerticalQueryPadding => _verticalQueryPadding;

        /// <summary>Allows deterministic settings construction in tests.</summary>
        public void Configure(SkillData skill, EnemyData normalEnemy, float range, float verticalQueryPadding)
        {
            _skill = skill;
            _normalEnemy = normalEnemy;
            _range = range;
            _verticalQueryPadding = verticalQueryPadding;
        }
    }
}
