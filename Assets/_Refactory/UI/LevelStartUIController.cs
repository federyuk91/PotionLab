using System.Collections;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelStartUIController : MonoBehaviour
{
    [Header("Sources")]
    [SerializeField, RequiredInspectorReference(ResolveMode.SceneSingleton)] private LevelSettings levelSettings;

    [Header("Intro Presentation")]
    [SerializeField, RequiredInspectorReference] private TMP_Text introPresentationText;
    [SerializeField] private AudioSource introPresentationAudioSource;

    [Header("Start Transition")]
    [SerializeField, RequiredInspectorReference] private CanvasGroup gameplayUI;
    [SerializeField, RequiredInspectorReference] private Button startLevelButton;
    [SerializeField, RequiredInspectorReference] private RectTransform currentNight;
    [SerializeField, Min(1f)] private float nightIntroScale = 2.5f;
    [SerializeField, Min(0f)] private float nightTransitionDuration = 1f;
    [SerializeField, Min(0f)] private float gameplayUIFadeDuration = 0.3f;

    private bool levelStartHandled;
    private bool nightIntroReady;
    private Coroutine introPresentationRoutine;
    private Vector2 nightTargetPosition;
    private Vector3 nightTargetScale;
    private Vector2 nightIntroPosition;
    private Vector3 nightIntroStartScale;

    private void Awake()
    {
        SetGameplayUIVisible(false);
    }

    private void Start()
    {
        PrepareNightIntro();
        PlayIntroPresentation();
    }

    private void OnEnable()
    {
        if (startLevelButton == null)
        {
            Debug.LogError("LevelStartUIController requires the Start Level Button Inspector reference.", this);
            return;
        }

        startLevelButton.onClick.AddListener(HandleLevelStartClicked);
    }

    private void OnDisable()
    {
        if (startLevelButton != null)
        {
            startLevelButton.onClick.RemoveListener(HandleLevelStartClicked);
        }

        StopIntroPresentation(false);
    }

    private void HandleLevelStartClicked()
    {
        if (levelStartHandled)
        {
            return;
        }

        levelStartHandled = true;
        StopIntroPresentation(true);
        StartCoroutine(FadeGameplayUIIn());

        if (nightIntroReady)
        {
            StartCoroutine(AnimateNightToHud());
        }

        startLevelButton.gameObject.SetActive(false);
    }

    private void PlayIntroPresentation()
    {
        if (introPresentationRoutine != null)
        {
            StopCoroutine(introPresentationRoutine);
        }

        introPresentationRoutine = StartCoroutine(PlayIntroPresentationRoutine());
    }

    private IEnumerator PlayIntroPresentationRoutine()
    {
        if (introPresentationText == null)
        {
            Debug.LogError("LevelStartUIController requires the Intro Presentation Text Inspector reference.", this);
            introPresentationRoutine = null;
            yield break;
        }

        introPresentationText.text = string.Empty;
        if (levelSettings == null)
        {
            Debug.LogError("LevelStartUIController requires the Level Settings Inspector reference.", this);
            introPresentationRoutine = null;
            yield break;
        }

        string introLine = levelSettings.IntroPresentationLine;
        if (string.IsNullOrWhiteSpace(introLine))
        {
            introPresentationRoutine = null;
            yield break;
        }

        float startDelay = levelSettings.IntroPresentationStartDelay;
        if (startDelay > 0f)
        {
            yield return new WaitForSecondsRealtime(startDelay);
        }

        PlayIntroPresentationAudio(levelSettings.IntroPresentationVoiceClip);
        yield return TypeIntroPresentationLine(introLine, levelSettings.IntroPresentationCharactersPerSecond);
        introPresentationRoutine = null;
    }

    private IEnumerator TypeIntroPresentationLine(string introLine, float charactersPerSecond)
    {
        float safeCharactersPerSecond = Mathf.Max(1f, charactersPerSecond);
        float secondsPerCharacter = 1f / safeCharactersPerSecond;
        float elapsed = 0f;
        int visibleCharacters = 0;

        introPresentationText.text = string.Empty;
        while (visibleCharacters < introLine.Length)
        {
            elapsed += Time.unscaledDeltaTime;
            int targetVisibleCharacters = Mathf.Clamp(
                Mathf.FloorToInt(elapsed / secondsPerCharacter),
                0,
                introLine.Length);

            if (targetVisibleCharacters > visibleCharacters)
            {
                visibleCharacters = targetVisibleCharacters;
                introPresentationText.text = introLine.Substring(0, visibleCharacters);
            }

            yield return null;
        }

        introPresentationText.text = introLine;
    }

    private void PlayIntroPresentationAudio(AudioClip voiceClip)
    {
        if (voiceClip == null)
        {
            return;
        }

        if (introPresentationAudioSource == null)
        {
            Debug.LogWarning("LevelStartUIController cannot play the intro presentation voice because Intro Presentation Audio Source is missing.", this);
            return;
        }

        introPresentationAudioSource.Stop();
        introPresentationAudioSource.PlayOneShot(voiceClip);
    }

    private void StopIntroPresentation(bool clearText)
    {
        if (introPresentationRoutine != null)
        {
            StopCoroutine(introPresentationRoutine);
            introPresentationRoutine = null;
        }

        StopIntroPresentationAudio();

        if (clearText && introPresentationText != null)
        {
            introPresentationText.text = string.Empty;
        }
    }

    private void StopIntroPresentationAudio()
    {
        if (introPresentationAudioSource != null && introPresentationAudioSource.isPlaying)
        {
            introPresentationAudioSource.Stop();
        }
    }

    private void PrepareNightIntro()
    {
        if (currentNight == null)
        {
            Debug.LogError("LevelStartUIController requires the Current Night RectTransform Inspector reference.", this);
            return;
        }

        RectTransform nightParent = currentNight.parent as RectTransform;
        if (nightParent == null)
        {
            Debug.LogError("LevelStartUIController requires Current Night to have a RectTransform parent.", this);
            return;
        }

        Canvas.ForceUpdateCanvases();

        nightTargetPosition = currentNight.anchoredPosition;
        nightTargetScale = currentNight.localScale;
        nightIntroStartScale = new Vector3(
            nightTargetScale.x * nightIntroScale,
            nightTargetScale.y * nightIntroScale,
            nightTargetScale.z);

        Rect parentRect = nightParent.rect;
        Vector2 anchorReference = new Vector2(
            parentRect.xMin + parentRect.width * currentNight.anchorMin.x,
            parentRect.yMin + parentRect.height * currentNight.anchorMin.y);
        Vector2 pivotToCenter = new Vector2(
            (0.5f - currentNight.pivot.x) * currentNight.rect.width * nightIntroStartScale.x,
            (0.5f - currentNight.pivot.y) * currentNight.rect.height * nightIntroStartScale.y);

        nightIntroPosition = parentRect.center - anchorReference - pivotToCenter;
        currentNight.anchoredPosition = nightIntroPosition;
        currentNight.localScale = nightIntroStartScale;
        nightIntroReady = true;
    }

    private IEnumerator AnimateNightToHud()
    {
        if (nightTransitionDuration <= 0f)
        {
            currentNight.anchoredPosition = nightTargetPosition;
            currentNight.localScale = nightTargetScale;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < nightTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / nightTransitionDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);

            currentNight.anchoredPosition = Vector2.LerpUnclamped(
                nightIntroPosition,
                nightTargetPosition,
                easedProgress);
            currentNight.localScale = Vector3.LerpUnclamped(
                nightIntroStartScale,
                nightTargetScale,
                easedProgress);

            yield return null;
        }

        currentNight.anchoredPosition = nightTargetPosition;
        currentNight.localScale = nightTargetScale;
    }

    private IEnumerator FadeGameplayUIIn()
    {
        if (gameplayUI == null)
        {
            Debug.LogError("LevelStartUIController requires the Gameplay UI CanvasGroup Inspector reference.", this);
            yield break;
        }

        gameplayUI.interactable = false;
        gameplayUI.blocksRaycasts = false;

        if (gameplayUIFadeDuration <= 0f)
        {
            SetGameplayUIVisible(true);
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < gameplayUIFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / gameplayUIFadeDuration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            gameplayUI.alpha = easedProgress;
            yield return null;
        }

        SetGameplayUIVisible(true);
    }

    private void SetGameplayUIVisible(bool visible)
    {
        if (gameplayUI == null)
        {
            Debug.LogError("LevelStartUIController requires the Gameplay UI CanvasGroup Inspector reference.", this);
            return;
        }

        gameplayUI.alpha = visible ? 1f : 0f;
        gameplayUI.interactable = visible;
        gameplayUI.blocksRaycasts = visible;
    }
}
