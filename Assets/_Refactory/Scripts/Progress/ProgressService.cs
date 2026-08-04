using System;
using UnityEngine;

namespace ProgressSystem
{
    public class ProgressService : MonoBehaviour
    {
        public event Action<PlayerProgress> ProgressChanged;

        [Header("Persistence")]
        [SerializeField] private MonoBehaviour repositoryBehaviour;

        [Header("Classic Mode")]
        [SerializeField] private int classicLevelCount = 25;
        [SerializeField] private int finalClassicLevelBuildIndex = 25;

        private IProgressRepository repository;
        private PlayerProgress progress;

        public PlayerProgress Progress => progress;
        public int FinalClassicLevelBuildIndex => finalClassicLevelBuildIndex;

        private void Awake()
        {
            ResolveRepository();
            LoadProgress();
        }

        public void AddRunStats(int deaths, int drunkedPotions, int transformations)
        {
            EnsureProgressLoaded();

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

            SaveProgress();
        }

        public void SaveProceduralScore(int score)
        {
            EnsureProgressLoaded();

            progress.lastProceduralScore = score;
            if (score > progress.bestProceduralScore)
            {
                progress.bestProceduralScore = score;
            }

            SaveProgress();
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

        private void ResolveRepository()
        {
            repository = repositoryBehaviour as IProgressRepository;
            if (repository != null)
            {
                return;
            }

            Debug.LogWarning($"{name}: SAVE: repositoryBehaviour is missing or does not implement IProgressRepository. Assign a JsonProgressRepository in Inspector.", this);
        }

        private void LoadProgress()
        {
            if (repository == null)
            {
                progress = new PlayerProgress();
                progress.EnsureClassicLevelCount(classicLevelCount);
                ProgressChanged?.Invoke(progress);
                return;
            }

            progress = repository.Load();
            progress.EnsureClassicLevelCount(classicLevelCount);
            ProgressChanged?.Invoke(progress);
        }

        private void SaveProgress()
        {
            progress.EnsureClassicLevelCount(classicLevelCount);

            if (repository == null)
            {
                Debug.LogWarning($"{name}: SAVE: Progress changed but was not saved because repositoryBehaviour is missing. Assign it in Inspector.", this);
                ProgressChanged?.Invoke(progress);
                return;
            }

            repository.Save(progress);
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
    }
}
