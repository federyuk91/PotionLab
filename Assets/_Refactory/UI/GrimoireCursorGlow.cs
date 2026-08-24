using InspectorValidation;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI
{
    [DisallowMultipleComponent]
    public sealed class GrimoireCursorGlow : MonoBehaviour
    {
        [Header("Required References")]
        [SerializeField, RequiredInspectorReference] private RectTransform movementArea;
        [SerializeField, RequiredInspectorReference] private Canvas rootCanvas;
        [SerializeField, RequiredInspectorReference] private Graphic glowGraphic;

        [Header("Motion")]
        [SerializeField, Min(0f)] private float followSharpness = 24f;
        [SerializeField] private bool hideOutsideMovementArea = true;

        [Header("Light Pulse")]
        [SerializeField, Range(0f, 1f)] private float baseAlpha = 0.22f;
        [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.035f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.5f;

        private RectTransform glowTransform;
        private Color glowColor;

        private void Awake()
        {
            glowTransform = GetComponent<RectTransform>();

            if (glowGraphic != null)
            {
                glowGraphic.raycastTarget = false;
                glowColor = glowGraphic.color;
            }
        }

        private void OnEnable()
        {
            transform.SetAsLastSibling();
            RefreshGlowPosition(true);
        }

        private void LateUpdate()
        {
            RefreshGlowPosition(false);
        }

        private void OnDisable()
        {
            if (glowGraphic != null)
            {
                glowGraphic.enabled = false;
            }
        }

        private void RefreshGlowPosition(bool snapToCursor)
        {
            if (glowTransform == null || movementArea == null || rootCanvas == null || glowGraphic == null)
            {
                return;
            }

            Camera eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : rootCanvas.worldCamera;
            Vector2 pointerPosition = Input.mousePosition;
            bool pointerInside = RectTransformUtility.RectangleContainsScreenPoint(
                movementArea,
                pointerPosition,
                eventCamera);

            if (hideOutsideMovementArea && !pointerInside)
            {
                glowGraphic.enabled = false;
                return;
            }

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    movementArea,
                    pointerPosition,
                    eventCamera,
                    out Vector2 targetPosition))
            {
                glowGraphic.enabled = false;
                return;
            }

            glowGraphic.enabled = true;

            if (snapToCursor || followSharpness <= 0f)
            {
                glowTransform.anchoredPosition = targetPosition;
            }
            else
            {
                float followFactor = 1f - Mathf.Exp(-followSharpness * Time.unscaledDeltaTime);
                glowTransform.anchoredPosition = Vector2.Lerp(
                    glowTransform.anchoredPosition,
                    targetPosition,
                    followFactor);
            }

            float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmplitude;
            glowColor.a = Mathf.Clamp01(baseAlpha + pulse);
            glowGraphic.color = glowColor;
        }
    }
}
