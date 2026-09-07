using UnityEngine;
using UnityEngine.UI;

public class LightUIController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private LightController lightController;

    [Header("Light UI")]
    [SerializeField] private Image lightTimerBar;

    private void OnEnable()
    {
        Subscribe();
        RefreshInitialState();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (lightController == null)
        {
            return;
        }

        lightController.LightTimerChanged += RefreshLightTimer;
    }

    private void Unsubscribe()
    {
        if (lightController == null)
        {
            return;
        }

        lightController.LightTimerChanged -= RefreshLightTimer;
    }

    private void RefreshInitialState()
    {
        if (lightController == null)
        {
            return;
        }

        RefreshLightTimer(lightController.LightDecayProgress);
    }

    private void RefreshLightTimer(float progress)
    {
        if (lightTimerBar != null)
        {
            lightTimerBar.fillAmount = 1f - progress;
        }
    }
}
