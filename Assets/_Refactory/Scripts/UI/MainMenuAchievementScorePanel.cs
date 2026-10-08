using System.Collections.Generic;
using InspectorValidation;
using ProgressSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Refactory.UI
{
    public sealed class MainMenuAchievementScorePanel : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField, RequiredInspectorReference] private ProgressService progressService;
        [SerializeField, RequiredInspectorReference] private AchievementDatabase achievementDatabase;
        [SerializeField, RequiredInspectorReference] private RectTransform achievementGrid;
        [SerializeField, RequiredInspectorReference] private GameObject descriptionAchievement;
        [SerializeField, RequiredInspectorReference] private TMP_Text achievementNameText;
        [SerializeField, RequiredInspectorReference] private TMP_Text descriptionAchievementText;
        [SerializeField, RequiredInspectorReference] private TMP_Text unlockedAchievementCounter;

        private readonly List<MainMenuAchievementSlot> slots = new List<MainMenuAchievementSlot>();
        private bool missingSlotWarningShown;

        private void Awake()
        {
            BuildSlotList();
        }

        private void OnEnable()
        {
            if (progressService != null)
            {
                progressService.ProgressChanged += HandleProgressChanged;
                progressService.AchievementUnlocked += HandleAchievementUnlocked;
            }

            Refresh();
        }

        private void OnDisable()
        {
            if (progressService != null)
            {
                progressService.ProgressChanged -= HandleProgressChanged;
                progressService.AchievementUnlocked -= HandleAchievementUnlocked;
            }

            HideDescription();
        }

        private void HandleProgressChanged(PlayerProgress changedProgress)
        {
            Refresh();
        }

        private void HandleAchievementUnlocked(AchievementId achievementId)
        {
            Refresh();
        }

        private void BuildSlotList()
        {
            slots.Clear();
            if (achievementGrid == null)
            {
                Debug.LogError($"{name}: Achievement Grid is missing. Assign it in Inspector.", this);
                return;
            }

            foreach (Transform slotRoot in achievementGrid)
            {
                Button button = slotRoot.GetComponentInChildren<Button>(true);
                Glowing glow = slotRoot.GetComponentInChildren<Glowing>(true);
                Image achievementImage = FindAchievementImage(button);
                if (button == null || glow == null || achievementImage == null)
                {
                    Debug.LogWarning($"{slotRoot.name}: expected AchievementGlow, AchievementButton and AchievementImageOrLock references are missing.", slotRoot);
                    continue;
                }

                MainMenuAchievementSlot slot = slotRoot.GetComponent<MainMenuAchievementSlot>();
                if (slot == null)
                {
                    slot = slotRoot.gameObject.AddComponent<MainMenuAchievementSlot>();
                }

                slot.Configure(button, achievementImage, glow);
                slots.Add(slot);
            }
        }

        private static Image FindAchievementImage(Button button)
        {
            if (button == null)
            {
                return null;
            }

            Image[] images = button.GetComponentsInChildren<Image>(true);
            foreach (Image image in images)
            {
                if (image != button.targetGraphic)
                {
                    return image;
                }
            }

            return null;
        }

        private void Refresh()
        {
            HideDescription();
            if (progressService == null || achievementDatabase == null)
            {
                Debug.LogError($"{name}: Progress Service and Achievement Database must be assigned to populate the achievement panel.", this);
                return;
            }

            if (slots.Count == 0)
            {
                BuildSlotList();
            }

            IReadOnlyList<AchievementDatabase.AchievementDefinition> achievements = achievementDatabase.Achievements;
            int unlockedCount = 0;
            int displayedAchievementCount = 0;
            for (int index = 0; index < achievements.Count; index++)
            {
                AchievementDatabase.AchievementDefinition definition = achievements[index];
                if (definition == null || definition.id == AchievementId.None)
                {
                    continue;
                }

                bool isUnlocked = progressService.IsAchievementUnlocked(definition.id);
                if (isUnlocked)
                {
                    unlockedCount++;
                }

                if (displayedAchievementCount >= slots.Count)
                {
                    WarnMissingSlots();
                    continue;
                }

                slots[displayedAchievementCount].Bind(this, definition, isUnlocked);
                displayedAchievementCount++;
            }

            for (int index = displayedAchievementCount; index < slots.Count; index++)
            {
                slots[index].gameObject.SetActive(false);
            }

            if (unlockedAchievementCounter != null)
            {
                unlockedAchievementCounter.text = $"{unlockedCount}/{displayedAchievementCount}";
            }
        }

        public void ShowDescription(AchievementDatabase.AchievementDefinition definition)
        {
            if (definition == null || descriptionAchievement == null)
            {
                return;
            }

            achievementNameText.text = definition.displayName;
            descriptionAchievementText.text = definition.description;
            descriptionAchievement.SetActive(true);
        }

        public void HideDescription()
        {
            if (descriptionAchievement != null)
            {
                descriptionAchievement.SetActive(false);
            }
        }

        private void WarnMissingSlots()
        {
            if (missingSlotWarningShown)
            {
                return;
            }

            missingSlotWarningShown = true;
            Debug.LogWarning($"{name}: Achievement Grid has fewer slots than the Achievement Database. Add slots to display every achievement.", this);
        }
    }

    public sealed class MainMenuAchievementSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private MainMenuAchievementScorePanel panel;
        private AchievementDatabase.AchievementDefinition definition;
        private Image icon;
        private Button button;
        private Glowing glow;
        private Sprite lockedSprite;
        private Color lockedColor;
        private bool lockedPresentationCached;
        private bool isUnlocked;

        public void Configure(Button achievementButton, Image achievementImage, Glowing achievementGlow)
        {
            button = achievementButton;
            icon = achievementImage;
            glow = achievementGlow;
            glow.SetGlowActive(false);
        }

        public void Bind(
            MainMenuAchievementScorePanel owner,
            AchievementDatabase.AchievementDefinition achievementDefinition,
            bool unlocked)
        {
            panel = owner;
            definition = achievementDefinition;
            isUnlocked = unlocked;

            if (icon != null)
            {
                CacheLockedPresentation();
                icon.sprite = isUnlocked ? definition.icon : lockedSprite;
                icon.enabled = icon.sprite != null;
                icon.color = isUnlocked ? Color.white : lockedColor;
            }

            if (button != null)
            {
                button.interactable = isUnlocked;
            }

            if (glow != null)
            {
                glow.SetGlowActive(isUnlocked);
            }

            gameObject.SetActive(true);
        }

        private void CacheLockedPresentation()
        {
            if (lockedPresentationCached)
            {
                return;
            }

            lockedPresentationCached = true;
            lockedSprite = icon.sprite;
            lockedColor = icon.color;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isUnlocked)
            {
                panel.ShowDescription(definition);
            }
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            panel.HideDescription();
        }
    }
}
