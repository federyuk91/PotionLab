using InspectorValidation;
using UnityEngine;
using UnityEngine.UI;

public sealed class ClassicLevelButtonProgressView : MonoBehaviour
{
    [SerializeField, RequiredInspectorReference] private Button loadButton;
    [SerializeField, RequiredInspectorReference] private Image[] sprites;
    [SerializeField] private Color inactiveColor = new Color(0.3882353f, 0.3137255f, 0.3137255f, 1f);

    private GameObject scorePanel;

    private void Awake()
    {
        ResolveScorePanel();
    }

    public void Refresh(bool unlocked, int score)
    {
        if (!ValidateReferences())
        {
            return;
        }

        loadButton.interactable = unlocked;
        scorePanel.SetActive(unlocked);

        int earnedIconCount = Mathf.Clamp(score, 0, sprites.Length);
        int firstEarnedIconIndex = sprites.Length - earnedIconCount;
        for (int iconIndex = 0; iconIndex < sprites.Length; iconIndex++)
        {
            sprites[iconIndex].color = iconIndex >= firstEarnedIconIndex ? Color.white : inactiveColor;
        }
    }

    private void ResolveScorePanel()
    {
        if (sprites == null || sprites.Length == 0 || sprites[0] == null)
        {
            return;
        }

        Transform panelTransform = sprites[0].transform.parent;
        scorePanel = panelTransform != null ? panelTransform.gameObject : null;
    }

    private bool ValidateReferences()
    {
        bool valid = true;
        if (loadButton == null)
        {
            Debug.LogError($"{name}: assign the Load Button reference on ClassicLevelButtonProgressView.", this);
            valid = false;
        }

        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError($"{name}: assign the Score Icon references on ClassicLevelButtonProgressView.", this);
            return false;
        }

        for (int iconIndex = 0; iconIndex < sprites.Length; iconIndex++)
        {
            if (sprites[iconIndex] != null)
            {
                continue;
            }

            Debug.LogError($"{name}: Score Icon {iconIndex + 1} is missing on ClassicLevelButtonProgressView.", this);
            valid = false;
        }

        if (scorePanel == null)
        {
            ResolveScorePanel();
        }

        if (scorePanel == null)
        {
            Debug.LogError($"{name}: the Score Icons must share a parent Panel.", this);
            valid = false;
        }

        return valid;
    }
}
