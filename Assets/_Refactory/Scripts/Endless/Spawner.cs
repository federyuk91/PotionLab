using System.Collections;
using EndlessSystem;
using InspectorValidation;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    [Header("References")]
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private GameManager gameManager;
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private PotionPool potionPool;
    [SerializeField, RequiredInspectorReference] private EndlessPhaseSettings spawnSettings;
    [SerializeField, RequiredInspectorReference] private BoxCollider2D blockCollider;
    [SerializeField, RequiredInspectorReference] private GameObject spawnerButton;
    [SerializeField] private AudioSource audioSource;

    [Header("Spawn")]
    [SerializeField] private bool spawnOnStart = true;
    [SerializeField] private float dropTime = 1.5f;
    [SerializeField] private float replaceMissingPotionDelay = 2f;
    [SerializeField] private bool autoReplaceMissingPotion = true;
    [SerializeField] private bool stopDrop;

    private PotionScript currentPotion;
    private Coroutine scheduledSpawnRoutine;
    private bool missingGameManagerWarningShown;
    private bool missingPotionPoolWarningShown;
    private bool missingSpawnSettingsWarningShown;
    private bool missingBlockColliderWarningShown;
    private bool missingSpawnerButtonWarningShown;

    public PotionScript potion => currentPotion;

    private void Awake()
    {
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                Debug.LogWarning($"{name}: AudioSource was recovered with GetComponent. Assign it in Inspector before production.", this);
            }
        }
    }

    private void OnEnable()
    {
        if (gameManager == null)
        {
            WarnMissingGameManager();
            return;
        }

        gameManager.PotionRemoved += HandlePotionRemoved;
        gameManager.PotionReplaced += HandlePotionReplaced;
    }

    private void OnDisable()
    {
        if (gameManager != null)
        {
            gameManager.PotionRemoved -= HandlePotionRemoved;
            gameManager.PotionReplaced -= HandlePotionReplaced;
        }

        CancelScheduledSpawn();
    }

    private void Start()
    {
        SetSpawnerButtonActive(false);

        if (spawnOnStart)
        {
            Spawn();
        }
    }

    public void Spawn()
    {
        if (!CanSpawn())
        {
            return;
        }

        CancelScheduledSpawn();

        GameObject potionPrefab = spawnSettings.PickRandomPotionPrefab();
        if (potionPrefab == null)
        {
            Debug.LogWarning($"{name}: Spawn settings '{spawnSettings.name}' did not return a potion prefab.", this);
            return;
        }

        PlaySpawnAudio();

        currentPotion = potionPool.Get(potionPrefab, transform.position, Quaternion.identity);
        if (currentPotion == null)
        {
            Debug.LogWarning($"{name}: PotionPool could not provide potion prefab '{potionPrefab.name}'.", this);
            return;
        }

        blockCollider.enabled = true;
        currentPotion.isActive = true;
        gameManager.RegisterSpawnedPotion(currentPotion);
        currentPotion.DropPotion(false);
    }

    public void ActivateButton()
    {
        SetSpawnerButtonActive(true);
    }

    public void DropPotion()
    {
        if (stopDrop || !CanCharacterContinue())
        {
            Debug.Log("Wait");
            return;
        }

        if (currentPotion == null)
        {
            ScheduleSpawn(replaceMissingPotionDelay);
            return;
        }

        blockCollider.enabled = false;
        SetSpawnerButtonActive(false);

        ClickLightEvents.RaiseTargetClicked(currentPotion.transform);
        ScheduleSpawn(dropTime);
    }

    public void EnableBlockCollider()
    {
        if (blockCollider == null)
        {
            WarnMissingBlockCollider();
            return;
        }

        blockCollider.enabled = true;
    }

    private bool CanSpawn()
    {
        bool canSpawn = true;

        if (gameManager == null)
        {
            WarnMissingGameManager();
            canSpawn = false;
        }

        if (potionPool == null)
        {
            WarnMissingPotionPool();
            canSpawn = false;
        }

        if (spawnSettings == null)
        {
            WarnMissingSpawnSettings();
            canSpawn = false;
        }

        if (blockCollider == null)
        {
            WarnMissingBlockCollider();
            canSpawn = false;
        }

        if (spawnerButton == null)
        {
            WarnMissingSpawnerButton();
            canSpawn = false;
        }

        return canSpawn;
    }

    private bool CanCharacterContinue()
    {
        if (gameManager == null)
        {
            WarnMissingGameManager();
            return false;
        }

        return gameManager.CanCharacterContinue();
    }

    private void ScheduleSpawn(float delay)
    {
        if (!autoReplaceMissingPotion && currentPotion == null)
        {
            return;
        }

        CancelScheduledSpawn();
        scheduledSpawnRoutine = StartCoroutine(SpawnAfterDelay(delay));
    }

    private void CancelScheduledSpawn()
    {
        if (scheduledSpawnRoutine == null)
        {
            return;
        }

        StopCoroutine(scheduledSpawnRoutine);
        scheduledSpawnRoutine = null;
    }

    private IEnumerator SpawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, delay));
        scheduledSpawnRoutine = null;
        Spawn();
    }

    private void HandlePotionRemoved(PotionScript removedPotion, bool drunked)
    {
        if (removedPotion == null || removedPotion != currentPotion)
        {
            return;
        }

        currentPotion = null;
        SetSpawnerButtonActive(false);

        if (scheduledSpawnRoutine == null)
        {
            ScheduleSpawn(replaceMissingPotionDelay);
        }
    }

    private void HandlePotionReplaced(PotionScript sourcePotion, PotionScript replacementPotion)
    {
        if (sourcePotion != currentPotion)
        {
            return;
        }

        currentPotion = replacementPotion;
    }

    private void PlaySpawnAudio()
    {
        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    private void SetSpawnerButtonActive(bool active)
    {
        if (spawnerButton == null)
        {
            WarnMissingSpawnerButton();
            return;
        }

        spawnerButton.SetActive(active);
    }

    private void WarnMissingGameManager()
    {
        if (missingGameManagerWarningShown)
        {
            return;
        }

        missingGameManagerWarningShown = true;
        Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so spawned potions can be registered.", this);
    }

    private void WarnMissingPotionPool()
    {
        if (missingPotionPoolWarningShown)
        {
            return;
        }

        missingPotionPoolWarningShown = true;
        Debug.LogWarning($"{name}: PotionPool reference is missing. Assign it in Inspector so this spawner can reuse potion instances.", this);
    }

    private void WarnMissingSpawnSettings()
    {
        if (missingSpawnSettingsWarningShown)
        {
            return;
        }

        missingSpawnSettingsWarningShown = true;
        Debug.LogWarning($"{name}: EndlessPhaseSettings reference is missing. Assign it in Inspector so this spawner can choose potion prefabs.", this);
    }

    private void WarnMissingBlockCollider()
    {
        if (missingBlockColliderWarningShown)
        {
            return;
        }

        missingBlockColliderWarningShown = true;
        Debug.LogWarning($"{name}: BlockCollider reference is missing. Assign it in Inspector so dropped potions can be held/released.", this);
    }

    private void WarnMissingSpawnerButton()
    {
        if (missingSpawnerButtonWarningShown)
        {
            return;
        }

        missingSpawnerButtonWarningShown = true;
        Debug.LogWarning($"{name}: SpawnerButton reference is missing. Assign it in Inspector so the manual drop button can be shown/hidden.", this);
    }
}
