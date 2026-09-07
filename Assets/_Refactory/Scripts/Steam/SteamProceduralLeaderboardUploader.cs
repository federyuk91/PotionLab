#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using InspectorValidation;
using ProgressSystem;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamIntegration
{
    public sealed class SteamProceduralLeaderboardUploader : MonoBehaviour
    {
        private static readonly int[] EmptyScoreDetails = new int[0];

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private ProgressService progressService;

        [Header("Steam Leaderboard")]
        [SerializeField] private string leaderboardName = "endless_best_score";
        [SerializeField] private bool createLeaderboardIfMissing = true;
        [SerializeField] private bool submitExistingBestOnStart = true;

#if !DISABLESTEAMWORKS
        private CallResult<LeaderboardFindResult_t> leaderboardFindResult;
        private CallResult<LeaderboardScoreUploaded_t> scoreUploadedResult;
        private SteamLeaderboard_t leaderboardHandle;
#endif

        private bool leaderboardReady;
        private int pendingScore;
        private bool hasPendingScore;

        private void Awake()
        {
#if !DISABLESTEAMWORKS
            leaderboardFindResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
            scoreUploadedResult = CallResult<LeaderboardScoreUploaded_t>.Create(OnScoreUploaded);
#endif
        }

        private void OnEnable()
        {
            if (progressService == null)
            {
                Debug.LogWarning($"{name}: ProgressService reference is missing. Assign it in Inspector to submit Steam leaderboard scores.", this);
                return;
            }

            progressService.BestProceduralScoreChanged += SubmitScore;
        }

        private void Start()
        {
            RequestLeaderboard();

            if (!submitExistingBestOnStart || progressService == null || progressService.Progress == null)
            {
                return;
            }

            int bestScore = progressService.Progress.bestProceduralScore;
            if (bestScore > 0)
            {
                SubmitScore(bestScore);
            }
        }

        private void OnDisable()
        {
            if (progressService != null)
            {
                progressService.BestProceduralScoreChanged -= SubmitScore;
            }
        }

        private void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            leaderboardFindResult?.Dispose();
            scoreUploadedResult?.Dispose();
#endif
        }

        public void SubmitScore(int score)
        {
            if (score <= 0)
            {
                return;
            }

            if (!CanUseSteam())
            {
                hasPendingScore = true;
                pendingScore = Mathf.Max(pendingScore, score);
                return;
            }

            if (!leaderboardReady)
            {
                hasPendingScore = true;
                pendingScore = Mathf.Max(pendingScore, score);
                RequestLeaderboard();
                return;
            }

            UploadScore(score);
        }

        private void RequestLeaderboard()
        {
            if (leaderboardReady || !CanUseSteam())
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(leaderboardName))
            {
                Debug.LogWarning($"{name}: Steam leaderboard name is empty. Assign it in Inspector.", this);
                return;
            }

#if !DISABLESTEAMWORKS
            SteamAPICall_t apiCall = createLeaderboardIfMissing
                ? SteamUserStats.FindOrCreateLeaderboard(
                    leaderboardName,
                    ELeaderboardSortMethod.k_ELeaderboardSortMethodDescending,
                    ELeaderboardDisplayType.k_ELeaderboardDisplayTypeNumeric)
                : SteamUserStats.FindLeaderboard(leaderboardName);

            leaderboardFindResult.Set(apiCall);
#endif
        }

        private bool CanUseSteam()
        {
#if DISABLESTEAMWORKS
            return false;
#else
            return SteamManager.Initialized;
#endif
        }

        private void UploadScore(int score)
        {
#if !DISABLESTEAMWORKS
            SteamAPICall_t apiCall = SteamUserStats.UploadLeaderboardScore(
                leaderboardHandle,
                ELeaderboardUploadScoreMethod.k_ELeaderboardUploadScoreMethodKeepBest,
                score,
                EmptyScoreDetails,
                0);

            scoreUploadedResult.Set(apiCall);
#endif
        }

#if !DISABLESTEAMWORKS
        private void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bLeaderboardFound == 0)
            {
                Debug.LogWarning($"{name}: Steam leaderboard '{leaderboardName}' was not found or could not be created.", this);
                return;
            }

            leaderboardHandle = result.m_hSteamLeaderboard;
            leaderboardReady = true;

            if (!hasPendingScore)
            {
                return;
            }

            int score = pendingScore;
            pendingScore = 0;
            hasPendingScore = false;
            UploadScore(score);
        }

        private void OnScoreUploaded(LeaderboardScoreUploaded_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bSuccess == 0)
            {
                Debug.LogWarning($"{name}: Steam leaderboard score upload failed.", this);
            }
        }
#endif
    }
}
