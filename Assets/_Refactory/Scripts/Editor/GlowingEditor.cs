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
        private SerializedProperty rayColor;
        private SerializedProperty rayCount;
        private SerializedProperty rotationDegreesPerSecond;
        private SerializedProperty minimumPulse;
        private SerializedProperty pulseAmplitude;
        private SerializedProperty pulseSpeed;
        private SerializedProperty innerRadiusRatio;
        private SerializedProperty shortRayHalfAngle;
        private SerializedProperty longRayHalfAngle;

        protected override void OnEnable()
        {
            base.OnEnable();

            glowMaterial = serializedObject.FindProperty("glowMaterial");
            glowActive = serializedObject.FindProperty("glowActive");
            imageColor = serializedObject.FindProperty("imageColor");
            rayColor = serializedObject.FindProperty("rayColor");
            rayCount = serializedObject.FindProperty("rayCount");
            rotationDegreesPerSecond = serializedObject.FindProperty("rotationDegreesPerSecond");
            minimumPulse = serializedObject.FindProperty("minimumPulse");
            pulseAmplitude = serializedObject.FindProperty("pulseAmplitude");
            pulseSpeed = serializedObject.FindProperty("pulseSpeed");
            innerRadiusRatio = serializedObject.FindProperty("innerRadiusRatio");
            shortRayHalfAngle = serializedObject.FindProperty("shortRayHalfAngle");
            longRayHalfAngle = serializedObject.FindProperty("longRayHalfAngle");
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
            DrawProperty(rayColor);
            EditorGUILayout.Space();
            DrawProperty(rayCount);
            DrawProperty(rotationDegreesPerSecond);
            DrawProperty(minimumPulse);
            DrawProperty(pulseAmplitude);
            DrawProperty(pulseSpeed);
            DrawProperty(innerRadiusRatio);
            DrawProperty(shortRayHalfAngle);
            DrawProperty(longRayHalfAngle);
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
