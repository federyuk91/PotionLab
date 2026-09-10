using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace CharacterSystem
{
    public class CharacterSpells : MonoBehaviour
    {
        private const int SpellSlotCount = 3;

        public event Action<IReadOnlyList<Spell>, CharacterType> SpellListChanged;
        public event Action<int, Spell, bool> SpellAvailabilityChanged;

        [SerializeField] private TransformationManager transformationManager;
        [SerializeField] private CharacterStats characterStats;

        private BaseCharacter Character => transformationManager != null ? transformationManager.Current : null;
        private bool missingTransformationManagerWarningShown;
        private bool missingCharacterStatsWarningShown;
        private bool missingCurrentCharacterWarningShown;
        private bool missingLightControllerWarningShown;
        private readonly HashSet<string> missingSpellWarnings = new HashSet<string>();

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

            IReadOnlyList<Spell> spells = character.spellList;
            for (int i = 0; i < SpellSlotCount; i++)
            {
                if (spells == null || i >= spells.Count || spells[i] == null)
                {
                    WarnMissingSpell(character, i);
                    SpellAvailabilityChanged?.Invoke(i, null, false);
                    continue;
                }

                Spell spell = spells[i];
                bool isActive = CanPaySpellCost(character, spell.costo);
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

        private void WarnMissingSpell(BaseCharacter character, int index)
        {
            string warningKey = character.GetCharacterForm() + ":" + index;
            if (!missingSpellWarnings.Add(warningKey))
            {
                return;
            }

            string dataName = character.Data != null ? character.Data.name : "missing TransformationData";
            Debug.LogWarning($"{name}: {character.name} has no spell configured at index {index}. Check {dataName} in Inspector.", this);
        }

    }

    [Serializable]
    public class Spell
    {
        [FormerlySerializedAs("spellName")] public string nome;
        [FormerlySerializedAs("sprite")] public Sprite icona;
        [FormerlySerializedAs("cost")] public int costo;
        [FormerlySerializedAs("castAudio")] public AudioClip audio;
        [FormerlySerializedAs("description"), TextArea(2, 5)] public string descrizioneNormale;
        [FormerlySerializedAs("poweredDescription"), TextArea(2, 5)] public string descrizionePotenziata;
        [TextArea(1, 3)] public string descrizioneBreve;
    }

}



