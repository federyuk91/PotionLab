using System.Collections;
using System.Collections.Generic;
using InspectorValidation;
using ProgressSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Refactory.UI
{
    public sealed class MainMenuAchievementScorePanel : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField, RequiredInspectorReference] private ProgressService progressService;
        [SerializeField, RequiredInspectorReference] private AchievementDatabase achievementDatabase;
        [SerializeField, RequiredInspectorReference] private RectTransform achievementGrid;
        [SerializeField, RequiredInspectorReference] private GameObject descriptionAchievement;
        [SerializeField, RequiredInspectorReference] private TMP_Text achievementNameText;
        [SerializeField, RequiredInspectorReference] private TMP_Text descriptionAchievementText;
        [SerializeField, RequiredInspectorReference] private TMP_Text unlockedAchievementCounter;

        [Header("Hover")]
        [SerializeField, Min(1f)] private float hoverScaleMultiplier = 1.5f;

        [Header("Reveal")]
        [SerializeField, RequiredInspectorReference] private AudioSource achievementAppearAudioSource;
        [SerializeField, RequiredInspectorReference] private AudioClip achievementAppearClip;
        [SerializeField, Range(0f, 1f)] private float achievementAppearVolume = 0.22f;
        [SerializeField, Range(0.1f, 3f)] private float achievementPitchMin = 0.85f;
        [SerializeField, Range(0.1f, 3f)] private float achievementPitchMax = 1.2f;
        [SerializeField, Min(0f)] private float achievementRevealDuration = 0.18f;
        [SerializeField, Min(0f)] private float achievementRevealDelayMin = 0.03f;
        [SerializeField, Min(0f)] private float achievementRevealDelayMax = 0.055f;
        [SerializeField, Min(0f)] private float achievementRevealMaxSequenceDuration = 1.8f;
        [SerializeField, Range(0.1f, 1f)] private float achievementStartScale = 0.72f;
        [SerializeField, Range(1f, 1.5f)] private float achievementOvershootScale = 1.08f;

        private readonly List<MainMenuAchievementSlot> slots = new List<MainMenuAchievementSlot>();
        private readonly System.Random revealRandom = new System.Random();
        private bool missingSlotWarningShown;
        private string defaultAchievementTitle;
        private float achievementAudioBasePitch = 1f;
        private Coroutine revealCoroutine;

        public float HoverScaleMultiplier => hoverScaleMultiplier;

        private void Awake()
        {
            defaultAchievementTitle = achievementNameText != null ? achievementNameText.text : string.Empty;
            BuildSlotList();
            if (achievementAppearAudioSource != null)
            {
                achievementAudioBasePitch = achievementAppearAudioSource.pitch;
            }
        }

        private void OnEnable()
        {
            if (progressService != null)
            {
                progressService.ProgressChanged += HandleProgressChanged;
                progressService.AchievementUnlocked += HandleAchievementUnlocked;
            }

            Refresh();
            StartRevealAnimation();
        }

        private void OnDisable()
        {
            if (progressService != null)
            {
                progressService.ProgressChanged -= HandleProgressChanged;
                progressService.AchievementUnlocked -= HandleAchievementUnlocked;
            }

            HideDescription();
            StopRevealAnimation();
            RestoreAchievementAudioPitch();
        }

        private void HandleProgressChanged(PlayerProgress changedProgress)
        {
            Refresh();
        }

        private void HandleAchievementUnlocked(AchievementId achievementId)
        {
            Refresh();
        }

        private void BuildSlotList()
        {
            slots.Clear();
            if (achievementGrid == null)
            {
                Debug.LogError($"{name}: Achievement Grid is missing. Assign it in Inspector.", this);
                return;
            }

            foreach (Transform slotRoot in achievementGrid)
            {
                Button button = slotRoot.GetComponentInChildren<Button>(true);
                Glowing glow = slotRoot.GetComponentInChildren<Glowing>(true);
                Image achievementImage = FindAchievementImage(button);
                if (button == null || glow == null || achievementImage == null)
                {
                    Debug.LogWarning($"{slotRoot.name}: expected AchievementGlow, AchievementButton and AchievementImageOrLock references are missing.", slotRoot);
                    continue;
                }

                MainMenuAchievementSlot slot = slotRoot.GetComponent<MainMenuAchievementSlot>();
                if (slot == null)
                {
                    slot = slotRoot.gameObject.AddComponent<MainMenuAchievementSlot>();
                }

                CanvasGroup canvasGroup = slotRoot.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = slotRoot.gameObject.AddComponent<CanvasGroup>();
                }

                slot.Configure(button, achievementImage, glow, canvasGroup);
                slots.Add(slot);
            }
        }

        private static Image FindAchievementImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Image[] images = button.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != button.targetGraphic)
                {
                    return image;
                }
            }

            return null;
        }

        private void Refresh()
        {
            HideDescription();
            if (progressService == null || achievementDatabase == null)
            {
                Debug.LogError($"{name}: Progress Service and Achievement Database must be assigned to populate the achievement panel.", this);
                return;
            }

            if (slots.Count == 0)
            {
                BuildSlotList();
            }

            IReadOnlyList<AchievementDatabase.AchievementDefinition> achievements = achievementDatabase.Achievements;
            int unlockedCount = 0;
            int displayedAchievementCount = 0;
            for (int index = 0; index < achievements.Count; index++)
            {
                AchievementDatabase.AchievementDefinition definition = achievements[index];
                if (definition == null || definition.id == AchievementId.None)
                {
                    continue;
                }

                bool isUnlocked = progressService.IsAchievementUnlocked(definition.id);
                if (isUnlocked)
                {
                    unlockedCount++;
                }

                if (displayedAchievementCount >= slots.Count)
                {
                    WarnMissingSlots();
                    continue;
                }

                slots[displayedAchievementCount].Bind(this, definition, isUnlocked);
                displayedAchievementCount++;
            }

            for (int index = displayedAchievementCount; index < slots.Count; index++)
            {
                slots[index].gameObject.SetActive(false);
            }

            if (unlockedAchievementCounter != null)
            {
                unlockedAchievementCounter.text = $"{unlockedCount}/{displayedAchievementCount}";
            }
        }

        public void ShowDescription(AchievementDatabase.AchievementDefinition definition)
        {
            if (definition == null || descriptionAchievement == null)
            {
                return;
            }

            if (achievementNameText != null)
            {
                achievementNameText.text = definition.displayName;
            }

            if (descriptionAchievementText != null)
            {
                descriptionAchievementText.text = definition.description;
            }

            descriptionAchievement.SetActive(true);
        }

        public void HideDescription()
        {
            if (achievementNameText != null)
            {
                achievementNameText.text = defaultAchievementTitle;
            }

            if (descriptionAchievementText != null)
            {
                descriptionAchievementText.text = string.Empty;
            }

            if (descriptionAchievement != null)
            {
                descriptionAchievement.SetActive(true);
            }
        }

        private void WarnMissingSlots()
        {
            if (missingSlotWarningShown)
            {
                return;
            }

            missingSlotWarningShown = true;
            Debug.LogWarning($"{name}: Achievement Grid has fewer slots than the Achievement Database. Add slots to display every achievement.", this);
        }

        private void StartRevealAnimation()
        {
            StopRevealAnimation();

            List<MainMenuAchievementSlot> visibleSlots = new List<MainMenuAchievementSlot>();
            foreach (MainMenuAchievementSlot slot in slots)
            {
                if (slot != null && slot.gameObject.activeSelf)
                {
                    slot.PrepareReveal(achievementStartScale);
                    visibleSlots.Add(slot);
                }
            }

            if (visibleSlots.Count == 0)
            {
                return;
            }

            Shuffle(visibleSlots);
            revealCoroutine = StartCoroutine(RevealAchievements(visibleSlots));
        }

        private void StopRevealAnimation()
        {
            if (revealCoroutine == null)
            {
                return;
            }

            StopCoroutine(revealCoroutine);
            revealCoroutine = null;
            foreach (MainMenuAchievementSlot slot in slots)
            {
                if (slot != null)
                {
                    slot.CompleteReveal();
                }
            }
        }

        private IEnumerator RevealAchievements(List<MainMenuAchievementSlot> visibleSlots)
        {
            List<float> startTimes = BuildRevealStartTimes(visibleSlots.Count);
            bool[] audioPlayed = new bool[visibleSlots.Count];
            float sequenceDuration = startTimes[startTimes.Count - 1] + achievementRevealDuration;
            float elapsed = 0f;

            while (elapsed < sequenceDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                for (int index = 0; index < visibleSlots.Count; index++)
                {
                    float localElapsed = elapsed - startTimes[index];
                    if (localElapsed < 0f)
                    {
                        continue;
                    }

                    if (!audioPlayed[index])
                    {
                        audioPlayed[index] = true;
                        PlayAchievementAppearAudio();
                    }

                    float progress = achievementRevealDuration <= 0f
                        ? 1f
                        : Mathf.Clamp01(localElapsed / achievementRevealDuration);
                    visibleSlots[index].ApplyReveal(progress, achievementStartScale, achievementOvershootScale);
                }

                yield return null;
            }

            foreach (MainMenuAchievementSlot slot in visibleSlots)
            {
                slot.CompleteReveal();
            }

            RestoreAchievementAudioPitch();
            revealCoroutine = null;
        }

        private List<float> BuildRevealStartTimes(int slotCount)
        {
            List<float> delays = new List<float>();
            float totalDelay = 0f;
            float minimumDelay = Mathf.Min(achievementRevealDelayMin, achievementRevealDelayMax);
            float maximumDelay = Mathf.Max(achievementRevealDelayMin, achievementRevealDelayMax);

            for (int index = 0; index < slotCount - 1; index++)
            {
                float delay = Mathf.Lerp(minimumDelay, maximumDelay, (float)revealRandom.NextDouble());
                delays.Add(delay);
                totalDelay += delay;
            }

            if (totalDelay > achievementRevealMaxSequenceDuration && totalDelay > 0f)
            {
                float delayScale = achievementRevealMaxSequenceDuration / totalDelay;
                for (int index = 0; index < delays.Count; index++)
                {
                    delays[index] *= delayScale;
                }
            }

            List<float> startTimes = new List<float> { 0f };
            foreach (float delay in delays)
            {
                startTimes.Add(startTimes[startTimes.Count - 1] + delay);
            }

            return startTimes;
        }

        private void PlayAchievementAppearAudio()
        {
            if (achievementAppearAudioSource == null || achievementAppearClip == null)
            {
                return;
            }

            float minimumPitch = Mathf.Min(achievementPitchMin, achievementPitchMax);
            float maximumPitch = Mathf.Max(achievementPitchMin, achievementPitchMax);
            achievementAppearAudioSource.pitch = Mathf.Lerp(minimumPitch, maximumPitch, (float)revealRandom.NextDouble());
            achievementAppearAudioSource.PlayOneShot(achievementAppearClip, achievementAppearVolume);
        }

        private void RestoreAchievementAudioPitch()
        {
            if (achievementAppearAudioSource != null)
            {
                achievementAppearAudioSource.pitch = achievementAudioBasePitch;
            }
        }

        private void Shuffle(List<MainMenuAchievementSlot> visibleSlots)
        {
            for (int index = visibleSlots.Count - 1; index > 0; index--)
            {
                int swapIndex = revealRandom.Next(index + 1);
                MainMenuAchievementSlot slot = visibleSlots[index];
                visibleSlots[index] = visibleSlots[swapIndex];
                visibleSlots[swapIndex] = slot;
            }
        }
    }

    public sealed class MainMenuAchievementSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MainMenuAchievementScorePanel panel;
        private AchievementDatabase.AchievementDefinition definition;
        private Image icon;
        private Button button;
        private Glowing glow;
        private CanvasGroup canvasGroup;
        private Sprite lockedSprite;
        private Color lockedColor;
        private bool lockedPresentationCached;
        private Vector3 defaultScale;
        private bool defaultScaleCached;
        private bool isUnlocked;

        public void Configure(
            Button achievementButton,
            Image achievementImage,
            Glowing achievementGlow,
            CanvasGroup achievementCanvasGroup)
        {
            button = achievementButton;
            icon = achievementImage;
            glow = achievementGlow;
            canvasGroup = achievementCanvasGroup;
            glow.SetGlowActive(false);
            CacheDefaultScale();
            SetHovered(false);
        }

        public void Bind(
            MainMenuAchievementScorePanel owner,
            AchievementDatabase.AchievementDefinition achievementDefinition,
            bool unlocked)
        {
            panel = owner;
            definition = achievementDefinition;
            isUnlocked = unlocked;

            if (icon != null)
            {
                CacheLockedPresentation();
                icon.sprite = isUnlocked ? definition.icon : lockedSprite;
                icon.enabled = icon.sprite != null;
                icon.color = isUnlocked ? Color.white : lockedColor;
            }

            if (button != null)
            {
                button.interactable = isUnlocked;
            }

            if (glow != null)
            {
                glow.SetGlowActive(isUnlocked);
            }

            gameObject.SetActive(true);
        }

        private void CacheLockedPresentation()
        {
            if (lockedPresentationCached)
            {
                return;
            }

            lockedPresentationCached = true;
            lockedSprite = icon.sprite;
            lockedColor = icon.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isUnlocked && panel != null)
            {
                SetHovered(true);
                panel.ShowDescription(definition);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHovered(false);
            if (panel != null)
            {
                panel.HideDescription();
            }
        }

        private void OnDisable()
        {
            SetHovered(false);
        }

        private void CacheDefaultScale()
        {
            if (defaultScaleCached)
            {
                return;
            }

            defaultScaleCached = true;
            defaultScale = transform.localScale;
        }

        private void SetHovered(bool hovered)
        {
            CacheDefaultScale();
            float scaleMultiplier = hovered && panel != null ? panel.HoverScaleMultiplier : 1f;
            transform.localScale = defaultScale * scaleMultiplier;
        }

        public void PrepareReveal(float startScale)
        {
            CacheDefaultScale();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
            }

            transform.localScale = defaultScale * startScale;
        }

        public void ApplyReveal(float progress, float startScale, float overshootScale)
        {
            float smoothProgress = progress * progress * (3f - 2f * progress);
            if (canvasGroup != null)
            {
                canvasGroup.alpha = smoothProgress;
            }

            float scale;
            if (progress < 0.72f)
            {
                float popProgress = Mathf.Clamp01(progress / 0.72f);
                float smoothPopProgress = popProgress * popProgress * (3f - 2f * popProgress);
                scale = Mathf.Lerp(startScale, overshootScale, smoothPopProgress);
            }
            else
            {
                float settleProgress = Mathf.Clamp01((progress - 0.72f) / 0.28f);
                float smoothSettleProgress = settleProgress * settleProgress * (3f - 2f * settleProgress);
                scale = Mathf.Lerp(overshootScale, 1f, smoothSettleProgress);
            }

            transform.localScale = defaultScale * scale;
        }

        public void CompleteReveal()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            transform.localScale = defaultScale;
        }
    }
}
