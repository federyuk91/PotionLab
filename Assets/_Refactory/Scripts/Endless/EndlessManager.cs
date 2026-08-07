using System;
using System.Collections;
using System.Collections.Generic;
using InspectorValidation;
using UnityEngine;

namespace EndlessSystem
{
    public class EndlessManager : MonoBehaviour
    {
        public event Action<int> PhaseChanged;
        public event Action<int> SpawnedPotionCountChanged;
        public event Action<EndlessPhaseSettings> PhaseEventTriggered;
        public event Action OverflowBombTriggered;

        [Header("References")]
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private GameManager gameManager;
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private LevelSettings levelSettings;
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private EndlessEventController eventController;
        [RequiredInspectorReference]
        [SerializeField] private Transform spawnPoint;
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private PotionPool potionPool;

        [Header("Phases")]
        [SerializeField] private List<EndlessPhaseSettings> phases = new List<EndlessPhaseSettings>();
        [SerializeField] private bool loopPhases = true;
        [SerializeField] private bool startOnLevelInteraction = true;

        private int phaseIndex;
        private int spawnedPotionsInCurrentPhase;
        private Coroutine spawnCoroutine;
        private readonly HashSet<PotionScript> activeEndlessPotions = new HashSet<PotionScript>();
        private bool missingGameManagerWarningShown;
        private bool missingLevelSettingsWarningShown;
        private bool missingSpawnPointWarningShown;
        private bool missingEventControllerWarningShown;
        private bool missingPotionPoolWarningShown;

        private void OnEnable()
        {
            if (gameManager == null)
            {
                WarnMissingGameManager();
                return;
            }

            gameManager.LevelInteractionStarted += HandleLevelInteractionStarted;
            gameManager.LevelCompleted += StopEndless;
            gameManager.CharacterDied += HandleCharacterDied;
            gameManager.PotionRemoved += HandlePotionRemoved;
            gameManager.PotionReplaced += HandlePotionReplaced;
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.LevelInteractionStarted -= HandleLevelInteractionStarted;
                gameManager.LevelCompleted -= StopEndless;
                gameManager.CharacterDied -= HandleCharacterDied;
                gameManager.PotionRemoved -= HandlePotionRemoved;
                gameManager.PotionReplaced -= HandlePotionReplaced;
            }

            StopEndless();
        }

        private void Start()
        {
            if (!startOnLevelInteraction)
            {
                StartEndless();
            }
        }

