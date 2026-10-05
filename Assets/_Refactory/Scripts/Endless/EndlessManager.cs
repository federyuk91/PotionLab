using System;
using System.Collections;
using System.Collections.Generic;
using CharacterSystem;
using InspectorValidation;
using UnityEngine;

namespace EndlessSystem
{
    public class EndlessManager : MonoBehaviour
    {
        private const float SpawnRetryDelay = 0.1f;

        public int CurrentWave => Mathf.Max(1, waveNumber);
        public EndlessPhaseSettings CurrentPhase => phases != null && phases.Count > 0
            ? phases[Mathf.Clamp(phaseIndex, 0, phases.Count - 1)]
            : null;
        public int SpawnedPotionsInCurrentPhase => spawnedPotionsInCurrentPhase;

        public event Action<int> PhaseChanged;
        public event Action<int> WaveChanged;
        public event Action<int> SpawnedPotionCountChanged;
        public event Action<EndlessPhaseSettings, int, int> PhaseProgressChanged;
        public event Action<EndlessPhaseSettings> PhaseEventTriggered;
        public event Action OverflowBombTriggered;

        [Header("References")]
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private LightController lightController;
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private LevelSettings levelSettings;
        [RequiredInspectorReference(ResolveMode.SceneSingleton)]
        [SerializeField] private EndlessEventController eventController;
        [RequiredInspectorReference]
        [SerializeField] private Spawner automaticSpawner;

        [Header("Wave Audio")]
        [RequiredInspectorReference]
        [SerializeField] private AudioSource nextWaveAudioSource;
        [RequiredInspectorReference]
        [SerializeField] private AudioClip nextWaveAudioClip;
        [SerializeField, Range(0f, 1f)] private float nextWaveAudioVolume = 0.2f;

        [Header("Phases")]
        [SerializeField] private List<EndlessPhaseSettings> phases = new List<EndlessPhaseSettings>();
        [SerializeField] private bool loopPhases = true;
        [SerializeField] private bool startOnLevelInteraction = true;

        private int phaseIndex;
        private int waveNumber;
        private int spawnedPotionsInCurrentPhase;
        private Coroutine spawnCoroutine;
        private CharacterStats flawlessCharacterStats;
        private bool flawlessEndingRun;
        private bool missingGameManagerWarningShown;
        private bool missingLightControllerWarningShown;
        private bool missingLevelSettingsWarningShown;
        private bool missingEventControllerWarningShown;
        private bool missingAutomaticSpawnerWarningShown;

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
            gameManager.PotionRegistered += HandlePotionRegistered;
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.LevelInteractionStarted -= HandleLevelInteractionStarted;
                gameManager.LevelCompleted -= StopEndless;
                gameManager.CharacterDied -= HandleCharacterDied;
                gameManager.PotionRegistered -= HandlePotionRegistered;
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
            waveNumber = 1;
            flawlessEndingRun = false;
            LightController activeLightController = ResolveLightController();
            if (activeLightController != null)
            {
                activeLightController.ResumeLightDuration();
            }
            ConfigureAutomaticSpawner(CurrentPhase);
            automaticSpawner.EnsurePotionAvailable();
            SubscribeToFlawlessDamage();
            PhaseChanged?.Invoke(phaseIndex);
            WaveChanged?.Invoke(CurrentWave);
            NotifyPhaseProgress();
            spawnCoroutine = StartCoroutine(SpawnRoutine());
        }

        public void StopEndless()
        {
            if (spawnCoroutine != null)
            {
                StopCoroutine(spawnCoroutine);
                spawnCoroutine = null;
            }

            UnsubscribeFromFlawlessDamage();
        }

