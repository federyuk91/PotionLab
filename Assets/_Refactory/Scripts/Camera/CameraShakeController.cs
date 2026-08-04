using System.Collections;
using CharacterSystem;
using UnityEngine;

namespace Refactory.CameraSystem
{
    public class CameraShakeController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform shakeTarget;
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private CharacterStatusController statusController;

        [Header("Damage Shake")]
        [SerializeField] private float damageDuration = 0.15f;
        [SerializeField] private float damageMagnitude = 1f;

        [Header("Explosion Shake")]
        [SerializeField] private float explosionDuration = 0.25f;
        [SerializeField] private float explosionMagnitude = 1.5f;

        private Coroutine shakeRoutine;
        private Vector3 restLocalPosition;

        private void Awake()
        {
            if (shakeTarget == null)
            {
                Debug.LogWarning($"{name}: Shake Target reference is missing. Using this transform as local fallback; assign it in Inspector before production.", this);
                shakeTarget = transform;
            }

            restLocalPosition = shakeTarget.localPosition;
        }

        private void OnEnable()
        {
            if (characterStats == null)
            {
                Debug.LogWarning($"{name}: Character Stats reference is missing. Assign it in Inspector to enable damage camera shake.", this);
            }
            else
            {
                characterStats.DamageTaken += OnDamageTaken;
            }

            if (statusController == null)
            {
                Debug.LogWarning($"{name}: Character Status Controller reference is missing. Assign it in Inspector to enable explosion camera shake.", this);
            }
            else
            {
                statusController.OnExplosion += OnExplosion;
            }
        }

        private void OnDisable()
        {
            if (characterStats != null)
            {
                characterStats.DamageTaken -= OnDamageTaken;
            }

            if (statusController != null)
            {
                statusController.OnExplosion -= OnExplosion;
            }

            StopShake();
        }

        public void Shake(float duration, float magnitude)
        {
            if (!isActiveAndEnabled || shakeTarget == null)
            {
                return;
            }

            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeTarget.localPosition = restLocalPosition;
            }

            shakeRoutine = StartCoroutine(ShakeRoutine(duration, magnitude));
        }

        private void OnDamageTaken(int damage)
        {
            if (damage <= 0)
            {
                return;
            }

            Shake(damageDuration, damageMagnitude);
        }

        private void OnExplosion()
        {
            Shake(explosionDuration, explosionMagnitude);
        }

        private IEnumerator ShakeRoutine(float duration, float magnitude)
        {
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float x = Random.Range(-1f, 1f) * magnitude;
                float y = Random.Range(-1f, 1f) * magnitude;
                shakeTarget.localPosition = restLocalPosition + new Vector3(x, y, 0f);

                elapsed += Time.deltaTime;
                yield return null;
            }

            shakeTarget.localPosition = restLocalPosition;
            shakeRoutine = null;
        }

        private void StopShake()
        {
            if (shakeRoutine != null)
            {
                StopCoroutine(shakeRoutine);
                shakeRoutine = null;
            }

            if (shakeTarget != null)
            {
                shakeTarget.localPosition = restLocalPosition;
            }
        }
    }
}
