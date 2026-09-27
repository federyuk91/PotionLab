using InspectorValidation;
using UnityEngine;

namespace EndlessSystem
{
    [DisallowMultipleComponent]
    public sealed class EndlessEmptyBottle : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private Rigidbody2D body;
        [SerializeField, RequiredInspectorReference] private Collider2D bottleCollider;

        public void Release(Vector3 position, float angle, Vector2 velocity, float angularVelocity, bool simulatePhysics)
        {
            transform.SetPositionAndRotation(position, Quaternion.Euler(0f, 0f, angle));
            gameObject.SetActive(true);

            body.bodyType = RigidbodyType2D.Dynamic;
            bottleCollider.enabled = simulatePhysics;
            body.simulated = simulatePhysics;
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;

            if (!simulatePhysics)
            {
                return;
            }

            body.linearVelocity = velocity;
            body.angularVelocity = angularVelocity;
            body.WakeUp();
        }

        public void FreezePhysics()
        {
            body.linearVelocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.bodyType = RigidbodyType2D.Static;
            body.simulated = true;
            bottleCollider.enabled = true;
        }
    }
}
