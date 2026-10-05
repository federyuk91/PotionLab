using System.Collections.Generic;
using InspectorValidation;
using UnityEngine;

public class MagicSlidingPlatform : MonoBehaviour
{
    private const float MinimumSegmentLength = 0.0001f;

    private sealed class PathFollower
    {
        public GameObject Object;
        public Rigidbody2D Body;
        public float Distance;
    }

    [Header("Movement")]
    [SerializeField, Min(0f)] private float speedUnitsPerSecond = 1.75f;
    [SerializeField, RequiredInspectorReference] private List<Transform> points = new List<Transform>();
    [SerializeField] private bool reversePath;

    [Header("Initial Placement")]
    [Tooltip("0 preserves the authored spacing after snapping to the path. 1 distributes every object evenly across the full path.")]
    [SerializeField, Range(0f, 1f)] private float initialSpacing;

    [Header("Transported Objects")]
    [SerializeField, RequiredInspectorReference] private List<GameObject> trappedObjects = new List<GameObject>();

    private readonly List<PathFollower> followers = new List<PathFollower>();
    private bool isConfigured;
    private bool missingConfigurationWarningShown;

    private void Start()
    {
        ConfigureFollowers();
    }

    private void FixedUpdate()
    {
        if (!isConfigured || !TryGetPathLength(out float pathLength))
        {
            return;
        }

        float distanceDelta = speedUnitsPerSecond * Time.fixedDeltaTime;
        if (reversePath)
        {
            distanceDelta = -distanceDelta;
        }

        for (int index = followers.Count - 1; index >= 0; index--)
        {
            PathFollower follower = followers[index];
            if (follower.Object == null || follower.Body == null)
            {
                followers.RemoveAt(index);
                continue;
            }

            // DroppableObject owns the transition to Dynamic and must no longer be moved by the path.
            if (follower.Body.bodyType == RigidbodyType2D.Dynamic)
            {
                trappedObjects.Remove(follower.Object);
                followers.RemoveAt(index);
                continue;
            }

            follower.Distance = Mathf.Repeat(follower.Distance + distanceDelta, pathLength);
            if (!TryGetPointOnPath(follower.Distance, pathLength, out Vector2 nextPosition))
            {
                continue;
            }

            follower.Body.MovePosition(nextPosition);
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
        bool wasConfiguredObject = trappedObjects.Remove(obj);
        bool wasFollower = RemoveFollower(obj);

        if (!wasConfiguredObject && !wasFollower)
        {
            Debug.LogWarning($"{name}: cannot free an object that is not assigned to this moving platform.", this);
        }
    }

    private void ConfigureFollowers()
    {
        followers.Clear();
        isConfigured = false;

        if (!TryGetPathLength(out float pathLength))
        {
            return;
        }

        for (int index = 0; index < trappedObjects.Count; index++)
        {
            GameObject trappedObject = trappedObjects[index];
            if (trappedObject == null)
            {
                continue;
            }

            Rigidbody2D body = trappedObject.GetComponent<Rigidbody2D>();
            if (body == null)
            {
                Debug.LogError($"{name}: assign a Rigidbody2D to transported object '{trappedObject.name}'.", trappedObject);
                continue;
            }

            if (!TryProjectOntoPath(body.position, out float projectedDistance))
            {
                WarnMissingConfiguration();
                return;
            }

            PathFollower follower = new PathFollower
            {
                Object = trappedObject,
                Body = body,
                Distance = projectedDistance
            };

            followers.Add(follower);
        }

        followers.Sort(CompareFollowersByDistance);
        ApplyInitialSpacing(pathLength);

        for (int index = 0; index < followers.Count; index++)
        {
            PathFollower follower = followers[index];
            if (TryGetPointOnPath(follower.Distance, pathLength, out Vector2 position))
            {
                follower.Body.position = position;
            }
        }

        isConfigured = followers.Count > 0;
    }

    private void ApplyInitialSpacing(float pathLength)
    {
        if (followers.Count == 0 || initialSpacing <= 0f)
        {
            return;
        }

        float firstFollowerDistance = followers[0].Distance;
        float regularSpacing = pathLength / followers.Count;

        for (int index = 0; index < followers.Count; index++)
        {
            PathFollower follower = followers[index];
            float regularDistance = Mathf.Repeat(firstFollowerDistance + regularSpacing * index, pathLength);
            follower.Distance = LerpWrappedDistance(follower.Distance, regularDistance, pathLength, initialSpacing);
        }
    }

    private bool TryGetPathLength(out float pathLength)
    {
        pathLength = 0f;
        if (points == null || points.Count < 2)
        {
            WarnMissingConfiguration();
            return false;
        }

        for (int index = 0; index < points.Count; index++)
        {
            Transform currentPoint = points[index];
            Transform nextPoint = points[(index + 1) % points.Count];
            if (currentPoint == null || nextPoint == null)
            {
                WarnMissingConfiguration();
                return false;
            }

            pathLength += Vector2.Distance(currentPoint.position, nextPoint.position);
        }

        if (pathLength < MinimumSegmentLength)
        {
            WarnMissingConfiguration();
            return false;
        }

        return true;
    }

    private bool TryProjectOntoPath(Vector2 position, out float projectedDistance)
    {
        projectedDistance = 0f;
        float nearestSqrDistance = float.MaxValue;
        float accumulatedDistance = 0f;

        for (int index = 0; index < points.Count; index++)
        {
            Vector2 start = points[index].position;
            Vector2 end = points[(index + 1) % points.Count].position;
            Vector2 segment = end - start;
            float segmentLength = segment.magnitude;

            if (segmentLength < MinimumSegmentLength)
            {
                continue;
            }

            float interpolation = Mathf.Clamp01(Vector2.Dot(position - start, segment) / segment.sqrMagnitude);
            Vector2 closestPosition = start + segment * interpolation;
            float sqrDistance = (position - closestPosition).sqrMagnitude;
            if (sqrDistance < nearestSqrDistance)
            {
                nearestSqrDistance = sqrDistance;
                projectedDistance = accumulatedDistance + segmentLength * interpolation;
            }

            accumulatedDistance += segmentLength;
        }

        return nearestSqrDistance < float.MaxValue;
    }

    private bool TryGetPointOnPath(float distance, float pathLength, out Vector2 position)
    {
        float wrappedDistance = Mathf.Repeat(distance, pathLength);
        float accumulatedDistance = 0f;

        for (int index = 0; index < points.Count; index++)
        {
            Vector2 start = points[index].position;
            Vector2 end = points[(index + 1) % points.Count].position;
            float segmentLength = Vector2.Distance(start, end);
            if (segmentLength < MinimumSegmentLength)
            {
                continue;
            }

            if (wrappedDistance <= accumulatedDistance + segmentLength)
            {
                float interpolation = (wrappedDistance - accumulatedDistance) / segmentLength;
                position = Vector2.Lerp(start, end, interpolation);
                return true;
            }

            accumulatedDistance += segmentLength;
        }

        position = Vector2.zero;
        return false;
    }

    private bool RemoveFollower(GameObject obj)
    {
        for (int index = followers.Count - 1; index >= 0; index--)
        {
            if (followers[index].Object != obj)
            {
                continue;
            }

            followers.RemoveAt(index);
            return true;
        }

        return false;
    }

    private void WarnMissingConfiguration()
    {
        if (missingConfigurationWarningShown)
        {
            return;
        }

        missingConfigurationWarningShown = true;
        Debug.LogWarning($"{name}: assign at least two valid path points to MagicSlidingPlatform.", this);
    }

    private static int CompareFollowersByDistance(PathFollower first, PathFollower second)
    {
        return first.Distance.CompareTo(second.Distance);
    }

    private static float LerpWrappedDistance(float from, float to, float pathLength, float interpolation)
    {
        float difference = Mathf.Repeat(to - from + pathLength * 0.5f, pathLength) - pathLength * 0.5f;
        return Mathf.Repeat(from + difference * interpolation, pathLength);
    }
}
