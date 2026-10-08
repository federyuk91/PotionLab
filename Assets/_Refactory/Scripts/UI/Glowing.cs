using InspectorValidation;
using UnityEngine;
using UnityEngine.UI;

namespace Refactory.UI
{
    public class Glowing : Graphic
    {
        [Header("Shader")]
        [SerializeField, RequiredInspectorReference] private Material glowMaterial;

        [Header("Glow")]
        [SerializeField] private bool glowActive;
        [SerializeField] private Color imageColor = Color.white;

        private Image displayImage;
        private bool glowVisible;
        private float visibilityCheckElapsed;
        private bool missingDisplayImageWarningShown;
        private bool missingMaterialWarningShown;

        protected override void Awake()
        {
            base.Awake();
            EnableSecondUvChannel();
            ResolveDisplayImage();
            ApplyVisualState();
        }

        protected override void OnEnable()
        {
            base.OnEnable();
            EnableSecondUvChannel();
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
            float seed = CalculateSeed();

            AddVertex(vertexHelper, new Vector3(minimum.x, minimum.y), new Vector2(0f, 0f), seed);
            AddVertex(vertexHelper, new Vector3(minimum.x, maximum.y), new Vector2(0f, 1f), seed);
            AddVertex(vertexHelper, new Vector3(maximum.x, maximum.y), new Vector2(1f, 1f), seed);
            AddVertex(vertexHelper, new Vector3(maximum.x, minimum.y), new Vector2(1f, 0f), seed);
            vertexHelper.AddTriangle(0, 1, 2);
            vertexHelper.AddTriangle(2, 3, 0);
        }

        private static void AddVertex(VertexHelper vertexHelper, Vector3 position, Vector2 uv, float seed)
        {
            UIVertex vertex = UIVertex.simpleVert;
            vertex.position = position;
            vertex.color = Color.white;
            vertex.uv0 = uv;
            vertex.uv1 = new Vector4(seed, 0f, 0f, 0f);
            vertexHelper.AddVert(vertex);
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

        private void EnableSecondUvChannel()
        {
            Canvas parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas == null)
            {
                return;
            }

            parentCanvas.additionalShaderChannels |= AdditionalCanvasShaderChannels.TexCoord1;
        }

        private float CalculateSeed()
        {
            float value = (transform.GetSiblingIndex() + 1) * 12.9898f;
            return Mathf.Repeat(Mathf.Sin(value) * 43758.5453f, 1f);
        }
    }
}
