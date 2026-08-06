using UnityEngine;

public class PooledPotion : MonoBehaviour
{
    public GameObject Prefab => prefab;
    public PotionScript Potion => potion;

    [SerializeField] private PotionPool pool;
    [SerializeField] private GameObject prefab;
    [SerializeField] private PotionScript potion;

    public void Initialize(PotionPool ownerPool, GameObject sourcePrefab, PotionScript potionScript)
    {
        pool = ownerPool;
        prefab = sourcePrefab;
        potion = potionScript;
    }

    public bool ReleaseToPool()
    {
        if (pool == null)
        {
            return false;
        }

        pool.Release(this);
        return true;
    }
}
