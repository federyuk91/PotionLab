using InspectorValidation;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI
{
    public class Glowing : Graphic
    {
        private static readonly int RayColorId = Shader.PropertyToID("_RayColor");
        private static readonly int RayCountId = Shader.PropertyToID("_RayCount");
        private static readonly int RotationSpeedId = Shader.PropertyToID("_RotationSpeed");
        private static readonly int MinimumPulseId = Shader.PropertyToID("_MinimumPulse");
        private static readonly int PulseAmplitudeId = Shader.PropertyToID("_PulseAmplitude");
        private static readonly int PulseSpeedId = Shader.PropertyToID("_PulseSpeed");
        private static readonly int InnerRadiusRatioId = Shader.PropertyToID("_InnerRadiusRatio");
        private static readonly int ShortRayHalfAngleId = Shader.PropertyToID("_ShortRayHalfAngle");
        private static readonly int LongRayHalfAngleId = Shader.PropertyToID("_LongRayHalfAngle");

        [Header("Shader")]
        [SerializeField, RequiredInspectorReference] private Material glowMaterial;

        [Header("Glow")]
        [SerializeField] private bool glowActive;
        [SerializeField] private Color imageColor = Color.white;
        [SerializeField] private Color rayColor = new Color(1f, 0.72f, 0.16f, 1f);
        [SerializeField, Min(4)] private int rayCount = 12;
        [SerializeField, Min(0f)] private float rotationDegreesPerSecond = -20f;
        [SerializeField, Range(0f, 1f)] private float minimumPulse = 0.58f;
        [SerializeField, Range(0f, 1f)] private float pulseAmplitude = 0.32f;
        [SerializeField, Min(0f)] private float pulseSpeed = 2.4f;
        [SerializeField, Range(0f, 1f)] private float innerRadiusRatio = 0.24f;
        [SerializeField, Range(0f, 0.5f)] private float shortRayHalfAngle = 0.055f;
        [SerializeField, Range(0f, 0.5f)] private float longRayHalfAngle = 0.17f;

        private Image displayImage;
        private bool glowVisible;
        private float visibilityCheckElapsed;
        private bool missingDisplayImageWarningShown;
        private bool missingMaterialWarningShown;

        protected override void Awake()
        {
            base.Awake();
            ResolveDisplayImage();
            ApplyVisualState();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            ResolveDisplayImage();
            ApplyVisualState();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            ResolveDisplayImage();
            ApplyVisualState();
        }

        public void SetGlowActive(bool active)
        {
            glowActive = active;
            UpdateGlowVisibility();
        }

        private void Update()
        {
            visibilityCheckElapsed += Time.unscaledDeltaTime;
            if (visibilityCheckElapsed < 0.1f)
            {
                return;
            }

            visibilityCheckElapsed = 0f;
            UpdateGlowVisibility();
        }

        protected override void OnPopulateMesh(VertexHelper vertexHelper)
        {
            vertexHelper.Clear();
            if (!glowVisible || displayImage == null)
            {
                return;
            }

            Rect displayRect = displayImage.rectTransform.rect;
            Vector2 center = displayImage.rectTransform.anchoredPosition;
            float outerRadius = Mathf.Min(displayRect.width, displayRect.height) * 0.95f;
            Vector2 minimum = center - Vector2.one * outerRadius;
            Vector2 maximum = center + Vector2.one * outerRadius;

            vertexHelper.AddVert(new Vector3(minimum.x, minimum.y), Color.white, new Vector2(0f, 0f));
            vertexHelper.AddVert(new Vector3(minimum.x, maximum.y), Color.white, new Vector2(0f, 1f));
            vertexHelper.AddVert(new Vector3(maximum.x, maximum.y), Color.white, new Vector2(1f, 1f));
            vertexHelper.AddVert(new Vector3(maximum.x, minimum.y), Color.white, new Vector2(1f, 0f));
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        private void ApplyVisualState()
        {
            color = Color.white;
            raycastTarget = false;

            if (displayImage != null)
            {
                displayImage.color = imageColor;
            }

            if (glowMaterial == null)
            {
                if (Application.isPlaying && !missingMaterialWarningShown)
                {
                    missingMaterialWarningShown = true;
                    Debug.LogWarning($"{name}: Glow Material is missing. Assign the Glowing Rays material.", this);
                }

                return;
            }

            material = glowMaterial;
            glowMaterial.SetColor(RayColorId, rayColor);
            glowMaterial.SetFloat(RayCountId, rayCount);
            glowMaterial.SetFloat(RotationSpeedId, rotationDegreesPerSecond * Mathf.Deg2Rad);
            glowMaterial.SetFloat(MinimumPulseId, minimumPulse);
            glowMaterial.SetFloat(PulseAmplitudeId, pulseAmplitude);
            glowMaterial.SetFloat(PulseSpeedId, pulseSpeed);
            glowMaterial.SetFloat(InnerRadiusRatioId, innerRadiusRatio);
            glowMaterial.SetFloat(ShortRayHalfAngleId, shortRayHalfAngle);
            glowMaterial.SetFloat(LongRayHalfAngleId, longRayHalfAngle);
            UpdateGlowVisibility();
        }

        private void UpdateGlowVisibility()
        {
            bool shouldBeVisible = glowActive && displayImage != null && displayImage.isActiveAndEnabled;
            if (glowVisible == shouldBeVisible)
            {
                return;
            }

            glowVisible = shouldBeVisible;
            SetVerticesDirty();
        }

        private void ResolveDisplayImage()
        {
            if (displayImage != null)
            {
                return;
            }

            Button achievementButton = GetComponentInChildren<Button>(true);
            if (achievementButton != null)
            {
                Image[] images = achievementButton.GetComponentsInChildren<Image>(true);
                foreach (Image image in images)
                {
                    if (image != achievementButton.targetGraphic)
                    {
                        displayImage = image;
                        return;
                    }
                }
            }

            if (Application.isPlaying && !missingDisplayImageWarningShown)
            {
                missingDisplayImageWarningShown = true;
                Debug.LogWarning($"{name}: a child AchievementButton with an AchievementImageOrLock Image is required.", this);
            }
        }
    }
}
