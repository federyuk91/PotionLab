using System.Collections.Generic;
using InspectorValidation;
using Refactory.LevelObjects;
using UnityEngine;

namespace CharacterSystem
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider2D))]
    public sealed class YetiPunchHitbox : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private YetiCharacter yetiCharacter;
        [SerializeField] private Vector2 punchDirection = new Vector2(-0.7f, 0.5f);
        [SerializeField, Min(0f)] private float punchForce = 10f;

        private readonly HashSet<int> hitTargets = new HashSet<int>();

        private void Awake()
        {
            if (yetiCharacter == null)
            {
                Debug.LogError($"{name}: assign the YetiCharacter reference in YetiPunchHitbox.", this);
            }
        }

        private void OnEnable()
        {
            hitTargets.Clear();
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (collision == null)
            {
                return;
            }

            DroppableObject droppableObject = collision.GetComponentInParent<DroppableObject>();
            if (droppableObject == null || !hitTargets.Add(droppableObject.GetInstanceID()))
            {
                return;
            }

            if (!droppableObject.ApplyImpulse(punchDirection, punchForce))
            {
                return;
            }

            GroundBlock groundBlock = droppableObject.GetComponent<GroundBlock>();
            if (groundBlock != null)
            {
                groundBlock.BreakFromYetiPunch();
                return;
            }

            PotionScript potion = droppableObject as PotionScript;
            if (potion == null || yetiCharacter == null)
            {
                return;
            }

            yetiCharacter.RegisterPunchPotionHit();
        }
    }
}
