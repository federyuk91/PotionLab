using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    internal static class SpriteColorMath
    {
        public static bool AreEqual(Color first, Color second)
        {
            Color32 first32 = first;
            Color32 second32 = second;
            return first32.Equals(second32);
        }

        public static Color32 Blend(Color32 bottom, Color32 top, float opacity)
        {
            if (top.a == 0 || opacity <= 0f)
            {
                return bottom;
            }

            float clampedOpacity = Mathf.Clamp01(opacity);
            if (bottom.a == 0 && clampedOpacity >= 1f)
            {
                return top;
            }

            if (top.a == 255 && clampedOpacity >= 1f)
            {
                return new Color32(top.r, top.g, top.b, 255);
            }

            float topAlpha = top.a / 255f * clampedOpacity;
            if (bottom.a == 0)
            {
                return new Color(top.r / 255f, top.g / 255f, top.b / 255f, topAlpha);
            }

            float bottomAlpha = bottom.a / 255f;
            float inverseTopAlpha = 1f - topAlpha;
            float outputAlpha = topAlpha + bottomAlpha * inverseTopAlpha;
            float red = (top.r / 255f * topAlpha + bottom.r / 255f * bottomAlpha * inverseTopAlpha) / outputAlpha;
            float green = (top.g / 255f * topAlpha + bottom.g / 255f * bottomAlpha * inverseTopAlpha) / outputAlpha;
            float blue = (top.b / 255f * topAlpha + bottom.b / 255f * bottomAlpha * inverseTopAlpha) / outputAlpha;
            return new Color(red, green, blue, outputAlpha);
        }
    }
}
