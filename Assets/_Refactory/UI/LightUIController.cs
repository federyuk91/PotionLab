using UnityEngine;
using UnityEngine.UI;

public class LightUIController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField] private LightController lightController;

    [Header("Light UI")]
    [SerializeField] private Text lightLevelText;
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

        lightController.LightLevelChanged += RefreshLightLevel;
        lightController.LightTimerChanged += RefreshLightTimer;
    }

    private void Unsubscribe()
    {
        if (lightController == null)
        {
            return;
        }

        lightController.LightLevelChanged -= RefreshLightLevel;
        lightController.LightTimerChanged -= RefreshLightTimer;
    }

    private void RefreshInitialState()
    {
        if (lightController == null)
        {
            return;
        }

        RefreshLightLevel(lightController.LightIntensity);
        RefreshLightTimer(lightController.LightDecayProgress);
    }

    private void RefreshLightLevel(int lightIntensity)
    {
        if (lightLevelText == null)
        {
            return;
        }

        switch (lightIntensity)
        {
            case 0:
                SetLightLevelText("No Magic Power", Color.red);
                break;
            case 1:
                SetLightLevelText("Low Magic Power", Color.green);
                break;
            case 2:
                SetLightLevelText("Medium Magic Power", Color.blue);
                break;
            case 3:
                SetLightLevelText("High Magic Power", Color.yellow);
                break;
            default:
                SetLightLevelText("Unknown Magic Power", Color.white);
                break;
        }
    }

    private void SetLightLevelText(string text, Color color)
    {
        lightLevelText.text = text;
        lightLevelText.color = color;
    }

    private void RefreshLightTimer(float progress)
    {
        if (lightTimerBar != null)
        {
            lightTimerBar.fillAmount = 1f - progress;
        }
    }
}
