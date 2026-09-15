using System.Collections;
using InspectorValidation;
using UnityEngine;

namespace Refactory.LevelObjects
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D), typeof(Animator))]
    [RequireComponent(typeof(AudioSource))]
    public sealed class IceBlock : MonoBehaviour
    {
        private static readonly int BreakTrigger = Animator.StringToHash("Break");

        [Header("References")]
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private SpriteRenderer spriteRenderer;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Collider2D blockCollider;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private Animator animator;
        [SerializeField, RequiredInspectorReference(ResolveMode.Local)] private AudioSource breakAudioSource;

        private bool isBreaking;
        private Sprite intactSprite;

        private void Reset()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            blockCollider = GetComponent<Collider2D>();
            animator = GetComponent<Animator>();
            breakAudioSource = GetComponent<AudioSource>();
        }

        private void Awake()
        {
            ValidateReferences();
            if (spriteRenderer != null)
                intactSprite = spriteRenderer.sprite;
        }

        private void OnEnable()
        {
            isBreaking = false;
            StopAllCoroutines();

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

        private void OnCollisionEnter2D(Collision2D collision)
        {
            if (isBreaking || collision == null || !IsPotion(collision.collider))
                return;

            isBreaking = true;
            blockCollider.enabled = false;
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

        private static bool IsPotion(Collider2D other)
        {
            if (other == null)
                return false;
            if (other.CompareTag("Potion"))
                return true;

            PotionScript potion = other.GetComponentInParent<PotionScript>();
            return potion != null && potion.CompareTag("Potion");
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
                Debug.LogError($"{name}: assign the local SpriteRenderer in IceBlock.", this);
            if (blockCollider == null)
                Debug.LogError($"{name}: assign the local Collider2D in IceBlock.", this);
            if (animator == null)
                Debug.LogError($"{name}: assign the local Animator in IceBlock.", this);
            if (breakAudioSource == null)
                Debug.LogError($"{name}: assign the ice break AudioSource in IceBlock.", this);
        }
    }
}
