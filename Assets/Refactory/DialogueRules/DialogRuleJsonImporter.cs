// DialogRuleJsonImporter.cs
using CharacterSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using UnityEditor;
using UnityEngine;

public static class DialogRuleJsonImporter
{
    private static Status[] BalrogImmunity = new Status[] { Status.Poisoned, Status.Grounded, Status.Grass, Status.Wet, Status.Algae, Status.Freezed};
    private static Status[] TreeImmunity = new Status[] { Status.Poisoned, Status.Grass, Status.Wet, Status.Algae, Status.Freezed};
    private static Status[] YetiImmunity = new Status[] { Status.Grass, Status.Wet, Status.Algae, Status.Freezed, Status.Burned};
    private static Status[] PupperFishImmunity = new Status[] { Status.Grounded, Status.Freezed, Status.Burned};

    public static Status[] GetImmunitiesForCharacter(CharacterType character)
    {
        return character switch
        {
            CharacterType.Balrog => BalrogImmunity,
            CharacterType.Tree => TreeImmunity,
            CharacterType.Yeti => YetiImmunity,
            CharacterType.PupperFish => PupperFishImmunity,
            _ => Array.Empty<Status>(),
        };
    }


    [MenuItem("Tools/Dialog/Import Dialog Rules from JSON")]
    public static void ImportFromJson()
    {
        // Selezione file JSON
        string path = EditorUtility.OpenFilePanel(
            "Import Dialog Rules JSON",
            Application.dataPath,
            "json"
        );

        if (string.IsNullOrEmpty(path))
            return;

        string json = File.ReadAllText(path);
        var data = Newtonsoft.Json.JsonConvert.DeserializeObject<DialogRuleDataCollection>(json);//JsonConverter<DialogRuleDataCollection>(json);

        if (data == null || data.rules == null)
        {
            Debug.LogError("JSON non valido o vuoto");
            return;
        }

        // Cartella di output
        string targetFolder = "Assets/DialogRules";
        if (!AssetDatabase.IsValidFolder(targetFolder))
            AssetDatabase.CreateFolder("Assets", "DialogRules");

        foreach (var ruleData in data.rules)
        {
            CreateRuleAsset(ruleData, targetFolder);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Importate {data.rules.Count} DialogRule");
    }

    [MenuItem("Tools/Dialog/InitializeDrunkRules")]
    public static void InitializeRules()
    {
        int createdCount = 0;
        CharacterType[] characters = (CharacterType[])Enum.GetValues(typeof(CharacterType));
        PotionScriptable.EffectType[] potions = (PotionScriptable.EffectType[])Enum.GetValues(typeof(PotionScriptable.EffectType));
        Status[] statuses = (Status[])Enum.GetValues(typeof(Status));
        foreach (CharacterType characterType in characters)
        {
            if (characterType == CharacterType.Any)
                continue;
            foreach (PotionScriptable.EffectType potionType in potions)
            {
                if (potionType == (PotionScriptable.EffectType.Any))
                    continue;
                foreach (Status status in statuses)
                {
                    if(GetImmunitiesForCharacter(characterType).Contains(status))
                        continue;
                    DialogRuleData data = new DialogRuleData
                    {
                        trigger = DialogTrigger.PotionDrink,
                        character = characterType,
                        potion = potionType,
                        priority = 0,
                        requiredStatuses = status.Equals(Status.None) ? new List<Status>() : new List<Status> { status },
                        forbiddenStatuses = new List<Status>(),
                        lines = new List<string> { $"{characterType} drank {potionType} with status {status}." }
                    };
                    string targetFolder = $"Assets/DialogRules/{characterType}";
                    if (!AssetDatabase.IsValidFolder(targetFolder))
                        AssetDatabase.CreateFolder("Assets/DialogRules", characterType.ToString());
                    CreateRuleAsset(data, targetFolder, $"{characterType}_drank_{potionType}_in_{status}.");
                    createdCount++;
                }
            }
        }
        Debug.Log($"Created {createdCount} drunk dialog rules.");
    }

    private static void CreateRuleAsset(DialogRuleData data, string folder, string assetName = "")
    {
        var asset = ScriptableObject.CreateInstance<DialogRule>();

        asset.trigger = data.trigger;
        Debug.Log(data.trigger);
        asset.character = data.character;
        Debug.Log(data.character);
        asset.potion = data.potion;
        asset.priority = data.priority;

        asset.requiredStatuses = data.requiredStatuses.ToArray();
        asset.forbiddenStatuses = data.forbiddenStatuses.ToArray();
        asset.lines = data.lines.ToArray();

        string safeName = "";
        if (assetName != "")
        {
            safeName = assetName;
        }
        else
        {
            switch (data.trigger)
            {
                case DialogTrigger.PotionDrink:
                    safeName = $"{data.character}_Drink_{data.potion}_{data.priority}";
                    break;
                case DialogTrigger.Transformation:
                    safeName = $"TransformTo_{data.character}_{data.priority}";
                    break;
                case DialogTrigger.StatusTick:
                    safeName = $"{data.character}_StatusTick_" + (data.requiredStatuses.Count > 0 ? data.requiredStatuses[0] : "NONE") + $"_{data.priority}";
                    break;
                default:
                    safeName = $"{data.character}_{data.trigger}_{data.potion}_{data.priority}";
                    break;
            }
        }
        string assetPath = AssetDatabase.GenerateUniqueAssetPath(
            $"{folder}/{safeName}.asset"
        );


        AssetDatabase.CreateAsset(asset, assetPath);
    }
}

[Serializable]
public class DialogRuleData
{
    public DialogTrigger trigger;
    public CharacterType character;
    public PotionScriptable.EffectType potion;
    public int priority;

    public List<Status> requiredStatuses = new();
    public List<Status> forbiddenStatuses = new();

    public List<string> lines = new();
}

[Serializable]
public class DialogRuleDataCollection
{
    public List<DialogRuleData> rules = new();
}