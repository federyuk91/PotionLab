using System.Collections;
using System.Collections.Generic;
using CharacterSystem;
using InspectorValidation;
using Refactory.CameraSystem;
using UnityEngine;

namespace EndlessSystem
{
    [DisallowMultipleComponent]
    public sealed class EndlessEndSequenceController : MonoBehaviour
    {
        [Header("Sources")]
        [SerializeField, RequiredInspectorReference] private GameManager gameManager;
        [SerializeField, RequiredInspectorReference] private CharacterUIController characterUIController;
        [SerializeField, RequiredInspectorReference] private Camera gameplayCamera;
        [SerializeField, RequiredInspectorReference] private CameraShakeController cameraShakeController;
        [SerializeField, RequiredInspectorReference] private CanvasGroup gameplayUI;
        [SerializeField, RequiredInspectorReference] private GrimoireAnimation grimoireAnimation;

        [Header("Empty Bottle Prefabs")]
        [SerializeField, RequiredInspectorReference] private EndlessEmptyBottle smallBottlePrefab;
        [SerializeField, RequiredInspectorReference] private EndlessEmptyBottle mediumBottlePrefab;
        [SerializeField, RequiredInspectorReference] private EndlessEmptyBottle largeBottlePrefab;
        [SerializeField, RequiredInspectorReference] private Transform bottleContainer;
        [SerializeField, RequiredInspectorReference] private BoxCollider2D bottleDropArea;
        [SerializeField, RequiredInspectorReference] private BoxCollider2D bottlePileGround;

        [Header("Bottle Activation Audio")]
        [SerializeField, RequiredInspectorReference] private AudioSource bottleAudioSource;
        [SerializeField, RequiredInspectorReference] private AudioClip bottleActivationClip;
        [SerializeField, Range(0f, 1f)] private float bottleActivationVolume = 0.21f;
        [SerializeField, Range(0.1f, 3f)] private float bottleActivationPitchMin = 0.9f;
        [SerializeField, Range(0.1f, 3f)] private float bottleActivationPitchMax = 1.15f;
        [SerializeField, Range(0f, 1f)] private float bottleActivationSoundChance = 0.1f;
        [SerializeField, Min(0f)] private float bottleActivationSoundCooldown = 0.18f;

        [Header("Camera Transition")]
        [SerializeField] private Vector3 cameraTargetLocalPosition = new Vector3(21.3f, -0.49f, -10f);
        [SerializeField, Min(0.1f)] private float cameraShiftDuration = 1.5f;

        [Header("Death Animation Timing")]
        [SerializeField, Range(0.8f, 1f)] private float deathAnimationCompletion = 0.98f;
        [SerializeField, Min(0f)] private float deathAnimationEndPadding = 0.12f;
        [SerializeField, Min(0.1f)] private float deathAnimationStartTimeout = 0.75f;
        [SerializeField, Min(0.5f)] private float deathAnimationMaximumWait = 3f;

        [Header("Bottle Drop")]
        [SerializeField, Min(0f)] private float bottleReleaseInterval = 0.075f;
        [SerializeField, Min(0f)] private float maximumBottleReleaseDuration = 2.5f;
        [SerializeField, Min(0f)] private float settledPileHeight = 1.6f;
        [SerializeField, Min(0)] private int maximumSimulatedBottles = 64;
        [SerializeField, Min(0.1f)] private float physicsDuration = 3f;
        [SerializeField] private Vector2 horizontalLaunchRange = new Vector2(-1.25f, 1.25f);
        [SerializeField] private Vector2 verticalLaunchRange = new Vector2(0.4f, 1.8f);
        [SerializeField] private Vector2 angularVelocityRange = new Vector2(-180f, 180f);

        [Header("Testing")]
        [Tooltip("Editor/Development Build only. Values above 0 override only the number of empty bottles shown in the final sequence.")]
        [SerializeField, Min(0)] private int testBottleCountOverride;

        private readonly List<EndlessEmptyBottle> pooledBottles = new List<EndlessEmptyBottle>();
        private Coroutine endSequence;
        private Coroutine freezeBottlesRoutine;
        private float bottleAudioBasePitch = 1f;
        private float nextBottleSoundTime;

        private void Awake()
        {
            if (bottleAudioSource != null)
            {
                bottleAudioBasePitch = bottleAudioSource.pitch;
            }
        }

        private void OnEnable()
        {
            if (gameManager != null)
            {
                gameManager.PotionRemoved += HandlePotionRemoved;
            }

            if (characterUIController != null)
            {
                characterUIController.EndlessResultPresentationRequested += BeginEndSequence;
            }
        }

        private void OnDisable()
        {
            if (gameManager != null)
            {
                gameManager.PotionRemoved -= HandlePotionRemoved;
            }

            if (characterUIController != null)
            {
                characterUIController.EndlessResultPresentationRequested -= BeginEndSequence;
            }
        }

        private void HandlePotionRemoved(PotionScript potion, bool drunked)
        {
            if (!drunked || gameManager == null || !gameManager.IsEndlessMode)
            {
                return;
            }

            AddPooledBottle();
        }

