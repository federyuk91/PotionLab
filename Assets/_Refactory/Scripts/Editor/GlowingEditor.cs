using Refactory.UI;
using UnityEditor;
using UnityEditor.UI;

namespace Refactory.Editor
{
    [CustomEditor(typeof(Glowing))]
    [CanEditMultipleObjects]
    public sealed class GlowingEditor : GraphicEditor
    {
        private SerializedProperty glowMaterial;
        private SerializedProperty glowActive;
        private SerializedProperty imageColor;

        protected override void OnEnable()
        {
            base.OnEnable();

            glowMaterial = serializedObject.FindProperty("glowMaterial");
            glowActive = serializedObject.FindProperty("glowActive");
            imageColor = serializedObject.FindProperty("imageColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            using (new EditorGUI.DisabledScope(true))
            {
                DrawProperty(m_Script);
            }

            DrawProperty(m_Maskable);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Shader", EditorStyles.boldLabel);
            DrawProperty(glowMaterial);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Glow", EditorStyles.boldLabel);
            DrawProperty(glowActive);
            DrawProperty(imageColor);
            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawProperty(SerializedProperty property)
        {
            if (property != null)
            {
                EditorGUILayout.PropertyField(property);
            }
        }
    }
}
