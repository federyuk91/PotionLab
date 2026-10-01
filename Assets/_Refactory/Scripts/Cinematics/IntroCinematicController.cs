using System;
using System.Collections.Generic;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;

namespace Cinematics
{
    [ExecuteAlways]
    [RequireComponent(typeof(AudioSource))]
    public sealed class IntroCinematicController : MonoBehaviour
    {
        private const double TimelineClipTolerance = 0.0001d;

        [Serializable]
        private sealed class IntroDialogueLine
        {
            [TextArea(2, 4)] public string text = "Some nights begin with a single spark.";
            public AudioClip audioClip = null;
        }

        [Header("Sequence")]
        [SerializeField, RequiredInspectorReference] private PlayableDirector director;
        [SerializeField] private bool playOnStart = true;

        [Header("Dialogue")]
        [SerializeField, RequiredInspectorReference] private GameObject dialogueRoot;
        [SerializeField, RequiredInspectorReference] private TMP_Text dialogueText;
        [SerializeField] private AudioSource dialogueAudioSource;
        [SerializeField] private List<IntroDialogueLine> dialogueLines = new List<IntroDialogueLine>
        {
            new IntroDialogueLine()
        };

        [Header("Exit")]
        [SerializeField] private bool loadMainMenuOnFinish = true;
        [SerializeField] private string mainMenuSceneName = "Main Menu";
        [SerializeField] private bool allowSkip = true;

        private bool isLeavingScene;
        private int displayedDialogueIndex = -1;
        private int playingDialogueAudioIndex = -1;
        private int warnedMissingDialogueIndex = -1;

        private void Awake()
        {
            ResolveDialogueAudioSource();

            if (director == null)
            {
                Debug.LogError("IntroCinematicController requires the Playable Director Inspector reference.", this);
            }

            if (dialogueRoot == null)
            {
                Debug.LogError("IntroCinematicController requires the Dialogue Root Inspector reference.", this);
            }

            if (dialogueText == null)
            {
                Debug.LogError("IntroCinematicController requires the Dialogue Text Inspector reference.", this);
                return;
            }

            SetFirstDialogueLine();
        }

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                UpdateDialogueFromTimeline();
                return;
            }

