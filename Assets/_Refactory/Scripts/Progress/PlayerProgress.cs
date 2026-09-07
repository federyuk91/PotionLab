using System;
using System.Collections.Generic;

namespace ProgressSystem
{
    [Serializable]
    public class PlayerProgress
    {
        public int schemaVersion = 1;

        public string playerName = "Player";

        public int totalDeaths;
        public int totalDrunkedPotions;
        public int totalTransformations;

        public int bestProceduralScore;
        public int lastProceduralScore;

        public int maxClassicLevelReached = 1;
        public bool endlessUnlocked;
        public List<int> classicLevelScores = new List<int>();
        public List<string> unlockedAchievementIds = new List<string>();

        public void EnsureClassicLevelCount(int levelCount)
        {
            if (classicLevelScores == null)
            {
                classicLevelScores = new List<int>();
            }

            if (levelCount < 0)
            {
                return;
            }

            while (classicLevelScores.Count < levelCount)
            {
                classicLevelScores.Add(0);
            }

            if (classicLevelScores.Count > levelCount)
            {
                classicLevelScores.RemoveRange(levelCount, classicLevelScores.Count - levelCount);
            }
        }
    }
}
