using UnityEditor;

[CustomEditor(typeof(LevelSettings))]
public class LevelSettingsEditor : Editor
{
    private SerializedProperty isPuzzleMode;
    private SerializedProperty bestHealthScore;
    private SerializedProperty startingLightIntensity;
    private SerializedProperty decayLightOverTime;
    private SerializedProperty lightDecayInterval;
    private SerializedProperty defaultSpawnSeconds;
    private SerializedProperty hyperModeSpawnSeconds;
    private SerializedProperty hyperHyperModeSpawnSeconds;
    private SerializedProperty minimumSpawnSeconds;
    private SerializedProperty maxActivePotionsBeforeBomb;

    private void OnEnable()
    {
        isPuzzleMode = serializedObject.FindProperty("isPuzzleMode");
        bestHealthScore = serializedObject.FindProperty("bestHealthScore");
        startingLightIntensity = serializedObject.FindProperty("startingLightIntensity");
        decayLightOverTime = serializedObject.FindProperty("decayLightOverTime");
        lightDecayInterval = serializedObject.FindProperty("lightDecayInterval");
        defaultSpawnSeconds = serializedObject.FindProperty("defaultSpawnSeconds");
        hyperModeSpawnSeconds = serializedObject.FindProperty("hyperModeSpawnSeconds");
        hyperHyperModeSpawnSeconds = serializedObject.FindProperty("hyperHyperModeSpawnSeconds");
        minimumSpawnSeconds = serializedObject.FindProperty("minimumSpawnSeconds");
        maxActivePotionsBeforeBomb = serializedObject.FindProperty("maxActivePotionsBeforeBomb");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(isPuzzleMode);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Score", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(bestHealthScore);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startingLightIntensity);
        EditorGUILayout.PropertyField(decayLightOverTime);
        EditorGUILayout.PropertyField(lightDecayInterval);

        if (!isPuzzleMode.boolValue)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Spawn Speed", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(defaultSpawnSeconds);
            EditorGUILayout.PropertyField(hyperModeSpawnSeconds);
            EditorGUILayout.PropertyField(hyperHyperModeSpawnSeconds);
            EditorGUILayout.PropertyField(minimumSpawnSeconds);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Events", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(maxActivePotionsBeforeBomb);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
