using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;
using UnityEngine.Networking;

public class LootLockerLogin : MonoBehaviour
{
    public Leaderboard leaderBoard;
    public GooglePlayManager googleREF;
    public Text playerNameInputField;

    private string filePurpose = "saveFile"; //indica al serve che tipo di file caricare
    string fileNameOnServer = "save.lzr";

    private void Start()
    {
        StartCoroutine(SetUpRoutine());
    }

    IEnumerator SetUpRoutine()
    {
        yield return LoginRoutine();

        if (leaderBoard != null) { yield return leaderBoard.FetchTopHighScoreRoutine();  }

        yield return leaderBoard.FetchLeaderboardPosition();



    }

    public void SetPlayerName()
    {

        PlayerPrefs.SetString("PlayerName", playerNameInputField.text);
            LootLockerSDKManager.SetPlayerName(playerNameInputField.text, (response) =>
            {
                if (response.success)
                {
                    Debug.Log("Successfully set player name");
                }
                else
                {
                    Debug.Log("Could not set player name" + response.errorData);
                }
            });
        
    }

    IEnumerator LoginRoutine()
    {

        bool done = false;
        LootLockerSDKManager.StartGuestSession((response) =>
        {
            if (response.success)
            {
                Debug.Log("Player was logged in");
                PlayerPrefs.SetString("PlayerID", response.player_id.ToString());
                done = true;
                DataSaver.instance.InitializeData();
            }

            else
            {
                Debug.Log("Could not start a session!");
            }
        });

        yield return new WaitWhile(() => done == false);



    }

    /*
        public void LoginGoogle()
        {
            LootLockerSDKManager.StartGoogleSession(googleREF.GooglePlayToken, (response) =>
            {
                if (!response.success)
                {
                    Debug.Log("error starting LootLocker session");

                    return;
                }

                Debug.Log("session started successfully");
                Debug.Log(response.player_id.ToString());

                // Store these to be able to refresh the session without using the full sign in flow
                string refreshToken = response.refresh_token;
                googleREF.GooglePlayRefreshToken = refreshToken;
            });
        }*/

    public void UploadFile(byte[] saveFile)
    {
        Debug.Log("Save uploading");
        LootLockerSDKManager.UploadPlayerFile(saveFile, filePurpose, fileNameOnServer, false, (response) =>
        {
            // Save the file id in PlayerPrefs
            PlayerPrefs.SetInt("PlayerSaveDataFileID", response.id);
        });
    }

    public void UpdatingFile(byte[] saveFile)
    {
        if (!PlayerPrefs.HasKey("PlayerSaveDataFileID"))
        {
            Debug.LogWarning("Save file id not found");
            return;
        }
        int fileID = PlayerPrefs.GetInt("PlayerSaveDataFileID");

        LootLockerSDKManager.UpdatePlayerFile(fileID, saveFile, (response) =>
        {
            if (response.success)
            {
                Debug.Log("File was uploaded!");
            }
            else
            {
                Debug.Log("File was not uploaded:" + response.EventId);
            }
        });
    }

    public void DeleteFiles(byte[] saveFile)
    {
        int fileID = PlayerPrefs.GetInt("playerSaveFileID");
        LootLockerSDKManager.DeletePlayerFile(fileID, (response) =>
        {
            Debug.Log("Save File was deleted.");
        });
    }

    public void GetPlayerFileData()
    {
     
        int fileID = PlayerPrefs.GetInt("PlayerSaveDataFileID"); //"PlayerSaveDataFileID"

        LootLockerSDKManager.GetPlayerFile(fileID, (response) =>
        {
            //Se esiste
            if (response.success)
            {
                Debug.Log("Retrieved URL");
                StartCoroutine(Download(response.url, (fileContent) =>
                {
                    Debug.Log("File is downloaded");
                    // Do something with the content
                    //binaryFile;

                }));
            }
            // Se non esiste
            else
            {
                //Creo e carico vuoto! e salvo una ref                
                UploadFile(DataSaver.instance.SaveToByteArray());
                Debug.Log("File caricato");
            }
                
        });
    }

    public IEnumerator Download(string url, Action<string> fileContent)
    {
        UnityWebRequest www = new UnityWebRequest(url);
        www.downloadHandler = new DownloadHandlerBuffer();
        yield return www.SendWebRequest();

        if (www.result != UnityWebRequest.Result.Success)
        {
            Debug.Log(www.error);
        }
        else
        {
            fileContent(www.downloadHandler.text);
            byte[] barray = www.downloadHandler.data;
            DataSaver.instance.ReadFromByteArray(barray);
            //Debug.Log("Max level reached by binary file downloaded is");
            DataSaver.instance.InitializeScores();
        }
    }
}
