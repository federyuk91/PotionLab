using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Refactory.UI
{
    public sealed class InfoHoverTooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
    {
        [SerializeField, RequiredInspectorReference] private GameObject tooltipRoot;
        [SerializeField, RequiredInspectorReference] private TMP_Text tooltipText;
        [SerializeField, RequiredInspectorReference] private GameObject grimoireRoot;
        [SerializeField, TextArea(2, 6)] private string description;
        [SerializeField, Min(0f)] private float hoverDelay = 1f;
        private bool hovered;
        private float elapsed;

        private void Awake()
        {
            if (tooltipRoot == null || tooltipText == null || grimoireRoot == null)
                Debug.LogWarning($"{name}: assign Tooltip Root, Tooltip Text and Grimoire Root in InfoHoverTooltip.", this);
            Hide();
        }

        private void Update()
        {
            if (!GamePreferences.ShowTooltips || !hovered || Time.timeScale <= 0f || (grimoireRoot != null && grimoireRoot.activeInHierarchy))
            {
                Hide();
                return;
            }
            elapsed += Time.unscaledDeltaTime;
            if (elapsed < hoverDelay || tooltipRoot == null || tooltipText == null || string.IsNullOrWhiteSpace(description))
                return;
            if (!tooltipRoot.activeSelf)
            {
                tooltipText.text = description;
                tooltipRoot.SetActive(true);
            }
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
