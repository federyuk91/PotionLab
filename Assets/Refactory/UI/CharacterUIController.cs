using System.Collections.Generic;
using CharacterSystem;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUIController : MonoBehaviour
{
    private static readonly int SpellOpenParameter = Animator.StringToHash("isOpen");

    [Header("Sources")]
    [SerializeField] private CharacterStats characterStats;
    [SerializeField] private CharacterSpells characterSpells;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private TransformationManager transformationManager;

    [Header("Stats UI")]
    [SerializeField] private Image hpFill;
    [SerializeField] private Image mpFill;
    [SerializeField] private Text hpText;
    [SerializeField] private Text mpText;
    [SerializeField] private Text statPopupText;

    [Header("Spell UI")]
    [SerializeField] private GameObject spellBar;
    [SerializeField] private Animator[] spellAnimators;
    [SerializeField] private Image[] spellImages;
    [SerializeField] private Text[] spellCosts;

    [Header("Death UI")]
    [SerializeField] private GameObject deathPanel;
    [SerializeField] private Text deathText;

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

        if (gameManager != null)
        {
            gameManager.SpellBarVisibilityChanged += SetSpellBarVisible;
            gameManager.CharacterDied += ShowDeathPanel;
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

        if (gameManager != null)
        {
            gameManager.SpellBarVisibilityChanged -= SetSpellBarVisible;
            gameManager.CharacterDied -= ShowDeathPanel;
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

    private void ShowDeathPanel(string deathDialog)
    {
        if (deathPanel != null)
        {
            deathPanel.SetActive(true);
        }

        if (deathText != null)
        {
            deathText.text = deathDialog;
        }
    }
}
