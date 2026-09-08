using System.Collections.Generic;
using UnityEngine;

public class MagicSlidingPlatform : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Range(0f, 0.1f)] private float speed = 0.01f;
    [SerializeField] private List<Transform> points = new List<Transform>();
    [SerializeField] private bool reversePath;

    [Header("Transported Objects")]
    [SerializeField] private List<GameObject> trappedObjects = new List<GameObject>();
    [SerializeField] private List<int> dest = new List<int>();

    private bool isConfigured;
    private bool missingPointWarningShown;

    private void Awake()
    {
        NormalizeDestinations();
    }

    private void FixedUpdate()
    {
        if (!isConfigured)
        {
            return;
        }

        for (int index = trappedObjects.Count - 1; index >= 0; index--)
        {
            GameObject trappedObject = trappedObjects[index];
            if (trappedObject == null)
            {
                trappedObjects.RemoveAt(index);
                dest.RemoveAt(index);
                continue;
            }

            int destinationIndex = dest[index];
            Transform destination = points[destinationIndex];
            if (destination == null)
            {
                WarnMissingPoint();
                continue;
            }

            if (Vector3.Distance(trappedObject.transform.position, destination.position) < 0.1f)
            {
                destinationIndex = reversePath
                    ? (destinationIndex - 1 + points.Count) % points.Count
                    : (destinationIndex + 1) % points.Count;
                dest[index] = destinationIndex;
                destination = points[destinationIndex];
            }

            if (destination != null)
            {
                trappedObject.transform.position = Vector3.MoveTowards(
                    trappedObject.transform.position,
                    destination.position,
                    speed);
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (points == null || points.Count < 2)
        {
            return;
        }

        for (int index = 0; index < points.Count; index++)
        {
            Transform currentPoint = points[index];
            Transform nextPoint = points[(index + 1) % points.Count];
            if (currentPoint != null && nextPoint != null)
            {
                Debug.DrawLine(currentPoint.position, nextPoint.position);
            }
        }
    }

    public void FreeObject(GameObject obj)
    {
        int index = trappedObjects.IndexOf(obj);
        if (index < 0)
        {
            Debug.LogWarning($"{name}: cannot free an object that is not assigned to this moving platform.", this);
            return;
        }

        trappedObjects.RemoveAt(index);
        dest.RemoveAt(index);
    }

    [ContextMenu("Shuffle Points")]
    public void ShufflePoints()
    {
        if (points == null)
        {
            return;
        }

        for (int index = points.Count - 1; index > 0; index--)
        {
            int swapIndex = Random.Range(0, index + 1);
            Transform temporaryPoint = points[index];
            points[index] = points[swapIndex];
            points[swapIndex] = temporaryPoint;
        }

        NormalizeDestinations();
    }

    private void NormalizeDestinations()
    {
        if (points == null || points.Count == 0)
        {
            isConfigured = false;
            Debug.LogWarning($"{name}: Moving Platform requires at least one path point.", this);
            return;
        }

        if (trappedObjects == null)
        {
            trappedObjects = new List<GameObject>();
        }

        if (dest == null)
        {
            dest = new List<int>();
        }

        while (dest.Count < trappedObjects.Count)
        {
            dest.Add(dest.Count % points.Count);
        }

        while (dest.Count > trappedObjects.Count)
        {
            dest.RemoveAt(dest.Count - 1);
        }

        for (int index = 0; index < dest.Count; index++)
        {
            dest[index] = Mathf.Clamp(dest[index], 0, points.Count - 1);
        }

        isConfigured = true;
    }

    private void WarnMissingPoint()
    {
        if (missingPointWarningShown)
        {
            return;
        }

        missingPointWarningShown = true;
        Debug.LogWarning($"{name}: a moving platform path point is missing. Assign all points in Inspector.", this);
    }
}
