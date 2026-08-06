using CharacterSystem;
using UnityEngine;

namespace EndlessSystem
{
    public class EndlessEventController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LightController lightController;
        [SerializeField] private DialogManager dialogManager;
        [SerializeField] private AudioSource powerDownAudioSource;

        [Header("Platforms")]
        [SerializeField] private SurfaceEffector2D bottomPlatformNear;
        [SerializeField] private SurfaceEffector2D bottomPlatformFar;
        [SerializeField] private SurfaceEffector2D upperPlatform;

        [Header("Feedback")]
        [SerializeField] private SpriteRenderer imageSpriteUp;
        [SerializeField] private SpriteRenderer imageSpriteDown;
        [SerializeField] private Sprite[] velocityImages;

        [Header("Scene Events")]
        [SerializeField] private GameObject[] obstacles;
        [SerializeField] private Transform[] familiars;

        private GameObject currentObstacle;
        private bool missingLightControllerWarningShown;
        private bool missingDialogManagerWarningShown;

        public void StartEvent(EndlessEventType eventType, float value)
        {
            switch (eventType)
            {
                case EndlessEventType.None:
                    break;
                case EndlessEventType.LightVariation:
                    SetSceneLightLevel(Mathf.RoundToInt(value));
                    break;
                case EndlessEventType.Obstacle:
                    RandomObstacle();
                    break;
                case EndlessEventType.ChangeSpeedBottomSlider:
                    SetBottomPlatformSpeed(value);
                    break;
                case EndlessEventType.ChangeSpeedUpperSlider:
                    SetUpperPlatformSpeed(value);
                    break;
                case EndlessEventType.SpawnFamiliar:
                    SpawnFamiliar();
                    break;
                case EndlessEventType.Bomb:
                    SpawnFamiliarBomb();
                    break;
            }
        }

        public void SetSceneLightLevel(int intensity)
        {
            if (lightController == null)
            {
                WarnMissingLightController();
                return;
            }

            int clampedIntensity = Mathf.Max(0, intensity);
            lightController.SetLightLevel(clampedIntensity);

            if (powerDownAudioSource != null)
            {
                powerDownAudioSource.Play();
            }

            if (intensity < 0)
            {
                PopDialog("I need my power back! HURRY!");
                return;
            }

            PopDialog("My power is low!");
        }

        public void SetUpperPlatformSpeed(float speed)
        {
            if (upperPlatform != null)
            {
                upperPlatform.speed = speed;
            }

            UpdateVelocitySprite(imageSpriteUp, speed, false);
        }

        public void SetBottomPlatformSpeed(float speed)
        {
            if (bottomPlatformFar != null)
            {
                bottomPlatformFar.speed = speed;
            }

            if (bottomPlatformNear != null)
            {
                bottomPlatformNear.speed = speed;
            }

            UpdateVelocitySprite(imageSpriteDown, speed, true);
        }

        public void RandomObstacle()
        {
            if (obstacles == null || obstacles.Length == 0)
            {
                Debug.LogWarning($"{name}: Endless obstacle event requested, but no obstacles are assigned in Inspector.", this);
                return;
            }

            if (currentObstacle != null)
            {
                currentObstacle.SetActive(false);
            }

            int index = Random.Range(0, obstacles.Length);
            currentObstacle = obstacles[index];

            if (currentObstacle != null)
            {
                currentObstacle.SetActive(true);
            }
        }

        public void SpawnFamiliar()
        {
            if (familiars == null || familiars.Length == 0)
            {
                Debug.LogWarning($"{name}: Endless familiar event requested, but no familiars are assigned in Inspector.", this);
                return;
            }

            int randomIndex = Random.Range(0, familiars.Length);

            for (int index = 0; index < familiars.Length; index++)
            {
                if (familiars[index] != null)
                {
                    familiars[index].gameObject.SetActive(index == randomIndex);
                }
            }
        }

        public void SpawnFamiliarBomb()
        {
            const int LegacyBombIndex = 1;

            if (familiars == null || familiars.Length <= LegacyBombIndex || familiars[LegacyBombIndex] == null)
            {
                Debug.LogWarning($"{name}: Endless bomb event requested, but familiar index 1 is not assigned in Inspector.", this);
                return;
            }

            familiars[LegacyBombIndex].gameObject.SetActive(true);
        }

        private void UpdateVelocitySprite(SpriteRenderer target, float speed, bool invertPositiveDirection)
        {
            if (target == null || velocityImages == null || velocityImages.Length == 0)
            {
                return;
            }

            target.flipX = speed < 0f ? !invertPositiveDirection : invertPositiveDirection;

            float absoluteSpeed = Mathf.Abs(speed);
            int spriteIndex = 0;

            if (absoluteSpeed >= 9f)
            {
                spriteIndex = 2;
            }
            else if (absoluteSpeed >= 6f)
            {
                spriteIndex = 1;
            }

            if (spriteIndex < velocityImages.Length && velocityImages[spriteIndex] != null)
            {
                target.sprite = velocityImages[spriteIndex];
            }
        }

        private void PopDialog(string line)
        {
            if (dialogManager == null)
            {
                WarnMissingDialogManager();
                return;
            }

            dialogManager.PopDialog(line, 2f);
        }

        private void WarnMissingLightController()
        {
            if (missingLightControllerWarningShown)
            {
                return;
            }

            missingLightControllerWarningShown = true;
            Debug.LogWarning($"{name}: LightController reference is missing. Assign it in Inspector to allow endless light events.", this);
        }

        private void WarnMissingDialogManager()
        {
            if (missingDialogManagerWarningShown)
            {
                return;
            }

            missingDialogManagerWarningShown = true;
            Debug.LogWarning($"{name}: DialogManager reference is missing. Assign it in Inspector to show endless event dialogs.", this);
        }
    }
}
