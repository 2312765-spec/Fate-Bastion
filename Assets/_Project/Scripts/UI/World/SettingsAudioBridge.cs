using System;
using UnityEngine;
using FateBastion.Audio;
using FateBastion.Meta;

namespace FateBastion.UI.World
{
    /// <summary>
    /// Applies the player's Settings (S9 SaveSettings) to the audio and camera shake (C-13, C-18): live while a slider
    /// moves (SettingsScreen.Applied) and once at startup from the save. Sits on the _Audio prefab of every scene.
    /// Lives in UI because Audio cannot reference Meta/UI (asmdef direction: UI → Audio).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SettingsAudioBridge : MonoBehaviour
    {
        private Action<SaveSettings> _onApplied;
        private Action<MetaSession> _onSession;

        private void Awake()
        {
            _onApplied = Apply;
            _onSession = ApplySaved;
        }

        private void OnEnable()
        {
            SettingsScreen.Applied += _onApplied;
            MetaRuntime.SessionChanged += _onSession;
        }

        private void OnDisable()
        {
            SettingsScreen.Applied -= _onApplied;
            MetaRuntime.SessionChanged -= _onSession;
        }

        // Start, not Awake: AudioManager.Instance and FeedbackService.Instance are set in their own Awake.
        private void Start()
        {
            if (MetaRuntime.HasSession) ApplySaved(MetaRuntime.Session);
        }

        private void ApplySaved(MetaSession session)
        {
            SaveSettings settings = session != null && session.Data != null ? session.Data.settings : null;
            if (settings != null) Apply(settings);
        }

        /// <summary>Pushes every audio-related setting; cheap, so it runs on each slider step.</summary>
        public static void Apply(SaveSettings settings)
        {
            if (settings == null) return;
            AudioManager audio = AudioManager.Instance;
            if (audio != null)
            {
                audio.SetVolume(AudioGroup.Master, settings.master);
                audio.SetVolume(AudioGroup.Music, settings.music);
                audio.SetVolume(AudioGroup.Sfx, settings.sfx);
            }

            FeedbackService.SetShakeEnabled(settings.cameraShake);
        }
    }
}
