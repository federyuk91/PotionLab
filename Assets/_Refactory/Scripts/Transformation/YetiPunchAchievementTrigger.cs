using InspectorValidation;
using UnityEngine;

namespace CharacterSystem
{
    public sealed class YetiPunchAchievementTrigger : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private YetiCharacter yetiCharacter;
        [SerializeField] private Vector2 punchDirection;
        [SerializeField] private float punchForce = 10f;

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (!collision.CompareTag("Potion"))
            {
                return;
            }

            if (yetiCharacter == null)
            {
                Debug.LogWarning($"{name}: YetiCharacter reference is missing. Assign it in Inspector to track punch achievements.", this);
            }
            else
            {
                yetiCharacter.RegisterPunchPotionHit();
            }

            Rigidbody2D potionBody = collision.attachedRigidbody;
            if (potionBody == null)
            {
                return;
            }

            potionBody.mass = 1f;
            potionBody.AddForce(punchDirection.normalized * punchForce, ForceMode2D.Impulse);
        }
    }
}
