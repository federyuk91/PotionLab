using CharacterSystem;
using InspectorValidation;
using UnityEngine;

public class StatusTickRunner : MonoBehaviour
{
    private const float LingeringEffectsTickMultiplier = 1.25f;
    [SerializeField, RequiredInspectorReference] private CharacterStatusController status;
    [SerializeField, RequiredInspectorReference] private TransformationManager transformationManager;

    private float fireTimer;
    private float poisonTimer;
    private float groundTimer;
    private float iceTimer;
    private bool missingStatusWarningShown;
    private bool missingTransformationManagerWarningShown;
    private bool missingCurrentCharacterWarningShown;



    private void Awake()
    {
        if (status == null)
        {
            Debug.LogWarning($"{name}: CharacterStatusController reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
            status = GetComponent<CharacterStatusController>();
        }

        if (status == null)
        {
            WarnMissingStatus();
        }

        if (transformationManager == null)
        {
            WarnMissingTransformationManager();
        }
    }

    private void Update()
    {
        if (status == null || transformationManager == null || transformationManager.Current == null)
        {
            if (status == null)
            {
                WarnMissingStatus();
            }

            if (transformationManager == null)
            {
                WarnMissingTransformationManager();
            }
            else
            {
                WarnMissingCurrentCharacter();
            }

            return;
        }

        TickFire();
        TickPoison();
        TickGround();
        TickIce();
    }
    private void TickFire()
    {
        if (!status.Has(Status.Burned))
        {
            fireTimer = 0f;
            return;
        }

        fireTimer += Time.deltaTime;

        BaseCharacter character = transformationManager.Current;
        float delay = GetModifiedTickDelay(character.GetFireTickDelay());

        if (fireTimer >= delay)
        {
            fireTimer = 0f;
            character.FireTick();
        }
    }
    private void TickPoison()
    {
        if (!status.Has(Status.Poisoned))
        {
            poisonTimer = 0f;
            return;
        }

        poisonTimer += Time.deltaTime;

        BaseCharacter character = transformationManager.Current;
        if (poisonTimer >= GetModifiedTickDelay(character.GetPoisonTickDelay()))
        {
            poisonTimer = 0f;
            character.PoisonTick();
        }
    }
    private void TickGround()
    {
        if (!status.Has(Status.Grounded))
        {
            groundTimer = 0f;
            return;
        }

        groundTimer += Time.deltaTime;

        BaseCharacter character = transformationManager.Current;
        if (groundTimer >= GetModifiedTickDelay(character.GetGroundTickDelay()))
        {
            groundTimer = 0f;
            character.GroundTick();
        }
    }
    private void TickIce()
    {
        if (!status.Has(Status.Freezed))
        {
            iceTimer = 0f;
            return;
        }

        iceTimer += Time.deltaTime;

        BaseCharacter character = transformationManager.Current;
        if (iceTimer >= GetModifiedTickDelay(character.GetIceTickDelay()))
        {
            iceTimer = 0f;
            character.IceTick();
        }
    }

    private float GetModifiedTickDelay(float baseDelay)
    {
        LevelSettings settings = transformationManager != null && transformationManager.lightController != null
            ? transformationManager.lightController.LevelSettings
            : null;
        return settings != null && settings.EndlessLingeringEffects
            ? baseDelay / LingeringEffectsTickMultiplier
            : baseDelay;
    }

    private void WarnMissingStatus()
    {
        if (missingStatusWarningShown)
        {
            return;
        }

        missingStatusWarningShown = true;
        Debug.LogWarning($"{name}: CharacterStatusController reference is missing. Assign it in Inspector.", this);
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

    private void WarnMissingCurrentCharacter()
    {
        if (missingCurrentCharacterWarningShown)
        {
            return;
        }

        missingCurrentCharacterWarningShown = true;
        Debug.LogWarning($"{name}: TransformationManager has no current character.", this);
    }
}
