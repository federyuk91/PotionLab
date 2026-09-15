using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public static class SpriteColorReductionService
    {
        public const int MaximumColorFamilyOperationColorCount = 256;

        public static bool SupportsColorFamilyOperations(int colorCount)
        {
            return colorCount <= MaximumColorFamilyOperationColorCount;
        }

        public static IReadOnlyList<Color> OrderByColorFamilies(IReadOnlyList<Color> colors)
        {
            if (colors == null || colors.Count < 2)
            {
                return colors ?? new Color[0];
            }

            if (!SupportsColorFamilyOperations(colors.Count))
            {
                return new List<Color>(colors);
            }

            List<Color32> remaining = new List<Color32>(colors.Count);
            foreach (Color color in colors)
            {
                remaining.Add(color);
            }

            remaining.Sort(CompareByLightness);
            List<Color> ordered = new List<Color>(colors.Count) { remaining[0] };
            Color32 current = remaining[0];
            remaining.RemoveAt(0);
            while (remaining.Count > 0)
            {
                int nearestIndex = FindNearestColorIndex(current, remaining);
                current = remaining[nearestIndex];
                ordered.Add(current);
                remaining.RemoveAt(nearestIndex);
            }

            return ordered;
        }

        public static IReadOnlyList<SpriteRecolorMapping> CreateMappings(SpriteCanvasColorMap sourceMap, int targetColorCount)
        {
            if (sourceMap == null || sourceMap.Colors.Count == 0)
            {
                return new SpriteRecolorMapping[0];
            }

            if (!SupportsColorFamilyOperations(sourceMap.Colors.Count))
            {
                return new SpriteRecolorMapping[0];
            }

            int clampedTargetColorCount = Mathf.Clamp(targetColorCount, 1, sourceMap.Colors.Count);
            List<ColorCluster> clusters = CreateClusters(sourceMap);
            int familyResolution = GetFamilyResolution(clampedTargetColorCount);
            float preferredFamilySize = Mathf.Max(1f, sourceMap.Colors.Count / (float)familyResolution);

            while (clusters.Count > clampedTargetColorCount)
            {
                ClusterPair pair = FindPreferredPair(clusters, preferredFamilySize);
                if (pair.FirstIndex < 0)
                {
                    break;
                }

                ColorCluster merged = ColorCluster.Merge(clusters[pair.FirstIndex], clusters[pair.SecondIndex]);
                clusters.RemoveAt(pair.SecondIndex);
                clusters.RemoveAt(pair.FirstIndex);
                clusters.Add(merged);
            }

            return CreateMappings(sourceMap.Colors, clusters);
        }

        internal static Color GetNearestPaletteColor(Color source, IReadOnlyList<Color> palette)
        {
            if (palette == null || palette.Count == 0)
            {
                return Color.clear;
            }

            Color32 source32 = source;
            Color nearestColor = palette[0];
            float nearestDistance = float.MaxValue;
            foreach (Color candidate in palette)
            {
                float distance = GetPerceptualDistance(source32, (Color32)candidate);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestColor = candidate;
                }
            }

            return nearestColor;
        }

        private static List<ColorCluster> CreateClusters(SpriteCanvasColorMap sourceMap)
        {
            List<ColorCluster> clusters = new List<ColorCluster>(sourceMap.Colors.Count);
            foreach (Color color in sourceMap.Colors)
            {
                Color32 color32 = color;
                clusters.Add(new ColorCluster(color32, sourceMap.GetPositions(color).Count));
            }

            return clusters;
        }

        private static ClusterPair FindPreferredPair(IReadOnlyList<ColorCluster> clusters, float preferredFamilySize)
        {
            ClusterPair preferredPair = new ClusterPair(-1, -1);
            float preferredScore = float.MaxValue;
            for (int firstIndex = 0; firstIndex < clusters.Count - 1; firstIndex++)
            {
                for (int secondIndex = firstIndex + 1; secondIndex < clusters.Count; secondIndex++)
                {
                    float score = GetMergeScore(clusters[firstIndex], clusters[secondIndex], preferredFamilySize);
                    if (score < preferredScore)
                    {
                        preferredScore = score;
                        preferredPair = new ClusterPair(firstIndex, secondIndex);
                    }
                }
            }

            return preferredPair;
        }

        private static float GetMergeScore(ColorCluster first, ColorCluster second, float preferredFamilySize)
        {
            float colorDistance = GetPerceptualDistance(first.Representative, second.Representative);
            int mergedColorCount = first.Sources.Count + second.Sources.Count;
            int mergedPixelCount = first.PixelCount + second.PixelCount;

            // Dense families are reduced earlier, while the preferred family size prevents one hue from collapsing entirely.
            float densityPriority = Mathf.Sqrt(Mathf.Max(1, mergedPixelCount));
            float familyBalancePenalty = Mathf.Max(1f, mergedColorCount / preferredFamilySize);
            return colorDistance * familyBalancePenalty / densityPriority;
        }

        private static int FindNearestColorIndex(Color32 current, IReadOnlyList<Color32> colors)
        {
            int nearestIndex = 0;
            float nearestDistance = float.MaxValue;
            for (int colorIndex = 0; colorIndex < colors.Count; colorIndex++)
            {
                float distance = GetPerceptualDistance(current, colors[colorIndex]);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestIndex = colorIndex;
                }
            }

            return nearestIndex;
        }

        private static int CompareByLightness(Color32 first, Color32 second)
        {
            float firstLightness = ToLab(first).x;
            float secondLightness = ToLab(second).x;
            int lightnessComparison = firstLightness.CompareTo(secondLightness);
            if (lightnessComparison != 0)
            {
                return lightnessComparison;
            }

            int redComparison = first.r.CompareTo(second.r);
            if (redComparison != 0)
            {
                return redComparison;
            }

            int greenComparison = first.g.CompareTo(second.g);
            return greenComparison != 0 ? greenComparison : first.b.CompareTo(second.b);
        }

        private static IReadOnlyList<SpriteRecolorMapping> CreateMappings(IReadOnlyList<Color> sourceColors, IReadOnlyList<ColorCluster> clusters)
        {
            Dictionary<Color32, Color32> targetsBySource = new Dictionary<Color32, Color32>();
            foreach (ColorCluster cluster in clusters)
            {
                foreach (Color32 source in cluster.Sources)
                {
                    targetsBySource[source] = cluster.Representative;
                }
            }

            List<SpriteRecolorMapping> mappings = new List<SpriteRecolorMapping>(sourceColors.Count);
            foreach (Color source in sourceColors)
            {
                Color32 source32 = source;
                Color32 target32 = targetsBySource[source32];
                mappings.Add(new SpriteRecolorMapping(source32, target32));
            }

            return mappings;
        }

        private static int GetFamilyResolution(int targetColorCount)
        {
            int resolution = 1;
            while (resolution <= targetColorCount / 2)
            {
                resolution *= 2;
            }

            return resolution;
        }

        private static float GetPerceptualDistance(Color32 first, Color32 second)
        {
            Vector3 firstLab = ToLab(first);
            Vector3 secondLab = ToLab(second);
            return Vector3.Distance(firstLab, secondLab);
        }

        private static Vector3 ToLab(Color32 color)
        {
            float red = ToLinear(color.r / 255f);
            float green = ToLinear(color.g / 255f);
            float blue = ToLinear(color.b / 255f);
            float x = (red * 0.4124f + green * 0.3576f + blue * 0.1805f) / 0.95047f;
            float y = red * 0.2126f + green * 0.7152f + blue * 0.0722f;
            float z = (red * 0.0193f + green * 0.1192f + blue * 0.9505f) / 1.08883f;
            float labX = ToLabComponent(x);
            float labY = ToLabComponent(y);
            float labZ = ToLabComponent(z);
            return new Vector3(116f * labY - 16f, 500f * (labX - labY), 200f * (labY - labZ));
        }

        private static float ToLinear(float value)
        {
            return value <= 0.04045f ? value / 12.92f : Mathf.Pow((value + 0.055f) / 1.055f, 2.4f);
        }

        private static float ToLabComponent(float value)
        {
            return value > 0.008856f ? Mathf.Pow(value, 1f / 3f) : value * 7.787f + 16f / 116f;
        }

        private readonly struct ClusterPair
        {
            public int FirstIndex { get; }
            public int SecondIndex { get; }

            public ClusterPair(int firstIndex, int secondIndex)
            {
                FirstIndex = firstIndex;
                SecondIndex = secondIndex;
            }
        }

        private sealed class ColorCluster
        {
            public List<Color32> Sources { get; }
            public Color32 Representative { get; }
            public int PixelCount { get; }

            public ColorCluster(Color32 source, int pixelCount)
            {
                Sources = new List<Color32> { source };
                Representative = source;
                PixelCount = pixelCount;
            }

            private ColorCluster(List<Color32> sources, Color32 representative, int pixelCount)
            {
                Sources = sources;
                Representative = representative;
                PixelCount = pixelCount;
            }

            public static ColorCluster Merge(ColorCluster first, ColorCluster second)
            {
                List<Color32> sources = new List<Color32>(first.Sources.Count + second.Sources.Count);
                sources.AddRange(first.Sources);
                sources.AddRange(second.Sources);
                Color32 representative = first.PixelCount >= second.PixelCount ? first.Representative : second.Representative;
                return new ColorCluster(sources, representative, first.PixelCount + second.PixelCount);
            }
        }
    }
}
