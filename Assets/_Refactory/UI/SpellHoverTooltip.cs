using CharacterSystem;
using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Refactory.UI
{
    public sealed class SpellHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField, RequiredInspectorReference] private GameObject tooltipRoot;
        [SerializeField, RequiredInspectorReference] private TMP_Text tooltipText;
        [SerializeField, RequiredInspectorReference] private GameObject grimoireRoot;
        [SerializeField, Min(0f)] private float hoverDelay = 1f;
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
            if (!hovered || !available || spell == null || string.IsNullOrWhiteSpace(spell.descrizioneBreve)
                || (grimoireRoot != null && grimoireRoot.activeInHierarchy) || Time.timeScale <= 0f)
            {
                Hide();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < hoverDelay || tooltipRoot == null || tooltipText == null) return;
            tooltipText.text = spell.descrizioneBreve;
            tooltipRoot.SetActive(true);
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
