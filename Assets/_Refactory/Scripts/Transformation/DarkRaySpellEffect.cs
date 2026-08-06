using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    [RequireComponent(typeof(Collider2D))]
    public class DarkRaySpellEffect : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PotionScript darkPotionPrefab;
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Collider2D rayCollider;
        [SerializeField] private PotionPool potionPool;

        [Header("Timing")]
        [SerializeField] private float activeDuration = 1f;

        private readonly HashSet<PotionScript> convertedPotions = new HashSet<PotionScript>();
        private readonly List<Collider2D> overlapResults = new List<Collider2D>();
        private ContactFilter2D potionFilter;
        private Coroutine castCoroutine;
        private bool missingGameManagerWarningShown;

        private void Reset()
        {
            rayCollider = GetComponent<Collider2D>();
            rayCollider.isTrigger = true;
        }

        private void Awake()
        {
            if (rayCollider == null)
            {
                rayCollider = GetComponent<Collider2D>();
            }

            if (rayCollider != null)
            {
                rayCollider.isTrigger = true;
            }

            potionFilter = ContactFilter2D.noFilter;

            if (gameManager == null)
            {
                WarnMissingGameManager();
            }
        }

        public void Cast()
        {
            if (darkPotionPrefab == null)
            {
                Debug.LogWarning($"{name} cannot cast Dark-Ray: Dark Potion prefab is missing.", this);
                return;
            }

            if (castCoroutine != null)
            {
                StopCoroutine(castCoroutine);
            }

            convertedPotions.Clear();
            gameObject.SetActive(true);
            castCoroutine = StartCoroutine(CastRoutine());
        }

        private IEnumerator CastRoutine()
        {
            ConvertCurrentOverlaps();

            if (activeDuration > 0f)
            {
                yield return new WaitForSeconds(activeDuration);
            }

            gameObject.SetActive(false);
            castCoroutine = null;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            TryConvert(other);
        }

        private void ConvertCurrentOverlaps()
        {
            if (rayCollider == null)
            {
                return;
            }

            overlapResults.Clear();
            rayCollider.Overlap(potionFilter, overlapResults);

            foreach (Collider2D overlap in overlapResults)
            {
                TryConvert(overlap);
            }
        }

        private void TryConvert(Collider2D targetCollider)
        {
            if (targetCollider == null)
            {
                return;
            }

            if (!targetCollider.TryGetComponent(out PotionScript potion))
            {
                potion = targetCollider.GetComponentInParent<PotionScript>();
            }

            if (potion == null || !potion.CompareTag("Potion") || convertedPotions.Contains(potion))
            {
                return;
            }

            if (potion.potion != null && potion.potion.effectType == PotionScriptable.EffectType.dark)
            {
                convertedPotions.Add(potion);
                return;
            }

            ConvertPotion(potion);
        }

        private void ConvertPotion(PotionScript sourcePotion)
        {
            convertedPotions.Add(sourcePotion);

            Transform sourceTransform = sourcePotion.transform;
            Vector3 position = sourceTransform.position;
            Quaternion rotation = sourceTransform.rotation;
            Transform parent = sourceTransform.parent;
            Rigidbody2D sourceRigidbody = sourcePotion.GetComponent<Rigidbody2D>();

            PotionScript darkPotion = CreateDarkPotion(position, rotation, parent);
            if (darkPotion == null)
            {
                return;
            }

            CopyMotion(sourceRigidbody, darkPotion.GetComponent<Rigidbody2D>());

            if (gameManager != null)
            {
                gameManager.ReplacePotion(sourcePotion, darkPotion);
                ReleaseOrDestroyPotion(sourcePotion);
                return;
            }

            WarnMissingGameManager();

            ReleaseOrDestroyPotion(sourcePotion);
        }

        private PotionScript CreateDarkPotion(Vector3 position, Quaternion rotation, Transform parent)
        {
            if (potionPool != null)
            {
                PotionScript pooledPotion = potionPool.Get(darkPotionPrefab.gameObject, position, rotation);
                if (pooledPotion != null)
                {
                    pooledPotion.isActive = true;
                    pooledPotion.transform.SetParent(parent);
                }

                return pooledPotion;
            }

            PotionScript instantiatedPotion = Instantiate(darkPotionPrefab, position, rotation, parent);
            instantiatedPotion.isActive = true;
            return instantiatedPotion;
        }

        private void ReleaseOrDestroyPotion(PotionScript potion)
        {
            PooledPotion pooledPotion = potion.GetComponent<PooledPotion>();
            if (pooledPotion != null && pooledPotion.ReleaseToPool())
            {
                return;
            }

            Destroy(potion.gameObject);
        }

        private void CopyMotion(Rigidbody2D sourceRigidbody, Rigidbody2D targetRigidbody)
        {
            if (sourceRigidbody == null || targetRigidbody == null)
            {
                return;
            }

            targetRigidbody.bodyType = sourceRigidbody.bodyType;
            targetRigidbody.linearVelocity = sourceRigidbody.linearVelocity;
            targetRigidbody.angularVelocity = sourceRigidbody.angularVelocity;
            targetRigidbody.mass = sourceRigidbody.mass;
        }

        private void WarnMissingGameManager()
        {
            if (missingGameManagerWarningShown)
            {
                return;
            }

            missingGameManagerWarningShown = true;
            Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so Dark-Ray can replace potions in level tracking.", this);
        }
    }
}
