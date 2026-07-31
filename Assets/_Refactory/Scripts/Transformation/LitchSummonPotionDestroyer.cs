using UnityEngine;

namespace CharacterSystem
{
    [RequireComponent(typeof(Collider2D))]
    public class LitchSummonPotionDestroyer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private GameObject objectToDeactivateAfterPotionDestroyed;

        [Header("Rules")]
        [SerializeField]
        private PotionScriptable.EffectType[] harmfulPotionTypes =
        {
            PotionScriptable.EffectType.healing,
            PotionScriptable.EffectType.grass,
            PotionScriptable.EffectType.lava,
            PotionScriptable.EffectType.light
        };

        [Header("Lifecycle")]
        [SerializeField] private bool deactivateSelfAfterPotionDestroyed = true;

        private bool hasDestroyedPotion;

        private void Awake()
        {
            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }
        }

        private void OnEnable()
        {
            hasDestroyedPotion = false;
        }

        private void OnCollisionEnter2D(Collision2D collision)
        {
            TryDestroyPotion(collision.collider);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryDestroyPotion(other);
        }

        public void ConsumeByDrinkingTrigger()
        {
            DeactivateAfterUse();
        }

        private void TryDestroyPotion(Collider2D potionCollider)
        {
            if (hasDestroyedPotion || potionCollider == null)
            {
                return;
            }

            PotionScript potion = potionCollider.GetComponent<PotionScript>();
            if (potion == null)
            {
                potion = potionCollider.GetComponentInParent<PotionScript>();
            }

            if (potion == null || !potion.CompareTag("Potion") || !IsHarmfulForLitch(potion))
            {
                return;
            }

            DestroyPotion(potion);
        }

        private bool IsHarmfulForLitch(PotionScript potion)
        {
            if (potion.potion == null || harmfulPotionTypes == null)
            {
                return false;
            }

            foreach (PotionScriptable.EffectType harmfulPotionType in harmfulPotionTypes)
            {
                if (potion.potion.effectType == harmfulPotionType)
                {
                    return true;
                }
            }

            return false;
        }

        private void DestroyPotion(PotionScript potion)
        {
            hasDestroyedPotion = true;
            potion.gameObject.SetActive(false);

            if (gameManager == null)
            {
                gameManager = GameManager.Instance;
            }

            if (gameManager != null)
            {
                gameManager.RemovePotion(potion, false);
            }

            Destroy(potion.gameObject);

            DeactivateAfterUse();
        }

        private void DeactivateAfterUse()
        {
            if (!deactivateSelfAfterPotionDestroyed)
            {
                return;
            }

            GameObject targetObject = objectToDeactivateAfterPotionDestroyed != null
                ? objectToDeactivateAfterPotionDestroyed
                : gameObject;

            targetObject.SetActive(false);
        }
    }
}
