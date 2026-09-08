#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using InspectorValidation;
using ProgressSystem;
using TMPro;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamIntegration
{
    public sealed class SteamStatsController : MonoBehaviour
    {
        private const string TotalDeathsApiName = "TOTAL_DEATHS";
        private const string TotalPotionsDrunkApiName = "TOTAL_POTIONS_DRUNK";
        private const string TotalTransformationsApiName = "TOTAL_TRANSFORMATIONS";
        private const string BestEndlessScoreApiName = "BEST_ENDLESS_SCORE";
        private const string LastEndlessScoreApiName = "LAST_ENDLESS_SCORE";
        private const string MaxClassicLevelApiName = "MAX_CLASSIC_LEVEL";

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private ProgressService progressService;
        [Header("Optional Records UI")]
        [SerializeField] private TMP_Text labelsText;
        [SerializeField] private TMP_Text primaryValuesText;
        [SerializeField] private TMP_Text secondaryValuesText;
        [SerializeField] private TMP_Text tertiaryValuesText;

#if !DISABLESTEAMWORKS
        private CallResult<UserStatsReceived_t> userStatsReceivedResult;
        private Callback<UserStatsStored_t> userStatsStoredCallback;
#endif

        private bool requestInProgress;
        private bool statsReady;
        private bool applyingSteamStats;
        private bool storePending;

        private void Awake()
        {
#if !DISABLESTEAMWORKS
            userStatsReceivedResult = CallResult<UserStatsReceived_t>.Create(OnUserStatsReceived);
            userStatsStoredCallback = Callback<UserStatsStored_t>.Create(OnUserStatsStored);
#endif
        }

        private void OnEnable()
        {
            SteamManager.SteamInitialized += OnSteamInitialized;
            if (progressService != null)
            {
                progressService.ProgressChanged += OnProgressChanged;
            }

            RefreshDisplay();
            RequestSteamStats();
        }

        private void OnDisable()
        {
            SteamManager.SteamInitialized -= OnSteamInitialized;
            if (progressService != null)
            {
                progressService.ProgressChanged -= OnProgressChanged;
            }
        }

        private void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            userStatsReceivedResult?.Dispose();
            userStatsStoredCallback?.Dispose();
#endif
        }

        private void OnApplicationQuit()
        {
#if !DISABLESTEAMWORKS
            if (statsReady && CanUseSteam())
            {
                Debug.Log($"{name}: Application quit requested; flushing Steam stats.", this);
                UploadCurrentStats();
            }
#endif
        }

        [ContextMenu("Refresh Steam Stats")]
        public void RequestSteamStats()
        {
            if (!ValidateProgressService() || requestInProgress || !CanUseSteam())
            {
                return;
            }

#if !DISABLESTEAMWORKS
            SteamAPICall_t apiCall = SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
            if (apiCall == SteamAPICall_t.Invalid)
            {
                Debug.LogWarning($"{name}: Steam rejected the user stats request.", this);
                return;
            }

            requestInProgress = true;
            userStatsReceivedResult.Set(apiCall);
#endif
        }

        private void OnSteamInitialized()
        {
            RequestSteamStats();
        }

        private void OnProgressChanged(PlayerProgress progress)
        {
            RefreshDisplay();
            if (statsReady && !applyingSteamStats)
            {
                UploadCurrentStats();
            }
        }

        private void RefreshDisplay()
        {
            if (!HasCompleteUi() || progressService == null || progressService.Progress == null)
            {
                return;
            }

            PlayerProgress progress = progressService.Progress;
            labelsText.text = "POTIONS DRUNK\nDEATHS\nTRANSFORMATIONS\nBEST ENDLESS\nLAST ENDLESS\nCLASSIC LEVEL";
            primaryValuesText.text = $"{progress.totalDrunkedPotions}\n{progress.totalDeaths}";
            secondaryValuesText.text = $"{progress.totalTransformations}\n{progress.bestProceduralScore}";
            tertiaryValuesText.text = $"{progress.lastProceduralScore}\n{progress.maxClassicLevelReached}";
        }

        private bool ValidateProgressService()
        {
            if (progressService == null)
            {
                Debug.LogError($"{name}: Assign the Progress Service Inspector reference.", this);
                return false;
            }

            return true;
        }

        private bool HasCompleteUi()
        {
            return labelsText != null
                && primaryValuesText != null
                && secondaryValuesText != null
                && tertiaryValuesText != null;
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
        private void OnUserStatsReceived(UserStatsReceived_t result, bool ioFailure)
        {
            requestInProgress = false;
            uint currentAppId = SteamUtils.GetAppID().m_AppId;
            if (result.m_nGameID != currentAppId)
            {
                return;
            }

            if (ioFailure || result.m_eResult != EResult.k_EResultOK)
            {
                Debug.LogWarning($"{name}: Steam stats download failed. Result: {result.m_eResult}; IO failure: {ioFailure}.", this);
                return;
            }

            bool allStatsRead = true;
            allStatsRead &= TryGetIntStat(TotalDeathsApiName, out int steamDeaths);
            allStatsRead &= TryGetIntStat(TotalPotionsDrunkApiName, out int steamPotions);
            allStatsRead &= TryGetIntStat(TotalTransformationsApiName, out int steamTransformations);
            allStatsRead &= TryGetIntStat(BestEndlessScoreApiName, out int steamBestEndless);
            allStatsRead &= TryGetIntStat(LastEndlessScoreApiName, out int steamLastEndless);
            allStatsRead &= TryGetIntStat(MaxClassicLevelApiName, out int steamMaxClassicLevel);

            applyingSteamStats = true;
            progressService.MergeSteamStats(
                steamDeaths,
                steamPotions,
                steamTransformations,
                steamBestEndless,
                steamMaxClassicLevel);
            applyingSteamStats = false;
            statsReady = true;
            RefreshDisplay();
            UploadCurrentStats();

            string status = allStatsRead ? "all API names resolved" : "one or more API names missing";
            Debug.Log($"{name}: Steam stats downloaded and merged; {status}. Steam LAST_ENDLESS_SCORE was {steamLastEndless}.", this);
        }

        private bool TryGetIntStat(string apiName, out int value)
        {
            if (SteamUserStats.GetStat(apiName, out value))
            {
                return true;
            }

            value = 0;
            Debug.LogWarning($"{name}: Steam stat '{apiName}' could not be read. Check its API name, INT type, and published state in Steamworks.", this);
            return false;
        }

        private void UploadCurrentStats()
        {
            if (!statsReady || progressService == null || progressService.Progress == null || !CanUseSteam())
            {
                return;
            }

            PlayerProgress progress = progressService.Progress;
            bool allStatsSet = true;
            allStatsSet &= TrySetIntStat(TotalDeathsApiName, progress.totalDeaths);
            allStatsSet &= TrySetIntStat(TotalPotionsDrunkApiName, progress.totalDrunkedPotions);
            allStatsSet &= TrySetIntStat(TotalTransformationsApiName, progress.totalTransformations);
            allStatsSet &= TrySetIntStat(BestEndlessScoreApiName, progress.bestProceduralScore);
            allStatsSet &= TrySetIntStat(LastEndlessScoreApiName, progress.lastProceduralScore);
            allStatsSet &= TrySetIntStat(MaxClassicLevelApiName, progress.maxClassicLevelReached);

            if (!allStatsSet)
            {
                return;
            }

            if (!SteamUserStats.StoreStats())
            {
                Debug.LogWarning($"{name}: Steam StoreStats rejected the records update.", this);
                return;
            }

            storePending = true;
        }

        private bool TrySetIntStat(string apiName, int value)
        {
            if (SteamUserStats.SetStat(apiName, value))
            {
                return true;
            }

            Debug.LogWarning($"{name}: Steam stat '{apiName}' could not be set to {value}. Check its API name, INT type, constraints, and published state in Steamworks.", this);
            return false;
        }

        private void OnUserStatsStored(UserStatsStored_t result)
        {
            uint currentAppId = SteamUtils.GetAppID().m_AppId;
            if (result.m_nGameID != currentAppId || !storePending)
            {
                return;
            }

            storePending = false;

            if (result.m_eResult == EResult.k_EResultOK)
            {
                Debug.Log($"{name}: All six Steam stats were stored successfully.", this);
                return;
            }

            Debug.LogWarning($"{name}: Steam stats storage failed for App ID {currentAppId}. Result: {result.m_eResult}.", this);
        }
#endif
    }
}
