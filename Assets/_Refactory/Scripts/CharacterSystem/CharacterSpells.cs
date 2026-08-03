using System;
using UnityEngine;
using System.Collections.Generic;

namespace CharacterSystem
{
    public class CharacterSpells : MonoBehaviour
    {
        public event Action<IReadOnlyList<Spell>, CharacterType> SpellListChanged;
        public event Action<int, Spell, bool> SpellAvailabilityChanged;

        [SerializeField] private TransformationManager transformationManager;
        [SerializeField] private CharacterStats characterStats;

        private BaseCharacter Character => transformationManager != null ? transformationManager.Current : null;
        private bool missingTransformationManagerWarningShown;
        private bool missingCharacterStatsWarningShown;
        private bool missingCurrentCharacterWarningShown;
        private bool missingLightControllerWarningShown;

        private void Awake()
        {
            if (transformationManager == null)
            {
                Debug.LogWarning($"{name}: TransformationManager reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
                transformationManager = GetComponent<TransformationManager>();
            }

            if (characterStats == null)
            {
                Debug.LogWarning($"{name}: CharacterStats reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
                characterStats = GetComponent<CharacterStats>();
            }

            if (characterStats != null)
            {
                characterStats.OnManaDown += OnManaChange;
                characterStats.OnManaUp += OnManaChange;
                characterStats.OnHealtDown += OnHealthChange;
                characterStats.OnHealtUp += OnHealthChange;
            }
            else
            {
                WarnMissingCharacterStats();
            }

            if (transformationManager != null)
            {
                transformationManager.OnTransformation += OnTransformation;
            }
            else
            {
                WarnMissingTransformationManager();
            }
        }

        private void Start()
        {
            RefreshSpellUI();
        }

        private void OnDestroy()
        {
            if (characterStats != null)
            {
                characterStats.OnManaDown -= OnManaChange;
                characterStats.OnManaUp -= OnManaChange;
                characterStats.OnHealtDown -= OnHealthChange;
                characterStats.OnHealtUp -= OnHealthChange;
            }

            if (transformationManager != null)
            {
                transformationManager.OnTransformation -= OnTransformation;
            }
        }

        public void OnManaChange()
        {
            RefreshSpellAvailability();
        }

        public void OnHealthChange()
        {
            RefreshSpellAvailability();
        }

        private void OnTransformation(CharacterType fromType, CharacterType toType)
        {
            RefreshSpellUI();
        }

        private void RefreshSpellUI()
        {
            BaseCharacter character = GetCurrentCharacter();
            if (character == null)
            {
                return;
            }

            SpellListChanged?.Invoke(character.spellList, character.GetCharacterForm());

            RefreshSpellAvailability();
        }

        private void RefreshSpellAvailability()
        {
            BaseCharacter character = GetCurrentCharacter();
            if (character == null)
            {
                return;
            }

            for (int i = 0; i < 3; i++)
            {
                Spell spell = character.spellList[i];
                bool isActive = CanPaySpellCost(character, spell.cost);
                SpellAvailabilityChanged?.Invoke(i, spell, isActive);
            }
        }

        private bool CanPaySpellCost(BaseCharacter character, int cost)
        {
            if (characterStats == null)
            {
                WarnMissingCharacterStats();
                return false;
            }

            if (character.GetCharacterForm() != CharacterType.Litch)
            {
                return characterStats.HasMana(cost);
            }

            return characterStats.MP + Mathf.Max(characterStats.HP - 1, 0) >= cost;
        }

        public void OnSpell(int i)
        {
            BaseCharacter character = GetCurrentCharacter();
            if (character == null)
            {
                return;
            }

            character.Cast(i, IsPowered(character));
        }

        private bool IsPowered(BaseCharacter character)
        {
            if (transformationManager == null)
            {
                WarnMissingTransformationManager();
                return false;
            }

            if (transformationManager.lightController == null)
            {
                WarnMissingLightController();
                return false;
            }

            return transformationManager.lightController.IsPoweredFor(character.GetCharacterForm());
        }

        private BaseCharacter GetCurrentCharacter()
        {
            if (transformationManager == null)
            {
                WarnMissingTransformationManager();
                return null;
            }

            if (transformationManager.Current == null)
            {
                WarnMissingCurrentCharacter();
                return null;
            }

            return transformationManager.Current;
        }

        private void WarnMissingTransformationManager()
        {
            if (missingTransformationManagerWarningShown)
            {
                return;
            }

            missingTransformationManagerWarningShown = true;
            Debug.LogWarning($"{name}: TransformationManager reference is missing. Assign it in Inspector.", this);
        }

        private void WarnMissingCharacterStats()
        {
            if (missingCharacterStatsWarningShown)
            {
                return;
            }

            missingCharacterStatsWarningShown = true;
            Debug.LogWarning($"{name}: CharacterStats reference is missing. Assign it in Inspector.", this);
        }

        private void WarnMissingCurrentCharacter()
        {
            if (missingCurrentCharacterWarningShown)
            {
                return;
            }

            missingCurrentCharacterWarningShown = true;
            Debug.LogWarning($"{name}: TransformationManager has no current character.", this);
        }

        private void WarnMissingLightController()
        {
            if (missingLightControllerWarningShown)
            {
                return;
            }

            missingLightControllerWarningShown = true;
            Debug.LogWarning($"{name}: LightController reference is missing on TransformationManager. Assign it in Inspector.", this);
        }

    }

    [Serializable]
    public class Spell
    {
        public string spellName;
        public int cost;
        public Sprite sprite;
        public AudioClip castAudio;
    }

}


