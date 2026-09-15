using System;
using UnityEngine;

namespace Refactory.UI
{
    // Only user preferences, never level rules or progression data.
    public static class GamePreferences
    {
        private const string Prefix = "PotionLab.Options.";
        public static float MasterVolume { get; private set; } = 1f;
        public static float MusicVolume { get; private set; } = 1f;
        public static float EffectsVolume { get; private set; } = 1f;
        public static float TextSpeed { get; private set; } = 1f;
        public static bool ShowTooltips { get; private set; } = true;
        public static bool ShowMageDialogs { get; private set; } = true;
        public static event Action Changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntime()
        {
            Changed = null;
            Application.quitting -= Save;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            MasterVolume = ReadFloat("Master", 1f, 0f, 1f);
            MusicVolume = ReadFloat("Music", 1f, 0f, 1f);
            EffectsVolume = ReadFloat("Effects", 1f, 0f, 1f);
            TextSpeed = ReadFloat("TextSpeed", 1f, 0.5f, 2f);
            ShowTooltips = PlayerPrefs.GetInt(Prefix + "Tooltips", 1) != 0;
            ShowMageDialogs = PlayerPrefs.GetInt(Prefix + "Dialogs", 1) != 0;
            if (PlayerPrefs.HasKey(Prefix + "Fullscreen"))
                Screen.fullScreen = PlayerPrefs.GetInt(Prefix + "Fullscreen") != 0;
            ApplyAudio();
            Application.quitting += Save;
        }

        private static float ReadFloat(string key, float fallback, float min, float max)
        {
            float value = PlayerPrefs.GetFloat(Prefix + key, fallback);
            return float.IsNaN(value) || float.IsInfinity(value) ? fallback : Mathf.Clamp(value, min, max);
        }

        public static void SetVolumes(float master, float music, float effects)
        {
            MasterVolume = Mathf.Clamp01(master);
            MusicVolume = Mathf.Clamp01(music);
            EffectsVolume = Mathf.Clamp01(effects);
            ApplyAudio();
            Store();
        }

        // Music sources opt out of listener gain using MusicVolumeBinding.
        private static void ApplyAudio() { AudioListener.volume = MasterVolume * EffectsVolume; }

        public static void SetTextSpeed(float value) { TextSpeed = Mathf.Clamp(value, 0.5f, 2f); Store(); }
        public static void SetTooltips(bool value) { ShowTooltips = value; Store(); }
        public static void SetMageDialogs(bool value) { ShowMageDialogs = value; Store(); }
        public static void SetFullscreen(bool value)
        {
            Screen.fullScreen = value;
            PlayerPrefs.SetInt(Prefix + "Fullscreen", value ? 1 : 0);
            Store();
        }

        public static void ResetToDefaults()
        {
            MasterVolume = MusicVolume = EffectsVolume = TextSpeed = 1f;
            ShowTooltips = ShowMageDialogs = true;
            Screen.fullScreen = true;
            PlayerPrefs.SetInt(Prefix + "Fullscreen", 1);
            ApplyAudio();
            Store();
            Save();
        }

        private static void Store()
        {
            PlayerPrefs.SetFloat(Prefix + "Master", MasterVolume);
            PlayerPrefs.SetFloat(Prefix + "Music", MusicVolume);
            PlayerPrefs.SetFloat(Prefix + "Effects", EffectsVolume);
            PlayerPrefs.SetFloat(Prefix + "TextSpeed", TextSpeed);
            PlayerPrefs.SetInt(Prefix + "Tooltips", ShowTooltips ? 1 : 0);
            PlayerPrefs.SetInt(Prefix + "Dialogs", ShowMageDialogs ? 1 : 0);
            Changed?.Invoke();
        }

        public static void Save() { PlayerPrefs.Save(); }
    }
}
