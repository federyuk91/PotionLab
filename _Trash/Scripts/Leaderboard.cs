using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LootLocker.Requests;
using UnityEngine.UI;

public class Leaderboard : MonoBehaviour
{
    int leaderboardID = 19402;
    public Text playersNames, playersScore, playerPosition;
    // Start is called before the first frame update


    private void Start()
    {
      if(PlayerPrefs.GetInt("BestScore") > 25){
            ConfigureLeaderBoard(PlayerPrefs.GetInt("BestScore"));
        }
    }

    public void ConfigureLeaderBoard(int i)
    {
        StartCoroutine(SubmitScoreRoutine(i));
    }

    public IEnumerator SubmitScoreRoutine(int scoreToUpload)
    {

        Debug.Log("LeaderBoard");
        bool done = false;
        string playerID = PlayerPrefs.GetString("PlayerID");
        Debug.Log("Player ID: " + playerID);
        LootLockerSDKManager.SubmitScore(playerID, scoreToUpload, leaderboardID.ToString(), (response) =>
        {
            if (response.success)
            {
                Debug.Log("Successfully Upload Score");
                done = true;
            }
            else
            {
                Debug.Log("Failed" + response.errorData);
                done = true;
            }
        });

        yield return new WaitWhile(() => done == false);
    }


    public IEnumerator FetchTopHighScoreRoutine()
    {
        bool done = false;
        LootLockerSDKManager.GetScoreList(leaderboardID.ToString(), 25, 0, (response) =>
        {
            if (response.success)
            {
                string tempPlayerNames = "\n";
                string tempPlayerScore = "\n";

                LootLockerLeaderboardMember[] members = response.items;

                for (int i = 0; i < members.Length; i++)
                {
                    tempPlayerNames += members[i].rank + ". ";

                    if (members[i].player.name != "")
                    {
                        tempPlayerNames += members[i].player.name;
                    }
                    else
                    {
                        tempPlayerNames += members[i].player.id;
                    }

                    tempPlayerScore += members[i].score + "\n";
                    tempPlayerNames += "\n";
                }

                done = true;
                playersNames.text = tempPlayerNames;
                playersScore.text = tempPlayerScore;

            }
            else
            {
                Debug.Log("Failed" + response.errorData);
                done = true;
            }
        });

        yield return new WaitWhile(() => done == false);
    }


    public IEnumerator FetchLeaderboardPosition()
    {

        bool done = false;
        LootLockerSDKManager.GetMemberRank(leaderboardID.ToString(), PlayerPrefs.GetString("PlayerID"), (response) =>
        {
            if (response.statusCode == 200)
            {
                int rank = response.rank;
                Debug.Log("current leaderboard position: "+rank);
                playerPosition.text = rank.ToString();

            }
            else
            {
                Debug.Log("failed: ");
            }
        });

        yield return new WaitWhile(() => done == false);
    }
}