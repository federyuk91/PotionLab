using CharacterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int currentLevel = 0;

    public BaseCharacter Character => TransformationManager.Instance.Current;


    [Header("Compiled from code")]
    public List<PotionScript> levelPotions;
    public List<DroppableObject> droppables;
    public SpawnerManager spawnerManager;

    [Header("Reference necessarie")]
    public GameObject spellBar;
    public LightController lightController;
    public GameObject text_dialog, text_magic, continueButton;
    public int potionDrunked = 0, spawnedPotion = 0;
    //Stats
    public int dieCounter = 0, mutationCounter = 0;

    private void Awake()
    {
        dieCounter = 0;
        potionDrunked = 0;
        mutationCounter = 0;
        currentLevel = SceneManager.GetActiveScene().buildIndex;

        CompileLevelReferences();
        if (Instance == null)
        {
            Instance = this;
        }

    }

    private void Start()
    {
        Time.timeScale = 1;
    }



    public void CompileLevelReferences()
    {
        PotionScript[] potions = FindObjectsByType<PotionScript>(FindObjectsSortMode.None);
        levelPotions = new List<PotionScript>();
        foreach (PotionScript p in potions)
        {
            levelPotions.Add(p);
        }

        DroppableObject[] drops = FindObjectsByType<DroppableObject>(FindObjectsSortMode.None);
        droppables = new List<DroppableObject>();
        foreach (DroppableObject d in drops)
        {
            droppables.Add(d);
        }
    }




    public void StartGame()
    {
        Time.timeScale = 1;
        StartCoroutine("StartingDialog");

    }


    public void LoadPotion()
    {
        potionDrunked = 0;

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

        lightController.StartLight();

        //Lancia l'animazione del personaggio per castare la magia 1
        TransformationManager.Instance.Current.animator.SetTrigger("cast");
        TransformationManager.Instance.Current.animator.SetInteger("castInt", 1);

        //Invoke("LoadPotion", 6f);
        //Attende 6 secondi (durata animazione ?)
        yield return new WaitForSeconds(6f);

        //Attiva la visualizzazione per gli oggetti droppabili
        LoadPotion();
        yield return new WaitForSeconds(.3f);
        
        spellBar.SetActive(true);


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
        if (text_dialog.activeSelf || Character.stats.HP <= 0)
            text_dialog.SetActive(false);

        Debug.Log("[GameMan] PopDialog: " + dialog);
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


    public void RemovePotion(PotionScript potion, bool drunked = true)
    {
        if (drunked)
        {
            potionDrunked++;
            
        }

        levelPotions.Remove(potion);

        if (levelPotions.Count <= 0)
        {
            OnLevelComplete();
        }
    }


    public void OnLevelComplete()
    {
        DataSaver.instance.UpdateStats(dieCounter, potionDrunked, mutationCounter);
        spellBar.SetActive(false);
       
        if (Character.GetCharacterForm().Equals(CharacterType.Tree))
        {
            if (Character.status.Has(Status.Burned))
            {
                //cc.PlayClip(audio);
                Character.animator.SetTrigger("treeBurned");
                AchievementManager.instance.Achive("Old Toby");
            }
            OnCharacterDie("trees never sleeps");
            return;
        }
        else if (Character.GetCharacterForm().Equals(CharacterType.Balrog))
        {
            OnCharacterDie("evil doens't sleep");
            return;
        }
        else if (Character.GetCharacterForm().Equals(CharacterType.PupperFish))
        {
            OnCharacterDie("you drowned in your nightmare");
            return;
        }
        else if (Character.GetCharacterForm().Equals(CharacterType.Yeti))
        {
            OnCharacterDie("freezing to death");
            return;
        }

        Character.animator.SetBool("goodNight", true);


    }

    public void OnCharacterDie(string deathDialog)
    {

        dieCounter++;
        Debug.Log("Char DIE");
        Character.stats.HP = 0;
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



