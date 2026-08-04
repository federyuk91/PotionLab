using UnityEngine;

public class LevelSettings : MonoBehaviour
{
    private const int MinLightIntensity = 0;
    private const int MaxLightIntensity = 3;

    [Header("Mode")]
    [SerializeField] private bool isPuzzleMode = true;

    [Header("Score")]
    [SerializeField] private int bestHealthScore = 10;

    [Header("Light")]
    [SerializeField, Range(MinLightIntensity, MaxLightIntensity)] private int startingLightIntensity = 1;
    [SerializeField] private bool decayLightOverTime = false;
    [SerializeField] private float lightDecayInterval = 43f;

    public bool IsPuzzleMode => isPuzzleMode;
    public int BestHealthScore => bestHealthScore;
    public int StartingLightIntensity => startingLightIntensity;
    public bool DecayLightOverTime => decayLightOverTime;
    public float LightDecayInterval => lightDecayInterval;
}
