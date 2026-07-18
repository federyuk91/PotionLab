using System;
using UnityEngine;
using System.Collections.Generic;

namespace CharacterSystem
{
    public class CharacterSpells : MonoBehaviour
    {
        public event Action<IReadOnlyList<Spell>, CharacterType> SpellListChanged;
        public event Action<int, Spell, bool> SpellAvailabilityChanged;

        private TransformationManager transformationManager;
        private CharacterStats characterStats;
        private BaseCharacter Character => TransformationManager.Instance.Current;

        private void Awake()
        {
            transformationManager = GetComponent<TransformationManager>();
            characterStats = GetComponent<CharacterStats>();
            characterStats.OnManaDown += OnManaChange;
            characterStats.OnManaUp += OnManaChange;
            transformationManager.OnTransformation += OnTransformation;
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

        private void OnTransformation(CharacterType fromType, CharacterType toType)
        {
            RefreshSpellUI();
        }

        private void RefreshSpellUI()
        {
            SpellListChanged?.Invoke(Character.spellList, Character.GetCharacterForm());

            RefreshSpellAvailability();
        }

        private void RefreshSpellAvailability()
        {
            for (int i = 0; i < 3; i++)
            {
                bool isActive = characterStats.HasMana(Character.spellList[i].cost);
                SpellAvailabilityChanged?.Invoke(i, Character.spellList[i], isActive);
            }
        }

        public void OnSpell(int i)
        {
            Character.Cast(i, IsPowered());
        }

        private bool IsPowered()
        {
            return transformationManager.lightController.IsPoweredFor(Character.GetCharacterForm());
        }

    }

    [Serializable]
    public class Spell
    {
        public string spellName;
        public int cost;
        public Sprite sprite;
    }

}


