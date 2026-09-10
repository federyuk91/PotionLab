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
        [Serializable]
        private sealed class SpellFields
        {
            [RequiredInspectorReference] public GameObject root;
            [RequiredInspectorReference] public Image icon;
            [RequiredInspectorReference] public Button button;
        }

        [SerializeField, RequiredInspectorReference] private List<TransformationData> transformations = new List<TransformationData>();
        [SerializeField, RequiredInspectorReference] private ScrollRect listScroll;
        [SerializeField, RequiredInspectorReference] private ScrollRect detailScroll;
        [SerializeField, RequiredInspectorReference] private Button buttonTemplate;
        [SerializeField, RequiredInspectorReference] private TMP_Text title;
        [SerializeField, RequiredInspectorReference] private Image portrait;
        [SerializeField, RequiredInspectorReference] private TMP_Text description;
        [SerializeField, RequiredInspectorReference] private TMP_Text spellCost;
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
        private TransformationData selectedData;
        private int previewSpell = -1;
        private Vector2 descriptionSize;
        private Vector2 descriptionPosition;
        private bool descriptionSizeCached;
        private float idleTime;

        private void Update()
        {
            if (listScroll == null || !listScroll.gameObject.activeInHierarchy) return;
            idleTime += Time.unscaledDeltaTime;
            for (int i = 0; i < buttons.Count; i++)
                ((Image)buttons[i].targetGraphic).sprite = buttonData[i].GetIdleSprite(idleTime + i * 0.13f);
            if (selectedData != null && previewSpell < 0)
                portrait.sprite = selectedData.GetIdleSprite(idleTime);
        }

        public void SetVisible(bool visible)
        {
            if (!visible) previewSpell = -1;
            if (listScroll != null) listScroll.gameObject.SetActive(visible);
            if (detailScroll != null) detailScroll.gameObject.SetActive(visible);
        }

        public void Show()
        {
            if (!ValidateReferences()) return;
            // Show can be called by another component's OnEnable before our Awake.
            if (!descriptionSizeCached)
            {
                descriptionSize = description.rectTransform.sizeDelta;
                descriptionPosition = description.rectTransform.anchoredPosition;
                descriptionSizeCached = true;
            }
            SetVisible(true);
            foreach (Button button in buttons)
            {
                button.gameObject.SetActive(false);
                Destroy(button.gameObject);
            }
            buttons.Clear();
            buttonData.Clear();
            selectedData = null;
            previewSpell = -1;
            detailScroll.content.gameObject.SetActive(false);
            HashSet<CharacterType> ids = new HashSet<CharacterType>();
            TransformationData first = null;
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
                button.onClick.AddListener(() => Select(data, button));
                button.gameObject.SetActive(true);
                buttons.Add(button);
                buttonData.Add(data);
                if (first == null) first = data;
                if (data.Image == null || string.IsNullOrWhiteSpace(data.Description) || data.Spells == null || data.Spells.Count != 3)
                    Debug.LogWarning($"{data.name}: check Image, Description and the three Spells in TransformationData.", data);
            }
            if (first != null) Select(first, buttons[0]);
            ResetScroll(listScroll);
            illumination.RefreshTexts();
        }

        private void Select(TransformationData data, Button selected)
        {
            selectedData = data;
            previewSpell = -1;
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
            foreach (Button button in buttons)
                button.targetGraphic.rectTransform.localScale = button == selected ? Vector3.one * 1.08f : Vector3.one;

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
            title.text = $"{spell.nome} <size=65%>(Cost: {spell.costo})</size>";
            portrait.sprite = spell.icona;
            portrait.gameObject.SetActive(spell.icona != null);
            spellCost.text = $"Cost: {spell.costo}";
            spellCost.gameObject.SetActive(false);
            string text = spell.descrizioneNormale ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(spell.descrizionePotenziata))
                text += "\n\n<b>Powered</b>\n" + spell.descrizionePotenziata;
            // Keep the spell buttons fixed; use the lower page for the full explanation.
            description.rectTransform.anchoredPosition = new Vector2(descriptionPosition.x, -30f);
            description.rectTransform.sizeDelta = new Vector2(descriptionSize.x, 20f);
            SetText(description, "<b>Normal</b>\n" + text);
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
            portrait.gameObject.SetActive(portrait.sprite != null);
            description.rectTransform.sizeDelta = descriptionSize;
            description.rectTransform.anchoredPosition = descriptionPosition;
            SetText(description, selectedData.Description);
            spellCost.gameObject.SetActive(false);
            immunityContainer.gameObject.SetActive(immunityIcons.Count > 0);
            transformationPotionContainer.gameObject.SetActive(transformationIcons.Count > 0);
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
            scroll.verticalNormalizedPosition = 1f;
        }

        private bool ValidateReferences()
        {
            bool valid = listScroll != null && listScroll.content != null && detailScroll != null
                && detailScroll.content != null && buttonTemplate != null && buttonTemplate.targetGraphic is Image
                && title != null && portrait != null && description != null && transformationMethod != null
                && transformationPotionContainer != null
                && spellCost != null && formOnlyFields != null
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
