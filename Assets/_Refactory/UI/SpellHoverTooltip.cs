using CharacterSystem;
using InspectorValidation;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Refactory.UI
{
    public sealed class SpellHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        private const string TitleColor = "#FFD34D";

        [SerializeField, RequiredInspectorReference] private GameObject tooltipRoot;
        [SerializeField, RequiredInspectorReference] private TMP_Text tooltipText;
        [SerializeField, RequiredInspectorReference] private GameObject grimoireRoot;
        [SerializeField, Min(0f)] private float hoverDelay = 1f;
        [SerializeField, Min(0f)] private float verticalPadding = 10f;
        [SerializeField, Min(1f)] private float minimumHeight = 64f;
        [SerializeField, Min(1f)] private float maximumHeight = 260f;
        private Spell spell;
        private bool available;
        private bool hovered;
        private float elapsed;

        private void Awake()
        {
            if (tooltipRoot == null || tooltipText == null || grimoireRoot == null)
                Debug.LogError($"{name}: assign Tooltip Root, Tooltip Text and Grimoire Root in SpellHoverTooltip.", this);
            Hide();
        }

        public void Bind(Spell value, bool isAvailable)
        {
            if (spell != value || available != isAvailable) Hide();
            spell = value;
            available = isAvailable;
        }

        private void Update()
        {
            if (!GamePreferences.ShowTooltips || !hovered || !available || spell == null || !HasTooltipContent(spell)
                || (grimoireRoot != null && grimoireRoot.activeInHierarchy) || Time.timeScale <= 0f)
            {
                Hide();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < hoverDelay || tooltipRoot == null || tooltipText == null) return;
            if (tooltipRoot.activeSelf) return;
            tooltipText.text = BuildTooltipText(spell);
            ResizeTooltip();
            tooltipRoot.SetActive(true);
        }

        private void ResizeTooltip()
        {
            RectTransform tooltipRect = tooltipRoot.transform as RectTransform;
            if (tooltipRect == null)
            {
                return;
            }

            float availableWidth = Mathf.Max(1f, tooltipText.rectTransform.rect.width);
            float preferredHeight = tooltipText.GetPreferredValues(tooltipText.text, availableWidth, 0f).y;
            float maximum = Mathf.Max(minimumHeight, maximumHeight);
            float targetHeight = Mathf.Clamp(preferredHeight + verticalPadding, minimumHeight, maximum);
            tooltipRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        }

        private static bool HasTooltipContent(Spell value)
        {
            return value != null
                && (!string.IsNullOrWhiteSpace(value.nome)
                    || !string.IsNullOrWhiteSpace(value.descrizioneNormale)
                    || !string.IsNullOrWhiteSpace(value.descrizionePotenziata));
        }

        private static string BuildTooltipText(Spell value)
        {
            StringBuilder text = new StringBuilder();
            string title = string.IsNullOrWhiteSpace(value.nome) ? "Spell" : value.nome.Trim();
            text.Append("<color=").Append(TitleColor).Append("><b>").Append(title).Append("</b></color>");

            if (!string.IsNullOrWhiteSpace(value.descrizioneNormale))
            {
                text.Append("\n<b>Normal</b>\n").Append(value.descrizioneNormale.Trim());
            }

            if (!string.IsNullOrWhiteSpace(value.descrizionePotenziata))
            {
                text.Append("\n\n<b>Powered</b>\n").Append(value.descrizionePotenziata.Trim());
            }

            return text.ToString();
        }

        public void OnPointerEnter(PointerEventData eventData) { hovered = true; Hide(); }
        public void OnPointerExit(PointerEventData eventData) { hovered = false; Hide(); }
        public void OnPointerDown(PointerEventData eventData) { Hide(); }
        private void OnDisable() { hovered = false; Hide(); }
        private void Hide()
        {
            elapsed = 0f;
            if (tooltipRoot != null) tooltipRoot.SetActive(false);
        }
    }
}
