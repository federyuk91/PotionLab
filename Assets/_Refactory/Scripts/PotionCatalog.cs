using System.Collections.Generic;
using InspectorValidation;
using UnityEngine;

namespace PotionSystem
{
    [CreateAssetMenu(fileName = "PotionCatalog", menuName = "TheGoodNightPotion/Potions/Potion Catalog")]
    public sealed class PotionCatalog : ScriptableObject
    {
        [SerializeField, RequiredInspectorReference]
        private List<PotionScriptable> potions = new List<PotionScriptable>();

        public IReadOnlyList<PotionScriptable> Potions => potions;

#if UNITY_EDITOR
        private void OnValidate()
        {
            HashSet<PotionScriptable.PotionId> knownPotionIds = new HashSet<PotionScriptable.PotionId>();
            foreach (PotionScriptable potion in potions)
            {
                if (potion == null)
                {
                    Debug.LogWarning($"{name}: catalog contains a missing potion reference.", this);
                    continue;
                }

                if (potion.Id == PotionScriptable.PotionId.None)
                {
                    Debug.LogWarning($"{name}: potion '{potion.name}' has no stable PotionId.", potion);
                    continue;
                }

                if (!knownPotionIds.Add(potion.Id))
                {
                    Debug.LogWarning($"{name}: potion family '{potion.Id}' is configured more than once.", this);
                }
            }
        }
#endif
    }
}
