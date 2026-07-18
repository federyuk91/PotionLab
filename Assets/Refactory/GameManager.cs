using CharacterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public static int currentLevel = 0;

    public BaseCharacter Character => TransformationManager.Instance.Current;
    public bool IsPuzzleMode => isPuzzleMode;

    [Header("Compiled from code")]
    public List<PotionScript> levelPotions;
    public List<DroppableObject> droppables;

    [Header("Reference necessarie")]
    public GameObject spellBar;
    public LightController lightController;
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private bool isPuzzleMode = true;

    public int potionDrunked = 0, spawnedPotion = 0;
    public int dieCounter = 0, mutationCounter = 0;

    private void Awake()
    {
        dieCounter = 0;
        potionDrunked = 0;
        mutationCounter = 0;
        currentLevel = SceneManager.GetActiveScene().buildIndex;

        if (isPuzzleMode)
        {
            CompileLevelReferences();
        }

        if (Instance == null)
        {
            Instance = this;
        }
    }

    private void Start()
    {
        Time.timeScale = 1;
    }

    // Puzzle mode uses the potions and droppables already placed in the level.
    private void CompileLevelReferences()
    {
        PotionScript[] potions = FindObjectsByType<PotionScript>(FindObjectsSortMode.None);
        levelPotions = new List<PotionScript>();
        foreach (PotionScript potion in potions)
        {
            levelPotions.Add(potion);
        }

        DroppableObject[] drops = FindObjectsByType<DroppableObject>(FindObjectsSortMode.None);
        droppables = new List<DroppableObject>();
        foreach (DroppableObject droppable in drops)
        {
            droppables.Add(droppable);
        }
    }

    public void StartGame()
    {
        StartLevel();
    }

    public void StartLevel()
    {
        Time.timeScale = 1;
        StartCoroutine(nameof(StartingLevel));
    }

    private void LoadPotion()
    {
        potionDrunked = 0;

        foreach (PotionScript potion in levelPotions)
        {
            potion.ActivateBox();
        }

        foreach (DroppableObject droppable in droppables)
        {
            droppable.ActivateBox();
        }
    }

    public void RegisterSpawnedPotion(PotionScript potion)
    {
        if (potion == null)
        {
            return;
        }

        if (levelPotions == null)
        {
            levelPotions = new List<PotionScript>();
        }

        spawnedPotion++;
        levelPotions.Add(potion);
    }

    public bool IsCharacterAlive()
    {
        return Character != null && Character.stats != null && Character.stats.HP > 0;
    }

    // Initial light and character animation before level interaction starts.
    private IEnumerator StartingLevel()
    {
        if (dialogManager == null)
        {
            Debug.LogError("GameManager cannot start level: DialogManager reference is missing.", this);
            yield break;
        }

        if (lightController == null && TransformationManager.Instance != null)
        {
            lightController = TransformationManager.Instance.lightController;
        }

        if (lightController == null)
        {
            Debug.LogError("GameManager cannot start level: LightController reference is missing.", this);
            yield break;
        }

        if (spellBar == null)
        {
            Debug.LogError("GameManager cannot start level: spellBar reference is missing.", this);
            yield break;
        }

        dialogManager.CloseDialog();
        dialogManager.SetContinueButtonActive(false);

        lightController.StartLight();

        //Parametri di animazione legacy? 
        //TransformationManager.Instance.Current.animator.SetTrigger("cast");
        //TransformationManager.Instance.Current.animator.SetInteger("castInt", 1);

        yield return new WaitForSeconds(6f);

        LoadPotion();
        yield return new WaitForSeconds(.3f);

        spellBar.SetActive(true);

        yield return new WaitForSeconds(3.2f);

        dialogManager.CloseDialog();
    }

    public void RemovePotion(PotionScript potion, bool drunked = true)
    {
        if (drunked)
        {
            potionDrunked++;
        }

        levelPotions.Remove(potion);

        // Other game modes should end only through their own failure conditions.
        if (isPuzzleMode && levelPotions.Count <= 0)
        {
            OnLevelComplete();
        }
    }

    public void OnLevelComplete()
    {
        DataSaver.instance.UpdateStats(dieCounter, potionDrunked, mutationCounter);
        spellBar.SetActive(false);

        CharacterType characterForm = Character.GetCharacterForm();

        if (characterForm.Equals(CharacterType.Tree))
        {
            if (Character.status.Has(Status.Burned))
            {
                Character.animator.SetTrigger("treeBurned");
                AchievementManager.instance.Achive("Old Toby");
            }

            OnCharacterDie("trees never sleeps");
            return;
        }

        if (characterForm.Equals(CharacterType.Balrog))
        {
            OnCharacterDie("evil doens't sleep");
            return;
        }

        if (characterForm.Equals(CharacterType.PupperFish))
        {
            OnCharacterDie("you drowned in your nightmare");
            return;
        }

        if (characterForm.Equals(CharacterType.Yeti))
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

        dialogManager.PopDialog(deathDialog, 3f);

        // Remaining potions must stop interacting after death.
        foreach (PotionScript potion in levelPotions)
        {
            potion.gameObject.SetActive(false);
        }
    }

    public void NextLevel()
    {
        currentLevel++;
        if (currentLevel >= SceneManager.sceneCountInBuildSettings)
        {
            currentLevel = 0;
        }

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
