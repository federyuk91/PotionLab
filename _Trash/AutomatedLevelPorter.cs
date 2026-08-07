using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Refactory.EditorTools
{
    public static class AutomatedLevelPorter
    {
        private const string TemplateScenePath = "Assets/_Refactory/Scene/LevelLayout_ref.unity";
        private const string OutputFolder = "Assets/_Refactory/Scene/AutomatedPorting";
        private const string IsPuzzleModeProperty = "isPuzzleMode";
        private const string BestHealthScoreProperty = "bestHealthScore";
        private const string StartingLightIntensityProperty = "startingLightIntensity";
        private const string DecayLightOverTimeProperty = "decayLightOverTime";
        private const string LegacyLightIntensityProperty = "lightIntensity";
        private const string LegacyBestHealthScoreProperty = "bestHealthScore";
        private static readonly Regex LegacyLevelRegex = new Regex(
            @"Level\s+(?<level>\d+)(\s|_|$)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private static readonly LevelPortRequest[] FirstPortRequests =
        {
            new LevelPortRequest(
                2,
                "Assets/_Project/Scenes/All Level/Marco Level v2/Level 2 v2.unity"),
            new LevelPortRequest(
                3,
                "Assets/_Project/Scenes/All Level/Marco Level v2/Level 3 v2.unity"),
            new LevelPortRequest(
                4,
                "Assets/_Project/Scenes/All Level/Marco Level v2/Level 4 v2.unity")
        };

        [MenuItem("Tools/Refactory/Port First 3 Legacy Levels")]
        public static void PortFirstThreeLevels()
        {
            EnsureOutputFolder();

            foreach (LevelPortRequest request in FirstPortRequests)
            {
                PortLevel(request);
            }

            AssetDatabase.Refresh();
            Debug.Log("AutomatedLevelPorter: first 3 legacy levels ported.");
        }

        [MenuItem("Tools/Refactory/Port Legacy Levels 4-25")]
        public static void PortLegacyLevelsFourToTwentyFive()
        {
            EnsureOutputFolder();

            List<LevelPortRequest> requests = GetEnabledLegacyLevelRequests(4, 25);
            foreach (LevelPortRequest request in requests)
            {
                PortLevel(request);
            }

            AssetDatabase.Refresh();
            Debug.Log($"AutomatedLevelPorter: legacy levels 4-25 ported. Levels processed: {requests.Count}.");
        }

        private static List<LevelPortRequest> GetEnabledLegacyLevelRequests(int minLevel, int maxLevel)
        {
            List<LevelPortRequest> requests = new List<LevelPortRequest>();
            EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;

            foreach (EditorBuildSettingsScene buildScene in buildScenes)
            {
                if (!buildScene.enabled)
                {
                    continue;
                }

                Match match = LegacyLevelRegex.Match(buildScene.path);
                if (!match.Success)
                {
                    continue;
                }

                int levelIndex;
                if (!int.TryParse(match.Groups["level"].Value, out levelIndex))
                {
                    continue;
                }

                if (levelIndex < minLevel || levelIndex > maxLevel)
                {
                    continue;
                }

                requests.Add(new LevelPortRequest(levelIndex, buildScene.path));
            }

            requests.Sort(CompareByLevelIndex);
            return requests;
        }

        private static int CompareByLevelIndex(LevelPortRequest first, LevelPortRequest second)
        {
            return first.LevelIndex.CompareTo(second.LevelIndex);
        }

        private static void PortLevel(LevelPortRequest request)
        {
            Scene targetScene = EditorSceneManager.OpenScene(TemplateScenePath, OpenSceneMode.Single);
            Scene sourceScene = EditorSceneManager.OpenScene(request.SourceScenePath, OpenSceneMode.Additive);

            GameObject gameplayParent = FindObjectInScene(targetScene, "Puzzle - GamePlay Elements");
            GameObject scenarioParent = FindObjectInScene(targetScene, "Environment - Scenario only elements");

            if (gameplayParent == null || scenarioParent == null)
            {
                throw new InvalidOperationException(
                    $"AutomatedLevelPorter: template scene '{TemplateScenePath}' is missing expected container objects.");
            }

            List<GameObject> sourceRoots = new List<GameObject>(sourceScene.GetRootGameObjects());
            LevelPortStats stats = new LevelPortStats();

            foreach (GameObject root in sourceRoots)
            {
                PortObjectRecursive(root, targetScene, gameplayParent.transform, scenarioParent.transform, stats);
            }

            ApplyLevelSettings(sourceScene, targetScene, request, stats);

            string outputPath = $"{OutputFolder}/Level {request.LevelIndex} refactored.unity";
            EditorSceneManager.SaveScene(targetScene, outputPath);
            EditorSceneManager.CloseScene(sourceScene, true);

            Debug.Log(
                $"AutomatedLevelPorter: Level {request.LevelIndex} saved to {outputPath}. " +
                $"Gameplay: {stats.GameplayObjects}, Scenario: {stats.ScenarioObjects}, Excluded: {stats.ExcludedObjects}.");
        }

        private static void ApplyLevelSettings(
            Scene sourceScene,
            Scene targetScene,
            LevelPortRequest request,
            LevelPortStats stats)
        {
            LegacyLevelSettings legacySettings = ReadLegacyLevelSettings(sourceScene, request);
            LevelSettings targetLevelSettings = FindComponentInScene<LevelSettings>(targetScene);

            if (targetLevelSettings == null)
            {
                Debug.LogWarning(
                    $"AutomatedLevelPorter: Level {request.LevelIndex} target scene has no LevelSettings. " +
                    "Assign LevelSettings in the template scene.",
                    null);
                return;
            }

            SerializedObject serializedLevelSettings = new SerializedObject(targetLevelSettings);
            SetBool(serializedLevelSettings, IsPuzzleModeProperty, true);
            SetBool(serializedLevelSettings, DecayLightOverTimeProperty, false);

            if (legacySettings.HasLightIntensity)
            {
                SetInt(serializedLevelSettings, StartingLightIntensityProperty, legacySettings.LightIntensity);
            }
            else
            {
                Debug.LogWarning(
                    $"AutomatedLevelPorter: Level {request.LevelIndex} legacy scene has no readable lightIntensity. " +
                    "Keeping template startingLightIntensity.",
                    null);
            }

            if (legacySettings.HasBestHealthScore)
            {
                SetInt(serializedLevelSettings, BestHealthScoreProperty, legacySettings.BestHealthScore);
            }
            else
            {
                Debug.LogWarning(
                    $"AutomatedLevelPorter: Level {request.LevelIndex} legacy scene has no readable bestHealthScore. " +
                    "Keeping template bestHealthScore.",
                    null);
            }

            serializedLevelSettings.ApplyModifiedPropertiesWithoutUndo();
            stats.LevelSettingsUpdated = true;
        }

        private static LegacyLevelSettings ReadLegacyLevelSettings(Scene sourceScene, LevelPortRequest request)
        {
            LegacyLevelSettings levelSettings = new LegacyLevelSettings();
            Component[] sourceComponents = GetComponentsInScene(sourceScene);

            foreach (Component component in sourceComponents)
            {
                if (component == null || component.GetType().Name != "GameMan")
                {
                    continue;
                }

                SerializedObject serializedGameMan = new SerializedObject(component);
                SerializedProperty lightIntensity = serializedGameMan.FindProperty(LegacyLightIntensityProperty);
                SerializedProperty bestHealthScore = serializedGameMan.FindProperty(LegacyBestHealthScoreProperty);

                if (lightIntensity != null && lightIntensity.propertyType == SerializedPropertyType.Integer)
                {
                    levelSettings.LightIntensity = lightIntensity.intValue;
                    levelSettings.HasLightIntensity = true;
                }

                if (bestHealthScore != null && bestHealthScore.propertyType == SerializedPropertyType.Integer)
                {
                    levelSettings.BestHealthScore = bestHealthScore.intValue;
                    levelSettings.HasBestHealthScore = true;
                }

                return levelSettings;
            }

            Debug.LogWarning(
                $"AutomatedLevelPorter: Level {request.LevelIndex} legacy scene has no GameMan component. " +
                "LevelSettings values were not imported.",
                null);
            return levelSettings;
        }

        private static void PortObjectRecursive(
            GameObject sourceObject,
            Scene targetScene,
            Transform gameplayParent,
            Transform scenarioParent,
            LevelPortStats stats)
        {
            if (sourceObject == null)
            {
                return;
            }

            if (ShouldExclude(sourceObject))
            {
                stats.ExcludedObjects++;
                return;
            }

            if (IsGameplayObject(sourceObject))
            {
                MoveToTargetScene(sourceObject, targetScene, gameplayParent);
                stats.GameplayObjects++;
                return;
            }

            if (HasMeaningfulComponents(sourceObject) && !HasGameplayChildren(sourceObject))
            {
                MoveToTargetScene(sourceObject, targetScene, scenarioParent);
                stats.ScenarioObjects++;
                return;
            }

            List<GameObject> children = GetChildren(sourceObject.transform);
            foreach (GameObject child in children)
            {
                PortObjectRecursive(child, targetScene, gameplayParent, scenarioParent, stats);
            }

            if (HasMeaningfulComponents(sourceObject))
            {
                MoveToTargetScene(sourceObject, targetScene, scenarioParent);
                stats.ScenarioObjects++;
            }
        }

        private static void MoveToTargetScene(GameObject sourceObject, Scene targetScene, Transform parent)
        {
            GameObject copiedObject = UnityEngine.Object.Instantiate(sourceObject);
            copiedObject.name = sourceObject.name;
            copiedObject.transform.SetPositionAndRotation(sourceObject.transform.position, sourceObject.transform.rotation);
            copiedObject.transform.localScale = sourceObject.transform.lossyScale;

            SceneManager.MoveGameObjectToScene(copiedObject, targetScene);
            copiedObject.transform.SetParent(parent, true);
        }

        private static bool ShouldExclude(GameObject sourceObject)
        {
            string objectName = sourceObject.name;

            if (string.Equals(objectName, "Point Light 2D", StringComparison.OrdinalIgnoreCase)
                || string.Equals(objectName, "Global Light 2D", StringComparison.OrdinalIgnoreCase)
                || string.Equals(objectName, "Floor", StringComparison.OrdinalIgnoreCase)
                || string.Equals(objectName, "Character", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return IsSlidingPlatform(sourceObject)
                && sourceObject.transform.position.y >= -1f
                && sourceObject.transform.position.y <= 1f;
        }

        private static bool IsGameplayObject(GameObject sourceObject)
        {
            string objectName = sourceObject.name;

            return objectName.IndexOf("Potion", StringComparison.OrdinalIgnoreCase) >= 0
                || IsSlidingPlatform(sourceObject)
                || objectName.IndexOf("Leva", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Lever", StringComparison.OrdinalIgnoreCase) >= 0
                || objectName.IndexOf("Drop", StringComparison.OrdinalIgnoreCase) >= 0
                || sourceObject.CompareTag("Drop");
        }

        private static bool IsSlidingPlatform(GameObject sourceObject)
        {
            return sourceObject.name.IndexOf("Sliding Platform", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasGameplayChildren(GameObject sourceObject)
        {
            Transform sourceTransform = sourceObject.transform;

            for (int index = 0; index < sourceTransform.childCount; index++)
            {
                GameObject child = sourceTransform.GetChild(index).gameObject;
                if (!ShouldExclude(child) && (IsGameplayObject(child) || HasGameplayChildren(child)))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasMeaningfulComponents(GameObject sourceObject)
        {
            Component[] components = sourceObject.GetComponents<Component>();
            foreach (Component component in components)
            {
                if (component != null && component is not Transform)
                {
                    return true;
                }
            }

            return false;
        }

        private static List<GameObject> GetChildren(Transform parent)
        {
            List<GameObject> children = new List<GameObject>();

            for (int index = 0; index < parent.childCount; index++)
            {
                children.Add(parent.GetChild(index).gameObject);
            }

            return children;
        }

        private static GameObject FindObjectInScene(Scene scene, string objectName)
        {
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                Transform foundTransform = FindChildRecursive(root.transform, objectName);
                if (foundTransform != null)
                {
                    return foundTransform.gameObject;
                }
            }

            return null;
        }

        private static T FindComponentInScene<T>(Scene scene) where T : Component
        {
            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                T component = root.GetComponentInChildren<T>(true);
                if (component != null)
                {
                    return component;
                }
            }

            return null;
        }

        private static Component[] GetComponentsInScene(Scene scene)
        {
            List<Component> components = new List<Component>();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                components.AddRange(root.GetComponentsInChildren<Component>(true));
            }

            return components.ToArray();
        }

        private static void SetInt(SerializedObject serializedObject, string propertyName, int value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.Integer)
            {
                property.intValue = value;
            }
        }

        private static void SetBool(SerializedObject serializedObject, string propertyName, bool value)
        {
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            if (property != null && property.propertyType == SerializedPropertyType.Boolean)
            {
                property.boolValue = value;
            }
        }

        private static Transform FindChildRecursive(Transform root, string objectName)
        {
            if (root.name == objectName)
            {
                return root;
            }

            for (int index = 0; index < root.childCount; index++)
            {
                Transform foundTransform = FindChildRecursive(root.GetChild(index), objectName);
                if (foundTransform != null)
                {
                    return foundTransform;
                }
            }

            return null;
        }

        private static void EnsureOutputFolder()
        {
            string absoluteOutputFolder = Path.Combine(Directory.GetCurrentDirectory(), OutputFolder);
            Directory.CreateDirectory(absoluteOutputFolder);
        }

        private readonly struct LevelPortRequest
        {
            public readonly int LevelIndex;
            public readonly string SourceScenePath;

            public LevelPortRequest(int levelIndex, string sourceScenePath)
            {
                LevelIndex = levelIndex;
                SourceScenePath = sourceScenePath;
            }
        }

        private sealed class LevelPortStats
        {
            public int GameplayObjects;
            public int ScenarioObjects;
            public int ExcludedObjects;
            public bool LevelSettingsUpdated;
        }

        private struct LegacyLevelSettings
        {
            public bool HasLightIntensity;
            public bool HasBestHealthScore;
            public int LightIntensity;
            public int BestHealthScore;
        }
    }
}
