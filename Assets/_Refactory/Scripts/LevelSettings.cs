using System;
using System.Collections.Generic;
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

[Serializable]
public sealed class LevelIntroPhrase
{
    [SerializeField, TextArea] private string text;
    [SerializeField] private AudioClip voiceClip;

    public string Text => text;
    public AudioClip VoiceClip => voiceClip;
}

public class LevelSettings : MonoBehaviour
{
    private const int MinLightIntensity = 0;
    private const int MaxLightIntensity = 3;
    private const string EndlessHyperModeKey = "Endless_HyperMode";
    private const string EndlessHyperHyperModeKey = "Endless_HyperHyperMode";
    private const string EndlessFailureModeKey = "Endless_FailureMode";
    private const string EndlessFadingLightKey = "Endless_FadingLight";
    private const string EndlessNightFallKey = "Endless_NightFall";
    private const string EndlessExpensiveMagicKey = "Endless_ExpensiveMagic";
    private const string EndlessTransformationFatigueKey = "Endless_TransformationFatigue";
    private const string EndlessLingeringEffectsKey = "Endless_LingeringEffects";

    [Header("Mode")]
    [SerializeField] private bool isPuzzleMode = true;
    [SerializeField] private LevelGameMode gameMode = LevelGameMode.Classic;

    [Header("Score")]
    [SerializeField] private int bestHealthScore = 10;
    [SerializeField, Min(0)] private int maxMalusScore = 0;

    [Header("Intro Presentation")]
    [SerializeField, TextArea] private string introPresentationLine;
    [SerializeField] private AudioClip introPresentationVoiceClip;
    [SerializeField] private List<LevelIntroPhrase> additionalIntroPhrases = new List<LevelIntroPhrase>();
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
    [SerializeField] private int maxActivePotionsBeforeBomb = 40;

    public bool IsPuzzleMode => isPuzzleMode;
    public LevelGameMode GameMode => gameMode;
    public int BestHealthScore => bestHealthScore;
    public int MaxMalusScore => maxMalusScore;
    public string IntroPresentationLine => introPresentationLine;
    public AudioClip IntroPresentationVoiceClip => introPresentationVoiceClip;
    public IReadOnlyList<LevelIntroPhrase> AdditionalIntroPhrases => additionalIntroPhrases;
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
    public bool EndlessFadingLight => gameMode == LevelGameMode.Endless && SavedEndlessFadingLight;
    public bool EndlessNightFall => gameMode == LevelGameMode.Endless && SavedEndlessNightFall;
    public bool EndlessExpensiveMagic => gameMode == LevelGameMode.Endless && SavedEndlessExpensiveMagic;
    public bool EndlessTransformationFatigue => gameMode == LevelGameMode.Endless && SavedEndlessTransformationFatigue;
    public bool EndlessLingeringEffects => gameMode == LevelGameMode.Endless && SavedEndlessLingeringEffects;

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
    public static bool SavedEndlessFadingLight => GetSavedBool(EndlessFadingLightKey);
    public static bool SavedEndlessNightFall => GetSavedBool(EndlessNightFallKey);
    public static bool SavedEndlessExpensiveMagic => GetSavedBool(EndlessExpensiveMagicKey);
    public static bool SavedEndlessTransformationFatigue => GetSavedBool(EndlessTransformationFatigueKey);
    public static bool SavedEndlessLingeringEffects => GetSavedBool(EndlessLingeringEffectsKey);

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
        1f
        + (SavedEndlessBaseScoreMultiplier - 1f)
        + (SavedEndlessFlawlessMode ? 1f : 0f)
        + (SavedEndlessFadingLight ? 0.2f : 0f)
        + (SavedEndlessNightFall ? 0.2f : 0f)
        + (SavedEndlessExpensiveMagic ? 0.5f : 0f)
        + (SavedEndlessTransformationFatigue ? 0.5f : 0f)
        + (SavedEndlessLingeringEffects ? 0.25f : 0f);

    public static void SetSavedEndlessBaseModifier(EndlessBaseModifier modifier)
    {
        SetSavedBool(EndlessHyperModeKey, modifier == EndlessBaseModifier.Hyper, false);
        SetSavedBool(EndlessHyperHyperModeKey, modifier == EndlessBaseModifier.HyperHyper, true);
    }

    public static void SetSavedEndlessFlawlessMode(bool active)
    {
        SetSavedBool(EndlessFailureModeKey, active, true);
    }

    public static void SetSavedEndlessFadingLight(bool active) => SetSavedBool(EndlessFadingLightKey, active, true);
    public static void SetSavedEndlessNightFall(bool active) => SetSavedBool(EndlessNightFallKey, active, true);
    public static void SetSavedEndlessExpensiveMagic(bool active) => SetSavedBool(EndlessExpensiveMagicKey, active, true);
    public static void SetSavedEndlessTransformationFatigue(bool active) => SetSavedBool(EndlessTransformationFatigueKey, active, true);
    public static void SetSavedEndlessLingeringEffects(bool active) => SetSavedBool(EndlessLingeringEffectsKey, active, true);

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
        SetSavedEndlessFadingLight(false);
        SetSavedEndlessNightFall(false);
        SetSavedEndlessExpensiveMagic(false);
        SetSavedEndlessTransformationFatigue(false);
        SetSavedEndlessLingeringEffects(false);
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
