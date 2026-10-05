using InspectorValidation;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Toggle))]
public sealed class EndlessModifierOptionView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField, RequiredInspectorReference] private Toggle toggle;
    [SerializeField, RequiredInspectorReference] private RectTransform visualRoot;
    [SerializeField, RequiredInspectorReference] private TMP_Text titleText;
    [SerializeField, RequiredInspectorReference] private EndlessModifiersMenuController menuController;
    [SerializeField] private string tooltipTitle;
    [SerializeField, TextArea] private string tooltipDescription;
    [SerializeField] private Color selectedColor = new Color(1f, 0.78f, 0.24f, 1f);
    [SerializeField] private Color hoverColor = Color.white;
    [SerializeField, Min(1f)] private float hoverScale = 1.08f;
    [SerializeField, Min(1f)] private float selectedScale = 1.04f;
    [SerializeField, Min(1f)] private float responseSpeed = 14f;

    private Vector3 baseScale;
    private Color baseTextColor;
    private bool pointerInside;

    public void Configure(
        EndlessModifiersMenuController controller,
        string label,
        string multiplier,
        string title,
        string description)
    {
        menuController = controller;
        tooltipTitle = title;
        tooltipDescription = description;

        if (titleText != null)
        {
            titleText.text = label;
            titleText.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 110f);
        }

        TMP_Text[] localTexts = GetComponentsInChildren<TMP_Text>(true);
        for (int index = 0; index < localTexts.Length; index++)
        {
            TMP_Text localText = localTexts[index];
            if (localText != null && localText != titleText && localText.gameObject.name == "Score Multiplier")
            {
                localText.text = multiplier;
                break;
            }
        }

        gameObject.name = title + " Toggle";
    }

    public void SetBaseScale(float scale)
    {
        if (visualRoot == null)
        {
            visualRoot = transform as RectTransform;
        }

        if (visualRoot == null)
        {
            return;
        }

        baseScale = new Vector3(scale, scale, visualRoot.localScale.z);
        visualRoot.localScale = baseScale;
        ApplyImmediateVisual();
    }

    private void Awake()
    {
        if (toggle == null)
        {
            toggle = GetComponent<Toggle>();
        }

        if (visualRoot == null)
        {
            visualRoot = transform as RectTransform;
        }

        baseScale = visualRoot != null ? visualRoot.localScale : Vector3.one;
        baseTextColor = titleText != null ? titleText.color : Color.white;
    }

    private void OnEnable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.AddListener(HandleToggleChanged);
        }

        ApplyImmediateVisual();
    }

    private void OnDisable()
    {
        if (toggle != null)
        {
            toggle.onValueChanged.RemoveListener(HandleToggleChanged);
        }

        pointerInside = false;
    }

    private void Update()
    {
        bool selected = toggle != null && toggle.isOn;
        float scaleMultiplier = pointerInside ? hoverScale : selected ? selectedScale : 1f;
        Color targetColor = pointerInside ? hoverColor : selected ? selectedColor : baseTextColor;
        float interpolation = 1f - Mathf.Exp(-responseSpeed * Time.unscaledDeltaTime);

        if (visualRoot != null)
        {
            visualRoot.localScale = Vector3.Lerp(visualRoot.localScale, baseScale * scaleMultiplier, interpolation);
        }

        if (titleText != null)
        {
            titleText.color = Color.Lerp(titleText.color, targetColor, interpolation);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerInside = true;
        ShowTooltip();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerInside = false;
        HideTooltip();
    }

    public void OnSelect(BaseEventData eventData)
    {
        pointerInside = true;
        ShowTooltip();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        pointerInside = false;
        HideTooltip();
    }

    private void HandleToggleChanged(bool active)
    {
        ApplyImmediateVisual();
    }

    private void ApplyImmediateVisual()
    {
        bool selected = toggle != null && toggle.isOn;
        if (visualRoot != null)
        {
            visualRoot.localScale = baseScale * (selected ? selectedScale : 1f);
        }

        if (titleText != null)
        {
            titleText.color = selected ? selectedColor : baseTextColor;
        }
    }

    private void ShowTooltip()
    {
        if (menuController != null)
        {
            menuController.ShowTooltip(tooltipTitle, tooltipDescription);
        }
    }

    private void HideTooltip()
    {
        if (menuController != null)
        {
            menuController.HideTooltip();
        }
    }
}
