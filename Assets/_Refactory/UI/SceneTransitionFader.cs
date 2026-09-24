using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class SceneTransitionFader : MonoBehaviour
{
    private const float FadeInDuration = 0.4f;
    private const float FadeOutDuration = 0.45f;
    private const float MaximumFadeFrameDelta = 1f / 30f;
    private const int OverlaySortingOrder = 32767;

    private static SceneTransitionFader instance;

    public static bool IsTransitioning => instance != null
        && (instance.sceneLoadPending
            || instance.canvasGroup != null && instance.canvasGroup.blocksRaycasts);

    private CanvasGroup canvasGroup;
    private Coroutine fadeCoroutine;
    private bool sceneLoadPending;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStaticState()
    {
        instance = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentFader()
    {
        if (instance != null)
        {
            return;
        }

        GameObject faderObject = new GameObject(
            "Scene Transition Fader",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(CanvasGroup));
        DontDestroyOnLoad(faderObject);

        instance = faderObject.AddComponent<SceneTransitionFader>();
        instance.CreateOverlay();
        SceneManager.sceneLoaded += instance.HandleSceneLoaded;
    }

    public static void LoadScene(int sceneBuildIndex)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(sceneBuildIndex);
            return;
        }

        instance.BeginSceneLoad(() => SceneManager.LoadScene(sceneBuildIndex));
    }

    public static void LoadScene(string sceneName)
    {
        if (instance == null)
        {
            SceneManager.LoadScene(sceneName);
            return;
        }

        instance.BeginSceneLoad(() => SceneManager.LoadScene(sceneName));
    }

    private void CreateOverlay()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = OverlaySortingOrder;

        CanvasScaler canvasScaler = GetComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasScaler.matchWidthOrHeight = 0.5f;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        raycaster.enabled = true;

        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = true;

        GameObject imageObject = new GameObject(
            "Black Overlay",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image));
        imageObject.transform.SetParent(transform, false);

        RectTransform imageTransform = imageObject.GetComponent<RectTransform>();
        imageTransform.anchorMin = Vector2.zero;
        imageTransform.anchorMax = Vector2.one;
        imageTransform.offsetMin = Vector2.zero;
        imageTransform.offsetMax = Vector2.zero;

        Image overlayImage = imageObject.GetComponent<Image>();
        overlayImage.color = Color.black;
        overlayImage.raycastTarget = true;
    }

    private void BeginSceneLoad(System.Action loadSceneAction)
    {
        if (sceneLoadPending || loadSceneAction == null)
        {
            return;
        }

        sceneLoadPending = true;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        fadeCoroutine = StartCoroutine(FadeOutThenLoad(loadSceneAction));
    }

    private IEnumerator FadeOutThenLoad(System.Action loadSceneAction)
    {
        canvasGroup.blocksRaycasts = true;
        yield return Fade(canvasGroup.alpha, 1f, FadeOutDuration);

        fadeCoroutine = null;
        loadSceneAction.Invoke();
    }

    private void HandleSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        sceneLoadPending = false;
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        // Scene loading can produce a delta larger than the whole fade duration.
        // Render one fully black frame before measuring the fade time.
        canvasGroup.alpha = 1f;
        yield return null;

        yield return Fade(1f, 0f, FadeInDuration);
        canvasGroup.blocksRaycasts = false;
        fadeCoroutine = null;
    }

    private IEnumerator Fade(float startAlpha, float targetAlpha, float duration)
    {
        if (duration <= 0f)
        {
            canvasGroup.alpha = targetAlpha;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Mathf.Min(Time.unscaledDeltaTime, MaximumFadeFrameDelta);
            float progress = Mathf.Clamp01(elapsed / duration);
            float easedProgress = progress * progress * (3f - 2f * progress);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (instance == this)
        {
            instance = null;
        }
    }
}
