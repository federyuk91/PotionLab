using UnityEditor;
using UnityEngine;
using CharacterSystem;

[CustomPropertyDrawer(typeof(StatusDialogCase))]
public class StatusDialogCaseDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty statusesProp = property.FindPropertyRelative("requiredStatuses");

        string title = "No Status (empty list)";
        if (statusesProp.arraySize > 0)
        {
            title = "";
            for (int i = 0; i < statusesProp.arraySize; i++)
            {
                SerializedProperty statusProperty = statusesProp.GetArrayElementAtIndex(i);
                string statusName = statusProperty.enumDisplayNames[statusProperty.enumValueIndex];
                if (statusName == Status.None.ToString())
                {
                    statusName = "Status.None (invalid)";
                }

                title += statusName + (i < statusesProp.arraySize - 1 ? "-" : "");
            }
        }

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }
}
