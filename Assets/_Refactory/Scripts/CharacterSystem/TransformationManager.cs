using System;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class TransformationManager : MonoBehaviour
    {
        public LightController lightController;
        private DialogManager dialogManager;
        public static TransformationManager Instance;
        private CharacterType startCharacter = CharacterType.Mage;
        public event Action<CharacterType, CharacterType> OnTransformation;
        [Header("Characters")]
        public CharacterType previousForm = CharacterType.Mage;

        [SerializeField] private List<MonoBehaviour> characterBehaviours;


        private readonly Dictionary<CharacterType, BaseCharacter> characters = new();
        private BaseCharacter currentCharacter;

        public BaseCharacter Current => currentCharacter;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
                return;
            }
            else
            {
                Instance = this;
            }

            dialogManager = GetComponentInParent<DialogManager>();
            foreach (MonoBehaviour behaviour in characterBehaviours)
            {
                if (behaviour is not BaseCharacter character)
                {
                    Debug.LogError($"{behaviour.name} does not implement ICharacter");
                    continue;
                }

                CharacterType type = character.GetCharacterForm();
                characters[type] = character;

                // Disattiva tutto all'avvio
                behaviour.gameObject.SetActive(false);
            }

            SwitchTo(startCharacter);
        }

        public void SwitchTo(CharacterType type)
        {
            if (!characters.TryGetValue(type, out BaseCharacter next))
            {
                Debug.LogError($"Character {type} not registered");
                return;
            }

            if (currentCharacter == next)
            {
                return;
            }

            if (currentCharacter != null)
            {
                previousForm = currentCharacter.GetCharacterForm();
                currentCharacter.OnExitTransformation();
                currentCharacter.gameObject.SetActive(false);
            }

            currentCharacter = next;

            currentCharacter.gameObject.SetActive(true);
            currentCharacter.OnEnterTransformation();

            if (type == CharacterType.Mage)
            {
                lightController.ClearLightField();
            }

            lightController.ChangeLightColor(currentCharacter.TransformationLightColor);
            OnTransformation?.Invoke(previousForm, type);
        }
    }
}
