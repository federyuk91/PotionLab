using UnityEngine;
using UnityEngine.UI;
using TMPro;
using InspectorValidation;

public class LightUIController : MonoBehaviour
{
    [Header("Source")]
    [SerializeField, RequiredInspectorReference] private LightController lightController;

    [Header("Light UI")]
    [SerializeField] private Image lightTimerBar;
    [SerializeField, RequiredInspectorReference] private TMP_Text lightLevelText;

    private void Awake()
    {
        if (lightController == null || lightLevelText == null)
            Debug.LogWarning($"{name}: assign Light Controller and Light Level Text in LightUIController.", this);
    }

    private void Start()
    {
        // Read again after every source has completed Awake.
        RefreshInitialState();
    }

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
        lightController.LightLevelChanged += RefreshLightLevel;
    }

    private void Unsubscribe()
    {
        if (lightController == null)
        {
            return;
        }

        lightController.LightTimerChanged -= RefreshLightTimer;
        lightController.LightLevelChanged -= RefreshLightLevel;
    }

    private void RefreshInitialState()
    {
        if (lightController == null)
        {
            return;
        }

        RefreshLightTimer(lightController.LightDecayProgress);
        RefreshLightLevel(lightController.LightIntensity);
    }

    private void RefreshLightLevel(int level)
    {
        if (lightLevelText != null)
            lightLevelText.SetText("{0}", level);
    }

    private void RefreshLightTimer(float progress)
    {
        if (lightTimerBar != null)
        {
            lightTimerBar.fillAmount = 1f - progress;
        }
    }
}
