using CharacterSystem;
using System;
using UnityEngine;

public class Flower : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private DialogManager dialogManager;

    private int status;

    public event Action<Flower> Destroyed;


    private void Awake()
    {

        dialogManager = GetComponentInParent<DialogManager>();
    }
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

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RemovePotion(potion, true);
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

        Debug.LogWarning($"{name} has no DialogManager assigned.", this);
    }

    private void PlayAudio()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }
}
