using Refactory.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI.GridList.Editor
{
    // Editor-only repair: creates serialized controls, never builds UI during gameplay.
    [InitializeOnLoad]
    public static class GameOptionsPrefabSetup
    {
        private const string PrefabPath = "Assets/_Refactory/Prefabs/_RapidLevels/UI Controller - Canvas.prefab";
        private const string SessionKey = "PotionLab.OptionsPrefabRepair.v1";
        private static readonly Color Ink = new Color(0.24f, 0.17f, 0.1f);
        private static readonly Color Gold = new Color(0.65f, 0.43f, 0.14f);
        private static readonly Color Paper = new Color(0.72f, 0.63f, 0.45f);

        static GameOptionsPrefabSetup()
        {
            if (!SessionState.GetBool(SessionKey, false)) EditorApplication.update += RepairWhenReady;
        }

        private static void RepairWhenReady()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating) return;
            EditorApplication.update -= RepairWhenReady;
            if (Install()) SessionState.SetBool(SessionKey, true);
        }

        [MenuItem("Tools/The Good Night Potion/Repair Grimoire Options")]
        public static void Repair()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                Debug.LogWarning("Exit Play Mode before repairing Grimoire Options.");
                return;
            }
            Install();
        }

        private static bool Install()
        {
            PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
            bool useOpenStage = stage != null && stage.assetPath == PrefabPath;
            GameObject root = useOpenStage ? stage.prefabContentsRoot : PrefabUtility.LoadPrefabContents(PrefabPath);
            try
            {
                CompendiumView view = root.GetComponentInChildren<CompendiumView>(true);
                if (view == null) throw new System.InvalidOperationException("CompendiumView is missing from the UI prefab.");
                SerializedObject data = new SerializedObject(view);
                RectTransform leftParent = (RectTransform)data.FindProperty("pageLeft").objectReferenceValue;
                RectTransform rightParent = (RectTransform)data.FindProperty("pageRight").objectReferenceValue;
                TMP_Text reference = (TMP_Text)data.FindProperty("detailDescriptionText").objectReferenceValue;
                if (leftParent == null || rightParent == null || reference == null)
                    throw new System.InvalidOperationException("Assign Page Left, Page Right and Detail Description Text before repairing Options.");

                GameObject leftObject = (GameObject)data.FindProperty("optionsLeftPage").objectReferenceValue;
                GameObject rightObject = (GameObject)data.FindProperty("optionsRightPage").objectReferenceValue;
                if (leftObject == null && rightObject == null)
                {
                    RectTransform left = Rect("Options - Audio", leftParent, 0, 0, 50, 62, true);
                    RectTransform right = Rect("Options - Gameplay", rightParent, 0, 0, 53, 62, true);
                    left.gameObject.SetActive(false);
                    right.gameObject.SetActive(false);
                    Text(left, reference, "Audio", 0, -3, 44, 5, 2.4f, TextAlignmentOptions.Center);
                    Text(right, reference, "Options", 0, -3, 46, 5, 2.4f, TextAlignmentOptions.Center);
                    Slider master = Volume(left, reference, "Master Volume", -13, out TMP_Text masterValue);
                    Slider music = Volume(left, reference, "Music Volume", -27, out TMP_Text musicValue);
                    Slider effects = Volume(left, reference, "Sound Effects", -41, out TMP_Text effectsValue);
                    Text(left, reference, "Changes are saved automatically.", 0, -57, 44, 4, 1.2f, TextAlignmentOptions.Center);
                    Toggle fullscreen = Checkbox(right, reference, "Fullscreen", -13);
                    Toggle tooltips = Checkbox(right, reference, "Show Tooltips", -22);
                    Toggle dialogs = Checkbox(right, reference, "Mage Dialogs", -31);
                    Slider speed = Volume(right, reference, "Text Speed", -41, out TMP_Text speedValue);
                    speed.minValue = 0.5f;
                    speed.maxValue = 2f;
                    speed.value = 1f;
                    speedValue.text = "1.0x";
                    RectTransform resetRoot = Rect("Reset to Defaults", right, 0, -55, 38, 5);
                    Image resetImage = Image(resetRoot, Paper, true);
                    Button reset = resetRoot.gameObject.AddComponent<Button>();
                    reset.targetGraphic = resetImage;
                    Text(resetRoot, reference, "Reset to Defaults", 0, -2.5f, 36, 5, 1.5f, TextAlignmentOptions.Center);
                    GameOptionsPanel panel = left.gameObject.AddComponent<GameOptionsPanel>();
                    SerializedObject panelData = new SerializedObject(panel);
                    Assign(panelData, "masterVolume", master); Assign(panelData, "musicVolume", music);
                    Assign(panelData, "effectsVolume", effects); Assign(panelData, "textSpeed", speed);
                    Assign(panelData, "fullscreen", fullscreen); Assign(panelData, "tooltips", tooltips);
                    Assign(panelData, "mageDialogs", dialogs); Assign(panelData, "resetButton", reset);
                    Assign(panelData, "masterValue", masterValue); Assign(panelData, "musicValue", musicValue);
                    Assign(panelData, "effectsValue", effectsValue); Assign(panelData, "speedValue", speedValue);
                    panelData.ApplyModifiedPropertiesWithoutUndo();
                    Assign(data, "optionsLeftPage", left.gameObject);
                    Assign(data, "optionsRightPage", right.gameObject);
                }
                else if (leftObject == null || rightObject == null)
                    throw new System.InvalidOperationException("Only one Options page is assigned. Repair its Inspector reference before rebuilding to avoid duplicate controls.");

                data.FindProperty("startingCategory").intValue = (int)GridListCategoryType.Home;
                data.ApplyModifiedPropertiesWithoutUndo();
                foreach (Button button in root.GetComponentsInChildren<Button>(true))
                {
                    bool home = button.name == "Tab Menu" || button.name == "Tab Home";
                    bool options = button.name == "Tab Option" || button.name == "Tab Options";
                    if (!home && !options) continue;
                    for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                        if (button.onClick.GetPersistentTarget(i) == view) UnityEventTools.RemovePersistentListener(button.onClick, i);
                    if (home) UnityEventTools.AddPersistentListener(button.onClick, view.ShowHome);
                    else UnityEventTools.AddPersistentListener(button.onClick, view.ShowOptions);
                    button.name = home ? "Tab Home" : "Tab Options";
                    EditorUtility.SetDirty(button);
                }
                GameObject homePage = (GameObject)data.FindProperty("rightPageMainMenu").objectReferenceValue;
                if (homePage != null) homePage.name = "RightPage Home";
                EditorUtility.SetDirty(view);
                PrefabUtility.SaveAsPrefabAsset(root, PrefabPath, out bool saved);
                if (!saved) throw new System.InvalidOperationException("Unity did not save the UI prefab.");
                Debug.Log("[Grimoire Options] Saved UI prefab: two Options pages, four sliders, three toggles, Reset, Home and Options tab callbacks.");
                return true;
            }
            catch (System.Exception exception) { Debug.LogException(exception); return false; }
            finally { if (!useOpenStage) PrefabUtility.UnloadPrefabContents(root); }
        }

        private static void Assign(SerializedObject target, string field, Object value) { target.FindProperty(field).objectReferenceValue = value; }

        private static RectTransform Rect(string name, RectTransform parent, float x, float y, float w, float h, bool centered = false)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform));
            obj.layer = parent.gameObject.layer;
            RectTransform rect = (RectTransform)obj.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, centered ? 0.5f : 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, y);
            return rect;
        }

        private static Image Image(RectTransform rect, Color color, bool raycast = false)
        {
            Image image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = raycast;
            return image;
        }

        private static TMP_Text Text(RectTransform parent, TMP_Text reference, string value, float x, float y, float w, float h, float size = 1.6f, TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
        {
            RectTransform rect = Rect(value, parent, x, y, w, h);
            TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.font = reference.font;
            text.fontSharedMaterial = reference.fontSharedMaterial;
            text.fontSize = size;
            text.enableAutoSizing = false;
            text.color = Ink;
            text.alignment = alignment;
            text.raycastTarget = false;
            text.text = value;
            return text;
        }

        private static Slider Volume(RectTransform parent, TMP_Text reference, string name, float y, out TMP_Text value)
        {
            Text(parent, reference, name, -7, y, 30, 4);
            value = Text(parent, reference, "100%", 18, y, 10, 4, 1.5f, TextAlignmentOptions.MidlineRight);
            RectTransform root = Rect(name + " Slider", parent, 0, y - 5, 44, 5);
            Image(root, Color.clear, true);
            Image(Rect("Track", root, 0, -2.5f, 44, 1.2f), Ink);
            RectTransform fill = Rect("Fill", root, 0, -2.5f, 0, 1.2f);
            Image(fill, Gold);
            RectTransform handle = Rect("Handle", root, 0, -2.5f, 2.2f, 3.6f);
            Image handleImage = Image(handle, Paper, true);
            Slider slider = root.gameObject.AddComponent<Slider>();
            slider.fillRect = fill; slider.handleRect = handle; slider.targetGraphic = handleImage;
            slider.minValue = 0f; slider.maxValue = 1f; slider.value = 1f;
            return slider;
        }

        private static Toggle Checkbox(RectTransform parent, TMP_Text reference, string name, float y)
        {
            RectTransform root = Rect(name + " Toggle", parent, 0, y, 44, 5);
            Image background = Image(root, Color.clear, true);
            Text(root, reference, name, -4, -2.5f, 35, 5, 1.5f);
            RectTransform box = Rect("Checkbox", root, 20, -2.5f, 3.6f, 3.6f);
            Image(box, Ink);
            Image check = Image(Rect("Check", box, 0, -1.8f, 2.2f, 2.2f), Gold);
            Toggle toggle = root.gameObject.AddComponent<Toggle>();
            toggle.targetGraphic = background; toggle.graphic = check; toggle.isOn = true;
            return toggle;
        }
    }
}
