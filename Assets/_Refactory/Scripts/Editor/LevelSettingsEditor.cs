using UnityEditor;

[CustomEditor(typeof(LevelSettings))]
public class LevelSettingsEditor : Editor
{
    private SerializedProperty isPuzzleMode;
    private SerializedProperty bestHealthScore;
    private SerializedProperty maxMalusScore;
    private SerializedProperty introPresentationLine;
    private SerializedProperty introPresentationVoiceClip;
    private SerializedProperty introPresentationCharactersPerSecond;
    private SerializedProperty introPresentationStartDelay;
    private SerializedProperty startingLightIntensity;
    private SerializedProperty startingCatchphrase;
    private SerializedProperty startingCatchphraseDuration;
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
        maxMalusScore = serializedObject.FindProperty("maxMalusScore");
        introPresentationLine = serializedObject.FindProperty("introPresentationLine");
        introPresentationVoiceClip = serializedObject.FindProperty("introPresentationVoiceClip");
        introPresentationCharactersPerSecond = serializedObject.FindProperty("introPresentationCharactersPerSecond");
        introPresentationStartDelay = serializedObject.FindProperty("introPresentationStartDelay");
        startingLightIntensity = serializedObject.FindProperty("startingLightIntensity");
        startingCatchphrase = serializedObject.FindProperty("startingCatchphrase");
        startingCatchphraseDuration = serializedObject.FindProperty("startingCatchphraseDuration");
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
        EditorGUILayout.PropertyField(maxMalusScore);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Intro Presentation", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(introPresentationLine);
        EditorGUILayout.PropertyField(introPresentationVoiceClip);
        EditorGUILayout.PropertyField(introPresentationCharactersPerSecond);
        EditorGUILayout.PropertyField(introPresentationStartDelay);
        EditorGUILayout.Space();

        EditorGUILayout.LabelField("Light", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(startingLightIntensity);
        EditorGUILayout.PropertyField(startingCatchphrase);
        EditorGUILayout.PropertyField(startingCatchphraseDuration);
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
