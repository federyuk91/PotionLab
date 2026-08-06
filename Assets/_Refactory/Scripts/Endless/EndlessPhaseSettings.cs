using System;
using System.Collections.Generic;
using UnityEngine;

namespace EndlessSystem
{
    public enum EndlessEventType
    {
        None,
        LightVariation,
        Obstacle,
        ChangeSpeedUpperSlider,
        ChangeSpeedBottomSlider,
        SpawnFamiliar,
        Bomb,
    }

    [CreateAssetMenu(fileName = "EndlessPhaseSettings", menuName = "TheGoodNightPotion/Endless/Phase Settings", order = 1)]
    public class EndlessPhaseSettings : ScriptableObject
    {
        [Header("Event")]
        [SerializeField] private EndlessEventType eventType;
        [SerializeField] private float eventValue;

        [Header("Spawn")]
        [SerializeField] private int nextSetupAfterSpawnedPotion = 1;
        [SerializeField] private float spawnSpeedIncrement;
        [SerializeField] private List<EndlessPotionSpawnChance> spawnChanches = new List<EndlessPotionSpawnChance>();

        [Header("Debug")]
        [SerializeField] private float totalChance;

        public EndlessEventType EventType => eventType;
        public float EventValue => eventValue;
        public int NextPhaseAfterSpawnedPotions => Mathf.Max(0, nextSetupAfterSpawnedPotion);
        public float SpawnSpeedIncrement => spawnSpeedIncrement;

        public GameObject PickRandomPotionPrefab()
        {
            if (spawnChanches == null || spawnChanches.Count == 0)
            {
                Debug.LogWarning($"{name}: Endless phase has no potion chances configured.");
                return null;
            }

            float total = CalculateTotalChance();
            if (total <= 0f)
            {
                Debug.LogWarning($"{name}: Endless phase has no positive potion chance configured.");
                return null;
            }

            float roll = UnityEngine.Random.Range(0f, total);
            float currentChance = 0f;

            foreach (EndlessPotionSpawnChance potionChance in spawnChanches)
            {
                if (potionChance == null || potionChance.Potion == null || potionChance.Chance <= 0f)
                {
                    continue;
                }

                currentChance += potionChance.Chance;
                if (roll <= currentChance)
                {
                    return potionChance.Potion.gameObject;
                }
            }

            return null;
        }

        private float CalculateTotalChance()
        {
            float total = 0f;

            foreach (EndlessPotionSpawnChance potionChance in spawnChanches)
            {
                if (potionChance == null || potionChance.Potion == null)
                {
                    continue;
                }

                total += Mathf.Max(0f, potionChance.Chance);
            }

            return total;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            totalChance = CalculateTotalChance();

            foreach (EndlessPotionSpawnChance potionChance in spawnChanches)
            {
                if (potionChance != null)
                {
                    potionChance.RefreshEditorName();
                }
            }
        }
#endif
    }

    [Serializable]
    public class EndlessPotionSpawnChance
    {
        [SerializeField] private string name;
        [SerializeField] private PotionScript potion;
        [SerializeField] private float chance = 10f;

        public PotionScript Potion => potion;
        public float Chance => chance;

#if UNITY_EDITOR
        public void RefreshEditorName()
        {
            if (potion == null)
            {
                name = $"Missing Potion {chance}%";
                return;
            }

            name = $"{potion.name.Replace("Potion", string.Empty)} {chance}%";
        }
#endif
    }
}
