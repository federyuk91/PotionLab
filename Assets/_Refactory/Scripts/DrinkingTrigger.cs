using System.Collections;
using InspectorValidation;
using UnityEngine;

namespace CharacterSystem
{
    [RequireComponent(typeof(Collider2D))]
    public class DrinkingTrigger : MonoBehaviour
    {
        [Header("References")]
        [SerializeField, RequiredInspectorReference] private TransformationManager transformationManager;
        [SerializeField, RequiredInspectorReference] private GameManager gameManager;
        [SerializeField] private PotionScriptable litchSummonPotionEffect;

        [Header("Rules")]
        [SerializeField] private float consumedPotionDestroyDelay = 2f;

        private bool missingTransformationManagerWarningShown;
        private bool missingGameManagerWarningShown;
        private bool missingLitchSummonPotionWarningShown;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            if (TryGetLitchSummon(collision, out LitchSummonPotionDestroyer summon))
            {
                DrinkLitchSummon(summon);
                return;
            }

            if (TryGetPotion(collision, out PotionScript potion))
            {
                DrinkPotion(potion);
                return;
            }

            if (collision.CompareTag("Drop"))
            {
                Destroy(collision.gameObject);
            }
        }

        private void DrinkPotion(PotionScript potion)
        {
            if (potion == null || !potion.gameObject.activeSelf)
            {
                return;
            }

            if (potion.potion == null)
            {
                Debug.LogError($"{potion.name}: assign PotionScript.potion before this potion can be consumed.", potion);
                return;
            }

            BaseCharacter currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            HideConsumedPotion(potion);
            StartCoroutine(ConsumePotionRoutine(potion, currentCharacter));
        }

        private IEnumerator ConsumePotionRoutine(PotionScript potion, BaseCharacter character)
        {
            // Await this exact drink, not the shared event emitted by every concurrent potion.
            yield return character.ResolveDrink(potion.potion);
            yield return WaitForPendingTransformation();

            RegisterConsumedPotion(potion, true);
            ReleaseConsumedPotion(potion);
            TryCompletePuzzleLevel();
        }

        private void DrinkLitchSummon(LitchSummonPotionDestroyer summon)
        {
            if (summon == null)
            {
                return;
            }

            if (litchSummonPotionEffect == null)
            {
                WarnMissingLitchSummonPotion();
                return;
            }

            BaseCharacter currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            StartCoroutine(currentCharacter.ResolveDrink(litchSummonPotionEffect));
            summon.ConsumeByDrinkingTrigger();
        }

        private void RegisterConsumedPotion(PotionScript potion, bool drunked)
        {
            if (potion == null)
            {
                return;
            }

            if (gameManager == null)
            {
                WarnMissingGameManager();
            }
            else
            {
                gameManager.RemovePotion(potion, drunked);
            }
        }

        private void TryCompletePuzzleLevel()
        {
            if (gameManager != null)
            {
                gameManager.TryCompletePuzzleLevel();
            }
        }

        private IEnumerator WaitForPendingTransformation()
        {
            while (transformationManager != null
                && transformationManager.Current != null
                && transformationManager.Current.IsReturnMagePending
                && gameManager != null
                && gameManager.CanCharacterContinue())
            {
                yield return null;
            }
        }

        private void HideConsumedPotion(PotionScript potion)
        {
            if (potion == null)
            {
                return;
            }

            potion.gameObject.SetActive(false);
        }

        private void ReleaseConsumedPotion(PotionScript potion)
        {
            if (potion == null)
            {
                return;
            }

            PooledPotion pooledPotion = potion.GetComponent<PooledPotion>();
            if (pooledPotion != null && pooledPotion.ReleaseToPool())
            {
                return;
            }

            Destroy(potion.gameObject, consumedPotionDestroyDelay);
        }

        private BaseCharacter GetCurrentCharacter()
        {
            if (transformationManager == null)
            {
                WarnMissingTransformationManager();
                return null;
            }

            if (transformationManager.Current == null)
            {
                Debug.LogWarning($"{name}: TransformationManager has no current character.", this);
                return null;
            }

            return transformationManager.Current;
        }

        private void WarnMissingTransformationManager()
        {
            if (missingTransformationManagerWarningShown)
            {
                return;
            }

            missingTransformationManagerWarningShown = true;
            Debug.LogWarning($"{name}: TransformationManager reference is missing. Assign it in Inspector.", this);
        }

        private void WarnMissingGameManager()
        {
            if (missingGameManagerWarningShown)
            {
                return;
            }

            missingGameManagerWarningShown = true;
            Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector to keep potion tracking consistent.", this);
        }

        private void WarnMissingLitchSummonPotion()
        {
            if (missingLitchSummonPotionWarningShown)
            {
                return;
            }

            missingLitchSummonPotionWarningShown = true;
            Debug.LogWarning($"{name}: Litch summon potion effect is missing. Assign it in Inspector.", this);
        }

        private bool TryGetPotion(Collider2D collision, out PotionScript potion)
        {
            if (!collision.TryGetComponent(out potion))
            {
                potion = collision.GetComponentInParent<PotionScript>();
            }

            if (potion == null)
            {
                if (collision.CompareTag("Potion"))
                {
                    Debug.LogWarning($"{name}: object tagged as Potion entered the drinking trigger but has no PotionScript.", collision);
                }

                return false;
            }

            return potion.CompareTag("Potion");
        }

        private bool TryGetLitchSummon(Collider2D collision, out LitchSummonPotionDestroyer summon)
        {
            if (!collision.TryGetComponent(out summon))
            {
                summon = collision.GetComponentInParent<LitchSummonPotionDestroyer>();
            }

            return summon != null;
        }
    }
}
