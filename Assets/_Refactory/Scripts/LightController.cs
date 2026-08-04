using System;
using CharacterSystem;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class LightController : MonoBehaviour
{
    private const int MinLightIntensity = 0;
    private const int MaxLightIntensity = 3;

    //public Color Mage, Balrog, Tree, Yeti, Pupperfish, Litch, WhiteMage;
    [Header("Light Fields")]
    [SerializeField] private GameObject fireField;
    [SerializeField] private GameObject grassField;
    [SerializeField] private GameObject waterField;
    [SerializeField] private GameObject iceField;

    [Header("Level Settings")]
    [SerializeField] private LevelSettings levelSettings;

    [Header("References")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Animator animator;
    [SerializeField] private Light2D light2D;
    private float lightDecayTimer = 0f;
    [SerializeField] private LightFieldType currentLightField = LightFieldType.None;

    public int lightIntensity = 0;
    public LightFieldType CurrentLightField => currentLightField;
    public int LightIntensity => lightIntensity;
    public float LightDecayProgress => GetLightDecayProgress();

    public event Action<int> LightLevelChanged;
    public event Action<float> LightTimerChanged;

    private bool missingLevelSettingsWarningShown;
    private bool missingAnimatorWarningShown;
    private bool missingLight2DWarningShown;
    private bool missingAudioSourceWarningShown;

    private void Awake()
    {
        ResolveLevelSettings();
        ResolveLocalReferences();
        SetLightLevel(GetStartingLightIntensity(), false, false, false);
        RefreshLightFields();

    }

    private void Update()
    {
        if (!ShouldDecayLight())
        {
            return;
        }

        lightDecayTimer += Time.deltaTime;

        if (lightDecayTimer >= GetLightDecayInterval())
        {
            DecreaseLightLevel();
            ResetLightDecayTimer();
        }

        NotifyLightTimerChanged();
    }

    public void ChangeLightColor(Color c)
    {
        if (light2D == null)
        {
            WarnMissingLight2D();
            return;
        }

        light2D.color = c;
    }
    /* deprecated, now we use light fields to determine if a character is powered or not
    public void ChangeLightColor(CharacterType character)
    {
        switch (character)
        {
            case CharacterType.Mage:
                light2D.color = Mage;
                break;
            case CharacterType.Balrog:
                light2D.color = Balrog;
                break;
            case CharacterType.Tree:
                light2D.color = Tree;
                break;
            case CharacterType.Yeti:
                light2D.color = Yeti;
                break;
            case CharacterType.PupperFish:
                light2D.color = Pupperfish;
                break;
            case CharacterType.Litch:
                light2D.color = Litch;
                break;
            case CharacterType.WhiteMage:
                light2D.color = WhiteMage;
                break;
        }
    }
    */


    public void SetLightField(LightFieldType fieldType)
    {
        currentLightField = fieldType;
        RefreshLightFields();
    }

    public void ClearLightField()
    {
        currentLightField = LightFieldType.None;
        RefreshLightFields();
    }

    public void ToggleLightField(LightFieldType fieldType)
    {
        if (currentLightField == fieldType)
        {
            ClearLightField();
            return;
        }

        SetLightField(fieldType);
    }

    public bool IsLightFieldActive(LightFieldType fieldType)
    {
        return currentLightField == fieldType;
    }

    public bool IsPoweredFor(CharacterType characterType)
    {
        switch (characterType)
        {
            case CharacterType.Mage:
                return lightIntensity == MaxLightIntensity;
            case CharacterType.Balrog:
                return IsLightFieldActive(LightFieldType.Fire);
            case CharacterType.Tree:
                return IsLightFieldActive(LightFieldType.Grass);
            case CharacterType.PupperFish:
                return IsLightFieldActive(LightFieldType.Water);
            case CharacterType.Yeti:
                return IsLightFieldActive(LightFieldType.Ice);
            default:
                Debug.LogWarning($"IsPoweredFor: {characterType} not implemented yet. Returning false.");
                return false;
        }
    }

    private void RefreshLightFields()
    {
        SetFieldActive(fireField, currentLightField == LightFieldType.Fire);
        SetFieldActive(grassField, currentLightField == LightFieldType.Grass);
        SetFieldActive(waterField, currentLightField == LightFieldType.Water);
        SetFieldActive(iceField, currentLightField == LightFieldType.Ice);
    }

    private void SetFieldActive(GameObject field, bool active)
    {
        if (field == null)
        {
            return;
        }

        field.SetActive(active);
    }
    public void IncreaseLightLevel()
    {
        if (lightIntensity < MaxLightIntensity)
        {
            SetLightLevel(lightIntensity + 1, true, true, true);
        }

        if (ShouldDecayLight())
        {
            ResetLightDecayTimer();
        }

    }

    public void DecreaseLightLevel()
    {
        SetLightLevel(lightIntensity - 1, false, true, true);
    }

    public void SetLightLevel(int intensity)
    {
        SetLightLevel(intensity, false, true, true);
    }

    private void SetLightLevel(int intensity, bool triggerLightOn, bool triggerAnimation, bool playAudio)
    {
        lightIntensity = Mathf.Clamp(intensity, MinLightIntensity, MaxLightIntensity);

        if (animator == null)
        {
            WarnMissingAnimator();
        }
        else if (triggerAnimation && triggerLightOn)
        {
            animator.SetTrigger("lightOn");
        }
        else if (triggerAnimation)
        {
            animator.SetTrigger("lightAdvance");
        }

        if (animator != null)
        {
            animator.SetInteger("lightIntesity", lightIntensity);
        }

        LightLevelChanged?.Invoke(lightIntensity);

        if (playAudio)
        {
            PlayAudio();
        }
    }

    private bool ShouldDecayLight()
    {
        return GetDecayLightOverTime();
    }

    private void ResetLightDecayTimer()
    {
        lightDecayTimer = 0f;
        NotifyLightTimerChanged();
    }

    private void NotifyLightTimerChanged()
    {
        LightTimerChanged?.Invoke(GetLightDecayProgress());
    }

    private float GetLightDecayProgress()
    {
        float lightDecayInterval = GetLightDecayInterval();
        if (lightDecayInterval <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(lightDecayTimer / lightDecayInterval);
    }

    public void PlayAudio()
    {
        if (audioSource == null)
        {
            WarnMissingAudioSource();
            return;
        }

        if (audioSource.clip == null)
        {
            Debug.LogWarning($"AUDIO: {name} cannot play light feedback because the AudioSource clip is missing.", this);
            return;
        }

        audioSource.Play();
    }

    public void StartLight()
    {

        //lancia l'animazione della magia di luce determinando il livello di intensità stabilito per questo livello
        if (animator == null)
        {
            WarnMissingAnimator();
        }
        else
        {
            animator.SetTrigger("lightOn");
            animator.SetInteger("lightIntesity", lightIntensity);
        }

        PlayAudio();
    }

    private void ResolveLocalReferences()
    {
        if (animator == null)
        {
            animator = GetComponent<Animator>();

            if (animator != null)
            {
                Debug.LogWarning($"{name}: Animator was recovered with GetComponent. Assign it in Inspector before production.", this);
            }
        }

        if (light2D == null)
        {
            light2D = GetComponent<Light2D>();

            if (light2D != null)
            {
                Debug.LogWarning($"{name}: Light2D was recovered with GetComponent. Assign it in Inspector before production.", this);
            }
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();

            if (audioSource != null)
            {
                Debug.LogWarning($"{name}: AudioSource was recovered with GetComponent. Assign it in Inspector before production.", this);
            }
        }
    }

    private void WarnMissingAnimator()
    {
        if (missingAnimatorWarningShown)
        {
            return;
        }

        missingAnimatorWarningShown = true;
        Debug.LogWarning($"{name}: Animator reference is missing. Assign it in Inspector to play light animations.", this);
    }

    private void WarnMissingLight2D()
    {
        if (missingLight2DWarningShown)
        {
            return;
        }

        missingLight2DWarningShown = true;
        Debug.LogError($"{name}: Light2D reference is missing. Assign it in Inspector to allow light color changes.", this);
    }

    private void WarnMissingAudioSource()
    {
        if (missingAudioSourceWarningShown)
        {
            return;
        }

        missingAudioSourceWarningShown = true;
        Debug.LogWarning($"AUDIO: {name} AudioSource reference is missing. Assign it in Inspector to play light feedback.", this);
    }

    private int GetStartingLightIntensity()
    {
        if (levelSettings == null)
        {
            WarnMissingLevelSettings();
            return 1;
        }

        return levelSettings.StartingLightIntensity;
    }

    private bool GetDecayLightOverTime()
    {
        if (levelSettings == null)
        {
            WarnMissingLevelSettings();
            return false;
        }

        return levelSettings.DecayLightOverTime;
    }

    private float GetLightDecayInterval()
    {
        if (levelSettings == null)
        {
            WarnMissingLevelSettings();
            return 43f;
        }

        return levelSettings.LightDecayInterval;
    }

    private void WarnMissingLevelSettings()
    {
        if (missingLevelSettingsWarningShown)
        {
            return;
        }

        missingLevelSettingsWarningShown = true;
        Debug.LogWarning($"{name}: LevelSettings reference is missing. Assign it in Inspector to configure starting light and light decay.", this);
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
}
