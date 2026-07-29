using System.Collections;
using UnityEngine;

namespace CharacterSystem
{
    public class PotionDestroyFeedback : MonoBehaviour
    {
        [Header("Dialog")]
        [SerializeField] private DialogManager dialogManager;
        [SerializeField] private string dialogOnPotionDestroyed = "BURN!";

        [Header("Audio")]
        [SerializeField] private AudioSource audioSource;

        [Header("Blink")]
        [SerializeField] private SpriteRenderer[] blinkRenderers;
        [SerializeField] private Color blinkColor = Color.white;
        [SerializeField] private float blinkDuration = 0.08f;
        [SerializeField] private bool useSolidWhiteBlink = true;

        [Header("Sprite Animation")]
        [SerializeField] private SpriteRenderer animatedRenderer;
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField] private float animationFramesPerSecond = 12f;
        [SerializeField] private bool animateOnEnable = true;

        private Coroutine blinkCoroutine;
        private Coroutine spriteAnimationCoroutine;
        private Color[] rendererBaseColors;
        private Material[] rendererBaseMaterials;
        private static Material solidWhiteBlinkMaterial;

        private void Reset()
        {
            audioSource = GetComponent<AudioSource>();
            blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            animatedRenderer = GetComponent<SpriteRenderer>();
        }

        private void Awake()
        {
            if (animatedRenderer == null)
            {
                animatedRenderer = GetComponent<SpriteRenderer>();
            }

            if (blinkRenderers == null || blinkRenderers.Length == 0)
            {
                blinkRenderers = GetComponentsInChildren<SpriteRenderer>(true);
            }

            if (dialogManager == null)
            {
                dialogManager = GetComponentInParent<DialogManager>();
            }

            DisableLocalSpriteAnimation();
        }

        private void OnEnable()
        {
            DisableLocalSpriteAnimation();
            RestartSpriteAnimation();
        }

        private void OnDisable()
        {
            StopSpriteAnimation();
            RestoreBlinkRenderers();
        }

        public void Play()
        {
            ShowDialog();
            PlayBlink();

            if (audioSource != null)
            {
                audioSource.Play();
            }
        }

        public void RestartSpriteAnimation()
        {
            if (!animateOnEnable || animatedRenderer == null || animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            StopSpriteAnimation();
            spriteAnimationCoroutine = StartCoroutine(SpriteAnimationRoutine());
        }

        private void ShowDialog()
        {
            if (dialogManager != null && !string.IsNullOrEmpty(dialogOnPotionDestroyed))
            {
                dialogManager.PopDialog(dialogOnPotionDestroyed, 3f);
            }
        }

        private void PlayBlink()
        {
            if (blinkRenderers == null || blinkRenderers.Length == 0)
            {
                return;
            }

            if (blinkCoroutine != null)
            {
                StopCoroutine(blinkCoroutine);
                RestoreBlinkRenderers();
            }

            blinkCoroutine = StartCoroutine(BlinkRoutine());
        }

        private IEnumerator BlinkRoutine()
        {
            rendererBaseColors = new Color[blinkRenderers.Length];
            rendererBaseMaterials = new Material[blinkRenderers.Length];

            for (int i = 0; i < blinkRenderers.Length; i++)
            {
                if (blinkRenderers[i] == null)
                {
                    continue;
                }

                rendererBaseColors[i] = blinkRenderers[i].color;
                rendererBaseMaterials[i] = blinkRenderers[i].sharedMaterial;

                blinkRenderers[i].color = blinkColor;

                Material blinkMaterial = GetSolidWhiteBlinkMaterial();
                if (useSolidWhiteBlink && blinkMaterial != null)
                {
                    blinkRenderers[i].sharedMaterial = blinkMaterial;
                }
            }

            yield return new WaitForSeconds(blinkDuration);

            RestoreBlinkRenderers();
            blinkCoroutine = null;
        }

        private void RestoreBlinkRenderers()
        {
            if (blinkRenderers == null || rendererBaseColors == null)
            {
                return;
            }

            int count = Mathf.Min(blinkRenderers.Length, rendererBaseColors.Length);
            for (int i = 0; i < count; i++)
            {
                if (blinkRenderers[i] == null)
                {
                    continue;
                }

                blinkRenderers[i].color = rendererBaseColors[i];

                if (rendererBaseMaterials != null && i < rendererBaseMaterials.Length && rendererBaseMaterials[i] != null)
                {
                    blinkRenderers[i].sharedMaterial = rendererBaseMaterials[i];
                }
            }
        }

        private static Material GetSolidWhiteBlinkMaterial()
        {
            if (solidWhiteBlinkMaterial != null)
            {
                return solidWhiteBlinkMaterial;
            }

            Shader shader = Shader.Find("PotionLab/Sprite Solid White");
            if (shader == null)
            {
                return null;
            }

            solidWhiteBlinkMaterial = new Material(shader)
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            solidWhiteBlinkMaterial.color = Color.white;
            return solidWhiteBlinkMaterial;
        }

        private void StopSpriteAnimation()
        {
            if (spriteAnimationCoroutine != null)
            {
                StopCoroutine(spriteAnimationCoroutine);
                spriteAnimationCoroutine = null;
            }
        }

        private void DisableLocalSpriteAnimation()
        {
            if (animationFrames == null || animationFrames.Length == 0)
            {
                return;
            }

            Animation localAnimation = GetComponent<Animation>();
            if (localAnimation != null)
            {
                localAnimation.Stop();
                localAnimation.enabled = false;
            }
        }

        private IEnumerator SpriteAnimationRoutine()
        {
            int frameIndex = 0;
            float frameDelay = animationFramesPerSecond > 0f ? 1f / animationFramesPerSecond : 0.083333336f;

            while (true)
            {
                Sprite frame = animationFrames[frameIndex];
                if (frame != null)
                {
                    animatedRenderer.sprite = frame;
                }

                frameIndex = (frameIndex + 1) % animationFrames.Length;
                yield return new WaitForSeconds(frameDelay);
            }
        }
    }
}
