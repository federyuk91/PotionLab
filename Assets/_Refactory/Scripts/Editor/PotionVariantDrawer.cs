using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PotionScriptable.PotionVariant))]
public sealed class PotionVariantDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty sizeProperty = property.FindPropertyRelative("size");
        string title = GetSizeTitle(sizeProperty);

        EditorGUI.PropertyField(position, property, new GUIContent(title), true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, true);
    }

    private static string GetSizeTitle(SerializedProperty sizeProperty)
    {
        if (sizeProperty == null)
        {
            return "Potion Variant";
        }

        if (sizeProperty.hasMultipleDifferentValues)
        {
            return "Mixed Sizes";
        }

        int enumIndex = sizeProperty.enumValueIndex;
        if (enumIndex < 0 || enumIndex >= sizeProperty.enumDisplayNames.Length)
        {
            return "Potion Variant";
        }

        return sizeProperty.enumDisplayNames[enumIndex];
    }
}
