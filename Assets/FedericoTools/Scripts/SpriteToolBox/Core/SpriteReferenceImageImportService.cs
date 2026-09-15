using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public static class SpriteReferenceImageImportService
    {
        private const float TargetSizeTolerance = 0.1f;
        private const int MaximumReductionColorFamilies = 256;

        public static Color32[] ReduceAndResize(
            IReadOnlyList<Color32> sourcePixels,
            int sourceWidth,
            int sourceHeight,
            int targetColorCount,
            int targetWidth,
            int targetHeight)
        {
            if (sourcePixels == null || sourceWidth < 1 || sourceHeight < 1 || sourcePixels.Count != sourceWidth * sourceHeight)
            {
                throw new ArgumentException("Source pixels must match the provided dimensions.", nameof(sourcePixels));
            }

            if (targetColorCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetColorCount));
            }

            if (targetWidth < 1 || targetHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(targetWidth));
            }

            Color32[] reducedPixels = ReduceColors(sourcePixels, sourceWidth, sourceHeight, targetColorCount);
            return DownscaleToCanvas(reducedPixels, sourceWidth, sourceHeight, targetWidth, targetHeight);
        }

        public static Color32[] ReduceColors(IReadOnlyList<Color32> sourcePixels, int width, int height, int targetColorCount)
        {
            if (sourcePixels == null || width < 1 || height < 1 || sourcePixels.Count != width * height)
            {
                throw new ArgumentException("Source pixels must match the provided dimensions.", nameof(sourcePixels));
            }

            Color32[] boundedPixels = LimitColorFamilies(sourcePixels, targetColorCount);
            SpriteCanvasColorMap sourceMap = CreateColorMap(boundedPixels, width, height);
            IReadOnlyList<SpriteRecolorMapping> mappings = SpriteColorReductionService.CreateMappings(sourceMap, targetColorCount);
            return ApplyReductionMappings(boundedPixels, CreateTargetMap(mappings));
        }

        public static Color32[] MapToNearestPalette(IReadOnlyList<Color32> sourcePixels, IReadOnlyList<Color> palette)
        {
            if (sourcePixels == null)
            {
                throw new ArgumentNullException(nameof(sourcePixels));
            }

            if (palette == null || palette.Count == 0)
            {
                throw new ArgumentException("An active palette with at least one color is required.", nameof(palette));
            }

            Dictionary<Color32, Color32> paletteTargets = new Dictionary<Color32, Color32>();
            Color32[] result = new Color32[sourcePixels.Count];
            for (int pixelIndex = 0; pixelIndex < sourcePixels.Count; pixelIndex++)
            {
                Color32 source = sourcePixels[pixelIndex];
                if (source.a == 0)
                {
                    result[pixelIndex] = source;
                    continue;
                }

                if (!paletteTargets.TryGetValue(source, out Color32 paletteColor))
                {
                    paletteColor = SpriteColorReductionService.GetNearestPaletteColor(source, palette);
                    paletteTargets.Add(source, paletteColor);
                }

                result[pixelIndex] = paletteColor;
            }

            return result;
        }

        public static Color32[] DitherToColors(IReadOnlyList<Color32> sourcePixels, int width, int height, IReadOnlyList<Color> targetColors)
        {
            if (sourcePixels == null || width < 1 || height < 1 || sourcePixels.Count != width * height)
            {
                throw new ArgumentException("Source pixels must match the provided dimensions.", nameof(sourcePixels));
            }

            if (targetColors == null || targetColors.Count == 0)
            {
                throw new ArgumentException("At least one target color is required.", nameof(targetColors));
            }

            Color32[] result = new Color32[sourcePixels.Count];
            Vector3[] errors = new Vector3[sourcePixels.Count];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int pixelIndex = y * width + x;
                    Color32 source = sourcePixels[pixelIndex];
                    if (source.a == 0)
                    {
                        result[pixelIndex] = source;
                        continue;
                    }

                    Vector3 adjusted = new Vector3(source.r / 255f, source.g / 255f, source.b / 255f) + errors[pixelIndex];
                    Color adjustedColor = new Color(Mathf.Clamp01(adjusted.x), Mathf.Clamp01(adjusted.y), Mathf.Clamp01(adjusted.z), source.a / 255f);
                    Color32 target = SpriteColorReductionService.GetNearestPaletteColor(adjustedColor, targetColors);
                    target.a = source.a;
                    result[pixelIndex] = target;

                    Vector3 quantizationError = adjusted - new Vector3(target.r / 255f, target.g / 255f, target.b / 255f);
                    AddDitherError(errors, width, height, x + 1, y, quantizationError * (7f / 16f));
                    AddDitherError(errors, width, height, x - 1, y + 1, quantizationError * (3f / 16f));
                    AddDitherError(errors, width, height, x, y + 1, quantizationError * (5f / 16f));
                    AddDitherError(errors, width, height, x + 1, y + 1, quantizationError * (1f / 16f));
                }
            }

            return result;
        }

        public static IReadOnlyList<Color> GetOpaqueColors(IReadOnlyList<Color32> sourcePixels)
        {
            if (sourcePixels == null)
            {
                throw new ArgumentNullException(nameof(sourcePixels));
            }

            HashSet<Color32> uniqueColors = new HashSet<Color32>();
            List<Color> colors = new List<Color>();
            foreach (Color32 color in sourcePixels)
            {
                if (color.a > 0 && uniqueColors.Add(color))
                {
                    colors.Add(color);
                }
            }

            return colors;
        }

        private static SpriteCanvasColorMap CreateColorMap(IReadOnlyList<Color32> pixels, int width, int height)
        {
            List<Color> colors = new List<Color>();
            Dictionary<Color32, List<Vector2Int>> mutablePositions = new Dictionary<Color32, List<Vector2Int>>();
            for (int pixelIndex = 0; pixelIndex < pixels.Count; pixelIndex++)
            {
                Color32 color = pixels[pixelIndex];
                if (color.a == 0)
                {
                    continue;
                }

                if (!mutablePositions.TryGetValue(color, out List<Vector2Int> positions))
                {
                    positions = new List<Vector2Int>();
                    mutablePositions.Add(color, positions);
                    colors.Add(color);
                }

                positions.Add(new Vector2Int(pixelIndex % width, pixelIndex / width));
            }

            Dictionary<Color32, IReadOnlyList<Vector2Int>> positionsByColor = new Dictionary<Color32, IReadOnlyList<Vector2Int>>();
            foreach (KeyValuePair<Color32, List<Vector2Int>> pair in mutablePositions)
            {
                positionsByColor.Add(pair.Key, pair.Value);
            }

            IReadOnlyList<Color> orderedColors = SpriteColorReductionService.OrderByColorFamilies(colors);
            return new SpriteCanvasColorMap(orderedColors, positionsByColor);
        }

        private static Color32[] LimitColorFamilies(IReadOnlyList<Color32> sourcePixels, int targetColorCount)
        {
            int maximumFamilies = Mathf.Clamp(Mathf.Max(64, targetColorCount * 4), 64, MaximumReductionColorFamilies);
            HashSet<Color32> uniqueColors = new HashSet<Color32>();
            foreach (Color32 color in sourcePixels)
            {
                if (color.a == 0)
                {
                    continue;
                }

                uniqueColors.Add(color);
                if (uniqueColors.Count > maximumFamilies)
                {
                    break;
                }
            }

            Color32[] result = new Color32[sourcePixels.Count];
            if (uniqueColors.Count <= maximumFamilies)
            {
                for (int pixelIndex = 0; pixelIndex < sourcePixels.Count; pixelIndex++)
                {
                    result[pixelIndex] = sourcePixels[pixelIndex];
                }

                return result;
            }

            int channelLevels = Mathf.Clamp(Mathf.FloorToInt(Mathf.Pow(maximumFamilies, 1f / 3f)), 2, 6);
            Dictionary<int, ColorAccumulator> accumulators = new Dictionary<int, ColorAccumulator>();
            foreach (Color32 source in sourcePixels)
            {
                if (source.a == 0)
                {
                    continue;
                }

                int key = GetColorBinKey(source, channelLevels);
                if (!accumulators.TryGetValue(key, out ColorAccumulator accumulator))
                {
                    accumulator = new ColorAccumulator();
                }

                accumulator.Add(source);
                accumulators[key] = accumulator;
            }

            for (int pixelIndex = 0; pixelIndex < sourcePixels.Count; pixelIndex++)
            {
                Color32 source = sourcePixels[pixelIndex];
                result[pixelIndex] = source.a == 0
                    ? source
                    : accumulators[GetColorBinKey(source, channelLevels)].GetAverage(source.a);
            }

            return result;
        }

        private static int GetColorBinKey(Color32 color, int levels)
        {
            int red = Mathf.Clamp(Mathf.FloorToInt(color.r * levels / 256f), 0, levels - 1);
            int green = Mathf.Clamp(Mathf.FloorToInt(color.g * levels / 256f), 0, levels - 1);
            int blue = Mathf.Clamp(Mathf.FloorToInt(color.b * levels / 256f), 0, levels - 1);
            return (red * levels + green) * levels + blue;
        }

        private static Dictionary<Color32, Color32> CreateTargetMap(IReadOnlyList<SpriteRecolorMapping> mappings)
        {
            Dictionary<Color32, Color32> targets = new Dictionary<Color32, Color32>();
            foreach (SpriteRecolorMapping mapping in mappings)
            {
                targets[(Color32)mapping.Source] = mapping.Target;
            }

            return targets;
        }

        private static Color32[] ApplyReductionMappings(IReadOnlyList<Color32> sourcePixels, IReadOnlyDictionary<Color32, Color32> reducedColors)
        {
            Color32[] result = new Color32[sourcePixels.Count];
            for (int pixelIndex = 0; pixelIndex < sourcePixels.Count; pixelIndex++)
            {
                Color32 source = sourcePixels[pixelIndex];
                if (source.a == 0)
                {
                    result[pixelIndex] = source;
                    continue;
                }

                result[pixelIndex] = reducedColors.TryGetValue(source, out Color32 mapped) ? mapped : source;
            }

            return result;
        }

        public static Color32[] DownscaleToCanvas(Color32[] sourcePixels, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            Color32[] currentPixels = sourcePixels;
            int currentWidth = sourceWidth;
            int currentHeight = sourceHeight;
            while (!IsWithinTargetTolerance(currentWidth, currentHeight, targetWidth, targetHeight))
            {
                int halfWidth = Mathf.Max(1, currentWidth / 2);
                int halfHeight = Mathf.Max(1, currentHeight / 2);
                if (halfWidth < targetWidth * (1f - TargetSizeTolerance)
                    || halfHeight < targetHeight * (1f - TargetSizeTolerance))
                {
                    return ResizeNearestNeighbor(currentPixels, currentWidth, currentHeight, targetWidth, targetHeight);
                }

                currentPixels = ResizeNearestNeighbor(currentPixels, currentWidth, currentHeight, halfWidth, halfHeight);
                currentWidth = halfWidth;
                currentHeight = halfHeight;
            }

            return currentWidth == targetWidth && currentHeight == targetHeight
                ? currentPixels
                : ResizeNearestNeighbor(currentPixels, currentWidth, currentHeight, targetWidth, targetHeight);
        }

        private static bool IsWithinTargetTolerance(int width, int height, int targetWidth, int targetHeight)
        {
            return Mathf.Abs(width - targetWidth) <= targetWidth * TargetSizeTolerance
                && Mathf.Abs(height - targetHeight) <= targetHeight * TargetSizeTolerance;
        }

        private static Color32[] ResizeNearestNeighbor(IReadOnlyList<Color32> sourcePixels, int sourceWidth, int sourceHeight, int targetWidth, int targetHeight)
        {
            Color32[] result = new Color32[targetWidth * targetHeight];
            for (int targetY = 0; targetY < targetHeight; targetY++)
            {
                int sourceY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt(targetY * sourceHeight / (float)targetHeight));
                for (int targetX = 0; targetX < targetWidth; targetX++)
                {
                    int sourceX = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt(targetX * sourceWidth / (float)targetWidth));
                    result[targetY * targetWidth + targetX] = sourcePixels[sourceY * sourceWidth + sourceX];
                }
            }

            return result;
        }

        private static void AddDitherError(Vector3[] errors, int width, int height, int x, int y, Vector3 error)
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                errors[y * width + x] += error;
            }
        }

        private struct ColorAccumulator
        {
            private int red;
            private int green;
            private int blue;
            private int count;

            public void Add(Color32 color)
            {
                red += color.r;
                green += color.g;
                blue += color.b;
                count++;
            }

            public Color32 GetAverage(byte alpha)
            {
                return count == 0
                    ? new Color32(0, 0, 0, alpha)
                    : new Color32((byte)(red / count), (byte)(green / count), (byte)(blue / count), alpha);
            }
        }
    }
}
