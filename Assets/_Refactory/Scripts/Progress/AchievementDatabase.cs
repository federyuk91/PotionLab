using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProgressSystem
{
    [CreateAssetMenu(fileName = "AchievementDatabase", menuName = "TheGoodNightPotion/Progress/Achievement Database")]
    public sealed class AchievementDatabase : ScriptableObject
    {
        [Serializable]
        public sealed class AchievementDefinition
        {
            public string displayName;
            public AchievementId id;
            [TextArea] public string description;
            public Sprite icon;
            public string steamApiName;
            public string googlePlayId;
        }

        [SerializeField] private List<AchievementDefinition> achievements = new List<AchievementDefinition>();

        public IReadOnlyList<AchievementDefinition> Achievements => achievements;

        public bool TryGet(AchievementId id, out AchievementDefinition definition)
        {
            foreach (AchievementDefinition candidate in achievements)
            {
                if (candidate != null && candidate.id == id)
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        private void OnValidate()
        {
            HashSet<AchievementId> knownIds = new HashSet<AchievementId>();
            foreach (AchievementDefinition definition in achievements)
            {
                if (definition == null || definition.id == AchievementId.None)
                {
                    continue;
                }

                if (!knownIds.Add(definition.id))
                {
                    Debug.LogWarning($"{name}: Achievement id '{definition.id}' is configured more than once.", this);
                }
            }
        }
    }
}
