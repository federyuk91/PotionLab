using UnityEngine;
using UnityEngine.Serialization;

public class AreaEffectFamiliar : MonoBehaviour
{
    public enum Effect
    {
        Destruction,
        Change,
    }

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField, FormerlySerializedAs("au")] private AudioSource audioSource;
    [SerializeField] private GameObject potionPrefab;

    [Header("Effect")]
    [SerializeField] private Effect effect;

    public void Configure(GameManager assignedGameManager)
    {
        gameManager = assignedGameManager;
    }

    public void SetEffect(Effect newEffect)
    {
        effect = newEffect;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Potion") || !other.TryGetComponent(out PotionScript potion))
        {
            return;
        }

        switch (effect)
        {
            case Effect.Change:
                ChangePotion(potion);
                break;
            case Effect.Destruction:
                DestroyPotion(potion);
                break;
        }
    }

    public void DestroyThis()
    {
        Destroy(gameObject);
    }

    private void ChangePotion(PotionScript targetPotion)
    {
        if (potionPrefab == null || !potionPrefab.TryGetComponent(out PotionScript sourcePotion))
        {
            Debug.LogWarning($"{name}: Potion Prefab must reference a GameObject with PotionScript to change a potion.", this);
            return;
        }

        targetPotion.potion = sourcePotion.potion;

        Animator targetAnimator = targetPotion.GetComponent<Animator>();
        Animator sourceAnimator = potionPrefab.GetComponent<Animator>();
        if (targetAnimator != null && sourceAnimator != null)
        {
            targetAnimator.runtimeAnimatorController = sourceAnimator.runtimeAnimatorController;
        }
    }

    private void DestroyPotion(PotionScript potion)
    {
        if (gameManager == null)
        {
            Debug.LogError($"{name}: GameManager reference is missing. Assign it in Inspector to destroy familiar targets.", this);
            return;
        }

        potion.gameObject.SetActive(false);
        gameManager.RemovePotion(potion, false);
        gameManager.TryCompletePuzzleLevel();

        if (audioSource != null)
        {
            audioSource.Play();
        }

        PooledPotion pooledPotion = potion.GetComponent<PooledPotion>();
        if (pooledPotion == null || !pooledPotion.ReleaseToPool())
        {
            Destroy(potion.gameObject);
        }
    }
}
