using System.Collections;
using Refactory.UI.GridList;
using UnityEngine;

public class GrimoireAnimation : MonoBehaviour
{
    public Animator anim;
    public bool isOpen = false;
    public GameObject menuPanel;
    [SerializeField] private GameObject grimoireBase;
    [SerializeField] private CanvasGroup grimoireBaseCanvasGroup;
    [SerializeField, Min(0f)] private float grimoireBaseFadeDuration = 0.25f;
    [SerializeField] private CompendiumView compendiumView;

    private bool missingCompendiumViewWarningShown;
    private Coroutine grimoireBaseFade;

    public void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Open_Close()
    {
        isOpen = !isOpen;
        if (isOpen)
        {
            Time.timeScale = 0;
            if(GameMan.Instance!=null && GameMan.Instance.cc.cameraShake.shake)
            {
                AchievementManager.instance.Achive("Shaky Shaky");
            }

            StartBaseFadeIn();
        }
        else
        {
            StopBaseFade();

            if (compendiumView != null)
            {
                compendiumView.CloseWithFade();
            }
            else if (!missingCompendiumViewWarningShown)
            {
                missingCompendiumViewWarningShown = true;
                Debug.LogWarning($"{name}: Compendium View reference is missing. Assign it in Inspector to fade the grimoire when closing.", this);
            }

            Time.timeScale = 1;
            anim.SetBool("IsOpen", false);
        }
    }

    public void ActivatePanel()
    {
        if (grimoireBase != null)
        {
            grimoireBase.SetActive(true);
        }

        menuPanel.SetActive(true);
    }

    public void DeactivatePanel()
    {
        if (compendiumView == null && menuPanel != null)
        {
            menuPanel.SetActive(false);
        }

        StartBaseFadeOut();
    }

    private void StartBaseFadeIn()
    {
        StopBaseFade();

        if (grimoireBase == null)
        {
            Debug.LogWarning($"{name}: Grimoire Base reference is missing. Assign the new grimoire root in Inspector.", this);
            anim.SetBool("IsOpen", true);
            return;
        }

        grimoireBase.SetActive(true);

        if (grimoireBaseCanvasGroup == null)
        {
            Debug.LogWarning($"{name}: Grimoire Base Canvas Group reference is missing. Assign it in Inspector to enable the base fade.", this);
            anim.SetBool("IsOpen", true);
            return;
        }

        grimoireBaseFade = StartCoroutine(FadeBaseInThenOpen());
    }

    private IEnumerator FadeBaseInThenOpen()
    {
        SetBaseInputEnabled(false);
        yield return FadeBase(0f, 1f);
        SetBaseInputEnabled(true);
        grimoireBaseFade = null;

        if (isOpen)
        {
            anim.SetBool("IsOpen", true);
        }
    }

    private void StartBaseFadeOut()
    {
        StopBaseFade();

        if (grimoireBase == null)
        {
            return;
        }

        if (grimoireBaseCanvasGroup == null)
        {
            grimoireBase.SetActive(false);
            return;
        }

        grimoireBaseFade = StartCoroutine(FadeBaseOutThenDisable());
    }

    private IEnumerator FadeBaseOutThenDisable()
    {
        SetBaseInputEnabled(false);
        yield return FadeBase(grimoireBaseCanvasGroup.alpha, 0f);
        grimoireBaseFade = null;
        grimoireBase.SetActive(false);
    }

    private IEnumerator FadeBase(float startAlpha, float endAlpha)
    {
        if (grimoireBaseFadeDuration <= 0f)
        {
            grimoireBaseCanvasGroup.alpha = endAlpha;
            yield break;
        }

        float elapsed = 0f;
        grimoireBaseCanvasGroup.alpha = startAlpha;

        while (elapsed < grimoireBaseFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(elapsed / grimoireBaseFadeDuration);
            grimoireBaseCanvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, progress);
            yield return null;
        }

        grimoireBaseCanvasGroup.alpha = endAlpha;
    }

    private void StopBaseFade()
    {
        if (grimoireBaseFade == null)
        {
            return;
        }

        StopCoroutine(grimoireBaseFade);
        grimoireBaseFade = null;
    }

    private void SetBaseInputEnabled(bool enabled)
    {
        if (grimoireBaseCanvasGroup == null)
        {
            return;
        }

        grimoireBaseCanvasGroup.interactable = enabled;
        grimoireBaseCanvasGroup.blocksRaycasts = enabled;
    }

}
