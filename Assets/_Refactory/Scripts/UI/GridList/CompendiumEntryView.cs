using System;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Refactory.UI.GridList
{
    public class CompendiumEntryView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Required References")]
        [SerializeField, RequiredInspectorReference] private Button button;
        [SerializeField, RequiredInspectorReference] private TMP_Text titleText;
        [SerializeField, RequiredInspectorReference] private TMP_Text shortDescription;
        [SerializeField, RequiredInspectorReference] private Image iconImage;

        [Header("Text State Colors")]
        [SerializeField] private Color hoverTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = new Color(0.48f, 0.24f, 0.62f, 1f);
        [SerializeField, Min(0f)] private float colorTransitionDuration = 0.12f;

        private GridListEntryData entry;
        private Action<CompendiumEntryView, GridListEntryData> selected;
        private Color normalTitleColor;
        private Color normalDescriptionColor;
        private bool isHovered;
        private bool isSelected;

        private void Awake()
        {
            if (button == null)
            {
                Debug.LogWarning($"{name}: Button reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
                button = GetComponent<Button>();
            }

            if (titleText != null)
            {
                normalTitleColor = titleText.color;
            }

            if (shortDescription != null)
            {
                normalDescriptionColor = shortDescription.color;
            }
        }

        private void OnEnable()
        {
            if (button != null)
            {
                button.onClick.AddListener(Select);
            }
        }

        private void OnDisable()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(Select);
            }

            isHovered = false;
        }

        public void Bind(
            GridListEntryData newEntry,
            GridListEntryData lockedEntry,
            Action<CompendiumEntryView, GridListEntryData> selectedCallback)
        {
            entry = newEntry != null && newEntry.UnlockedByDefault ? newEntry : lockedEntry;
            selected = selectedCallback;
            isHovered = false;
            isSelected = false;

            if (entry == null)
            {
                Debug.LogWarning($"{name}: cannot bind compendium entry because entry data is missing.", this);
                gameObject.SetActive(false);
                return;
            }

            if (titleText != null)
            {
                titleText.text = entry.DisplayName;
            }

            if (shortDescription != null)
            {
                bool hasShortDescription = !string.IsNullOrWhiteSpace(entry.ShortDescription);
                shortDescription.text = hasShortDescription ? entry.ShortDescription : string.Empty;
                shortDescription.gameObject.SetActive(hasShortDescription);
            }

            if (iconImage != null)
            {
                iconImage.sprite = entry.Sprite;
                iconImage.enabled = entry.Sprite != null;
            }

            gameObject.SetActive(true);
            RefreshTextColors(true);
        }

        public void SetSelected(bool selectedState)
        {
            if (isSelected == selectedState)
            {
                return;
            }

            isSelected = selectedState;
            RefreshTextColors(false);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
            RefreshTextColors(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
            RefreshTextColors(false);
        }

        private void Select()
        {
            if (entry == null)
            {
                Debug.LogWarning($"{name}: cannot select compendium entry because entry data is missing.", this);
                return;
            }

            selected?.Invoke(this, entry);
        }

        private void RefreshTextColors(bool instant)
        {
            Color titleColor = isSelected
                ? selectedTextColor
                : isHovered
                    ? hoverTextColor
                    : normalTitleColor;
            Color descriptionColor = isSelected
                ? selectedTextColor
                : isHovered
                    ? hoverTextColor
                    : normalDescriptionColor;

            ApplyTextColor(titleText, titleColor, instant);
            ApplyTextColor(shortDescription, descriptionColor, instant);
        }

        private void ApplyTextColor(TMP_Text text, Color targetColor, bool instant)
        {
            if (text == null)
            {
                return;
            }

            if (instant || colorTransitionDuration <= 0f)
            {
                text.color = targetColor;
                return;
            }

            text.CrossFadeColor(targetColor, colorTransitionDuration, true, true);
        }
    }
}
