using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>Summoner movement numbers (S3). One asset at Data/Settings/PlayerMovementSettings, owned by C.</summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Settings/Player Movement", fileName = "PlayerMovementSettings")]
    public class PlayerMovementSettings : ScriptableObject
    {
        [Tooltip("Walk speed in m/s (real time, unaffected by x2).")]
        [SerializeField, Min(0f)] private float _walkSpeed = 6f;

        [Tooltip("Speed while holding Shift, m/s.")]
        [SerializeField, Min(0f)] private float _sprintSpeed = 9f;

        [Tooltip("Gravity in m/s² (negative). There is no jump.")]
        [SerializeField] private float _gravity = -20f;

        [Tooltip("Seconds to turn the body towards the move direction.")]
        [SerializeField, Min(0f)] private float _turnSmoothTime = 0.08f;

        [Tooltip("Below this world height the Summoner fell off the map and returns to the spawn point.")]
        [SerializeField] private float _fallResetY = -20f;

        public float WalkSpeed => _walkSpeed;
        public float SprintSpeed => _sprintSpeed;
        public float Gravity => _gravity;
        public float TurnSmoothTime => _turnSmoothTime;
        public float FallResetY => _fallResetY;
    }
}
