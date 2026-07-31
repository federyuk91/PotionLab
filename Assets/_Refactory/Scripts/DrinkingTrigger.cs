using UnityEngine;
using CharacterSystem;
public class DrinkingTrigger : MonoBehaviour
{
    [SerializeField] private PotionScriptable litchSummonPotionEffect;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (TryGetLitchSummon(collision, out LitchSummonPotionDestroyer summon))
        {
            DrinkLitchSummon(summon);
        }
        else if (collision.gameObject.CompareTag("Potion"))
        {
            TransformationManager.Instance.Current.Drunk(collision.GetComponent<PotionScript>());
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
        if (summon == null || litchSummonPotionEffect == null || TransformationManager.Instance == null || TransformationManager.Instance.Current == null)
        {
            return;
        }

        TransformationManager.Instance.Current.Drunk(litchSummonPotionEffect);
        summon.ConsumeByDrinkingTrigger();
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
