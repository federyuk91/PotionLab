using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI
{
    public sealed class GameOptionsPanel : MonoBehaviour
    {
        [SerializeField, RequiredInspectorReference] private Slider masterVolume;
        [SerializeField, RequiredInspectorReference] private Slider musicVolume;
        [SerializeField, RequiredInspectorReference] private Slider effectsVolume;
        [SerializeField, RequiredInspectorReference] private Slider textSpeed;
        [SerializeField, RequiredInspectorReference] private Toggle fullscreen;
        [SerializeField, RequiredInspectorReference] private Toggle tooltips;
        [SerializeField, RequiredInspectorReference] private Toggle mageDialogs;
        [SerializeField, RequiredInspectorReference] private Button resetButton;
        [SerializeField, RequiredInspectorReference] private TMP_Text masterValue;
        [SerializeField, RequiredInspectorReference] private TMP_Text musicValue;
        [SerializeField, RequiredInspectorReference] private TMP_Text effectsValue;
        [SerializeField, RequiredInspectorReference] private TMP_Text speedValue;
        private bool ready;
        private float saveDelay = -1f;

        private void Awake()
        {
            ready = masterVolume != null && musicVolume != null && effectsVolume != null && textSpeed != null
                && fullscreen != null && tooltips != null && mageDialogs != null && resetButton != null
                && masterValue != null && musicValue != null && effectsValue != null && speedValue != null;
            if (!ready) Debug.LogError($"{name}: assign all sliders, toggles, value labels and Reset Button in GameOptionsPanel.", this);
        }

        private void OnEnable()
        {
            if (!ready) return;
            Refresh();
            masterVolume.onValueChanged.AddListener(ChangeVolumes);
            musicVolume.onValueChanged.AddListener(ChangeVolumes);
            effectsVolume.onValueChanged.AddListener(ChangeVolumes);
            textSpeed.onValueChanged.AddListener(GamePreferences.SetTextSpeed);
            fullscreen.onValueChanged.AddListener(GamePreferences.SetFullscreen);
            tooltips.onValueChanged.AddListener(GamePreferences.SetTooltips);
            mageDialogs.onValueChanged.AddListener(GamePreferences.SetMageDialogs);
            resetButton.onClick.AddListener(ResetDefaults);
            GamePreferences.Changed += Changed;
        }

        private void OnDisable()
        {
            if (!ready) return;
            masterVolume.onValueChanged.RemoveListener(ChangeVolumes);
            musicVolume.onValueChanged.RemoveListener(ChangeVolumes);
            effectsVolume.onValueChanged.RemoveListener(ChangeVolumes);
            textSpeed.onValueChanged.RemoveListener(GamePreferences.SetTextSpeed);
            fullscreen.onValueChanged.RemoveListener(GamePreferences.SetFullscreen);
            tooltips.onValueChanged.RemoveListener(GamePreferences.SetTooltips);
            mageDialogs.onValueChanged.RemoveListener(GamePreferences.SetMageDialogs);
            resetButton.onClick.RemoveListener(ResetDefaults);
            GamePreferences.Changed -= Changed;
            GamePreferences.Save();
            saveDelay = -1f;
        }

        private void Update()
        {
            if (saveDelay < 0f) return;
            saveDelay -= Time.unscaledDeltaTime;
            if (saveDelay <= 0f) { GamePreferences.Save(); saveDelay = -1f; }
        }

        private void OnApplicationPause(bool paused) { if (paused) GamePreferences.Save(); }
        private void ChangeVolumes(float unused) { GamePreferences.SetVolumes(masterVolume.value, musicVolume.value, effectsVolume.value); }
        private void Changed() { saveDelay = 0.5f; RefreshValues(); }
        private void ResetDefaults() { GamePreferences.ResetToDefaults(); Refresh(); fullscreen.SetIsOnWithoutNotify(true); }

        private void Refresh()
        {
            masterVolume.SetValueWithoutNotify(GamePreferences.MasterVolume);
            musicVolume.SetValueWithoutNotify(GamePreferences.MusicVolume);
            effectsVolume.SetValueWithoutNotify(GamePreferences.EffectsVolume);
            textSpeed.SetValueWithoutNotify(GamePreferences.TextSpeed);
            fullscreen.SetIsOnWithoutNotify(Screen.fullScreen);
            tooltips.SetIsOnWithoutNotify(GamePreferences.ShowTooltips);
            mageDialogs.SetIsOnWithoutNotify(GamePreferences.ShowMageDialogs);
            RefreshValues();
        }

        private void RefreshValues()
        {
            masterValue.SetText("{0}%", Mathf.RoundToInt(GamePreferences.MasterVolume * 100f));
            musicValue.SetText("{0}%", Mathf.RoundToInt(GamePreferences.MusicVolume * 100f));
            effectsValue.SetText("{0}%", Mathf.RoundToInt(GamePreferences.EffectsVolume * 100f));
            speedValue.SetText("{0:1}x", GamePreferences.TextSpeed);
            textSpeed.interactable = GamePreferences.ShowMageDialogs;
        }
    }
}
