using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

namespace CharacterSystem
{
    public class CharacterSpells : MonoBehaviour
    {
        [Header("UI visualization")]
        //Animazione pergamene da 0 a 2
        public Animator[] spellsAnimator;
        //Immagine sulle pergamene da 0 a 2
        public Image[] spellsImage;
        //Immagine sulle pergamene da 0 a 2
        public Text[] spellsCost;

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
            for (int i = 0; i < 3; i++)
            {
                spellsImage[i].sprite = Character.spellList[i].sprite;
            }

            RefreshSpellAvailability();
        }

        private void RefreshSpellAvailability()
        {
            for (int i = 0; i < 3; i++)
            {
                spellsAnimator[i].SetBool("isOpen", characterStats.HasMana(Character.spellList[i].cost));
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


