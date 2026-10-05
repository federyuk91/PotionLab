using System.Collections;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class EndlessModifiersMenuController : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField, RequiredInspectorReference] private Button openModifiersButton;
    [SerializeField, RequiredInspectorReference] private GameObject modifiersPanel;
    [SerializeField, RequiredInspectorReference] private CanvasGroup modifiersCanvasGroup;
    [SerializeField, RequiredInspectorReference] private GameObject secondaryModifiersPanel;
    [SerializeField, RequiredInspectorReference] private CanvasGroup secondaryModifiersCanvasGroup;

    [Header("Modifiers")]
    [SerializeField, RequiredInspectorReference] private Toggle normalToggle;
    [SerializeField, RequiredInspectorReference] private Toggle hyperToggle;
    [SerializeField, RequiredInspectorReference] private Toggle hyperHyperToggle;
    [SerializeField, RequiredInspectorReference] private Toggle flawlessToggle;
    [SerializeField, RequiredInspectorReference] private TMP_Text totalModifierValueText;

    [Header("Tooltip")]
    [SerializeField, RequiredInspectorReference] private GameObject tooltipPanel;
    [SerializeField, RequiredInspectorReference] private CanvasGroup tooltipCanvasGroup;
    [SerializeField, RequiredInspectorReference] private TMP_Text tooltipTitleText;
    [SerializeField, RequiredInspectorReference] private TMP_Text tooltipDescriptionText;

    [Header("Animation")]
    [SerializeField, Min(0.05f)] private float panelAnimationDuration = 0.2f;
    [SerializeField, Range(0.5f, 1f)] private float closedPanelScale = 0.92f;
    [SerializeField, Min(0f)] private float secondaryPanelClosedOffset = 36f;
    [SerializeField, Min(0.05f)] private float tooltipAnimationDuration = 0.12f;

    [Header("Total Modifier Animation")]
    [SerializeField, Min(0.05f)] private float totalModifierAnimationDuration = 0.32f;
    [SerializeField, Min(0f)] private float totalModifierVerticalTravel = 18f;
    [SerializeField, Min(0f)] private float totalModifierScaleGrowth = 0.16f;
    [SerializeField, Min(1f)] private float totalModifierMaximumScale = 1.55f;
    [SerializeField, Min(1f)] private float totalModifierPulseScale = 1.12f;

    private Coroutine panelAnimation;
    private Coroutine tooltipAnimation;
    private Coroutine totalModifierAnimation;
    private Vector3 panelOpenScale;
    private Vector3 secondaryPanelOpenScale;
    private Vector3 tooltipOpenScale;
    private Vector2 secondaryPanelOpenPosition;
    private RectTransform secondaryPanelRectTransform;
    private Vector3 totalModifierBaseScale;
    private Vector2 totalModifierBasePosition;
    private RectTransform totalModifierRectTransform;
    private float displayedTotalMultiplier = 1f;
    private bool panelOpen;
    private Toggle fadingLightToggle;
    private Toggle nightFallToggle;
    private Toggle expensiveMagicToggle;
    private Toggle transformationFatigueToggle;
    private Toggle lingeringEffectsToggle;

    private void Awake()
    {
        panelOpenScale = modifiersPanel != null ? modifiersPanel.transform.localScale : Vector3.one;
        secondaryPanelOpenScale = secondaryModifiersPanel != null
            ? secondaryModifiersPanel.transform.localScale
            : Vector3.one;
        secondaryPanelRectTransform = secondaryModifiersPanel != null
            ? secondaryModifiersPanel.transform as RectTransform
            : null;
        secondaryPanelOpenPosition = secondaryPanelRectTransform != null
            ? secondaryPanelRectTransform.anchoredPosition
            : Vector2.zero;
        tooltipOpenScale = tooltipPanel != null ? tooltipPanel.transform.localScale : Vector3.one;
        totalModifierRectTransform = totalModifierValueText != null ? totalModifierValueText.rectTransform : null;
        totalModifierBaseScale = totalModifierRectTransform != null ? totalModifierRectTransform.localScale : Vector3.one;
        totalModifierBasePosition = totalModifierRectTransform != null ? totalModifierRectTransform.anchoredPosition : Vector2.zero;

        CreateSecondaryModifierToggles();
        BindListeners();
        RefreshFromPreferences(false);
        SetPanelOpen(false, false);
        SetTooltipVisible(false, false);
    }

    private void OnEnable()
    {
        RefreshFromPreferences(false);
    }

    private void OnDisable()
    {
        if (panelAnimation != null)
        {
            StopCoroutine(panelAnimation);
            panelAnimation = null;
        }

        if (tooltipAnimation != null)
        {
            StopCoroutine(tooltipAnimation);
            tooltipAnimation = null;
        }

        if (totalModifierAnimation != null)
        {
            StopCoroutine(totalModifierAnimation);
            totalModifierAnimation = null;
        }

        ApplyTotalModifierTransform(LevelSettings.SavedEndlessTotalScoreMultiplier);

        SetPanelOpen(false, false);
        SetTooltipVisible(false, false);
    }

    private void OnDestroy()
    {
        if (openModifiersButton != null)
        {
            openModifiersButton.onClick.RemoveListener(ToggleModifiersPanel);
        }

        if (normalToggle != null)
        {
            normalToggle.onValueChanged.RemoveListener(SetNormal);
        }

        if (hyperToggle != null)
        {
            hyperToggle.onValueChanged.RemoveListener(SetHyper);
        }

        if (hyperHyperToggle != null)
        {
            hyperHyperToggle.onValueChanged.RemoveListener(SetHyperHyper);
        }

        if (flawlessToggle != null)
        {
            flawlessToggle.onValueChanged.RemoveListener(SetFlawless);
        }

        RemoveSecondaryModifierListeners();
    }

    public void ToggleModifiersPanel()
    {
        SetPanelOpen(!panelOpen, true);
    }

    public void ShowTooltip(string title, string description)
    {
        if (tooltipTitleText != null)
        {
            tooltipTitleText.text = title;
        }

        if (tooltipDescriptionText != null)
        {
            tooltipDescriptionText.text = description;
        }

        SetTooltipVisible(true, true);
    }

    public void HideTooltip()
    {
        SetTooltipVisible(false, true);
    }

    private void BindListeners()
    {
        if (openModifiersButton == null
            || modifiersPanel == null
            || modifiersCanvasGroup == null
            || secondaryModifiersPanel == null
            || secondaryModifiersCanvasGroup == null
            || normalToggle == null
            || hyperToggle == null
            || hyperHyperToggle == null
            || flawlessToggle == null
            || fadingLightToggle == null
            || nightFallToggle == null
            || expensiveMagicToggle == null
            || transformationFatigueToggle == null
            || lingeringEffectsToggle == null
            || totalModifierValueText == null)
        {
            Debug.LogError(
                $"{name}: assign both modifier panels, the Modifiers button and every Endless modifier Toggle in the Inspector.",
                this);
            return;
        }

        openModifiersButton.onClick.AddListener(ToggleModifiersPanel);
        normalToggle.onValueChanged.AddListener(SetNormal);
        hyperToggle.onValueChanged.AddListener(SetHyper);
        hyperHyperToggle.onValueChanged.AddListener(SetHyperHyper);
        flawlessToggle.onValueChanged.AddListener(SetFlawless);
        fadingLightToggle.onValueChanged.AddListener(SetFadingLight);
        nightFallToggle.onValueChanged.AddListener(SetNightFall);
        expensiveMagicToggle.onValueChanged.AddListener(SetExpensiveMagic);
        transformationFatigueToggle.onValueChanged.AddListener(SetTransformationFatigue);
        lingeringEffectsToggle.onValueChanged.AddListener(SetLingeringEffects);
    }

    private void SetNormal(bool active)
    {
        if (!active)
        {
            return;
        }

        LevelSettings.SetSavedEndlessBaseModifier(EndlessBaseModifier.Normal);
        RefreshBaseModifierToggles(EndlessBaseModifier.Normal);
        RefreshTotalModifierValue(true);
    }

    private void SetHyper(bool active)
    {
        if (!active)
        {
            return;
        }

        LevelSettings.SetSavedEndlessBaseModifier(EndlessBaseModifier.Hyper);
        RefreshBaseModifierToggles(EndlessBaseModifier.Hyper);
        RefreshTotalModifierValue(true);
    }

    private void SetHyperHyper(bool active)
    {
        if (!active)
        {
            return;
        }

        LevelSettings.SetSavedEndlessBaseModifier(EndlessBaseModifier.HyperHyper);
        RefreshBaseModifierToggles(EndlessBaseModifier.HyperHyper);
        RefreshTotalModifierValue(true);
    }

    private void SetFlawless(bool active)
    {
        LevelSettings.SetSavedEndlessFlawlessMode(active);
        RefreshTotalModifierValue(true);
    }

    private void SetFadingLight(bool active)
    {
        LevelSettings.SetSavedEndlessFadingLight(active);
        RefreshTotalModifierValue(true);
    }

    private void SetNightFall(bool active)
    {
        LevelSettings.SetSavedEndlessNightFall(active);
        RefreshTotalModifierValue(true);
    }

    private void SetExpensiveMagic(bool active)
    {
        LevelSettings.SetSavedEndlessExpensiveMagic(active);
        RefreshTotalModifierValue(true);
    }

    private void SetTransformationFatigue(bool active)
    {
        LevelSettings.SetSavedEndlessTransformationFatigue(active);
        RefreshTotalModifierValue(true);
    }

    private void SetLingeringEffects(bool active)
    {
        LevelSettings.SetSavedEndlessLingeringEffects(active);
        RefreshTotalModifierValue(true);
    }

    private void RefreshFromPreferences(bool animateTotalModifier)
    {
        RefreshBaseModifierToggles(LevelSettings.SavedEndlessBaseModifier);

        if (flawlessToggle != null)
        {
            flawlessToggle.SetIsOnWithoutNotify(LevelSettings.SavedEndlessFlawlessMode);
        }


        SetSecondaryToggleState(fadingLightToggle, LevelSettings.SavedEndlessFadingLight);
        SetSecondaryToggleState(nightFallToggle, LevelSettings.SavedEndlessNightFall);
        SetSecondaryToggleState(expensiveMagicToggle, LevelSettings.SavedEndlessExpensiveMagic);
        SetSecondaryToggleState(transformationFatigueToggle, LevelSettings.SavedEndlessTransformationFatigue);
        SetSecondaryToggleState(lingeringEffectsToggle, LevelSettings.SavedEndlessLingeringEffects);

        RefreshTotalModifierValue(animateTotalModifier);
    }

    private void CreateSecondaryModifierToggles()
    {
        if (flawlessToggle == null)
        {
            return;
        }

        RectTransform content = flawlessToggle.transform.parent as RectTransform;
        RectTransform flawlessRect = flawlessToggle.transform as RectTransform;
        if (content == null || flawlessRect == null)
        {
            Debug.LogError($"{name}: Flawless Toggle must be inside a RectTransform content container.", this);
            return;
        }

        const float optionScale = 1.25f;
        float startY = 120f;
        float spacing = 48f;
        EndlessModifierOptionView flawlessOption = flawlessToggle.GetComponent<EndlessModifierOptionView>();
        if (flawlessOption != null)
        {
            flawlessOption.SetBaseScale(optionScale);
        }

        flawlessRect.anchoredPosition = new Vector2(flawlessRect.anchoredPosition.x, startY);
        fadingLightToggle = CreateSecondaryToggle(content, flawlessToggle, "FADING LIGHT", "x1.2", "FADING LIGHT",
            "The light fades 20% faster during the run. Score multiplier: x1.2.", startY - spacing);
        nightFallToggle = CreateSecondaryToggle(content, flawlessToggle, "NIGHT FALL", "x1.2", "NIGHT FALL",
            "Begin the run in complete darkness, with Light at 0. Score multiplier: x1.2.", startY - spacing * 2f);
        expensiveMagicToggle = CreateSecondaryToggle(content, flawlessToggle, "EXPENSIVE MAGIC", "x1.5", "EXPENSIVE MAGIC",
            "Every spell costs 1 additional MP. Score multiplier: x1.5.", startY - spacing * 3f);
        transformationFatigueToggle = CreateSecondaryToggle(content, flawlessToggle, "TRANSFORMATION FATIGUE", "x1.5", "TRANSFORMATION FATIGUE",
            "Every transformation into another form consumes 2 MP. Score multiplier: x1.5.", startY - spacing * 4f);
        lingeringEffectsToggle = CreateSecondaryToggle(content, flawlessToggle, "LINGERING EFFECTS", "x1.25", "LINGERING EFFECTS",
            "Active status effects trigger 25% more often. Score multiplier: x1.25.", startY - spacing * 5f);

        content.sizeDelta = new Vector2(Mathf.Max(content.sizeDelta.x, 180f), Mathf.Max(content.sizeDelta.y, 310f));
        if (secondaryPanelRectTransform != null)
        {
            secondaryPanelRectTransform.sizeDelta = new Vector2(
                Mathf.Max(secondaryPanelRectTransform.sizeDelta.x, 210f),
                Mathf.Max(secondaryPanelRectTransform.sizeDelta.y, 370f));
        }
    }

    private Toggle CreateSecondaryToggle(
        RectTransform parent,
        Toggle template,
        string label,
        string multiplier,
        string tooltipTitle,
        string tooltipDescription,
        float anchoredY)
    {
        Toggle createdToggle = Instantiate(template, parent);
        RectTransform createdRect = createdToggle.transform as RectTransform;
        if (createdRect != null)
        {
            createdRect.anchoredPosition = new Vector2(0f, anchoredY);
        }

        EndlessModifierOptionView optionView = createdToggle.GetComponent<EndlessModifierOptionView>();
        if (optionView != null)
        {
            optionView.Configure(this, label, multiplier, tooltipTitle, tooltipDescription);
        }

        createdToggle.SetIsOnWithoutNotify(false);
        return createdToggle;
    }

    private static void SetSecondaryToggleState(Toggle toggle, bool active)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(active);
        }
    }

    private void RemoveSecondaryModifierListeners()
    {
        if (fadingLightToggle != null) fadingLightToggle.onValueChanged.RemoveListener(SetFadingLight);
        if (nightFallToggle != null) nightFallToggle.onValueChanged.RemoveListener(SetNightFall);
        if (expensiveMagicToggle != null) expensiveMagicToggle.onValueChanged.RemoveListener(SetExpensiveMagic);
        if (transformationFatigueToggle != null) transformationFatigueToggle.onValueChanged.RemoveListener(SetTransformationFatigue);
        if (lingeringEffectsToggle != null) lingeringEffectsToggle.onValueChanged.RemoveListener(SetLingeringEffects);
    }

    private void RefreshBaseModifierToggles(EndlessBaseModifier modifier)
    {
        if (normalToggle != null)
        {
            normalToggle.SetIsOnWithoutNotify(modifier == EndlessBaseModifier.Normal);
        }

        if (hyperToggle != null)
        {
            hyperToggle.SetIsOnWithoutNotify(modifier == EndlessBaseModifier.Hyper);
        }

        if (hyperHyperToggle != null)
        {
            hyperHyperToggle.SetIsOnWithoutNotify(modifier == EndlessBaseModifier.HyperHyper);
        }
    }

    private void RefreshTotalModifierValue(bool animated)
    {
        if (totalModifierValueText == null)
        {
            return;
        }

        float totalMultiplier = LevelSettings.SavedEndlessTotalScoreMultiplier;
        totalModifierValueText.text = "x" + FormatMultiplier(totalMultiplier);

        if (!animated || totalModifierRectTransform == null || !isActiveAndEnabled)
        {
            displayedTotalMultiplier = totalMultiplier;
            ApplyTotalModifierTransform(totalMultiplier);
            return;
        }

        if (totalModifierAnimation != null)
        {
            StopCoroutine(totalModifierAnimation);
        }

        float previousMultiplier = displayedTotalMultiplier;
        displayedTotalMultiplier = totalMultiplier;
        totalModifierAnimation = StartCoroutine(AnimateTotalModifier(previousMultiplier, totalMultiplier));
    }

    private IEnumerator AnimateTotalModifier(float previousMultiplier, float targetMultiplier)
    {
        Vector2 startPosition = totalModifierRectTransform.anchoredPosition;
        Vector3 startScale = totalModifierRectTransform.localScale;
        Vector3 targetScale = GetTotalModifierScale(targetMultiplier);
        float movementDirection = targetMultiplier >= previousMultiplier ? 1f : -1f;
        float elapsed = 0f;

        while (elapsed < totalModifierAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / totalModifierAnimationDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            float verticalOffset = Mathf.Sin(progress * Mathf.PI) * totalModifierVerticalTravel * movementDirection;
            float pulse = Mathf.Sin(progress * Mathf.PI) * (totalModifierPulseScale - 1f);

            totalModifierRectTransform.anchoredPosition = Vector2.Lerp(startPosition, totalModifierBasePosition, easedProgress)
                + Vector2.up * verticalOffset;
            totalModifierRectTransform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress)
                * (1f + pulse);
            yield return null;
        }

        totalModifierRectTransform.anchoredPosition = totalModifierBasePosition;
        totalModifierRectTransform.localScale = targetScale;
        totalModifierAnimation = null;
    }

    private void ApplyTotalModifierTransform(float multiplier)
    {
        if (totalModifierRectTransform == null)
        {
            return;
        }

        totalModifierRectTransform.anchoredPosition = totalModifierBasePosition;
        totalModifierRectTransform.localScale = GetTotalModifierScale(multiplier);
    }

    private Vector3 GetTotalModifierScale(float multiplier)
    {
        float scaleMultiplier = 1f + Mathf.Max(0f, multiplier - 1f) * totalModifierScaleGrowth;
        scaleMultiplier = Mathf.Min(scaleMultiplier, totalModifierMaximumScale);
        return totalModifierBaseScale * scaleMultiplier;
    }

    private static string FormatMultiplier(float multiplier)
    {
        if (Mathf.Approximately(multiplier, Mathf.Round(multiplier)))
        {
            return Mathf.RoundToInt(multiplier).ToString();
        }

        return multiplier.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
    }

    private void SetPanelOpen(bool open, bool animated)
    {
        panelOpen = open;
        if (modifiersPanel == null
            || modifiersCanvasGroup == null
            || secondaryModifiersPanel == null
            || secondaryModifiersCanvasGroup == null)
        {
            return;
        }

        if (panelAnimation != null)
        {
            StopCoroutine(panelAnimation);
        }

        if (!animated)
        {
            modifiersPanel.SetActive(open);
            modifiersCanvasGroup.alpha = open ? 1f : 0f;
            modifiersCanvasGroup.interactable = open;
            modifiersCanvasGroup.blocksRaycasts = open;
            modifiersPanel.transform.localScale = open ? panelOpenScale : panelOpenScale * closedPanelScale;

            secondaryModifiersPanel.SetActive(open);
            secondaryModifiersCanvasGroup.alpha = open ? 1f : 0f;
            secondaryModifiersCanvasGroup.interactable = open;
            secondaryModifiersCanvasGroup.blocksRaycasts = open;
            secondaryModifiersPanel.transform.localScale = open
                ? secondaryPanelOpenScale
                : secondaryPanelOpenScale * closedPanelScale;
            ApplySecondaryPanelPosition(open ? 0f : secondaryPanelClosedOffset);
            return;
        }

        panelAnimation = StartCoroutine(AnimatePanel(open));
    }

    private IEnumerator AnimatePanel(bool open)
    {
        if (open)
        {
            modifiersPanel.SetActive(true);
            secondaryModifiersPanel.SetActive(true);
        }

        modifiersCanvasGroup.interactable = false;
        modifiersCanvasGroup.blocksRaycasts = false;
        secondaryModifiersCanvasGroup.interactable = false;
        secondaryModifiersCanvasGroup.blocksRaycasts = false;
        float startAlpha = modifiersCanvasGroup.alpha;
        float secondaryStartAlpha = secondaryModifiersCanvasGroup.alpha;
        float targetAlpha = open ? 1f : 0f;
        Vector3 startScale = modifiersPanel.transform.localScale;
        Vector3 targetScale = open ? panelOpenScale : panelOpenScale * closedPanelScale;
        Vector3 secondaryStartScale = secondaryModifiersPanel.transform.localScale;
        Vector3 secondaryTargetScale = open
            ? secondaryPanelOpenScale
            : secondaryPanelOpenScale * closedPanelScale;
        Vector2 secondaryStartPosition = secondaryPanelRectTransform != null
            ? secondaryPanelRectTransform.anchoredPosition
            : Vector2.zero;
        Vector2 secondaryTargetPosition = secondaryPanelOpenPosition
            + Vector2.right * (open ? 0f : secondaryPanelClosedOffset);
        float elapsed = 0f;

        while (elapsed < panelAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / panelAnimationDuration);
            float easedProgress = 1f - Mathf.Pow(1f - progress, 3f);
            modifiersCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            modifiersPanel.transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, easedProgress);
            secondaryModifiersCanvasGroup.alpha = Mathf.Lerp(secondaryStartAlpha, targetAlpha, easedProgress);
            secondaryModifiersPanel.transform.localScale = Vector3.LerpUnclamped(
                secondaryStartScale,
                secondaryTargetScale,
                easedProgress);
            if (secondaryPanelRectTransform != null)
            {
                secondaryPanelRectTransform.anchoredPosition = Vector2.LerpUnclamped(
                    secondaryStartPosition,
                    secondaryTargetPosition,
                    easedProgress);
            }

            yield return null;
        }

        modifiersCanvasGroup.alpha = targetAlpha;
        modifiersPanel.transform.localScale = targetScale;
        modifiersCanvasGroup.interactable = open;
        modifiersCanvasGroup.blocksRaycasts = open;
        secondaryModifiersCanvasGroup.alpha = targetAlpha;
        secondaryModifiersPanel.transform.localScale = secondaryTargetScale;
        secondaryModifiersCanvasGroup.interactable = open;
        secondaryModifiersCanvasGroup.blocksRaycasts = open;
        if (secondaryPanelRectTransform != null)
        {
            secondaryPanelRectTransform.anchoredPosition = secondaryTargetPosition;
        }

        modifiersPanel.SetActive(open);
        secondaryModifiersPanel.SetActive(open);
        panelAnimation = null;
    }

    private void ApplySecondaryPanelPosition(float closedOffset)
    {
        if (secondaryPanelRectTransform != null)
        {
            secondaryPanelRectTransform.anchoredPosition = secondaryPanelOpenPosition + Vector2.right * closedOffset;
        }
    }

    private void SetTooltipVisible(bool visible, bool animated)
    {
        if (tooltipPanel == null || tooltipCanvasGroup == null)
        {
            return;
        }

        if (tooltipAnimation != null)
        {
            StopCoroutine(tooltipAnimation);
        }

        if (!animated)
        {
            tooltipPanel.SetActive(visible);
            tooltipCanvasGroup.alpha = visible ? 1f : 0f;
            tooltipPanel.transform.localScale = visible ? tooltipOpenScale : tooltipOpenScale * 0.96f;
            return;
        }

        tooltipAnimation = StartCoroutine(AnimateTooltip(visible));
    }

    private IEnumerator AnimateTooltip(bool visible)
    {
        if (visible)
        {
            tooltipPanel.SetActive(true);
        }

        float startAlpha = tooltipCanvasGroup.alpha;
        float targetAlpha = visible ? 1f : 0f;
        Vector3 startScale = tooltipPanel.transform.localScale;
        Vector3 targetScale = visible ? tooltipOpenScale : tooltipOpenScale * 0.96f;
        float elapsed = 0f;

        while (elapsed < tooltipAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / tooltipAnimationDuration);
            tooltipCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, progress);
            tooltipPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, progress);
            yield return null;
        }

        tooltipCanvasGroup.alpha = targetAlpha;
        tooltipPanel.transform.localScale = targetScale;
        tooltipPanel.SetActive(visible);
        tooltipAnimation = null;
    }
}
