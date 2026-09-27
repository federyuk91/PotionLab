using System.Collections;
using System.Collections.Generic;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace EndlessSystem
{
    public sealed class EndlessWaveUIController : MonoBehaviour
    {
        [Header("Layout")]
        [SerializeField, RequiredInspectorReference] private RectTransform hudTarget;
        [SerializeField, RequiredInspectorReference] private TMP_Text headerText;
        [SerializeField, RequiredInspectorReference] private TMP_Text drunkPotionsText;
        [SerializeField, RequiredInspectorReference] private Button infoButton;
        [SerializeField, RequiredInspectorReference] private CanvasGroup infoPanel;
        [SerializeField, RequiredInspectorReference] private TMP_Text infoText;

        [Header("Likely Potions")]
        [SerializeField, RequiredInspectorReference] private Image[] potionIcons;
        [SerializeField, RequiredInspectorReference] private TMP_Text[] potionLabels;

        [Header("Panel Animation")]
        [SerializeField, Min(0f)] private float panelFadeDuration = 0.18f;
        [SerializeField, Min(0f)] private float panelSlideDistance = 12f;

        public RectTransform HudTarget => hudTarget;

        private EndlessManager endlessManager;
        private GameManager gameManager;
        private Coroutine panelAnimation;
        private Vector2 panelOpenPosition;
        private int drunkPotionsAtWaveStart;
        private bool gameplayHudVisible;

        private void OnEnable()
        {
            if (infoButton != null)
            {
                infoButton.onClick.AddListener(ToggleInfoPanel);
            }
        }

        private void OnDisable()
        {
            if (infoButton != null)
            {
                infoButton.onClick.RemoveListener(ToggleInfoPanel);
            }

            Unsubscribe();
        }

        public void Configure(EndlessManager assignedEndlessManager, GameManager assignedGameManager)
        {
            Unsubscribe();
            endlessManager = assignedEndlessManager;
            gameManager = assignedGameManager;

            if (endlessManager == null)
            {
                Debug.LogError($"{name}: Endless Manager reference is missing. Assign it through LevelStartUIController.", this);
                return;
            }

            endlessManager.WaveChanged += HandleWaveChanged;
            endlessManager.PhaseProgressChanged += HandlePhaseProgressChanged;

            if (gameManager != null)
            {
                gameManager.PotionRemoved += HandlePotionRemoved;
                drunkPotionsAtWaveStart = Mathf.Max(0, gameManager.potionDrunked);
            }

            RefreshAll();
        }

        public void PrepareIntro()
        {
            gameplayHudVisible = false;
            headerText.alignment = TextAlignmentOptions.Center;
            headerText.text = "ENDLESS";
            drunkPotionsText.gameObject.SetActive(false);
            infoButton.gameObject.SetActive(false);
            SetInfoPanelImmediate(false);
        }

        public void ShowGameplayHud()
        {
            gameplayHudVisible = true;
            headerText.alignment = TextAlignmentOptions.TopLeft;
            drunkPotionsText.gameObject.SetActive(true);
            infoButton.gameObject.SetActive(true);
            RefreshAll();
        }

        public void RefreshWave(int waveNumber)
        {
            if (endlessManager == null)
            {
                return;
            }

            RefreshAll();
        }

        private void HandleWaveChanged(int waveNumber)
        {
            drunkPotionsAtWaveStart = gameManager != null ? Mathf.Max(0, gameManager.potionDrunked) : 0;
            RefreshAll();
        }

        private void HandlePhaseProgressChanged(EndlessPhaseSettings phase, int current, int total)
        {
            RefreshAll();
        }

        private void HandlePotionRemoved(PotionScript potion, bool drunked)
        {
            if (drunked)
            {
                RefreshAll();
            }
        }

        private void RefreshAll()
        {
            if (!gameplayHudVisible || endlessManager == null)
            {
                return;
            }

            EndlessPhaseSettings currentPhase = endlessManager.CurrentPhase;
            if (currentPhase == null)
            {
                return;
            }

            int currentProgress = Mathf.Clamp(
                endlessManager.SpawnedPotionsInCurrentPhase,
                0,
                currentPhase.NextPhaseAfterSpawnedPotions);
            int totalProgress = currentPhase.NextPhaseAfterSpawnedPotions;

            headerText.text =
                $"ENDLESS - Wave: {endlessManager.CurrentWave}\n" +
                $"<size=80%><color=#FFD45A>{currentPhase.DisplayName}</color> " +
                $"<color=#E7DDCA>({currentProgress}/{totalProgress})</color></size>\n" +
                $"<size=70%><color=#5CD1F2>Speed: {FormatSeconds(endlessManager.GetSpawnInterval(currentPhase))}</color></size>";

            int totalDrunk = gameManager != null ? Mathf.Max(0, gameManager.potionDrunked) : 0;
            drunkPotionsText.text = $"POTIONS DRUNK: <color=#61DB73>{totalDrunk}</color>";

            RefreshInfoText(currentPhase, currentProgress, totalProgress);
            RefreshLikelyPotions(currentPhase);
        }

        private void RefreshInfoText(EndlessPhaseSettings currentPhase, int currentProgress, int totalProgress)
        {
            EndlessPhaseSettings nextPhase = endlessManager.GetPhaseAfterCurrent();
            int totalDrunk = gameManager != null ? Mathf.Max(0, gameManager.potionDrunked) : 0;
            int waveDrunk = Mathf.Max(0, totalDrunk - drunkPotionsAtWaveStart);
            string description = string.IsNullOrWhiteSpace(currentPhase.Description)
                ? "An unpredictable mixture of potions crosses the laboratory."
                : currentPhase.Description;
            string nextWaveName = nextPhase != null ? nextPhase.DisplayName : "No next wave";
            string nextEvent = nextPhase != null ? FormatEventCompact(nextPhase) : "Run complete";
            string nextWaveDetails = nextPhase != null
                ? $"{nextPhase.NextPhaseAfterSpawnedPotions} potions · {FormatSpeedTransition(currentPhase, nextPhase)}"
                : string.Empty;

            infoText.text =
                $"<color=#FFD45A>WAVE {endlessManager.CurrentWave} — {currentPhase.DisplayName.ToUpperInvariant()}</color>\n" +
                $"<size=85%>{description}</size>\n\n" +
                "<color=#D9B96E>MOST LIKELY POTIONS</color>\n\n\n\n" +
                "<color=#D9B96E>POTIONS</color>\n" +
                $"Spawned <color=#5CD1F2>{currentProgress}/{totalProgress}</color> · " +
                $"Drunk <color=#61DB73>{waveDrunk}</color> · " +
                $"Total <color=#FFD45A>{totalDrunk}</color>\n\n" +
                "<color=#D9B96E>END OF WAVE</color>\n" +
                $"{FormatEventCompact(currentPhase)}\n\n" +
                "<color=#D9B96E>NEXT WAVE</color>\n" +
                $"<color=#FFD45A>{nextWaveName}</color>" +
                (string.IsNullOrEmpty(nextWaveDetails) ? "\n" : $" · {nextWaveDetails}\n") +
                nextEvent;
        }

        private void RefreshLikelyPotions(EndlessPhaseSettings phase)
        {
            List<EndlessPotionSpawnChance> likelyPotions = phase.GetMostLikelyPotions(3);
            int slotCount = Mathf.Min(potionIcons.Length, potionLabels.Length);

            for (int index = 0; index < slotCount; index++)
            {
                bool hasPotion = index < likelyPotions.Count;
                potionIcons[index].gameObject.SetActive(hasPotion);
                potionLabels[index].gameObject.SetActive(hasPotion);

                if (!hasPotion)
                {
                    continue;
                }

                EndlessPotionSpawnChance potionChance = likelyPotions[index];
                PotionScript potion = potionChance.Potion;
                SpriteRenderer potionRenderer = potion.GetComponentInChildren<SpriteRenderer>(true);
                potionIcons[index].sprite = potionRenderer != null ? potionRenderer.sprite : null;
                potionIcons[index].enabled = potionIcons[index].sprite != null;

                float percentage = phase.GetNormalizedChance(potionChance) * 100f;
                potionLabels[index].text = $"<color=#5CD1F2>{percentage:0.#}%</color>";
            }
        }

        private string FormatSpeedTransition(EndlessPhaseSettings currentPhase, EndlessPhaseSettings nextPhase)
        {
            float currentSeconds = endlessManager.GetSpawnInterval(currentPhase);
            float nextSeconds = endlessManager.GetSpawnInterval(nextPhase);
            if (Mathf.Approximately(currentSeconds, nextSeconds))
            {
                return $"Speed: {FormatSeconds(nextSeconds)}";
            }

            return $"Speed: {FormatSeconds(currentSeconds)} → {FormatSeconds(nextSeconds)}";
        }

        private static string FormatSeconds(float seconds)
        {
            return $"{seconds:0.##} sec";
        }

        private static string FormatEventCompact(EndlessPhaseSettings phase)
        {
            switch (phase.EventType)
            {
                case EndlessEventType.LightVariation:
                    return $"Light → level {Mathf.RoundToInt(phase.EventValue)}";
                case EndlessEventType.Obstacle:
                    return "Random obstacle";
                case EndlessEventType.ChangeSpeedUpperSlider:
                    return $"Upper conveyor → {FormatSignedValue(phase.EventValue)}";
                case EndlessEventType.ChangeSpeedBottomSlider:
                    return $"Lower conveyor → {FormatSignedValue(phase.EventValue)}";
                case EndlessEventType.SpawnFamiliar:
                    return "Familiar spawn";
                case EndlessEventType.Bomb:
                    return "Bomb spawn";
                default:
                    return "No event";
            }
        }

        private static string FormatSignedValue(float value)
        {
            return value > 0f ? $"+{value:0.#}" : value.ToString("0.#");
        }

        private void ToggleInfoPanel()
        {
            bool shouldOpen = !infoPanel.gameObject.activeSelf || infoPanel.alpha < 0.5f;
            if (panelAnimation != null)
            {
                StopCoroutine(panelAnimation);
            }

            panelAnimation = StartCoroutine(AnimateInfoPanel(shouldOpen));
        }

        private IEnumerator AnimateInfoPanel(bool open)
        {
            RectTransform panelRect = infoPanel.transform as RectTransform;
            if (open)
            {
                infoPanel.gameObject.SetActive(true);
                RefreshAll();
            }

            float startAlpha = infoPanel.alpha;
            float targetAlpha = open ? 1f : 0f;
            Vector2 startPosition = panelRect != null ? panelRect.anchoredPosition : Vector2.zero;
            Vector2 targetPosition = open
                ? panelOpenPosition
                : panelOpenPosition + Vector2.up * panelSlideDistance;

            if (panelFadeDuration <= 0f)
            {
                infoPanel.alpha = targetAlpha;
                if (panelRect != null)
                {
                    panelRect.anchoredPosition = targetPosition;
                }
            }
            else
            {
                float elapsed = 0f;
                while (elapsed < panelFadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float progress = Mathf.Clamp01(elapsed / panelFadeDuration);
                    float easedProgress = progress * progress * (3f - 2f * progress);
                    infoPanel.alpha = Mathf.Lerp(startAlpha, targetAlpha, easedProgress);
                    if (panelRect != null)
                    {
                        panelRect.anchoredPosition = Vector2.LerpUnclamped(startPosition, targetPosition, easedProgress);
                    }

                    yield return null;
                }
            }

            infoPanel.alpha = targetAlpha;
            infoPanel.interactable = open;
            infoPanel.blocksRaycasts = open;
            if (panelRect != null)
            {
                panelRect.anchoredPosition = targetPosition;
            }

            if (!open)
            {
                infoPanel.gameObject.SetActive(false);
            }

            panelAnimation = null;
        }

        private void SetInfoPanelImmediate(bool visible)
        {
            if (panelAnimation != null)
            {
                StopCoroutine(panelAnimation);
                panelAnimation = null;
            }

            RectTransform panelRect = infoPanel.transform as RectTransform;
            if (panelRect != null)
            {
                panelOpenPosition = panelRect.anchoredPosition;
            }

            infoPanel.alpha = visible ? 1f : 0f;
            infoPanel.interactable = visible;
            infoPanel.blocksRaycasts = visible;
            infoPanel.gameObject.SetActive(visible);
        }

        private void Unsubscribe()
        {
            if (endlessManager != null)
            {
                endlessManager.WaveChanged -= HandleWaveChanged;
                endlessManager.PhaseProgressChanged -= HandlePhaseProgressChanged;
            }

            if (gameManager != null)
            {
                gameManager.PotionRemoved -= HandlePotionRemoved;
            }
        }
    }
}
