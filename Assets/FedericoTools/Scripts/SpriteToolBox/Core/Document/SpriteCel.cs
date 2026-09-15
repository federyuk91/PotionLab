using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [Serializable]
    public sealed class SpriteCel : ISerializationCallbackReceiver
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private Color32[] pixels32;

        public int Width => width;
        public int Height => height;
        internal Color32[] Pixels
        {
            get
            {
                EnsurePixelStorage();
                return pixels32;
            }
        }

        public SpriteCel(int width, int height)
        {
            this.width = width;
            this.height = height;
            pixels32 = new Color32[width * height];
        }

        public bool Contains(Vector2Int position)
        {
            return position.x >= 0 && position.x < width && position.y >= 0 && position.y < height;
        }

        public Color GetPixel(Vector2Int position)
        {
            EnsurePixelStorage();
            return pixels32[GetIndex(position)];
        }

        public Color32 GetPixel32(Vector2Int position)
        {
            EnsurePixelStorage();
            return pixels32[GetIndex(position)];
        }

        public void SetPixel(Vector2Int position, Color color)
        {
            EnsurePixelStorage();
            pixels32[GetIndex(position)] = color;
        }

        public void SetPixel(Vector2Int position, Color32 color)
        {
            EnsurePixelStorage();
            pixels32[GetIndex(position)] = color;
        }

        public void SetPixels(Color32[] sourcePixels)
        {
            if (sourcePixels == null)
            {
                throw new ArgumentNullException(nameof(sourcePixels));
            }

            EnsurePixelStorage();
            if (sourcePixels.Length != pixels32.Length)
            {
                throw new ArgumentException("The source pixel buffer must match the cel dimensions.", nameof(sourcePixels));
            }

            System.Array.Copy(sourcePixels, pixels32, pixels32.Length);
        }

        public void Composite(SpriteCel topCel, float topOpacity)
        {
            Color32[] topPixels = topCel.Pixels;
            EnsurePixelStorage();
            for (int pixelIndex = 0; pixelIndex < pixels32.Length; pixelIndex++)
            {
                pixels32[pixelIndex] = SpriteColorMath.Blend(pixels32[pixelIndex], topPixels[pixelIndex], topOpacity);
            }
        }

        public SpriteCel Clone()
        {
            SpriteCel clone = new SpriteCel(width, height);
            EnsurePixelStorage();
            pixels32.CopyTo(clone.pixels32, 0);
            return clone;
        }

        public void OnBeforeSerialize()
        {
            EnsurePixelStorage();
        }

        public void OnAfterDeserialize()
        {
            EnsurePixelStorage();
        }

        private int GetIndex(Vector2Int position)
        {
            if (!Contains(position))
            {
                throw new ArgumentOutOfRangeException(nameof(position));
            }

            return position.y * width + position.x;
        }

        private void EnsurePixelStorage()
        {
            int expectedLength = Mathf.Max(0, width * height);
            if (pixels32 == null || pixels32.Length != expectedLength) pixels32 = new Color32[expectedLength];
        }

    }

}
