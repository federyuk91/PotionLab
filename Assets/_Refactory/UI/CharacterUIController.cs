using System;
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
    [SerializeField, RequiredInspectorReference] private Text currentNightText;
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
    [SerializeField] private Text deathText;

    [Header("Classic Score UI")]
    [SerializeField] private GameObject classicResultPanel;
    [SerializeField] private Text classicScoreText;
    [SerializeField] private Text classicFinalMessage;
    [SerializeField] private Image[] classicScoreIcons;
    [SerializeField] private Color inactiveScoreIconColor = new Color(0.3207547f, 0.3207547f, 0.3207547f, 1f);
    [SerializeField] private Color activeScoreIconColor = Color.white;
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
        int currentStatusCount = status != null ? status.Count() : 0;
        int totalPotion = gameManager.LevelPotionTarget > 0 ? gameManager.LevelPotionTarget : gameManager.potionDrunked;

        if (classicScoreText != null)
        {
            classicScoreText.text = "Drunked: " + gameManager.potionDrunked + "/" + totalPotion + "\n\n"
                + "Health: " + currentHP + "/" + gameManager.BestHealthScore + "\n\n"
                + "Malus: " + currentStatusCount;
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
