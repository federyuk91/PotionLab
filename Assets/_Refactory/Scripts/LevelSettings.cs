using UnityEngine;

public enum LevelGameMode
{
    Classic = 0,
    Laboratory = 1,
    Endless = 2
}

public enum EndlessBaseModifier
{
    Normal = 0,
    Hyper = 1,
    HyperHyper = 2
}

public class LevelSettings : MonoBehaviour
{
    private const int MinLightIntensity = 0;
    private const int MaxLightIntensity = 3;
    private const string EndlessHyperModeKey = "Endless_HyperMode";
    private const string EndlessHyperHyperModeKey = "Endless_HyperHyperMode";
    private const string EndlessFailureModeKey = "Endless_FailureMode";

    [Header("Mode")]
    [SerializeField] private bool isPuzzleMode = true;
    [SerializeField] private LevelGameMode gameMode = LevelGameMode.Classic;

    [Header("Score")]
    [SerializeField] private int bestHealthScore = 10;
    [SerializeField, Min(0)] private int maxMalusScore = 0;

    [Header("Intro Presentation")]
    [SerializeField, TextArea] private string introPresentationLine;
    [SerializeField] private AudioClip introPresentationVoiceClip;
    [SerializeField, Min(1f)] private float introPresentationCharactersPerSecond = 35f;
    [SerializeField, Min(0f)] private float introPresentationStartDelay = 0f;

    [Header("Light")]
    [SerializeField, Range(MinLightIntensity, MaxLightIntensity)] private int startingLightIntensity = 1;
    [SerializeField] private string startingCatchphrase;
    [SerializeField, Min(0f)] private float startingCatchphraseDuration = 3f;
    [SerializeField] private bool decayLightOverTime = false;
    [SerializeField] private float lightDecayInterval = 43f;

    [Header("Endless Spawn Speed")]
    [SerializeField] private float defaultSpawnSeconds = 5f;
    [SerializeField] private float hyperModeSpawnSeconds = 3f;
    [SerializeField] private float hyperHyperModeSpawnSeconds = 2f;
    [SerializeField] private float minimumSpawnSeconds = 1f;

    [Header("Endless Events")]
    [SerializeField] private int maxActivePotionsBeforeBomb = 30;

    public bool IsPuzzleMode => isPuzzleMode;
    public LevelGameMode GameMode => gameMode;
    public int BestHealthScore => bestHealthScore;
    public int MaxMalusScore => maxMalusScore;
    public string IntroPresentationLine => introPresentationLine;
    public AudioClip IntroPresentationVoiceClip => introPresentationVoiceClip;
    public float IntroPresentationCharactersPerSecond => introPresentationCharactersPerSecond;
    public float IntroPresentationStartDelay => introPresentationStartDelay;
    public int StartingLightIntensity => startingLightIntensity;
    public string StartingCatchphrase => startingCatchphrase;
    public float StartingCatchphraseDuration => startingCatchphraseDuration;
    public bool DecayLightOverTime => decayLightOverTime;
    public float LightDecayInterval => lightDecayInterval;
    public float DefaultSpawnSeconds => defaultSpawnSeconds;
    public float HyperModeSpawnSeconds => hyperModeSpawnSeconds;
    public float HyperHyperModeSpawnSeconds => hyperHyperModeSpawnSeconds;
    public float MinimumSpawnSeconds => minimumSpawnSeconds;
    public int MaxActivePotionsBeforeBomb => maxActivePotionsBeforeBomb;

    public bool EndlessHyperMode => SavedEndlessBaseModifier == EndlessBaseModifier.Hyper;
    public bool EndlessHyperHyperMode => SavedEndlessBaseModifier == EndlessBaseModifier.HyperHyper;
    public bool EndlessFlawlessMode => SavedEndlessFlawlessMode;
    public bool EndlessFailureMode => EndlessFlawlessMode;

    public static EndlessBaseModifier SavedEndlessBaseModifier
    {
        get
        {
            if (GetSavedBool(EndlessHyperHyperModeKey))
            {
                return EndlessBaseModifier.HyperHyper;
            }

            return GetSavedBool(EndlessHyperModeKey)
                ? EndlessBaseModifier.Hyper
                : EndlessBaseModifier.Normal;
        }
    }

    public static bool SavedEndlessFlawlessMode => GetSavedBool(EndlessFailureModeKey);

    public static float SavedEndlessBaseScoreMultiplier
    {
        get
        {
            switch (SavedEndlessBaseModifier)
            {
                case EndlessBaseModifier.Hyper:
                    return 1.5f;
                case EndlessBaseModifier.HyperHyper:
                    return 2f;
                default:
                    return 1f;
            }
        }
    }

    public static float SavedEndlessTotalScoreMultiplier =>
        SavedEndlessBaseScoreMultiplier * (SavedEndlessFlawlessMode ? 2f : 1f);

    public static void SetSavedEndlessBaseModifier(EndlessBaseModifier modifier)
    {
        SetSavedBool(EndlessHyperModeKey, modifier == EndlessBaseModifier.Hyper, false);
        SetSavedBool(EndlessHyperHyperModeKey, modifier == EndlessBaseModifier.HyperHyper, true);
    }

    public static void SetSavedEndlessFlawlessMode(bool active)
    {
        SetSavedBool(EndlessFailureModeKey, active, true);
    }

    public void SetEndlessHyperMode(bool active)
    {
        SetSavedEndlessBaseModifier(active ? EndlessBaseModifier.Hyper : EndlessBaseModifier.Normal);
    }

    public void SetEndlessHyperHyperMode(bool active)
    {
        SetSavedEndlessBaseModifier(active ? EndlessBaseModifier.HyperHyper : EndlessBaseModifier.Normal);
    }

    public void SetEndlessFailureMode(bool active)
    {
        SetSavedEndlessFlawlessMode(active);
    }

    public void ToggleEndlessHyperMode()
    {
        SetEndlessHyperMode(!EndlessHyperMode);
    }

    public void ToggleEndlessHyperHyperMode()
    {
        SetEndlessHyperHyperMode(!EndlessHyperHyperMode);
    }

    public void ToggleEndlessFailureMode()
    {
        SetEndlessFailureMode(!EndlessFailureMode);
    }

    public void ResetEndlessPreferences()
    {
        SetSavedEndlessBaseModifier(EndlessBaseModifier.Normal);
        SetSavedEndlessFlawlessMode(false);
    }

    private static bool GetSavedBool(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private static void SetSavedBool(string key, bool value, bool save)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        if (save)
        {
            PlayerPrefs.Save();
        }
    }
}
