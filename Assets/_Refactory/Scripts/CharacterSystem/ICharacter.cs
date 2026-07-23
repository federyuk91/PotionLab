
using UnityEngine;

namespace CharacterSystem
{
    public interface ICharacter
    {
        CharacterType GetCharacterForm();
        void Drunk(PotionScript potion);

        void OnEnterTransformation();
        void OnExitTransformation();
    }
}
