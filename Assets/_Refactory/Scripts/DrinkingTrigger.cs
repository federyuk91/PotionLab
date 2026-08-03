using UnityEngine;
using CharacterSystem;
public class DrinkingTrigger : MonoBehaviour
{
    [SerializeField] private TransformationManager transformationManager;
    [SerializeField] private PotionScriptable litchSummonPotionEffect;

    private bool missingTransformationManagerWarningShown;
    private bool missingLitchSummonPotionWarningShown;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (TryGetLitchSummon(collision, out LitchSummonPotionDestroyer summon))
        {
            DrinkLitchSummon(summon);
        }
        else if (collision.gameObject.CompareTag("Potion"))
        {
            BaseCharacter currentCharacter = GetCurrentCharacter();
            if (currentCharacter == null)
            {
                return;
            }

            currentCharacter.Drunk(collision.GetComponent<PotionScript>());
            Destroy(collision.gameObject, 2f);
            collision.gameObject.SetActive(false);

        }
        else if (collision.gameObject.CompareTag("Drop"))
        {
            Destroy(collision.gameObject);
        }
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

        currentCharacter.Drunk(litchSummonPotionEffect);
        summon.ConsumeByDrinkingTrigger();
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

    private void WarnMissingLitchSummonPotion()
    {
        if (missingLitchSummonPotionWarningShown)
        {
            return;
        }

        missingLitchSummonPotionWarningShown = true;
        Debug.LogWarning($"{name}: Litch summon potion effect is missing. Assign it in Inspector.", this);
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
