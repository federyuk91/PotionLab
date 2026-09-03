using InspectorValidation;
using UnityEngine;

public sealed class TorchAnimationVariation : MonoBehaviour
{
    private const string TorchTag = "Torch";

    [Header("Animation Tracks")]
    [SerializeField, RequiredInspectorReference] private Animator[] animatorTracks;
    [SerializeField] private Animation[] legacyAnimationTracks;

    [Header("Random Variation")]
    [SerializeField] private Vector2 animatorSpeedRange = new Vector2(0.9f, 1.1f);
    [SerializeField] private Vector2 legacyAnimationSpeedRange = new Vector2(0.85f, 1.15f);

    private void Start()
    {
        if (!CompareTag(TorchTag))
        {
            Debug.LogWarning($"{name}: TorchAnimationVariation expects the '{TorchTag}' tag.", this);
        }

        RandomizeAnimatorTracks();
        RandomizeLegacyAnimationTracks();
    }

    private void RandomizeAnimatorTracks()
    {
        if (animatorTracks == null || animatorTracks.Length == 0)
        {
            Debug.LogError($"{name}: assign at least one Animator Track in TorchAnimationVariation.", this);
            return;
        }

        foreach (Animator animatorTrack in animatorTracks)
        {
            if (animatorTrack == null)
            {
                Debug.LogError($"{name}: TorchAnimationVariation has a missing Animator Track reference.", this);
                continue;
            }

            float normalizedStartTime = Random.value;
            animatorTrack.speed = GetRandomSpeed(animatorSpeedRange);
            animatorTrack.Play(0, 0, normalizedStartTime);
            animatorTrack.Update(0f);
        }
    }

    private void RandomizeLegacyAnimationTracks()
    {
        if (legacyAnimationTracks == null)
        {
            return;
        }

        foreach (Animation animationTrack in legacyAnimationTracks)
        {
            if (animationTrack == null)
            {
                Debug.LogError($"{name}: TorchAnimationVariation has a missing Legacy Animation Track reference.", this);
                continue;
            }

            AnimationClip clip = animationTrack.clip;
            if (clip == null)
            {
                Debug.LogWarning($"{name}: a torch Legacy Animation Track has no default clip.", animationTrack);
                continue;
            }

            animationTrack.Play(clip.name);
            AnimationState state = animationTrack[clip.name];
            if (state == null)
            {
                Debug.LogWarning($"{name}: the default torch light clip is not registered in its Animation component.", animationTrack);
                continue;
            }

            state.speed = GetRandomSpeed(legacyAnimationSpeedRange);
            state.normalizedTime = Random.value;
            animationTrack.Sample();
        }
    }

    private float GetRandomSpeed(Vector2 speedRange)
    {
        float minimumSpeed = Mathf.Max(0.01f, Mathf.Min(speedRange.x, speedRange.y));
        float maximumSpeed = Mathf.Max(minimumSpeed, Mathf.Max(speedRange.x, speedRange.y));
        return Random.Range(minimumSpeed, maximumSpeed);
    }
}
