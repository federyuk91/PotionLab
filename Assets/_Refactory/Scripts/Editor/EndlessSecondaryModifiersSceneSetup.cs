#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class EndlessSecondaryModifiersSceneSetup
{
    private const string MainMenuScenePath = "Assets/_Refactory/Scene/Main Menu.unity";
    private const float FirstOptionY = 120f;
    private const float OptionSpacing = 48f;
    private const float OptionScale = 1.25f;

    private sealed class ModifierSetup
    {
        public string FieldName;
        public string ObjectName;
        public string Label;
        public string Multiplier;
        public string Tooltip;
    }

    private static readonly ModifierSetup[] ModifierSetups =
    {
        new ModifierSetup
        {
            FieldName = "fadingLightToggle",
            ObjectName = "FADING LIGHT Toggle",
            Label = "FADING LIGHT",
            Multiplier = "x1.2",
            Tooltip = "The light fades 20% faster during the run. Score multiplier: x1.2."
        },
        new ModifierSetup
        {
            FieldName = "nightFallToggle",
            ObjectName = "NIGHT FALL Toggle",
            Label = "NIGHT FALL",
            Multiplier = "x1.2",
            Tooltip = "Begin the run in complete darkness, with Light at 0. Score multiplier: x1.2."
        },
        new ModifierSetup
        {
            FieldName = "expensiveMagicToggle",
            ObjectName = "EXPENSIVE MAGIC Toggle",
            Label = "EXPENSIVE MAGIC",
            Multiplier = "x1.5",
            Tooltip = "Every spell costs 1 additional MP. Score multiplier: x1.5."
        },
        new ModifierSetup
        {
            FieldName = "transformationFatigueToggle",
            ObjectName = "TRANSFORMATION FATIGUE Toggle",
            Label = "TRANSFORMATION FATIGUE",
            Multiplier = "x1.5",
            Tooltip = "Every transformation into another form consumes 2 MP. Score multiplier: x1.5."
        },
        new ModifierSetup
        {
            FieldName = "lingeringEffectsToggle",
            ObjectName = "LINGERING EFFECTS Toggle",
            Label = "LINGERING EFFECTS",
            Multiplier = "x1.25",
            Tooltip = "Active status effects trigger 25% more often. Score multiplier: x1.25."
        }
    };

    [MenuItem("Tools/Potion Lab/Setup Endless Secondary Modifiers")]
    public static void SetupFromMenu()
    {
        SetupScene(true);
    }

    private static void SetupScene(bool logCompletion)
    {
        Scene scene = SceneManager.GetSceneByPath(MainMenuScenePath);
        bool openedForSetup = !scene.IsValid() || !scene.isLoaded;
        if (openedForSetup)
        {
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
        }

        EndlessModifiersMenuController controller = FindController(scene);
        if (controller == null)
        {
            Debug.LogError($"Endless modifier setup: no EndlessModifiersMenuController found in {MainMenuScenePath}.");
            CloseSetupSceneIfNeeded(scene, openedForSetup);
            return;
        }

        SerializedObject controllerObject = new SerializedObject(controller);
        if (AreAllSecondaryTogglesAssigned(controllerObject))
        {
            CloseSetupSceneIfNeeded(scene, openedForSetup);
            return;
        }

        SerializedProperty flawlessProperty = controllerObject.FindProperty("flawlessToggle");
        Toggle flawlessToggle = flawlessProperty != null ? flawlessProperty.objectReferenceValue as Toggle : null;
        if (flawlessToggle == null)
        {
            Debug.LogError("Endless modifier setup: assign the Flawless Toggle before generating secondary modifiers.", controller);
            CloseSetupSceneIfNeeded(scene, openedForSetup);
            return;
        }

        RectTransform content = flawlessToggle.transform.parent as RectTransform;
        RectTransform flawlessRect = flawlessToggle.transform as RectTransform;
        if (content == null || flawlessRect == null)
        {
            Debug.LogError("Endless modifier setup: Flawless Toggle must be under a UI RectTransform.", controller);
            CloseSetupSceneIfNeeded(scene, openedForSetup);
            return;
        }

        ConfigureRect(flawlessRect, FirstOptionY);
        for (int index = 0; index < ModifierSetups.Length; index++)
        {
            ModifierSetup setup = ModifierSetups[index];
            Toggle toggle = FindDirectChildToggle(content, setup.ObjectName);
            if (toggle == null)
            {
                GameObject clone = UnityEngine.Object.Instantiate(flawlessToggle.gameObject, content);
                clone.name = setup.ObjectName;
                toggle = clone.GetComponent<Toggle>();
            }

            ConfigureToggle(toggle, controller, setup, FirstOptionY - OptionSpacing * (index + 1));
            SerializedProperty toggleProperty = controllerObject.FindProperty(setup.FieldName);
            if (toggleProperty != null)
            {
                toggleProperty.objectReferenceValue = toggle;
            }
        }

        content.sizeDelta = new Vector2(Mathf.Max(content.sizeDelta.x, 180f), Mathf.Max(content.sizeDelta.y, 310f));
        SerializedProperty secondaryPanelProperty = controllerObject.FindProperty("secondaryModifiersPanel");
        GameObject secondaryPanel = secondaryPanelProperty != null
            ? secondaryPanelProperty.objectReferenceValue as GameObject
            : null;
        RectTransform panelRect = secondaryPanel != null ? secondaryPanel.transform as RectTransform : null;
        if (panelRect != null)
        {
            panelRect.sizeDelta = new Vector2(Mathf.Max(panelRect.sizeDelta.x, 210f), Mathf.Max(panelRect.sizeDelta.y, 370f));
            EditorUtility.SetDirty(panelRect);
        }

        controllerObject.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(content);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (logCompletion)
        {
            Debug.Log("Endless secondary modifier Toggles created and assigned in Main Menu.", controller);
        }

        CloseSetupSceneIfNeeded(scene, openedForSetup);
    }

    private static EndlessModifiersMenuController FindController(Scene scene)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int index = 0; index < roots.Length; index++)
        {
            EndlessModifiersMenuController controller = roots[index].GetComponentInChildren<EndlessModifiersMenuController>(true);
            if (controller != null)
            {
                return controller;
            }
        }

        return null;
    }

    private static bool AreAllSecondaryTogglesAssigned(SerializedObject controllerObject)
    {
        for (int index = 0; index < ModifierSetups.Length; index++)
        {
            SerializedProperty property = controllerObject.FindProperty(ModifierSetups[index].FieldName);
            if (property == null || property.objectReferenceValue == null)
            {
                return false;
            }
        }

        return true;
    }

    private static Toggle FindDirectChildToggle(RectTransform parent, string objectName)
    {
        for (int index = 0; index < parent.childCount; index++)
        {
            Transform child = parent.GetChild(index);
            if (child.name == objectName)
            {
                return child.GetComponent<Toggle>();
            }
        }

        return null;
    }

    private static void ConfigureToggle(
        Toggle toggle,
        EndlessModifiersMenuController controller,
        ModifierSetup setup,
        float anchoredY)
    {
        if (toggle == null)
        {
            return;
        }

        toggle.SetIsOnWithoutNotify(false);
        toggle.group = null;
        RectTransform rect = toggle.transform as RectTransform;
        ConfigureRect(rect, anchoredY);

        TMP_Text[] texts = toggle.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text titleText = null;
        for (int index = 0; index < texts.Length; index++)
        {
            TMP_Text text = texts[index];
            if (text.gameObject.name == "Modifier Name")
            {
                titleText = text;
                text.text = setup.Label;
                text.rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, 110f);
            }
            else if (text.gameObject.name == "Score Multiplier")
            {
                text.text = setup.Multiplier;
            }

            EditorUtility.SetDirty(text);
        }

        EndlessModifierOptionView optionView = toggle.GetComponent<EndlessModifierOptionView>();
        if (optionView != null)
        {
            SerializedObject optionObject = new SerializedObject(optionView);
            optionObject.FindProperty("toggle").objectReferenceValue = toggle;
            optionObject.FindProperty("visualRoot").objectReferenceValue = rect;
            optionObject.FindProperty("titleText").objectReferenceValue = titleText;
            optionObject.FindProperty("menuController").objectReferenceValue = controller;
            optionObject.FindProperty("tooltipTitle").stringValue = setup.Label;
            optionObject.FindProperty("tooltipDescription").stringValue = setup.Tooltip;
            optionObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(optionView);
        }

        EditorUtility.SetDirty(toggle);
        if (rect != null)
        {
            EditorUtility.SetDirty(rect);
        }
    }

    private static void ConfigureRect(RectTransform rect, float anchoredY)
    {
        if (rect == null)
        {
            return;
        }

        rect.anchoredPosition = new Vector2(0f, anchoredY);
        rect.localScale = new Vector3(OptionScale, OptionScale, 1f);
        EditorUtility.SetDirty(rect);
    }

    private static void CloseSetupSceneIfNeeded(Scene scene, bool openedForSetup)
    {
        if (openedForSetup && scene.IsValid() && scene.isLoaded)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
