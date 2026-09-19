using InspectorValidation;
using UnityEngine;

namespace CharacterSystem
{
    [DisallowMultipleComponent]
    public sealed class MageRandomIdleController : MonoBehaviour
    {
        private static readonly int IdleStateHash = Animator.StringToHash("Anim_Mage_Idle");
        private static readonly int IdleStateFullPathHash = Animator.StringToHash("Base Layer.Anim_Mage_Idle");

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Animator animator;
        [SerializeField, RequiredInspectorReference] private AnimationClip idleClip;

        [Header("Random Idle")]
        [SerializeField, Range(0f, 1f)] private float specialIdleChance = 0.15f;
        [SerializeField, Min(2)] private int idleVariantCount = 9;

        private float activeVariantStart;
        private float variantNormalizedLength;
        private bool wasPlayingIdle;
        private bool referencesValid;

        private void Awake()
        {
            referencesValid = ValidateReferences();
            if (!referencesValid)
            {
                enabled = false;
                return;
            }

            variantNormalizedLength = 1f / idleVariantCount;
        }

        private void OnEnable()
        {
            wasPlayingIdle = false;
            activeVariantStart = 0f;
        }

        private void LateUpdate()
        {
            if (!referencesValid || animator.IsInTransition(0))
            {
                wasPlayingIdle = false;
                return;
            }

            AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);
            if (stateInfo.shortNameHash != IdleStateHash)
            {
                wasPlayingIdle = false;
                return;
            }

            if (!wasPlayingIdle)
            {
                wasPlayingIdle = true;
                PlayVariant(0);
                return;
            }

            float elapsedNormalizedTime = stateInfo.normalizedTime - activeVariantStart;
            if (elapsedNormalizedTime < variantNormalizedLength)
            {
                return;
            }

            int nextVariant = Random.value < specialIdleChance
                ? Random.Range(1, idleVariantCount)
                : 0;
            PlayVariant(nextVariant);
        }

        private void PlayVariant(int variantIndex)
        {
            int clampedVariant = Mathf.Clamp(variantIndex, 0, idleVariantCount - 1);
            activeVariantStart = clampedVariant * variantNormalizedLength;
            animator.Play(IdleStateFullPathHash, 0, activeVariantStart);

            // Apply the selected sprite segment before rendering this frame.
            animator.Update(0f);
        }

        private bool ValidateReferences()
        {
            bool valid = true;
            if (animator == null)
            {
                Debug.LogError($"{name}: assign Animator in MageRandomIdleController.", this);
                valid = false;
            }

            if (idleClip == null)
            {
                Debug.LogError($"{name}: assign the complete Mage idle clip in MageRandomIdleController.", this);
                valid = false;
            }

            if (idleVariantCount < 2)
            {
                Debug.LogError($"{name}: MageRandomIdleController requires at least two idle variants.", this);
                valid = false;
            }

            return valid;
        }
    }
}
