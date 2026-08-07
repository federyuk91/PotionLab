using UnityEngine;

public class ClickLight : MonoBehaviour
{
    public float smoothSpeed = 5f;
    public Transform target;

    public void SetTarget(Transform target)
    {
        if (target == null)
        {
            Debug.LogWarning($"{name}: ClickLight target is missing.", this);
            return;
        }

        this.target = target;
        transform.position = target.position;
    }

    void Update()
    {
        //if target is not null, follow the target's position with smoothing
        if (target != null)
        {
            transform.position = Vector3.Lerp(transform.position, target.position, Time.deltaTime * smoothSpeed);
        }

    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}
