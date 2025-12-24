using UnityEditor;
using UnityEngine;
using CharacterSystem;

[CustomPropertyDrawer(typeof(StatusDialogCase))]
public class StatusDialogCaseDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var statusesProp = property.FindPropertyRelative("requiredStatuses");

        string title = "None";
        if (statusesProp.arraySize > 0)
        {
            title = "";
            for (int i = 0; i < statusesProp.arraySize; i++)
            {
                var s = statusesProp.GetArrayElementAtIndex(i).enumDisplayNames[
                    statusesProp.GetArrayElementAtIndex(i).enumValueIndex
                ];
                title += s + (i < statusesProp.arraySize - 1 ? "-" : "");
            }
        }

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }
}
