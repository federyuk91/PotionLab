#if !(UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX || UNITY_STANDALONE_OSX || STEAMWORKS_WIN || STEAMWORKS_LIN_OSX)
#define DISABLESTEAMWORKS
#endif

using ProgressSystem;
using UnityEngine;

#if !DISABLESTEAMWORKS
using Steamworks;
#endif

namespace SteamIntegration
{
    public sealed class SteamPlayerNameProvider : MonoBehaviour, IPlayerNameProvider
    {
        public bool TryGetPlayerName(out string playerName, out string failureReason)
        {
            playerName = string.Empty;
            failureReason = string.Empty;

#if DISABLESTEAMWORKS
            failureReason = "Steamworks is disabled for this build target or scripting define configuration.";
            return false;
#else
            if (!SteamManager.Initialized)
            {
                failureReason = "SteamManager.Initialized is false.";
                return false;
            }

            string steamPlayerName = SteamFriends.GetPersonaName();
            if (string.IsNullOrWhiteSpace(steamPlayerName))
            {
                failureReason = "SteamFriends.GetPersonaName returned an empty name.";
                return false;
            }

            playerName = steamPlayerName.Trim();
            return true;
#endif
        }
    }
}
