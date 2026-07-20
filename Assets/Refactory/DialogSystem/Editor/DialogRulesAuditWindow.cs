using System;
using System.Collections.Generic;
using CharacterSystem;
using UnityEditor;
using UnityEngine;

public class DialogRulesAuditWindow : EditorWindow
{
    private const string NoStatusLabel = "No Status";
    private const string RulesSearchFilter = "t:CharacterDialogRule";

    private CharacterDialogRule[] rules = Array.Empty<CharacterDialogRule>();
    private string[] ruleLabels = Array.Empty<string>();
    private string[] statusLabels = Array.Empty<string>();
    private Status[] selectableStatuses = Array.Empty<Status>();

    private int selectedRuleIndex;
    private PotionScriptable.EffectType selectedPotion;
    private int selectedStatusIndex;
    private Vector2 scroll;

    [MenuItem("Tools/Good Night Potion/Dialog Rules Audit")]
    public static void Open()
    {
        DialogRulesAuditWindow window = GetWindow<DialogRulesAuditWindow>("Dialog Rules");
        window.minSize = new Vector2(520f, 420f);
        window.RefreshRules();
        window.Show();
    }

    private void OnEnable()
    {
        BuildStatusOptions();
        RefreshRules();
    }

    private void OnGUI()
    {
        DrawToolbar();

        if (rules.Length == 0)
        {
            EditorGUILayout.HelpBox("No CharacterDialogRule assets found in the project.", MessageType.Warning);
            return;
        }

        CharacterDialogRule selectedRule = rules[Mathf.Clamp(selectedRuleIndex, 0, rules.Length - 1)];

        EditorGUILayout.Space(8f);
        DrawSelection(selectedRule);

        EditorGUILayout.Space(8f);
        DrawSelectedCombination(selectedRule);

        EditorGUILayout.Space(12f);
        DrawMissingForSelectedStatus(selectedRule);
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(70f)))
            {
                RefreshRules();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Save Assets", EditorStyles.toolbarButton, GUILayout.Width(90f)))
            {
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawSelection(CharacterDialogRule selectedRule)
    {
        selectedRuleIndex = EditorGUILayout.Popup("Rule Asset", selectedRuleIndex, ruleLabels);
        selectedPotion = (PotionScriptable.EffectType)EditorGUILayout.EnumPopup("Potion", selectedPotion);
        selectedStatusIndex = EditorGUILayout.Popup("Current Status", selectedStatusIndex, statusLabels);

        EditorGUILayout.LabelField("Character", selectedRule.character.ToString());
    }

    private void DrawSelectedCombination(CharacterDialogRule selectedRule)
    {
        StatusDialogCase dialogCase = FindCase(selectedRule, selectedPotion, GetSelectedStatus());
        bool hasCase = dialogCase != null;
        bool hasLines = hasCase && dialogCase.lines != null && dialogCase.lines.Count > 0;

        string condition = $"{selectedRule.character} + {selectedPotion} + {GetSelectedStatusLabel()}";
        EditorGUILayout.LabelField(condition, EditorStyles.boldLabel);

        if (!hasCase)
        {
            EditorGUILayout.HelpBox("This condition does not exist yet.", MessageType.Info);

            if (GUILayout.Button("Create Missing Case"))
            {
                dialogCase = EnsureCase(selectedRule, selectedPotion, GetSelectedStatus());
                MarkDirty(selectedRule);
            }

            return;
        }

        if (!hasLines)
        {
            EditorGUILayout.HelpBox("This condition exists, but has no lines.", MessageType.Warning);
        }

        DrawLines(selectedRule, dialogCase);
    }

    private void DrawLines(CharacterDialogRule selectedRule, StatusDialogCase dialogCase)
    {
        if (dialogCase.lines == null)
        {
            dialogCase.lines = new List<string>();
            MarkDirty(selectedRule);
        }

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(130f));

        for (int i = 0; i < dialogCase.lines.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                string oldLine = dialogCase.lines[i];
                string newLine = EditorGUILayout.TextField($"Line {i + 1}", oldLine);

                if (newLine != oldLine)
                {
                    Undo.RecordObject(selectedRule, "Edit dialog line");
                    dialogCase.lines[i] = newLine;
                    MarkDirty(selectedRule);
                }

                if (GUILayout.Button("-", GUILayout.Width(24f)))
                {
                    Undo.RecordObject(selectedRule, "Remove dialog line");
                    dialogCase.lines.RemoveAt(i);
                    MarkDirty(selectedRule);
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.EndScrollView();

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Line"))
            {
                Undo.RecordObject(selectedRule, "Add dialog line");
                dialogCase.lines.Add(string.Empty);
                MarkDirty(selectedRule);
            }

            if (GUILayout.Button("Save"))
            {
                MarkDirty(selectedRule);
                AssetDatabase.SaveAssets();
            }
        }
    }

    private void DrawMissingForSelectedStatus(CharacterDialogRule selectedRule)
    {
        EditorGUILayout.LabelField($"Missing lines for {GetSelectedStatusLabel()}", EditorStyles.boldLabel);

        List<PotionScriptable.EffectType> missingPotions = GetMissingPotions(selectedRule, GetSelectedStatus());

        if (missingPotions.Count == 0)
        {
            EditorGUILayout.HelpBox("No missing lines for the selected status.", MessageType.None);
            return;
        }

        foreach (PotionScriptable.EffectType potion in missingPotions)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(potion.ToString());

                if (GUILayout.Button("Open", GUILayout.Width(58f)))
                {
                    selectedPotion = potion;
                    GUI.FocusControl(null);
                }

                if (GUILayout.Button("Create", GUILayout.Width(64f)))
                {
                    EnsureCase(selectedRule, potion, GetSelectedStatus());
                    MarkDirty(selectedRule);
                }
            }
        }
    }

    private void RefreshRules()
    {
        string[] guids = AssetDatabase.FindAssets(RulesSearchFilter);
        List<CharacterDialogRule> foundRules = new List<CharacterDialogRule>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            CharacterDialogRule rule = AssetDatabase.LoadAssetAtPath<CharacterDialogRule>(path);

            if (rule != null)
            {
                foundRules.Add(rule);
            }
        }

        foundRules.Sort((CharacterDialogRule a, CharacterDialogRule b) =>
            string.Compare(a.name, b.name, StringComparison.Ordinal));

        rules = foundRules.ToArray();
        ruleLabels = new string[rules.Length];

        for (int i = 0; i < rules.Length; i++)
        {
            ruleLabels[i] = $"{rules[i].name} ({rules[i].character})";
        }

        if (selectedRuleIndex >= rules.Length)
        {
            selectedRuleIndex = Mathf.Max(0, rules.Length - 1);
        }
    }

    private void BuildStatusOptions()
    {
        List<Status> statuses = new List<Status>();

        foreach (Status status in Enum.GetValues(typeof(Status)))
        {
            if (status != Status.None)
            {
                statuses.Add(status);
            }
        }

        selectableStatuses = statuses.ToArray();
        statusLabels = new string[selectableStatuses.Length + 1];
        statusLabels[0] = NoStatusLabel;

        for (int i = 0; i < selectableStatuses.Length; i++)
        {
            statusLabels[i + 1] = selectableStatuses[i].ToString();
        }
    }

    private Status? GetSelectedStatus()
    {
        if (selectedStatusIndex <= 0)
        {
            return null;
        }

        return selectableStatuses[selectedStatusIndex - 1];
    }

    private string GetSelectedStatusLabel()
    {
        if (selectedStatusIndex <= 0)
        {
            return NoStatusLabel;
        }

        return selectableStatuses[selectedStatusIndex - 1].ToString();
    }

    private List<PotionScriptable.EffectType> GetMissingPotions(CharacterDialogRule rule, Status? status)
    {
        List<PotionScriptable.EffectType> missingPotions = new List<PotionScriptable.EffectType>();

        foreach (PotionScriptable.EffectType potion in Enum.GetValues(typeof(PotionScriptable.EffectType)))
        {
            if (potion == PotionScriptable.EffectType.Any)
            {
                continue;
            }

            StatusDialogCase dialogCase = FindCase(rule, potion, status);

            if (dialogCase == null || dialogCase.lines == null || dialogCase.lines.Count == 0)
            {
                missingPotions.Add(potion);
            }
        }

        return missingPotions;
    }

    private StatusDialogCase FindCase(CharacterDialogRule rule, PotionScriptable.EffectType potion, Status? status)
    {
        PotionDialogEntry entry = FindEntry(rule, potion);

        if (entry == null || entry.cases == null)
        {
            return null;
        }

        foreach (StatusDialogCase dialogCase in entry.cases)
        {
            if (MatchesSelectedStatus(dialogCase, status))
            {
                return dialogCase;
            }
        }

        return null;
    }

    private PotionDialogEntry FindEntry(CharacterDialogRule rule, PotionScriptable.EffectType potion)
    {
        if (rule.potionDialogs == null)
        {
            return null;
        }

        foreach (PotionDialogEntry entry in rule.potionDialogs)
        {
            if (entry.potion == potion)
            {
                return entry;
            }
        }

        return null;
    }

    private StatusDialogCase EnsureCase(CharacterDialogRule rule, PotionScriptable.EffectType potion, Status? status)
    {
        Undo.RecordObject(rule, "Create dialog rule case");

        if (rule.potionDialogs == null)
        {
            rule.potionDialogs = new List<PotionDialogEntry>();
        }

        PotionDialogEntry entry = FindEntry(rule, potion);

        if (entry == null)
        {
            entry = new PotionDialogEntry
            {
                potion = potion,
                cases = new List<StatusDialogCase>()
            };

            rule.potionDialogs.Add(entry);
        }

        if (entry.cases == null)
        {
            entry.cases = new List<StatusDialogCase>();
        }

        StatusDialogCase dialogCase = FindCase(rule, potion, status);

        if (dialogCase != null)
        {
            return dialogCase;
        }

        dialogCase = new StatusDialogCase
        {
            requiredStatuses = BuildRequiredStatuses(status),
            lines = new List<string>()
        };

        entry.cases.Add(dialogCase);
        return dialogCase;
    }

    private List<Status> BuildRequiredStatuses(Status? status)
    {
        List<Status> requiredStatuses = new List<Status>();

        if (status.HasValue)
        {
            requiredStatuses.Add(status.Value);
        }

        return requiredStatuses;
    }

    private bool MatchesSelectedStatus(StatusDialogCase dialogCase, Status? status)
    {
        if (HasNoRequiredStatuses(dialogCase))
        {
            return !status.HasValue;
        }

        if (!status.HasValue || dialogCase.requiredStatuses.Count != 1)
        {
            return false;
        }

        return dialogCase.requiredStatuses[0] == status.Value;
    }

    private bool HasNoRequiredStatuses(StatusDialogCase dialogCase)
    {
        return dialogCase.requiredStatuses == null || dialogCase.requiredStatuses.Count == 0;
    }

    private void MarkDirty(CharacterDialogRule rule)
    {
        EditorUtility.SetDirty(rule);
    }
}
