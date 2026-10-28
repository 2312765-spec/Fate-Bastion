using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// Attack and Ultimate sounds of one hero model (S11). Sits on the model root next to HeroVisual; C picks the
    /// sound per prefab variant by role (sword for SingleDps melee, spell for Mage…). Played by
    /// <see cref="AnimatorSoundBehaviour"/> when the Attack or Ultimate state starts.
    /// </summary>
    [DisallowMultipleComponent]
    public class HeroSoundSet : MonoBehaviour
    {
        [Tooltip("Leave empty to use SoundLibrary.defaultAttack.")]
        [SerializeField] private SoundData _attack;

        [Tooltip("Leave empty to use SoundLibrary.defaultUltimate.")]
        [SerializeField] private SoundData _ultimate;

        public void Play(HeroSoundSlot slot)
        {
            AudioManager manager = AudioManager.Instance;
            if (manager == null) return;

            SoundData sound = slot == HeroSoundSlot.Attack ? _attack : _ultimate;
            if (sound == null)
            {
                SoundLibrary library = manager.Library;
                if (library == null) return;
                sound = slot == HeroSoundSlot.Attack ? library.defaultAttack : library.defaultUltimate;
            }

            if (sound != null) manager.Play(sound, transform.position);
        }
    }
}
