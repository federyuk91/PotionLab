using System;
using CharacterSystem;
using ProgressSystem;
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

    public BaseCharacter Character => transformationManager != null ? transformationManager.Current : null;
    public bool IsPuzzleMode => GetIsPuzzleMode();
    public int LevelPotionTarget => levelPotionTarget;
    public int BestHealthScore => GetBestHealthScore();
    public int BestProceduralScore => GetBestProceduralScore();

    [Header("Compiled from code")]
    public List<PotionScript> levelPotions;
    public List<DroppableObject> droppables;

    [Header("Reference necessarie")]
    [SerializeField] private LevelSettings levelSettings;
    [SerializeField] private TransformationManager transformationManager;
    public LightController lightController;
    [SerializeField] private DialogManager dialogManager;
    [SerializeField] private ProgressService progressService;

    public int potionDrunked = 0, spawnedPotion = 0;
    public int dieCounter = 0, mutationCounter = 0;
    private int levelPotionTarget;
    private bool deathHandled;
    private bool levelStarted;
    private bool missingLevelSettingsWarningShown;
    private bool missingTransformationManagerWarningShown;
    private bool missingProgressServiceWarningShown;

    private void Awake()
    {
        ResolveLevelSettings();
        ResolveProgressService();

        dieCounter = 0;
        potionDrunked = 0;
        mutationCounter = 0;
        currentLevel = SceneManager.GetActiveScene().buildIndex;

        if (IsPuzzleMode)
        {
            CompileLevelReferences();
        }

        if (Instance == null)
        {
            Instance = this;
        }

        if (transformationManager == null)
        {
            WarnMissingTransformationManager();
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

        levelPotionTarget = levelPotions.Count;
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
        levelPotionTarget = levelPotions != null ? levelPotions.Count : 0;

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

    public void ReplacePotion(PotionScript sourcePotion, PotionScript replacementPotion)
    {
        if (replacementPotion == null)
        {
            return;
        }

        if (levelPotions == null)
        {
            levelPotions = new List<PotionScript>();
        }

        if (!levelPotions.Contains(replacementPotion))
        {
            levelPotions.Add(replacementPotion);
        }

        if (sourcePotion != null)
        {
            levelPotions.Remove(sourcePotion);
        }
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

        if (lightController == null)
        {
            Debug.LogError("GameManager cannot start level: LightController reference is missing.", this);
            yield break;
        }

        dialogManager.CloseDialog();
        dialogManager.SetContinueButtonActive(false);

        lightController.StartLight();

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
        if (IsPuzzleMode && !deathHandled && IsCharacterAlive() && levelPotions.Count <= 0)
        {
            OnLevelComplete();
        }
    }

    public void OnLevelComplete()
    {
        if (deathHandled)
        {
            return;
        }

        UpdateStatsIfAvailable(dieCounter, potionDrunked, mutationCounter);
        SetSpellBarVisible(false);

        if (!IsPuzzleMode)
        {
            SaveProceduralScoreIfAvailable();
            LevelCompleted?.Invoke();
            return;
        }

        BaseCharacter character = Character;
        if (character == null)
        {
            Debug.LogError($"{name}: Cannot complete level because the active character is missing. Check TransformationManager setup.", this);
            return;
        }

        CharacterType characterForm = character.GetCharacterForm();

        if (characterForm.Equals(CharacterType.Tree))
        {
            if (character.status.Has(Status.Burned))
            {
                character.animator.SetTrigger("treeBurned");
                UnlockAchievementIfAvailable("Old Toby");
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

        character.animator.SetBool("goodNight", true);
        SaveClassicScoreIfAvailable(CalculateClassicScorePoints());
        LevelCompleted?.Invoke();
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

        if (!IsPuzzleMode)
        {
            SaveProceduralScoreIfAvailable();
        }

        UpdateStatsIfAvailable(1, potionDrunked, mutationCounter);

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
        if (Character == null || Character.stats == null)
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

    private void WarnMissingTransformationManager()
    {
        if (missingTransformationManagerWarningShown)
        {
            return;
        }

        missingTransformationManagerWarningShown = true;
        Debug.LogWarning($"{name}: TransformationManager reference is missing. Assign it in Inspector so GameManager can read the active character.", this);
    }

    private void UpdateStatsIfAvailable(int deaths, int drunkedPotions, int mutations)
    {
        if (progressService == null)
        {
            WarnMissingProgressService();
            return;
        }

        progressService.AddRunStats(deaths, drunkedPotions, mutations);
    }

    public int CalculateClassicScorePoints()
    {
        BaseCharacter character = Character;
        if (character == null || character.stats == null || character.status == null)
        {
            Debug.LogWarning($"{name}: Cannot calculate classic score because active character data is missing.", this);
            return 1;
        }

        int points = 1;

        if (potionDrunked >= levelPotionTarget)
        {
            points++;
        }

        if (character.stats.HP >= BestHealthScore)
        {
            points++;
        }

        if (character.status.Count() == 0)
        {
            points++;
        }

        return points;
    }

    private void SaveClassicScoreIfAvailable(int score)
    {
        if (progressService == null)
        {
            WarnMissingProgressService();
            return;
        }

        int sceneIndex = SceneManager.GetActiveScene().buildIndex;
        if (sceneIndex <= 0)
        {
            Debug.LogWarning($"{name}: Current scene build index is {sceneIndex}. Classic score will not be saved because it would produce an invalid level index.", this);
            return;
        }

        if (sceneIndex == 26)
        {
            return;
        }

        progressService.SaveClassicLevelResult(sceneIndex, score);
    }

    private void SaveProceduralScoreIfAvailable()
    {
        if (progressService == null)
        {
            WarnMissingProgressService();
            return;
        }

        progressService.SaveProceduralScore(potionDrunked);
    }

    private void UnlockAchievementIfAvailable(string achievementName)
    {
        if (AchievementManager.instance == null)
        {
            Debug.LogWarning($"{name}: AchievementManager instance is missing. Achievement '{achievementName}' will not be unlocked in this scene.", this);
            return;
        }

        AchievementManager.instance.Achive(achievementName);
    }

    private bool GetIsPuzzleMode()
    {
        if (levelSettings == null)
        {
            WarnMissingLevelSettings();
            return true;
        }

        return levelSettings.IsPuzzleMode;
    }

    private int GetBestHealthScore()
    {
        if (levelSettings == null)
        {
            WarnMissingLevelSettings();
            return 10;
        }

        return levelSettings.BestHealthScore;
    }

    private int GetBestProceduralScore()
    {
        if (progressService == null)
        {
            WarnMissingProgressService();
            return 0;
        }

        if (progressService.Progress == null)
        {
            return 0;
        }

        return progressService.Progress.bestProceduralScore;
    }

    private void WarnMissingLevelSettings()
    {
        if (missingLevelSettingsWarningShown)
        {
            return;
        }

        missingLevelSettingsWarningShown = true;
        Debug.LogWarning($"{name}: LevelSettings reference is missing. Assign it in Inspector to configure puzzle mode and score targets.", this);
    }

    private void ResolveLevelSettings()
    {
        if (levelSettings != null)
        {
            return;
        }

        WarnMissingLevelSettings();
        levelSettings = FindFirstObjectByType<LevelSettings>();

        if (levelSettings != null)
        {
            Debug.LogWarning($"{name}: LevelSettings was found in the scene at runtime. Assign it in Inspector before production.", this);
        }
    }

    private void ResolveProgressService()
    {
        if (progressService != null)
        {
            return;
        }

        WarnMissingProgressService();
        progressService = FindFirstObjectByType<ProgressService>();

        if (progressService != null)
        {
            Debug.LogWarning($"{name}: ProgressService was found in the scene at runtime. Assign it in Inspector before production.", this);
        }
    }

    private void WarnMissingProgressService()
    {
        if (missingProgressServiceWarningShown)
        {
            return;
        }

        missingProgressServiceWarningShown = true;
        Debug.LogWarning($"{name}: ProgressService reference is missing. Assign it in Inspector so level progress, stats, and procedural scores can be saved.", this);
    }
}
