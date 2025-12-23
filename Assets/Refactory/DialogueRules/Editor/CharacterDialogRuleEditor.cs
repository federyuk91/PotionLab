using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CharacterDialogRule))]
public class CharacterDialogRuleEditor : Editor
{
    private ReorderableList potionList;

    private void OnEnable()
    {
        var prop = serializedObject.FindProperty("potionDialogs");

        potionList = new ReorderableList(serializedObject, prop, true, true, true, true);

        potionList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(rect, "Potion Dialogs");
        };

        potionList.drawElementCallback = (rect, index, active, focused) =>
        {
            var element = prop.GetArrayElementAtIndex(index);


            // Spazio per il drag handle
            rect.xMin += 18f;

            // Lasciamo che Unity gestisca tutto
            EditorGUI.PropertyField(
                rect,
                element,
                new GUIContent(GetPotionLabel(element)),
                true
            );
        };

        potionList.elementHeightCallback = index =>
        {
            var element = prop.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(element, true) + 4;
        };
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        potionList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private string GetPotionLabel(SerializedProperty element)
    {
        var potion = element.FindPropertyRelative("potion");
        return potion.enumDisplayNames[potion.enumValueIndex];
    }
}
