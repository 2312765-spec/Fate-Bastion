using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using FateBastion.Audio;
using FateBastion.Editor.UI;
using FateBastion.Meta;
using FateBastion.UI.World;

namespace FateBastion.Tests.EditMode.S11
{
    /// <summary>Settings sliders and the camera shake toggle reach audio and feedback (C-13, C-18).</summary>
    public class SettingsAudioBridgeTests
    {
        [TearDown]
        public void TearDown() => FeedbackService.SetShakeEnabled(true);

        [Test]
        public void Apply_ShakeOff_DisablesCameraShake()
        {
            SettingsAudioBridge.Apply(new SaveSettings { cameraShake = false });

            Assert.IsFalse(FeedbackService.ShakeEnabled);
        }

        [Test]
        public void Apply_ShakeOn_EnablesCameraShake()
        {
            FeedbackService.SetShakeEnabled(false);

            SettingsAudioBridge.Apply(new SaveSettings { cameraShake = true });

            Assert.IsTrue(FeedbackService.ShakeEnabled);
        }

        [Test]
        public void Apply_NullSettings_DoesNothing()
        {
            Assert.DoesNotThrow(() => SettingsAudioBridge.Apply(null));
        }

        [Test]
        public void AudioPrefab_HasSettingsBridge()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(SettingsAudioBridgeInstaller.AudioPrefabPath);
            Assert.IsNotNull(prefab);
            Assert.IsNotNull(prefab.GetComponent<SettingsAudioBridge>(),
                "_Audio thiếu SettingsAudioBridge: chạy Tools > Audio > Add Settings Bridge to _Audio.");
            Assert.IsNotNull(prefab.GetComponent<AudioManager>(), "Bridge phải nằm cạnh AudioManager.");
        }
    }
}
