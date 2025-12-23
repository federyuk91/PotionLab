using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameMan : MonoBehaviour
{
    //public static int maxLevelReached = 1;
    /// FASE 1: Start Dialog - Load Potion 
    /// FASE 2: Drop Potion

    public static GameMan Instance { get; private set; }
    public string finalSentence;

    public GameObject text_dialog, text_magic, continueButton;// healthbar;

    public CharacterController cc;


    //public PotionScript[] thisLevelPotion;
    [Header("Compiled from code")]
    public List<PotionScript> levelPotions;
    public List<DroppableObject> droppables;
    public SpawnerManager spawnerManager;
    
    public Animator lightAnimation;
    public int lightIntensity = 1;

    public static int currentLevel = 0;


    [Header("End game panels")]
    public GameObject deathPanel;
    public GameObject levelCompletePanel;
    public GameObject levelAdvancePanel;
    public Image[] finalScoreUI;
    public Text nightCount, score, nightSubtitles, finalMessage, potionScore;
    public Text bestScore, finalScore, playername;
    //public Animator scoreViewer;

    public int potionDrunked = 0, potionToDrunk = 0, spawnedPotion = 0;

    //Stats
    public int dieCounter = 0, mutationCounter = 0;
    public int bestHealthScore = 10;

    public Text popUpHealth;


    [Header("Procedural")]
    public bool isProceduralMode = false;
    public DataBetweenLevel endlessModifier;
    public bool hyperMode = false;
    public bool hyperHyperMode = false;
    public bool failureMode = false;

    private void Awake()
    {
        dieCounter = 0;
        potionDrunked = 0;
        mutationCounter = 0;
        currentLevel = SceneManager.GetActiveScene().buildIndex;
        //Mooved to Data Saver
        /*if (PlayerPrefs.HasKey("MaxLevelReached"))
        {
            maxLevelReached = PlayerPrefs.GetInt("MaxLevelReached");
            Debug.Log("Max level reached is " + maxLevelReached);
        }*/

        /* 
         if (currentLevel > maxLevelReached)
         {
             maxLevelReached = currentLevel;
             PlayerPrefs.SetInt("MaxLevelReached", maxLevelReached);
         }*/

        GetLightRef();
        CompileUILevel();
        
        CompileLevelReferences();
        if (Instance == null)
        {
            Instance = this;
        }

    }

    private void Start()
    {
        
        if (isProceduralMode)
        {
            spawnerManager = FindObjectOfType<SpawnerManager>();
        }
        Time.timeScale = 1;

    }



    public void CompileLevelReferences()
    {
        PotionScript[] potions = FindObjectsOfType<PotionScript>();
        levelPotions = new List<PotionScript>();
        foreach (PotionScript p in potions)
        {
            levelPotions.Add(p);
        }

        DroppableObject[] drops = FindObjectsOfType<DroppableObject>();
        droppables = new List<DroppableObject>();
        foreach (DroppableObject d in drops)
        {
            droppables.Add(d);
        }


       
    }

    public void GetLightRef()
    {
        lightAnimation = GameObject.FindGameObjectWithTag("Light").GetComponent<Animator>();
        

    }

    public void SetUpLightLevel()
    {
        if (lightIntensity < 3)
        {
            lightIntensity++;
            CompileUILevel();
            

        }
        if (isProceduralMode)
        {
            spawnerManager.timer = 0;
        }



        lightAnimation.SetTrigger("lightOn");
        lightAnimation.SetInteger("lightIntesity", lightIntensity);

        lightAnimation.gameObject.GetComponent<LightScript>().PlayAudio();

    }


    public void CompileUILevel()
    {
        if (!isProceduralMode)
        {
            nightCount.text = "Night: " + currentLevel;
        }
        else
        {
            CompileEndlessModifier();
        }

        switch (lightIntensity)
        {
            case 0:
                nightSubtitles.text = "No Magic Power";
                nightSubtitles.color = Color.red;
                break;
            case 1:
                nightSubtitles.text = "Low Magic Power";
                nightSubtitles.color = Color.green;
                break;
            case 2:
                nightSubtitles.text = "Medium Magic Power";
                nightSubtitles.color = Color.blue;
                break;
            case 3:
                nightSubtitles.text = "High Magic Power";
                nightSubtitles.color = Color.yellow;
                break;
        }
    }

    public void CompileEndlessModifier()
    {
        endlessModifier = GameObject.FindGameObjectWithTag("music").GetComponent<DataBetweenLevel>();
        if (endlessModifier != null)
        {
            hyperMode = endlessModifier.hyperMode;
            hyperHyperMode = endlessModifier.hyperHyperMode;
            failureMode = endlessModifier.failureMode;
        }

    }

    public void StartGame()
    {
        Time.timeScale = 1;
        GetLightRef();
        StartCoroutine("StartingDialog");

    }


    public void LoadPotion()
    {
        potionDrunked = 0;
        potionToDrunk = levelPotions.Count;

        PopDialog(finalSentence);

        foreach (PotionScript ps in levelPotions)
        {
            ps.ActivateBox();
        }
        foreach (DroppableObject d in droppables)
        {
            d.ActivateBox();
        }
    }

    IEnumerator StartingDialog()
    {
        //nasconde il testo iniziale
        text_dialog.SetActive(false);
        continueButton.SetActive(false);
        //lancia l'animazione della magia di luce determinando il livello di intensità stabilito per questo livello
        lightAnimation.SetTrigger("lightOn");
        lightAnimation.SetInteger("lightIntesity", lightIntensity);

        lightAnimation.gameObject.GetComponent<LightScript>().PlayAudio();
        
        //Lancia l'animazione del personaggio per castare la magia 1
        cc.mageAnimator.SetTrigger("cast");
        cc.mageAnimator.SetInteger("castInt", 1);

        //Invoke("LoadPotion", 6f);
        //Attende 6 secondi (durata animazione ?)
        yield return new WaitForSeconds(6f);

        //Attiva la visualizzazione per gli oggetti droppabili
        LoadPotion();
        yield return new WaitForSeconds(.3f);
        //Attiva hp, magie e mp 
        cc.hpBar.gameObject.SetActive(true);
        cc.spellBar.SetActive(true);
        cc.magicBar.gameObject.SetActive(true);

        yield return new WaitForSeconds(3.2f);

        CloseDialog();

    }


    public void ChangeLightColor(Color c)
    {
        lightAnimation.gameObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>().color = c;
    }


    public void PickADialog(List<string> dialogs, float duration = -1f)
    {
        int d = Random.Range(0, dialogs.Count);
        PopDialog(dialogs[d], duration);

    }

    public void PopDialog(string dialog, float duration = -1f)
    {
        //Evita di sovrapporre dialoghi e di parlare da morto
        if (text_dialog.activeSelf || cc.currentHP <= 0)
            text_dialog.SetActive(false);

        //Debug.Log("[GameMan] PopDialog: " + dialog);
        //return;
        text_dialog.SetActive(true);
        text_dialog.GetComponentInChildren<Text>().text = dialog;
        if (duration > 0)
        {
            Invoke("CloseDialog", duration);
        }
    }

    public void CloseDialog()
    {
        text_dialog.SetActive(false);
    }

    public void PopUpUp(string s, Color color)
    {
        popUpHealth.text = s;
        popUpHealth.color = color;

        if (popUpHealth.gameObject.activeSelf)
        {
            popUpHealth.gameObject.SetActive(false);
            popUpHealth.gameObject.SetActive(true);
        }
        else
        {
            popUpHealth.gameObject.SetActive(true);
        }
    }

    public void RemovePotion(PotionScript potion, bool drunked = true)
    {
        if (drunked)
        {
            potionDrunked++;
            if (isProceduralMode)
            {
                if (hyperMode) { potionDrunked      += 1; }
                if (hyperHyperMode) { potionDrunked += 2; }
                if (failureMode) { potionDrunked    += 1; }


                switch (potionDrunked)
                {
                    case 25:
                        AchievementManager.instance.Achive("Casual Drinker");
                        break;
                    case 75:
                        AchievementManager.instance.Achive("Regular Drinker");
                        break;
                    case 150:
                        AchievementManager.instance.Achive("Hardcore Drinker");
                        break;
                    case 300:
                        AchievementManager.instance.Achive("Master Drinker");
                        break;
                    case 500:
                        AchievementManager.instance.Achive("Almost a problem...Drinker");
                        break;
                    case 700:
                        AchievementManager.instance.Achive("God of Libations!");
                        break;
                }
            }
        }
        potionScore.text = potionDrunked.ToString();
        levelPotions.Remove(potion);

        if (levelPotions.Count <= 0 && !isProceduralMode)
        {
            OnLevelComplete();
        }
    }


    public void OnLevelComplete()
    {
        if (deathPanel.activeSelf)
            return;
        DataSaver.instance.UpdateStats(dieCounter, potionDrunked, mutationCounter);
        cc.spellBar.SetActive(false);
        if (isProceduralMode)
        {
            DataSaver.instance.SaveBestScore(potionDrunked);

            AdvanceScore();
            return;
        }
        if (cc.currentStatus.Contains(CharacterController.Status.tree))
        {
            if (cc.currentStatus.Contains(CharacterController.Status.burned))
            {
                //cc.PlayClip(audio);
                cc.mageAnimator.SetTrigger("treeBurned");
                AchievementManager.instance.Achive("Old Toby");
            }
            OnCharacterDie("trees never sleeps");
            return;
        }
        else if (cc.currentStatus.Contains(CharacterController.Status.balrog))
        {
            OnCharacterDie("evil doens't sleep");
            return;
        }
        else if (cc.currentStatus.Contains(CharacterController.Status.pupperfish))
        {
            OnCharacterDie("you drowned in your nightmare");
            return;
        }
        else if (cc.currentStatus.Contains(CharacterController.Status.yeti))
        {
            OnCharacterDie("freezing to death");
            return;
        }

        cc.mageAnimator.SetBool("goodNight", true);

        CalculateScore();

    }

    public void AdvanceScore()
    {

        finalScore.text = potionDrunked.ToString();
        bestScore.text = PlayerPrefs.GetInt("BestScore").ToString();
        playername.text = PlayerPrefs.GetString("PlayerID");
        levelAdvancePanel.SetActive(true);
        cc.spellBar.SetActive(false);
        //scoreViewer.SetInteger("Points", viewerLevel);
    }
    public void CalculateScore()
    {
        int totalPotion = isProceduralMode ? spawnedPotion : potionToDrunk;
        string points = "Drunked: " + potionDrunked + "/" + totalPotion + "\n\n"
                       + "Health: " + cc.currentHP + "/" + bestHealthScore + "\n\n"
                       + "Malus: " + cc.currentStatus.Count;

        score.text = points;

        int pointsNew = 1;
        int temp = 0;
        if (potionDrunked >= potionToDrunk)
        {
            pointsNew++;
            finalScoreUI[temp].color = Color.white;
            temp++;

        }
        if (cc.currentHP >= bestHealthScore)
        {
            pointsNew++;
            finalScoreUI[temp].color = Color.white;
            temp++;
        }

        if (cc.currentStatus.Count == 0)
        {
            pointsNew++;
            finalScoreUI[temp].color = Color.white;
        }



        switch (pointsNew)
        {
            case 1:
                finalMessage.text = "Very bad night...";
                break;
            case 2:
                finalMessage.text = "It's ok...";
                break;
            case 3:
                finalMessage.text = "Nice! Not bad!";
                break;
            case 4:
                finalMessage.text = "PERFECT NIGHT!";
                break;
        }

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        Debug.Log(sceneIndex);
        if (sceneIndex != 26)
        {
            DataSaver.instance.SaveMaxReached(sceneIndex);
            DataSaver.instance.SaveScore(sceneIndex - 1, pointsNew);
        }

        /*float potionPoints = (float)potionDrunked / (float)potionToDrunk;
        float hpPoints = (float)cc.currentHP / (float)cc.staringHP;
        float malus = (float)cc.currentStatus.Count / 5f;
        float result = potionPoints * 3f + hpPoints * 3f - malus * 3f;

        int viewerLevel = Mathf.FloorToInt(result);*/
        levelCompletePanel.SetActive(true);
        cc.spellBar.SetActive(false);
        //scoreViewer.SetInteger("Points", viewerLevel);
    }


    public void OnCharacterDie(string deathDialog)
    {
        if (levelCompletePanel.activeSelf)
            return;

        dieCounter++;
        Debug.Log("Char DIE");
        cc.currentHP = 0;
        if (isProceduralMode)
        {
            FindObjectOfType<SpawnerManager>().StopAllCoroutines();
            FindObjectOfType<Spawner>().StopAllCoroutines();
            OnLevelComplete();
        }
        else
        {
            deathPanel.SetActive(true);
            deathPanel.GetComponentInChildren<Text>().text = deathDialog;
            DataSaver.instance.UpdateStats(1, potionDrunked, mutationCounter);
            //StopAllCoroutines();
        }
        //Rimuovere le pozioni rimaste per evitare ulteriori interazioni con il livello

        foreach (PotionScript s in levelPotions)
        {
            s.gameObject.SetActive(false);
        }

    }

    public void NextLevel()
    {
        currentLevel++;
        if (currentLevel >= SceneManager.sceneCountInBuildSettings)
            currentLevel = 0;
        SceneManager.LoadScene(currentLevel);

    }
    public void MainMenu()
    {
        SceneManager.LoadScene(0);

    }

    public void TryAgain()
    {
        SceneManager.LoadScene(currentLevel);
    }
}