        public void StartEndless()
        {
            if (!CanStartEndless())
            {
                return;
            }

            if (spawnCoroutine != null)
            {
                return;
            }

            phaseIndex = Mathf.Clamp(phaseIndex, 0, phases.Count - 1);
            activeEndlessPotions.Clear();
            PhaseChanged?.Invoke(phaseIndex);
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        public void StopEndless()
        {
            if (spawnCoroutine == null)
            {
                return;
            }

            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
        }

        private IEnumerator SpawnRoutine()
        {
            while (phases.Count > 0)
            {
                EndlessPhaseSettings phase = phases[phaseIndex];
                spawnedPotionsInCurrentPhase = 0;

                while (spawnedPotionsInCurrentPhase < phase.NextPhaseAfterSpawnedPotions)
                {
                    yield return new WaitForSeconds(GetSpawnSeconds(phase));
                    SpawnPotion(phase);
                    spawnedPotionsInCurrentPhase++;
                }

                TriggerPhaseEvent(phase);
                AdvancePhase();
            }
        }

        private void SpawnPotion(EndlessPhaseSettings phase)
        {
            GameObject potionPrefab = phase.PickRandomPotionPrefab();
            if (potionPrefab == null)
            {
                Debug.LogWarning($"{name}: Endless phase '{phase.name}' did not return a potion prefab.", this);
                return;
            }

            PotionScript potion = potionPool.Get(potionPrefab, spawnPoint.position, Quaternion.identity);

            if (potion == null)
            {
                Debug.LogWarning($"{name}: Spawned endless prefab '{potionPrefab.name}' has no PotionScript component.", this);
                return;
            }

            potion.isActive = true;
            potion.DropPotion();
            activeEndlessPotions.Add(potion);
            gameManager.RegisterSpawnedPotion(potion);
            SpawnedPotionCountChanged?.Invoke(gameManager.spawnedPotion);

            if (activeEndlessPotions.Count > levelSettings.MaxActivePotionsBeforeBomb)
            {
                TriggerOverflowBombEvent();
            }
        }

        private void TriggerPhaseEvent(EndlessPhaseSettings phase)
        {
            if (eventController == null)
            {
                WarnMissingEventController();
            }
            else
            {
                eventController.StartEvent(phase.EventType, phase.EventValue);
            }

            PhaseEventTriggered?.Invoke(phase);
        }

        private void TriggerOverflowBombEvent()
        {
            if (eventController == null)
            {
                WarnMissingEventController();
                return;
            }

            eventController.StartEvent(EndlessEventType.Bomb, 0f);
            OverflowBombTriggered?.Invoke();
        }

        private void AdvancePhase()
        {
            phaseIndex++;

            if (phaseIndex >= phases.Count)
            {
                if (!loopPhases)
                {
                    StopEndless();
                    return;
                }

                phaseIndex = 0;
            }

            PhaseChanged?.Invoke(phaseIndex);
        }

        private float GetSpawnSeconds(EndlessPhaseSettings phase)
        {
            if (levelSettings != null && levelSettings.EndlessHyperHyperMode)
            {
                return Mathf.Max(levelSettings.MinimumSpawnSeconds, levelSettings.HyperHyperModeSpawnSeconds);
            }

            float spawnSeconds = levelSettings.DefaultSpawnSeconds;
            if (levelSettings != null && levelSettings.EndlessHyperMode)
            {
                spawnSeconds = levelSettings.HyperModeSpawnSeconds;
            }

            spawnSeconds -= phase.SpawnSpeedIncrement;
            return Mathf.Max(levelSettings.MinimumSpawnSeconds, spawnSeconds);
        }

        private bool CanStartEndless()
        {
            bool canStart = true;

            if (gameManager == null)
            {
                WarnMissingGameManager();
                canStart = false;
            }

            if (levelSettings == null)
            {
                WarnMissingLevelSettings();
                canStart = false;
            }
            else if (levelSettings.IsPuzzleMode)
            {
                Debug.LogWarning($"{name}: EndlessManager is active but LevelSettings is configured as puzzle mode.", this);
                canStart = false;
            }

            if (spawnPoint == null)
            {
                WarnMissingSpawnPoint();
                canStart = false;
            }

            if (potionPool == null)
            {
                WarnMissingPotionPool();
                canStart = false;
            }

            if (phases == null || phases.Count == 0)
            {
                Debug.LogWarning($"{name}: EndlessManager has no phases assigned in Inspector.", this);
                canStart = false;
            }

            return canStart;
        }

        private void HandleLevelInteractionStarted()
        {
            StartEndless();
        }

        private void HandleCharacterDied(string deathDialog)
        {
            StopEndless();
        }

        private void HandlePotionRemoved(PotionScript potion, bool drunked)
        {
            if (potion == null)
            {
                return;
            }

            activeEndlessPotions.Remove(potion);
        }

        private void HandlePotionReplaced(PotionScript sourcePotion, PotionScript replacementPotion)
        {
            if (sourcePotion != null)
            {
                activeEndlessPotions.Remove(sourcePotion);
            }

            if (replacementPotion != null)
            {
                activeEndlessPotions.Add(replacementPotion);
            }
        }

        private void WarnMissingGameManager()
        {
            if (missingGameManagerWarningShown)
            {
                return;
            }

            missingGameManagerWarningShown = true;
            Debug.LogWarning($"{name}: GameManager reference is missing. Assign it in Inspector so endless potions can be registered.", this);
        }

        private void WarnMissingLevelSettings()
        {
            if (missingLevelSettingsWarningShown)
            {
                return;
            }

            missingLevelSettingsWarningShown = true;
            Debug.LogWarning($"{name}: LevelSettings reference is missing. Assign it in Inspector so endless mode settings can be read.", this);
        }

        private void WarnMissingSpawnPoint()
        {
            if (missingSpawnPointWarningShown)
            {
                return;
            }

            missingSpawnPointWarningShown = true;
            Debug.LogWarning($"{name}: SpawnPoint reference is missing. Assign it in Inspector to spawn endless potions.", this);
        }

        private void WarnMissingEventController()
        {
            if (missingEventControllerWarningShown)
            {
                return;
            }

            missingEventControllerWarningShown = true;
            Debug.LogWarning($"{name}: EndlessEventController reference is missing. Assign it in Inspector to run endless phase events.", this);
        }

        private void WarnMissingPotionPool()
        {
            if (missingPotionPoolWarningShown)
            {
                return;
            }

            missingPotionPoolWarningShown = true;
            Debug.LogWarning($"{name}: PotionPool reference is missing. Assign it in Inspector so endless can reuse potion instances.", this);
        }
    }
}