        private IEnumerator SpawnRoutine()
        {
            while (phases.Count > 0)
            {
                EndlessPhaseSettings phase = phases[phaseIndex];
                spawnedPotionsInCurrentPhase = 0;
                ConfigureAutomaticSpawner(phase);

                while (spawnedPotionsInCurrentPhase < phase.NextPhaseAfterSpawnedPotions)
                {
                    yield return new WaitForSeconds(GetSpawnInterval(phase));

                    while (!automaticSpawner.TryDropPotion())
                    {
                        yield return new WaitForSeconds(SpawnRetryDelay);
                    }

                    spawnedPotionsInCurrentPhase++;
                    NotifyPhaseProgress();
                }

                TriggerPhaseEvent(phase);
                AdvancePhase();
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

            spawnedPotionsInCurrentPhase = 0;
            waveNumber++;
            ConfigureAutomaticSpawner(CurrentPhase);
            PlayNextWaveAudio();
            PhaseChanged?.Invoke(phaseIndex);
            WaveChanged?.Invoke(CurrentWave);
            NotifyPhaseProgress();
        }

        private void PlayNextWaveAudio()
        {
            if (waveNumber <= 1)
            {
                return;
            }

            if (nextWaveAudioSource == null)
            {
                Debug.LogWarning($"{name}: Next Wave Audio Source is missing. Assign it in Inspector to play the wave transition sound.", this);
                return;
            }

            if (nextWaveAudioClip == null)
            {
                Debug.LogWarning($"{name}: Next Wave Audio Clip is missing. Assign it in Inspector to play the wave transition sound.", this);
                return;
            }

            nextWaveAudioSource.PlayOneShot(nextWaveAudioClip, nextWaveAudioVolume);
        }

        public EndlessPhaseSettings GetPhaseAfterCurrent(int offset = 1)
        {
            if (phases == null || phases.Count == 0)
            {
                return null;
            }

            int requestedIndex = phaseIndex + Mathf.Max(0, offset);
            if (loopPhases)
            {
                requestedIndex %= phases.Count;
            }
            else
            {
                requestedIndex = Mathf.Clamp(requestedIndex, 0, phases.Count - 1);
            }

            return phases[requestedIndex];
        }

        private void NotifyPhaseProgress()
        {
            EndlessPhaseSettings phase = CurrentPhase;
            if (phase == null)
            {
                return;
            }

            PhaseProgressChanged?.Invoke(
                phase,
                Mathf.Clamp(spawnedPotionsInCurrentPhase, 0, phase.NextPhaseAfterSpawnedPotions),
                phase.NextPhaseAfterSpawnedPotions);
        }

        public float GetSpawnInterval(EndlessPhaseSettings phase)
        {
            if (levelSettings == null)
            {
                WarnMissingLevelSettings();
                return 0f;
            }

            if (levelSettings.EndlessHyperHyperMode)
            {
                return Mathf.Max(levelSettings.MinimumSpawnSeconds, levelSettings.HyperHyperModeSpawnSeconds);
            }

            float spawnSeconds = levelSettings.DefaultSpawnSeconds;
            if (levelSettings.EndlessHyperMode)
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

            if (automaticSpawner == null)
            {
                WarnMissingAutomaticSpawner();
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
            LightController activeLightController = ResolveLightController();
            if (activeLightController == null)
            {
                WarnMissingLightController();
            }
            else
            {
                activeLightController.PauseLightDuration();
            }

            StopEndless();
        }

        private LightController ResolveLightController()
        {
            if (lightController != null)
            {
                return lightController;
            }

            return gameManager != null ? gameManager.lightController : null;
        }

        private void ConfigureAutomaticSpawner(EndlessPhaseSettings phase)
        {
            if (automaticSpawner == null || phase == null)
            {
                return;
            }

            automaticSpawner.SetSpawnSettings(phase);
        }

        private void HandlePotionRegistered(PotionScript potion)
        {
            if (potion == null || gameManager == null || levelSettings == null)
            {
                return;
            }

            SpawnedPotionCountChanged?.Invoke(gameManager.spawnedPotion);

            if (gameManager.ActivePotionCount > levelSettings.MaxActivePotionsBeforeBomb)
            {
                TriggerOverflowBombEvent();
            }
        }

        private void SubscribeToFlawlessDamage()
        {
            UnsubscribeFromFlawlessDamage();

            BaseCharacter character = gameManager != null ? gameManager.Character : null;
            flawlessCharacterStats = character != null ? character.stats : null;
            if (flawlessCharacterStats != null)
            {
                flawlessCharacterStats.DamageTaken += HandleFlawlessDamage;
            }
        }

        private void UnsubscribeFromFlawlessDamage()
        {
            if (flawlessCharacterStats != null)
            {
                flawlessCharacterStats.DamageTaken -= HandleFlawlessDamage;
                flawlessCharacterStats = null;
            }
        }

        private void HandleFlawlessDamage(int damage)
        {
            if (damage <= 0
                || flawlessEndingRun
                || levelSettings == null
                || !levelSettings.EndlessFlawlessMode)
            {
                return;
            }

            flawlessEndingRun = true;
            if (flawlessCharacterStats != null && flawlessCharacterStats.HP > 0)
            {
                flawlessCharacterStats.SetHP(0);
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

        private void WarnMissingLightController()
        {
            if (missingLightControllerWarningShown)
            {
                return;
            }

            missingLightControllerWarningShown = true;
            Debug.LogWarning($"{name}: LightController reference is missing. Assign it in Inspector so light duration can stop when the Endless run ends.", this);
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

        private void WarnMissingEventController()
        {
            if (missingEventControllerWarningShown)
            {
                return;
            }

            missingEventControllerWarningShown = true;
            Debug.LogWarning($"{name}: EndlessEventController reference is missing. Assign it in Inspector to run endless phase events.", this);
        }

        private void WarnMissingAutomaticSpawner()
        {
            if (missingAutomaticSpawnerWarningShown)
            {
                return;
            }

            missingAutomaticSpawnerWarningShown = true;
            Debug.LogWarning($"{name}: Automatic Spawner reference is missing. Assign a dedicated Spawner in Inspector to release endless potions.", this);
        }
    }
}
