using System;
using UnityEngine;

namespace ProgressSystem
{
    public class ProgressService : MonoBehaviour
    {
        public event Action<PlayerProgress> ProgressChanged;
        public event Action<int> BestProceduralScoreChanged;
        public event Action<string> AchievementUnlocked;

        [Header("Persistence")]
        [SerializeField] private MonoBehaviour repositoryBehaviour;
        [SerializeField] private MonoBehaviour playerNameProviderBehaviour;

        [Header("Classic Mode")]
        [SerializeField] private int classicLevelCount = 25;
        [SerializeField] private int finalClassicLevelBuildIndex = 25;

        [Header("Diagnostics")]
        [SerializeField] private bool logPlayerNameFlow = true;

        private IProgressRepository repository;
        private IPlayerNameProvider playerNameProvider;
        private PlayerProgress progress;

        public PlayerProgress Progress => progress;
        public int FinalClassicLevelBuildIndex => finalClassicLevelBuildIndex;

        private void Awake()
        {
            ResolveRepository();
            ResolvePlayerNameProvider();
            LoadProgress();
        }

        private void OnEnable()
        {
            SteamManager.SteamInitialized += OnSteamInitialized;
        }

        private void OnDisable()
        {
            SteamManager.SteamInitialized -= OnSteamInitialized;
        }

        private void Start()
        {
            LogPlayerNameFlow("Start refresh requested.");
            RefreshPlayerNameFromProvider();
        }

        public void AddRunStats(int deaths, int drunkedPotions, int transformations)
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();

            progress.totalDeaths += deaths;
            progress.totalDrunkedPotions += drunkedPotions;
            progress.totalTransformations += transformations;

            SaveProgress();
        }

        public void SaveClassicLevelResult(int sceneBuildIndex, int score)
        {
            if (sceneBuildIndex <= 0)
            {
                Debug.LogWarning($"{name}: SAVE: Cannot save classic score because scene build index {sceneBuildIndex} does not map to a classic level.", this);
                return;
            }

            EnsureProgressLoaded();
            EnsureProgressDefaults();

            int levelIndex = sceneBuildIndex - 1;
            progress.EnsureClassicLevelCount(classicLevelCount);

            if (levelIndex >= progress.classicLevelScores.Count)
            {
                Debug.LogWarning($"{name}: SAVE: Cannot save classic score for level index {levelIndex}. Increase classicLevelCount in Inspector if this is a valid puzzle level.", this);
                return;
            }

            if (score > progress.classicLevelScores[levelIndex])
            {
                progress.classicLevelScores[levelIndex] = score;
            }

            if (sceneBuildIndex != finalClassicLevelBuildIndex && sceneBuildIndex + 1 > progress.maxClassicLevelReached)
            {
                progress.maxClassicLevelReached = sceneBuildIndex + 1;
            }

            if (sceneBuildIndex >= finalClassicLevelBuildIndex)
            {
                progress.endlessUnlocked = true;
            }

            SaveProgress();
        }

        public void SaveProceduralScore(int score)
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();

            bool isNewBestScore = score > progress.bestProceduralScore;
            progress.lastProceduralScore = score;
            if (isNewBestScore)
            {
                progress.bestProceduralScore = score;
            }

            SaveProgress();

