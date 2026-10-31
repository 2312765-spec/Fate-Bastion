using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Clears and replays a pooled object's particles each time it is enabled, so a projectile trail (world space)
    /// does not draw a streak from where the previous flight ended (C-09). Put it on the projectile root.
    /// </summary>
    [DisallowMultipleComponent]
    public class ParticleRestartOnEnable : MonoBehaviour
    {
        private ParticleSystem[] _systems;

        private void Awake()
        {
            _systems = GetComponentsInChildren<ParticleSystem>(true);
            if (_systems.Length == 0) Debug.LogError($"{nameof(ParticleRestartOnEnable)} on '{name}': no ParticleSystem below.", this);
        }

        private void OnEnable()
        {
            for (int i = 0; i < _systems.Length; i++)
            {
                _systems[i].Clear(false);
                _systems[i].Play(false);
            }
        }
    }
}
