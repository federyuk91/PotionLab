using UnityEngine;

namespace EndlessSystem
{
    public class EndlessAudioController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField] private EndlessManager endlessManager;
        [SerializeField] private EndlessEventController eventController;
        [SerializeField] private AudioSource audioSource;

        [Header("Clips")]
        [SerializeField] private AudioClip phaseEventClip;
        [SerializeField] private AudioClip overflowBombClip;
        [SerializeField] private AudioClip lightLevelSetClip;

        private bool missingAudioSourceWarningShown;

        private void OnEnable()
        {
            if (endlessManager != null)
            {
                endlessManager.PhaseEventTriggered += HandlePhaseEventTriggered;
                endlessManager.OverflowBombTriggered += HandleOverflowBombTriggered;
            }
            else
            {
                Debug.LogWarning($"AUDIO: {name} EndlessManager reference is missing. Assign it in Inspector to play endless phase audio.", this);
            }

            if (eventController != null)
            {
                eventController.LightLevelSet += HandleLightLevelSet;
            }
            else
            {
                Debug.LogWarning($"AUDIO: {name} EndlessEventController reference is missing. Assign it in Inspector to play endless event audio.", this);
            }
        }

        private void OnDisable()
        {
            if (endlessManager != null)
            {
                endlessManager.PhaseEventTriggered -= HandlePhaseEventTriggered;
                endlessManager.OverflowBombTriggered -= HandleOverflowBombTriggered;
            }

            if (eventController != null)
            {
                eventController.LightLevelSet -= HandleLightLevelSet;
            }
        }

        private void HandlePhaseEventTriggered(EndlessPhaseSettings phase)
        {
            PlayFeedback(phaseEventClip, "endless phase event");
        }

        private void HandleOverflowBombTriggered()
        {
            PlayFeedback(overflowBombClip, "endless overflow bomb");
        }

        private void HandleLightLevelSet(int lightLevel)
        {
            PlayFeedback(lightLevelSetClip, "endless light level event");
        }

        private void PlayFeedback(AudioClip clip, string context)
        {
            if (audioSource == null)
            {
                WarnMissingAudioSource();
                return;
            }

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
                return;
            }

            if (audioSource.clip != null)
            {
                audioSource.Play();
                return;
            }

            Debug.LogWarning($"AUDIO: {name} cannot play {context} because no AudioClip is assigned. Assign a clip in Inspector or set one on the AudioSource.", this);
        }

        private void WarnMissingAudioSource()
        {
            if (missingAudioSourceWarningShown)
            {
                return;
            }

            missingAudioSourceWarningShown = true;
            Debug.LogWarning($"AUDIO: {name} AudioSource reference is missing. Assign it in Inspector to play endless feedback.", this);
        }
    }
}
