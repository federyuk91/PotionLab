using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class TransformationManager : MonoBehaviour
    {
        public static TransformationManager Instance;
        [Header("Characters")]
        [SerializeField] private CharacterType startCharacter = CharacterType.Mage;
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
            }else
            {
                Instance = this;
            }

            foreach (var behaviour in characterBehaviours)
                {
                    if (behaviour is not BaseCharacter character)
                    {
                        Debug.LogError($"{behaviour.name} does not implement ICharacter");
                        continue;
                    }

                    CharacterType type = character.GetCharacterForm();
                    characters[type] = character;

                    // Disattiva tutto all’avvio
                    behaviour.gameObject.SetActive(false);
                }

            SwitchTo(startCharacter);
        }

        public void SwitchTo(CharacterType type)
        {
            if (!characters.TryGetValue(type, out var next))
            {
                Debug.LogError($"Character {type} not registered");
                return;
            }

            if (currentCharacter is MonoBehaviour currentMb)
                currentMb.gameObject.SetActive(false);

            currentCharacter = next;

            if (currentCharacter is MonoBehaviour nextMb)
                nextMb.gameObject.SetActive(true);
        }
    }
}
