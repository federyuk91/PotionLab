using CharacterSystem;
using UnityEngine;

public class VFXStatusController : MonoBehaviour
{
    private CharacterStatusController statusController;
    private CharacterStats stats;

    [Header("Animators")]
    [SerializeField] private Animator fireAnimator;
    [SerializeField] private Animator algaeAnimator;
    [SerializeField] private Animator groundAnimator;
    [SerializeField] private Animator iceAnimator;

    [Header("VFX")]
    [SerializeField] private GameObject explosionVFX;
    [SerializeField] private GameObject healVFX;
    [SerializeField] private GameObject manaVFX;
    [SerializeField] private GameObject burnVFX;
    [SerializeField] private GameObject wetVFX;
    [SerializeField] private GameObject freezeVFX;
    [SerializeField] private GameObject poisonVFX;
    [SerializeField] private GameObject grassVFX;
    [SerializeField] private GameObject groundedVFX;
    [SerializeField] private GameObject algaeVFX;
    [SerializeField] private GameObject immuneFx;

    private SpriteRenderer freezeRenderer;
    private SpriteRenderer groundRenderer;

    private static readonly Color PoisonDormantColor = new(0.7f, 0.3f, 1f);

    private void Awake()
    {
        statusController = GetComponent<CharacterStatusController>();
        stats = GetComponent<CharacterStats>();

        freezeRenderer = freezeVFX.GetComponent<SpriteRenderer>();
        groundRenderer = groundedVFX.GetComponent<SpriteRenderer>();

        statusController.StatusAdded += OnStatudsAdded;
        statusController.StatusRemoved += OnStatudsRemoved;
        statusController.StatusLevelChanged += OnStatusLevelChange;
        statusController.OnImmunity += OnStatusImmunity;
        statusController.OnExplosion += OnExplosion;

        stats.OnHeal += OnHeal;
        stats.OnLight += OnMana;

        RefreshVFX(); // sicurezza all’avvio
    }

    // ---------------- STATUS ----------------

    private void OnStatudsAdded(Status _)
    {
        RefreshVFX();
    }
    private void OnStatudsRemoved(Status statusRemoved)
    {
        switch (statusRemoved)
        {
            case Status.Grounded:
                groundAnimator.SetTrigger("lava");
                break;
            case Status.Freezed:
                iceAnimator.SetTrigger("melt");
                break;
        }
        RefreshVFX();
    }

    private void OnStatusLevelChange(Status status)
    {
        switch (status)
        {
            case Status.Burned:
                fireAnimator.SetInteger("fireLevel", statusController.fireLevel);
                break;
            case Status.Grounded:
                groundAnimator.SetInteger("groundLevel", statusController.groundLevel);
                break;
            case Status.Algae:
                algaeAnimator.SetInteger("level", statusController.algaeLevel);
                break;
        }
    }

    // ---------------- VFX CORE ----------------

    private void RefreshVFX()
    {
        bool burned = statusController.Has(Status.Burned);
        bool wet = statusController.Has(Status.Wet);
        bool freezed = statusController.Has(Status.Freezed);
        bool poisoned = statusController.Has(Status.Poisoned);
        bool grass = statusController.Has(Status.Grass);
        bool grounded = statusController.Has(Status.Grounded);
        bool algae = statusController.Has(Status.Algae);

        bool poisonDormant = poisoned && (freezed || grounded);

        // FX base
        burnVFX.SetActive(burned);
        wetVFX.SetActive(wet);
        grassVFX.SetActive(grass);
        algaeVFX.SetActive(algae);
        //La disattivazione di grounded e freezed avviene tramite animazioni per permettere l’effetto di scioglimento/lava
        if (grounded)
            groundedVFX.SetActive(grounded);
        if (freezed)
            freezeVFX.SetActive(freezed);

        // Veleno (attivo solo se non dormiente)
        poisonVFX.SetActive(poisoned && !poisonDormant);

        // Colori speciali
        UpdateFreezeColor(freezed, poisoned);
        UpdateGroundColor(grounded, poisoned);
    }

    private void UpdateFreezeColor(bool freezed, bool poisoned)
    {
        if (!freezed)
        {
            freezeRenderer.color = Color.white;
            return;
        }

        freezeRenderer.color = poisoned ? PoisonDormantColor : Color.white;
    }

    private void UpdateGroundColor(bool grounded, bool poisoned)
    {
        if (!grounded)
        {
            groundRenderer.color = Color.white;
            return;
        }

        groundRenderer.color = poisoned ? PoisonDormantColor : Color.white;
    }

    // ---------------- FEEDBACK ----------------

    private void OnStatusImmunity()
    {
        immuneFx.SetActive(true);
    }

    private void OnExplosion()
    {
        if (explosionVFX != null)
            explosionVFX.SetActive(true);
    }

    private void OnHeal()
    {
        healVFX.SetActive(true);
    }

    private void OnMana()
    {
        manaVFX.SetActive(true);
    }

    private void OnDestroy()
    {
        if (statusController == null) return;

        statusController.StatusAdded -= OnStatudsAdded;
        statusController.StatusRemoved -= OnStatudsAdded;
        statusController.StatusLevelChanged -= OnStatusLevelChange;
        statusController.OnImmunity -= OnStatusImmunity;
        statusController.OnExplosion -= OnExplosion;

        stats.OnHeal -= OnHeal;
        stats.OnLight -= OnMana;
    }
}
