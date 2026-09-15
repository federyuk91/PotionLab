using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public static class SpriteBrushShapeUtility
    {
        public static bool ContainsOffset(int offsetX, int offsetY, int brushSize, SpriteBrushShape shape)
        {
            if (shape == SpriteBrushShape.Square)
            {
                return true;
            }

            int minimumOffset = -(brushSize - 1) / 2;
            int maximumOffset = minimumOffset + brushSize - 1;
            float center = (minimumOffset + maximumOffset) * 0.5f;
            float radius = (brushSize - 1) * 0.5f + 0.25f;
            float horizontalDistance = offsetX - center;
            float verticalDistance = offsetY - center;
            return horizontalDistance * horizontalDistance + verticalDistance * verticalDistance <= radius * radius;
        }
    }

    public interface ISpriteToolboxState
    {
        int DocumentWidth { get; }
        int DocumentHeight { get; }
        int SelectedFrameIndex { get; }
        int SelectedLayerIndex { get; }
        int ActiveAnimationTagIndex { get; }
        bool IsPreviewPlaying { get; }
        SpriteToolboxTool ActiveTool { get; }
        int BrushSize { get; }
        SpriteBrushShape BrushShape { get; }
        int FillTolerance { get; }
        SpriteSelectionMode SelectionMode { get; }
        bool HorizontalSymmetry { get; }
        bool VerticalSymmetry { get; }
        bool TiledMode { get; }
        bool PixelPerfectPencil { get; }
        Color PrimaryColor { get; }
        Color SecondaryColor { get; }
        IReadOnlyList<Color> Palette { get; }
        IReadOnlyList<SpriteToolboxLayerState> Layers { get; }
        IReadOnlyList<SpriteToolboxFrameState> Frames { get; }
        IReadOnlyList<SpriteToolboxAnimationTagState> AnimationTags { get; }
        bool CelHasVisiblePixels(int frameIndex, int layerIndex);
        RectInt SelectionRect { get; }
        bool HasPixelSelection { get; }
        bool HasSelectionClipboard { get; }
    }

    public interface ICanvasActions
    {
        SpriteToolboxOperationResult Paint(Vector2Int position, Color color);
        SpriteToolboxOperationResult Erase(Vector2Int position);
        SpriteToolboxOperationResult Fill(Vector2Int position, Color color);
        SpriteToolboxOperationResult DrawLine(Vector2Int start, Vector2Int end, Color color);
        SpriteToolboxOperationResult BeginStroke(Vector2Int position);
        SpriteToolboxOperationResult ContinueStroke(Vector2Int position);
        SpriteToolboxOperationResult CommitStroke();
        SpriteToolboxOperationResult CancelStroke();
        Color PickColor(Vector2Int position);
        bool CopySelection();
        bool CutSelection();
        bool PasteSelection();
        bool BeginFloatingMoveSelection();
        bool MoveFloatingSelection(Vector2Int requestedDelta);
        bool CommitFloatingSelection();
        bool CancelFloatingSelection();
    }
    public interface ITimelineActions
    {
        void SelectFrame(int frameIndex);
        void SelectLayer(int layerIndex);
        void SelectCell(int frameIndex, int layerIndex);
        bool AddFrame(bool duplicateCurrentFrame);
        bool RemoveFrame();
        bool MoveFrame(int direction);
        bool MoveFrameTo(int sourceIndex, int destinationIndex);
        bool AddLayer(string name);
        bool DuplicateLayer();
        bool RemoveLayer();
        bool MoveLayer(int direction);
        bool MoveLayerTo(int sourceIndex, int destinationIndex);
        bool MergeLayerDown();
        bool SetFrameDuration(float duration);
        bool ClearSelectedCel();
        bool SetLayerVisibility(int layerIndex, bool isVisible);
        bool SetLayerLock(int layerIndex, bool isLocked);
        bool SetLayerName(int layerIndex, string name);
        bool SetAllLayersVisibility(bool isVisible);
        bool SetAllLayersLock(bool isLocked);
        bool AddAnimationTag(string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color);
        bool UpdateAnimationTag(int tagIndex, string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color);
        bool RemoveAnimationTag(int tagIndex);
        void SelectAnimationTag(int tagIndex);
        void SetPreviewPlaying(bool value);
    }

    public interface IPaletteActions
    {
        void SetPrimaryColor(Color color);
        void SetSecondaryColor(Color color);
        void SwapColors();
        bool AddPaletteColor(Color color);
        bool SetPalette(IReadOnlyList<Color> colors);
        bool SortPaletteByColorFamilies();
        bool BuildPaletteFromCanvas();
        bool RecolorSelectedFrame(Color sourceColor, Color targetColor);
    }

    public interface IToolActions
    {
        void SetActiveTool(SpriteToolboxTool tool);
        void SetBrushSize(int size);
        void SetBrushShape(SpriteBrushShape shape);
        void SetFillTolerance(int tolerance);
        void SetSelectionMode(SpriteSelectionMode mode);
        void SetHorizontalSymmetry(bool value);
        void SetVerticalSymmetry(bool value);
        void SetTiledMode(bool value);
        void SetPixelPerfectPencil(bool value);
        bool CropCanvasToSelection();
        bool CopyPixelSelection();
        bool CutPixelSelection();
        bool PastePixelSelection();
        bool FillPixelSelection(Color color);
        bool ClearPixelSelectionPixels();
        bool FlipPixelSelection(bool horizontally);
        bool InvertPixelSelection();
        bool RecolorSelectedFrame(Color sourceColor, Color targetColor);
    }

    public interface IRecolorActions
    {
        IReadOnlyList<Color> GetCanvasColors();
        Color32[] GetSelectedLayerPixels();
        SpriteCanvasColorMap CreateCanvasColorMap();
        IReadOnlyList<SpriteRecolorMapping> CreateColorReductionMappings(SpriteCanvasColorMap sourceMap, int targetColorCount);
        bool ApplyRecolorMappings(IReadOnlyList<SpriteRecolorMapping> mappings, SpriteCanvasColorMap sourceMap);
        bool MapSelectedCelToPalette(IReadOnlyList<Color> palette, bool useDithering);
    }

    public interface ISpriteToolboxCanvasContext : ISpriteToolboxState, IToolActions
    {
    }

    public interface ISpriteToolboxTimelineContext : ISpriteToolboxState, ITimelineActions
    {
    }

    public interface ISpriteToolboxPaletteContext : ISpriteToolboxState, IPaletteActions
    {
    }

    public interface ISpriteToolboxToolsContext : ISpriteToolboxState, IToolActions, IRecolorActions, IPaletteActions
    {
    }

    public sealed class SpriteCanvasColorMap
    {
        private static readonly IReadOnlyList<Vector2Int> EmptyPositions = new Vector2Int[0];
        private readonly IReadOnlyList<Color> colors;
        private readonly IReadOnlyDictionary<Color32, IReadOnlyList<Vector2Int>> positionsByColor;

        public SpriteCanvasColorMap(
            IReadOnlyList<Color> colors,
            IReadOnlyDictionary<Color32, IReadOnlyList<Vector2Int>> positionsByColor)
        {
            this.colors = colors;
            this.positionsByColor = positionsByColor;
        }

        public IReadOnlyList<Color> Colors => colors;

        public IReadOnlyList<Vector2Int> GetPositions(Color sourceColor)
        {
            return positionsByColor.TryGetValue((Color32)sourceColor, out IReadOnlyList<Vector2Int> positions)
                ? positions
                : EmptyPositions;
        }
    }

    public readonly struct SpriteRecolorMapping
    {
        public Color Source { get; }
        public Color Target { get; }

        public SpriteRecolorMapping(Color source, Color target)
        {
            Source = source;
            Target = target;
        }
    }

    public readonly struct SpriteToolboxLayerState
    {
        public string Name { get; }
        public bool IsVisible { get; }
        public bool IsLocked { get; }
        public float Opacity { get; }

        public SpriteToolboxLayerState(string name, bool isVisible, bool isLocked, float opacity)
        {
            Name = name;
            IsVisible = isVisible;
            IsLocked = isLocked;
            Opacity = opacity;
        }
    }

    public readonly struct SpriteToolboxFrameState
    {
        public float Duration { get; }

        public SpriteToolboxFrameState(float duration)
        {
            Duration = duration;
        }
    }

    public readonly struct SpriteToolboxAnimationTagState
    {
        public string Name { get; }
        public int FromFrame { get; }
        public int ToFrame { get; }
        public SpriteAnimationDirection Direction { get; }
        public Color Color { get; }

        public SpriteToolboxAnimationTagState(
            string name,
            int fromFrame,
            int toFrame,
            SpriteAnimationDirection direction,
            Color color)
        {
            Name = name;
            FromFrame = fromFrame;
            ToFrame = toFrame;
            Direction = direction;
            Color = color;
        }
    }
}
