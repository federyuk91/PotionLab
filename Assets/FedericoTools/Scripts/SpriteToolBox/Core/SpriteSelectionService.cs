using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SpriteSelectionService
    {
        private static readonly IReadOnlyList<Vector2Int> EmptyPositions = new Vector2Int[0];
        private IReadOnlyList<Vector2Int> positions = EmptyPositions;

        public RectInt Rect { get; private set; }
        public bool HasSelection { get; private set; }
        public SpriteSelectionClipboard Clipboard { get; private set; }
        public IReadOnlyList<Vector2Int> Positions
        {
            get
            {
                if (!IsMagicSelection && positions.Count == 0 && Rect.width > 0 && Rect.height > 0)
                {
                    positions = CreateRectPositions(Rect);
                }

                return positions;
            }
        }
        public bool IsMagicSelection { get; private set; }

        public bool SetRect(RectInt value)
        {
            if (Rect == value && !IsMagicSelection)
            {
                return false;
            }

            Rect = value;
            positions = EmptyPositions;
            IsMagicSelection = false;
            return true;
        }

        public bool SetMagicPositions(IReadOnlyList<Vector2Int> positions)
        {
            if (positions == null || positions.Count == 0)
            {
                return SetActive(false);
            }

            List<Vector2Int> copiedPositions = new List<Vector2Int>(positions);
            Rect = CalculateBounds(copiedPositions);
            this.positions = copiedPositions;
            IsMagicSelection = true;
            return true;
        }

        public bool SetActive(bool value)
        {
            if (HasSelection == value)
            {
                return false;
            }

            HasSelection = value;
            return true;
        }

        public bool SetClipboard(SpriteSelectionClipboard value)
        {
            if (ReferenceEquals(Clipboard, value))
            {
                return false;
            }

            Clipboard = value;
            return true;
        }

        public void Reset()
        {
            Rect = new RectInt();
            HasSelection = false;
            Clipboard = null;
            positions = EmptyPositions;
            IsMagicSelection = false;
        }

        private static List<Vector2Int> CreateRectPositions(RectInt rect)
        {
            List<Vector2Int> positions = new List<Vector2Int>(rect.width * rect.height);
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++) positions.Add(new Vector2Int(x, y));
            }
            return positions;
        }

        private static RectInt CalculateBounds(IReadOnlyList<Vector2Int> positions)
        {
            int minX = positions[0].x;
            int maxX = minX;
            int minY = positions[0].y;
            int maxY = minY;
            for (int index = 1; index < positions.Count; index++)
            {
                Vector2Int position = positions[index];
                minX = Mathf.Min(minX, position.x);
                maxX = Mathf.Max(maxX, position.x);
                minY = Mathf.Min(minY, position.y);
                maxY = Mathf.Max(maxY, position.y);
            }
            return new RectInt(minX, minY, maxX - minX + 1, maxY - minY + 1);
        }
    }
}