        private void BeginEndSequence()
        {
            if (endSequence != null)
            {
                return;
            }

            if (!HasRequiredReferences())
            {
                characterUIController.ShowEndlessResultAfterTransition();
                return;
            }

            endSequence = StartCoroutine(PlayEndSequence());
        }

        private IEnumerator PlayEndSequence()
        {
            yield return WaitForDeathAnimation();

            grimoireAnimation.HideCompletely();
            SetGameplayUIVisible(false);
            cameraShakeController.enabled = false;

            Vector3 startPosition = gameplayCamera.transform.localPosition;
            float elapsed = 0f;

            while (elapsed < cameraShiftDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / cameraShiftDuration);
                float smoothTime = normalizedTime * normalizedTime * (3f - 2f * normalizedTime);
                gameplayCamera.transform.localPosition = Vector3.LerpUnclamped(startPosition, cameraTargetLocalPosition, smoothTime);
                yield return null;
            }

            gameplayCamera.transform.localPosition = cameraTargetLocalPosition;

            int bottleCount = GetBottleRevealCount();
            EnsureBottleCount(bottleCount);
            SetGameplayUIVisible(true);
            characterUIController.ShowEndlessResultAfterTransition();
            yield return RevealBottles(bottleCount);
            endSequence = null;
        }

        private int GetBottleRevealCount()
        {
            int actualBottleCount = gameManager != null ? Mathf.Max(0, gameManager.potionDrunked) : 0;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (testBottleCountOverride > 0)
            {
                return testBottleCountOverride;
            }
#endif

            return actualBottleCount;
        }

        private IEnumerator WaitForDeathAnimation()
        {
            BaseCharacter character = gameManager != null ? gameManager.Character : null;
            Animator characterAnimator = character != null ? character.animator : null;
            if (characterAnimator == null)
            {
                yield break;
            }

            float elapsed = 0f;
            bool enteredDeathAnimation = false;

            while (elapsed < deathAnimationMaximumWait)
            {
                if (TryGetDeathAnimationState(characterAnimator, out AnimatorStateInfo deathState))
                {
                    enteredDeathAnimation = true;
                    if (deathState.normalizedTime >= deathAnimationCompletion)
                    {
                        break;
                    }
                }
                else if (enteredDeathAnimation && !characterAnimator.IsInTransition(0))
                {
                    break;
                }

                elapsed += Time.unscaledDeltaTime;
                if (!enteredDeathAnimation && elapsed >= deathAnimationStartTimeout)
                {
                    break;
                }

                yield return null;
            }

            if (enteredDeathAnimation && deathAnimationEndPadding > 0f)
            {
                yield return new WaitForSecondsRealtime(deathAnimationEndPadding);
            }
        }

        private static bool TryGetDeathAnimationState(Animator characterAnimator, out AnimatorStateInfo deathState)
        {
            AnimatorClipInfo[] currentClips = characterAnimator.GetCurrentAnimatorClipInfo(0);
            if (ContainsDeathClip(currentClips))
            {
                deathState = characterAnimator.GetCurrentAnimatorStateInfo(0);
                return true;
            }

            if (characterAnimator.IsInTransition(0))
            {
                AnimatorClipInfo[] nextClips = characterAnimator.GetNextAnimatorClipInfo(0);
                if (ContainsDeathClip(nextClips))
                {
                    deathState = characterAnimator.GetNextAnimatorStateInfo(0);
                    return true;
                }
            }

            deathState = default;
            return false;
        }

