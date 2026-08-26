using System;
using System.Collections;
using System.Collections.Generic;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public sealed class ClassicMenuController : MonoBehaviour
{
    [Serializable]
    private sealed class ClassicSection
    {
        [SerializeField] private string title;
        [SerializeField] private Color titleColor = Color.white;
        [SerializeField, TextArea(2, 4)] private string description;
        [SerializeField, Min(1)] private int firstLevel = 1;
        [SerializeField, Min(1)] private int lastLevel = 1;
        [FormerlySerializedAs("environmentSprites")]
        [SerializeField] private GameObject[] environmentObjects = Array.Empty<GameObject>();

        public string Title => title;
        public Color TitleColor => titleColor;
        public string Description => description;
        public int FirstLevel => firstLevel;
        public int LastLevel => lastLevel;
        public GameObject[] EnvironmentObjects => environmentObjects;
    }

    private static readonly System.Random TransitionRandom = new System.Random();

    [Header("Shared View")]
    [SerializeField, RequiredInspectorReference] private TMP_Text sectionTitle;
    [SerializeField, RequiredInspectorReference] private TMP_Text sectionDescription;
    [SerializeField, RequiredInspectorReference] private RectTransform levelGrid;
    [SerializeField, RequiredInspectorReference] private CanvasGroup sectionCanvasGroup;
    [SerializeField, RequiredInspectorReference] private CanvasGroup[] levelButtons;

    [Header("Transition Audio")]
    [SerializeField, RequiredInspectorReference] private AudioSource buttonAppearAudioSource;
    [SerializeField, RequiredInspectorReference] private AudioClip buttonAppearClip;
    [SerializeField, Range(0.1f, 3f)] private float buttonPitchMin = 0.85f;
    [SerializeField, Range(0.1f, 3f)] private float buttonPitchMax = 1.2f;

    [Header("Transition Timing")]
    [SerializeField, Min(0f)] private float uiFadeOutDuration = 0.15f;
    [SerializeField, Min(0f)] private float uiFadeInDuration = 0.25f;
    [SerializeField, Min(0f)] private float environmentFadeDuration = 0.35f;
    [SerializeField, Min(0f)] private float buttonFadeDuration = 0.18f;
    [SerializeField, Min(0f)] private float buttonActivationDelayMin = 0.035f;
    [SerializeField, Min(0f)] private float buttonActivationDelayMax = 0.075f;
    [SerializeField, Min(0f)] private float buttonActivationMaxSequenceDuration = 0.4f;
    [SerializeField, Range(0.1f, 1f)] private float buttonStartScale = 0.72f;
    [SerializeField, Range(1f, 1.5f)] private float buttonOvershootScale = 1.08f;

    [Header("Sections")]
    [SerializeField] private ClassicSection[] sections;

    private readonly Dictionary<SpriteRenderer, Color> environmentTargetColors = new Dictionary<SpriteRenderer, Color>();
    private readonly Dictionary<ClassicSection, SpriteRenderer[]> sectionEnvironmentSprites = new Dictionary<ClassicSection, SpriteRenderer[]>();
    private Vector3[] buttonTargetScales = Array.Empty<Vector3>();
    private Coroutine sectionTransition;
    private int currentSectionIndex;
    private float buttonAudioBasePitch = 1f;
    private bool transitionLocked;

    private void Awake()
    {
        CacheButtonScales();
        CacheEnvironmentColors();

        if (buttonAppearAudioSource != null)
        {
            buttonAudioBasePitch = buttonAppearAudioSource.pitch;
        }
    }

    private void OnEnable()
    {
        currentSectionIndex = 0;
        transitionLocked = false;

        if (!ValidateReferences())
        {
            return;
        }

        HideAllEnvironmentSpritesImmediate();
        HideAllLevelButtonsImmediate();
        sectionCanvasGroup.alpha = 0f;
        sectionCanvasGroup.interactable = false;
        sectionCanvasGroup.blocksRaycasts = false;
        sectionTransition = StartCoroutine(ShowInitialSection());
    }

    private void OnDisable()
    {
        if (sectionTransition != null)
        {
            StopCoroutine(sectionTransition);
            sectionTransition = null;
        }

        transitionLocked = false;
        RestoreButtonAudioPitch();
    }

    public void ScrollLeft()
    {
        if (transitionLocked || currentSectionIndex <= 0)
        {
            return;
        }

        StartSectionTransition(currentSectionIndex - 1);
    }

    public void ScrollRight()
    {
        if (transitionLocked || sections == null || currentSectionIndex >= sections.Length - 1)
        {
            return;
        }

        StartSectionTransition(currentSectionIndex + 1);
    }

    private void StartSectionTransition(int targetSectionIndex)
    {
        if (ValidateReferences())
        {
            sectionTransition = StartCoroutine(TransitionToSection(targetSectionIndex));
        }
    }

    private IEnumerator ShowInitialSection()
    {
        transitionLocked = true;
        ApplySection(currentSectionIndex);
        ClassicSection section = sections[currentSectionIndex];
        PrepareEnvironmentSprites(section);
        yield return FadeSectionIn(section);
        yield return RevealLevelButtons(section);
        CompleteTransition();
    }

    private IEnumerator TransitionToSection(int targetSectionIndex)
    {
        transitionLocked = true;
        sectionCanvasGroup.interactable = false;
        sectionCanvasGroup.blocksRaycasts = false;

        ClassicSection outgoingSection = sections[currentSectionIndex];
        yield return FadeSectionOut(outgoingSection);
        HideEnvironmentSpritesImmediate(outgoingSection);
        HideAllLevelButtonsImmediate();

        currentSectionIndex = Mathf.Clamp(targetSectionIndex, 0, sections.Length - 1);
        ApplySection(currentSectionIndex);
        ClassicSection incomingSection = sections[currentSectionIndex];
        PrepareEnvironmentSprites(incomingSection);
        yield return FadeSectionIn(incomingSection);
        yield return RevealLevelButtons(incomingSection);
        CompleteTransition();
    }

    private void CompleteTransition()
    {
        RestoreButtonAudioPitch();
        sectionCanvasGroup.alpha = 1f;
        sectionCanvasGroup.interactable = true;
        sectionCanvasGroup.blocksRaycasts = true;
        transitionLocked = false;
        sectionTransition = null;
    }

    private void ApplySection(int sectionIndex)
    {
        ClassicSection section = sections[sectionIndex];
        sectionTitle.text = section.Title;
        sectionTitle.color = section.TitleColor;
        sectionDescription.text = section.Description;

        for (int buttonIndex = 0; buttonIndex < levelButtons.Length; buttonIndex++)
        {
            CanvasGroup levelButton = levelButtons[buttonIndex];
            if (levelButton == null)
            {
                Debug.LogError($"ClassicMenuController level button {buttonIndex + 1} is missing. Assign it in Inspector.", this);
                continue;
            }

            int levelNumber = buttonIndex + 1;
            bool belongsToSection = levelNumber >= section.FirstLevel && levelNumber <= section.LastLevel;
            levelButton.gameObject.SetActive(belongsToSection);
            levelButton.alpha = 0f;
            levelButton.interactable = false;
            levelButton.blocksRaycasts = false;
            SetButtonScale(buttonIndex, buttonStartScale);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(levelGrid);
    }

    private IEnumerator FadeSectionOut(ClassicSection section)
    {
        float duration = Mathf.Max(uiFadeOutDuration, environmentFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            sectionCanvasGroup.alpha = 1f - Smooth(GetProgress(elapsed, uiFadeOutDuration));
            SetEnvironmentFade(section, 1f - Smooth(GetProgress(elapsed, environmentFadeDuration)));
            yield return null;
        }

        sectionCanvasGroup.alpha = 0f;
        SetEnvironmentFade(section, 0f);
    }

    private IEnumerator FadeSectionIn(ClassicSection section)
    {
        float duration = Mathf.Max(uiFadeInDuration, environmentFadeDuration);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            sectionCanvasGroup.alpha = Smooth(GetProgress(elapsed, uiFadeInDuration));
            SetEnvironmentFade(section, Smooth(GetProgress(elapsed, environmentFadeDuration)));
            yield return null;
        }

        sectionCanvasGroup.alpha = 1f;
        SetEnvironmentFade(section, 1f);
    }

    private IEnumerator RevealLevelButtons(ClassicSection section)
    {
        List<int> visibleButtonIndexes = GetVisibleButtonIndexes(section);
        Shuffle(visibleButtonIndexes);
        if (visibleButtonIndexes.Count == 0)
        {
            yield break;
        }

        List<float> startTimes = BuildActivationStartTimes(visibleButtonIndexes.Count);
        bool[] audioPlayed = new bool[visibleButtonIndexes.Count];
        float sequenceDuration = startTimes[startTimes.Count - 1] + buttonFadeDuration;
        float elapsed = 0f;

        while (elapsed < sequenceDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            for (int sequenceIndex = 0; sequenceIndex < visibleButtonIndexes.Count; sequenceIndex++)
            {
                float localElapsed = elapsed - startTimes[sequenceIndex];
                if (localElapsed < 0f)
                {
                    continue;
                }

                int buttonIndex = visibleButtonIndexes[sequenceIndex];
                CanvasGroup levelButton = levelButtons[buttonIndex];
                if (!audioPlayed[sequenceIndex])
                {
                    audioPlayed[sequenceIndex] = true;
                    PlayButtonAppearAudio();
                }

                float progress = GetProgress(localElapsed, buttonFadeDuration);
                levelButton.alpha = Smooth(progress);
                SetButtonScale(buttonIndex, GetButtonScale(progress));
            }

            yield return null;
        }

        foreach (int buttonIndex in visibleButtonIndexes)
        {
            CanvasGroup levelButton = levelButtons[buttonIndex];
            levelButton.alpha = 1f;
            levelButton.interactable = true;
            levelButton.blocksRaycasts = true;
            SetButtonScale(buttonIndex, 1f);
        }
    }

    private List<int> GetVisibleButtonIndexes(ClassicSection section)
    {
        List<int> visibleButtonIndexes = new List<int>();
        for (int buttonIndex = 0; buttonIndex < levelButtons.Length; buttonIndex++)
        {
            int levelNumber = buttonIndex + 1;
            if (levelButtons[buttonIndex] != null && levelNumber >= section.FirstLevel && levelNumber <= section.LastLevel)
            {
                visibleButtonIndexes.Add(buttonIndex);
            }
        }

        return visibleButtonIndexes;
    }

    private List<float> BuildActivationStartTimes(int buttonCount)
    {
        List<float> delays = new List<float>();
        float totalDelay = 0f;
        float minimumDelay = Mathf.Min(buttonActivationDelayMin, buttonActivationDelayMax);
        float maximumDelay = Mathf.Max(buttonActivationDelayMin, buttonActivationDelayMax);

        for (int delayIndex = 0; delayIndex < buttonCount - 1; delayIndex++)
        {
            float delay = Mathf.Lerp(minimumDelay, maximumDelay, (float)TransitionRandom.NextDouble());
            delays.Add(delay);
            totalDelay += delay;
        }

        if (totalDelay > buttonActivationMaxSequenceDuration && totalDelay > 0f)
        {
            float durationScale = buttonActivationMaxSequenceDuration / totalDelay;
            for (int delayIndex = 0; delayIndex < delays.Count; delayIndex++)
            {
                delays[delayIndex] *= durationScale;
            }
        }

        List<float> startTimes = new List<float> { 0f };
        foreach (float delay in delays)
        {
            startTimes.Add(startTimes[startTimes.Count - 1] + delay);
        }

        return startTimes;
    }

    private void PlayButtonAppearAudio()
    {
        float minimumPitch = Mathf.Min(buttonPitchMin, buttonPitchMax);
        float maximumPitch = Mathf.Max(buttonPitchMin, buttonPitchMax);
        buttonAppearAudioSource.pitch = Mathf.Lerp(minimumPitch, maximumPitch, (float)TransitionRandom.NextDouble());
        buttonAppearAudioSource.PlayOneShot(buttonAppearClip);
    }

    private void CacheButtonScales()
    {
        if (levelButtons == null)
        {
            buttonTargetScales = Array.Empty<Vector3>();
            return;
        }

        buttonTargetScales = new Vector3[levelButtons.Length];
        for (int buttonIndex = 0; buttonIndex < levelButtons.Length; buttonIndex++)
        {
            CanvasGroup levelButton = levelButtons[buttonIndex];
            buttonTargetScales[buttonIndex] = levelButton != null ? levelButton.transform.localScale : Vector3.one;
        }
    }

    private void CacheEnvironmentColors()
    {
        environmentTargetColors.Clear();
        sectionEnvironmentSprites.Clear();
        if (sections == null)
        {
            return;
        }

        foreach (ClassicSection section in sections)
        {
            if (section == null)
            {
                continue;
            }

            List<SpriteRenderer> sectionSprites = new List<SpriteRenderer>();
            HashSet<SpriteRenderer> uniqueSprites = new HashSet<SpriteRenderer>();
            if (section.EnvironmentObjects != null)
            {
                foreach (GameObject environmentObject in section.EnvironmentObjects)
                {
                    if (environmentObject == null)
                    {
                        continue;
                    }

                    SpriteRenderer[] childSprites = environmentObject.GetComponentsInChildren<SpriteRenderer>(true);
                    foreach (SpriteRenderer spriteRenderer in childSprites)
                    {
                        if (spriteRenderer == null || !uniqueSprites.Add(spriteRenderer))
                        {
                            continue;
                        }

                        sectionSprites.Add(spriteRenderer);
                        if (!environmentTargetColors.ContainsKey(spriteRenderer))
                        {
                            environmentTargetColors.Add(spriteRenderer, spriteRenderer.color);
                        }
                    }
                }
            }

            sectionEnvironmentSprites.Add(section, sectionSprites.ToArray());
        }
    }

    private void PrepareEnvironmentSprites(ClassicSection section)
    {
        SetEnvironmentObjectsActive(section, true);
        foreach (SpriteRenderer spriteRenderer in GetEnvironmentSprites(section))
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = true;
                SetSpriteAlpha(spriteRenderer, 0f);
            }
        }
    }

    private void SetEnvironmentFade(ClassicSection section, float progress)
    {
        foreach (SpriteRenderer spriteRenderer in GetEnvironmentSprites(section))
        {
            if (spriteRenderer == null)
            {
                continue;
            }

            Color targetColor = environmentTargetColors.TryGetValue(spriteRenderer, out Color configuredColor)
                ? configuredColor
                : Color.white;
            targetColor.a *= Mathf.Clamp01(progress);
            spriteRenderer.color = targetColor;
        }
    }

    private void HideAllEnvironmentSpritesImmediate()
    {
        if (sections == null)
        {
            return;
        }

        foreach (ClassicSection section in sections)
        {
            if (section != null)
            {
                HideEnvironmentSpritesImmediate(section);
            }
        }
    }

    private void HideEnvironmentSpritesImmediate(ClassicSection section)
    {
        foreach (SpriteRenderer spriteRenderer in GetEnvironmentSprites(section))
        {
            if (spriteRenderer != null)
            {
                SetSpriteAlpha(spriteRenderer, 0f);
            }
        }

        SetEnvironmentObjectsActive(section, false);
    }

    private SpriteRenderer[] GetEnvironmentSprites(ClassicSection section)
    {
        if (section != null && sectionEnvironmentSprites.TryGetValue(section, out SpriteRenderer[] sprites))
        {
            return sprites;
        }

        return Array.Empty<SpriteRenderer>();
    }

    private static void SetEnvironmentObjectsActive(ClassicSection section, bool active)
    {
        if (section == null || section.EnvironmentObjects == null)
        {
            return;
        }

        foreach (GameObject environmentObject in section.EnvironmentObjects)
        {
            if (environmentObject != null)
            {
                environmentObject.SetActive(active);
            }
        }
    }

    private void HideAllLevelButtonsImmediate()
    {
        if (levelButtons == null)
        {
            return;
        }

        for (int buttonIndex = 0; buttonIndex < levelButtons.Length; buttonIndex++)
        {
            CanvasGroup levelButton = levelButtons[buttonIndex];
            if (levelButton != null)
            {
                levelButton.alpha = 0f;
                levelButton.interactable = false;
                levelButton.blocksRaycasts = false;
                levelButton.gameObject.SetActive(false);
                SetButtonScale(buttonIndex, buttonStartScale);
            }
        }
    }

    private void SetButtonScale(int buttonIndex, float scaleMultiplier)
    {
        if (buttonIndex < 0 || buttonIndex >= levelButtons.Length || levelButtons[buttonIndex] == null)
        {
            return;
        }

        Vector3 targetScale = buttonIndex < buttonTargetScales.Length ? buttonTargetScales[buttonIndex] : Vector3.one;
        levelButtons[buttonIndex].transform.localScale = targetScale * scaleMultiplier;
    }

    private float GetButtonScale(float progress)
    {
        float clampedProgress = Mathf.Clamp01(progress);
        if (clampedProgress < 0.75f)
        {
            return Mathf.Lerp(buttonStartScale, buttonOvershootScale, Smooth(clampedProgress / 0.75f));
        }

        return Mathf.Lerp(buttonOvershootScale, 1f, Smooth((clampedProgress - 0.75f) / 0.25f));
    }

    private void RestoreButtonAudioPitch()
    {
        if (buttonAppearAudioSource != null)
        {
            buttonAppearAudioSource.pitch = buttonAudioBasePitch;
        }
    }

    private static void SetSpriteAlpha(SpriteRenderer spriteRenderer, float alpha)
    {
        Color color = spriteRenderer.color;
        color.a = alpha;
        spriteRenderer.color = color;
    }

    private static float GetProgress(float elapsed, float duration)
    {
        return duration <= 0f ? 1f : Mathf.Clamp01(elapsed / duration);
    }

    private static float Smooth(float progress)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(progress));
    }

    private static void Shuffle<T>(IList<T> items)
    {
        for (int itemIndex = items.Count - 1; itemIndex > 0; itemIndex--)
        {
            int randomIndex = TransitionRandom.Next(itemIndex + 1);
            T temporaryItem = items[itemIndex];
            items[itemIndex] = items[randomIndex];
            items[randomIndex] = temporaryItem;
        }
    }

    private bool ValidateReferences()
    {
        bool referencesValid = true;
        referencesValid &= ValidateReference(sectionTitle, "Section Title");
        referencesValid &= ValidateReference(sectionDescription, "Section Description");
        referencesValid &= ValidateReference(levelGrid, "Level Grid");
        referencesValid &= ValidateReference(sectionCanvasGroup, "Section Canvas Group");
        referencesValid &= ValidateReference(buttonAppearAudioSource, "Button Appear Audio Source");
        referencesValid &= ValidateReference(buttonAppearClip, "Button Appear Clip");

        if (levelButtons == null || levelButtons.Length == 0)
        {
            Debug.LogError("ClassicMenuController requires the Level Button Canvas Group Inspector references.", this);
            referencesValid = false;
        }

        if (sections == null || sections.Length == 0)
        {
            Debug.LogError("ClassicMenuController requires at least one section in Inspector.", this);
            referencesValid = false;
        }

        return referencesValid;
    }

    private bool ValidateReference(UnityEngine.Object reference, string referenceName)
    {
        if (reference != null)
        {
            return true;
        }

        Debug.LogError($"ClassicMenuController requires the {referenceName} Inspector reference.", this);
        return false;
    }
}
