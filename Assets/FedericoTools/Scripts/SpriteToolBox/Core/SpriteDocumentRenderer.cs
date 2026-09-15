using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public static class SpriteDocumentRenderer
    {
        public static Texture2D RenderFrame(SpriteDocument document, int frameIndex)
        {
            Texture2D texture = new Texture2D(document.Width, document.Height, TextureFormat.RGBA32, false, false)
            {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true
            };

            RenderFrameInto(document, frameIndex, texture, null);
            return texture;
        }

        public static void RenderFrameInto(SpriteDocument document, int frameIndex, Texture2D texture, Color32[] pixelBuffer)
        {
            if (texture == null || texture.width != document.Width || texture.height != document.Height)
            {
                throw new System.ArgumentException("The target texture must match the document dimensions.", nameof(texture));
            }

            Color32[] pixels = pixelBuffer ?? new Color32[document.Width * document.Height];
            if (pixels.Length != document.Width * document.Height)
            {
                throw new System.ArgumentException("The pixel buffer must match the document dimensions.", nameof(pixelBuffer));
            }

            RenderFrameIntoBuffer(document, frameIndex, pixels);
            texture.SetPixels32(pixels);
            texture.Apply(false);
        }

        public static void RenderFrameIntoBuffer(SpriteDocument document, int frameIndex, Color32[] pixelBuffer)
        {
            if (pixelBuffer == null || pixelBuffer.Length != document.Width * document.Height)
            {
                throw new System.ArgumentException("The pixel buffer must match the document dimensions.", nameof(pixelBuffer));
            }

            System.Array.Clear(pixelBuffer, 0, pixelBuffer.Length);
            SpriteFrame frame = document.GetFrame(frameIndex);
            System.Collections.Generic.IReadOnlyList<SpriteLayerTrack> layerTracks = document.LayerTracks;
            for (int layerIndex = 0; layerIndex < layerTracks.Count; layerIndex++)
            {
                SpriteLayerTrack track = layerTracks[layerIndex];
                if (!track.IsVisible || track.Opacity <= 0f)
                {
                    continue;
                }

                Color32[] layerPixels = frame.GetCel(layerIndex).Pixels;
                for (int pixelIndex = 0; pixelIndex < pixelBuffer.Length; pixelIndex++)
                {
                    Color32 top = layerPixels[pixelIndex];
                    if (top.a == 0)
                    {
                        continue;
                    }

                    if (top.a == 255 && track.Opacity >= 1f)
                    {
                        pixelBuffer[pixelIndex] = top;
                        continue;
                    }

                    pixelBuffer[pixelIndex] = SpriteColorMath.Blend(pixelBuffer[pixelIndex], top, track.Opacity);
                }
            }
        }

        public static Color GetCompositePixel(SpriteDocument document, int frameIndex, Vector2Int position)
        {
            if (document == null)
            {
                throw new System.ArgumentNullException(nameof(document));
            }

            if (position.x < 0 || position.x >= document.Width || position.y < 0 || position.y >= document.Height)
            {
                throw new System.ArgumentOutOfRangeException(nameof(position));
            }

            Color32 compositeColor = new Color32(0, 0, 0, 0);
            SpriteFrame frame = document.GetFrame(frameIndex);
            for (int layerIndex = 0; layerIndex < document.LayerTracks.Count; layerIndex++)
            {
                SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
                if (layer.IsVisible)
                {
                    compositeColor = SpriteColorMath.Blend(compositeColor, frame.GetCel(layerIndex).GetPixel32(position), layer.Opacity);
                }
            }

            return compositeColor;
        }

        public static Texture2D RenderSpriteSheet(SpriteDocument document, int columns)
        {
            int safeColumns = Mathf.Clamp(columns, 1, document.Frames.Count);
            int rows = Mathf.CeilToInt(document.Frames.Count / (float)safeColumns);
            Texture2D spriteSheet = new Texture2D(document.Width * safeColumns, document.Height * rows, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                alphaIsTransparency = true
            };

            Color32[] spriteSheetPixels = new Color32[spriteSheet.width * spriteSheet.height];
            Color32[] framePixels = new Color32[document.Width * document.Height];
            for (int frameIndex = 0; frameIndex < document.Frames.Count; frameIndex++)
            {
                RenderFrameIntoBuffer(document, frameIndex, framePixels);
                int columnIndex = frameIndex % safeColumns;
                int rowIndex = rows - 1 - frameIndex / safeColumns;
                int destinationX = columnIndex * document.Width;
                int destinationY = rowIndex * document.Height;
                for (int row = 0; row < document.Height; row++)
                {
                    int destinationOffset = (destinationY + row) * spriteSheet.width + destinationX;
                    System.Array.Copy(framePixels, row * document.Width, spriteSheetPixels, destinationOffset, document.Width);
                }
            }

            spriteSheet.SetPixels32(spriteSheetPixels);
            spriteSheet.Apply(false);
            return spriteSheet;
        }

    }
}
