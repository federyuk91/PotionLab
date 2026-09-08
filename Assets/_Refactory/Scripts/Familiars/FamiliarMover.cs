using System.Collections;
using System.Collections.Generic;
using ProgressSystem;
using Refactory.CameraSystem;
using UnityEngine;
using UnityEngine.Serialization;

public class FamiliarMover : MonoBehaviour
{
    public enum FamiliarType
    {
        None,
        Destruction,
        Change,
        AreaEffect,
    }

    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private CameraShakeController cameraShakeController;
    [SerializeField] private AudioSource familiarAudioSource;
    [SerializeField] private PotionScriptable potion;
    [SerializeField, FormerlySerializedAs("objectToInstantiate")] private GameObject areaEffectPrefab;

    [Header("Lifetime")]
    [SerializeField, Min(0f)] private float timeBeforeGoOut = 15f;
    [SerializeField, FormerlySerializedAs("maxClick"), Min(1)] private int maxClicks = 3;
    [SerializeField, FormerlySerializedAs("clickTimer"), Min(0f)] private float clickCooldown = 0.5f;

    [Header("Movement")]
    [SerializeField, Range(0f, 0.25f)] private float speed;
    [SerializeField] private List<Transform> points = new List<Transform>();
    [SerializeField] private bool reversePath;
    [SerializeField] private bool restartPosition;
    [SerializeField, FormerlySerializedAs("startingPos")] private Vector3 startingPosition;

    [Header("Effect")]
    [SerializeField] private FamiliarType type;

    private bool canMove;
    private bool isClickable = true;
    private int clickCount;
    [FormerlySerializedAs("dest")] private int destinationIndex;
    private Coroutine lifetimeRoutine;

    public void Configure(GameManager assignedGameManager, CameraShakeController assignedCameraShakeController)
    {
        gameManager = assignedGameManager;
        cameraShakeController = assignedCameraShakeController;
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

    private void OnEnable()
    {
        if (restartPosition)
        {
            transform.position = startingPosition;
        }

        clickCount = 0;
        isClickable = true;
        canMove = HasValidPath();
        lifetimeRoutine = StartCoroutine(DeactivateAfterLifetime());
    }

    private void OnDisable()
    {
        canMove = false;

        if (lifetimeRoutine != null)
        {
            StopCoroutine(lifetimeRoutine);
            lifetimeRoutine = null;
        }

        CancelInvoke(nameof(MakeClickable));
    }

    private void FixedUpdate()
    {
        if (!canMove)
        {
            return;
        }

        Transform destination = points[destinationIndex];
        if (destination == null)
        {
            canMove = false;
            Debug.LogWarning($"{name}: Familiar path contains an unassigned point.", this);
            return;
        }

        if (Vector3.Distance(transform.position, destination.position) < 0.1f)
        {
            destinationIndex = reversePath
                ? (destinationIndex - 1 + points.Count) % points.Count
                : (destinationIndex + 1) % points.Count;
            destination = points[destinationIndex];
        }

        if (destination != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, destination.position, speed);
        }
    }

    public void CatchTheFamiliar()
    {
        if (!isClickable)
        {
            return;
        }

        if (clickCount >= maxClicks)
        {
            DeactivateObject();
            return;
        }

        clickCount++;
        isClickable = false;

        if (familiarAudioSource != null)
        {
            familiarAudioSource.Play();
        }

        if (clickCount >= 5)
        {
            if (gameManager != null)
            {
                gameManager.UnlockAchievementIfAvailable(AchievementId.Spammer);
            }
        }

        switch (type)
        {
            case FamiliarType.Destruction:
                DestroyPotions();
                return;
            case FamiliarType.Change:
                SpawnAreaEffect();
                break;
        }

        Invoke(nameof(MakeClickable), clickCooldown);
    }

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

        destinationIndex = 0;
    }

    public void DeactivateObject()
    {
        gameObject.SetActive(false);
    }

    private IEnumerator DeactivateAfterLifetime()
    {
        yield return new WaitForSeconds(timeBeforeGoOut);
        DeactivateObject();
    }

    private void DestroyPotions()
    {
        if (gameManager == null)
        {
            Debug.LogError($"{name}: GameManager reference is missing. Assign it in Inspector to destroy familiar targets.", this);
            return;
        }

        List<PotionScript> targets = new List<PotionScript>();
        foreach (PotionScript candidate in gameManager.levelPotions)
        {
            if (candidate != null && (potion == null || candidate.potion != null && candidate.potion.effectType == potion.effectType))
            {
                targets.Add(candidate);
            }
        }

        foreach (PotionScript target in targets)
        {
            target.gameObject.SetActive(false);
            gameManager.RemovePotion(target, false);
            ReleaseOrDestroy(target);
        }

        if (cameraShakeController != null)
        {
            cameraShakeController.Shake(1f, 1f);
        }

        SpawnAreaEffect();

        gameManager.TryCompletePuzzleLevel();
        DeactivateObject();
    }

    private void SpawnAreaEffect()
    {
        if (areaEffectPrefab == null)
        {
            Debug.LogWarning($"{name}: Area Effect Prefab reference is missing.", this);
            return;
        }

        GameObject spawnedEffect = Instantiate(areaEffectPrefab, transform.position, Quaternion.identity);
        if (spawnedEffect.TryGetComponent(out AreaEffectFamiliar areaEffect))
        {
            areaEffect.Configure(gameManager);
            if (type == FamiliarType.Change)
            {
                areaEffect.SetEffect(AreaEffectFamiliar.Effect.Change);
            }
        }
    }

    private bool HasValidPath()
    {
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning($"{name}: Familiar movement is disabled because no path points are assigned.", this);
            return false;
        }

        destinationIndex = Mathf.Clamp(destinationIndex, 0, points.Count - 1);
        return true;
    }

    private void MakeClickable()
    {
        isClickable = true;
    }

    private static void ReleaseOrDestroy(PotionScript potionScript)
    {
        PooledPotion pooledPotion = potionScript.GetComponent<PooledPotion>();
        if (pooledPotion != null && pooledPotion.ReleaseToPool())
        {
            return;
        }

        Destroy(potionScript.gameObject);
    }
}
