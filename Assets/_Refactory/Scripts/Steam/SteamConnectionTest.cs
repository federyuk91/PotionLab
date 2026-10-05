using Steamworks;
using UnityEngine;

public class SteamConnectionTest : MonoBehaviour
{
    private void Start()
    {
        if (!SteamManager.Initialized)
        {
            Debug.LogError("Steam non è stato inizializzato.");
            return;
        }

        // Mostra l'utente Steam attualmente connesso.
        Debug.Log($"Steam connesso: {SteamFriends.GetPersonaName()}");
    }
}