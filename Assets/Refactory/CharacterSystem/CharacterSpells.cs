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

        //Lista degli sprite da applicare alle image, cambia con la mutazione, contiene anche costo e nome della magia che può essere usato come trigger
        private List<Spell> spellList;
        private TransformationManager transformationManager;
        private CharacterStats characterStats;
        private BaseCharacter Character => TransformationManager.Instance.Current;

        private void Awake()
        {
            transformationManager = GetComponent<TransformationManager>();
            characterStats = GetComponent<CharacterStats>();
            characterStats.OnManaDown += OnManaChange;
            characterStats.OnManaUp += OnManaChange;
            transformationManager.OnTransformation += OnMageMutate;
        }

        public void OnManaChange()
        {
            //Apro o chiudo gli spell di diverso livello:
            for (int i = 0; i < 3; i++)
            {
                spellsAnimator[i].SetBool("isOpen", characterStats.HasMana(spellList[i].cost));
            }
        }

        public void OnMageMutate(CharacterType fromType, CharacterType toType)
        {
            spellList = Character.spellList;
            
            //Aggiorno la UI
            for(int i = 0; i<3; i++)
            {
                spellsImage[i].sprite = spellList[i].sprite;
                spellsAnimator[i].SetBool("isOpen", characterStats.HasMana(spellList[i].cost));
            }
        }

        public void OnSpell(int i)
        {
            Character.Cast(spellList[i], false);
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