        private static bool ContainsDeathClip(AnimatorClipInfo[] clips)
        {
            if (clips == null)
            {
                return false;
            }

            for (int index = 0; index < clips.Length; index++)
            {
                AnimationClip clip = clips[index].clip;
                if (clip != null && clip.name.IndexOf("Death", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }

            return false;
        }

        private void SetGameplayUIVisible(bool visible)
        {
            gameplayUI.alpha = visible ? 1f : 0f;
            gameplayUI.interactable = visible;
            gameplayUI.blocksRaycasts = visible;
        }

        private void EnsureBottleCount(int requiredCount)
        {
            int safeCount = Mathf.Max(0, requiredCount);
            while (pooledBottles.Count < safeCount)
            {
                AddPooledBottle();
            }
        }

        private void AddPooledBottle()
        {
            EndlessEmptyBottle prefab = SelectBottlePrefab();
            if (prefab == null || bottleContainer == null)
            {
                return;
            }

            EndlessEmptyBottle bottle = Instantiate(prefab, bottleContainer);
            bottle.gameObject.SetActive(false);
            pooledBottles.Add(bottle);
        }

        private EndlessEmptyBottle SelectBottlePrefab()
        {
            int index = Random.Range(0, 3);
            if (index == 0)
            {
                return smallBottlePrefab;
            }

            return index == 1 ? mediumBottlePrefab : largeBottlePrefab;
        }

        private IEnumerator RevealBottles(int requestedCount)
        {
            int count = Mathf.Min(Mathf.Max(0, requestedCount), pooledBottles.Count);
            float releaseInterval = CalculateBottleReleaseInterval(count);
            int physicsCapacity = Mathf.Max(0, maximumSimulatedBottles);
            Queue<EndlessEmptyBottle> activePhysicsBottles = new Queue<EndlessEmptyBottle>(physicsCapacity);

            for (int index = 0; index < count; index++)
            {
                bool simulatePhysics = physicsCapacity > 0;
                if (simulatePhysics && activePhysicsBottles.Count >= physicsCapacity)
                {
                    EndlessEmptyBottle settledBottle = activePhysicsBottles.Dequeue();
                    settledBottle.FreezePhysics();
                }

                Vector3 position = CreateBottlePosition(simulatePhysics);
                float angle = Random.Range(-180f, 180f);
                Vector2 velocity = simulatePhysics
                    ? new Vector2(Random.Range(horizontalLaunchRange.x, horizontalLaunchRange.y), Random.Range(verticalLaunchRange.x, verticalLaunchRange.y))
                    : Vector2.zero;
                float angularVelocity = simulatePhysics
                    ? Random.Range(angularVelocityRange.x, angularVelocityRange.y)
                    : 0f;

                pooledBottles[index].Release(position, angle, velocity, angularVelocity, simulatePhysics);
                if (simulatePhysics)
                {
                    activePhysicsBottles.Enqueue(pooledBottles[index]);
                }

                PlayBottleActivationSound();

                if (index < count - 1 && releaseInterval > 0f)
                {
                    yield return new WaitForSecondsRealtime(releaseInterval);
                }
            }

            if (bottleAudioSource != null)
            {
                bottleAudioSource.pitch = bottleAudioBasePitch;
            }

            if (freezeBottlesRoutine != null)
            {
                StopCoroutine(freezeBottlesRoutine);
            }

            freezeBottlesRoutine = StartCoroutine(FreezeBottlesAfterDelay(activePhysicsBottles.ToArray()));
        }

        private float CalculateBottleReleaseInterval(int bottleCount)
        {
            if (bottleCount <= 1 || bottleReleaseInterval <= 0f)
            {
                return 0f;
            }

            float interval = bottleReleaseInterval;
            float requestedDuration = interval * (bottleCount - 1);
            if (maximumBottleReleaseDuration > 0f && requestedDuration > maximumBottleReleaseDuration)
            {
                interval = maximumBottleReleaseDuration / (bottleCount - 1);
            }

            if (maximumSimulatedBottles > 0 && physicsDuration > 0f)
            {
                float minimumPhysicsInterval = physicsDuration / maximumSimulatedBottles;
                interval = Mathf.Max(interval, minimumPhysicsInterval);
            }

            return interval;
        }

        private void PlayBottleActivationSound()
        {
            if (bottleAudioSource == null || bottleActivationClip == null)
            {
                return;
            }

            if (Time.unscaledTime < nextBottleSoundTime || Random.value > bottleActivationSoundChance)
            {
                return;
            }

            float minimumPitch = Mathf.Min(bottleActivationPitchMin, bottleActivationPitchMax);
            float maximumPitch = Mathf.Max(bottleActivationPitchMin, bottleActivationPitchMax);
            bottleAudioSource.pitch = Random.Range(minimumPitch, maximumPitch);
            bottleAudioSource.PlayOneShot(bottleActivationClip, bottleActivationVolume);
            nextBottleSoundTime = Time.unscaledTime + bottleActivationSoundCooldown;
        }

        private Vector3 CreateBottlePosition(bool simulatePhysics)
        {
            Bounds dropBounds = bottleDropArea.bounds;
            float x = Random.Range(dropBounds.min.x, dropBounds.max.x);
            float y = simulatePhysics
                ? Random.Range(dropBounds.min.y, dropBounds.max.y)
                : bottlePileGround.bounds.max.y + Random.Range(0f, settledPileHeight);

            return new Vector3(x, y, bottleDropArea.transform.position.z);
        }

        private IEnumerator FreezeBottlesAfterDelay(EndlessEmptyBottle[] activeBottles)
        {
            yield return new WaitForSecondsRealtime(physicsDuration);

            for (int index = 0; index < activeBottles.Length; index++)
            {
                EndlessEmptyBottle bottle = activeBottles[index];
                if (bottle != null)
                {
                    bottle.FreezePhysics();
                }
            }

            freezeBottlesRoutine = null;
        }

        private bool HasRequiredReferences()
        {
            bool valid = gameManager != null
                && characterUIController != null
                && gameplayCamera != null
                && cameraShakeController != null
                && gameplayUI != null
                && grimoireAnimation != null
                && smallBottlePrefab != null
                && mediumBottlePrefab != null
                && largeBottlePrefab != null
                && bottleContainer != null
                && bottleDropArea != null
                && bottlePileGround != null
                && bottleAudioSource != null
                && bottleActivationClip != null;

            if (!valid)
            {
                Debug.LogError($"{name}: Endless end sequence is missing one or more required Inspector references.", this);
            }

            return valid;
        }
    }
}
