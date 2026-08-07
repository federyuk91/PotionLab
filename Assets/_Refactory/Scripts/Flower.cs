using CharacterSystem;
using System;
using UnityEngine;

public class Flower : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DialogManager dialogManager;

    private int status;
    private bool missingGameManagerWarningShown;
    private bool missingDialogManagerWarningShown;

    public event Action<Flower> Destroyed;

    private void OnEnable()
    {
        ResetFlower();
    }

    public void ResetFlower()
    {
        status = 0;

        if (animator != null)
        {
            animator.SetInteger("status", status);
        }
    }

    public void Grow()
    {
        if (status != 0)
        {
            return;
        }

        status++;
        animator.SetInteger("status", status);
        PlayAudio();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Potion"))
        {
            return;
        }

        if (!other.TryGetComponent(out PotionScript potion))
        {
            return;
        }

        if (!CanPotionDamageFlower(potion.potion.effectType))
        {
            return;
        }

        ConsumePotion(potion);
        ApplyDamageStep();
    }

    private bool CanPotionDamageFlower(PotionScriptable.EffectType effectType)
    {
        return effectType == PotionScriptable.EffectType.lava
            || effectType == PotionScriptable.EffectType.poisoned
            || effectType == PotionScriptable.EffectType.ice
            || effectType == PotionScriptable.EffectType.fire;
    }

    private void ConsumePotion(PotionScript potion)
    {
        potion.gameObject.SetActive(false);

        if (gameManager != null)
        {
            gameManager.RemovePotion(potion, true);
            gameManager.TryCompletePuzzleLevel();
        }
        else
        {
            WarnMissingGameManager();
        }

        ReleaseOrDestroyPotion(potion);
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

    private void ApplyDamageStep()
    {
        if (status == 0)
        {
            ShowDialog("Bye my friend", 2f);
            DestroyFlower();
            PlayAudio();
            return;
        }

        if (status == 1)
        {
            status++;
            ShowDialog("My precious child! :(", 2f);
            animator.SetInteger("status", status);
            PlayAudio();
            return;
        }

        if (status == 2)
        {
            ShowDialog("I will miss you! ", 2f);
            DestroyFlower();
            PlayAudio();
        }
    }

    public void DestroyFlower()
    {
        Destroyed?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void ShowDialog(string dialog, float duration)
    {
        if (dialogManager != null)
        {
            dialogManager.PopDialog(dialog, duration);
            return;
        }

        WarnMissingDialogManager();
    }

    private void PlayAudio()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void WarnMissingGameManager()
    {
        if (missingGameManagerWarningShown)
        {
            return;
        }

        missingGameManagerWarningShown = true;
        Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so consumed potions are removed from level tracking.", this);
    }

    private void WarnMissingDialogManager()
    {
        if (missingDialogManagerWarningShown)
        {
            return;
        }

        missingDialogManagerWarningShown = true;
        Debug.LogWarning($"{name}: DialogManager reference is missing. Assign it in Inspector to show flower dialogs.", this);
    }
}
