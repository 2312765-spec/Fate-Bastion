using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Summoner footsteps (S11): one step sound every few metres walked on the ground plane. Reads only its own
    /// transform, so it needs no input or Player reference; sprinting simply makes steps come faster.
    /// </summary>
    [DisallowMultipleComponent]
    public class FootstepSound : MonoBehaviour
    {
        [Tooltip("Metres walked between two step sounds (presentation only).")]
        [SerializeField, Min(0.1f)] private float _stepDistance = 1.6f;

        [Tooltip("A jump larger than this in one frame (respawn after falling) does not count as walking.")]
        [SerializeField, Min(0.1f)] private float _teleportDistance = 3f;

        private Vector3 _lastPosition;
        private float _travelled;

        private void OnEnable()
        {
            _lastPosition = transform.position;
            _travelled = 0f;
        }

        private void Update()
        {
            Vector3 position = transform.position;
            Vector3 delta = position - _lastPosition;
            _lastPosition = position;
            delta.y = 0f;

            float distance = delta.magnitude;
            if (distance > _teleportDistance)
            {
                _travelled = 0f;
                return;
            }

            _travelled += distance;
            if (_travelled < _stepDistance) return;
            _travelled -= _stepDistance;

            AudioManager manager = AudioManager.Instance;
            if (manager == null) return;
            SoundLibrary library = manager.Library;
            if (library != null && library.footstep != null) manager.Play(library.footstep, position);
        }
    }
}
