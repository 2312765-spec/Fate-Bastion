using UnityEngine;

namespace FateBastion.Player
{
    /// <summary>
    /// Pure movement math of the Summoner (S3): camera-relative walk / sprint plus gravity, no jump.
    /// The caller passes unscaled delta time so x2 and pause never change the real speed.
    /// </summary>
    public sealed class SummonerMotor
    {
        /// <summary>Small downward speed while grounded so CharacterController.isGrounded stays true on slopes.</summary>
        public const float GroundedVerticalVelocity = -2f;

        private readonly float _walkSpeed;
        private readonly float _sprintSpeed;
        private readonly float _gravity;

        public SummonerMotor(float walkSpeed, float sprintSpeed, float gravity)
        {
            _walkSpeed = walkSpeed;
            _sprintSpeed = sprintSpeed;
            _gravity = gravity;
        }

        public float VerticalVelocity { get; private set; }

        /// <summary>
        /// Velocity in m/s for this frame. <paramref name="move"/> is WASD (x = right, y = forward),
        /// clamped to length 1 so diagonals are not faster; <paramref name="cameraYaw"/> is in degrees.
        /// </summary>
        public Vector3 ComputeVelocity(Vector2 move, float cameraYaw, bool sprint, bool grounded, float dt)
        {
            Vector3 planar = PlanarDirection(move, cameraYaw) * (sprint ? _sprintSpeed : _walkSpeed);

            if (grounded && VerticalVelocity <= 0f)
            {
                VerticalVelocity = GroundedVerticalVelocity;
            }
            else
            {
                VerticalVelocity += _gravity * dt;
            }

            return new Vector3(planar.x, VerticalVelocity, planar.z);
        }

        /// <summary>Call after a teleport so a long fall does not carry over.</summary>
        public void ResetVertical()
        {
            VerticalVelocity = 0f;
        }

        /// <summary>World XZ direction for the input, rotated by the camera yaw; length 0..1.</summary>
        public static Vector3 PlanarDirection(Vector2 move, float cameraYaw)
        {
            move = Vector2.ClampMagnitude(move, 1f);
            float rad = cameraYaw * Mathf.Deg2Rad;
            float sin = Mathf.Sin(rad);
            float cos = Mathf.Cos(rad);

            // forward = (sin, 0, cos), right = (cos, 0, -sin)
            return new Vector3(move.y * sin + move.x * cos, 0f, move.y * cos - move.x * sin);
        }
    }
}
