using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal static class SpriteToolboxEditorGui
    {
        public static void DrawAccentLabel(string label, Color color, GUIStyle style)
        {
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(label, style);
            GUI.contentColor = previousContentColor;
        }

        public static Color DrawColorField(string label, Color color, float labelWidth, Color? labelColor = null)
        {
            EditorGUILayout.BeginHorizontal();
            if (labelColor.HasValue)
            {
                Color previousContentColor = GUI.contentColor;
                GUI.contentColor = labelColor.Value;
                GUILayout.Label(label, GUILayout.Width(labelWidth));
                GUI.contentColor = previousContentColor;
            }
            else
            {
                GUILayout.Label(label, GUILayout.Width(labelWidth));
            }

            Color result = EditorGUILayout.ColorField(color, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            return result;
        }
    }
}
