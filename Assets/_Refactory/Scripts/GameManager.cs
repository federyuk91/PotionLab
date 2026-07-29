using System;
using CharacterSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private static readonly int DieParameter = Animator.StringToHash("Die");

    public event Action LevelStarted;
    public event Action LevelInteractionStarted;
    public event Action LevelCompleted;
    public event Action<bool> SpellBarVisibilityChanged;
    public event Action<string> CharacterDied;

    public static GameManager Instance { get; private set; }
    public static int currentLevel = 0;

    public BaseCharacter Character => TransformationManager.Instance != null ? TransformationManager.Instance.Current : null;
    public bool IsPuzzleMode => isPuzzleMode;

    [Header("Compiled from code")]
    public List<PotionScript> levelPotions;
    public List<DroppableObject> droppables;

    [Header("Reference necessarie")]
    public LightController lightController;
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private bool isPuzzleMode = true;

    public int potionDrunked = 0, spawnedPotion = 0;
    public int dieCounter = 0, mutationCounter = 0;
    private bool deathHandled;
    private bool levelStarted;

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
        SubscribeToCharacterDeath();
    }

    private void OnDestroy()
    {
        UnsubscribeFromCharacterDeath();
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
        if (levelStarted)
        {
            return;
        }

        levelStarted = true;

        Time.timeScale = 1;
        LevelStarted?.Invoke();
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

        dialogManager.CloseDialog();
        dialogManager.SetContinueButtonActive(false);

        lightController.StartLight();

        //Parametri di animazione legacy? 
        //TransformationManager.Instance.Current.animator.SetTrigger("cast");
        //TransformationManager.Instance.Current.animator.SetInteger("castInt", 1);

        yield return new WaitForSeconds(6f);

        LoadPotion();
        yield return new WaitForSeconds(.3f);

        SetSpellBarVisible(true);
        LevelInteractionStarted?.Invoke();

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
        LevelCompleted?.Invoke();
        DataSaver.instance.UpdateStats(dieCounter, potionDrunked, mutationCounter);
        SetSpellBarVisible(false);

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
        if (deathHandled)
        {
            return;
        }

        deathHandled = true;
        dieCounter++;
        Debug.Log("Char DIE");

        BaseCharacter character = Character;
        if (character != null && character.stats != null && character.stats.HP > 0)
        {
            character.stats.SetHP(0);
        }

        TriggerDeathAnimation();
        CloseActiveDialog();

        SetSpellBarVisible(false);

        if (DataSaver.instance != null)
        {
            DataSaver.instance.UpdateStats(1, potionDrunked, mutationCounter);
        }

        CharacterDied?.Invoke(deathDialog);

        // Remaining potions must stop interacting after death.
        if (levelPotions == null)
        {
            return;
        }

        foreach (PotionScript potion in levelPotions)
        {
            if (potion != null)
            {
                potion.gameObject.SetActive(false);
            }
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

    private void SubscribeToCharacterDeath()
    {
        if (Character == null || Character.stats == null)
        {
            Debug.LogError("GameManager cannot subscribe to character death: CharacterStats reference is missing.", this);
            return;
        }

        Character.stats.OnDeath += HandleCharacterDeath;
    }

    private void UnsubscribeFromCharacterDeath()
    {
        if (TransformationManager.Instance == null || Character == null || Character.stats == null)
        {
            return;
        }

        Character.stats.OnDeath -= HandleCharacterDeath;
    }

    private void HandleCharacterDeath()
    {
        OnCharacterDie(GetDeathDialog(Character.GetCharacterForm()));
    }

    private string GetDeathDialog(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Tree:
                return "...you will remain ash throught time...";
            case CharacterType.Balrog:
                return "...back from where i belong...";
            case CharacterType.PupperFish:
                return "...the sea takes back what is its own...";
            case CharacterType.Yeti:
                return "...mountains call me back...";
            default:
                return "...the last goodnight potion is unforgettable...";
        }
    }

    private void TriggerDeathAnimation()
    {
        if (Character == null || Character.animator == null)
        {
            return;
        }

        Character.animator.SetBool(DieParameter, true);
    }

    private void CloseActiveDialog()
    {
        if (dialogManager != null)
        {
            dialogManager.CloseDialog();
        }
    }

    private void SetSpellBarVisible(bool visible)
    {
        SpellBarVisibilityChanged?.Invoke(visible);
    }
}
