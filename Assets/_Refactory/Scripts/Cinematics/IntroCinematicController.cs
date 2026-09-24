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
    public sealed class IntroCinematicController : MonoBehaviour
    {
        private const double TimelineClipTolerance = 0.0001d;

        [Serializable]
        private sealed class IntroDialogueLine
        {
            [TextArea(2, 4)] public string text = "Some nights begin with a single spark.";
        }

        [Header("Sequence")]
        [SerializeField, RequiredInspectorReference] private PlayableDirector director;
        [SerializeField] private bool playOnStart = true;

        [Header("Dialogue")]
        [SerializeField, RequiredInspectorReference] private GameObject dialogueRoot;
        [SerializeField, RequiredInspectorReference] private TMP_Text dialogueText;
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
        private int warnedMissingDialogueIndex = -1;

        private void Awake()
        {
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
        }

        private void UpdateDialogueFromTimeline()
        {
            if (director == null || dialogueRoot == null || dialogueText == null)
            {
                return;
            }

            int dialogueIndex = FindCurrentOrNextDialogueClipIndex();
            if (dialogueIndex < 0)
            {
                displayedDialogueIndex = -1;
                return;
            }

            if (dialogueIndex == displayedDialogueIndex)
            {
                return;
            }

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

                return;
            }

            IntroDialogueLine dialogueLine = dialogueLines[dialogueIndex];
            dialogueText.text = dialogueLine != null ? dialogueLine.text ?? string.Empty : string.Empty;
        }

        private int FindCurrentOrNextDialogueClipIndex()
        {
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
                        return index;
                    }
                }

                return -1;
            }

            return -1;
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
