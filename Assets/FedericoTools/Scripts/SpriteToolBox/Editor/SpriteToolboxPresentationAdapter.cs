using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal static class SpriteToolboxPresentationAdapter
    {
        public static string GetFrameLabel(int frameIndex) { return (frameIndex + 1).ToString(); }
        public static bool TryGetCanvasPixel(ISpriteToolboxState state, Vector2 mousePosition, Rect canvasRect, float zoom, out Vector2Int position)
        {
            position = Vector2Int.zero;
            if (state == null || state.DocumentWidth <= 0 || state.DocumentHeight <= 0) return false;

            // The canvas is nested in an Area and a ScrollView. Screen coordinates avoid their local GUI transforms.
            Rect screenCanvasRect = GUIUtility.GUIToScreenRect(canvasRect);
            Vector2 screenMousePosition = GUIUtility.GUIToScreenPoint(mousePosition);
            return SpriteCanvasCoordinates.TryGetPixelPosition(screenMousePosition, screenCanvasRect, state.DocumentWidth, state.DocumentHeight, zoom, out position);
        }

        public static bool TryGetCanvasPixelUnbounded(ISpriteToolboxState state, Vector2 mousePosition, Rect canvasRect, float zoom, out Vector2Int position)
        {
            position = Vector2Int.zero;
            if (state == null || state.DocumentWidth <= 0 || state.DocumentHeight <= 0 || zoom <= 0f) return false;

            // Line endpoints and rectangular selections may extend outside the document; retain logical coordinates for clipping.
            Rect screenCanvasRect = GUIUtility.GUIToScreenRect(canvasRect);
            Vector2 screenMousePosition = GUIUtility.GUIToScreenPoint(mousePosition);
            return SpriteCanvasCoordinates.TryGetPixelPositionUnbounded(screenMousePosition, screenCanvasRect, state.DocumentWidth, state.DocumentHeight, zoom, out position);
        }
    }
}
