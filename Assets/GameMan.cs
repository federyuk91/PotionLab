using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameMan : MonoBehaviour
{

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
    [HideInInspector]
    public Animation lightAnimation;

    public static int currentLevel = 0;

    [Header("End game panels")]
    public GameObject deathPanel;
    public GameObject levelCompletePanel;

    public Text nightCount, score;
    public Animator scoreViewer;

    private int potionDrunked = 0, potionToDrunk = 0;

    //Stats
    public static int dieCounter = 0;

    private void Awake()
    {
        currentLevel = SceneManager.GetActiveScene().buildIndex;
        nightCount.text = "Night: " + currentLevel;
        CompileLevelReferences();
        if (Instance == null)
        {
            Instance = this;
        }

    }

    public void CompileLevelReferences()
    {
        potionDrunked = 0;
        potionToDrunk = levelPotions.Count;
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

        lightAnimation = GameObject.Find("Point Light 2D").GetComponent<Animation>();

    }


    public void StartGame()
    {
        Time.timeScale = 1;
        StartCoroutine("StartingDialog");

    }


    public void LoadPotion()
    {

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
        text_dialog.SetActive(false);
        text_magic.SetActive(true);
        continueButton.SetActive(false);
        lightAnimation.Play();
        //Invoke("LoadPotion", 6f);
        yield return new WaitForSeconds(6f);

        LoadPotion();
        cc.hpBar.gameObject.SetActive(true);

        yield return new WaitForSeconds(3.2f);

        CloseDialog();

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
            return;
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

    public void RemovePotion(PotionScript potion, bool drunked = true)
    {
        if (drunked)
            potionDrunked++;

        levelPotions.Remove(potion);
        if (levelPotions.Count <= 0)
        {
            OnLevelComplete();
        }
    }


    public void OnLevelComplete()
    {
        if (deathPanel.activeSelf)
            return;

        if (cc.currentStatus.Contains(CharacterController.Status.tree))
        {
            OnCharacterDie();
            return;
        }

        cc.mageAnimator.SetBool("goodNight", true);

        string points = "Drunked: " + potionDrunked + "/" + potionToDrunk + "\n\n"
                       + "Healt: " + cc.currentHP + "/" + cc.staringHP + "\n\n"
                       + "Malus: " + cc.currentStatus.Count;
        score.text = points;

        float potionPoints = (float)potionDrunked / (float)potionToDrunk;
        float hpPoints = (float)cc.currentHP / (float)cc.staringHP;
        float malus = (float)cc.currentStatus.Count / 5f;
        float result = potionPoints * 3f + hpPoints * 3f - malus * 3f;

        int viewerLevel = Mathf.FloorToInt(result);
        levelCompletePanel.SetActive(true);

        scoreViewer.SetInteger("Points", viewerLevel);

    }

    public void OnCharacterDie()
    {
        if (levelCompletePanel.activeSelf)
            return;

        dieCounter++;

        cc.currentHP = 0;
        deathPanel.SetActive(true);
        //Rimuovere le pozioni rimaste per evitare ulteriori interazioni con il livello
        while (levelPotions.Count > 0)
        {
            Destroy(levelPotions[0].gameObject);
            levelPotions.RemoveAt(0);
        }
    }

    public void NextLevel()
    {
        scoreViewer.SetTrigger("Reset");
        currentLevel++;
        if (currentLevel >= SceneManager.sceneCountInBuildSettings)
            currentLevel = 0;
        SceneManager.LoadScene(currentLevel);

    }

    public void TryAgain()
    {
        scoreViewer.SetTrigger("Reset");
        SceneManager.LoadScene(currentLevel);
    }
}



