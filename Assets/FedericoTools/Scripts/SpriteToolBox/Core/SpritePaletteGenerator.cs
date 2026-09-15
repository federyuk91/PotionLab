using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public enum PaletteHarmonyMode
    {
        Analogous,
        Complementary,
        Triadic,
        Monochrome,
        Planetary
    }

    public static class SpritePaletteGenerator
    {
        public static List<Color> Generate(Color baseColor, int colorCount, PaletteHarmonyMode harmony)
        {
            int safeCount = Mathf.Clamp(colorCount, 2, 32);
            Color.RGBToHSV(baseColor, out float hue, out float saturation, out float value);
            List<Color> colors = new List<Color>();
            for (int colorIndex = 0; colorIndex < safeCount; colorIndex++)
            {
                float ratio = safeCount == 1 ? 0f : colorIndex / (float)(safeCount - 1);
                float colorHue = GetHue(hue, ratio, harmony);
                float colorSaturation = harmony == PaletteHarmonyMode.Monochrome
                    ? Mathf.Lerp(Mathf.Clamp01(saturation * 0.35f), Mathf.Clamp01(saturation + 0.1f), ratio)
                    : Mathf.Clamp01(Mathf.Lerp(saturation * 0.65f, Mathf.Min(1f, saturation + 0.2f), ratio));
                float colorValue = Mathf.Lerp(Mathf.Clamp01(value * 0.35f), Mathf.Clamp01(value + 0.25f), ratio);
                colors.Add(Color.HSVToRGB(colorHue, colorSaturation, colorValue));
            }

            return colors;
        }

        public static List<Color> GeneratePlanetary(int colorCount, int seed, float baseHue, float hueVariation, Vector2 saturationRange, Vector2 valueRange)
        {
            int safeCount = Mathf.Clamp(colorCount, 2, 32);
            System.Random random = new System.Random(seed);
            float hue = Mathf.Repeat(baseHue, 1f);
            float minimumSaturation = Mathf.Clamp01(Mathf.Min(saturationRange.x, saturationRange.y));
            float maximumSaturation = Mathf.Clamp01(Mathf.Max(saturationRange.x, saturationRange.y));
            float minimumValue = Mathf.Clamp01(Mathf.Min(valueRange.x, valueRange.y));
            float maximumValue = Mathf.Clamp01(Mathf.Max(valueRange.x, valueRange.y));
            List<Color> colors = new List<Color>();
            for (int colorIndex = 0; colorIndex < safeCount; colorIndex++)
            {
                float saturation = Mathf.Lerp(minimumSaturation, maximumSaturation, (float)random.NextDouble());
                float value = Mathf.Lerp(minimumValue, maximumValue, (float)random.NextDouble());
                colors.Add(Color.HSVToRGB(hue, saturation, value));
                float jitter = ((float)random.NextDouble() - 0.5f) * hueVariation * 0.2f;
                hue = Mathf.Repeat(hue + hueVariation + jitter, 1f);
            }

            return colors;
        }

        private static float GetHue(float baseHue, float ratio, PaletteHarmonyMode harmony)
        {
            float offset = harmony switch
            {
                PaletteHarmonyMode.Analogous => Mathf.Lerp(-0.10f, 0.10f, ratio),
                PaletteHarmonyMode.Complementary => ratio < 0.5f ? 0f : 0.5f,
                PaletteHarmonyMode.Triadic => Mathf.Round(ratio * 2f) / 3f,
                _ => 0f
            };
            return Mathf.Repeat(baseHue + offset, 1f);
        }
    }
}