            if (director != null)
            {
                director.stopped += HandleDirectorStopped;
            }
        }

        private void Start()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            if (playOnStart && director != null)
            {
                director.Play();
            }
        }

        private void Update()
        {
            UpdateDialogueFromTimeline();

            if (!Application.isPlaying)
            {
                return;
            }

            if (!allowSkip || isLeavingScene || SceneTransitionFader.IsTransitioning)
            {
                return;
            }

            bool skipPressed = Input.GetKeyDown(KeyCode.Escape)
                || Input.GetKeyDown(KeyCode.Space)
                || Input.GetKeyDown(KeyCode.Return)
                || Input.GetKeyDown(KeyCode.KeypadEnter)
                || Input.GetMouseButtonDown(0);
            if (skipPressed)
            {
                FinishIntro();
            }
        }

        private void OnDisable()
        {
            if (director != null)
            {
                director.stopped -= HandleDirectorStopped;
            }

            StopDialogueAudio();
        }

        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                UpdateDialogueFromTimeline();
            }
        }

        public void SetDialogueText(string line)
        {
            if (dialogueText == null)
            {
                Debug.LogWarning("IntroCinematicController cannot change dialogue because Dialogue Text is not assigned.", this);
                return;
            }

            dialogueText.text = line;
        }

        public void ShowDialogue()
        {
            SetDialogueVisible(true);
        }

        public void HideDialogue()
        {
            SetDialogueVisible(false);
        }

        public void FinishIntro()
        {
            if (isLeavingScene)
            {
                return;
            }

            isLeavingScene = true;

            if (!loadMainMenuOnFinish)
            {
                if (director != null && director.state == PlayState.Playing)
                {
                    director.Stop();
                }

                isLeavingScene = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(mainMenuSceneName))
            {
                Debug.LogError("IntroCinematicController requires a Main Menu Scene Name.", this);
                isLeavingScene = false;
                return;
            }

            if (director != null && director.state == PlayState.Playing)
            {
                // Keep the current cinematic frame visible underneath the fade.
                director.Pause();
            }

            SceneTransitionFader.LoadScene(mainMenuSceneName);
        }

        private void HandleDirectorStopped(PlayableDirector stoppedDirector)
        {
            if (!isLeavingScene)
            {
                FinishIntro();
            }
        }

        private void SetDialogueVisible(bool isVisible)
        {
            if (dialogueRoot == null)
            {
                Debug.LogWarning("IntroCinematicController cannot change dialogue visibility because Dialogue Root is not assigned.", this);
                return;
            }

            dialogueRoot.SetActive(isVisible);
            if (!isVisible)
            {
                StopDialogueAudio();
            }
        }

        private void UpdateDialogueFromTimeline()
        {
            if (director == null || dialogueRoot == null || dialogueText == null)
            {
                return;
            }

            int dialogueIndex = FindCurrentOrNextDialogueClipIndex(out bool isInsideActiveClip);
            if (dialogueIndex < 0)
            {
                displayedDialogueIndex = -1;
                StopDialogueAudio();
                return;
            }

            if (dialogueIndex != displayedDialogueIndex)
            {
                displayedDialogueIndex = dialogueIndex;
                if (dialogueLines == null || dialogueIndex >= dialogueLines.Count)
                {
                    dialogueText.text = string.Empty;
                    if (Application.isPlaying && warnedMissingDialogueIndex != dialogueIndex)
                    {
                        warnedMissingDialogueIndex = dialogueIndex;
                        Debug.LogWarning(
                            $"IntroCinematicController Active dialogue clip {dialogueIndex + 1} has no matching Dialogue Line. " +
                            "Add a line at the same position in the Dialogue Lines list.",
                            this);
                    }
                }
                else
                {
                    IntroDialogueLine dialogueLine = dialogueLines[dialogueIndex];
                    dialogueText.text = dialogueLine != null ? dialogueLine.text ?? string.Empty : string.Empty;
                }
            }

            if (!Application.isPlaying)
            {
                return;
            }

            if (isInsideActiveClip)
            {
                PlayDialogueAudio(dialogueIndex);
            }
            else
            {
                StopDialogueAudio();
            }
        }

        private int FindCurrentOrNextDialogueClipIndex(out bool isInsideActiveClip)
        {
            isInsideActiveClip = false;
            TimelineAsset timeline = director.playableAsset as TimelineAsset;
            if (timeline == null)
            {
                return -1;
            }

            double currentTime = director.time;
            foreach (TrackAsset track in timeline.GetOutputTracks())
            {
                ActivationTrack activationTrack = track as ActivationTrack;
                if (activationTrack == null || director.GetGenericBinding(activationTrack) != dialogueRoot)
                {
                    continue;
                }

                List<TimelineClip> clips = new List<TimelineClip>(activationTrack.GetClips());
                clips.Sort((TimelineClip left, TimelineClip right) => left.start.CompareTo(right.start));

                for (int index = 0; index < clips.Count; index++)
                {
                    TimelineClip clip = clips[index];
                    if (currentTime < clip.start - TimelineClipTolerance)
                    {
                        // Preload the next line while the balloon is inactive between clips.
                        return index;
                    }

                    bool isInsideClip = currentTime + TimelineClipTolerance >= clip.start
                        && currentTime < clip.end;
                    if (isInsideClip)
                    {
                        isInsideActiveClip = true;
                        return index;
                    }
                }

                return -1;
            }

            return -1;
        }

        private void PlayDialogueAudio(int dialogueIndex)
        {
            if (dialogueIndex == playingDialogueAudioIndex
                || dialogueLines == null
                || dialogueIndex < 0
                || dialogueIndex >= dialogueLines.Count)
            {
                return;
            }

            playingDialogueAudioIndex = dialogueIndex;
            IntroDialogueLine dialogueLine = dialogueLines[dialogueIndex];
            AudioClip audioClip = dialogueLine != null ? dialogueLine.audioClip : null;
            if (audioClip == null)
            {
                return;
            }

            ResolveDialogueAudioSource();
            if (dialogueAudioSource == null)
            {
                Debug.LogWarning(
                    $"IntroCinematicController cannot play audio for Dialogue Line {dialogueIndex + 1} because its AudioSource is missing.",
                    this);
                return;
            }

            dialogueAudioSource.Stop();
            dialogueAudioSource.clip = audioClip;
            dialogueAudioSource.Play();
        }

        private void StopDialogueAudio()
        {
            if (dialogueAudioSource != null && dialogueAudioSource.isPlaying)
            {
                dialogueAudioSource.Stop();
            }

            playingDialogueAudioIndex = -1;
        }

        private void ResolveDialogueAudioSource()
        {
            if (dialogueAudioSource != null)
            {
                return;
            }

            dialogueAudioSource = GetComponent<AudioSource>();
            if (dialogueAudioSource == null && Application.isPlaying)
            {
                dialogueAudioSource = gameObject.AddComponent<AudioSource>();
                dialogueAudioSource.playOnAwake = false;
                dialogueAudioSource.loop = false;
                dialogueAudioSource.spatialBlend = 0f;
            }
        }

        private void SetFirstDialogueLine()
        {
            if (dialogueText == null || dialogueLines == null || dialogueLines.Count == 0)
            {
                return;
            }

            IntroDialogueLine firstLine = dialogueLines[0];
            dialogueText.text = firstLine != null ? firstLine.text ?? string.Empty : string.Empty;
        }

    }

    internal static class IntroCinematicLaunchGate
    {
        private const string StartupSceneName = "Main Menu";
        private const string IntroSceneName = "Intro Cinematic";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void OpenIntroWhenGameStartsFromMainMenu()
        {
            Scene activeScene = SceneManager.GetActiveScene();
            if (activeScene.name != StartupSceneName)
            {
                return;
            }

            if (!Application.CanStreamedLevelBeLoaded(IntroSceneName))
            {
                Debug.LogError($"The startup cinematic scene '{IntroSceneName}' is missing from Build Settings.");
                return;
            }

            SceneManager.LoadScene(IntroSceneName);
        }
    }
}
