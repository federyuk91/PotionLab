using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem;
using InspectorValidation;
using Refactory.UI.GridList;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CharacterUIController : MonoBehaviour
{
    private static readonly int SpellOpenParameter = Animator.StringToHash("isOpen");

    [Header("Sources")]
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private CharacterStats characterStats;
    [SerializeField] private CharacterSpells characterSpells;
    [SerializeField] private CharacterStatusController statusController;
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private GameManager gameManager;
    [SerializeField] private TransformationManager transformationManager;

    [Header("Stats UI")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image mpFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private Text statPopupText;

    [Header("Level UI")]
    [SerializeField, RequiredInspectorReference] private TMP_Text currentNightText;
    [SerializeField, RequiredInspectorReference] private GridListCategoryData nightsData;

    [Header("Spell UI")]
    [SerializeField] private GameObject spellBar;
    [SerializeField] private Animator[] spellAnimators;
    [SerializeField] private Image[] spellImages;
    [SerializeField] private TextMeshProUGUI[] spellCosts;

    [Header("Status UI")]
    [SerializeField] private StatusUIEntry[] statusEntries;

    [Header("Mage Blessed/Cursed Status UI")]
    [SerializeField] private LevelStatusUIEntry blessedStatusEntry;
    [SerializeField] private LevelStatusUIEntry cursedStatusEntry;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField, RequiredInspectorReference] private TMP_Text deathText;

    [Header("Classic Score UI")]
    [SerializeField, RequiredInspectorReference] private GameObject classicResultPanel;
    [SerializeField, RequiredInspectorReference] private TMP_Text classicScoreText;
    [SerializeField, RequiredInspectorReference] private TMP_Text classicFinalMessage;
    [SerializeField] private Image[] classicScoreIcons;
    [SerializeField] private Color inactiveScoreIconColor = new Color(0.3207547f, 0.3207547f, 0.3207547f, 1f);
    [SerializeField] private Color activeScoreIconColor = Color.white;

    [Header("Classic Result Animation")]
    [SerializeField, Min(0.1f)] private float classicNumberCountDuration = 1.05f;
    [SerializeField, Min(0.05f)] private float classicIconRevealDuration = 0.24f;
    [SerializeField, Min(1f)] private float classicIconZoomScale = 1.35f;
    [SerializeField, Min(0.1f)] private float classicMessageRevealDuration = 0.35f;
    [SerializeField] private Color classicCompletedSectionColor = new Color(1f, 0.82f, 0.24f, 1f);

    private Coroutine classicResultAnimation;
    private Vector3[] classicIconBaseScales;
    [Header("Endless Score UI")]
    [FormerlySerializedAs("proceduralResultPanel")]
    [SerializeField] private GameObject endlessResultPanel;
    [SerializeField] private Text endlessCurrentScoreText;
    [FormerlySerializedAs("endlessScoreText")]
    [SerializeField] private Text endlessBestScoreText;

    [Serializable]
    private class StatusUIEntry
    {
        [SerializeField] private Status status;
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text levelText;

        public Status Status => status;
        public GameObject Root => root;
        public TMP_Text LevelText => levelText;
    }

    [Serializable]
    private class LevelStatusUIEntry
    {
        [SerializeField] private GameObject root;
        [SerializeField] private TMP_Text levelText;

        public GameObject Root => root;
        public TMP_Text LevelText => levelText;
    }

    private MageCharacter subscribedMage;

    private void Awake()
    {
        if (statusController == null)
        {
            statusController = GetComponent<CharacterStatusController>();
        }
    }

    private void OnEnable()
    {
        Subscribe();
        RefreshInitialState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (characterStats != null)
        {
            characterStats.HPChanged += RefreshHP;
            characterStats.MPChanged += RefreshMP;
            characterStats.StatPopupRequested += ShowStatPopup;
        }

        if (characterSpells != null)
        {
            characterSpells.SpellListChanged += RefreshSpells;
            characterSpells.SpellAvailabilityChanged += RefreshSpellAvailability;
        }

        if (statusController != null)
        {
            statusController.StatusAdded += RefreshStatus;
            statusController.StatusRemoved += RefreshStatus;
            statusController.StatusLevelChanged += RefreshStatus;
        }

        if (gameManager != null)
        {
            gameManager.SpellBarVisibilityChanged += SetSpellBarVisible;
            gameManager.CharacterDied += ShowDeathPanel;
            gameManager.LevelCompleted += ShowResultPanel;
        }

        if (transformationManager != null)
        {
            transformationManager.OnTransformation += OnTransformation;
        }

        SubscribeCurrentMage();
    }

    private void Unsubscribe()
    {
        if (characterStats != null)
        {
            characterStats.HPChanged -= RefreshHP;
            characterStats.MPChanged -= RefreshMP;
            characterStats.StatPopupRequested -= ShowStatPopup;
        }

        if (characterSpells != null)
        {
            characterSpells.SpellListChanged -= RefreshSpells;
            characterSpells.SpellAvailabilityChanged -= RefreshSpellAvailability;
        }

        if (statusController != null)
        {
            statusController.StatusAdded -= RefreshStatus;
            statusController.StatusRemoved -= RefreshStatus;
            statusController.StatusLevelChanged -= RefreshStatus;
        }

        if (gameManager != null)
        {
            gameManager.SpellBarVisibilityChanged -= SetSpellBarVisible;
            gameManager.CharacterDied -= ShowDeathPanel;
            gameManager.LevelCompleted -= ShowResultPanel;
        }

        if (transformationManager != null)
        {
            transformationManager.OnTransformation -= OnTransformation;
        }

        UnsubscribeCurrentMage();
    }

    private void RefreshInitialState()
    {
        if (characterStats != null)
        {
            RefreshHP(characterStats.HP, characterStats.MaxHP);
            RefreshMP(characterStats.MP, characterStats.MaxMP);
        }

        if (transformationManager != null && transformationManager.Current != null)
        {
            RefreshSpells(transformationManager.Current.spellList, transformationManager.Current.GetCharacterForm());
        }

        if (spellBar != null)
        {
            SetSpellBarVisible(spellBar.activeSelf);
        }

        SetResultPanelsVisible(false, false);
        RefreshCurrentNight();
        RefreshStatuses();
        RefreshMageStatusLevels();
    }

    private void RefreshCurrentNight()
    {
        if (currentNightText == null)
        {
            Debug.LogError($"{name}: Cannot display the current night because Current Night Text is missing.", this);
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        int nightIndex = GetNightIndex(activeScene);
        if (nightsData == null)
        {
            Debug.LogError($"{name}: Cannot display the night subtitle because Nights Data is missing.", this);
            currentNightText.text = FormatNightLabel(nightIndex, null);
            return;
        }

        foreach (GridListEntryData nightEntry in nightsData.Entries)
        {
            if (nightEntry != null && nightEntry.SceneBuildIndex == nightIndex)
            {
                currentNightText.text = FormatNightLabel(nightIndex, nightEntry.DisplayName);
                return;
            }
        }

        Debug.LogWarning($"{name}: Nights Data has no entry for scene '{activeScene.name}' (night index {nightIndex}).", this);
        currentNightText.text = FormatNightLabel(nightIndex, null);
    }

    private static string FormatNightLabel(int nightIndex, string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            return $"Night {nightIndex}";
        }

        const string separator = " - ";
        int separatorIndex = displayName.IndexOf(separator, StringComparison.Ordinal);
        string subtitle = separatorIndex >= 0
            ? displayName.Substring(separatorIndex + separator.Length).Trim()
            : displayName.Trim();

        return string.IsNullOrWhiteSpace(subtitle)
            ? $"Night {nightIndex}"
            : $"Night {nightIndex}\n\n<size=12>\"{subtitle}\"</size>";
    }

    private int GetNightIndex(Scene scene)
    {
        if (TryGetNightNumberFromSceneName(scene.name, out int nightNumber))
        {
            return nightNumber;
        }

        return scene.buildIndex;
    }

    private bool TryGetNightNumberFromSceneName(string sceneName, out int nightNumber)
    {
        nightNumber = -1;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        const string levelMarker = "Level ";
        int markerIndex = sceneName.IndexOf(levelMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
        {
            return false;
        }

        int numberStart = markerIndex + levelMarker.Length;
        int numberEnd = numberStart;
        while (numberEnd < sceneName.Length && char.IsDigit(sceneName[numberEnd]))
        {
            numberEnd++;
        }

        if (numberEnd == numberStart)
        {
            return false;
        }

        string numberText = sceneName.Substring(numberStart, numberEnd - numberStart);
        return int.TryParse(numberText, out nightNumber);
    }

    private void RefreshHP(int currentHP, int maxHP)
    {
        if (hpFill != null)
        {
            hpFill.fillAmount = GetFillAmount(currentHP, maxHP);
        }

        if (hpText != null)
        {
            hpText.text = currentHP.ToString();
        }
    }

    private void RefreshMP(int currentMP, int maxMP)
    {
        if (mpFill != null)
        {
            mpFill.fillAmount = GetFillAmount(currentMP, maxMP);
        }

        if (mpText != null)
        {
            mpText.text = currentMP.ToString();
        }
    }

    private float GetFillAmount(int currentValue, int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0f;
        }

        return Mathf.Clamp01((float)currentValue / maxValue);
    }

    private void ShowStatPopup(string text, Color color)
    {
        if (statPopupText == null)
        {
            return;
        }

        statPopupText.text = text;
        statPopupText.color = color;
        statPopupText.gameObject.SetActive(true);
    }

    private void RefreshSpells(IReadOnlyList<Spell> spells, CharacterType characterType)
    {
        int slotCount = GetSpellSlotCount(spells);

        for (int index = 0; index < slotCount; index++)
        {
            RefreshSpellSlot(index, spells[index], true);
        }
    }

    private void RefreshSpellAvailability(int index, Spell spell, bool isAvailable)
    {
        RefreshSpellSlot(index, spell, isAvailable);
    }

    private void RefreshSpellSlot(int index, Spell spell, bool isAvailable)
    {
        if (spell == null || index < 0)
        {
            return;
        }

        if (spellAnimators != null && index < spellAnimators.Length && spellAnimators[index] != null)
        {
            spellAnimators[index].SetBool(SpellOpenParameter, isAvailable);
        }

        if (spellImages != null && index < spellImages.Length && spellImages[index] != null)
        {
            spellImages[index].sprite = spell.sprite;
            spellImages[index].gameObject.SetActive(isAvailable);
        }

        if (spellCosts != null && index < spellCosts.Length && spellCosts[index] != null)
        {
            spellCosts[index].text = spell.cost.ToString();
        }
    }

    private int GetSpellSlotCount(IReadOnlyList<Spell> spells)
    {
        if (spells == null)
        {
            return 0;
        }

        int slotCount = spells.Count;

        if (spellImages != null)
        {
            slotCount = Mathf.Min(slotCount, spellImages.Length);
        }

        if (spellAnimators != null)
        {
            slotCount = Mathf.Min(slotCount, spellAnimators.Length);
        }

        if (spellCosts != null)
        {
            slotCount = Mathf.Min(slotCount, spellCosts.Length);
        }

        return slotCount;
    }

    private void SetSpellBarVisible(bool visible)
    {
        if (spellBar != null)
        {
            spellBar.SetActive(visible);
        }
    }

    private void RefreshStatuses()
    {
        if (statusEntries == null)
        {
            return;
        }

        foreach (StatusUIEntry entry in statusEntries)
        {
            RefreshStatusEntry(entry);
        }
    }

    private void RefreshStatus(Status status)
    {
        if (statusEntries == null)
        {
            return;
        }

        foreach (StatusUIEntry entry in statusEntries)
        {
            if (entry != null && entry.Status == status)
            {
                RefreshStatusEntry(entry);
            }
        }
    }

    private void RefreshStatusEntry(StatusUIEntry entry)
    {
        if (entry == null || statusController == null)
        {
            return;
        }

        bool isActive = statusController.Has(entry.Status);

        if (entry.Root != null)
        {
            entry.Root.SetActive(isActive);
        }

        int level = GetStatusLevel(entry.Status);
        bool showLevel = isActive && HasVisibleLevel(entry.Status) && level > 0;

        SetLevelText(entry.LevelText, showLevel, level);
    }

    private int GetStatusLevel(Status status)
    {
        if (statusController == null)
        {
            return 0;
        }

        return status switch
        {
            Status.Burned => statusController.fireLevel,
            Status.Grounded => statusController.groundLevel,
            Status.Algae => statusController.algaeLevel,
            Status.Poisoned => statusController.poisonLevel,
            _ => 0
        };
    }

    private bool HasVisibleLevel(Status status)
    {
        return status == Status.Burned
            || status == Status.Grounded
            || status == Status.Algae
            || status == Status.Poisoned;
    }

    private void SetLevelText(TMP_Text text, bool visible, int level)
    {
        if (text == null)
        {
            return;
        }

        text.gameObject.SetActive(visible);
        text.text = visible ? level.ToString() : string.Empty;
    }

    private void OnTransformation(CharacterType _, CharacterType __)
    {
        UnsubscribeCurrentMage();
        SubscribeCurrentMage();
        RefreshMageStatusLevels();
    }

    private void SubscribeCurrentMage()
    {
        if (transformationManager == null || transformationManager.Current is not MageCharacter mage)
        {
            return;
        }

        subscribedMage = mage;
        subscribedMage.BlessCurseLevelsChanged += RefreshMageStatusLevels;
    }

    private void UnsubscribeCurrentMage()
    {
        if (subscribedMage == null)
        {
            return;
        }

        subscribedMage.BlessCurseLevelsChanged -= RefreshMageStatusLevels;
        subscribedMage = null;
    }

    private void RefreshMageStatusLevels(int _, int __)
    {
        RefreshMageStatusLevels();
    }

    private void RefreshMageStatusLevels()
    {
        int blessLevel = subscribedMage != null ? subscribedMage.BlessLevel : 0;
        int curseLevel = subscribedMage != null ? subscribedMage.CurseLevel : 0;

        RefreshLevelStatusEntry(blessedStatusEntry, blessLevel);
        RefreshLevelStatusEntry(cursedStatusEntry, curseLevel);
    }

    private void RefreshLevelStatusEntry(LevelStatusUIEntry entry, int level)
    {
        if (entry == null)
        {
            return;
        }

        bool isActive = level > 0;
        if (entry.Root != null)
        {
            entry.Root.SetActive(isActive);
        }

        SetLevelText(entry.LevelText, isActive, level);
    }

    private void ShowDeathPanel(string deathDialog)
    {
        StopClassicResultAnimation();
        SetResultPanelsVisible(false, false);

        if (gameManager != null && !gameManager.IsPuzzleMode)
        {
            PopulateEndlessScorePanel();
            SetResultPanelsVisible(false, true);
            return;
        }

        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        if (deathText != null)
        {
            deathText.text = deathDialog;
        }
    }
    private void ShowResultPanel()
    {
        if (gameManager == null)
        {
            Debug.LogWarning($"{name}: Cannot show result panel because GameManager reference is missing.", this);
            return;
        }

        if (gameManager.IsPuzzleMode)
        {
            PopulateClassicScorePanel();
        }
        else
        {
            PopulateEndlessScorePanel();
        }

        SetResultPanelsVisible(gameManager.IsPuzzleMode, !gameManager.IsPuzzleMode);

        if (gameManager.IsPuzzleMode)
        {
            PlayClassicResultAnimation();
        }
    }

    private void PopulateClassicScorePanel()
    {
        BaseCharacter character = gameManager.Character;
        if (character == null)
        {
            Debug.LogWarning($"{name}: Cannot populate classic score panel because the active character is missing.", this);
            return;
        }

        CharacterStats stats = character.stats;
        CharacterStatusController status = character.status;
        int currentHP = stats != null ? stats.HP : 0;
        int malusCount = status != null ? status.Count() : 0;
        int maxMalus = gameManager.MaxMalusScore;
        int totalPotion = gameManager.LevelPotionTarget > 0 ? gameManager.LevelPotionTarget : gameManager.potionDrunked;

        if (classicScoreText != null)
        {
            classicScoreText.text = FormatClassicScore(
                gameManager.potionDrunked,
                totalPotion,
                currentHP,
                gameManager.BestHealthScore,
                malusCount,
                maxMalus);
        }

        int score = gameManager.CalculateClassicScorePoints();
        RefreshClassicScoreIcons(score);

        if (classicFinalMessage != null)
        {
            classicFinalMessage.text = GetClassicFinalMessage(score);
        }
    }

    private void RefreshClassicScoreIcons(int score)
    {
        if (classicScoreIcons == null)
        {
            return;
        }

        int activeIcons = Mathf.Clamp(score - 1, 0, classicScoreIcons.Length);

        for (int index = 0; index < classicScoreIcons.Length; index++)
        {
            if (classicScoreIcons[index] != null)
            {
                classicScoreIcons[index].color = index < activeIcons ? activeScoreIconColor : inactiveScoreIconColor;
            }
        }
    }

    private string GetClassicFinalMessage(int score)
    {
        switch (score)
        {
            case 1:
                return "Very bad night...";
            case 2:
                return "It's ok...";
            case 3:
                return "Nice! Not bad!";
            case 4:
                return "PERFECT NIGHT!";
            default:
                return "Very bad night...";
        }
    }

    private void PopulateEndlessScorePanel()
    {
        if (gameManager == null)
        {
            Debug.LogWarning($"{name}: Cannot populate endless score panel because GameManager reference is missing.", this);
            return;
        }


        if (endlessCurrentScoreText == null)
        {
            Debug.LogWarning($"{name}: Cannot populate endless score panel current score because Endless Current Score Text is missing. Assign it in Inspector.", this);
        }
        else
        {
            endlessCurrentScoreText.text = gameManager.potionDrunked.ToString();
        }

        if (endlessBestScoreText == null)
        {
            Debug.LogWarning($"{name}: Cannot populate endless score panel best score because Endless Best Score Text is missing. Assign it in Inspector.", this);
        }
        else
        {
            endlessBestScoreText.text = gameManager.BestProceduralScore.ToString();
        }
    }

    private void SetResultPanelsVisible(bool classicVisible, bool endlessVisible)
    {
        if (classicResultPanel != null)
        {
            classicResultPanel.SetActive(classicVisible);
        }

        if (endlessResultPanel != null)
        {
            endlessResultPanel.SetActive(endlessVisible);
        }
    }

    private void PlayClassicResultAnimation()
    {
        StopClassicResultAnimation();
        classicResultAnimation = StartCoroutine(AnimateClassicResult());
    }

    private IEnumerator AnimateClassicResult()
    {
        BaseCharacter character = gameManager != null ? gameManager.Character : null;
        if (character == null)
        {
            classicResultAnimation = null;
            yield break;
        }

        CharacterStats stats = character.stats;
        CharacterStatusController status = character.status;
        int potionCount = gameManager.potionDrunked;
        int totalPotion = gameManager.LevelPotionTarget > 0 ? gameManager.LevelPotionTarget : potionCount;
        int currentHP = stats != null ? stats.HP : 0;
        int bestHealth = gameManager.BestHealthScore;
        int malusCount = status != null ? status.Count() : 0;
        int maxMalus = gameManager.MaxMalusScore;
        int score = gameManager.CalculateClassicScorePoints();

        Color scoreTextColor = classicScoreText != null ? classicScoreText.color : Color.white;
        Color messageColor = classicFinalMessage != null ? classicFinalMessage.color : Color.white;
        Vector3 messageBaseScale = classicFinalMessage != null ? classicFinalMessage.rectTransform.localScale : Vector3.one;

        PrepareClassicIcons(score);

        if (classicFinalMessage != null)
        {
            classicFinalMessage.color = WithAlpha(messageColor, 0f);
            classicFinalMessage.rectTransform.localScale = messageBaseScale * 0.8f;
        }

        float elapsed = 0f;
        while (elapsed < classicNumberCountDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / classicNumberCountDuration);
            float easedTime = EaseOutCubic(normalizedTime);

            if (classicScoreText != null)
            {
                int displayedPotions = Mathf.RoundToInt(Mathf.Lerp(0f, potionCount, easedTime));
                int displayedHealth = Mathf.RoundToInt(Mathf.Lerp(0f, currentHP, easedTime));
                int displayedMalus = Mathf.RoundToInt(Mathf.Lerp(0f, malusCount, easedTime));
                classicScoreText.text = FormatClassicScore(displayedPotions, totalPotion, displayedHealth, bestHealth, displayedMalus, maxMalus);
                classicScoreText.color = WithAlpha(scoreTextColor, easedTime * scoreTextColor.a);
            }

            yield return null;
        }

        if (classicScoreText != null)
        {
            classicScoreText.text = FormatClassicScore(potionCount, totalPotion, currentHP, bestHealth, malusCount, maxMalus);
            classicScoreText.color = scoreTextColor;
        }

        if (classicScoreIcons != null)
        {
            int activeIcons = Mathf.Clamp(score - 1, 0, classicScoreIcons.Length);
            for (int index = 0; index < classicScoreIcons.Length; index++)
            {
                Image icon = classicScoreIcons[index];
                if (icon == null)
                {
                    continue;
                }

                Color targetColor = index < activeIcons ? activeScoreIconColor : inactiveScoreIconColor;
                Vector3 baseScale = GetClassicIconBaseScale(index);
                yield return AnimateClassicIcon(icon, baseScale, targetColor);
            }
        }

        if (classicFinalMessage != null)
        {
            elapsed = 0f;
            while (elapsed < classicMessageRevealDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / classicMessageRevealDuration);
                float scaleProgress = EaseOutBack(normalizedTime);
                classicFinalMessage.color = WithAlpha(messageColor, normalizedTime * messageColor.a);
                classicFinalMessage.rectTransform.localScale = Vector3.LerpUnclamped(messageBaseScale * 0.8f, messageBaseScale, scaleProgress);
                yield return null;
            }

            classicFinalMessage.color = messageColor;
            classicFinalMessage.rectTransform.localScale = messageBaseScale;
        }

        classicResultAnimation = null;
    }

    private void PrepareClassicIcons(int score)
    {
        if (classicScoreIcons == null)
        {
            return;
        }

        if (classicIconBaseScales == null || classicIconBaseScales.Length != classicScoreIcons.Length)
        {
            classicIconBaseScales = new Vector3[classicScoreIcons.Length];
            for (int index = 0; index < classicScoreIcons.Length; index++)
            {
                Image icon = classicScoreIcons[index];
                classicIconBaseScales[index] = icon != null ? icon.rectTransform.localScale : Vector3.one;
            }
        }

        int activeIcons = Mathf.Clamp(score - 1, 0, classicScoreIcons.Length);
        for (int index = 0; index < classicScoreIcons.Length; index++)
        {
            Image icon = classicScoreIcons[index];
            if (icon == null)
            {
                continue;
            }

            Color targetColor = index < activeIcons ? activeScoreIconColor : inactiveScoreIconColor;
            icon.gameObject.SetActive(true);
            icon.color = WithAlpha(targetColor, 0f);
            icon.rectTransform.localScale = GetClassicIconBaseScale(index) * 0.65f;
        }
    }

    private IEnumerator AnimateClassicIcon(Image icon, Vector3 baseScale, Color targetColor)
    {
        float elapsed = 0f;
        while (elapsed < classicIconRevealDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float normalizedTime = Mathf.Clamp01(elapsed / classicIconRevealDuration);
            float scaleMultiplier;

            if (normalizedTime < 0.65f)
            {
                float zoomTime = EaseOutCubic(normalizedTime / 0.65f);
                scaleMultiplier = Mathf.LerpUnclamped(0.65f, classicIconZoomScale, zoomTime);
            }
            else
            {
                float settleTime = (normalizedTime - 0.65f) / 0.35f;
                scaleMultiplier = Mathf.LerpUnclamped(classicIconZoomScale, 1f, EaseOutCubic(settleTime));
            }

            icon.color = WithAlpha(targetColor, normalizedTime * targetColor.a);
            icon.rectTransform.localScale = baseScale * scaleMultiplier;
            yield return null;
        }

        icon.color = targetColor;
        icon.rectTransform.localScale = baseScale;
    }

    private Vector3 GetClassicIconBaseScale(int index)
    {
        if (classicIconBaseScales == null || index < 0 || index >= classicIconBaseScales.Length)
        {
            return Vector3.one;
        }

        return classicIconBaseScales[index];
    }

    private void StopClassicResultAnimation()
    {
        if (classicResultAnimation == null)
        {
            return;
        }

        StopCoroutine(classicResultAnimation);
        classicResultAnimation = null;
    }

    private string FormatClassicScore(int potionCount, int totalPotion, int currentHP, int bestHealth, int malusCount, int maxMalus)
    {
        string potionLine = "Drunked: " + potionCount + "/" + totalPotion;
        string healthLine = "Health: " + currentHP + "/" + bestHealth;
        string malusLine = "Malus: " + malusCount + "/" + maxMalus;

        return HighlightCompletedSection(potionLine, totalPotion > 0 && potionCount >= totalPotion) + "\n\n"
            + HighlightCompletedSection(healthLine, bestHealth > 0 && currentHP >= bestHealth) + "\n\n"
            + HighlightCompletedSection(malusLine, malusCount <= maxMalus);
    }

    private string HighlightCompletedSection(string text, bool isCompleted)
    {
        if (!isCompleted)
        {
            return text;
        }

        string htmlColor = ColorUtility.ToHtmlStringRGB(classicCompletedSectionColor);
        return "<color=#" + htmlColor + ">" + text + "</color>";
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static float EaseOutCubic(float value)
    {
        float inverse = 1f - Mathf.Clamp01(value);
        return 1f - inverse * inverse * inverse;
    }

    private static float EaseOutBack(float value)
    {
        const float overshoot = 1.70158f;
        float shifted = Mathf.Clamp01(value) - 1f;
        return 1f + (overshoot + 1f) * shifted * shifted * shifted + overshoot * shifted * shifted;
    }

    public void OpenMainMenu()
    {
        if (!CanUseGameManager("open the main menu"))
        {
            return;
        }

        gameManager.MainMenu();
    }

    public void ReplayLevel()
    {
        if (!CanUseGameManager("replay the level"))
        {
            return;
        }

        gameManager.TryAgain();
    }

    public void ExitGame()
    {
        if (!CanUseGameManager("exit the game"))
        {
            return;
        }

        gameManager.ExitGame();
    }

    private bool CanUseGameManager(string action)
    {
        if (gameManager != null)
        {
            return true;
        }

        Debug.LogError($"{name}: Cannot {action} because the GameManager Inspector reference is missing.", this);
        return false;
    }
}
