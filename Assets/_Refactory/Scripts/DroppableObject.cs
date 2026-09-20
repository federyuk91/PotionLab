using InspectorValidation;
using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class DroppableObject : MonoBehaviour
{
    [Header("References")]
    [SerializeField] protected GameObject whiteSquare;

    [Header("Runtime State")]
    [SerializeField] protected bool isActive;

    private Rigidbody2D body;
    private RigidbodyType2D initialBodyType;
    private float initialMass;

    protected Rigidbody2D Body => body;

    protected virtual void Awake()
    {
        body = GetComponent<Rigidbody2D>();
        if (body == null)
        {
            Debug.LogError($"{name}: DroppableObject requires a local Rigidbody2D.", this);
            return;
        }

        initialBodyType = body.bodyType;
        initialMass = body.mass;

        if (whiteSquare == null && body.bodyType == RigidbodyType2D.Kinematic)
        {
            Debug.LogWarning($"{name}: Without drop square indicator set in DroppableObject or dinamic rb this object cannot be dropped", this);
        }
    }

    public virtual void ActivateBox()
    {
        TryActivateDropSelection();
    }

    public void Drop()
    {
        TryDrop(true);
    }

    public bool ApplyImpulse(Vector2 direction, float force)
    {
        if (body == null || direction.sqrMagnitude <= Mathf.Epsilon || force <= 0f)
        {
            return false;
        }

        ActivatePhysics(false);
        body.AddForce(direction.normalized * force, ForceMode2D.Impulse);
        return true;
    }

    public void ResetRuntimeState()
    {
        isActive = false;

        if (whiteSquare != null)
        {
            whiteSquare.SetActive(false);
        }

        if (body == null)
        {
            return;
        }

        body.linearVelocity = Vector2.zero;
        body.angularVelocity = 0f;
        body.mass = initialMass;
        body.bodyType = initialBodyType;
    }

    protected bool TryActivateDropSelection()
    {
        if (body == null || body.bodyType != RigidbodyType2D.Kinematic || whiteSquare == null)
        {
            return false;
        }

        whiteSquare.SetActive(true);
        isActive = true;
        return true;
    }

    protected bool TryDrop(bool notifyTargetClicked)
    {
        if (!isActive)
        {
            return false;
        }

        return ActivatePhysics(notifyTargetClicked);
    }

    protected bool ActivatePhysics(bool notifyTargetClicked)
    {
        if (body == null)
        {
            return false;
        }

        if (notifyTargetClicked)
        {
            ClickLightEvents.RaiseTargetClicked(transform);
        }

        body.bodyType = RigidbodyType2D.Dynamic;
        isActive = false;

        if (whiteSquare != null)
        {
            whiteSquare.SetActive(false);
        }

        return true;
    }
}
