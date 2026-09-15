using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class SpriteToolboxLightPadController : ILightPadState, IDisposable
    {
        private const int DefaultCameraCaptureSize = 512;
        private const int MaximumReferenceCapturePixels = 512 * 512;
        private readonly SpriteToolboxLightPadSettings settings;
        private readonly SpriteToolboxLightPadEditorState editorState;
        private Texture2D snapshotTexture;
        private string status = "Select a source, then refresh the LightPad.";
        private SceneView capturedSceneView;

        internal SpriteToolboxLightPadSettings Settings => settings;
        internal Texture ReferenceTexture => settings.SourceType == SpriteLightPadSourceType.Texture ? settings.SourceTexture : snapshotTexture;
        internal string Status => status;
        internal SceneView CapturedSceneView => capturedSceneView;
        bool ILightPadState.IsSelected => editorState.IsSelected;
        bool ILightPadState.IsEnabled => settings.IsEnabled;
        bool ILightPadState.HasReferenceCapture => ReferenceTexture != null;
        SpriteLightPadSourceType ILightPadState.SourceType => settings.SourceType;
        Texture ILightPadState.SourceTexture => settings.SourceTexture;
        Camera ILightPadState.SourceCamera => settings.SourceCamera;
        float ILightPadState.Opacity => settings.Opacity;
        SpriteLightPadAlignment ILightPadState.Alignment => settings.Alignment;
        float ILightPadState.ReferenceZoom => settings.ReferenceZoom;
        SpriteLightPadSamplingMode ILightPadState.SamplingMode => settings.SamplingMode;
        bool ILightPadState.UseDithering => settings.UseDithering;
        string ILightPadState.Status => status;

        public SpriteToolboxLightPadController(SpriteToolboxLightPadSettings settings, SpriteToolboxLightPadEditorState editorState)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.editorState = editorState ?? throw new ArgumentNullException(nameof(editorState));
            this.settings.Opacity = Mathf.Clamp01(this.settings.Opacity);
        }

        public void Toggle()
        {
            settings.IsEnabled = !settings.IsEnabled;
            if (settings.IsEnabled)
            {
                Refresh();
            }
        }

        public void Enable()
        {
            if (settings.IsEnabled)
            {
                return;
            }

            settings.IsEnabled = true;
            Refresh();
        }

        public void MoveReference(Vector2 viewSpaceDelta)
        {
            if (settings.SourceType == SpriteLightPadSourceType.SceneView || viewSpaceDelta == Vector2.zero)
            {
                return;
            }

            settings.PositionOffset += viewSpaceDelta;
        }

        public void ResetReferencePosition()
        {
            settings.PositionOffset = Vector2.zero;
        }

        public void SetEnabled(bool value)
        {
            if (settings.IsEnabled == value)
            {
                return;
            }

            settings.IsEnabled = value;
            if (value)
            {
                Refresh();
            }
        }

        public void SetSourceType(SpriteLightPadSourceType value)
        {
            if (settings.SourceType == value)
            {
                return;
            }

            settings.SourceType = value;
            capturedSceneView = null;
            Refresh();
        }

        public void SetSourceTexture(Texture value)
        {
            if (settings.SourceTexture == value)
            {
                return;
            }

            settings.SourceTexture = value;
            Refresh();
        }

        public void SetSourceCamera(Camera value)
        {
            if (settings.SourceCamera == value)
            {
                return;
            }

            settings.SourceCamera = value;
            Refresh();
        }

        public void SetOpacity(float value)
        {
            settings.Opacity = Mathf.Clamp01(value);
        }

        public void SetAlignment(SpriteLightPadAlignment value)
        {
            settings.Alignment = value;
        }

        public void SetReferenceZoom(float value)
        {
            settings.ReferenceZoom = Mathf.Clamp(value, 0.01f, 2f);
        }

        public void SetSamplingMode(SpriteLightPadSamplingMode value)
        {
            settings.SamplingMode = value;
        }

        public void SetDithering(bool value)
        {
            settings.UseDithering = value;
        }


        public void ClearSnapshot(string message)
        {
            if (snapshotTexture != null)
            {
                UnityEngine.Object.DestroyImmediate(snapshotTexture);
                snapshotTexture = null;
            }

            status = message;
        }

        public void Refresh()
        {
            try
            {
                bool captured = false;
                switch (settings.SourceType)
                {
                    case SpriteLightPadSourceType.Texture:
                        if (settings.SourceTexture == null)
                        {
                            ClearSnapshot("LightPad needs a Texture2D or RenderTexture source.");
                        }
                        else
                        {
                            ClearSnapshot(string.Empty);
                            captured = true;
                        }
                        break;
                    case SpriteLightPadSourceType.Camera:
                        captured = CaptureCamera(settings.SourceCamera, "The selected Camera is missing or invalid.");
                        break;
                    case SpriteLightPadSourceType.SceneView:
                        SceneView sceneView = SceneView.lastActiveSceneView;
                        if (sceneView == null)
                        {
                            ClearSnapshot("Open and focus a Scene View before refreshing the LightPad.");
                        }
                        else
                        {
                            captured = CaptureSceneView(sceneView);
                        }
                        break;
                    default:
                        ClearSnapshot("Unsupported LightPad source.");
                        break;
                }

                if (captured)
                {
                    status = settings.SourceType == SpriteLightPadSourceType.Texture
                        ? "Texture reference is used directly."
                        : "Reference captured. It remains frozen until Refresh is pressed.";
                }
            }
            catch (Exception exception)
            {
                ClearSnapshot("LightPad capture failed: " + exception.Message);
            }
        }

        internal void RefreshSceneView(SceneView sceneView)
        {
            if (settings.SourceType != SpriteLightPadSourceType.SceneView || sceneView == null)
            {
                return;
            }

            try
            {
                if (CaptureSceneView(sceneView))
                {
                    status = "Reference captured. It remains frozen until the Scene View changes.";
                }
            }
            catch (Exception exception)
            {
                ClearSnapshot("LightPad capture failed: " + exception.Message);
            }
        }

        public void Dispose()
        {
            ClearSnapshot(string.Empty);
        }

        internal bool TryCaptureReferenceForDocument(
            int documentWidth,
            int documentHeight,
            out Color32[] pixels,
            out int captureWidth,
            out int captureHeight,
            out string error)
        {
            pixels = null;
            captureWidth = 0;
            captureHeight = 0;
            error = null;
            Texture referenceTexture = ReferenceTexture;
            if (referenceTexture == null)
            {
                error = "Capture a LightPad reference before importing it.";
                return false;
            }

            if (documentWidth < 1 || documentHeight < 1)
            {
                error = "The drawing area is not ready for a reference capture.";
                return false;
            }

            Rect documentRect = new Rect(0f, 0f, documentWidth, documentHeight);
            Rect destinationRect = SpriteToolboxLightPadLayout.CalculateDestinationRect(documentRect, referenceTexture.width, referenceTexture.height, settings.Alignment, settings.ReferenceZoom);
            destinationRect.position += settings.PositionOffset;
            if (!destinationRect.Overlaps(documentRect))
            {
                error = "The LightPad reference does not overlap the drawing area.";
                return false;
            }

            float sourceScale = Mathf.Max(referenceTexture.width / destinationRect.width, referenceTexture.height / destinationRect.height);
            float minimumScale = 1f;
            float maximumScale = Mathf.Sqrt(MaximumReferenceCapturePixels / (documentWidth * documentHeight));
            float captureScale = Mathf.Clamp(Mathf.Max(sourceScale, minimumScale), minimumScale, Mathf.Max(minimumScale, maximumScale));
            captureWidth = Mathf.Max(documentWidth, Mathf.RoundToInt(documentWidth * captureScale));
            captureHeight = Mathf.Max(documentHeight, Mathf.RoundToInt(documentHeight * captureScale));
            pixels = new Color32[captureWidth * captureHeight];

            Texture2D readableTexture = GetReadableTexture(referenceTexture, out bool ownsReadableTexture);
            try
            {
                for (int pixelY = 0; pixelY < captureHeight; pixelY++)
                {
                    float documentY = (pixelY + 0.5f) * documentHeight / captureHeight;
                    for (int pixelX = 0; pixelX < captureWidth; pixelX++)
                    {
                        float documentX = (pixelX + 0.5f) * documentWidth / captureWidth;
                        float normalizedX = (documentX - destinationRect.xMin) / destinationRect.width;
                        float normalizedY = (documentY - destinationRect.yMin) / destinationRect.height;
                        if (normalizedX < 0f || normalizedX > 1f || normalizedY < 0f || normalizedY > 1f)
                        {
                            continue;
                        }

                        pixels[pixelY * captureWidth + pixelX] = SampleReference(readableTexture, normalizedX, normalizedY, captureWidth, captureHeight);
                    }
                }
            }
            finally
            {
                if (ownsReadableTexture)
                {
                    UnityEngine.Object.DestroyImmediate(readableTexture);
                }
            }

            return true;
        }

        private Color32 SampleReference(Texture2D texture, float normalizedX, float normalizedY, int captureWidth, int captureHeight)
        {
            // The document and LightPad layout both use bottom-origin pixel coordinates.
            // Inverting here mirrors the imported reference across the horizontal axis.
            float textureY = normalizedY;
            if (settings.SamplingMode == SpriteLightPadSamplingMode.Nearest)
            {
                int pixelX = Mathf.Clamp(Mathf.FloorToInt(normalizedX * texture.width), 0, texture.width - 1);
                int pixelY = Mathf.Clamp(Mathf.FloorToInt(textureY * texture.height), 0, texture.height - 1);
                return texture.GetPixel(pixelX, pixelY);
            }

            int sampleCountX = Mathf.Clamp(Mathf.CeilToInt(texture.width / (float)captureWidth), 1, 4);
            int sampleCountY = Mathf.Clamp(Mathf.CeilToInt(texture.height / (float)captureHeight), 1, 4);
            Color accumulated = Color.clear;
            for (int sampleY = 0; sampleY < sampleCountY; sampleY++)
            {
                float sampleOffsetY = ((sampleY + 0.5f) / sampleCountY - 0.5f) / captureHeight;
                for (int sampleX = 0; sampleX < sampleCountX; sampleX++)
                {
                    float sampleOffsetX = ((sampleX + 0.5f) / sampleCountX - 0.5f) / captureWidth;
                    accumulated += texture.GetPixelBilinear(Mathf.Clamp01(normalizedX + sampleOffsetX), Mathf.Clamp01(textureY + sampleOffsetY));
                }
            }

            return accumulated / (sampleCountX * sampleCountY);
        }

        private bool CaptureCamera(Camera camera, string invalidMessage)
        {
            if (camera == null)
            {
                ClearSnapshot(invalidMessage);
                return false;
            }

            if (camera.targetTexture != null)
            {
                CaptureTextureToSnapshot(camera.targetTexture);
                return true;
            }

            int width = camera.pixelWidth > 0 ? camera.pixelWidth : DefaultCameraCaptureSize;
            int height = camera.pixelHeight > 0 ? camera.pixelHeight : DefaultCameraCaptureSize;
            RenderTexture temporaryTarget = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            GameObject temporaryCameraObject = new GameObject("SpriteToolbox LightPad Capture Camera") { hideFlags = HideFlags.HideAndDontSave };
            Camera temporaryCamera = temporaryCameraObject.AddComponent<Camera>();
            try
            {
                temporaryCamera.CopyFrom(camera);
                temporaryCamera.enabled = false;
                temporaryCamera.targetTexture = temporaryTarget;
                temporaryCamera.Render();
                ReadRenderTextureIntoSnapshot(temporaryTarget);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(temporaryCameraObject);
                RenderTexture.ReleaseTemporary(temporaryTarget);
            }

            return true;
        }

        private bool CaptureSceneView(SceneView sceneView)
        {
            Camera sceneViewCamera = sceneView.camera;
            if (sceneViewCamera == null)
            {
                ClearSnapshot("The active Scene View camera is unavailable.");
                return false;
            }

            int width = sceneViewCamera.pixelWidth > 0 ? sceneViewCamera.pixelWidth : DefaultCameraCaptureSize;
            int height = sceneViewCamera.pixelHeight > 0 ? sceneViewCamera.pixelHeight : DefaultCameraCaptureSize;
            RenderTexture temporaryTarget = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previousTarget = sceneViewCamera.targetTexture;
            try
            {
                sceneViewCamera.targetTexture = temporaryTarget;
                sceneViewCamera.Render();
                ReadRenderTextureIntoSnapshot(temporaryTarget);
                capturedSceneView = sceneView;
                return true;
            }
            finally
            {
                sceneViewCamera.targetTexture = previousTarget;
                RenderTexture.ReleaseTemporary(temporaryTarget);
                sceneView.Repaint();
            }
        }

        private void CaptureTextureToSnapshot(Texture source)
        {
            if (source.width < 1 || source.height < 1)
            {
                throw new InvalidOperationException("The selected texture has no available pixels.");
            }

            if (snapshotTexture == null || snapshotTexture.width != source.width || snapshotTexture.height != source.height)
            {
                if (snapshotTexture != null) UnityEngine.Object.DestroyImmediate(snapshotTexture);
                snapshotTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point,
                    alphaIsTransparency = true,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            RenderTexture temporaryTarget = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            try
            {
                Graphics.Blit(source, temporaryTarget);
                ReadRenderTextureIntoSnapshot(temporaryTarget);
            }
            finally
            {
                RenderTexture.ReleaseTemporary(temporaryTarget);
            }
        }

        private void ReadRenderTextureIntoSnapshot(RenderTexture source)
        {
            if (snapshotTexture == null || snapshotTexture.width != source.width || snapshotTexture.height != source.height)
            {
                if (snapshotTexture != null) UnityEngine.Object.DestroyImmediate(snapshotTexture);
                snapshotTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
                {
                    filterMode = FilterMode.Point,
                    alphaIsTransparency = true,
                    hideFlags = HideFlags.HideAndDontSave
                };
            }

            RenderTexture previousTarget = RenderTexture.active;
            try
            {
                RenderTexture.active = source;
                snapshotTexture.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
                snapshotTexture.Apply(false, false);
            }
            finally
            {
                RenderTexture.active = previousTarget;
            }
        }

        private static Texture2D GetReadableTexture(Texture source, out bool ownsTexture)
        {
            if (source is Texture2D texture2D && texture2D.isReadable)
            {
                ownsTexture = false;
                return texture2D;
            }

            ownsTexture = true;
            RenderTexture temporaryTarget = RenderTexture.GetTemporary(source.width, source.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            Texture2D readableTexture = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            try
            {
                Graphics.Blit(source, temporaryTarget);
                RenderTexture previousTarget = RenderTexture.active;
                try
                {
                    RenderTexture.active = temporaryTarget;
                    readableTexture.ReadPixels(new Rect(0f, 0f, source.width, source.height), 0, 0, false);
                    readableTexture.Apply(false, false);
                }
                finally
                {
                    RenderTexture.active = previousTarget;
                }

                return readableTexture;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(readableTexture);
                throw;
            }
            finally
            {
                RenderTexture.ReleaseTemporary(temporaryTarget);
            }
        }
    }

}
