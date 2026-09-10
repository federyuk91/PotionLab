using InspectorValidation;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Refactory.UI.GridList
{
    public sealed class TransformationSpellButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
    {
        [SerializeField, RequiredInspectorReference] private TransformationCompendiumPanel panel;
        [SerializeField, Range(0, 2)] private int spellIndex;
        [SerializeField, RequiredInspectorReference] private RectTransform visualRoot;
        private bool highlighted;

        private void Awake()
        {
            if (panel == null) Debug.LogError($"{name}: assign Panel in TransformationSpellButton.", this);
            if (visualRoot == null) Debug.LogError($"{name}: assign Visual Root in TransformationSpellButton.", this);
        }

        private void Update()
        {
            if (visualRoot != null)
                visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, Vector3.one * (highlighted ? 1.2f : 1f),
                    1f - Mathf.Exp(-16f * Time.unscaledDeltaTime));
        }

        private void Highlight(bool active)
        {
            highlighted = active;
            if (active) panel?.PreviewSpell(spellIndex);
            else panel?.EndSpellPreview(spellIndex);
        }

        public void OnPointerEnter(PointerEventData eventData) => Highlight(true);
        public void OnPointerExit(PointerEventData eventData) => Highlight(false);
        public void OnSelect(BaseEventData eventData) => Highlight(true);
        public void OnDeselect(BaseEventData eventData) => Highlight(false);
        private void OnDisable()
        {
            Highlight(false);
            if (visualRoot != null) visualRoot.localScale = Vector3.one;
        }
    }
}
