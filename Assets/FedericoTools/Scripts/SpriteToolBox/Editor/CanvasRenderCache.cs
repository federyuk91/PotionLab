using UnityEngine;
using System.Collections.Generic;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class CanvasRenderCache
    {
        private Texture2D checkerboardTexture;
        private Texture2D compositeTexture;
        private Color32[] compositePixels;
        private SpriteCanvasColorMap recolorSourceMap;
        private readonly Dictionary<Color32, Color32> recolorTargetsBySource = new Dictionary<Color32, Color32>();
        private Texture2D previousOnionTexture;
        private Color32[] previousOnionPixels;
        private Texture2D nextOnionTexture;
        private Color32[] nextOnionPixels;
        private Texture2D movingSelectionTexture;
        private SpriteDocument compositeDocument;
        private SpriteDocument onionDocument;
        private int compositeFrameIndex = -1;
        private int recolorPreviewVersion = -1;
        private int onionFrameIndex = -1;
        private int previousOnionFrameIndex = -1;
        private int nextOnionFrameIndex = -1;
        private bool onionLoop;
        private bool isCompositeDirty = true;
        private bool isCompositeUploadPending;
        private bool isPreviousOnionDirty = true;
        private bool isNextOnionDirty = true;

        internal bool HasCachedOnionSkin => previousOnionFrameIndex >= 0 || nextOnionFrameIndex >= 0;
        internal int PreviousOnionTextureInstanceId => previousOnionTexture == null ? 0 : previousOnionTexture.GetInstanceID();
        internal int NextOnionTextureInstanceId => nextOnionTexture == null ? 0 : nextOnionTexture.GetInstanceID();

        public Texture2D GetCheckerboardTexture()
        {
            if (checkerboardTexture != null) return checkerboardTexture;
            checkerboardTexture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat, hideFlags = HideFlags.HideAndDontSave };
            Color light = new Color(0.72f, 0.72f, 0.72f);
            Color dark = new Color(0.56f, 0.56f, 0.56f);
            checkerboardTexture.SetPixels(new[] { light, dark, dark, light });
            checkerboardTexture.Apply(false, true);
            return checkerboardTexture;
        }

        public Texture2D GetCompositeTexture(
            SpriteDocument document,
            int frameIndex,
            SpriteCanvasColorMap requestedRecolorSourceMap,
            IReadOnlyList<SpriteRecolorMapping> recolorMappings,
            int requestedRecolorPreviewVersion)
        {
            if (isCompositeDirty || compositeTexture == null || compositeDocument != document || compositeFrameIndex != frameIndex)
            {
                bool requiresNewTexture = compositeTexture == null || compositeTexture.width != document.Width || compositeTexture.height != document.Height;
                if (requiresNewTexture) ReleaseCompositeTexture();
                if (compositeTexture == null)
                {
                    compositeTexture = new Texture2D(document.Width, document.Height, TextureFormat.RGBA32, false, false) { filterMode = FilterMode.Point, alphaIsTransparency = true, hideFlags = HideFlags.HideAndDontSave };
                    compositePixels = new Color32[document.Width * document.Height];
                }

                SpriteDocumentRenderer.RenderFrameIntoBuffer(document, frameIndex, compositePixels);
                compositeTexture.SetPixels32(compositePixels);
                recolorSourceMap = null;
                recolorTargetsBySource.Clear();
                ApplyRecolorPreview(requestedRecolorSourceMap, recolorMappings, requestedRecolorPreviewVersion);
                compositeTexture.Apply(false);
                compositeDocument = document;
                compositeFrameIndex = frameIndex;
                isCompositeDirty = false;
                isCompositeUploadPending = false;
            }
            else if (recolorPreviewVersion != requestedRecolorPreviewVersion || recolorSourceMap != requestedRecolorSourceMap)
            {
                ApplyRecolorPreview(requestedRecolorSourceMap, recolorMappings, requestedRecolorPreviewVersion);
                compositeTexture.Apply(false);
            }
            else if (isCompositeUploadPending)
            {
                compositeTexture.Apply(false);
                isCompositeUploadPending = false;
            }

            return compositeTexture;
        }

        private void ApplyRecolorPreview(
            SpriteCanvasColorMap requestedSourceMap,
            IReadOnlyList<SpriteRecolorMapping> recolorMappings,
            int requestedVersion)
        {
            recolorPreviewVersion = requestedVersion;
            if (requestedSourceMap == null || recolorMappings == null || recolorMappings.Count == 0)
            {
                if (recolorSourceMap != null)
                {
                    compositeTexture.SetPixels32(compositePixels);
                }

                recolorSourceMap = null;
                recolorTargetsBySource.Clear();
                return;
            }

            if (recolorSourceMap != requestedSourceMap)
            {
                compositeTexture.SetPixels32(compositePixels);
                recolorTargetsBySource.Clear();
                recolorSourceMap = requestedSourceMap;
            }

            Dictionary<Color32, Color32> requestedTargetsBySource = new Dictionary<Color32, Color32>();
            foreach (SpriteRecolorMapping mapping in recolorMappings)
            {
                Color32 source = mapping.Source;
                Color32 target = mapping.Target;
                if (!source.Equals(target))
                {
                    requestedTargetsBySource[source] = target;
                }
            }

            foreach (KeyValuePair<Color32, Color32> previousTarget in recolorTargetsBySource)
            {
                if (requestedTargetsBySource.TryGetValue(previousTarget.Key, out Color32 requestedTarget))
                {
                    if (!requestedTarget.Equals(previousTarget.Value))
                    {
                        SetPreviewPixels(previousTarget.Key, requestedTarget);
                    }
                }
                else
                {
                    SetPreviewPixels(previousTarget.Key, previousTarget.Key);
                }
            }

            foreach (KeyValuePair<Color32, Color32> requestedTarget in requestedTargetsBySource)
            {
                if (!recolorTargetsBySource.TryGetValue(requestedTarget.Key, out Color32 previousTarget) || !previousTarget.Equals(requestedTarget.Value))
                {
                    SetPreviewPixels(requestedTarget.Key, requestedTarget.Value);
                }
            }

            recolorTargetsBySource.Clear();
            foreach (KeyValuePair<Color32, Color32> requestedTarget in requestedTargetsBySource)
            {
                recolorTargetsBySource.Add(requestedTarget.Key, requestedTarget.Value);
            }
        }

        private void SetPreviewPixels(Color32 source, Color32 target)
        {
            IReadOnlyList<Vector2Int> positions = recolorSourceMap.GetPositions((Color)source);
            foreach (Vector2Int position in positions)
            {
                compositeTexture.SetPixel(position.x, position.y, target);
            }
        }

        public void DrawOnionSkin(Rect canvasRect, SpriteDocument document, int frameIndex, bool showPrevious, bool showNext, bool loop, float opacity)
        {
            int frameCount = document.Frames.Count;
            if (frameCount <= 1) return;
            bool hasPreviousFrame = frameIndex > 0 || loop;
            bool hasNextFrame = frameIndex < frameCount - 1 || loop;
            if ((!showPrevious || !hasPreviousFrame) && (!showNext || !hasNextFrame)) return;
            PrepareOnionSkin(document, frameIndex, loop);

            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 0.45f, 0.45f, opacity);
            if (showPrevious && hasPreviousFrame && previousOnionTexture != null) GUI.DrawTexture(canvasRect, previousOnionTexture, ScaleMode.StretchToFill, true);
            GUI.color = new Color(0.45f, 0.75f, 1f, opacity);
            if (showNext && hasNextFrame && nextOnionTexture != null) GUI.DrawTexture(canvasRect, nextOnionTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        public void DrawLightPadBackground(Rect canvasRect, Texture referenceTexture, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom)
        {
            if (referenceTexture == null)
            {
                return;
            }

            Rect destinationRect = CalculateLightPadDestination(canvasRect, referenceTexture, alignment, referenceZoom, positionOffset, canvasZoom);
            Color previousColor = GUI.color;
            GUI.color = new Color(1f, 1f, 1f, 0.1f);
            GUI.DrawTexture(destinationRect, referenceTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        public void DrawLightPadCanvasOverlay(Rect canvasRect, Texture referenceTexture, float opacity, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom)
        {
            if (referenceTexture == null || opacity <= 0f)
            {
                return;
            }

            Rect destinationRect = CalculateLightPadDestination(canvasRect, referenceTexture, alignment, referenceZoom, positionOffset, canvasZoom);
            Rect clippedDestinationRect = new Rect(destinationRect.x - canvasRect.x, destinationRect.y - canvasRect.y, destinationRect.width, destinationRect.height);
            Color previousColor = GUI.color;
            GUI.BeginGroup(canvasRect);
            GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(opacity));
            GUI.DrawTexture(clippedDestinationRect, referenceTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
            GUI.EndGroup();
        }

        private static Rect CalculateLightPadDestination(Rect canvasRect, Texture referenceTexture, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom)
        {
            Rect destinationRect = SpriteToolboxLightPadLayout.CalculateDestinationRect(canvasRect, referenceTexture.width, referenceTexture.height, alignment, referenceZoom);
            destinationRect.position += positionOffset * canvasZoom;
            return destinationRect;
        }

        public void InvalidateComposite()
        {
            isCompositeDirty = true;
            isCompositeUploadPending = false;
        }

        public void InvalidateOnionSkin()
        {
            onionDocument = null;
            onionFrameIndex = -1;
            previousOnionFrameIndex = -1;
            nextOnionFrameIndex = -1;
            isPreviousOnionDirty = true;
            isNextOnionDirty = true;
        }

        public void InvalidateOnionSkinFrame(SpriteDocument document, int frameIndex)
        {
            if (document == null || onionDocument != document)
            {
                return;
            }

            if (previousOnionFrameIndex == frameIndex) isPreviousOnionDirty = true;
            if (nextOnionFrameIndex == frameIndex) isNextOnionDirty = true;
        }

        internal void PrepareOnionSkin(SpriteDocument document, int frameIndex, bool loop)
        {
            int frameCount = document.Frames.Count;
            if (frameCount <= 1)
            {
                InvalidateOnionSkin();
                return;
            }

            int previousFrameIndex = frameIndex > 0 ? frameIndex - 1 : frameCount - 1;
            int nextFrameIndex = frameIndex < frameCount - 1 ? frameIndex + 1 : 0;
            bool hasPreviousFrame = frameIndex > 0 || loop;
            bool hasNextFrame = frameIndex < frameCount - 1 || loop;
            bool bindingChanged = onionDocument != document || onionFrameIndex != frameIndex || onionLoop != loop;
            if (bindingChanged)
            {
                isPreviousOnionDirty |= previousOnionFrameIndex != (hasPreviousFrame ? previousFrameIndex : -1);
                isNextOnionDirty |= nextOnionFrameIndex != (hasNextFrame ? nextFrameIndex : -1);
                onionDocument = document;
                onionFrameIndex = frameIndex;
                onionLoop = loop;
                previousOnionFrameIndex = hasPreviousFrame ? previousFrameIndex : -1;
                nextOnionFrameIndex = hasNextFrame ? nextFrameIndex : -1;
            }

            if (hasPreviousFrame && isPreviousOnionDirty)
            {
                EnsureOnionTexture(ref previousOnionTexture, ref previousOnionPixels, document.Width, document.Height);
                SpriteDocumentRenderer.RenderFrameInto(document, previousFrameIndex, previousOnionTexture, previousOnionPixels);
                isPreviousOnionDirty = false;
            }

            if (hasNextFrame && isNextOnionDirty)
            {
                EnsureOnionTexture(ref nextOnionTexture, ref nextOnionPixels, document.Width, document.Height);
                SpriteDocumentRenderer.RenderFrameInto(document, nextFrameIndex, nextOnionTexture, nextOnionPixels);
                isNextOnionDirty = false;
            }
        }

        public void UpdateCompositePixel(SpriteDocument document, int frameIndex, Vector2Int position, Color color)
        {
            if (isCompositeDirty || compositeTexture == null || compositeDocument != document || compositeFrameIndex != frameIndex) return;
            compositeTexture.SetPixel(position.x, position.y, color);
            compositePixels[position.y * document.Width + position.x] = color;
            recolorPreviewVersion = int.MinValue;
            isCompositeUploadPending = true;
        }

        public void UpdateCompositeRegion(SpriteDocument document, int frameIndex, RectInt bounds)
        {
            if (document == null || bounds.width <= 0 || bounds.height <= 0)
            {
                return;
            }

            if (isCompositeDirty || compositeTexture == null || compositeDocument != document || compositeFrameIndex != frameIndex)
            {
                if (compositeDocument == document && compositeFrameIndex == frameIndex) isCompositeDirty = true;
                return;
            }

            int xMin = Mathf.Clamp(bounds.xMin, 0, document.Width);
            int xMax = Mathf.Clamp(bounds.xMax, 0, document.Width);
            int yMin = Mathf.Clamp(bounds.yMin, 0, document.Height);
            int yMax = Mathf.Clamp(bounds.yMax, 0, document.Height);
            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    Color32 color = SpriteDocumentRenderer.GetCompositePixel(document, frameIndex, position);
                    compositePixels[y * document.Width + x] = color;
                    compositeTexture.SetPixel(x, y, color);
                }
            }

            isCompositeUploadPending = true;
            recolorPreviewVersion = int.MinValue;
        }

        public void ApplyPendingUpload()
        {
            if (!isCompositeUploadPending || compositeTexture == null) return;
            compositeTexture.Apply(false);
            isCompositeUploadPending = false;
        }

        public Texture2D SetMovingSelectionPreview(Color32[] pixels, int width, int height)
        {
            if (pixels == null || pixels.Length != width * height) return null;
            if (movingSelectionTexture == null || movingSelectionTexture.width != width || movingSelectionTexture.height != height)
            {
                ReleaseMovingSelectionPreview();
                movingSelectionTexture = new Texture2D(width, height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, alphaIsTransparency = true, hideFlags = HideFlags.HideAndDontSave };
            }

            movingSelectionTexture.SetPixels32(pixels);
            movingSelectionTexture.Apply(false);
            return movingSelectionTexture;
        }

        public void ReleaseMovingSelectionPreview()
        {
            if (movingSelectionTexture != null) Object.DestroyImmediate(movingSelectionTexture);
            movingSelectionTexture = null;
        }

        public void Dispose()
        {
            ReleaseCompositeTexture();
            ReleaseOnionTextures();
            ReleaseMovingSelectionPreview();
            if (checkerboardTexture != null) Object.DestroyImmediate(checkerboardTexture);
            checkerboardTexture = null;
        }

        private void ReleaseCompositeTexture()
        {
            if (compositeTexture != null) Object.DestroyImmediate(compositeTexture);
            compositeTexture = null;
            compositePixels = null;
            recolorSourceMap = null;
            recolorTargetsBySource.Clear();
            compositeDocument = null;
            compositeFrameIndex = -1;
        }

        private void ReleaseOnionTextures()
        {
            if (previousOnionTexture != null) Object.DestroyImmediate(previousOnionTexture);
            if (nextOnionTexture != null) Object.DestroyImmediate(nextOnionTexture);
            previousOnionTexture = null;
            previousOnionPixels = null;
            nextOnionTexture = null;
            nextOnionPixels = null;
            onionDocument = null;
            onionFrameIndex = -1;
            previousOnionFrameIndex = -1;
            nextOnionFrameIndex = -1;
            onionLoop = false;
            isPreviousOnionDirty = true;
            isNextOnionDirty = true;
        }

        private static void EnsureOnionTexture(ref Texture2D texture, ref Color32[] pixels, int width, int height)
        {
            if (texture != null && texture.width == width && texture.height == height && pixels != null && pixels.Length == width * height)
            {
                return;
            }

            if (texture != null) Object.DestroyImmediate(texture);
            texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true,
                hideFlags = HideFlags.HideAndDontSave
            };
            pixels = new Color32[width * height];
        }
    }
}