            if (isNewBestScore)
            {
                BestProceduralScoreChanged?.Invoke(progress.bestProceduralScore);
            }
        }

        public void ResetProgress()
        {
            if (repository == null)
            {
                Debug.LogWarning($"{name}: SAVE: Cannot reset progress because repositoryBehaviour is missing or does not implement IProgressRepository. Assign it in Inspector.", this);
                return;
            }

            repository.Delete();
            LoadProgress();
            SaveProgress();
        }

        public void UnlockAchievement(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                Debug.LogWarning($"{name}: SAVE: Cannot unlock an empty achievement id.", this);
                return;
            }

            EnsureProgressLoaded();
            EnsureProgressDefaults();

            if (progress.unlockedAchievementIds.Contains(achievementId))
            {
                return;
            }

            progress.unlockedAchievementIds.Add(achievementId);
            SaveProgress();
            AchievementUnlocked?.Invoke(achievementId);
        }

        public bool IsAchievementUnlocked(string achievementId)
        {
            if (string.IsNullOrWhiteSpace(achievementId))
            {
                return false;
            }

            EnsureProgressLoaded();
            EnsureProgressDefaults();
            return progress.unlockedAchievementIds.Contains(achievementId);
        }

        [ContextMenu("Refresh Player Name From Provider")]
        public void RefreshPlayerNameFromProvider()
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();

            if (playerNameProvider == null)
            {
                LogPlayerNameFlow("Player name refresh failed: playerNameProvider is null. Check ProgressService.playerNameProviderBehaviour in Inspector.");
                return;
            }

            if (!playerNameProvider.TryGetPlayerName(out string playerName, out string failureReason))
            {
                LogPlayerNameFlow($"Player name refresh failed: {failureReason}");
                return;
            }

            if (progress.playerName == playerName)
            {
                LogPlayerNameFlow($"Player name refresh skipped: progress already contains '{playerName}'.");
                return;
            }

            LogPlayerNameFlow($"Player name refresh succeeded: '{progress.playerName}' -> '{playerName}'.");
            progress.playerName = playerName;
            SaveProgress();
        }

        public bool IsClassicLevelUnlocked(int sceneBuildIndex)
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();

            return sceneBuildIndex > 0
                && sceneBuildIndex <= finalClassicLevelBuildIndex
                && sceneBuildIndex <= progress.maxClassicLevelReached;
        }

        public bool IsEndlessUnlocked()
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();
            return progress.endlessUnlocked;
        }

        [ContextMenu("Unlock All Levels For Development")]
        public void UnlockAll()
        {
            EnsureProgressLoaded();
            EnsureProgressDefaults();

            progress.maxClassicLevelReached = finalClassicLevelBuildIndex;
            progress.endlessUnlocked = true;
            SaveProgress();
        }

        private void ResolveRepository()
        {
            repository = repositoryBehaviour as IProgressRepository;
            if (repository != null)
            {
                LogPlayerNameFlow($"Progress repository resolved from '{repositoryBehaviour.name}'.");
                return;
            }

            Debug.LogWarning($"{name}: SAVE: repositoryBehaviour is missing or does not implement IProgressRepository. Assign a JsonProgressRepository in Inspector.", this);
        }

        private void ResolvePlayerNameProvider()
        {
            playerNameProvider = playerNameProviderBehaviour as IPlayerNameProvider;
            if (playerNameProvider != null || playerNameProviderBehaviour == null)
            {
                if (playerNameProvider != null)
                {
                    LogPlayerNameFlow($"Player name provider resolved from '{playerNameProviderBehaviour.name}'.");
                }
                else
                {
                    LogPlayerNameFlow("Player name provider is not assigned; keeping saved/fallback player name.");
                }

                return;
            }

            Debug.LogWarning($"{name}: SAVE: playerNameProviderBehaviour does not implement IPlayerNameProvider. Assign a valid player name provider in Inspector.", this);
        }

        private void OnSteamInitialized()
        {
            LogPlayerNameFlow("SteamManager reported Steam initialized; refreshing player name.");
            RefreshPlayerNameFromProvider();
        }

        private void LoadProgress()
        {
            if (repository == null)
            {
                progress = new PlayerProgress();
                EnsureProgressDefaults();
                LogPlayerNameFlow($"Progress loaded without repository. Current player name: '{progress.playerName}'.");
                ProgressChanged?.Invoke(progress);
                return;
            }

            progress = repository.Load();
            EnsureProgressDefaults();
            LogPlayerNameFlow($"Progress loaded from repository. Current player name: '{progress.playerName}'.");
            ProgressChanged?.Invoke(progress);
        }

        private void SaveProgress()
        {
            EnsureProgressDefaults();

            if (repository == null)
            {
                Debug.LogWarning($"{name}: SAVE: Progress changed but was not saved because repositoryBehaviour is missing. Assign it in Inspector.", this);
                ProgressChanged?.Invoke(progress);
                return;
            }

            repository.Save(progress);
            LogPlayerNameFlow($"Progress saved. Current player name: '{progress.playerName}'.");
            ProgressChanged?.Invoke(progress);
        }

        private void EnsureProgressLoaded()
        {
            if (progress != null)
            {
                return;
            }

            LoadProgress();
        }

        private void EnsureProgressDefaults()
        {
            progress.EnsureClassicLevelCount(classicLevelCount);

            if (string.IsNullOrWhiteSpace(progress.playerName))
            {
                progress.playerName = "Player";
            }

            if (progress.maxClassicLevelReached < 1)
            {
                progress.maxClassicLevelReached = 1;
            }

            if (progress.maxClassicLevelReached > finalClassicLevelBuildIndex)
            {
                progress.maxClassicLevelReached = finalClassicLevelBuildIndex;
            }

            if (progress.unlockedAchievementIds == null)
            {
                progress.unlockedAchievementIds = new System.Collections.Generic.List<string>();
            }
        }

        private void LogPlayerNameFlow(string message)
        {
            if (!logPlayerNameFlow)
            {
                return;
            }

            Debug.Log($"{name}: PLAYER NAME: {message}", this);
        }
    }
}
