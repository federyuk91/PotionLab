using System.Collections;
using CharacterSystem;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PotionDestroyTrigger : MonoBehaviour
{
    [Header("Feedback")]
    [SerializeField] private string dialogOnPotionDestroyed = "BURN!";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SpriteRenderer[] blinkRenderers;
    [SerializeField] private Color blinkColor = Color.white;
    [SerializeField] private float blinkDuration = 0.08f;
    [SerializeField] private bool useSolidWhiteBlink = true;
    [SerializeField] private SpriteRenderer animatedRenderer;
    [SerializeField] private Sprite[] animationFrames;
    [SerializeField] private float animationFramesPerSecond = 12f;
    [SerializeField] private bool animateOnEnable = true;

    [Header("Refactored References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DialogManager dialogManager;
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
    private Coroutine blinkCoroutine;
    private Coroutine spriteAnimationCoroutine;
    private Color[] rendererBaseColors;
    private Material[] rendererBaseMaterials;
    private static Material solidWhiteBlinkMaterial;

    private void Reset()
    {
        // Reset is editor-only setup help: the object must work as a 2D trigger.
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        audioSource = GetComponent<AudioSource>();
        blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        animatedRenderer = GetComponent<SpriteRenderer>();
    }

    private void Awake()
    {
        if (animatedRenderer == null)
        {
            animatedRenderer = GetComponent<SpriteRenderer>();
        }

        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        DisableLocalSpriteAnimation();

        // Inspector references are preferred, these fallbacks keep scene setup tolerant.
        if (gameManager == null)
        {
            gameManager = GameManager.Instance;
        }

        if (dialogManager == null)
        {
            dialogManager = GetComponentInParent<DialogManager>();
        }

        if (lightController == null && TransformationManager.Instance != null)
        {
            lightController = TransformationManager.Instance.lightController;
        }
    }

    private void OnEnable()
    {
        DisableLocalSpriteAnimation();
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
        StopSpriteAnimation();
        RestoreBlinkRenderers();
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
        ShowDialog();
        PlayFeedback();

        if (gameManager != null)
        {
            gameManager.RemovePotion(potion, true);
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
        }
    }

    private void ShowDialog()
    {
        if (dialogManager != null && !string.IsNullOrEmpty(dialogOnPotionDestroyed))
        {
            dialogManager.PopDialog(dialogOnPotionDestroyed, 3f);
        }
    }

    private void PlayFeedback()
    {
        PlayBlink();

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void PlayBlink()
    {
        if (blinkRenderers == null || blinkRenderers.Length == 0)
        {
            return;
        }

        if (blinkCoroutine != null)
        {
            StopCoroutine(blinkCoroutine);
            RestoreBlinkRenderers();
        }

        blinkCoroutine = StartCoroutine(BlinkRoutine());
    }

    private IEnumerator BlinkRoutine()
    {
        rendererBaseColors = new Color[blinkRenderers.Length];
        rendererBaseMaterials = new Material[blinkRenderers.Length];

        for (int i = 0; i < blinkRenderers.Length; i++)
        {
            if (blinkRenderers[i] == null)
            {
                continue;
            }

            rendererBaseColors[i] = blinkRenderers[i].color;
            rendererBaseMaterials[i] = blinkRenderers[i].sharedMaterial;

            blinkRenderers[i].color = blinkColor;

            Material blinkMaterial = GetSolidWhiteBlinkMaterial();
            if (useSolidWhiteBlink && blinkMaterial != null)
            {
                blinkRenderers[i].sharedMaterial = blinkMaterial;
            }
        }

        yield return new WaitForSeconds(blinkDuration);

        RestoreBlinkRenderers();
        blinkCoroutine = null;
    }

    private void RestoreBlinkRenderers()
    {
        if (blinkRenderers == null || rendererBaseColors == null)
        {
            return;
        }

        int count = Mathf.Min(blinkRenderers.Length, rendererBaseColors.Length);
        for (int i = 0; i < count; i++)
        {
            if (blinkRenderers[i] != null)
            {
                blinkRenderers[i].color = rendererBaseColors[i];

                if (rendererBaseMaterials != null && i < rendererBaseMaterials.Length && rendererBaseMaterials[i] != null)
                {
                    blinkRenderers[i].sharedMaterial = rendererBaseMaterials[i];
                }
            }
        }
    }

    private static Material GetSolidWhiteBlinkMaterial()
    {
        if (solidWhiteBlinkMaterial != null)
        {
            return solidWhiteBlinkMaterial;
        }

        Shader shader = Shader.Find("PotionLab/Sprite Solid White");
        if (shader == null)
        {
            return null;
        }

        solidWhiteBlinkMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
        solidWhiteBlinkMaterial.color = Color.white;
        return solidWhiteBlinkMaterial;
    }

    public void RestartSpriteAnimation()
    {
        if (!animateOnEnable || animatedRenderer == null || animationFrames == null || animationFrames.Length == 0)
        {
            return;
        }

        StopSpriteAnimation();
        spriteAnimationCoroutine = StartCoroutine(SpriteAnimationRoutine());
    }

    private void StopSpriteAnimation()
    {
        if (spriteAnimationCoroutine != null)
        {
            StopCoroutine(spriteAnimationCoroutine);
            spriteAnimationCoroutine = null;
        }
    }

    private void DisableLocalSpriteAnimation()
    {
        if (animationFrames == null || animationFrames.Length == 0)
        {
            return;
        }

        Animation localAnimation = GetComponent<Animation>();
        if (localAnimation != null)
        {
            localAnimation.Stop();
            localAnimation.enabled = false;
        }
    }

    private IEnumerator SpriteAnimationRoutine()
    {
        int frameIndex = 0;
        float frameDelay = animationFramesPerSecond > 0f ? 1f / animationFramesPerSecond : 0.083333336f;

        while (true)
        {
            Sprite frame = animationFrames[frameIndex];
            if (frame != null)
            {
                animatedRenderer.sprite = frame;
            }

            frameIndex = (frameIndex + 1) % animationFrames.Length;
            yield return new WaitForSeconds(frameDelay);
        }
    }
}
