using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class LevelOnePotionPrefabMigration
{
    private const string GameplayElementsContainerName = "Puzzle - GamePlay Elements";
    private const string PotionsFolderPath = "Assets/_Refactory/Prefabs/Potions";
    private const string HealthMediumPrefabPath = "Assets/_Refactory/Prefabs/Potions/HealthPotion.prefab";
    private const string HealthSmallPrefabPath = "Assets/_Refactory/Prefabs/Potions/HealthPotionSmall.prefab";
    private const string HealthLargePrefabPath = "Assets/_Refactory/Prefabs/Potions/HealthPotionBig.prefab";
    private const string LightMediumPrefabPath = "Assets/_Refactory/Prefabs/Potions/Light Potion.prefab";
    private const string LightSmallPrefabPath = "Assets/_Refactory/Prefabs/Potions/LightPotionSmall.prefab";

    [MenuItem("Tools/The Good Night Potion/Migration/Replace Active Level Potion Prefabs %&m")]
    private static void ReplacePotionPrefabs()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("Open a level scene before replacing potion prefabs.");
            return;
        }

        Transform gameplayElementsContainer = FindGameplayElementsContainer(scene);
        if (gameplayElementsContainer == null)
        {
            Debug.LogError(
                $"{scene.name}: '{GameplayElementsContainerName}' is missing. " +
                "Potion replacement only runs inside this level container.");
            return;
        }

        Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        List<PotionScript> scenePotions = new List<PotionScript>();
        PotionScript[] potions = Object.FindObjectsByType<PotionScript>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (PotionScript potion in potions)
        {
            if (potion.gameObject.scene == scene && potion.transform.IsChildOf(gameplayElementsContainer))
            {
                scenePotions.Add(potion);
            }
        }

        int replacedCount = 0;
        int normalizedNameCount = 0;
        foreach (PotionScript potion in scenePotions)
        {
            GameObject prefab = GetReplacementPrefab(potion, prefabCache);
            if (prefab == null)
            {
                continue;
            }

            GameObject currentPrefab = PrefabUtility.GetCorrespondingObjectFromSource(potion.gameObject);
            if (currentPrefab == prefab)
            {
                if (potion.gameObject.name != prefab.name)
                {
                    Undo.RecordObject(potion.gameObject, "Normalize Potion Prefab Name");
                    potion.gameObject.name = prefab.name;
                    normalizedNameCount++;
                }

                continue;
            }

            ReplacePotionInstance(potion.gameObject, prefab, scene);
            replacedCount++;
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log(
            $"{scene.name}: replaced {replacedCount} potion objects and normalized {normalizedNameCount} prefab names. " +
            "Save the scene after reviewing the result.");
    }

    private static GameObject GetReplacementPrefab(PotionScript potion, Dictionary<string, GameObject> prefabCache)
    {
        if (potion.potion == null)
        {
            return GetMissingLightPotionPrefab(potion, prefabCache);
        }

        string prefabFileName = GetPrefabFileName(potion.potion.Id, potion.Size);
        if (string.IsNullOrEmpty(prefabFileName))
        {
            Debug.LogWarning(
                $"{potion.name}: no canonical prefab is configured for {potion.potion.Id} {potion.Size}. Skipped replacement.",
                potion);
            return null;
        }

        if (!prefabCache.TryGetValue(prefabFileName, out GameObject prefab))
        {
            prefab = LoadRequiredPrefab($"{PotionsFolderPath}/{prefabFileName}");
            prefabCache.Add(prefabFileName, prefab);
        }

        return prefab;
    }

    private static GameObject GetMissingLightPotionPrefab(
        PotionScript potion,
        Dictionary<string, GameObject> prefabCache)
    {
        if (potion.name.IndexOf("light", System.StringComparison.OrdinalIgnoreCase) < 0)
        {
            Debug.LogWarning($"{potion.name}: PotionScriptable reference is missing. Skipped replacement.", potion);
            return null;
        }

        string prefabFileName = GetLightPrefabFileName(potion.Size);
        if (string.IsNullOrEmpty(prefabFileName))
        {
            Debug.LogWarning(
                $"{potion.name}: missing Light Potion reference has unsupported size '{potion.Size}'. Skipped replacement.",
                potion);
            return null;
        }

        if (!prefabCache.TryGetValue(prefabFileName, out GameObject prefab))
        {
            prefab = LoadRequiredPrefab($"{PotionsFolderPath}/{prefabFileName}");
            prefabCache.Add(prefabFileName, prefab);
        }

        Debug.LogWarning(
            $"{potion.name}: restored a missing Light Potion reference using its serialized size '{potion.Size}'.",
            potion);
        return prefab;
    }

    private static string GetPrefabFileName(PotionScriptable.PotionId potionId, PotionScriptable.PotionSize size)
    {
        switch (potionId)
        {
            case PotionScriptable.PotionId.Health:
                return GetHealthPrefabFileName(size);
            case PotionScriptable.PotionId.Light:
                return GetLightPrefabFileName(size);
            case PotionScriptable.PotionId.Lava:
                return GetMediumOnlyPrefabFileName(size, "Lava Potion.prefab");
            case PotionScriptable.PotionId.Fire:
                return GetMediumOnlyPrefabFileName(size, "Fire Potion.prefab");
            case PotionScriptable.PotionId.Seed:
                return GetMediumOnlyPrefabFileName(size, "Grass Potion.prefab");
            case PotionScriptable.PotionId.Water:
                return GetMediumOnlyPrefabFileName(size, "Water Potion.prefab");
            case PotionScriptable.PotionId.Ground:
                return GetMediumOnlyPrefabFileName(size, "Ground Potion.prefab");
            case PotionScriptable.PotionId.Poison:
                return GetMediumOnlyPrefabFileName(size, "Venom Potion.prefab");
            case PotionScriptable.PotionId.Ice:
                return GetMediumOnlyPrefabFileName(size, "Ice Potion.prefab");
            case PotionScriptable.PotionId.Dark:
                return GetMediumOnlyPrefabFileName(size, "Dark Potion.prefab");
            default:
                return null;
        }
    }

    private static string GetHealthPrefabFileName(PotionScriptable.PotionSize size)
    {
        switch (size)
        {
            case PotionScriptable.PotionSize.Small:
                return HealthSmallPrefabPath.Replace($"{PotionsFolderPath}/", string.Empty);
            case PotionScriptable.PotionSize.Medium:
                return HealthMediumPrefabPath.Replace($"{PotionsFolderPath}/", string.Empty);
            case PotionScriptable.PotionSize.Large:
                return HealthLargePrefabPath.Replace($"{PotionsFolderPath}/", string.Empty);
            default:
                return null;
        }
    }

    private static string GetLightPrefabFileName(PotionScriptable.PotionSize size)
    {
        switch (size)
        {
            case PotionScriptable.PotionSize.Small:
                return LightSmallPrefabPath.Replace($"{PotionsFolderPath}/", string.Empty);
            case PotionScriptable.PotionSize.Medium:
                return LightMediumPrefabPath.Replace($"{PotionsFolderPath}/", string.Empty);
            default:
                return null;
        }
    }

    private static string GetMediumOnlyPrefabFileName(PotionScriptable.PotionSize size, string prefabFileName)
    {
        return size == PotionScriptable.PotionSize.Medium ? prefabFileName : null;
    }

    private static Transform FindGameplayElementsContainer(Scene scene)
    {
        Transform[] transforms = Object.FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        foreach (Transform transform in transforms)
        {
            if (transform.gameObject.scene == scene && transform.name == GameplayElementsContainerName)
            {
                return transform;
            }
        }

        return null;
    }

    private static GameObject LoadRequiredPrefab(string assetPath)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogError($"Required potion prefab is missing at '{assetPath}'.");
        }

        return prefab;
    }

    private static void ReplacePotionInstance(GameObject source, GameObject prefab, Scene scene)
    {
        Transform sourceTransform = source.transform;
        Transform parent = sourceTransform.parent;
        int siblingIndex = sourceTransform.GetSiblingIndex();
        Vector3 localPosition = sourceTransform.localPosition;
        Quaternion localRotation = sourceTransform.localRotation;
        Vector3 localScale = sourceTransform.localScale;
        int layer = source.layer;
        bool activeSelf = source.activeSelf;

        GameObject replacement = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        if (replacement == null)
        {
            Debug.LogError($"Could not instantiate potion prefab '{prefab.name}'.");
            return;
        }

        Transform replacementTransform = replacement.transform;
        replacementTransform.SetParent(parent, false);
        replacementTransform.SetSiblingIndex(siblingIndex);
        replacementTransform.localPosition = localPosition;
        replacementTransform.localRotation = localRotation;
        replacementTransform.localScale = localScale;
        replacement.layer = layer;
        replacement.SetActive(activeSelf);

        Undo.RegisterCreatedObjectUndo(replacement, "Replace Level 1 Potion Prefab");
        Undo.DestroyObjectImmediate(source);
    }
}
