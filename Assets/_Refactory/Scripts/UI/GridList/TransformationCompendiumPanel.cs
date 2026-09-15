using System;
using System.Collections.Generic;
using CharacterSystem;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI.GridList
{
    public sealed class TransformationCompendiumPanel : MonoBehaviour
    {
        private const float TargetVisibleSpriteExtent = 0.74f;
        private const float MinimumSpriteScale = 0.75f;
        private const float MaximumSpriteScale = 2.6f;

        [Serializable]
        private sealed class SpellFields
        {
            [RequiredInspectorReference] public GameObject root;
            [RequiredInspectorReference] public Image icon;
            [RequiredInspectorReference] public Button button;
        }

        [SerializeField, RequiredInspectorReference] private List<TransformationData> transformations = new List<TransformationData>();
        [SerializeField, RequiredInspectorReference] private ScrollRect listScroll;
        [SerializeField, RequiredInspectorReference] private GridLayoutGroup listLayout;
        [SerializeField, RequiredInspectorReference] private ScrollRect detailScroll;
        [SerializeField, RequiredInspectorReference] private Button buttonTemplate;
        [SerializeField, RequiredInspectorReference] private TMP_Text title;
        [SerializeField, RequiredInspectorReference] private Image portrait;
        [SerializeField, RequiredInspectorReference] private TMP_Text description;
        [SerializeField, RequiredInspectorReference] private TMP_Text spellCost;
        [SerializeField, RequiredInspectorReference] private TMP_Text immunityHeading;
        [SerializeField, RequiredInspectorReference] private GameObject[] formOnlyFields;
        [SerializeField, RequiredInspectorReference] private TMP_Text transformationMethod;
        [SerializeField, RequiredInspectorReference] private RectTransform transformationPotionContainer;
        [SerializeField, RequiredInspectorReference] private TMP_Text cureMethod;
        [SerializeField, RequiredInspectorReference] private RectTransform immunityContainer;
        [SerializeField, RequiredInspectorReference] private Image immunityTemplate;
        [SerializeField, RequiredInspectorReference] private SpellFields[] spellFields;
        [SerializeField, RequiredInspectorReference] private Refactory.UI.GrimoireTextIllumination illumination;

        private readonly List<Button> buttons = new List<Button>();
        private readonly List<Image> immunityIcons = new List<Image>();
        private readonly List<Image> transformationIcons = new List<Image>();
        private readonly List<TransformationData> buttonData = new List<TransformationData>();
        private readonly Dictionary<TransformationData, float> displayScales = new Dictionary<TransformationData, float>();
        private TransformationData selectedData;
        private int previewSpell = -1;
        private float idleTime;

        private void Update()
        {
            if (listScroll == null || !listScroll.gameObject.activeInHierarchy) return;
            idleTime += Time.unscaledDeltaTime;
            for (int i = 0; i < buttons.Count; i++)
            {
                Image buttonImage = (Image)buttons[i].targetGraphic;
                buttonImage.sprite = buttonData[i].GetIdleSprite(idleTime + i * 0.13f);
                ApplyCharacterScale(buttonImage, buttonData[i]);
            }
            if (selectedData != null && previewSpell < 0)
            {
                portrait.sprite = selectedData.GetIdleSprite(idleTime);
                ApplyCharacterScale(portrait, selectedData);
            }
        }

        public void SetVisible(bool visible)
        {
            if (!visible)
            {
                previewSpell = -1;
            }
            if (listScroll != null) listScroll.gameObject.SetActive(visible);
            if (detailScroll != null) detailScroll.gameObject.SetActive(visible);
        }

        public void Show()
        {
            if (!ValidateReferences()) return;
            SetVisible(true);
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
            buttons.Clear();
            buttonData.Clear();
            displayScales.Clear();
            selectedData = null;
            previewSpell = -1;
            detailScroll.content.gameObject.SetActive(false);
            HashSet<CharacterType> ids = new HashSet<CharacterType>();
            foreach (TransformationData data in transformations)
            {
                if (data == null || !ids.Add(data.Id))
                {
                    Debug.LogWarning($"{name}: null or duplicate TransformationData in Transformations; entry skipped.", this);
                    continue;
                }
                Button button = Instantiate(buttonTemplate, listScroll.content);
                button.name = data.TransformationName;
                Image icon = button.targetGraphic as Image;
                icon.sprite = data.GetIdleSprite(idleTime);
                ApplyCharacterScale(icon, data);
                button.onClick.AddListener(() => Select(data, button));
                button.gameObject.SetActive(true);
                buttons.Add(button);
                buttonData.Add(data);
                if (data.Image == null || string.IsNullOrWhiteSpace(data.Description) || data.Spells == null || data.Spells.Count != 3)
                    Debug.LogWarning($"{data.name}: check Image, Description and the three Spells in TransformationData.", data);
            }
            if (buttons.Count > 0) Select(buttonData[0], buttons[0]);
            ResetScroll(listScroll);
            illumination.RefreshTexts();
        }

        private void Select(TransformationData data, Button selectedButton)
        {
            selectedData = data;
            previewSpell = -1;
            LayoutRebuilder.ForceRebuildLayoutImmediate(listScroll.content);
            if (selectedButton != null)
                selectedButton.Select();
            detailScroll.content.gameObject.SetActive(true);
            foreach (Image icon in transformationIcons)
            {
                icon.gameObject.SetActive(false);
                Destroy(icon.gameObject);
            }
            transformationIcons.Clear();
            if (data.TransformationPotionSprites != null)
            {
                foreach (Sprite sprite in data.TransformationPotionSprites)
                {
                    if (sprite == null) continue;
                    Image icon = Instantiate(immunityTemplate, transformationPotionContainer);
                    icon.sprite = sprite;
                    icon.gameObject.SetActive(true);
                    transformationIcons.Add(icon);
                }
            }
            SetText(transformationMethod, transformationIcons.Count > 0 ? data.TransformationHint : data.TransformationMethod);
            SetText(cureMethod, data.CureMethod);

            foreach (Image icon in immunityIcons)
            {
                icon.gameObject.SetActive(false);
                Destroy(icon.gameObject);
            }
            immunityIcons.Clear();
            if (data.ImmunePotionSprites != null)
            {
                foreach (Sprite sprite in data.ImmunePotionSprites)
                {
                    if (sprite == null) continue;
                    Image icon = Instantiate(immunityTemplate, immunityContainer);
                    icon.sprite = sprite;
                    icon.gameObject.SetActive(true);
                    immunityIcons.Add(icon);
                }
            }
            for (int i = 0; i < spellFields.Length; i++)
            {
                SpellFields fields = spellFields[i];
                Spell spell = data.Spells != null && i < data.Spells.Count ? data.Spells[i] : null;
                fields.root.SetActive(spell != null);
                if (spell == null) continue;
                fields.icon.sprite = spell.icona;
                fields.icon.gameObject.SetActive(spell.icona != null);
            }
            ShowFormDetails();
        }

        public void PreviewSpell(int index)
        {
            if (selectedData == null || selectedData.Spells == null || index < 0 || index >= selectedData.Spells.Count) return;
            Spell spell = selectedData.Spells[index];
            if (spell == null) return;
            previewSpell = index;
            foreach (GameObject field in formOnlyFields) field.SetActive(false);
            title.text = spell.nome;
            portrait.sprite = spell.icona;
            portrait.rectTransform.localScale = Vector3.one;
            portrait.gameObject.SetActive(spell.icona != null);
            spellCost.text = $"Cost: {spell.costo} MP";
            spellCost.gameObject.SetActive(true);
            string text = !string.IsNullOrWhiteSpace(spell.descrizioneGenerica)
                ? spell.descrizioneGenerica
                : spell.descrizioneBreve ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(spell.descrizioneNormale))
                text += $"\n\n<size=140%><b>Normal</b></size>\n{spell.descrizioneNormale}";
            if (!string.IsNullOrWhiteSpace(spell.descrizionePotenziata))
                text += $"\n\n<size=140%><b>Powered</b></size>\n{spell.descrizionePotenziata}";
            SetText(description, text);
        }

        public void EndSpellPreview(int index)
        {
            if (index != previewSpell) return;
            previewSpell = -1;
            ShowFormDetails();
        }

        private void ShowFormDetails()
        {
            if (selectedData == null) return;
            foreach (GameObject field in formOnlyFields) field.SetActive(true);
            title.text = selectedData.TransformationName;
            portrait.sprite = selectedData.GetIdleSprite(idleTime);
            ApplyCharacterScale(portrait, selectedData);
            portrait.gameObject.SetActive(portrait.sprite != null);
            SetText(description, selectedData.Description);
            spellCost.gameObject.SetActive(false);
            immunityContainer.gameObject.SetActive(immunityIcons.Count > 0);
            transformationPotionContainer.gameObject.SetActive(transformationIcons.Count > 0);
            immunityHeading.gameObject.SetActive(immunityIcons.Count > 0);
        }

        private static void SetText(TMP_Text field, string value)
        {
            field.text = value ?? string.Empty;
            field.gameObject.SetActive(!string.IsNullOrWhiteSpace(value));
        }

        private static void ResetScroll(ScrollRect scroll)
        {
            Canvas.ForceUpdateCanvases();
            LayoutRebuilder.ForceRebuildLayoutImmediate(scroll.content);
            scroll.StopMovement();
            scroll.horizontalNormalizedPosition = 0f;
            scroll.verticalNormalizedPosition = 1f;
        }

        private void ApplyCharacterScale(Image image, TransformationData data)
        {
            if (image == null || data == null) return;
            if (!displayScales.TryGetValue(data, out float scale))
            {
                scale = CalculateCharacterScale(data.Image);
                displayScales.Add(data, scale);
            }
            image.rectTransform.localScale = Vector3.one * scale;
        }

        private static float CalculateCharacterScale(Sprite sprite)
        {
            if (sprite == null || sprite.vertices == null || sprite.vertices.Length == 0)
                return 1f;

            Vector2 minimum = sprite.vertices[0];
            Vector2 maximum = sprite.vertices[0];
            for (int i = 1; i < sprite.vertices.Length; i++)
            {
                minimum = Vector2.Min(minimum, sprite.vertices[i]);
                maximum = Vector2.Max(maximum, sprite.vertices[i]);
            }

            float pixelsPerUnit = Mathf.Max(sprite.pixelsPerUnit, Mathf.Epsilon);
            float fullWidth = sprite.rect.width / pixelsPerUnit;
            float fullHeight = sprite.rect.height / pixelsPerUnit;
            float visibleWidth = maximum.x - minimum.x;
            float visibleHeight = maximum.y - minimum.y;
            float fullExtent = Mathf.Max(fullWidth, fullHeight);
            float visibleExtent = Mathf.Max(visibleWidth, visibleHeight);
            if (fullExtent <= Mathf.Epsilon || visibleExtent <= Mathf.Epsilon)
                return 1f;

            // Images preserve aspect inside a square cell. Comparing the longest visible
            // edge compensates spritesheets whose frames contain different transparent padding.
            float visibleCoverage = visibleExtent / fullExtent;
            float scale = TargetVisibleSpriteExtent / visibleCoverage;
            return Mathf.Clamp(scale, MinimumSpriteScale, MaximumSpriteScale);
        }

        private bool ValidateReferences()
        {
            bool valid = listScroll != null && listScroll.content != null && listLayout != null && detailScroll != null
                && detailScroll.content != null && buttonTemplate != null && buttonTemplate.targetGraphic is Image
                && title != null && portrait != null && description != null && transformationMethod != null
                && transformationPotionContainer != null
                && spellCost != null && formOnlyFields != null
                && immunityHeading != null
                && cureMethod != null && immunityContainer != null && immunityTemplate != null && illumination != null
                && spellFields != null && spellFields.Length == 3 && transformations != null;
            if (spellFields != null)
                foreach (SpellFields fields in spellFields)
                    valid &= fields != null && fields.root != null && fields.icon != null && fields.button != null;
            if (formOnlyFields != null)
                foreach (GameObject field in formOnlyFields) valid &= field != null;
            if (!valid)
                Debug.LogError($"{name}: assign all TransformationCompendiumPanel Inspector references, including three complete Spell Fields and both ScrollRect contents.", this);
            return valid;
        }
    }
}
