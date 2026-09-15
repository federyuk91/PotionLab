using InspectorValidation;
using UnityEngine;

namespace Refactory.UI
{
    public sealed class MusicVolumeBinding : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private AudioSource musicSource;
        private float authoredVolume;
        private bool originalIgnoreListener;

        private void Awake()
        {
            if (musicSource == null)
            {
                Debug.LogError($"{name}: assign Music Source in MusicVolumeBinding.", this);
                return;
            }
            authoredVolume = musicSource.volume;
            originalIgnoreListener = musicSource.ignoreListenerVolume;
        }

        private void OnEnable() { GamePreferences.Changed += Apply; Apply(); }
        private void OnDisable()
        {
            GamePreferences.Changed -= Apply;
            if (musicSource == null) return;
            musicSource.ignoreListenerVolume = originalIgnoreListener;
            musicSource.volume = authoredVolume;
        }

        private void Apply()
        {
            if (musicSource == null) return;
            musicSource.ignoreListenerVolume = true;
            musicSource.volume = authoredVolume * GamePreferences.MasterVolume * GamePreferences.MusicVolume;
        }
    }
}
