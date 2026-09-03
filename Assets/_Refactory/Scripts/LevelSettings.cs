using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    private const int MinLightIntensity = 0;
    private const int MaxLightIntensity = 3;
    private const string EndlessHyperModeKey = "Endless_HyperMode";
    private const string EndlessHyperHyperModeKey = "Endless_HyperHyperMode";
    private const string EndlessFailureModeKey = "Endless_FailureMode";

    [Header("Mode")]
    [SerializeField] private bool isPuzzleMode = true;

    [Header("Score")]
    [SerializeField] private int bestHealthScore = 10;
    [SerializeField, Min(0)] private int maxMalusScore = 0;

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
    public int BestHealthScore => bestHealthScore;
    public int MaxMalusScore => maxMalusScore;
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

    public bool EndlessHyperMode => GetPlayerPrefBool(EndlessHyperModeKey);
    public bool EndlessHyperHyperMode => GetPlayerPrefBool(EndlessHyperHyperModeKey);
    public bool EndlessFailureMode => GetPlayerPrefBool(EndlessFailureModeKey);

    public void SetEndlessHyperMode(bool active)
    {
        SetPlayerPrefBool(EndlessHyperModeKey, active);

        if (active)
        {
            SetPlayerPrefBool(EndlessHyperHyperModeKey, false);
        }
    }

    public void SetEndlessHyperHyperMode(bool active)
    {
        SetPlayerPrefBool(EndlessHyperHyperModeKey, active);

        if (active)
        {
            SetPlayerPrefBool(EndlessHyperModeKey, false);
        }
    }

    public void SetEndlessFailureMode(bool active)
    {
        SetPlayerPrefBool(EndlessFailureModeKey, active);
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
        SetPlayerPrefBool(EndlessHyperModeKey, false);
        SetPlayerPrefBool(EndlessHyperHyperModeKey, false);
        SetPlayerPrefBool(EndlessFailureModeKey, false);
    }

    private bool GetPlayerPrefBool(string key)
    {
        return PlayerPrefs.GetInt(key, 0) == 1;
    }

    private void SetPlayerPrefBool(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
