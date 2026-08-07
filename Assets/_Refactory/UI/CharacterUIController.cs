using System;
using System.Collections.Generic;
using CharacterSystem;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CharacterUIController : MonoBehaviour
{
    private static readonly int SpellOpenParameter = Animator.StringToHash("isOpen");

    [Header("Sources")]
    [SerializeField, RequiredInspectorReference] private CharacterStats characterStats;
    [SerializeField] private CharacterSpells characterSpells;
    [SerializeField] private CharacterStatusController statusController;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TransformationManager transformationManager;

    [Header("Stats UI")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image mpFill;
    [SerializeField] private TextMeshProUGUI hpText;
    [SerializeField] private TextMeshProUGUI mpText;
    [SerializeField] private Text statPopupText;

    [Header("Spell UI")]
    [SerializeField] private GameObject spellBar;
    [SerializeField] private Animator[] spellAnimators;
    [SerializeField] private Image[] spellImages;
    [SerializeField] private TextMeshProUGUI[] spellCosts;

    [Header("Status UI")]
    [SerializeField] private StatusUIEntry[] statusEntries;

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
        RefreshStatuses();
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
}
