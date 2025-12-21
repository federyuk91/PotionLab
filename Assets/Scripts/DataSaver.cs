using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System;

public class DataSaver : MonoBehaviour
{
    public static int LAST_PUZZLE_LEVEL = 25;
    public static int CLASSIC_LEVEL = 25;
    public int totalDeath = 0, totalDrunkedPotion = 0, totalTransformation = 0;
    private static int levelsCount = 25;
    public int bestScore = 0, lastScore = 0;
    public static DataSaver instance;
    public List<int> scores = new List<int>();
    public int maxLevelReached = 1;
    public LootLockerLogin lootLockerRef;
    public SaveData binaryFile;


    private void Awake()
    {
        Debug.Log("Level is ");
        scores = new List<int>();
        for (int i = 0; i < levelsCount; i++)
        {
            scores.Add(0);
        }
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);


    }
    public void InitializeData()
    {
        ///Scarica il file da lootlooker
        lootLockerRef.GetPlayerFileData();
    }


    //Da chiamare all'avvio per caricare i punteggi, al primo avvio inizializza il PlayerPrefs con 0 punti su tutti i livelli.
    public void InitializeScores()
    {
        if (PlayerPrefs.HasKey("TotalDeath"))
        {
            totalDeath = PlayerPrefs.GetInt("TotalDeath");
        }
        else
        {
            totalDeath = binaryFile.totalDeath;
            PlayerPrefs.SetInt("TotalDeath", totalDeath);
        }
        if (PlayerPrefs.HasKey("TotalDrunkedPotion"))
        {
            totalDrunkedPotion = PlayerPrefs.GetInt("TotalDrunkedPotion");
        }
        else
        {
            totalDrunkedPotion = binaryFile.totalDrunkedPotion;
            PlayerPrefs.SetInt("TotalDrunkedPotion", totalDrunkedPotion);
            //Debug.LogWarning("TODO");
        }
        if (PlayerPrefs.HasKey("TotalTransformation"))
        {
            totalTransformation = PlayerPrefs.GetInt("TotalTransformation");
        }
        else
        {
            totalTransformation = binaryFile.totalTransformation;
            PlayerPrefs.SetInt("TotalTransformation", totalTransformation);
            //Debug.LogWarning("TODO");
        }
        if (PlayerPrefs.HasKey("BestScore"))
        {
            bestScore = PlayerPrefs.GetInt("BestScore");
        }
        else
        {
            bestScore = binaryFile.bestScore;
            PlayerPrefs.SetInt("BestScore", bestScore);
            //Debug.LogWarning("TODO");
        }
        if (PlayerPrefs.HasKey("LastScore"))
        {
            lastScore = PlayerPrefs.GetInt("LastScore");
        }
        else
        {
            lastScore = binaryFile.lastScore;
            PlayerPrefs.SetInt("LastScore", lastScore);
            //Debug.LogWarning("TODO");
        }
        scores.Clear();
        scores = new List<int>();
        for (int i = 0; i < levelsCount; i++)
        {
            int score = 0;
            if (PlayerPrefs.HasKey("Level_" + i))
            {
                score = PlayerPrefs.GetInt("Level_" + i);
            }
            else
            {
                Debug.Log(binaryFile.scores.Length);
                //PlayerPrefs.SetInt("Level_" + i, score);
                score = binaryFile.scores[i];
                PlayerPrefs.SetInt("Level_" + i, score);
            }
            scores.Add(score);
        }
        //Carico l'ultimo livello raggiunto se esiste
        if (PlayerPrefs.HasKey("MaxLevelReached"))
        {
            maxLevelReached = PlayerPrefs.GetInt("MaxLevelReached");
            Debug.Log("Max level reached is " + maxLevelReached);
        }
        else
        {
            maxLevelReached = binaryFile.maxLevelReached;
            PlayerPrefs.SetInt("MaxLevelReached", binaryFile.maxLevelReached);
        }


        //Aggiorno il binary file e lo carico su lootlocker con i dati del device che dovrebbero essere i più recenti
        lootLockerRef.UpdatingFile(SaveToByteArray());
    }
    public void SaveBestScore(int score)
    {
        lastScore = score;
        PlayerPrefs.SetInt("LastScore", score);
        if (PlayerPrefs.HasKey("BestScore"))
        {
            if (PlayerPrefs.GetInt("BestScore") < score)
            {
                bestScore = score;
                PlayerPrefs.SetInt("BestScore", bestScore);
                Debug.LogWarning("INSERIRE QUI L'UPLOAD DELLO SCORE ONLINE");
                lootLockerRef.leaderBoard.ConfigureLeaderBoard(bestScore);
            }
            else
            {
                //NON SALVO IL PUNTEGGIO è MINORE DEL RECORD
            }
        }
        else
        {
            //PRIMO RECORD
            PlayerPrefs.SetInt("BestScore", score);
        }

        //Aggiorno il binary file e lo carico su lootlocker
        lootLockerRef.UpdatingFile(SaveToByteArray());
    }

    public void SaveMaxReached(int level)
    {
        if (level+1 > maxLevelReached && level != LAST_PUZZLE_LEVEL)
        {
            maxLevelReached = level+1;
            PlayerPrefs.SetInt("MaxLevelReached", maxLevelReached);
            Debug.Log("Livello completato " + level + " max reach " + maxLevelReached);
        }
        if (level == CLASSIC_LEVEL)
        {
            AchievementManager.instance.Achive("The classic");
        }
    }
    public void SaveScore(int level, int score)
    {
        if (PlayerPrefs.HasKey("Level_" + level))
        {
            if (PlayerPrefs.GetInt("Level_" + level) < score)
                PlayerPrefs.SetInt("Level_" + level, score);
            else
            {
                Debug.Log("Salvo solo punteggi maggiori del vecchio score");
            }
        }
        else
        {
            Debug.Log("Salvo e inizializzo questo score che mancava nei PlayerPrefs");
            PlayerPrefs.SetInt("Level_" + level, score);
        }
        
        if (score == 4 && maxLevelReached >= CLASSIC_LEVEL)
        {
            bool perfect = true;
            for (int i = 0; i < CLASSIC_LEVEL; i++)
            {
                if (PlayerPrefs.GetInt("Level_" + i) < 4)
                {
                    perfect = false;
                    break;
                }
            }

            if (perfect)
            {
                //Questo Achievement viene chiamato solo facendo il perfetto nei livelli classici, primi 25!
                AchievementManager.instance.Achive("Perfectionist");
            }
        }

        //Aggiorno il binary file e lo carico su lootlocker
        lootLockerRef.UpdatingFile(SaveToByteArray());
    }

    public void UpdateStats(int death, int potion, int transformation)
    {
        totalDeath += death;
        totalDrunkedPotion += potion;
        totalTransformation += transformation;
        PlayerPrefs.SetInt("TotalDeath", totalDeath);
        PlayerPrefs.SetInt("TotalDrunkedPotion", totalDrunkedPotion);
        PlayerPrefs.SetInt("TotalTransformation", totalTransformation);


        //Aggiorno il binary file e lo carico su lootlocker
        lootLockerRef.UpdatingFile(SaveToByteArray());
    }


    public void ResetDataSave()
    {
        string version="0";
        maxLevelReached = 1;
        string id = "0", saveFileId = "0";
        if (PlayerPrefs.HasKey("PlayerID"))
            id = PlayerPrefs.GetString("PlayerID");
        if (PlayerPrefs.HasKey("PlayerSaveDataFileID"))
            saveFileId = PlayerPrefs.GetString("PlayerSaveDataFileID");
        if (PlayerPrefs.HasKey("Version"))
            version = PlayerPrefs.GetString("Version");
        PlayerPrefs.DeleteAll();

        if (id != "0")
            PlayerPrefs.SetString("PlayerID", id);
        if (saveFileId != "0")
            PlayerPrefs.SetString("PlayerSaveDataFileID", saveFileId);
        if (version != "0")
            PlayerPrefs.SetString("Version", version);

        //Aggiorno il binary file e lo carico su lootlocker
        lootLockerRef.UpdatingFile(SaveToByteArray());
        AchievementManager.instance.ResetAchievement();

        SceneManager.LoadScene(0);
    }



    ///BINARY FILES
    ///

    public byte[] SaveToByteArray()
    {
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream();
        if (scores.Count < 1)
        {
            Debug.Log("sto salvando un array di lunghezza 0");
            Debug.Break();
        }
        SaveData saveData = new SaveData(totalDeath, totalDrunkedPotion, totalTransformation, bestScore, lastScore, maxLevelReached, scores);
        binaryFile = saveData;

        formatter.Serialize(stream, saveData);
        byte[] byteArray = stream.ToArray();
        Debug.Log("Variabili salvate ");
        return byteArray;
    }

    public void ReadFromByteArray(byte[] byteArray)
    {
        BinaryFormatter formatter = new BinaryFormatter();
        MemoryStream stream = new MemoryStream(byteArray);

        SaveData saveData = formatter.Deserialize(stream) as SaveData;

        if (saveData != null)
        {
            totalDeath = saveData.totalDeath;
            totalDrunkedPotion = saveData.totalDrunkedPotion;
            totalTransformation = saveData.totalTransformation;
            bestScore = saveData.bestScore;
            lastScore = saveData.lastScore;
            maxLevelReached = saveData.maxLevelReached;
            for (int i = 0; i < saveData.scores.Length; i++)
            {
                scores.Add(saveData.scores[i]);
            }

            Debug.Log("Variabili lette");
            binaryFile = saveData;
            Debug.Log("Max level reached by binary file downloaded is "+binaryFile.maxLevelReached);
        }
        else
        {
            Debug.LogError("Errore ");
        }
    }
}

[Serializable]
public class SaveData
{
    public int totalDeath;
    public int totalDrunkedPotion;
    public int totalTransformation;
    public int bestScore;
    public int lastScore;
    public int maxLevelReached;
    public int[] scores;


    public SaveData(int death, int potion, int transformation, int best, int last, int maxLevel, List<int> score)
    {
        totalDeath = death;
        totalDrunkedPotion = potion;
        totalTransformation = transformation;
        bestScore = best;
        lastScore = last;
        maxLevelReached = maxLevel;
        this.scores = new int[25];
        for (int i = 0; i < 25; i++)
        {
            this.scores[i] = score[i];
        }

    }
}

