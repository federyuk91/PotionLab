using CharacterSystem;
using UnityEngine;

public class StatusTickRunner : MonoBehaviour
{
    private CharacterStatusController status;
    private BaseCharacter character => TransformationManager.Instance.Current;

    private float fireTimer;
    private float poisonTimer;
    private float groundTimer;
    private float iceTimer;

    private void Awake()
    {
        status = GetComponent<CharacterStatusController>();
    }

    private void Update()
    {
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

        float delay = character.GetFireTickDelay();

        if (fireTimer >= delay)
        {
            fireTimer = 0f;
            character.FireTickFX();
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

        if (poisonTimer >= character.GetPoisonTickDelay())
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

        if (groundTimer >= character.GetGroundTickDelay())
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

        if (iceTimer >= character.GetIceTickDelay())
        {
            iceTimer = 0f;
            character.GroundTick();
        }
    }
}
