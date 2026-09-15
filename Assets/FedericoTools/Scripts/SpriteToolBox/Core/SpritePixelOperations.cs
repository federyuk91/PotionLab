using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public static class SpriteCanvasCoordinates
    {
        public static bool TryGetPixelPosition(Vector2 mousePosition, Rect canvasRect, int documentWidth, int documentHeight, float zoom, out Vector2Int position)
        {
            position = Vector2Int.zero;
            if (documentWidth < 1 || documentHeight < 1 || zoom <= 0f || !canvasRect.Contains(mousePosition))
            {
                return false;
            }

            TryGetPixelPositionUnbounded(mousePosition, canvasRect, documentWidth, documentHeight, zoom, out position);
            return position.x >= 0 && position.x < documentWidth && position.y >= 0 && position.y < documentHeight;
        }

        public static bool TryGetPixelPositionUnbounded(Vector2 mousePosition, Rect canvasRect, int documentWidth, int documentHeight, float zoom, out Vector2Int position)
        {
            position = Vector2Int.zero;
            if (documentWidth < 1 || documentHeight < 1 || zoom <= 0f || canvasRect.width <= 0f || canvasRect.height <= 0f)
            {
                return false;
            }

            float pixelWidth = canvasRect.width / documentWidth;
            float pixelHeight = canvasRect.height / documentHeight;
            int x = Mathf.FloorToInt((mousePosition.x - canvasRect.xMin) / pixelWidth);
            int topOriginY = Mathf.FloorToInt((mousePosition.y - canvasRect.yMin) / pixelHeight);
            int y = documentHeight - 1 - topOriginY;
            position = new Vector2Int(x, y);
            return true;
        }
    }

    public static class SpritePixelOperations
    {
        private static readonly FloodFillWorkspace FloodFillBuffer = new FloodFillWorkspace();

        public static List<PixelChange> CreatePixelChange(SpriteCel cel, Vector2Int position, Color color)
        {
            List<PixelChange> changes = new List<PixelChange>();
            if (!cel.Contains(position))
            {
                return changes;
            }

            Color previousColor = cel.GetPixel(position);
            if (!SpriteColorMath.AreEqual(previousColor, color))
            {
                changes.Add(new PixelChange(position, previousColor, color));
            }

            return changes;
        }

        public static List<PixelChange> CreateLineChanges(SpriteCel cel, Vector2Int start, Vector2Int end, Color color)
        {
            List<PixelChange> changes = new List<PixelChange>();
            int deltaX = Mathf.Abs(end.x - start.x);
            int stepX = start.x < end.x ? 1 : -1;
            int deltaY = -Mathf.Abs(end.y - start.y);
            int stepY = start.y < end.y ? 1 : -1;
            int error = deltaX + deltaY;
            Vector2Int current = start;

            while (true)
            {
                AddChangeIfNeeded(cel, current, color, changes);
                if (current == end)
                {
                    break;
                }

                int doubleError = 2 * error;
                if (doubleError >= deltaY)
                {
                    error += deltaY;
                    current.x += stepX;
                }

                if (doubleError <= deltaX)
                {
                    error += deltaX;
                    current.y += stepY;
                }
            }

            return changes;
        }

        public static List<PixelChange> CreateFloodFillChanges(SpriteCel cel, Vector2Int start, Color replacementColor)
        {
            return CreateFloodFillChanges(cel, start, replacementColor, 0);
        }

        public static List<PixelChange> CreateFloodFillChanges(SpriteCel cel, Vector2Int start, Color replacementColor, int tolerance)
        {
            List<PixelChange> changes = new List<PixelChange>();
            if (!cel.Contains(start)) return changes;
            Color targetColor = cel.GetPixel(start);
            if (targetColor == replacementColor) return changes;
            foreach (Vector2Int position in CreateFloodFillPositions(cel, start, tolerance))
            {
                changes.Add(new PixelChange(position, cel.GetPixel(position), replacementColor));
            }

            return changes;
        }

        public static List<Vector2Int> CreateFloodFillPositions(SpriteCel cel, Vector2Int start, int tolerance)
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            if (!cel.Contains(start)) return positions;

            int clampedTolerance = Mathf.Clamp(tolerance, 0, 255);
            int pixelCount = cel.Width * cel.Height;
            FloodFillBuffer.Begin(pixelCount);
            int startIndex = start.y * cel.Width + start.x;
            FloodFillBuffer.Enqueue(startIndex);
            FloodFillBuffer.MarkVisited(startIndex);
            Color32 targetColor = cel.GetPixel32(start);
            Color32[] pixels = cel.Pixels;

            while (FloodFillBuffer.HasItems)
            {
                int currentIndex = FloodFillBuffer.Dequeue();
                if (!IsWithinTolerance(pixels[currentIndex], targetColor, clampedTolerance)) continue;

                int x = currentIndex % cel.Width;
                int y = currentIndex / cel.Width;
                positions.Add(new Vector2Int(x, y));
                EnqueueMatchingNeighbour(currentIndex - 1, x > 0, pixels, targetColor, clampedTolerance);
                EnqueueMatchingNeighbour(currentIndex + 1, x < cel.Width - 1, pixels, targetColor, clampedTolerance);
                EnqueueMatchingNeighbour(currentIndex - cel.Width, y > 0, pixels, targetColor, clampedTolerance);
                EnqueueMatchingNeighbour(currentIndex + cel.Width, y < cel.Height - 1, pixels, targetColor, clampedTolerance);
            }

            return positions;
        }

        private static void AddChangeIfNeeded(SpriteCel cel, Vector2Int position, Color color, List<PixelChange> changes)
        {
            if (cel.Contains(position) && !SpriteColorMath.AreEqual(cel.GetPixel(position), color))
            {
                changes.Add(new PixelChange(position, cel.GetPixel(position), color));
            }
        }

        private static void EnqueueMatchingNeighbour(int index, bool isWithinBounds, Color32[] pixels, Color32 targetColor, int tolerance)
        {
            if (isWithinBounds && !FloodFillBuffer.IsVisited(index) && IsWithinTolerance(pixels[index], targetColor, tolerance))
            {
                FloodFillBuffer.MarkVisited(index);
                FloodFillBuffer.Enqueue(index);
            }
        }

        private static bool IsWithinTolerance(Color32 candidate, Color32 target, int tolerance)
        {
            if (candidate.a == 0 && target.a == 0)
            {
                // Fully transparent pixels are visually identical regardless of their stored RGB data.
                return true;
            }

            if (Mathf.Abs(candidate.a - target.a) > tolerance)
            {
                return false;
            }

            return Mathf.Abs(candidate.r - target.r) <= tolerance
                && Mathf.Abs(candidate.g - target.g) <= tolerance
                && Mathf.Abs(candidate.b - target.b) <= tolerance;
        }

        private sealed class FloodFillWorkspace
        {
            private int[] queue = new int[0];
            private int[] visitMarks = new int[0];
            private int head;
            private int tail;
            private int visitToken;

            public bool HasItems => head < tail;

            public void Begin(int capacity)
            {
                if (queue.Length < capacity)
                {
                    queue = new int[capacity];
                    visitMarks = new int[capacity];
                }

                head = 0;
                tail = 0;
                if (visitToken == int.MaxValue)
                {
                    System.Array.Clear(visitMarks, 0, visitMarks.Length);
                    visitToken = 1;
                }
                else
                {
                    visitToken++;
                }
            }

            public void Enqueue(int index)
            {
                queue[tail++] = index;
            }

            public int Dequeue()
            {
                return queue[head++];
            }

            public bool IsVisited(int index)
            {
                return visitMarks[index] == visitToken;
            }

            public void MarkVisited(int index)
            {
                visitMarks[index] = visitToken;
            }
        }
    }

    public readonly struct PixelChange
    {
        public Vector2Int Position { get; }
        public Color PreviousColor { get; }
        public Color NewColor { get; }

        public PixelChange(Vector2Int position, Color previousColor, Color newColor)
        {
            Position = position;
            PreviousColor = previousColor;
            NewColor = newColor;
        }
    }
}
