using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    [CreateAssetMenu(fileName = "T_Transformation_Data", menuName = "The Good Night Potion/Transformation Data")]
    public class TransformationData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private CharacterType id;
        [SerializeField] private string transformationName;
        [SerializeField] private Sprite image;

        [Header("Description")]
        [SerializeField, TextArea(3, 8)] private string description;
        [SerializeField, TextArea(2, 5)] private string transformationMethod;
        [SerializeField, TextArea(2, 5)] private string cureMethod;
        [SerializeField] private List<Sprite> immunePotionSprites = new List<Sprite>();

        [Header("Spells")]
        [SerializeField] private List<Spell> spells = new List<Spell>(3);

        public CharacterType Id => id;
        public string TransformationName => transformationName;
        public Sprite Image => image;
        public string Description => description;
        public string TransformationMethod => transformationMethod;
        public string CureMethod => cureMethod;
        public IReadOnlyList<Sprite> ImmunePotionSprites => immunePotionSprites;
        public IReadOnlyList<Spell> Spells => spells;
    }
}
