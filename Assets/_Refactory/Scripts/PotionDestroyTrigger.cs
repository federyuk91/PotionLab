using CharacterSystem;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(PotionDestroyFeedback))]
public class PotionDestroyTrigger : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private PotionDestroyFeedback feedback;

    [Header("Refactored References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private LightController lightController;

    // These rules preserve the old CalderoneScript side effects, but can be
    // disabled per object when a spell should only destroy potions.
    [Header("Optional Rewards")]
    [SerializeField] private bool countLightPotionsForAchievement = true;
    [SerializeField] private int lightPotionsForAchievement = 5;
    [SerializeField] private string lightPotionAchievementName = "Mana BURN!";
    [SerializeField] private bool increaseLightAfterNonLightPotions = true;
    [SerializeField] private int nonLightPotionsBeforeLightIncrease = 5;

    private int destroyedLightPotionCount;
    private int destroyedNonLightPotionCount;
    private bool missingGameManagerWarningShown;
    private bool missingLightControllerWarningShown;

    private void Reset()
    {
        // Reset is editor-only setup help: the object must work as a 2D trigger.
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        feedback = GetComponent<PotionDestroyFeedback>();
    }

    private void Awake()
    {
        if (feedback == null)
        {
            Debug.LogWarning($"{name}: PotionDestroyFeedback reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
            feedback = GetComponent<PotionDestroyFeedback>();
        }

        if (feedback == null)
        {
            feedback = gameObject.AddComponent<PotionDestroyFeedback>();
        }

        if (gameManager == null)
        {
            WarnMissingGameManager();
        }

        if (lightController == null && increaseLightAfterNonLightPotions)
        {
            WarnMissingLightController();
        }
    }

    private void OnEnable()
    {
        RestartSpriteAnimation();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Potion"))
        {
            return;
        }

        if (!collision.TryGetComponent(out PotionScript potion))
        {
            return;
        }

        DestroyPotion(potion);
    }

    private void OnDisable()
    {
        destroyedLightPotionCount = 0;
        destroyedNonLightPotionCount = 0;
    }

    private void DestroyPotion(PotionScript potion)
    {
        PotionScriptable potionData = potion.potion;

        // Disable immediately so the same potion cannot trigger other gameplay while
        // it is being removed from GameManager and destroyed at end of frame.
        potion.gameObject.SetActive(false);

        HandlePotionDestroyed(potionData);
        PlayFeedback();

        if (gameManager != null)
        {
            gameManager.RemovePotion(potion, true);
        }
        else
        {
            WarnMissingGameManager();
        }

        Destroy(potion.gameObject);
    }

    private void HandlePotionDestroyed(PotionScriptable potionData)
    {
        if (potionData == null)
        {
            return;
        }

        if (potionData.effectType == PotionScriptable.EffectType.light)
        {
            RegisterDestroyedLightPotion();
            return;
        }

        RegisterDestroyedNonLightPotion();
    }

    private void RegisterDestroyedLightPotion()
    {
        destroyedLightPotionCount++;

        // Legacy behavior: after enough light potions destroyed by the cauldron,
        // unlock the old "Mana BURN!" achievement.
        if (!countLightPotionsForAchievement || destroyedLightPotionCount != lightPotionsForAchievement)
        {
            return;
        }

        if (AchievementManager.instance != null)
        {
            AchievementManager.instance.Achive(lightPotionAchievementName);
        }
    }

    private void RegisterDestroyedNonLightPotion()
    {
        destroyedNonLightPotionCount++;

        // Legacy behavior: after enough non-light potions, advance the light level.
        // In the refactor the light change belongs to LightController, not GameManager.
        if (!increaseLightAfterNonLightPotions || destroyedNonLightPotionCount != nonLightPotionsBeforeLightIncrease)
        {
            return;
        }

        if (lightController != null)
        {
            lightController.IncreaseLightLevel();
            return;
        }

        WarnMissingLightController();
    }

    private void PlayFeedback()
    {
        if (feedback != null)
        {
            feedback.Play();
        }
    }

    public void RestartSpriteAnimation()
    {
        if (feedback != null)
        {
            feedback.RestartSpriteAnimation();
        }
    }

    private void WarnMissingGameManager()
    {
        if (missingGameManagerWarningShown)
        {
            return;
        }

        missingGameManagerWarningShown = true;
        Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so destroyed potions are removed from level tracking.", this);
    }

    private void WarnMissingLightController()
    {
        if (missingLightControllerWarningShown)
        {
            return;
        }

        missingLightControllerWarningShown = true;
        Debug.LogWarning($"{name}: LightController reference is missing. Assign it in Inspector so potion destruction can increase light level.", this);
    }
}
