#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

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
        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private ProgressService progressService;
        [SerializeField, RequiredInspectorReference] private AchievementDatabase achievementDatabase;

        [Header("Steam Achievements")]
        [SerializeField] private bool submitExistingUnlockedOnStart = true;
        [SerializeField] private bool logKnownAchievementNamesOnFailure = true;

        private readonly HashSet<AchievementId> pendingAchievementIds = new HashSet<AchievementId>();

#if !DISABLESTEAMWORKS
        private CallResult<UserStatsReceived_t> userStatsReceivedResult;
        private bool statsReady;
        private bool statsRequestSent;
#endif

        private void Awake()
        {
#if !DISABLESTEAMWORKS
            userStatsReceivedResult = CallResult<UserStatsReceived_t>.Create(OnUserStatsReceived);
#endif
        }

        private void OnEnable()
        {
            if (progressService == null)
            {
                Debug.LogWarning($"{name}: ProgressService reference is missing. Assign it in Inspector to submit Steam achievements.", this);
                return;
            }

            if (achievementDatabase == null)
            {
                Debug.LogWarning($"{name}: AchievementDatabase reference is missing. Assign it in Inspector to submit Steam achievements.", this);
                return;
            }

            progressService.AchievementUnlocked += UnlockSteamAchievement;
        }

        private void Start()
        {
#if !DISABLESTEAMWORKS
            RequestCurrentStatsIfAvailable();
#endif

            if (!submitExistingUnlockedOnStart || progressService == null || progressService.Progress == null)
            {
                return;
            }

            List<AchievementId> unlockedAchievementIds = progressService.Progress.unlockedAchievementIds;
            if (unlockedAchievementIds == null)
            {
                return;
            }

            foreach (AchievementId achievementId in unlockedAchievementIds)
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

        private void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            userStatsReceivedResult?.Dispose();
#endif
        }

        private void Update()
        {
            if (pendingAchievementIds.Count == 0 || !CanUseSteam())
            {
                return;
            }

#if !DISABLESTEAMWORKS
            if (!statsReady)
            {
                RequestCurrentStatsIfAvailable();
                return;
            }

            FlushPendingAchievements();
#endif
        }

        public void UnlockSteamAchievement(AchievementId achievementId)
        {
            if (achievementId == AchievementId.None)
            {
                return;
            }

            if (!TryGetSteamApiName(achievementId, out string steamAchievementApiName))
            {
                Debug.LogWarning($"{name}: No Steam API name is configured for achievement '{achievementId}' in AchievementDatabase.", this);
                return;
            }

            if (!CanUseSteam())
            {
                pendingAchievementIds.Add(achievementId);
                return;
            }

#if !DISABLESTEAMWORKS
            if (!statsReady)
            {
                pendingAchievementIds.Add(achievementId);
                RequestCurrentStatsIfAvailable();
                return;
            }
#endif

            UnlockSteamAchievementNow(achievementId, steamAchievementApiName);
        }

        private void UnlockSteamAchievementNow(AchievementId achievementId, string steamAchievementApiName)
        {
#if !DISABLESTEAMWORKS
            if (!SteamListsAchievement(steamAchievementApiName))
            {
                Debug.LogWarning($"{name}: Unlocked local achievement '{achievementId}', but Steamworks does not list API name '{steamAchievementApiName}' for App ID {SteamUtils.GetAppID().m_AppId}.", this);
                LogKnownAchievementNamesIfUseful(steamAchievementApiName);
                return;
            }

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

            pendingAchievementIds.Remove(achievementId);
            Debug.Log($"{name}: Steam achievement '{steamAchievementApiName}' unlocked and submitted successfully.", this);
#endif
        }

        private bool TryGetSteamApiName(AchievementId achievementId, out string steamApiName)
        {
            if (achievementDatabase != null
                && achievementDatabase.TryGet(achievementId, out AchievementDatabase.AchievementDefinition definition)
                && !string.IsNullOrWhiteSpace(definition.steamApiName))
            {
                steamApiName = definition.steamApiName;
                return true;
            }

            steamApiName = string.Empty;
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
        private void RequestCurrentStatsIfAvailable()
        {
            if (!CanUseSteam() || statsReady || statsRequestSent)
            {
                return;
            }

            SteamAPICall_t statsRequest = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            if (statsRequest == SteamAPICall_t.Invalid)
            {
                Debug.LogWarning($"{name}: SteamUserStats.RequestUserStats failed. Pending achievements will be retried while Steam remains connected.", this);
                return;
            }

            statsRequestSent = true;
            userStatsReceivedResult.Set(statsRequest);
        }

        private void OnUserStatsReceived(UserStatsReceived_t callback, bool ioFailure)
        {
            uint currentAppId = SteamUtils.GetAppID().m_AppId;
            if (callback.m_nGameID != currentAppId)
            {
                return;
            }

            statsRequestSent = false;
            if (ioFailure || callback.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"{name}: Steam user stats could not be loaded for App ID {currentAppId}. Result: {callback.m_eResult}; IO failure: {ioFailure}. Pending achievements will be retried.", this);
                return;
            }

            statsReady = true;
            Debug.Log($"{name}: Steam user stats loaded. {pendingAchievementIds.Count} pending achievement(s) can now be processed.", this);
        }

        private static bool SteamListsAchievement(string steamAchievementApiName)
        {
            uint achievementCount = SteamUserStats.GetNumAchievements();
            for (uint achievementIndex = 0; achievementIndex < achievementCount; achievementIndex++)
            {
                if (SteamUserStats.GetAchievementName(achievementIndex) == steamAchievementApiName)
                {
                    return true;
                }
            }

            return false;
        }

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
            AchievementId[] achievementIds = new AchievementId[pendingAchievementIds.Count];
            pendingAchievementIds.CopyTo(achievementIds);

            foreach (AchievementId achievementId in achievementIds)
            {
                if (TryGetSteamApiName(achievementId, out string steamAchievementApiName))
                {
                    UnlockSteamAchievementNow(achievementId, steamAchievementApiName);
                }
            }
        }
#endif
    }
}
