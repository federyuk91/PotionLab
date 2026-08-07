using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using InspectorValidation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace InspectorValidation.Editor
{
    public sealed class RequiredInspectorReferenceBuildValidator : IPreprocessBuildWithReport, IPostprocessBuildWithReport
    {
        private static List<MissingInspectorReference> lastBuildMissingReferences = new List<MissingInspectorReference>();

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            lastBuildMissingReferences = RequiredInspectorReferenceScanner.ScanEnabledBuildScenes();
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RequiredInspectorReferenceReporter.LogBuildSummary(lastBuildMissingReferences);
        }
    }

    public static class RequiredInspectorReferenceMenu
    {
        [MenuItem("Tools/Inspector Validation/Validate Build Scenes")]
        public static void ValidateBuildScenes()
        {
            List<MissingInspectorReference> missingReferences = RequiredInspectorReferenceScanner.ScanEnabledBuildScenes();
            RequiredInspectorReferenceReporter.LogManualSummary(missingReferences);
        }
    }

    public static class RequiredInspectorReferenceScanner
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static List<MissingInspectorReference> ScanEnabledBuildScenes()
        {
            List<MissingInspectorReference> missingReferences = new List<MissingInspectorReference>();
            SceneSetup[] previousSceneSetup = EditorSceneManager.GetSceneManagerSetup();

            try
            {
                foreach (EditorBuildSettingsScene buildScene in EditorBuildSettings.scenes)
                {
                    if (buildScene == null || !buildScene.enabled || string.IsNullOrEmpty(buildScene.path))
                    {
                        continue;
                    }

                    Scene scene = EditorSceneManager.OpenScene(buildScene.path, OpenSceneMode.Single);
                    ScanScene(scene, missingReferences);
                }
            }
            finally
            {
                if (previousSceneSetup != null && previousSceneSetup.Length > 0)
                {
                    EditorSceneManager.RestoreSceneManagerSetup(previousSceneSetup);
                }
            }

            return missingReferences;
        }

        private static void ScanScene(Scene scene, List<MissingInspectorReference> missingReferences)
        {
            if (!scene.IsValid())
            {
                return;
            }

            GameObject[] roots = scene.GetRootGameObjects();
            foreach (GameObject root in roots)
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    ScanBehaviour(scene.path, behaviour, missingReferences);
                }
            }
        }

        private static void ScanBehaviour(
            string scenePath,
            MonoBehaviour behaviour,
            List<MissingInspectorReference> missingReferences)
        {
            if (behaviour == null)
            {
                return;
            }

            Type behaviourType = behaviour.GetType();
            FieldInfo[] fields = behaviourType.GetFields(FieldFlags);

            foreach (FieldInfo field in fields)
            {
                RequiredInspectorReferenceAttribute attribute =
                    field.GetCustomAttribute<RequiredInspectorReferenceAttribute>(true);

                if (attribute == null || !IsUnityReferenceField(field))
                {
                    continue;
                }

                object value = field.GetValue(behaviour);
                UnityEngine.Object unityObject = value as UnityEngine.Object;
                if (unityObject != null)
                {
                    continue;
                }

                missingReferences.Add(new MissingInspectorReference(
                    scenePath,
                    GetHierarchyPath(behaviour.transform),
                    behaviourType.Name,
                    field.Name,
                    attribute.Severity,
                    attribute.Message));
            }
        }

        private static bool IsUnityReferenceField(FieldInfo field)
        {
            return typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType);
        }

        private static string GetHierarchyPath(Transform transform)
        {
            if (transform == null)
            {
                return "<Missing GameObject>";
            }

            Stack<string> path = new Stack<string>();
            Transform current = transform;

            while (current != null)
            {
                path.Push(current.name);
                current = current.parent;
            }

            return string.Join("/", path);
        }
    }

    public static class RequiredInspectorReferenceReporter
    {
        private const string Red = "#ff5555";
        private const string Yellow = "#ffd34d";
        private const string Green = "#6ccf6c";

        public static void LogBuildSummary(List<MissingInspectorReference> missingReferences)
        {
            if (missingReferences == null || missingReferences.Count == 0)
            {
                return;
            }

            Debug.LogWarning(BuildMessage(
                BuildTitle("Build completed with missing Inspector references.", Red),
                missingReferences));
        }

        public static void LogManualSummary(List<MissingInspectorReference> missingReferences)
        {
            if (missingReferences == null || missingReferences.Count == 0)
            {
                Debug.Log(BuildTitle("No missing Inspector references found in enabled build scenes.", Green));
                return;
            }

            Debug.LogWarning(BuildMessage(
                BuildTitle("Missing Inspector references found in enabled build scenes.", Yellow),
                missingReferences));
        }

        private static string BuildTitle(string message, string color)
        {
            return $"{ColorBold("INSPECTOR VALIDATION", color)}: {message}";
        }

        private static string BuildMessage(string title, List<MissingInspectorReference> missingReferences)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine("Assign these fields in Inspector to avoid runtime issues or performance fallbacks:");

            string currentScenePath = null;
            foreach (MissingInspectorReference missingReference in missingReferences)
            {
                if (currentScenePath != missingReference.ScenePath)
                {
                    currentScenePath = missingReference.ScenePath;
                    builder.AppendLine();
                    builder.AppendLine(BuildSceneLine(currentScenePath));
                }

                builder.Append("- ");
                builder.Append(missingReference.GameObjectPath);
                builder.Append(" / ");
                builder.Append(ColorBold($"{missingReference.ComponentName}.{missingReference.FieldName}", Yellow));
                builder.Append(" [");
                builder.Append(missingReference.Severity);
                builder.Append("]");

                if (!string.IsNullOrEmpty(missingReference.Message))
                {
                    builder.Append(" - ");
                    builder.Append(missingReference.Message);
                }

                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string BuildSceneLine(string scenePath)
        {
            string sceneName = Path.GetFileName(scenePath);
            if (string.IsNullOrEmpty(sceneName))
            {
                return $"Scene: {ColorBold(scenePath, Yellow)}";
            }

            string directory = scenePath.Substring(0, scenePath.Length - sceneName.Length);
            return $"Scene: {directory}{ColorBold(sceneName, Yellow)}";
        }

        private static string ColorBold(string text, string color)
        {
            return $"<b><color={color}>{text}</color></b>";
        }
    }

    public readonly struct MissingInspectorReference
    {
        public readonly string ScenePath;
        public readonly string GameObjectPath;
        public readonly string ComponentName;
        public readonly string FieldName;
        public readonly RequiredReferenceSeverity Severity;
        public readonly string Message;

        public MissingInspectorReference(
            string scenePath,
            string gameObjectPath,
            string componentName,
            string fieldName,
            RequiredReferenceSeverity severity,
            string message)
        {
            ScenePath = scenePath;
            GameObjectPath = gameObjectPath;
            ComponentName = componentName;
            FieldName = fieldName;
            Severity = severity;
            Message = message;
        }
    }
}
