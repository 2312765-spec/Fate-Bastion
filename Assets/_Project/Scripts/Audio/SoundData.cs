using UnityEngine;

namespace FateBastion.Audio
{
    /// <summary>
    /// One sound effect (S11): a random clip from a list, volume, ±pitch variance, mixer group and a cap on
    /// how many copies may play at once. Presentation data assigned by C in the Inspector.
    /// </summary>
    [CreateAssetMenu(menuName = "Fate Bastion/Audio/Sound", fileName = "SFX_New")]
    public class SoundData : ScriptableObject
    {
        [Tooltip("One clip is picked at random each time the sound plays.")]
        public AudioClip[] clips = new AudioClip[0];

        [Range(0f, 1f)] public float volume = 1f;

        [Tooltip("Pitch is 1 ± this value, picked at random (0.1 = ±10%).")]
        [Range(0f, 0.5f)] public float pitchVariance = 0.1f;

        public AudioGroup group = AudioGroup.Sfx;

        [Tooltip("Requests beyond this many copies playing at once are dropped (100 deaths in one frame → 4 sounds).")]
        [Min(1)] public int maxConcurrent = 4;

        [Tooltip("0 = 2D (UI, alerts), 1 = fully 3D at the event position.")]
        [Range(0f, 1f)] public float spatialBlend = 1f;

        [Tooltip("3D only: full volume inside this distance in metres.")]
        [Min(0f)] public float minDistance = 5f;

        [Tooltip("3D only: silent beyond this distance in metres (linear rolloff).")]
        [Min(0f)] public float maxDistance = 45f;

        public bool HasClips => clips != null && clips.Length > 0;

        /// <summary>A random clip, or null when the list is empty.</summary>
        public AudioClip PickClip(System.Random random)
        {
            if (!HasClips) return null;
            return clips[random.Next(clips.Length)];
        }

        /// <summary>A random pitch in [1 − variance, 1 + variance].</summary>
        public float PickPitch(System.Random random)
        {
            return 1f + ((float)random.NextDouble() * 2f - 1f) * pitchVariance;
        }
    }
}
