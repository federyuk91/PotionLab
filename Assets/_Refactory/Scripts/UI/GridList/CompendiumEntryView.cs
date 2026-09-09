using System;
using System.Collections;
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
        [SerializeField, RequiredInspectorReference] private Image rowBackground;

        [Header("Text State Colors")]
        [SerializeField] private Color hoverTextColor = Color.white;
        [SerializeField] private Color selectedTextColor = new Color(0.48f, 0.24f, 0.62f, 1f);
        [SerializeField, Min(0f)] private float colorTransitionDuration = 0.18f;
        [SerializeField, Min(0f)] private float letterColorDelay = 0.018f;

        private GridListEntryData entry;
        private Action<CompendiumEntryView, GridListEntryData> selected;
        private Color normalTitleColor;
        private Color normalDescriptionColor;
        private bool isHovered;
        private bool isSelected;
        private Coroutine textColorRoutine;
        private bool cursorIllumination;
        private bool iconOnly;
        private float animationTime;

        public void UsePotionPresentation()
        {
            if (titleText == null || shortDescription == null || iconImage == null || button == null)
            {
                Debug.LogWarning($"{name}: assign Title Text, Short Description, Icon Image and Button on the compendium entry template.", this);
                return;
            }
            iconOnly = true;
            UseCursorIllumination();
            titleText.gameObject.SetActive(false);
            shortDescription.gameObject.SetActive(false);
            if (rowBackground != null && rowBackground != iconImage)
                rowBackground.color = Color.clear;
            else
                Debug.LogWarning($"{name}: assign Row Background on the compendium entry template.", this);

            RectTransform iconRect = iconImage.rectTransform;
            iconRect.SetParent(transform, false);
            iconRect.anchorMin = Vector2.zero;
            iconRect.anchorMax = Vector2.one;
            iconRect.offsetMin = Vector2.one;
            iconRect.offsetMax = -Vector2.one;
            iconRect.localScale = Vector3.one;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = true;
            iconImage.color = Color.white;
            button.targetGraphic = iconImage;
            ColorBlock colors = button.colors;
            colors.colorMultiplier = 1f;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.94f, 0.75f);
            colors.selectedColor = Color.white;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
            animationTime = UnityEngine.Random.value;
        }

        private void Update()
        {
            if (!iconOnly || entry == null || iconImage == null)
                return;
            animationTime += Time.unscaledDeltaTime;
            iconImage.sprite = entry.GetAnimatedSprite(animationTime);
            float targetScale = isSelected ? 1.12f : isHovered ? 1.06f : 1f;
            iconImage.rectTransform.localScale = Vector3.Lerp(iconImage.rectTransform.localScale,
                Vector3.one * targetScale, 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime));
        }

        public void UseCursorIllumination()
        {
            cursorIllumination = true;
            StopTextColorTransition();
        }

        private sealed class TextColorTransitionState
        {
            public TMP_Text Text { get; }
            public Color32[][] StartingColors { get; }
            public Color32 TargetColor { get; }
            public int VisibleCharacterCount { get; }

            public TextColorTransitionState(
                TMP_Text text,
                Color32[][] startingColors,
                Color32 targetColor,
                int visibleCharacterCount)
            {
                Text = text;
                StartingColors = startingColors;
                TargetColor = targetColor;
                VisibleCharacterCount = visibleCharacterCount;
            }
        }

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

            StopTextColorTransition();
            isHovered = false;
        }

        public void Bind(
            GridListEntryData newEntry,
            GridListEntryData lockedEntry,
            bool isUnlocked,
            Action<CompendiumEntryView, GridListEntryData> selectedCallback)
        {
            entry = newEntry != null && isUnlocked ? newEntry : lockedEntry;
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
            if (cursorIllumination)
                return;

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

            StopTextColorTransition();

            if (instant || colorTransitionDuration <= 0f || !isActiveAndEnabled)
            {
                ApplySolidTextColor(titleText, titleColor);
                ApplySolidTextColor(shortDescription, descriptionColor);
                return;
            }

            textColorRoutine = StartCoroutine(AnimateTextColors(titleColor, descriptionColor));
        }

        private IEnumerator AnimateTextColors(Color titleColor, Color descriptionColor)
        {
            TextColorTransitionState titleState = CreateTransitionState(titleText, titleColor);
            TextColorTransitionState descriptionState = CreateTransitionState(shortDescription, descriptionColor);
            int maximumCharacterCount = Mathf.Max(
                titleState != null ? titleState.VisibleCharacterCount : 0,
                descriptionState != null ? descriptionState.VisibleCharacterCount : 0);

            if (maximumCharacterCount == 0)
            {
                textColorRoutine = null;
                yield break;
            }

            float totalDuration = colorTransitionDuration
                + Mathf.Max(0, maximumCharacterCount - 1) * letterColorDelay;
            float elapsedSeconds = 0f;

            while (elapsedSeconds < totalDuration)
            {
                elapsedSeconds += Time.unscaledDeltaTime;
                ApplyLetterWave(titleState, elapsedSeconds);
                ApplyLetterWave(descriptionState, elapsedSeconds);
                yield return null;
            }

            ApplySolidTextColor(titleText, titleColor);
            ApplySolidTextColor(shortDescription, descriptionColor);
            textColorRoutine = null;
        }

        private TextColorTransitionState CreateTransitionState(TMP_Text text, Color targetColor)
        {
            if (text == null || !text.isActiveAndEnabled || string.IsNullOrEmpty(text.text))
            {
                return null;
            }

            // Keep intermediate vertex colors when the pointer reverses the transition.
            if (text.havePropertiesChanged || text.mesh == null)
            {
                text.ForceMeshUpdate();
            }

            if (!HasReadyMesh(text))
            {
                return null;
            }

            TMP_TextInfo textInfo = text.textInfo;
            Color32[][] startingColors = new Color32[textInfo.materialCount][];

            for (int meshIndex = 0; meshIndex < textInfo.materialCount; meshIndex++)
            {
                startingColors[meshIndex] = (Color32[])textInfo.meshInfo[meshIndex].colors32.Clone();
            }

            int visibleCharacterCount = 0;
            for (int characterIndex = 0; characterIndex < textInfo.characterCount; characterIndex++)
            {
                if (textInfo.characterInfo[characterIndex].isVisible)
                {
                    visibleCharacterCount++;
                }
            }

            return new TextColorTransitionState(text, startingColors, targetColor, visibleCharacterCount);
        }

        private void ApplyLetterWave(TextColorTransitionState state, float elapsedSeconds)
        {
            if (state == null || !HasReadyMesh(state.Text))
            {
                return;
            }

            TMP_TextInfo textInfo = state.Text.textInfo;
            int visibleCharacterIndex = 0;

            for (int characterIndex = 0; characterIndex < textInfo.characterCount; characterIndex++)
            {
                TMP_CharacterInfo characterInfo = textInfo.characterInfo[characterIndex];
                if (!characterInfo.isVisible)
                {
                    continue;
                }

                float characterDelay = visibleCharacterIndex * letterColorDelay;
                float progress = Mathf.Clamp01((elapsedSeconds - characterDelay) / colorTransitionDuration);
                float easedProgress = progress * progress * (3f - 2f * progress);
                int materialIndex = characterInfo.materialReferenceIndex;
                int vertexIndex = characterInfo.vertexIndex;
                if (materialIndex >= state.StartingColors.Length)
                {
                    continue;
                }

                Color32[] currentColors = textInfo.meshInfo[materialIndex].colors32;
                Color32[] startingColors = state.StartingColors[materialIndex];
                if (vertexIndex + 3 >= currentColors.Length || vertexIndex + 3 >= startingColors.Length)
                {
                    continue;
                }

                for (int cornerIndex = 0; cornerIndex < 4; cornerIndex++)
                {
                    currentColors[vertexIndex + cornerIndex] = Color32.Lerp(
                        startingColors[vertexIndex + cornerIndex],
                        state.TargetColor,
                        easedProgress);
                }

                visibleCharacterIndex++;
            }

            state.Text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        private void ApplySolidTextColor(TMP_Text text, Color targetColor)
        {
            if (text == null)
            {
                return;
            }

            // Store the final color in TMP so layout rebuilds preserve it, even while hidden.
            text.color = targetColor;
            text.SetVerticesDirty();
        }

        private static bool HasReadyMesh(TMP_Text text)
        {
            if (text == null || !text.isActiveAndEnabled || text.mesh == null)
            {
                return false;
            }

            TMP_TextInfo textInfo = text.textInfo;
            if (textInfo == null || textInfo.characterCount == 0 || textInfo.meshInfo == null)
            {
                return false;
            }

            for (int meshIndex = 0; meshIndex < textInfo.materialCount; meshIndex++)
            {
                if (meshIndex >= textInfo.meshInfo.Length
                    || textInfo.meshInfo[meshIndex].mesh == null
                    || textInfo.meshInfo[meshIndex].colors32 == null)
                {
                    return false;
                }
            }

            return true;
        }

        private void StopTextColorTransition()
        {
            if (textColorRoutine == null)
            {
                return;
            }

            StopCoroutine(textColorRoutine);
            textColorRoutine = null;
        }
    }
}
