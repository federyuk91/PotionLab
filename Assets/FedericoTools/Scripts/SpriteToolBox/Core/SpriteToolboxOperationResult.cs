using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public enum SpriteToolboxDocumentChangeKind
    {
        Pixels,
        Structure
    }

    public readonly struct SpriteToolboxDocumentChange
    {
        public SpriteToolboxDocumentChangeKind Kind { get; }
        public int FrameIndex { get; }
        public RectInt InvalidatedBounds { get; }
        public bool IsStructural => Kind == SpriteToolboxDocumentChangeKind.Structure;

        private SpriteToolboxDocumentChange(SpriteToolboxDocumentChangeKind kind, int frameIndex, RectInt invalidatedBounds)
        {
            Kind = kind;
            FrameIndex = frameIndex;
            InvalidatedBounds = invalidatedBounds;
        }

        public static SpriteToolboxDocumentChange Pixels(int frameIndex, RectInt invalidatedBounds)
        {
            return new SpriteToolboxDocumentChange(SpriteToolboxDocumentChangeKind.Pixels, frameIndex, invalidatedBounds);
        }

        public static SpriteToolboxDocumentChange Structure()
        {
            return new SpriteToolboxDocumentChange(SpriteToolboxDocumentChangeKind.Structure, -1, new RectInt());
        }
    }

    public readonly struct SpriteToolboxOperationResult
    {
        private static readonly IReadOnlyList<PixelChange> EmptyChanges = new PixelChange[0];
        public bool DidChange { get; }
        public IReadOnlyList<PixelChange> PixelChanges { get; }
        public RectInt InvalidatedBounds { get; }
        public string Reason { get; }
        private SpriteToolboxOperationResult(bool didChange, IReadOnlyList<PixelChange> pixelChanges, RectInt bounds, string reason) { DidChange = didChange; PixelChanges = pixelChanges; InvalidatedBounds = bounds; Reason = reason; }
        public static SpriteToolboxOperationResult NoChange(string reason) { return new SpriteToolboxOperationResult(false, EmptyChanges, new RectInt(), reason); }
        public static SpriteToolboxOperationResult FromPixelChanges(IReadOnlyList<PixelChange> changes)
        {
            if (changes == null || changes.Count == 0) return NoChange("No pixel changes.");
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int index = 0; index < changes.Count; index++) { Vector2Int point = changes[index].Position; minX = Mathf.Min(minX, point.x); minY = Mathf.Min(minY, point.y); maxX = Mathf.Max(maxX, point.x); maxY = Mathf.Max(maxY, point.y); }
            return new SpriteToolboxOperationResult(true, changes, Rect.MinMaxRect(minX, minY, maxX + 1, maxY + 1).ToRectInt(), string.Empty);
        }
    }

    internal static class RectExtensions { public static RectInt ToRectInt(this Rect value) { return new RectInt(Mathf.RoundToInt(value.xMin), Mathf.RoundToInt(value.yMin), Mathf.RoundToInt(value.width), Mathf.RoundToInt(value.height)); } }
}
