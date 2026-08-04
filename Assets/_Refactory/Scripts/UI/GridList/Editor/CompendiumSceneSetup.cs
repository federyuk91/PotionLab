using Refactory.UI.GridList;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Refactory.UI.GridList.Editor
{
    public static class CompendiumSceneSetup
    {
        private const string ScenePath = "Assets/_Refactory/Scene/TestingNew.unity";
        private const string DatabasePath = "Assets/_Refactory/Dati/GridList/GridListDatabase.asset";

        [MenuItem("TheGoodNightPotion/Refactory/Setup Compendium In TestingNew")]
        public static void SetupTestingNew()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject grimoire = GameObject.Find("Grimoire");

            if (grimoire == null)
            {
                Debug.LogError("Compendium setup failed: Grimoire GameObject is missing in TestingNew.");
                return;
            }

            Transform leftPage = grimoire.transform.Find("LeftPage");
            Transform rightPage = grimoire.transform.Find("RightPage");

            if (leftPage == null || rightPage == null)
            {
                Debug.LogError("Compendium setup failed: Grimoire must contain LeftPage and RightPage children.");
                return;
            }

            CompendiumView compendiumView = grimoire.GetComponent<CompendiumView>();
            if (compendiumView == null)
            {
                compendiumView = grimoire.AddComponent<CompendiumView>();
            }

            GridListDatabase database = AssetDatabase.LoadAssetAtPath<GridListDatabase>(DatabasePath);
            TMP_Text tabNameText = CreateTabName(leftPage);
            RectTransform scrollRoot = CreateScrollView(leftPage);
            RectTransform entriesContainer = scrollRoot.transform.Find("Viewport/EntriesContent") as RectTransform;
            CompendiumEntryView entryTemplate = CreateEntryTemplate(entriesContainer);
            RectTransform detailsRoot = CreateDetailsView(rightPage, out TMP_Text detailTitle, out TMP_Text detailDescription, out Image detailImage);
            CreateCategoryButtons(grimoire.transform, compendiumView);

            SerializedObject viewObject = new SerializedObject(compendiumView);
            SetObject(viewObject, "database", database);
            SetEnum(viewObject, "startingCategory", GridListCategoryType.Potion);
            SetObject(viewObject, "pageLeft", leftPage.GetComponent<RectTransform>());
            SetObject(viewObject, "pageRight", rightPage.GetComponent<RectTransform>());
            SetEnum(viewObject, "detailsPage", CompendiumPageSide.Right);
            SetObject(viewObject, "scrollViewRoot", scrollRoot);
            SetObject(viewObject, "entriesContainer", entriesContainer);
            SetObject(viewObject, "entryPrefab", entryTemplate);
            SetObject(viewObject, "detailsRoot", detailsRoot);
            SetObject(viewObject, "categoryTitleText", tabNameText);
            SetObject(viewObject, "detailTitleText", detailTitle);
            SetObject(viewObject, "detailDescriptionText", detailDescription);
            SetObject(viewObject, "detailImage", detailImage);
            viewObject.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(compendiumView);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Compendium setup completed in TestingNew.");
        }

        private static RectTransform CreateScrollView(Transform parent)
        {
            GameObject root = GetOrCreateChild(parent, "Compendium Scroll View");
            RectTransform rootRect = EnsureRectTransform(root);
            Stretch(rootRect, new Vector2(2f, 5f), new Vector2(-2f, -15f));

            Image rootImage = EnsureComponent<Image>(root);
            rootImage.color = new Color(1f, 1f, 1f, 0f);

            ScrollRect scrollRect = EnsureComponent<ScrollRect>(root);
            scrollRect.horizontal = false;
            scrollRect.vertical = true;

            GameObject viewport = GetOrCreateChild(root.transform, "Viewport");
            RectTransform viewportRect = EnsureRectTransform(viewport);
            Stretch(viewportRect, Vector2.zero, Vector2.zero);

            Image viewportImage = EnsureComponent<Image>(viewport);
            viewportImage.color = new Color(1f, 1f, 1f, 0.01f);

            Mask mask = EnsureComponent<Mask>(viewport);
            mask.showMaskGraphic = false;

            GameObject content = GetOrCreateChild(viewport.transform, "EntriesContent");
            RectTransform contentRect = EnsureRectTransform(content);
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup layout = EnsureComponent<VerticalLayoutGroup>(content);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.spacing = 1f;
            layout.padding = new RectOffset(1, 1, 1, 1);
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            ContentSizeFitter fitter = EnsureComponent<ContentSizeFitter>(content);
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewportRect;
            scrollRect.content = contentRect;

            return rootRect;
        }

        private static TMP_Text CreateTabName(Transform parent)
        {
            GameObject root = GetOrCreateChild(parent, "Tab Name");
            RectTransform rootRect = EnsureRectTransform(root);
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0.5f);
            rootRect.anchoredPosition = new Vector2(0f, -5f);
            rootRect.sizeDelta = new Vector2(40f, 7f);

            GameObject textObject = GetOrCreateChild(root.transform, "Text");
            RectTransform textRect = EnsureRectTransform(textObject);
            Stretch(textRect, Vector2.zero, Vector2.zero);
            return EnsureText(textObject, "Potions", 3.5f, TextAlignmentOptions.Center);
        }

        private static CompendiumEntryView CreateEntryTemplate(RectTransform parent)
        {
            GameObject root = GetOrCreateChild(parent, "Compendium Entry Template");
            root.SetActive(false);

            RectTransform rootRect = EnsureRectTransform(root);
            rootRect.sizeDelta = new Vector2(0f, 11f);

            Image background = EnsureComponent<Image>(root);
            background.color = new Color(0.18f, 0.12f, 0.08f, 0.55f);

            Button button = EnsureComponent<Button>(root);
            LayoutElement layoutElement = EnsureComponent<LayoutElement>(root);
            layoutElement.preferredHeight = 11f;
            layoutElement.minHeight = 11f;

            GameObject iconObject = GetOrCreateChild(root.transform, "Icon");
            RectTransform iconRect = EnsureRectTransform(iconObject);
            iconRect.anchorMin = new Vector2(0f, 0.5f);
            iconRect.anchorMax = new Vector2(0f, 0.5f);
            iconRect.pivot = new Vector2(0f, 0.5f);
            iconRect.anchoredPosition = new Vector2(2f, 0f);
            iconRect.sizeDelta = new Vector2(6f, 6f);
            Image icon = EnsureComponent<Image>(iconObject);
            icon.preserveAspect = true;

            GameObject titleObject = GetOrCreateChild(root.transform, "Title");
            RectTransform titleRect = EnsureRectTransform(titleObject);
            titleRect.anchorMin = new Vector2(0f, 0.5f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.offsetMin = new Vector2(10f, 0f);
            titleRect.offsetMax = new Vector2(-1f, -1f);
            TMP_Text title = EnsureText(titleObject, string.Empty, 2.5f, TextAlignmentOptions.MidlineLeft);

            GameObject shortDescriptionObject = GetOrCreateChild(root.transform, "Short Description");
            RectTransform shortDescriptionRect = EnsureRectTransform(shortDescriptionObject);
            shortDescriptionRect.anchorMin = new Vector2(0f, 0f);
            shortDescriptionRect.anchorMax = new Vector2(1f, 0.5f);
            shortDescriptionRect.offsetMin = new Vector2(10f, 1f);
            shortDescriptionRect.offsetMax = new Vector2(-1f, 0f);
            TMP_Text shortDescription = EnsureText(shortDescriptionObject, string.Empty, 1.8f, TextAlignmentOptions.MidlineLeft);
            shortDescription.color = new Color(0.22f, 0.16f, 0.1f, 1f);

            CompendiumEntryView entryView = EnsureComponent<CompendiumEntryView>(root);
            SerializedObject entryObject = new SerializedObject(entryView);
            SetObject(entryObject, "button", button);
            SetObject(entryObject, "titleText", title);
            SetObject(entryObject, "shortDescription", shortDescription);
            SetObject(entryObject, "iconImage", icon);
            entryObject.ApplyModifiedPropertiesWithoutUndo();

            return entryView;
        }

        private static RectTransform CreateDetailsView(Transform parent, out TMP_Text detailTitle, out TMP_Text detailDescription, out Image detailImage)
        {
            GameObject root = GetOrCreateChild(parent, "Compendium Details");
            RectTransform rootRect = EnsureRectTransform(root);
            Stretch(rootRect, new Vector2(2f, 5f), new Vector2(-2f, -5f));

            Transform legacyCategoryTitle = root.transform.Find("Category Title");
            if (legacyCategoryTitle != null)
            {
                legacyCategoryTitle.gameObject.SetActive(false);
            }

            GameObject imageObject = GetOrCreateChild(root.transform, "Detail Image");
            RectTransform imageRect = EnsureRectTransform(imageObject);
            imageRect.anchorMin = new Vector2(0.5f, 1f);
            imageRect.anchorMax = new Vector2(0.5f, 1f);
            imageRect.pivot = new Vector2(0.5f, 1f);
            imageRect.anchoredPosition = new Vector2(0f, -10f);
            imageRect.sizeDelta = new Vector2(16f, 16f);
            detailImage = EnsureComponent<Image>(imageObject);
            detailImage.preserveAspect = true;

            GameObject titleObject = GetOrCreateMigratedChild(root.transform, "title", "Detail Title");
            RectTransform titleRect = EnsureRectTransform(titleObject);
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0.5f, 1f);
            titleRect.anchoredPosition = Vector2.zero;
            titleRect.sizeDelta = new Vector2(0f, 8f);
            detailTitle = EnsureText(titleObject, string.Empty, 3f, TextAlignmentOptions.Center);

            imageRect.anchoredPosition = new Vector2(0f, -10f);

            GameObject descriptionObject = GetOrCreateChild(root.transform, "Detail Description");
            RectTransform descriptionRect = EnsureRectTransform(descriptionObject);
            descriptionRect.anchorMin = new Vector2(0f, 0f);
            descriptionRect.anchorMax = new Vector2(1f, 1f);
            descriptionRect.offsetMin = new Vector2(1f, 1f);
            descriptionRect.offsetMax = new Vector2(-1f, -37f);
            detailDescription = EnsureText(descriptionObject, string.Empty, 2.2f, TextAlignmentOptions.TopLeft);

            return rootRect;
        }

        private static void CreateCategoryButtons(Transform parent, CompendiumView compendiumView)
        {
            GameObject root = GetOrCreateChild(parent, "Compendium Tabs");
            RectTransform rootRect = EnsureRectTransform(root);
            rootRect.anchorMin = new Vector2(0.5f, 1f);
            rootRect.anchorMax = new Vector2(0.5f, 1f);
            rootRect.pivot = new Vector2(0.5f, 0f);
            rootRect.anchoredPosition = new Vector2(0f, 1f);
            rootRect.sizeDelta = new Vector2(115f, 9f);

            HorizontalLayoutGroup layout = EnsureComponent<HorizontalLayoutGroup>(root);
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.spacing = 1f;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;

            CreateCategoryButton(root.transform, compendiumView, "Potions", GridListCategoryType.Potion);
            CreateCategoryButton(root.transform, compendiumView, "Nights", GridListCategoryType.Night);
            CreateCategoryButton(root.transform, compendiumView, "Spells", GridListCategoryType.Spell);
            CreateCategoryButton(root.transform, compendiumView, "Familiars", GridListCategoryType.Familiar);
            CreateCategoryButton(root.transform, compendiumView, "Achievements", GridListCategoryType.Achievement);
        }

        private static void CreateCategoryButton(Transform parent, CompendiumView compendiumView, string label, GridListCategoryType categoryType)
        {
            GameObject buttonObject = GetOrCreateChild(parent, $"Tab {label}");
            RectTransform rectTransform = EnsureRectTransform(buttonObject);
            rectTransform.sizeDelta = new Vector2(20f, 8f);

            Image image = EnsureComponent<Image>(buttonObject);
            image.color = new Color(0.18f, 0.12f, 0.08f, 0.8f);

            Button button = EnsureComponent<Button>(buttonObject);
            button.onClick.RemoveAllListeners();
            UnityAction<int> action = compendiumView.ShowCategory;
            UnityEventTools.AddIntPersistentListener(button.onClick, action, (int)categoryType);

            GameObject textObject = GetOrCreateChild(buttonObject.transform, "Label");
            RectTransform textRect = EnsureRectTransform(textObject);
            Stretch(textRect, new Vector2(1f, 1f), new Vector2(-1f, -1f));
            EnsureText(textObject, label, 1.8f, TextAlignmentOptions.Center);
        }

        private static TMP_Text EnsureText(GameObject gameObject, string text, float fontSize, TextAlignmentOptions alignment)
        {
            TextMeshProUGUI tmpText = gameObject.GetComponent<TextMeshProUGUI>();
            if (tmpText == null)
            {
                tmpText = gameObject.AddComponent<TextMeshProUGUI>();
            }

            tmpText.text = text;
            tmpText.fontSize = fontSize;
            tmpText.enableAutoSizing = false;
            tmpText.alignment = alignment;
            tmpText.color = new Color(0.12f, 0.08f, 0.04f, 1f);
            tmpText.raycastTarget = false;
            return tmpText;
        }

        private static GameObject GetOrCreateChild(Transform parent, string childName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            GameObject child = new GameObject(childName);
            child.layer = parent.gameObject.layer;
            RectTransform rectTransform = child.AddComponent<RectTransform>();
            rectTransform.SetParent(parent, false);
            return child;
        }

        private static GameObject GetOrCreateMigratedChild(Transform parent, string childName, string legacyChildName)
        {
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                return existing.gameObject;
            }

            Transform legacy = parent.Find(legacyChildName);
            if (legacy != null)
            {
                legacy.name = childName;
                legacy.gameObject.SetActive(true);
                return legacy.gameObject;
            }

            return GetOrCreateChild(parent, childName);
        }

        private static RectTransform EnsureRectTransform(GameObject gameObject)
        {
            RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                return rectTransform;
            }

            Transform oldTransform = gameObject.transform;
            Object.DestroyImmediate(oldTransform);
            return gameObject.AddComponent<RectTransform>();
        }

        private static T EnsureComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component != null)
            {
                return component;
            }

            return gameObject.AddComponent<T>();
        }

        private static void Stretch(RectTransform rectTransform, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void SetObject(SerializedObject serializedObject, string propertyName, Object value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.objectReferenceValue = value;
            }
        }

        private static void SetEnum<T>(SerializedObject serializedObject, string propertyName, T value) where T : System.Enum
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null)
            {
                property.enumValueIndex = System.Convert.ToInt32(value);
            }
        }
    }
}
