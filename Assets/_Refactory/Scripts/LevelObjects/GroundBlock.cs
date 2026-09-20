using System.Collections;
using CharacterSystem;
using InspectorValidation;
using UnityEngine;

namespace Refactory.LevelObjects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    [RequireComponent(typeof(Rigidbody2D))]
    public sealed class GroundBlock : DroppableObject
    {
        private static readonly int BreakTrigger = Animator.StringToHash("Break");

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private SpriteRenderer spriteRenderer;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Collider2D blockCollider;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Animator animator;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private AudioSource breakAudioSource;
        [SerializeField, Min(1)] private int characterDamage = 1;

        private bool isBreaking;
        private Sprite intactSprite;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            blockCollider = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();
            breakAudioSource = GetComponent<AudioSource>();
        }

        protected override void Awake()
        {
            base.Awake();
            ValidateReferences();
            if (spriteRenderer != null)
                intactSprite = spriteRenderer.sprite;
        }

        private void OnEnable()
        {
            isBreaking = false;
            StopAllCoroutines();
            ResetRuntimeState();

            if (spriteRenderer != null)
                spriteRenderer.enabled = true;

            if (blockCollider != null)
                blockCollider.enabled = true;

            if (breakAudioSource != null)
                breakAudioSource.Stop();

            if (animator != null)
            {
                animator.Rebind();
                animator.Update(0f);
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.sprite = intactSprite;
                spriteRenderer.flipX = Random.value >= 0.5f;
                spriteRenderer.flipY = Random.value >= 0.5f;
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (isBreaking || other == null)
            {
                return;
            }

            DrinkingTrigger drinkingTrigger = other.GetComponentInParent<DrinkingTrigger>();
            if (drinkingTrigger == null)
            {
                return;
            }

            drinkingTrigger.TryDamageCurrentCharacter(characterDamage);
            Break();
        }

        public void BreakFromYetiPunch()
        {
            Break();
        }

        private void Break()
        {
            if (isBreaking)
                return;

            isBreaking = true;
            animator.ResetTrigger(BreakTrigger);
            animator.SetTrigger(BreakTrigger);
            breakAudioSource.Play();
        }

        // Called by the final frame of Anim_IceBlock_Break.
        public void CompleteBreak()
        {
            if (!isBreaking)
                return;

            spriteRenderer.enabled = false;
            if (breakAudioSource.isPlaying)
            {
                StartCoroutine(DeactivateAfterAudio());
                return;
            }

            gameObject.SetActive(false);
        }

        private IEnumerator DeactivateAfterAudio()
        {
            while (breakAudioSource.isPlaying)
                yield return null;

            gameObject.SetActive(false);
        }

        private void ValidateReferences()
        {
            if (spriteRenderer == null)
                Debug.LogError($"{name}: assign the local SpriteRenderer in groundBlock.", this);
            if (blockCollider == null)
                Debug.LogError($"{name}: assign the local Collider2D in groundBlock.", this);
            if (animator == null)
                Debug.LogError($"{name}: assign the local Animator in groundBlock.", this);
            if (breakAudioSource == null)
                Debug.LogError($"{name}: assign the ice break AudioSource in groundBlock.", this);
        }
    }
}
