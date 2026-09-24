using System;
using System.Collections.Generic;
using CharacterSystem;
using Cinemachine;
using Cinematics;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Timeline;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class IntroCinematicSceneSetup
{
    private const string ScenePath = "Assets/_Refactory/Scene/Intro Cinematic.unity";
    private const string TimelineFolder = "Assets/_Refactory/Timeline/Intro";
    private const string TimelinePath = TimelineFolder + "/Timeline_IntroMage.playable";
    private const string MageControllerPath = "Assets/_Refactory/Animation/_Controller/Mage.controller";
    private const string MageIdleClipPath = "Assets/_Refactory/Animation/T0_Mage/Anim_Mage_Idle.anim";
    private const string MageSpriteSheetPath = "Assets/_Refactory/Arts/Mage/BaseForm/mage_idle.png";
    private const string EnvironmentPrefabPath = "Assets/_Refactory/Prefabs/Environment.prefab";
    private const string BalloonSpritePath = "Assets/_Refactory/Arts/Ui/dialogue.png";
    private const string DialogueFontPath = "Assets/_Refactory/Fonts/TMPFont_PrStart.asset";
    private const string SpriteLitMaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
    private const string BasementBackgroundGuid = "6a84444c92e16c94c87a3306366d9f20";

    [MenuItem("Tools/PotionLab/Create Intro Cinematic Scene")]
    public static void CreateIntroCinematicScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
        {
            bool replaceScene = Application.isBatchMode || EditorUtility.DisplayDialog(
                "Intro Cinematic",
                "Intro Cinematic already exists. Replace the generated scene and its generated Timeline assets?",
                "Replace",
                "Cancel");

            if (!replaceScene)
            {
                return;
            }
        }

        EnsureFolder("Assets/_Refactory", "Timeline");
        EnsureFolder("Assets/_Refactory/Timeline", "Intro");
        DeleteGeneratedAssetIfPresent(TimelinePath);

        Scene previousActiveScene = SceneManager.GetActiveScene();
        NewSceneMode creationMode = previousActiveScene.IsValid() && string.IsNullOrEmpty(previousActiveScene.path)
            ? NewSceneMode.Single
            : NewSceneMode.Additive;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, creationMode);
        SceneManager.SetActiveScene(scene);
        GameObject root = new GameObject("Intro Cinematic");

        Transform environmentRoot = CreateChild(root.transform, "Environment");
        CreateBasementBackground(environmentRoot);

        Transform characterRoot = CreateChild(root.transform, "Character");
        GameObject mage = CreateMage(characterRoot);
        GameObject mageLight = CreateMageLight(characterRoot, mage.transform.localPosition);
        GameObject dialogueRoot = CreateDialogueBalloon(characterRoot, out TMP_Text dialogueText);

        Transform cameraRoot = CreateChild(root.transform, "Cinemachine Cameras");
        Camera mainCamera = CreateMainCamera(cameraRoot, out CinemachineBrain brain);
        CinemachineVirtualCamera wideCamera = CreateVirtualCamera(cameraRoot, "CM Intro Wide", new Vector3(0f, -1.15f, -10f), 5.4f, 20);
        CinemachineVirtualCamera closeCamera = CreateVirtualCamera(cameraRoot, "CM Mage Closeup", new Vector3(0f, -0.85f, -10f), 3.45f, 10);

        GameObject sequenceObject = new GameObject("Intro Sequence");
        sequenceObject.transform.SetParent(root.transform, false);
        PlayableDirector director = sequenceObject.AddComponent<PlayableDirector>();
        TimelineAsset timeline = CreateTimeline(director, brain, mage, mageLight, dialogueRoot, wideCamera, closeCamera);
        director.playableAsset = timeline;
        director.playOnAwake = false;
        director.extrapolationMode = DirectorWrapMode.None;

        IntroCinematicController controller = sequenceObject.AddComponent<IntroCinematicController>();
        SerializedObject serializedController = new SerializedObject(controller);
        serializedController.FindProperty("director").objectReferenceValue = director;
        serializedController.FindProperty("dialogueRoot").objectReferenceValue = dialogueRoot;
        serializedController.FindProperty("dialogueText").objectReferenceValue = dialogueText;
        SerializedProperty dialogueLines = serializedController.FindProperty("dialogueLines");
        dialogueLines.arraySize = 1;
        SerializedProperty firstDialogueLine = dialogueLines.GetArrayElementAtIndex(0);
        firstDialogueLine.FindPropertyRelative("text").stringValue = "Some nights begin with a single spark.";
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        RenderSettings.ambientLight = new Color(0.08f, 0.06f, 0.1f, 1f);
        RenderSettings.fog = false;
        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettingsAtEnd(ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
        {
            SceneManager.SetActiveScene(previousActiveScene);
        }

        EditorSceneManager.CloseScene(scene, true);

        EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath));
        Debug.Log($"Created Intro Cinematic scene at {ScenePath}. It was added at the end of Build Settings so existing level indexes remain unchanged.");
    }

    private static void CreateBasementBackground(Transform parent)
    {
        Sprite backgroundSprite = LoadSpriteFromGuid(BasementBackgroundGuid);

        CreateSpriteObject(
            parent,
            "Basement Background",
            backgroundSprite,
            new Vector3(0.11f, -2f, 0f),
            new Vector3(11.52f, 11.52f, 1f),
            new Color(0.577f, 0.577f, 0.577f, 1f),
            0);

        GameObject globalLightObject = new GameObject("Basement Ambient Light");
        globalLightObject.transform.SetParent(parent, false);
        Light2D globalLight = globalLightObject.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.55f, 0.62f, 0.82f, 1f);
        globalLight.intensity = 0.34f;
    }

    private static GameObject CreateMage(Transform parent)
    {
        GameObject mage = new GameObject("Mage");
        SpriteRenderer spriteRenderer = mage.AddComponent<SpriteRenderer>();
        mage.AddComponent<ShadowCaster2D>();
        Animator animator = mage.AddComponent<Animator>();

        mage.transform.SetParent(parent, false);
        mage.transform.localPosition = new Vector3(0f, -1.65f, 0f);
        mage.transform.localRotation = Quaternion.identity;
        mage.transform.localScale = new Vector3(10f, 10f, 10f);

        Object[] mageAssets = AssetDatabase.LoadAllAssetsAtPath(MageSpriteSheetPath);
        foreach (Object mageAsset in mageAssets)
        {
            Sprite mageSprite = mageAsset as Sprite;
            if (mageSprite != null)
            {
                spriteRenderer.sprite = mageSprite;
                break;
            }
        }

        spriteRenderer.sortingOrder = 10;
        spriteRenderer.color = Color.white;
        spriteRenderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMaterialPath);

        RuntimeAnimatorController mageController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(MageControllerPath);
        animator.runtimeAnimatorController = mageController;
        animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        MageRandomIdleController randomIdleController = mage.AddComponent<MageRandomIdleController>();
        SerializedObject serializedIdleController = new SerializedObject(randomIdleController);
        serializedIdleController.FindProperty("animator").objectReferenceValue = animator;
        serializedIdleController.FindProperty("idleClip").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(MageIdleClipPath);
        serializedIdleController.FindProperty("specialIdleChance").floatValue = 0.15f;
        serializedIdleController.FindProperty("idleVariantCount").intValue = 9;
        serializedIdleController.ApplyModifiedPropertiesWithoutUndo();

        return mage;
    }

    private static GameObject CreateMageLight(Transform parent, Vector3 mageLocalPosition)
    {
        GameObject lightObject = new GameObject("Mage Light");
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = mageLocalPosition + new Vector3(0f, 0.03f, 0f);

        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = Color.white;
        light.intensity = 1f;
        light.pointLightInnerRadius = 0.75f;
        light.pointLightOuterRadius = 1.5f;
        light.falloffIntensity = 0.5f;
        light.volumeIntensity = 0f;
        light.volumetricEnabled = false;
        light.shadowsEnabled = true;
        light.shadowIntensity = 0.5f;
        light.shadowSoftness = 0f;
        light.shadowVolumeIntensity = 0f;
        light.volumetricShadowsEnabled = false;

        SerializedObject serializedLight = new SerializedObject(light);
        SerializedProperty sortingLayers = serializedLight.FindProperty("m_ApplyToSortingLayers");
        sortingLayers.arraySize = 2;
        sortingLayers.GetArrayElementAtIndex(0).intValue = 0;
        sortingLayers.GetArrayElementAtIndex(1).intValue = 756726043;
        serializedLight.ApplyModifiedPropertiesWithoutUndo();

        Animator lightAnimator = lightObject.AddComponent<Animator>();
        lightAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

        GameObject environmentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnvironmentPrefabPath);
        Transform sourceLevelLight = environmentPrefab != null
            ? FindDescendant(environmentPrefab.transform, "LightController")
            : null;
        Transform sourceVisual = sourceLevelLight != null
            ? FindDescendant(sourceLevelLight, "baseLight-Sheet_0")
            : null;

        if (sourceVisual == null)
        {
            Debug.LogError(
                $"Intro Cinematic setup could not find LightController/baseLight-Sheet_0 in {EnvironmentPrefabPath}. " +
                "Assign the refactored Environment prefab before regenerating the scene.");
            return lightObject;
        }

        GameObject visual = Object.Instantiate(sourceVisual.gameObject);
        // Keep the level hierarchy name because AC_MagicLight animation bindings target this path.
        visual.name = sourceVisual.name;
        visual.transform.SetParent(lightObject.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = sourceVisual.localScale;

        Animator[] inheritedAnimators = visual.GetComponentsInChildren<Animator>(true);
        foreach (Animator inheritedAnimator in inheritedAnimators)
        {
            inheritedAnimator.runtimeAnimatorController = null;
        }

        return lightObject;
    }

    private static GameObject CreateDialogueBalloon(Transform parent, out TMP_Text dialogueText)
    {
        GameObject balloon = new GameObject("Dialogue Balloon", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(CanvasGroup));
        balloon.transform.SetParent(parent, false);
        RectTransform balloonTransform = balloon.GetComponent<RectTransform>();
        balloonTransform.localPosition = new Vector3(2.15f, 0.85f, 0f);
        balloonTransform.localScale = Vector3.one * 0.008f;
        balloonTransform.sizeDelta = new Vector2(500f, 250f);

        Canvas canvas = balloon.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = balloon.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.dynamicPixelsPerUnit = 16f;

        GameObject panel = new GameObject("Balloon", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(balloon.transform, false);
        RectTransform panelTransform = panel.GetComponent<RectTransform>();
        panelTransform.anchorMin = Vector2.zero;
        panelTransform.anchorMax = Vector2.one;
        panelTransform.offsetMin = Vector2.zero;
        panelTransform.offsetMax = Vector2.zero;

        Image image = panel.GetComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(BalloonSpritePath);
        image.type = Image.Type.Sliced;
        image.preserveAspect = true;
        image.useSpriteMesh = true;
        image.pixelsPerUnitMultiplier = 0.01f;
        image.color = Color.white;
        image.raycastTarget = false;

        GameObject textObject = new GameObject("Dialogue Text", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(panel.transform, false);
        RectTransform textTransform = textObject.GetComponent<RectTransform>();
        textTransform.anchorMin = Vector2.zero;
        textTransform.anchorMax = Vector2.one;
        textTransform.anchoredPosition = new Vector2(15f, 0f);
        textTransform.offsetMin = new Vector2(105f, 70f);
        textTransform.offsetMax = new Vector2(-105f, -70f);

        dialogueText = textObject.GetComponent<TextMeshProUGUI>();
        dialogueText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DialogueFontPath);
        dialogueText.text = "Some nights begin with a single spark.";
        dialogueText.fontSize = 30f;
        dialogueText.enableAutoSizing = true;
        dialogueText.fontSizeMin = 10f;
        dialogueText.fontSizeMax = 30f;
        dialogueText.color = Color.white;
        dialogueText.alignment = TextAlignmentOptions.Center;
        dialogueText.textWrappingMode = TextWrappingModes.Normal;
        dialogueText.overflowMode = TextOverflowModes.Ellipsis;
        dialogueText.lineSpacing = 20f;
        dialogueText.raycastTarget = false;

        return balloon;
    }

    private static Camera CreateMainCamera(Transform parent, out CinemachineBrain brain)
    {
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = new Vector3(0f, -1.15f, -10f);

        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.025f, 0.015f, 0.035f, 1f);
        camera.nearClipPlane = 0.1f;
        camera.farClipPlane = 100f;
        cameraObject.AddComponent<AudioListener>();
        cameraObject.AddComponent<UniversalAdditionalCameraData>();
        brain = cameraObject.AddComponent<CinemachineBrain>();
        brain.m_DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Style.EaseInOut, 0.75f);

        return camera;
    }

    private static CinemachineVirtualCamera CreateVirtualCamera(
        Transform parent,
        string cameraName,
        Vector3 position,
        float orthographicSize,
        int priority)
    {
        GameObject cameraObject = new GameObject(cameraName);
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.localPosition = position;

        CinemachineVirtualCamera virtualCamera = cameraObject.AddComponent<CinemachineVirtualCamera>();
        virtualCamera.Priority = priority;
        virtualCamera.m_Lens.OrthographicSize = orthographicSize;
        virtualCamera.m_Lens.NearClipPlane = 0.1f;
        virtualCamera.m_Lens.FarClipPlane = 100f;
        return virtualCamera;
    }

    private static TimelineAsset CreateTimeline(
        PlayableDirector director,
        CinemachineBrain brain,
        GameObject mage,
        GameObject mageLight,
        GameObject dialogueRoot,
        CinemachineVirtualCamera wideCamera,
        CinemachineVirtualCamera closeCamera)
    {
        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        timeline.name = "Timeline_IntroMage";
        timeline.durationMode = TimelineAsset.DurationMode.BasedOnClips;
        AssetDatabase.CreateAsset(timeline, TimelinePath);

        CinemachineTrack cameraTrack = timeline.CreateTrack<CinemachineTrack>(null, "Cinemachine Shots");
        CreateCameraShot(director, cameraTrack, wideCamera, 0f, 3f, "Wide Establishing Shot");
        CreateCameraShot(director, cameraTrack, closeCamera, 2.55f, 5.5f, "Mage Closeup");
        CreateCameraShot(director, cameraTrack, wideCamera, 7.65f, 2.35f, "Wide Exit Shot");
        director.SetGenericBinding(cameraTrack, brain);

        AnimationTrack mageTrack = timeline.CreateTrack<AnimationTrack>(null, "Mage Animation");
        director.SetGenericBinding(mageTrack, mage.GetComponent<Animator>());

        AnimationTrack mageLightTrack = timeline.CreateTrack<AnimationTrack>(null, "Mage Light");
        director.SetGenericBinding(mageLightTrack, mageLight.GetComponent<Animator>());

        ActivationTrack dialogueTrack = timeline.CreateTrack<ActivationTrack>(null, "Dialogue Balloon");
        TimelineClip dialogueClip = dialogueTrack.CreateDefaultClip();
        dialogueClip.start = 1.8f;
        dialogueClip.duration = 6.4f;
        dialogueClip.displayName = "Show Dialogue";
        dialogueTrack.postPlaybackState = ActivationTrack.PostPlaybackState.Inactive;
        director.SetGenericBinding(dialogueTrack, dialogueRoot);

        EditorUtility.SetDirty(timeline);
        return timeline;
    }

    private static void CreateCameraShot(
        PlayableDirector director,
        CinemachineTrack track,
        CinemachineVirtualCamera virtualCamera,
        double start,
        double duration,
        string displayName)
    {
        TimelineClip clip = track.CreateDefaultClip();
        clip.start = start;
        clip.duration = duration;
        clip.displayName = displayName;

        CinemachineShot shot = clip.asset as CinemachineShot;
        if (shot == null)
        {
            return;
        }

        PropertyName exposedName = new PropertyName(Guid.NewGuid().ToString("N"));
        shot.VirtualCamera = new ExposedReference<CinemachineVirtualCameraBase>
        {
            exposedName = exposedName
        };
        director.SetReferenceValue(exposedName, virtualCamera);
    }

    private static GameObject CreateSpriteObject(
        Transform parent,
        string objectName,
        Sprite sprite,
        Vector3 localPosition,
        Vector3 localScale,
        Color color,
        int sortingOrder)
    {
        GameObject spriteObject = new GameObject(objectName);
        spriteObject.transform.SetParent(parent, false);
        spriteObject.transform.localPosition = localPosition;
        spriteObject.transform.localScale = localScale;

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteLitMaterialPath);
        return spriteObject;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        return child.transform;
    }

    private static Transform FindDescendant(Transform root, string objectName)
    {
        if (root.name == objectName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform match = FindDescendant(child, objectName);
            if (match != null)
            {
                return match;
            }
        }

        return null;
    }

    private static Sprite LoadSpriteFromGuid(string guid)
    {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void EnsureFolder(string parentFolder, string folderName)
    {
        string folderPath = parentFolder + "/" + folderName;
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(parentFolder, folderName);
        }
    }

    private static void DeleteGeneratedAssetIfPresent(string assetPath)
    {
        if (AssetDatabase.LoadMainAssetAtPath(assetPath) != null)
        {
            AssetDatabase.DeleteAsset(assetPath);
        }
    }

    private static void AddSceneToBuildSettingsAtEnd(string scenePath)
    {
        List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (EditorBuildSettingsScene buildScene in scenes)
        {
            if (string.Equals(buildScene.path, scenePath, StringComparison.OrdinalIgnoreCase))
            {
                return;
            }
        }

        scenes.Add(new EditorBuildSettingsScene(scenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }
}
