using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal static class SpriteToolboxLightPadLayout
    {
        public static Rect CalculateDestinationRect(Rect canvasRect, int sourceWidth, int sourceHeight, SpriteLightPadAlignment alignment, float referenceZoom)
        {
            if (sourceWidth < 1 || sourceHeight < 1 || canvasRect.width <= 0f || canvasRect.height <= 0f)
            {
                return new Rect(canvasRect.center, Vector2.zero);
            }

            float width;
            float height;
            float aspect = sourceWidth / (float)sourceHeight;
            switch (alignment)
            {
                case SpriteLightPadAlignment.MatchWidth:
                    width = canvasRect.width;
                    height = width / aspect;
                    break;
                case SpriteLightPadAlignment.ZoomLevel:
                    float safeReferenceZoom = Mathf.Clamp(referenceZoom, 0.01f, 2f);
                    float fitScale = Mathf.Min(canvasRect.width / sourceWidth, canvasRect.height / sourceHeight);
                    width = sourceWidth * fitScale * safeReferenceZoom;
                    height = sourceHeight * fitScale * safeReferenceZoom;
                    break;
                default:
                    height = canvasRect.height;
                    width = height * aspect;
                    break;
            }

            return new Rect(canvasRect.center.x - width * 0.5f, canvasRect.center.y - height * 0.5f, width, height);
        }
    }
}
