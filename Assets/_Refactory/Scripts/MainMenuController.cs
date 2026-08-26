using System.Collections;
using InspectorValidation;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public sealed class MainMenuController : MonoBehaviour
{
    private enum MenuSection
    {
        None,
        Arcade,
        Advanced,
        Records
    }

    [Header("Core")]
    [SerializeField, RequiredInspectorReference] private AudioSource audioSource;
    [SerializeField, RequiredInspectorReference] private Animation titleScreenAnimation;
    [SerializeField, RequiredInspectorReference] private Animator lightAnimator;
    [SerializeField, RequiredInspectorReference] private Animator menuMovement;
    [FormerlySerializedAs("buttonAnimator")]
    [SerializeField, RequiredInspectorReference] private Animator[] buttonAnimators;

    [Header("Sections")]
    [FormerlySerializedAs("arcadeLevelCanvas")]
    [SerializeField, RequiredInspectorReference] private GameObject arcadePanel;
    [FormerlySerializedAs("advanceLevelCanvas")]
    [SerializeField, RequiredInspectorReference] private GameObject advancedPanel;
    [FormerlySerializedAs("recordLevelCanvas")]
    [SerializeField, RequiredInspectorReference] private GameObject recordsPanel;
    [SerializeField, RequiredInspectorReference] private GameObject pausePanel;
    [SerializeField, RequiredInspectorReference] private GameObject updateLogPanel;
    [FormerlySerializedAs("coffeMage")]
    [SerializeField, RequiredInspectorReference] private GameObject coffeeMage;

    [Header("Transition Timing")]
    [SerializeField, Min(0f)] private float buttonAnimationDelay = 1f;
    [SerializeField, Min(0f)] private float buttonAnimationStagger = 0.2f;
    [SerializeField, Min(0f)] private float sectionLightDelay = 0.8f;
    [SerializeField, Min(0f)] private float sectionOpenDelay = 2f;
    [SerializeField, Min(0f)] private float sectionCloseLightDelay = 0.5f;
    [SerializeField, Min(0f)] private float sectionCloseDelay = 2f;

    [Header("External Links")]
    [SerializeField] private string discordUrl = "https://discord.gg/M9CJxvkeFr";
    [SerializeField] private string itchUrl = "https://creative-lizards.itch.io/goodnight-potion";
    [SerializeField] private string buyMeACoffeeUrl = "https://www.buymeacoffee.com/creativelizards";

    [Header("Events")]
    [SerializeField] private UnityEvent resetProgressRequested = new UnityEvent();

    [FormerlySerializedAs("blockMenu")]
    [SerializeField] private bool transitionLocked;

    private const string ButtonPopTrigger = "pop";
    private const string StartLightTrigger = "Start";
    private const string LightMenuParameter = "menu";
    private const string VersionPreferenceKey = "Version";

    private Coroutine sectionTransitionCoroutine;
    private Coroutine buttonAnimationCoroutine;
    private MenuSection activeSection;

    private void Awake()
    {
        Time.timeScale = 1f;
        activeSection = ResolveInitiallyActiveSection();
        ShowUpdateLogForNewVersion();
    }

    public void StartScene()
    {
        if (!ValidateCoreAnimationReferences())
        {
            return;
        }

        titleScreenAnimation.Play();
        lightAnimator.SetTrigger(StartLightTrigger);
        PlayButtonAnimation();
        PlayMenuSound();
    }

    public void ButtonArcade()
    {
        ToggleSection(MenuSection.Arcade);
    }

    public void ButtonAdvance()
    {
        ToggleSection(MenuSection.Advanced);
    }

    public void ButtonRecord()
    {
        ToggleSection(MenuSection.Records);
    }

    public void PausePanel()
    {
        if (pausePanel == null)
        {
            Debug.LogError("MainMenuController requires the Pause Panel Inspector reference.", this);
            return;
        }

        pausePanel.SetActive(!pausePanel.activeSelf);
    }

    // Build-index routing is kept only until the refactored scene catalog is introduced.
    public void StartLevel(int sceneBuildIndex)
    {
        if (sceneBuildIndex < 0 || string.IsNullOrEmpty(SceneUtility.GetScenePathByBuildIndex(sceneBuildIndex)))
        {
            Debug.LogError($"MainMenuController cannot load build index {sceneBuildIndex}. Add the intended scene to Build Settings.", this);
            return;
        }

        SceneManager.LoadScene(sceneBuildIndex);
    }

    public void OpenDiscord()
    {
        OpenUrl(discordUrl, "Discord");
    }

    public void OpenItch()
    {
        OpenUrl(itchUrl, "Itch.io");
    }

    public void OpenBuyMeACoffe()
    {
        OpenUrl(buyMeACoffeeUrl, "Buy Me a Coffee");
    }

    public void CloseUpdateLog(bool resetProgress)
    {
        if (updateLogPanel == null)
        {
            Debug.LogError("MainMenuController requires the Update Log Panel Inspector reference.", this);
            return;
        }

        updateLogPanel.SetActive(false);
        if (resetProgress)
        {
            resetProgressRequested.Invoke();
        }
    }

    private void ToggleSection(MenuSection section)
    {
        GameObject sectionPanel = GetSectionPanel(section);
        if (sectionPanel == null)
        {
            Debug.LogError($"MainMenuController requires the {section} Panel Inspector reference.", this);
            return;
        }

        PlayMenuSound();

        if (activeSection == section || sectionPanel.activeSelf)
        {
            BeginSectionTransition(CloseSection(section, sectionPanel));
            return;
        }

        if (transitionLocked || activeSection != MenuSection.None)
        {
            return;
        }

        BeginSectionTransition(OpenSection(section, sectionPanel));
    }

    private void BeginSectionTransition(IEnumerator transition)
    {
        if (transitionLocked)
        {
            return;
        }

        if (sectionTransitionCoroutine != null)
        {
            StopCoroutine(sectionTransitionCoroutine);
        }

        sectionTransitionCoroutine = StartCoroutine(transition);
    }

    private IEnumerator OpenSection(MenuSection section, GameObject sectionPanel)
    {
        if (!ValidateTransitionReferences())
        {
            yield break;
        }

        transitionLocked = true;
        menuMovement.SetTrigger(GetMovementTrigger(section));
        PlayButtonAnimation();

        yield return WaitUnscaled(sectionLightDelay);

        if (section != MenuSection.Advanced)
        {
            lightAnimator.SetInteger(LightMenuParameter, 1);
            yield return WaitUnscaled(sectionOpenDelay);
        }
        else
        {
            float remainingDelay = Mathf.Max(0f, sectionOpenDelay - sectionLightDelay);
            yield return WaitUnscaled(remainingDelay);
        }

        if (section == MenuSection.Records && coffeeMage != null)
        {
            coffeeMage.SetActive(true);
        }

        sectionPanel.SetActive(true);
        activeSection = section;
        transitionLocked = false;
        sectionTransitionCoroutine = null;
    }

    private IEnumerator CloseSection(MenuSection section, GameObject sectionPanel)
    {
        if (!ValidateTransitionReferences())
        {
            yield break;
        }

        transitionLocked = true;
        sectionPanel.SetActive(false);

        if (section == MenuSection.Records && coffeeMage != null)
        {
            coffeeMage.SetActive(false);
        }

        PlayButtonAnimation();
        yield return WaitUnscaled(sectionCloseLightDelay);

        lightAnimator.SetInteger(LightMenuParameter, 0);
        menuMovement.SetTrigger(GetMovementTrigger(section));

        yield return WaitUnscaled(sectionCloseDelay);

        activeSection = MenuSection.None;
        transitionLocked = false;
        sectionTransitionCoroutine = null;
    }

    private void PlayButtonAnimation()
    {
        if (buttonAnimators == null || buttonAnimators.Length == 0)
        {
            Debug.LogError("MainMenuController requires at least one Button Animator Inspector reference.", this);
            return;
        }

        if (buttonAnimationCoroutine != null)
        {
            StopCoroutine(buttonAnimationCoroutine);
        }

        buttonAnimationCoroutine = StartCoroutine(AnimateButtons());
    }

    private IEnumerator AnimateButtons()
    {
        yield return WaitUnscaled(buttonAnimationDelay);

        foreach (Animator buttonAnimator in buttonAnimators)
        {
            if (buttonAnimator == null)
            {
                Debug.LogError("MainMenuController has a missing element in Button Animators. Assign every element in Inspector.", this);
                continue;
            }

            buttonAnimator.SetTrigger(ButtonPopTrigger);
            yield return WaitUnscaled(buttonAnimationStagger);
        }

        buttonAnimationCoroutine = null;
    }

    private IEnumerator WaitUnscaled(float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    private MenuSection ResolveInitiallyActiveSection()
    {
        if (arcadePanel != null && arcadePanel.activeSelf)
        {
            return MenuSection.Arcade;
        }

        if (advancedPanel != null && advancedPanel.activeSelf)
        {
            return MenuSection.Advanced;
        }

        if (recordsPanel != null && recordsPanel.activeSelf)
        {
            return MenuSection.Records;
        }

        return MenuSection.None;
    }

    private GameObject GetSectionPanel(MenuSection section)
    {
        switch (section)
        {
            case MenuSection.Arcade:
                return arcadePanel;
            case MenuSection.Advanced:
                return advancedPanel;
            case MenuSection.Records:
                return recordsPanel;
            default:
                return null;
        }
    }

    private string GetMovementTrigger(MenuSection section)
    {
        switch (section)
        {
            case MenuSection.Arcade:
                return "right";
            case MenuSection.Advanced:
                return "down";
            case MenuSection.Records:
                return "left";
            default:
                return string.Empty;
        }
    }

    private void ShowUpdateLogForNewVersion()
    {
        if (updateLogPanel == null)
        {
            Debug.LogError("MainMenuController requires the Update Log Panel Inspector reference.", this);
            return;
        }

        string currentVersion = Application.version;
        bool versionChanged = !PlayerPrefs.HasKey(VersionPreferenceKey)
            || PlayerPrefs.GetString(VersionPreferenceKey) != currentVersion;

        updateLogPanel.SetActive(versionChanged);
        if (versionChanged)
        {
            PlayerPrefs.SetString(VersionPreferenceKey, currentVersion);
        }
    }

    private bool ValidateCoreAnimationReferences()
    {
        if (titleScreenAnimation == null)
        {
            Debug.LogError("MainMenuController requires the Title Screen Animation Inspector reference.", this);
            return false;
        }

        if (lightAnimator == null)
        {
            Debug.LogError("MainMenuController requires the Light Animator Inspector reference.", this);
            return false;
        }

        return true;
    }

    private bool ValidateTransitionReferences()
    {
        if (menuMovement == null)
        {
            Debug.LogError("MainMenuController requires the Menu Movement Animator Inspector reference.", this);
            return false;
        }

        if (lightAnimator == null)
        {
            Debug.LogError("MainMenuController requires the Light Animator Inspector reference.", this);
            return false;
        }

        return true;
    }

    private void PlayMenuSound()
    {
        if (audioSource == null)
        {
            Debug.LogError("MainMenuController requires the Audio Source Inspector reference.", this);
            return;
        }

        audioSource.Play();
    }

    private void OpenUrl(string url, string destinationName)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            Debug.LogWarning($"MainMenuController cannot open {destinationName} because its URL is empty.", this);
            return;
        }

        Application.OpenURL(url);
    }
}
