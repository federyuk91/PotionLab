#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using System.Text;
using InspectorValidation;
using TMPro;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamIntegration
{
    public sealed class SteamLeaderboardPanelController : MonoBehaviour
    {
        [Header("Leaderboard")]
        [SerializeField] private string leaderboardName = "endless_best_score";
        [SerializeField, Min(1)] private int maximumEntries = 15;

        [Header("UI")]
        [SerializeField, RequiredInspectorReference] private TMP_Text playerNamesText;
        [SerializeField, RequiredInspectorReference] private TMP_Text playerScoresText;

#if !DISABLESTEAMWORKS
        private CallResult<LeaderboardFindResult_t> leaderboardFindResult;
        private CallResult<LeaderboardScoresDownloaded_t> leaderboardScoresDownloadedResult;
        private SteamLeaderboard_t leaderboardHandle;
#endif

        private bool requestInProgress;

        private void Awake()
        {
#if !DISABLESTEAMWORKS
            leaderboardFindResult = CallResult<LeaderboardFindResult_t>.Create(OnLeaderboardFound);
            leaderboardScoresDownloadedResult = CallResult<LeaderboardScoresDownloaded_t>.Create(OnLeaderboardScoresDownloaded);
#endif
        }

        private void OnEnable()
        {
            SteamManager.SteamInitialized += OnSteamInitialized;
            RefreshLeaderboard();
        }

        private void OnDisable()
        {
            SteamManager.SteamInitialized -= OnSteamInitialized;
        }

        private void OnDestroy()
        {
#if !DISABLESTEAMWORKS
            leaderboardFindResult?.Dispose();
            leaderboardScoresDownloadedResult?.Dispose();
#endif
        }

        [ContextMenu("Refresh Steam Leaderboard")]
        public void RefreshLeaderboard()
        {
            if (!ValidateReferences())
            {
                return;
            }

            if (!CanUseSteam())
            {
                ShowMessage("STEAM OFFLINE", "-");
                return;
            }

            if (requestInProgress)
            {
                return;
            }

            if (string.IsNullOrWhiteSpace(leaderboardName))
            {
                ShowMessage("LEADERBOARD UNAVAILABLE", "-");
                Debug.LogWarning($"{name}: Steam leaderboard name is empty. Assign it in Inspector.", this);
                return;
            }

            requestInProgress = true;
            ShowMessage("LOADING...", string.Empty);

#if !DISABLESTEAMWORKS
            SteamAPICall_t apiCall = SteamUserStats.FindLeaderboard(leaderboardName);
            if (apiCall == SteamAPICall_t.Invalid)
            {
                requestInProgress = false;
                ShowDownloadFailure("Steam rejected the leaderboard lookup request.");
                return;
            }

            leaderboardFindResult.Set(apiCall);
#endif
        }

        private void OnSteamInitialized()
        {
            RefreshLeaderboard();
        }

        private bool ValidateReferences()
        {
            if (playerNamesText == null || playerScoresText == null)
            {
                Debug.LogError($"{name}: Assign both Player Names Text and Player Scores Text Inspector references.", this);
                return false;
            }

            return true;
        }

        private bool CanUseSteam()
        {
#if DISABLESTEAMWORKS
            return false;
#else
            return SteamManager.Initialized;
#endif
        }

        private void ShowMessage(string namesMessage, string scoresMessage)
        {
            playerNamesText.text = namesMessage;
            playerScoresText.text = scoresMessage;
        }

        private void ShowDownloadFailure(string reason)
        {
            ShowMessage("LEADERBOARD UNAVAILABLE", "-");
            Debug.LogWarning($"{name}: Could not download Steam leaderboard '{leaderboardName}'. {reason}", this);
        }

#if !DISABLESTEAMWORKS
        private void OnLeaderboardFound(LeaderboardFindResult_t result, bool ioFailure)
        {
            if (ioFailure || result.m_bLeaderboardFound == 0)
            {
                requestInProgress = false;
                ShowDownloadFailure(ioFailure
                    ? "Steam reported an IO failure while finding it."
                    : "The leaderboard was not found. Check that it is published in Steamworks.");
                return;
            }

            leaderboardHandle = result.m_hSteamLeaderboard;
            SteamAPICall_t apiCall = SteamUserStats.DownloadLeaderboardEntries(
                leaderboardHandle,
                ELeaderboardDataRequest.k_ELeaderboardDataRequestGlobal,
                1,
                maximumEntries);

            if (apiCall == SteamAPICall_t.Invalid)
            {
                requestInProgress = false;
                ShowDownloadFailure("Steam rejected the entries download request.");
                return;
            }

            leaderboardScoresDownloadedResult.Set(apiCall);
        }

        private void OnLeaderboardScoresDownloaded(LeaderboardScoresDownloaded_t result, bool ioFailure)
        {
            requestInProgress = false;
            if (ioFailure)
            {
                ShowDownloadFailure("Steam reported an IO failure while downloading entries.");
                return;
            }

            int entryCount = result.m_cEntryCount;
            if (entryCount <= 0)
            {
                ShowMessage("NO RECORDS YET", "-");
                Debug.Log($"{name}: Steam leaderboard '{leaderboardName}' contains no entries.", this);
                return;
            }

            StringBuilder namesBuilder = new StringBuilder();
            StringBuilder scoresBuilder = new StringBuilder();
            for (int entryIndex = 0; entryIndex < entryCount; entryIndex++)
            {
                LeaderboardEntry_t entry;
                bool entryRead = SteamUserStats.GetDownloadedLeaderboardEntry(
                    result.m_hSteamLeaderboardEntries,
                    entryIndex,
                    out entry,
                    null,
                    0);
                if (!entryRead)
                {
                    Debug.LogWarning($"{name}: Steam leaderboard entry {entryIndex} could not be read.", this);
                    continue;
                }

                if (namesBuilder.Length > 0)
                {
                    namesBuilder.AppendLine();
                    scoresBuilder.AppendLine();
                }

                string playerName = SteamFriends.GetFriendPersonaName(entry.m_steamIDUser);
                if (string.IsNullOrWhiteSpace(playerName))
                {
                    playerName = "UNKNOWN PLAYER";
                }

                namesBuilder.Append(entry.m_nGlobalRank);
                namesBuilder.Append(". ");
                namesBuilder.Append(playerName);
                scoresBuilder.Append(entry.m_nScore);
            }

            ShowMessage(namesBuilder.ToString(), scoresBuilder.ToString());
            Debug.Log($"{name}: Downloaded {entryCount} entries from Steam leaderboard '{leaderboardName}'.", this);
        }
#endif
    }
}
