using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    internal sealed class SpriteToolboxSelectionApplicationService
    {
        private readonly SpriteSelectionService selection = new SpriteSelectionService();
        private readonly Action changed;
        private SpriteToolboxFloatingSelection floatingSelection;

        public SpriteToolboxSelectionApplicationService(Action changed)
        {
            this.changed = changed ?? throw new ArgumentNullException(nameof(changed));
        }

        public RectInt Rect => selection.Rect;
        public bool HasSelection => selection.HasSelection;
        public SpriteSelectionClipboard Clipboard => selection.Clipboard;
        public IReadOnlyList<Vector2Int> Positions => selection.Positions;
        public bool IsMagicSelection => selection.IsMagicSelection;
        public SpriteToolboxFloatingSelection FloatingSelection => floatingSelection;
        public bool HasFloatingSelection => floatingSelection != null;

        public void Reset()
        {
            selection.Reset();
            floatingSelection = null;
        }

        public void SetRect(RectInt value)
        {
            if (selection.SetRect(value)) changed();
        }

        public void SetMagicPositions(IReadOnlyList<Vector2Int> positions)
        {
            if (selection.SetMagicPositions(positions)) changed();
        }

        public void SetActive(bool value)
        {
            if (selection.SetActive(value)) changed();
        }

        public bool Invert(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                return false;
            }

            HashSet<Vector2Int> selectedPositions = new HashSet<Vector2Int>();
            if (HasSelection)
            {
                foreach (Vector2Int position in Positions)
                {
                    selectedPositions.Add(position);
                }
            }

            List<Vector2Int> invertedPositions = new List<Vector2Int>(width * height - selectedPositions.Count);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!selectedPositions.Contains(position))
                    {
                        invertedPositions.Add(position);
                    }
                }
            }

            bool didChange;
            if (invertedPositions.Count == 0)
            {
                didChange = selection.SetActive(false);
            }
            else
            {
                didChange = selection.SetMagicPositions(invertedPositions);
                didChange |= selection.SetActive(true);
            }

            if (didChange)
            {
                changed();
            }

            return didChange;
        }

        public bool CombineMagicPositions(IReadOnlyList<Vector2Int> positions, bool add)
        {
            if (positions == null || positions.Count == 0)
            {
                return false;
            }

            List<Vector2Int> combinedPositions = HasSelection ? new List<Vector2Int>(Positions) : new List<Vector2Int>();
            HashSet<Vector2Int> combinedPositionSet = new HashSet<Vector2Int>(combinedPositions);
            bool didChange = false;
            if (add)
            {
                foreach (Vector2Int position in positions)
                {
                    if (combinedPositionSet.Add(position))
                    {
                        combinedPositions.Add(position);
                        didChange = true;
                    }
                }
            }
            else
            {
                HashSet<Vector2Int> positionsToRemove = new HashSet<Vector2Int>(positions);
                didChange = combinedPositions.RemoveAll(position => positionsToRemove.Contains(position)) > 0;
            }

            if (!didChange)
            {
                return false;
            }

            if (combinedPositions.Count == 0)
            {
                selection.SetActive(false);
            }
            else
            {
                selection.SetMagicPositions(combinedPositions);
                selection.SetActive(true);
            }

            changed();
            return true;
        }

        public bool CombineRect(RectInt rect, bool add)
        {
            if (rect.width < 1 || rect.height < 1)
            {
                return false;
            }

            List<Vector2Int> positions = new List<Vector2Int>(rect.width * rect.height);
            for (int y = rect.yMin; y < rect.yMax; y++)
            {
                for (int x = rect.xMin; x < rect.xMax; x++)
                {
                    positions.Add(new Vector2Int(x, y));
                }
            }

            return CombineMagicPositions(positions, add);
        }

        public void SetClipboard(SpriteSelectionClipboard value)
        {
            if (selection.SetClipboard(value)) changed();
        }

        public bool Copy(SpriteCel cel)
        {
            if (cel == null || !HasSelection) return false;
            SetClipboard(new SpriteSelectionClipboard(Rect.width, Rect.height, CapturePixels(cel, Rect, Positions)));
            return true;
        }

        public bool Cut(SpriteCel cel, Func<SpriteCel, IReadOnlyList<PixelChange>, bool> apply)
        {
            if (cel == null || !Copy(cel)) return false;
            return apply(cel, CreateSelectionFillChanges(cel, Positions, Color.clear));
        }

        public bool BeginPaste(SpriteDocument document, SpriteCel cel)
        {
            if (document == null || cel == null || Clipboard == null || floatingSelection != null) return false;
            Vector2Int destination = HasSelection ? Rect.position + Vector2Int.one : Vector2Int.zero;
            destination.x = Mathf.Clamp(destination.x, 0, document.Width - Clipboard.Width);
            destination.y = Mathf.Clamp(destination.y, 0, document.Height - Clipboard.Height);
            SelectionState previousSelection = CaptureSelectionState();
            floatingSelection = new SpriteToolboxFloatingSelection(new RectInt(destination.x, destination.y, Clipboard.Width, Clipboard.Height), Clipboard.CopyPixels(), null, false, previousSelection);
            SetRect(new RectInt(destination.x, destination.y, Clipboard.Width, Clipboard.Height));
            SetActive(true);
            return true;
        }

        public bool Fill(SpriteCel cel, Color color, Func<SpriteCel, IReadOnlyList<PixelChange>, bool> apply)
        {
            return cel != null && HasSelection && apply(cel, CreateSelectionFillChanges(cel, Positions, color));
        }

        public bool Flip(SpriteCel cel, bool horizontally, Func<SpriteCel, IReadOnlyList<PixelChange>, bool> apply)
        {
            if (cel == null || !HasSelection) return false;
            if (IsMagicSelection) return FlipMagicSelection(cel, horizontally, apply);
            Color32[] sourcePixels = CapturePixels(cel, Rect, Positions);
            List<PixelChange> changes = new List<PixelChange>();
            for (int y = 0; y < Rect.height; y++)
            {
                for (int x = 0; x < Rect.width; x++)
                {
                    int sourceX = horizontally ? Rect.width - 1 - x : x;
                    int sourceY = horizontally ? y : Rect.height - 1 - y;
                    Vector2Int destination = new Vector2Int(Rect.x + x, Rect.y + y);
                    Color32 color = sourcePixels[sourceY * Rect.width + sourceX];
                    Color previousColor = cel.GetPixel(destination);
                    if (!SpriteColorMath.AreEqual(previousColor, color)) changes.Add(new PixelChange(destination, previousColor, color));
                }
            }
            return apply(cel, changes);
        }

        private bool FlipMagicSelection(SpriteCel cel, bool horizontally, Func<SpriteCel, IReadOnlyList<PixelChange>, bool> apply)
        {
            IReadOnlyList<Vector2Int> sourcePositions = Positions;
            List<Vector2Int> destinationPositions = new List<Vector2Int>(sourcePositions.Count);
            Dictionary<Vector2Int, PixelChange> changes = new Dictionary<Vector2Int, PixelChange>();
            foreach (Vector2Int sourcePosition in sourcePositions)
            {
                AddChange(changes, cel, sourcePosition, Color.clear);
            }

            foreach (Vector2Int sourcePosition in sourcePositions)
            {
                Vector2Int destinationPosition = horizontally
                    ? new Vector2Int(Rect.xMax - 1 - (sourcePosition.x - Rect.x), sourcePosition.y)
                    : new Vector2Int(sourcePosition.x, Rect.yMax - 1 - (sourcePosition.y - Rect.y));
                destinationPositions.Add(destinationPosition);
                AddChange(changes, cel, destinationPosition, cel.GetPixel(sourcePosition));
            }

            SetMagicPositions(destinationPositions);
            return apply(cel, new List<PixelChange>(changes.Values));
        }

        public bool BeginMove(SpriteDocument document, SpriteCel cel)
        {
            if (document == null || cel == null || !HasSelection || floatingSelection != null)
            {
                return false;
            }

            RectInt origin = Rect;
            Color32[] pixels = CapturePixels(cel, origin, Positions);
            floatingSelection = new SpriteToolboxFloatingSelection(origin, pixels, Positions, IsMagicSelection, CaptureSelectionState());
            changed();
            return true;
        }

        public bool MoveFloatingSelection(SpriteDocument document, Vector2Int requestedDelta)
        {
            if (document == null || floatingSelection == null) return false;
            RectInt destination = TranslateRect(document, floatingSelection.SourceRect, requestedDelta);
            if (floatingSelection.Translation == requestedDelta) return false;
            floatingSelection.SetTranslation(requestedDelta);
            if (floatingSelection.IsMagicSelection) SetMagicPositions(floatingSelection.CurrentPositions);
            else SetRect(destination);
            SetActive(true);
            return true;
        }

        public bool SetFloatingSelectionRotation(float angleDegrees)
        {
            if (floatingSelection == null) return false;
            floatingSelection.SetRotation(angleDegrees);
            SetRect(floatingSelection.Rect);
            SetActive(true);
            return true;
        }

        public bool SetFloatingSelectionRotationMode(bool value)
        {
            if (floatingSelection == null || floatingSelection.IsRotationMode == value) return false;
            floatingSelection.IsRotationMode = value;
            changed();
            return true;
        }

        public bool SetFloatingSelectionScale(Vector2 scale)
        {
            if (floatingSelection == null) return false;
            floatingSelection.SetScale(scale);
            SetRect(floatingSelection.Rect);
            SetActive(true);
            return true;
        }

        public bool SetFloatingSelectionScaleMode(bool value)
        {
            if (floatingSelection == null || floatingSelection.IsScaleMode == value) return false;
            floatingSelection.IsScaleMode = value;
            if (value) floatingSelection.IsRotationMode = false;
            changed();
            return true;
        }

        public bool SetFloatingSelectionTransform(RectInt rect)
        {
            if (floatingSelection == null || rect.width < 1 || rect.height < 1) return false;
            floatingSelection.SetTransformRect(rect);
            SetRect(rect);
            SetActive(true);
            return true;
        }

        public bool SetFloatingSelectionTransformMode(bool value)
        {
            if (floatingSelection == null || floatingSelection.IsTransformMode == value) return false;
            floatingSelection.IsTransformMode = value;
            if (value)
            {
                floatingSelection.IsRotationMode = false;
                floatingSelection.IsScaleMode = false;
            }

            changed();
            return true;
        }

        public bool CommitFloatingSelection(SpriteDocument document, SpriteCel cel, Func<SpriteCel, IReadOnlyList<PixelChange>, bool> apply)
        {
            if (document == null || cel == null || floatingSelection == null) return false;
            Dictionary<Vector2Int, PixelChange> changes = new Dictionary<Vector2Int, PixelChange>();
            foreach (Vector2Int sourcePosition in floatingSelection.SourcePositions)
            {
                AddChange(changes, cel, sourcePosition, Color.clear);
            }

            AddPixelArrayChanges(changes, cel, floatingSelection.Rect, floatingSelection.CopyPixels());
            if (floatingSelection.IsMagicSelection) SetMagicPositions(floatingSelection.CurrentPositions);
            else SetRect(floatingSelection.Rect);
            SetActive(true);
            floatingSelection = null;
            changed();
            return apply(cel, new List<PixelChange>(changes.Values));
        }

        public bool CancelFloatingSelection()
        {
            if (floatingSelection == null) return false;
            RestoreSelectionState(floatingSelection.PreviousSelection);
            floatingSelection = null;
            changed();
            return true;
        }

        private static RectInt TranslateRect(SpriteDocument document, RectInt source, Vector2Int delta)
        {
            int x = Mathf.Clamp(source.x + delta.x, 0, document.Width - source.width);
            int y = Mathf.Clamp(source.y + delta.y, 0, document.Height - source.height);
            return new RectInt(x, y, source.width, source.height);
        }

        private static Color32[] CapturePixels(SpriteCel cel, RectInt rect, IReadOnlyList<Vector2Int> positions)
        {
            Color32[] pixels = new Color32[rect.width * rect.height];
            foreach (Vector2Int position in positions)
            {
                if (rect.Contains(position))
                {
                    int index = (position.y - rect.y) * rect.width + position.x - rect.x;
                    pixels[index] = cel.GetPixel32(position);
                }
            }
            return pixels;
        }

        private static List<PixelChange> CreateRectFillChanges(SpriteCel cel, RectInt rect, Color color)
        {
            Dictionary<Vector2Int, PixelChange> changes = new Dictionary<Vector2Int, PixelChange>();
            AddRectFillChanges(changes, cel, rect, color);
            return new List<PixelChange>(changes.Values);
        }

        private static List<PixelChange> CreateSelectionFillChanges(SpriteCel cel, IReadOnlyList<Vector2Int> positions, Color color)
        {
            Dictionary<Vector2Int, PixelChange> changes = new Dictionary<Vector2Int, PixelChange>();
            foreach (Vector2Int position in positions) AddChange(changes, cel, position, color);
            return new List<PixelChange>(changes.Values);
        }

        private static void AddRectFillChanges(Dictionary<Vector2Int, PixelChange> changes, SpriteCel cel, RectInt rect, Color color)
        {
            for (int y = 0; y < rect.height; y++)
            {
                for (int x = 0; x < rect.width; x++) AddChange(changes, cel, new Vector2Int(rect.x + x, rect.y + y), color);
            }
        }

        private static void AddPixelArrayChanges(Dictionary<Vector2Int, PixelChange> changes, SpriteCel cel, RectInt rect, Color32[] pixels)
        {
            for (int y = 0; y < rect.height; y++)
            {
                for (int x = 0; x < rect.width; x++)
                {
                    Color32 sourceColor = pixels[y * rect.width + x];
                    if (sourceColor.a > 0)
                    {
                        AddChange(changes, cel, new Vector2Int(rect.x + x, rect.y + y), sourceColor);
                    }
                }
            }
        }

        private static void AddChange(Dictionary<Vector2Int, PixelChange> changes, SpriteCel cel, Vector2Int position, Color color)
        {
            if (!cel.Contains(position)) return;
            PixelChange existing;
            if (changes.TryGetValue(position, out existing)) changes[position] = new PixelChange(position, existing.PreviousColor, color);
            else if (!SpriteColorMath.AreEqual(cel.GetPixel(position), color)) changes.Add(position, new PixelChange(position, cel.GetPixel(position), color));
        }

        private SelectionState CaptureSelectionState()
        {
            IReadOnlyList<Vector2Int> positions = IsMagicSelection ? Positions : new Vector2Int[0];
            return new SelectionState(Rect, HasSelection, IsMagicSelection, positions);
        }

        private void RestoreSelectionState(SelectionState state)
        {
            if (state.IsMagicSelection) selection.SetMagicPositions(state.Positions);
            else selection.SetRect(state.Rect);
            selection.SetActive(state.IsActive);
        }

        internal sealed class SelectionState
        {
            public RectInt Rect { get; }
            public bool IsActive { get; }
            public bool IsMagicSelection { get; }
            public IReadOnlyList<Vector2Int> Positions { get; }

            public SelectionState(RectInt rect, bool isActive, bool isMagicSelection, IReadOnlyList<Vector2Int> positions)
            {
                Rect = rect;
                IsActive = isActive;
                IsMagicSelection = isMagicSelection;
                Positions = new List<Vector2Int>(positions);
            }
        }
    }

    public sealed class SpriteToolboxFloatingSelection
    {
        private readonly Color32[] sourcePixels;
        private Color32[] pixels;
        private readonly IReadOnlyList<Vector2Int> sourcePositions;

        public RectInt SourceRect { get; }
        public RectInt Rect { get; private set; }
        public Vector2Int Translation { get; private set; }
        public float RotationDegrees { get; private set; }
        public bool IsRotationMode { get; internal set; }
        public bool IsScaleMode { get; internal set; }
        public bool IsTransformMode { get; internal set; }
        public Vector2 Scale { get; private set; } = Vector2.one;
        public IReadOnlyList<Vector2Int> SourcePositions => sourcePositions;
        public bool IsMagicSelection { get; }
        internal SpriteToolboxSelectionApplicationService.SelectionState PreviousSelection { get; }

        internal SpriteToolboxFloatingSelection(RectInt sourceRect, Color32[] pixels, IReadOnlyList<Vector2Int> sourcePositions, bool isMagicSelection, SpriteToolboxSelectionApplicationService.SelectionState previousSelection)
        {
            SourceRect = sourceRect;
            Rect = sourceRect;
            sourcePixels = (Color32[])pixels.Clone();
            this.pixels = (Color32[])pixels.Clone();
            this.sourcePositions = sourcePositions == null ? new List<Vector2Int>() : new List<Vector2Int>(sourcePositions);
            IsMagicSelection = isMagicSelection;
            PreviousSelection = previousSelection;
        }

        public IReadOnlyList<Vector2Int> CurrentPositions
        {
            get
            {
                Vector2Int delta = Rect.position - SourceRect.position;
                List<Vector2Int> positions = new List<Vector2Int>(sourcePositions.Count);
                foreach (Vector2Int sourcePosition in sourcePositions)
                {
                    positions.Add(sourcePosition + delta);
                }

                return positions;
            }
        }

        public Color32[] CopyPixels()
        {
            return (Color32[])pixels.Clone();
        }

        internal void SetRect(RectInt value)
        {
            Rect = value;
        }

        internal void SetTranslation(Vector2Int value)
        {
            Translation = value;
            ApplyRotation();
        }

        internal void SetRotation(float angleDegrees)
        {
            RotationDegrees = angleDegrees;
            ApplyRotation();
        }

        internal void SetScale(Vector2 value)
        {
            Scale = new Vector2(Mathf.Clamp(value.x, 0.1f, 8f), Mathf.Clamp(value.y, 0.1f, 8f));
            ApplyRotation();
        }

        internal void SetTransformRect(RectInt value)
        {
            RotationDegrees = 0f;
            Scale = new Vector2(value.width / (float)SourceRect.width, value.height / (float)SourceRect.height);
            Translation = value.position - SourceRect.position;
            pixels = ScalePixels(sourcePixels, SourceRect.width, SourceRect.height, value.width, value.height);
            Rect = value;
        }

        private void ApplyRotation()
        {
            SpriteRotatedPixels rotated = SpriteRotSpriteRotationService.Rotate(sourcePixels, SourceRect.width, SourceRect.height, RotationDegrees);
            int scaledWidth = Mathf.Max(1, Mathf.RoundToInt(rotated.Bounds.width * Scale.x));
            int scaledHeight = Mathf.Max(1, Mathf.RoundToInt(rotated.Bounds.height * Scale.y));
            pixels = ScalePixels(rotated.Pixels, rotated.Bounds.width, rotated.Bounds.height, scaledWidth, scaledHeight);
            int x = SourceRect.x + Translation.x + rotated.Bounds.x - (scaledWidth - rotated.Bounds.width) / 2;
            int y = SourceRect.y + Translation.y + rotated.Bounds.y - (scaledHeight - rotated.Bounds.height) / 2;
            Rect = new RectInt(x, y, scaledWidth, scaledHeight);
        }

        private static Color32[] ScalePixels(Color32[] sourcePixels, int sourceWidth, int sourceHeight, int destinationWidth, int destinationHeight)
        {
            Color32[] result = new Color32[destinationWidth * destinationHeight];
            for (int y = 0; y < destinationHeight; y++)
            {
                int sourceY = Mathf.Min(sourceHeight - 1, Mathf.FloorToInt(y * sourceHeight / (float)destinationHeight));
                for (int x = 0; x < destinationWidth; x++)
                {
                    int sourceX = Mathf.Min(sourceWidth - 1, Mathf.FloorToInt(x * sourceWidth / (float)destinationWidth));
                    result[y * destinationWidth + x] = sourcePixels[sourceY * sourceWidth + sourceX];
                }
            }

            return result;
        }
    }

    public readonly struct SpriteRotatedPixels
    {
        public RectInt Bounds { get; }
        public Color32[] Pixels { get; }

        public SpriteRotatedPixels(RectInt bounds, Color32[] pixels)
        {
            Bounds = bounds;
            Pixels = pixels;
        }
    }

    public static class SpriteRotSpriteRotationService
    {
        private const int UpscaleFactor = 4;

        public static SpriteRotatedPixels Rotate(Color32[] sourcePixels, int width, int height, float angleDegrees)
        {
            if (sourcePixels == null) throw new ArgumentNullException(nameof(sourcePixels));
            if (width < 1 || height < 1 || sourcePixels.Length != width * height) throw new ArgumentException("The source pixels must match the supplied dimensions.");

            Color32[] scaledPixels = Scale2x(Scale2x(sourcePixels, width, height), width * 2, height * 2);
            float radians = angleDegrees * Mathf.Deg2Rad;
            float cosine = Mathf.Cos(radians);
            float sine = Mathf.Sin(radians);
            float pivotX = width * 0.5f;
            float pivotY = height * 0.5f;
            RectInt bounds = CalculateBounds(width, height, pivotX, pivotY, cosine, sine);
            Color32[] result = new Color32[bounds.width * bounds.height];
            float scaledPivotX = pivotX * UpscaleFactor - 0.5f;
            float scaledPivotY = pivotY * UpscaleFactor - 0.5f;
            for (int y = 0; y < bounds.height; y++)
            {
                for (int x = 0; x < bounds.width; x++)
                {
                    float destinationX = (bounds.x + x + 0.5f) * UpscaleFactor - 0.5f;
                    float destinationY = (bounds.y + y + 0.5f) * UpscaleFactor - 0.5f;
                    float relativeX = destinationX - scaledPivotX;
                    float relativeY = destinationY - scaledPivotY;
                    int sampleX = Mathf.RoundToInt(scaledPivotX + cosine * relativeX + sine * relativeY);
                    int sampleY = Mathf.RoundToInt(scaledPivotY - sine * relativeX + cosine * relativeY);
                    if (sampleX >= 0 && sampleY >= 0 && sampleX < width * UpscaleFactor && sampleY < height * UpscaleFactor) result[y * bounds.width + x] = scaledPixels[sampleY * width * UpscaleFactor + sampleX];
                }
            }

            return new SpriteRotatedPixels(bounds, result);
        }

        private static RectInt CalculateBounds(int width, int height, float pivotX, float pivotY, float cosine, float sine)
        {
            Vector2[] corners = { new Vector2(0f, 0f), new Vector2(width, 0f), new Vector2(0f, height), new Vector2(width, height) };
            float minimumX = float.MaxValue;
            float minimumY = float.MaxValue;
            float maximumX = float.MinValue;
            float maximumY = float.MinValue;
            foreach (Vector2 corner in corners)
            {
                float relativeX = corner.x - pivotX;
                float relativeY = corner.y - pivotY;
                float rotatedX = pivotX + cosine * relativeX - sine * relativeY;
                float rotatedY = pivotY + sine * relativeX + cosine * relativeY;
                minimumX = Mathf.Min(minimumX, rotatedX);
                minimumY = Mathf.Min(minimumY, rotatedY);
                maximumX = Mathf.Max(maximumX, rotatedX);
                maximumY = Mathf.Max(maximumY, rotatedY);
            }

            int xMin = Mathf.FloorToInt(minimumX);
            int yMin = Mathf.FloorToInt(minimumY);
            int xMax = Mathf.CeilToInt(maximumX);
            int yMax = Mathf.CeilToInt(maximumY);
            return new RectInt(xMin, yMin, Mathf.Max(1, xMax - xMin), Mathf.Max(1, yMax - yMin));
        }

        private static Color32[] Scale2x(Color32[] sourcePixels, int width, int height)
        {
            Color32[] result = new Color32[width * height * 4];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color32 center = GetPixel(sourcePixels, width, height, x, y);
                    Color32 top = GetPixel(sourcePixels, width, height, x, y + 1);
                    Color32 left = GetPixel(sourcePixels, width, height, x - 1, y);
                    Color32 right = GetPixel(sourcePixels, width, height, x + 1, y);
                    Color32 bottom = GetPixel(sourcePixels, width, height, x, y - 1);
                    int resultIndex = y * 2 * width * 2 + x * 2;
                    bool canInterpolate = !SpriteColorMath.AreEqual(top, bottom) && !SpriteColorMath.AreEqual(left, right);
                    result[resultIndex] = canInterpolate && SpriteColorMath.AreEqual(left, top) ? top : center;
                    result[resultIndex + 1] = canInterpolate && SpriteColorMath.AreEqual(top, right) ? right : center;
                    result[resultIndex + width * 2] = canInterpolate && SpriteColorMath.AreEqual(left, bottom) ? left : center;
                    result[resultIndex + width * 2 + 1] = canInterpolate && SpriteColorMath.AreEqual(bottom, right) ? bottom : center;
                }
            }

            return result;
        }

        private static Color32 GetPixel(Color32[] pixels, int width, int height, int x, int y)
        {
            x = Mathf.Clamp(x, 0, width - 1);
            y = Mathf.Clamp(y, 0, height - 1);
            return pixels[y * width + x];
        }
    }
}
