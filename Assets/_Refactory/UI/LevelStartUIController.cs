using System.Collections;
using InspectorValidation;
using UnityEngine;
using UnityEngine.UI;

public class LevelStartUIController : MonoBehaviour
{
    [SerializeField, RequiredInspectorReference] private CanvasGroup gameplayUI;
    [SerializeField, RequiredInspectorReference] private Button startLevelButton;
    [SerializeField, RequiredInspectorReference] private RectTransform currentNight;
    [SerializeField, Min(1f)] private float nightIntroScale = 2.5f;
    [SerializeField, Min(0f)] private float nightTransitionDuration = 1f;
    [SerializeField, Min(0f)] private float gameplayUIFadeDuration = 0.3f;

    private bool levelStartHandled;
    private bool nightIntroReady;
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
    }

    private void HandleLevelStartClicked()
    {
        if (levelStartHandled)
        {
            return;
        }

        levelStartHandled = true;
        StartCoroutine(FadeGameplayUIIn());

        if (nightIntroReady)
        {
            StartCoroutine(AnimateNightToHud());
        }

        startLevelButton.gameObject.SetActive(false);
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
