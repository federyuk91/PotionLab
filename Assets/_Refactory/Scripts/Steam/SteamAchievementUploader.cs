#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System;
using System.Collections.Generic;
using InspectorValidation;
using ProgressSystem;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamIntegration
{
    public sealed class SteamAchievementUploader : MonoBehaviour
    {
        [Serializable]
        public sealed class SteamAchievementMapping
        {
            public string progressAchievementId;
            public string steamAchievementApiName;
        }

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private ProgressService progressService;

        [Header("Steam Achievements")]
        [SerializeField] private bool submitExistingUnlockedOnStart = true;
        [SerializeField] private bool useProgressIdWhenMappingMissing = true;
        [SerializeField] private bool logKnownAchievementNamesOnFailure = true;
        [SerializeField] private SteamAchievementMapping[] achievementMappings = new SteamAchievementMapping[0];

        private readonly Dictionary<string, string> steamApiNameByProgressId = new Dictionary<string, string>();
        private readonly HashSet<string> pendingAchievementIds = new HashSet<string>();

        private void Awake()
        {
            BuildAchievementMapping();
        }

        private void OnEnable()
        {
            if (progressService == null)
            {
                Debug.LogWarning($"{name}: ProgressService reference is missing. Assign it in Inspector to submit Steam achievements.", this);
                return;
            }

            progressService.AchievementUnlocked += UnlockSteamAchievement;
        }

        private void Start()
        {
            if (!submitExistingUnlockedOnStart || progressService == null || progressService.Progress == null)
            {
                return;
            }

            List<string> unlockedAchievementIds = progressService.Progress.unlockedAchievementIds;
            if (unlockedAchievementIds == null)
            {
                return;
            }

            foreach (string achievementId in unlockedAchievementIds)
            {
                UnlockSteamAchievement(achievementId);
            }
        }

        private void OnDisable()
        {
            if (progressService != null)
            {
                progressService.AchievementUnlocked -= UnlockSteamAchievement;
            }
        }

        private void Update()
        {
            if (pendingAchievementIds.Count == 0 || !CanUseSteam())
            {
                return;
            }

#if !DISABLESTEAMWORKS
            FlushPendingAchievements();
#endif
        }

        public void UnlockSteamAchievement(string progressAchievementId)
        {
            if (string.IsNullOrWhiteSpace(progressAchievementId))
            {
                return;
            }

            if (!TryGetSteamApiName(progressAchievementId, out string steamAchievementApiName))
            {
                Debug.LogWarning($"{name}: No Steam achievement API name mapped for progress id '{progressAchievementId}'.", this);
                return;
            }

            if (!CanUseSteam())
            {
                pendingAchievementIds.Add(progressAchievementId);
                return;
            }

            UnlockSteamAchievementNow(progressAchievementId, steamAchievementApiName);
        }

        private void UnlockSteamAchievementNow(string progressAchievementId, string steamAchievementApiName)
        {
#if !DISABLESTEAMWORKS
            if (!SteamUserStats.SetAchievement(steamAchievementApiName))
            {
                Debug.LogWarning($"{name}: Steam achievement '{steamAchievementApiName}' could not be unlocked. Check that the API name exists, is published in Steamworks, and belongs to the current App ID.", this);
                LogKnownAchievementNamesIfUseful(steamAchievementApiName);
                return;
            }

            if (!SteamUserStats.StoreStats())
            {
                Debug.LogWarning($"{name}: Steam achievement '{steamAchievementApiName}' was set locally, but StoreStats failed.", this);
                return;
            }

            pendingAchievementIds.Remove(progressAchievementId);
#endif
        }

        private void BuildAchievementMapping()
        {
            steamApiNameByProgressId.Clear();

            if (achievementMappings == null)
            {
                return;
            }

            foreach (SteamAchievementMapping mapping in achievementMappings)
            {
                if (mapping == null || string.IsNullOrWhiteSpace(mapping.progressAchievementId) || string.IsNullOrWhiteSpace(mapping.steamAchievementApiName))
                {
                    continue;
                }

                steamApiNameByProgressId[mapping.progressAchievementId] = mapping.steamAchievementApiName;
            }
        }

        private bool TryGetSteamApiName(string progressAchievementId, out string steamAchievementApiName)
        {
            if (steamApiNameByProgressId.TryGetValue(progressAchievementId, out steamAchievementApiName))
            {
                return true;
            }

            if (useProgressIdWhenMappingMissing)
            {
                steamAchievementApiName = progressAchievementId;
                return true;
            }

            steamAchievementApiName = string.Empty;
            return false;
        }

        private bool CanUseSteam()
        {
#if DISABLESTEAMWORKS
            return false;
#else
            return SteamManager.Initialized;
#endif
        }

#if !DISABLESTEAMWORKS
        private void LogKnownAchievementNamesIfUseful(string failedSteamAchievementApiName)
        {
            if (!logKnownAchievementNamesOnFailure)
            {
                return;
            }

            uint achievementCount = SteamUserStats.GetNumAchievements();
            if (achievementCount == 0)
            {
                Debug.LogWarning($"{name}: Steam reports 0 achievements for App ID {SteamUtils.GetAppID().m_AppId}. The achievement '{failedSteamAchievementApiName}' is probably not published for this app yet.", this);
                return;
            }

            List<string> achievementNames = new List<string>();
            bool failedNameExists = false;
            for (uint achievementIndex = 0; achievementIndex < achievementCount; achievementIndex++)
            {
                string achievementName = SteamUserStats.GetAchievementName(achievementIndex);
                achievementNames.Add(achievementName);

                if (achievementName == failedSteamAchievementApiName)
                {
                    failedNameExists = true;
                }
            }

            if (failedNameExists)
            {
                Debug.LogWarning($"{name}: Steam knows achievement '{failedSteamAchievementApiName}', but SetAchievement still failed. Check Steam initialization, account/app access, and whether Steam is running under the correct App ID.", this);
                return;
            }

            Debug.LogWarning($"{name}: Steam does not list achievement '{failedSteamAchievementApiName}' for App ID {SteamUtils.GetAppID().m_AppId}. Known achievements: {string.Join(", ", achievementNames)}", this);
        }

        private void FlushPendingAchievements()
        {
            string[] achievementIds = new string[pendingAchievementIds.Count];
            pendingAchievementIds.CopyTo(achievementIds);

            foreach (string progressAchievementId in achievementIds)
            {
                if (TryGetSteamApiName(progressAchievementId, out string steamAchievementApiName))
                {
                    UnlockSteamAchievementNow(progressAchievementId, steamAchievementApiName);
                }
            }
        }
#endif
    }
}
