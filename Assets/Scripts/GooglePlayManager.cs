using System;
using System.Threading.Tasks;
//using GooglePlayGames;
//using GooglePlayGames.BasicApi;
using Unity.Services.Analytics;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.UI;

using UnityEngine;

public class GooglePlayManager : MonoBehaviour
{

    /*
    public string GooglePlayToken;
    public string GooglePlayRefreshToken;
    public string GooglePlayError;



    public async Task Authenticate()
    {
        PlayGamesPlatform.Activate();
        await UnityServices.InitializeAsync();

        PlayGamesPlatform.Instance.Authenticate((success) =>
        {
            if (success == SignInStatus.Success)
            {
                Debug.Log("Login with Google was successful.");
                PlayGamesPlatform.Instance.RequestServerSideAccess(true, code =>
                {
                    Debug.Log($"Auth code is {code}");
                    
                    GooglePlayToken = code;
                });
            }
            else
            {
                GooglePlayError = "Failed to retrieve GPG auth code";
                Debug.LogError("Login Unsuccessful");
            }
        });

        await AuthenticateWithUnity();
    }

    private async Task AuthenticateWithUnity()
    {
        try
        {
            await AuthenticationService.Instance.SignInWithGoogleAsync(GooglePlayToken);
        }
        catch (AuthenticationException ex)
        {
            Debug.LogException(ex);
            throw;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogException(ex);
            throw;
        }
    }
    */
}