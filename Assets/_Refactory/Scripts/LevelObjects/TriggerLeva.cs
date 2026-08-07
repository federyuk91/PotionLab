using CharacterSystem;
using InspectorValidation;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class TriggerLeva : MonoBehaviour
{
    private const string GoLeftTrigger = "goLeft";
    private const string GoRightTrigger = "goRight";

    [Header("References")]
    [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Animator animator;
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private GameManager gameManager;
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private DialogManager dialogManager;

    [Header("Events")]
    public UnityEvent onLevaTriggered;

    [Header("Dialog")]
    [SerializeField] private string sentenceForPotion = "Ohhh no my potie!";
    [SerializeField] private string sentenceForDrop = "mmm... What was that switch for?";
    [SerializeField] private float potionDialogDuration = 3f;
    [SerializeField] private float dropDialogDuration = 5f;

    [Header("State")]
    [SerializeField] private bool isLeft = true;

    private bool missingAnimatorWarningShown;
    private bool missingGameManagerWarningShown;
    private bool missingDialogManagerWarningShown;

    private void Reset()
    {
        Collider2D triggerCollider = GetComponent<Collider2D>();
        triggerCollider.isTrigger = true;

        animator = GetComponent<Animator>();
    }

    private void Awake()
    {
        ValidateReferences();
    }

    public void GoLeft()
    {
        if (isLeft)
        {
            return;
        }

        SetAnimatorTrigger(GoLeftTrigger);
        isLeft = true;
    }

    public void GoRight()
    {
        if (!isLeft)
        {
            return;
        }

        SetAnimatorTrigger(GoRightTrigger);
        isLeft = false;
    }

    public void Switch()
    {
        if (isLeft)
        {
            GoRight();
            return;
        }

        GoLeft();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null)
        {
            return;
        }

        bool validTrigger = false;

        if (collision.CompareTag("Potion"))
        {
            validTrigger = HandlePotion(collision);
        }
        else if (collision.CompareTag("Drop"))
        {
            validTrigger = HandleDrop(collision);
        }

        if (validTrigger)
        {
            onLevaTriggered?.Invoke();
        }
    }

    public void ChangeSlidingPlatform(SurfaceEffector2D platform)
    {
        if (platform == null)
        {
            Debug.LogWarning($"{name}: ChangeSlidingPlatform received a missing SurfaceEffector2D reference.", this);
            return;
        }

        platform.speed *= -1f;
        UpdatePlatformVisualDirection(platform);
    }

    private bool HandlePotion(Collider2D collision)
    {
        if (!collision.TryGetComponent(out PotionScript potion))
        {
            potion = collision.GetComponentInParent<PotionScript>();
        }

        if (potion == null)
        {
            Debug.LogWarning($"{name}: object tagged as Potion triggered the lever but has no PotionScript.", collision);
            return false;
        }

        potion.gameObject.SetActive(false);
        PopDialog(sentenceForPotion, potionDialogDuration);

        if (gameManager != null)
        {
            gameManager.RemovePotion(potion, false);
            gameManager.TryCompletePuzzleLevel();
        }
        else
        {
            WarnMissingGameManager();
        }

        ReleaseOrDestroyPotion(potion);
        return true;
    }

    private bool HandleDrop(Collider2D collision)
    {
        PopDialog(sentenceForDrop, dropDialogDuration);
        Destroy(collision.gameObject);
        return true;
    }

    private void ReleaseOrDestroyPotion(PotionScript potion)
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

        Destroy(potion.gameObject);
    }

    private void PopDialog(string sentence, float duration)
    {
        if (dialogManager == null)
        {
            WarnMissingDialogManager();
            return;
        }

        dialogManager.PopDialog(sentence, duration);
    }

    private void SetAnimatorTrigger(string triggerName)
    {
        if (animator == null)
        {
            WarnMissingAnimator();
            return;
        }

        animator.SetTrigger(triggerName);
    }

    private void UpdatePlatformVisualDirection(SurfaceEffector2D platform)
    {
        if (platform.transform.childCount <= 0)
        {
            return;
        }

        SpriteRenderer renderer = platform.transform.GetChild(0).GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            return;
        }

        renderer.flipX = platform.speed < 0f;
    }

    private void ValidateReferences()
    {
        if (animator == null)
        {
            WarnMissingAnimator();
        }

        if (gameManager == null)
        {
            WarnMissingGameManager();
        }

        if (dialogManager == null)
        {
            WarnMissingDialogManager();
        }
    }

    private void WarnMissingAnimator()
    {
        if (missingAnimatorWarningShown)
        {
            return;
        }

        missingAnimatorWarningShown = true;
        Debug.LogWarning($"{name}: Animator reference is missing. Assign it in Inspector.", this);
    }

    private void WarnMissingGameManager()
    {
        if (missingGameManagerWarningShown)
        {
            return;
        }

        missingGameManagerWarningShown = true;
        Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so lever-destroyed potions are removed from level tracking.", this);
    }

    private void WarnMissingDialogManager()
    {
        if (missingDialogManagerWarningShown)
        {
            return;
        }

        missingDialogManagerWarningShown = true;
        Debug.LogWarning($"{name}: DialogManager reference is missing. Assign it in Inspector so lever feedback dialogs can be shown.", this);
    }
}
