using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FateBastion.Meta;

namespace FateBastion.UI
{
    /// <summary>
    /// Screen_Settings (S5, S9, S10, B-15), in the lobby and the match pause menu: master / music / SFX volume, camera
    /// shake, ask before skipping a wave. Values go straight into SaveData.settings; the file is written once when the
    /// screen closes (S9 "khi đổi cài đặt"), not on every slider step.
    /// </summary>
    public sealed class SettingsScreen : UIScreen
    {
        private MetaSession _session;
        private Slider _master;
        private Slider _music;
        private Slider _sfx;
        private TMP_Text _masterValue;
        private TMP_Text _musicValue;
        private TMP_Text _sfxValue;
        private Toggle _cameraShake;
        private Toggle _confirmSkip;
        private bool _dirty;
        private bool _bound;

        /// <summary>
        /// Raised on every change so audio and camera can apply it live.
        /// STUB: C nối vào AudioManager.SetVolume / rung camera (Audio không tham chiếu được Meta/UI) – issue "[Cần C]".
        /// </summary>
        public static event Action<SaveSettings> Applied;

        public void Bind(MetaSession session)
        {
            _session = session;
            if (_bound) return;
            _bound = true;

            _master = BindSlider("Panel/MasterVolumeRow", out _masterValue, v => Settings.master = v);
            _music = BindSlider("Panel/MusicVolumeRow", out _musicValue, v => Settings.music = v);
            _sfx = BindSlider("Panel/SfxVolumeRow", out _sfxValue, v => Settings.sfx = v);
            _cameraShake = BindToggle("Panel/CameraShakeToggle", v => Settings.cameraShake = v);
            _confirmSkip = BindToggle("Panel/ConfirmSkipToggle", v => Settings.skipConfirm = v);

            Button close = UiFind.Get<Button>(transform, "Panel/Buttons/CloseButton", this);
            if (close != null) close.onClick.AddListener(Close);
        }

        private SaveSettings Settings
        {
            get
            {
                SaveData data = _session.Data;
                if (data.settings == null) data.settings = new SaveSettings();
                return data.settings;
            }
        }

        protected override void OnOpened()
        {
            if (_session == null)
            {
                Debug.LogError($"{name}: SettingsScreen opened before Bind(MetaSession).", this);
                return;
            }

            SaveSettings s = Settings;
            Show(_master, _masterValue, s.master);
            Show(_music, _musicValue, s.music);
            Show(_sfx, _sfxValue, s.sfx);
            if (_cameraShake != null) _cameraShake.SetIsOnWithoutNotify(s.cameraShake);
            if (_confirmSkip != null) _confirmSkip.SetIsOnWithoutNotify(s.skipConfirm);
            _dirty = false;
        }

        protected override void OnClosed()
        {
            if (_dirty && _session != null) _session.SaveNow();
            _dirty = false;
        }

        private Slider BindSlider(string row, out TMP_Text valueText, Action<float> write)
        {
            Slider slider = UiFind.Get<Slider>(transform, row + "/Slider", this);
            TMP_Text value = UiFind.Optional<TMP_Text>(transform, row + "/ValueText");
            valueText = value;
            if (slider == null) return null;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.onValueChanged.AddListener(v =>
            {
                if (_session == null) return;
                write(v);
                if (value != null) value.text = UiText.Percent(v);
                Changed();
            });
            return slider;
        }

        private Toggle BindToggle(string path, Action<bool> write)
        {
            Toggle toggle = UiFind.Get<Toggle>(transform, path, this);
            if (toggle == null) return null;

            toggle.onValueChanged.AddListener(v =>
            {
                if (_session == null) return;
                write(v);
                Changed();
            });
            return toggle;
        }

        private static void Show(Slider slider, TMP_Text value, float v)
        {
            if (slider != null) slider.SetValueWithoutNotify(Mathf.Clamp01(v));
            if (value != null) value.text = UiText.Percent(Mathf.Clamp01(v));
        }

        private void Changed()
        {
            _dirty = true;
            Applied?.Invoke(Settings);
        }
    }
}
