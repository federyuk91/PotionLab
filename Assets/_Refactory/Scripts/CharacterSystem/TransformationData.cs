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
        [SerializeField] private Sprite[] idleFrames;
        [SerializeField, Min(1f)] private float idleFrameRate = 12f;

        [Header("Description")]
        [SerializeField, TextArea(3, 8)] private string description;
        [SerializeField, TextArea(2, 5)] private string transformationMethod;
        [SerializeField] private List<Sprite> transformationPotionSprites = new List<Sprite>();
        [SerializeField, TextArea(1, 3)] private string transformationHint;
        [SerializeField, TextArea(2, 5)] private string cureMethod;
        [SerializeField] private List<Sprite> immunePotionSprites = new List<Sprite>();

        [Header("Spells")]
        [SerializeField] private List<Spell> spells = new List<Spell>(3);

        public CharacterType Id => id;
        public string TransformationName => transformationName;
        public Sprite Image => image;
        public Sprite GetIdleSprite(float elapsedSeconds)
        {
            if (idleFrames == null || idleFrames.Length == 0) return image;
            int index = Mathf.FloorToInt(Mathf.Repeat(elapsedSeconds * idleFrameRate, idleFrames.Length));
            return idleFrames[index] != null ? idleFrames[index] : image;
        }
        public string Description => description;
        public string TransformationMethod => transformationMethod;
        public IReadOnlyList<Sprite> TransformationPotionSprites => transformationPotionSprites;
        public string TransformationHint => transformationHint;
        public string CureMethod => cureMethod;
        public IReadOnlyList<Sprite> ImmunePotionSprites => immunePotionSprites;
        public IReadOnlyList<Spell> Spells => spells;
    }
}
