using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class PotionPoolEntry
{
    public GameObject Prefab;
    public int PreloadCount;
}

public class PotionPool : MonoBehaviour
{
    [SerializeField] private Transform inactiveContainer;
    [SerializeField] private List<PotionPoolEntry> preloadedPotions = new List<PotionPoolEntry>();

    private readonly Dictionary<GameObject, Queue<PotionScript>> availablePotions = new Dictionary<GameObject, Queue<PotionScript>>();
    private bool missingPrefabWarningShown;

    private void Awake()
    {
        if (inactiveContainer == null)
        {
            inactiveContainer = transform;
            Debug.LogWarning($"{name}: PotionPool inactive container is missing. Using this transform; assign it in Inspector before production.", this);
        }

        PreloadConfiguredPotions();
    }

    public PotionScript Get(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
        {
            WarnMissingPrefab();
            return null;
        }

        Queue<PotionScript> queue = GetQueue(prefab);
        PotionScript potion = queue.Count > 0 ? queue.Dequeue() : CreatePotion(prefab);

        if (potion == null)
        {
            return null;
        }

        PreparePotionForSpawn(potion, position, rotation);
        return potion;
    }

    public void Release(PooledPotion pooledPotion)
    {
        if (pooledPotion == null || pooledPotion.Potion == null || pooledPotion.Prefab == null)
        {
            return;
        }

        PotionScript potion = pooledPotion.Potion;
        potion.ResetRuntimeStateForPool();
        potion.transform.SetParent(inactiveContainer);
        potion.gameObject.SetActive(false);

        Queue<PotionScript> queue = GetQueue(pooledPotion.Prefab);
        if (!queue.Contains(potion))
        {
            queue.Enqueue(potion);
        }
    }

    private void PreloadConfiguredPotions()
    {
        foreach (PotionPoolEntry entry in preloadedPotions)
        {
            if (entry == null || entry.Prefab == null || entry.PreloadCount <= 0)
            {
                continue;
            }

            Queue<PotionScript> queue = GetQueue(entry.Prefab);
            for (int index = 0; index < entry.PreloadCount; index++)
            {
                PotionScript potion = CreatePotion(entry.Prefab);
                if (potion != null)
                {
                    potion.gameObject.SetActive(false);
                    queue.Enqueue(potion);
                }
            }
        }
    }

    private PotionScript CreatePotion(GameObject prefab)
    {
        GameObject potionObject = Instantiate(prefab, inactiveContainer);
        PotionScript potion = potionObject.GetComponent<PotionScript>();
        if (potion == null)
        {
            Debug.LogWarning($"{name}: pooled prefab '{prefab.name}' has no PotionScript component.", this);
            Destroy(potionObject);
            return null;
        }

        PooledPotion pooledPotion = potionObject.GetComponent<PooledPotion>();
        if (pooledPotion == null)
        {
            pooledPotion = potionObject.AddComponent<PooledPotion>();
        }

        pooledPotion.Initialize(this, prefab, potion);
        potion.ResetRuntimeStateForPool();
        return potion;
    }

    private void PreparePotionForSpawn(PotionScript potion, Vector3 position, Quaternion rotation)
    {
        Transform potionTransform = potion.transform;
        potionTransform.SetParent(null);
        potionTransform.SetPositionAndRotation(position, rotation);
        potion.ResetRuntimeStateForPool();
        potion.gameObject.SetActive(true);
    }

    private Queue<PotionScript> GetQueue(GameObject prefab)
    {
        if (!availablePotions.TryGetValue(prefab, out Queue<PotionScript> queue))
        {
            queue = new Queue<PotionScript>();
            availablePotions.Add(prefab, queue);
        }

        return queue;
    }

    private void WarnMissingPrefab()
    {
        if (missingPrefabWarningShown)
        {
            return;
        }

        missingPrefabWarningShown = true;
        Debug.LogWarning($"{name}: PotionPool received a missing prefab. Check EndlessPhaseSettings potion references.", this);
    }
}
