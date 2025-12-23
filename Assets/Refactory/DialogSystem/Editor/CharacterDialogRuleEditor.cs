using CharacterSystem;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.U2D.Animation;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(CharacterDialogRule))]
public class CharacterDialogRuleEditor : Editor
{
    private ReorderableList potionList;
    private SerializedProperty characterProp;
    private void OnEnable()
    {
        characterProp = serializedObject.FindProperty("character");
        var prop = serializedObject.FindProperty("potionDialogs");

        potionList = new ReorderableList(serializedObject, prop, true, true, true, true);


        potionList.drawHeaderCallback = rect =>
        {
            EditorGUI.LabelField(
            rect,
            $"{characterProp.GetEnumValue<CharacterType>()} Dialogs On Drunk");
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
        // Campo Character (aggiorna automaticamente il valore)
        EditorGUILayout.PropertyField(
            characterProp,
            new GUIContent("Character")
        );

        EditorGUILayout.Space(6);
        potionList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
    }

    private string GetPotionLabel(SerializedProperty element)
    {
        var potion = element.FindPropertyRelative("potion");
        return potion.enumDisplayNames[potion.enumValueIndex];
    }
}
