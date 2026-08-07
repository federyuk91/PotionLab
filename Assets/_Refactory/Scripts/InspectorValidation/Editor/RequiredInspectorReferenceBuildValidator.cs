#if UNITY_EDITOR
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
        private static List<AutoCompiledInspectorReference> lastBuildAutoCompiledReferences = new List<AutoCompiledInspectorReference>();

        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            RequiredInspectorReferenceScanResult result = RequiredInspectorReferenceScanner.CompileAndScanEnabledBuildScenes();
            lastBuildMissingReferences = result.MissingReferences;
            lastBuildAutoCompiledReferences = result.AutoCompiledReferences;
        }

        public void OnPostprocessBuild(BuildReport report)
        {
            RequiredInspectorReferenceReporter.LogAutoCompiledSummary(lastBuildAutoCompiledReferences);
            RequiredInspectorReferenceReporter.LogBuildSummary(lastBuildMissingReferences);
        }
    }

    public static class RequiredInspectorReferenceMenu
    {
        [MenuItem("Tools/Inspector Validation/Validate Build Scenes")]
        public static void ValidateBuildScenes()
        {
            RequiredInspectorReferenceScanResult result = RequiredInspectorReferenceScanner.CompileAndScanEnabledBuildScenes();
            RequiredInspectorReferenceReporter.LogAutoCompiledSummary(result.AutoCompiledReferences);
            RequiredInspectorReferenceReporter.LogManualSummary(result.MissingReferences);
        }
    }

    public static class RequiredInspectorReferenceScanner
    {
        private const BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const BindingFlags MethodFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private const string CompileReferenceMethodName = "CompileReference";

        public static List<MissingInspectorReference> ScanEnabledBuildScenes()
        {
            return CompileAndScanEnabledBuildScenes().MissingReferences;
        }

        public static RequiredInspectorReferenceScanResult CompileAndScanEnabledBuildScenes()
        {
            List<MissingInspectorReference> missingReferences = new List<MissingInspectorReference>();
            List<AutoCompiledInspectorReference> autoCompiledReferences = new List<AutoCompiledInspectorReference>();
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
                    bool sceneChanged = ResolveSceneReferences(scene, autoCompiledReferences);
                    if (sceneChanged)
                    {
                        EditorSceneManager.MarkSceneDirty(scene);
                        EditorSceneManager.SaveScene(scene);
                    }

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

            return new RequiredInspectorReferenceScanResult(missingReferences, autoCompiledReferences);
        }

        private static bool ResolveSceneReferences(
            Scene scene,
            List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            bool changed = false;
            List<MonoBehaviour> behavioursWithMissingReferences = new List<MonoBehaviour>();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                MonoBehaviour[] behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (MonoBehaviour behaviour in behaviours)
                {
                    if (TryResolveBehaviourReferences(scene, behaviour, autoCompiledReferences))
                    {
                        changed = true;
                    }

                    if (HasMissingRequiredReferences(behaviour))
                    {
                        behavioursWithMissingReferences.Add(behaviour);
                    }
                }
            }

            foreach (MonoBehaviour behaviour in behavioursWithMissingReferences)
            {
                if (TryCompileReference(scene.path, behaviour, autoCompiledReferences))
                {
                    changed = true;
                }
            }

            return changed;
        }

        private static bool TryResolveBehaviourReferences(
            Scene scene,
            MonoBehaviour behaviour,
            List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            if (behaviour == null)
            {
                return false;
            }

            bool changed = false;
            Type behaviourType = behaviour.GetType();
            FieldInfo[] fields = behaviourType.GetFields(FieldFlags);

            foreach (FieldInfo field in fields)
            {
                RequiredInspectorReferenceAttribute attribute =
                    field.GetCustomAttribute<RequiredInspectorReferenceAttribute>(true);

                if (attribute == null
                    || attribute.ResolveMode == ResolveMode.None
                    || !IsUnityReferenceField(field)
                    || IsReferenceAssigned(behaviour, field))
                {
                    continue;
                }

                UnityEngine.Object resolvedReference = ResolveReference(scene, behaviour, field, attribute.ResolveMode);
                if (resolvedReference == null)
                {
                    continue;
                }

                field.SetValue(behaviour, resolvedReference);
                changed = true;
                autoCompiledReferences.Add(new AutoCompiledInspectorReference(
                    scene.path,
                    GetHierarchyPath(behaviour.transform),
                    behaviourType.Name,
                    field.Name,
                    attribute.ResolveMode.ToString()));
            }

            return changed;
        }

        private static UnityEngine.Object ResolveReference(
            Scene scene,
            MonoBehaviour behaviour,
            FieldInfo field,
            ResolveMode resolveMode)
        {
            switch (resolveMode)
            {
                case ResolveMode.Local:
                    return ResolveLocalReference(behaviour, field);
                case ResolveMode.SceneSingleton:
                    return ResolveSceneSingletonReference(scene, behaviour, field);
                default:
                    return null;
            }
        }

        private static UnityEngine.Object ResolveLocalReference(MonoBehaviour behaviour, FieldInfo field)
        {
            if (!typeof(Component).IsAssignableFrom(field.FieldType))
            {
                return null;
            }

            return behaviour.GetComponent(field.FieldType);
        }

        private static UnityEngine.Object ResolveSceneSingletonReference(
            Scene scene,
            MonoBehaviour behaviour,
            FieldInfo field)
        {
            if (!typeof(Component).IsAssignableFrom(field.FieldType))
            {
                return null;
            }

            List<UnityEngine.Object> matches = new List<UnityEngine.Object>();
            GameObject[] roots = scene.GetRootGameObjects();

            foreach (GameObject root in roots)
            {
                Component[] components = root.GetComponentsInChildren(field.FieldType, true);
                foreach (Component component in components)
                {
                    matches.Add(component);
                }
            }

            if (matches.Count == 1)
            {
                return matches[0];
            }

            if (matches.Count > 1)
            {
                Debug.LogWarning(
                    RequiredInspectorReferenceReporter.BuildTitle(
                        $"SceneSingleton reference was not assigned because it is ambiguous: {scene.path} / {GetHierarchyPath(behaviour.transform)} / {behaviour.GetType().Name}.{field.Name}. Found {matches.Count} instances of {field.FieldType.Name}.",
                        RequiredInspectorReferenceReporter.Yellow),
                    behaviour);
            }

            return null;
        }

        private static bool TryCompileReference(
            string scenePath,
            MonoBehaviour behaviour,
            List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            if (behaviour == null)
            {
                return false;
            }

            List<string> missingFieldsBeforeCompile = GetMissingRequiredFieldNames(behaviour);
            if (missingFieldsBeforeCompile.Count == 0)
            {
                return false;
            }

            if (!InvokeCompileReference(scenePath, behaviour))
            {
                return false;
            }

            bool resolvedAnyReference = false;
            List<string> missingFieldsAfterCompile = GetMissingRequiredFieldNames(behaviour);

            foreach (string fieldName in missingFieldsBeforeCompile)
            {
                if (missingFieldsAfterCompile.Contains(fieldName))
                {
                    continue;
                }

                resolvedAnyReference = true;
                autoCompiledReferences.Add(new AutoCompiledInspectorReference(
                    scenePath,
                    GetHierarchyPath(behaviour.transform),
                    behaviour.GetType().Name,
                    fieldName,
                    CompileReferenceMethodName));
            }

            return resolvedAnyReference;
        }

        private static bool InvokeCompileReference(string scenePath, MonoBehaviour behaviour)
        {
            MethodInfo method = behaviour.GetType().GetMethod(CompileReferenceMethodName, MethodFlags);
            if (method == null || method.GetParameters().Length > 0)
            {
                return false;
            }

            try
            {
                object result = method.Invoke(behaviour, null);
                if (method.ReturnType == typeof(bool))
                {
                    return result is bool changed && changed;
                }

                return method.ReturnType == typeof(void);
            }
            catch (Exception exception)
            {
                Debug.LogWarning(
                    RequiredInspectorReferenceReporter.BuildTitle(
                        $"CompileReference failed on {scenePath} / {GetHierarchyPath(behaviour.transform)} / {behaviour.GetType().Name}: {exception.InnerException?.Message ?? exception.Message}",
                        RequiredInspectorReferenceReporter.Yellow),
                    behaviour);
                return false;
            }
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

                if (attribute == null || !IsUnityReferenceField(field) || IsReferenceAssigned(behaviour, field))
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

        private static bool HasMissingRequiredReferences(MonoBehaviour behaviour)
        {
            return GetMissingRequiredFieldNames(behaviour).Count > 0;
        }

        private static List<string> GetMissingRequiredFieldNames(MonoBehaviour behaviour)
        {
            List<string> missingFields = new List<string>();

            if (behaviour == null)
            {
                return missingFields;
            }

            FieldInfo[] fields = behaviour.GetType().GetFields(FieldFlags);
            foreach (FieldInfo field in fields)
            {
                RequiredInspectorReferenceAttribute attribute =
                    field.GetCustomAttribute<RequiredInspectorReferenceAttribute>(true);

                if (attribute != null && IsUnityReferenceField(field) && !IsReferenceAssigned(behaviour, field))
                {
                    missingFields.Add(field.Name);
                }
            }

            return missingFields;
        }

        private static bool IsReferenceAssigned(MonoBehaviour behaviour, FieldInfo field)
        {
            object value = field.GetValue(behaviour);
            UnityEngine.Object unityObject = value as UnityEngine.Object;
            return unityObject != null;
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
        public const string Red = "#ff5555";
        public const string Yellow = "#ffd34d";
        public const string Green = "#6ccf6c";

        public static void LogAutoCompiledSummary(List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            if (autoCompiledReferences == null || autoCompiledReferences.Count == 0)
            {
                return;
            }

            Debug.Log(BuildAutoCompiledMessage(
                BuildTitle("Auto-compiled missing Inspector references. Check these assignments if behavior looks wrong.", Yellow),
                autoCompiledReferences));
        }

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

        public static string BuildTitle(string message, string color)
        {
            return $"{ColorBold("INSPECTOR VALIDATION", color)}: {message}";
        }

        private static string BuildAutoCompiledMessage(string title, List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(title);
            builder.AppendLine("These references were assigned automatically before validation:");

            string currentScenePath = null;
            foreach (AutoCompiledInspectorReference autoCompiledReference in autoCompiledReferences)
            {
                if (currentScenePath != autoCompiledReference.ScenePath)
                {
                    currentScenePath = autoCompiledReference.ScenePath;
                    builder.AppendLine();
                    builder.AppendLine(BuildSceneLine(currentScenePath));
                }

                builder.Append("- ");
                builder.Append(autoCompiledReference.GameObjectPath);
                builder.Append(" / ");
                builder.Append(ColorBold($"{autoCompiledReference.ComponentName}.{autoCompiledReference.FieldName}", Yellow));
                builder.Append(" [");
                builder.Append(autoCompiledReference.Strategy);
                builder.AppendLine("]");
            }

            return builder.ToString();
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
        public readonly Severity Severity;
        public readonly string Message;

        public MissingInspectorReference(
            string scenePath,
            string gameObjectPath,
            string componentName,
            string fieldName,
            Severity severity,
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

    public readonly struct AutoCompiledInspectorReference
    {
        public readonly string ScenePath;
        public readonly string GameObjectPath;
        public readonly string ComponentName;
        public readonly string FieldName;
        public readonly string Strategy;

        public AutoCompiledInspectorReference(
            string scenePath,
            string gameObjectPath,
            string componentName,
            string fieldName,
            string strategy)
        {
            ScenePath = scenePath;
            GameObjectPath = gameObjectPath;
            ComponentName = componentName;
            FieldName = fieldName;
            Strategy = strategy;
        }
    }

    public sealed class RequiredInspectorReferenceScanResult
    {
        public readonly List<MissingInspectorReference> MissingReferences;
        public readonly List<AutoCompiledInspectorReference> AutoCompiledReferences;

        public RequiredInspectorReferenceScanResult(
            List<MissingInspectorReference> missingReferences,
            List<AutoCompiledInspectorReference> autoCompiledReferences)
        {
            MissingReferences = missingReferences;
            AutoCompiledReferences = autoCompiledReferences;
        }
    }
}
#endif
