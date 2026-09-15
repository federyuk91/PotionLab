using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public enum SpriteToolboxTool { Pencil, Eraser, Fill, Line, Eyedropper, Select, Recolor, EditorOnly }
    public enum SpriteBrushShape { Square, Round }
    public enum SpriteSelectionMode { Rectangular, Magic }

    /// <summary>Owns editor state and commands without depending on UnityEditor or a concrete UI.</summary>
    public sealed class SpriteToolboxSession : ICanvasActions, ISpriteToolboxCanvasContext, ISpriteToolboxTimelineContext, ISpriteToolboxPaletteContext, ISpriteToolboxToolsContext
    {
        private SpriteDocument document;
        private int selectedFrameIndex;
        private int selectedLayerIndex;
        private int activeAnimationTagIndex = -1;
        private SpriteToolboxTool activeTool = SpriteToolboxTool.Pencil;
        private int brushSize = 1;
        private SpriteBrushShape brushShape = SpriteBrushShape.Square;
        private int fillTolerance;
        private SpriteSelectionMode selectionMode = SpriteSelectionMode.Rectangular;
        private bool horizontalSymmetry;
        private bool verticalSymmetry;
        private bool tiledMode;
        private bool pixelPerfectPencil;
        private Color primaryColor = Color.black;
        private Color secondaryColor = Color.white;
        private readonly SpriteToolboxDocumentApplicationService documentService;
        private readonly SpriteToolboxPaletteApplicationService paletteService;
        private readonly SpriteToolboxPixelApplicationService pixelService;
        private readonly SpriteToolboxSelectionApplicationService selectionService;
        private readonly Stack<SelectionSnapshot> selectionUndoHistory = new Stack<SelectionSnapshot>();
        private readonly Stack<SelectionSnapshot> selectionRedoHistory = new Stack<SelectionSnapshot>();
        private readonly SpriteToolboxStrokeApplicationService strokeService = new SpriteToolboxStrokeApplicationService();
        private static readonly IReadOnlyList<Color> EmptyColors = new Color[0];
        private static readonly IReadOnlyList<SpriteToolboxLayerState> EmptyLayers = new SpriteToolboxLayerState[0];
        private static readonly IReadOnlyList<SpriteToolboxFrameState> EmptyFrames = new SpriteToolboxFrameState[0];
        private static readonly IReadOnlyList<SpriteToolboxAnimationTagState> EmptyAnimationTags = new SpriteToolboxAnimationTagState[0];
        private IReadOnlyList<SpriteToolboxLayerState> cachedLayers;
        private IReadOnlyList<SpriteToolboxFrameState> cachedFrames;
        private IReadOnlyList<SpriteToolboxAnimationTagState> cachedAnimationTags;

        public SpriteDocument Document => document;
        public int SelectedFrameIndex => selectedFrameIndex;
        public int SelectedLayerIndex => selectedLayerIndex;
        public int ActiveAnimationTagIndex => activeAnimationTagIndex;
        public bool IsPreviewPlaying => Playback.IsPlaying;
        public SpriteCommandHistory CommandHistory { get; } = new SpriteCommandHistory(64L * 1024L * 1024L);
        public RectInt SelectionRect => selectionService.Rect;
        public bool IsMagicPixelSelection => selectionService.IsMagicSelection;
        public IReadOnlyList<Vector2Int> PixelSelectionPositions => selectionService.Positions;
        public bool HasPixelSelection => selectionService.HasSelection;
        public bool HasSelectionClipboard => selectionService.Clipboard != null;
        public bool HasFloatingSelection => selectionService.HasFloatingSelection;
        public SpriteToolboxFloatingSelection FloatingSelection => selectionService.FloatingSelection;
        public bool CanUndoSelection => selectionUndoHistory.Count > 0;
        public SpriteSelectionClipboard SelectionClipboard => selectionService.Clipboard;
        public AnimationPlaybackService Playback { get; } = new AnimationPlaybackService();
        public int DocumentWidth => document == null ? 0 : document.Width;
        public int DocumentHeight => document == null ? 0 : document.Height;
        public Color PrimaryColor => primaryColor;
        public Color SecondaryColor => secondaryColor;
        public SpriteBrushShape BrushShape => brushShape;
        public int FillTolerance => fillTolerance;
        public SpriteSelectionMode SelectionMode => selectionMode;
        public IReadOnlyList<Color> Palette => document == null ? EmptyColors : document.Palette;
        public IReadOnlyList<SpriteToolboxLayerState> Layers => GetLayerStates();
        public IReadOnlyList<SpriteToolboxFrameState> Frames => GetFrameStates();
        public IReadOnlyList<SpriteToolboxAnimationTagState> AnimationTags => GetAnimationTagStates();

        public bool CelHasVisiblePixels(int frameIndex, int layerIndex)
        {
            if (document == null || frameIndex < 0 || layerIndex < 0 || frameIndex >= document.Frames.Count || layerIndex >= document.LayerTracks.Count)
            {
                return false;
            }

            Color32[] pixels = document.GetFrame(frameIndex).GetCel(layerIndex).Pixels;
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
            {
                if (pixels[pixelIndex].a > 0)
                {
                    return true;
                }
            }

            return false;
        }

        public event Action DocumentChanged;
        public event Action<SpriteToolboxDocumentChange> DocumentChangedDetailed;
        public event Action SelectionChanged;
        public event Action ToolSettingsChanged;
        public event Action DocumentEdited;

        public SpriteToolboxTool ActiveTool
        {
            get => activeTool;
            set
            {
                if (activeTool == value)
                {
                    return;
                }

                activeTool = value;
                ToolSettingsChanged?.Invoke();
            }
        }

        public int BrushSize
        {
            get => brushSize;
            set
            {
                int clampedSize = Mathf.Clamp(value, 1, 8);
                if (brushSize == clampedSize)
                {
                    return;
                }

                brushSize = clampedSize;
                ToolSettingsChanged?.Invoke();
            }
        }

        public bool HorizontalSymmetry
        {
            get => horizontalSymmetry;
            set => SetToolSetting(ref horizontalSymmetry, value);
        }

        public bool VerticalSymmetry
        {
            get => verticalSymmetry;
            set => SetToolSetting(ref verticalSymmetry, value);
        }

        public bool TiledMode
        {
            get => tiledMode;
            set => SetToolSetting(ref tiledMode, value);
        }

        public bool PixelPerfectPencil
        {
            get => pixelPerfectPencil;
            set => SetToolSetting(ref pixelPerfectPencil, value);
        }

        public void SetPrimaryColor(Color color)
        {
            if (primaryColor == color)
            {
                return;
            }

            primaryColor = color;
            ToolSettingsChanged?.Invoke();
        }

        public void SetSecondaryColor(Color color)
        {
            if (secondaryColor == color)
            {
                return;
            }

            secondaryColor = color;
            ToolSettingsChanged?.Invoke();
        }

        public void SwapColors()
        {
            Color previousPrimaryColor = primaryColor;
            primaryColor = secondaryColor;
            secondaryColor = previousPrimaryColor;
            ToolSettingsChanged?.Invoke();
        }

        public void SetActiveTool(SpriteToolboxTool tool)
        {
            ActiveTool = tool;
        }

        public void SetBrushSize(int size)
        {
            BrushSize = size;
        }

        public void SetBrushShape(SpriteBrushShape shape)
        {
            if (brushShape == shape)
            {
                return;
            }

            brushShape = shape;
            ToolSettingsChanged?.Invoke();
        }

        public void SetFillTolerance(int tolerance)
        {
            int clampedTolerance = Mathf.Clamp(tolerance, 0, 255);
            if (fillTolerance == clampedTolerance) return;
            fillTolerance = clampedTolerance;
            ToolSettingsChanged?.Invoke();
        }

        public void SetSelectionMode(SpriteSelectionMode mode)
        {
            if (selectionMode == mode) return;
            selectionMode = mode;
            ToolSettingsChanged?.Invoke();
        }

        public void SetHorizontalSymmetry(bool value)
        {
            HorizontalSymmetry = value;
        }

        public void SetVerticalSymmetry(bool value)
        {
            VerticalSymmetry = value;
        }

        public void SetTiledMode(bool value)
        {
            TiledMode = value;
        }

        public void SetPixelPerfectPencil(bool value)
        {
            PixelPerfectPencil = value;
        }

        public SpriteToolboxSession()
        {
            selectionService = new SpriteToolboxSelectionApplicationService(() => SelectionChanged?.Invoke());
            documentService = new SpriteToolboxDocumentApplicationService(
                () => document,
                () => selectedFrameIndex,
                () => selectedLayerIndex,
                SelectFrame,
                SelectLayer,
                SelectCell,
                Execute);
            pixelService = new SpriteToolboxPixelApplicationService(
                () => document,
                () => selectedFrameIndex,
                GetUnlockedSelectedCel,
                ExecutePixelChange);
            paletteService = new SpriteToolboxPaletteApplicationService(
                () => document,
                () => selectedFrameIndex,
                () => selectedLayerIndex,
                Execute);
        }

        public SpriteToolboxSession(SpriteDocument document) : this()
        {
            SetDocument(document);
        }

        public void SetDocument(SpriteDocument value)
        {
            if (ReferenceEquals(document, value))
            {
                ClampSelection();
                return;
            }

            document = value;
            CommandHistory.Clear();
            ClearSelectionHistory();
            selectionService.Reset();
            CancelStroke();
            Playback.ResetSchedule();
            ClampSelection();
            NotifyDocumentChanged(SpriteToolboxDocumentChange.Structure());
        }

        public void SelectFrame(int frameIndex)
        {
            int clampedFrameIndex = document == null ? 0 : Mathf.Clamp(frameIndex, 0, document.Frames.Count - 1);
            if (selectedFrameIndex == clampedFrameIndex)
            {
                return;
            }

            selectedFrameIndex = clampedFrameIndex;
            SelectionChanged?.Invoke();
        }

        public void SelectLayer(int layerIndex)
        {
            int clampedLayerIndex = document == null ? 0 : Mathf.Clamp(layerIndex, 0, document.LayerTracks.Count - 1);
            if (selectedLayerIndex == clampedLayerIndex)
            {
                return;
            }

            selectedLayerIndex = clampedLayerIndex;
            SelectionChanged?.Invoke();
        }

        public void SelectCell(int frameIndex, int layerIndex)
        {
            int previousFrameIndex = selectedFrameIndex;
            int previousLayerIndex = selectedLayerIndex;
            selectedFrameIndex = document == null ? 0 : Mathf.Clamp(frameIndex, 0, document.Frames.Count - 1);
            selectedLayerIndex = document == null ? 0 : Mathf.Clamp(layerIndex, 0, document.LayerTracks.Count - 1);
            if (previousFrameIndex != selectedFrameIndex || previousLayerIndex != selectedLayerIndex)
            {
                SelectionChanged?.Invoke();
            }
        }

        public void SelectAnimationTag(int tagIndex)
        {
            int tagCount = document == null ? 0 : document.AnimationTags.Count;
            int clampedTagIndex = tagCount == 0 ? -1 : Mathf.Clamp(tagIndex, -1, tagCount - 1);
            if (activeAnimationTagIndex == clampedTagIndex)
            {
                return;
            }

            activeAnimationTagIndex = clampedTagIndex;
            SelectionChanged?.Invoke();
        }

        public void SetPreviewPlaying(bool value)
        {
            if (Playback.IsPlaying == value)
            {
                return;
            }

            Playback.IsPlaying = value;
            SelectionChanged?.Invoke();
        }

        public void ClampSelection()
        {
            SelectCell(selectedFrameIndex, selectedLayerIndex);
        }

        public void SetPixelSelectionRect(RectInt value)
        {
            selectionService.SetRect(ClampSelectionRectToDocument(value));
        }

        public void SetPixelSelectionActive(bool value)
        {
            if (value && (selectionService.Rect.width < 1 || selectionService.Rect.height < 1))
            {
                value = false;
            }

            selectionService.SetActive(value);
        }

        public void ClearPixelSelection()
        {
            BeginSelectionUndo();
            SetPixelSelectionActive(false);
        }

        public void BeginSelectionUndo()
        {
            selectionUndoHistory.Push(CaptureSelection());
            selectionRedoHistory.Clear();
        }

        public bool UndoSelection()
        {
            if (selectionUndoHistory.Count == 0) return false;
            selectionRedoHistory.Push(CaptureSelection());
            RestoreSelection(selectionUndoHistory.Pop());
            return true;
        }

        public void SetSelectionClipboard(SpriteSelectionClipboard value)
        {
            selectionService.SetClipboard(value);
        }

        public void Execute(ISpriteCommand command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            CommandHistory.Execute(command);
            ClearSelectionHistory();
            NotifyDocumentChanged();
            DocumentEdited?.Invoke();
        }

        public bool ApplyPixelChanges(SpriteCel cel, IReadOnlyList<PixelChange> changes)
        {
            return ApplyPixelChangesResult(cel, changes).DidChange;
        }

        public SpriteToolboxOperationResult ApplyPixelChangesResult(SpriteCel cel, IReadOnlyList<PixelChange> changes)
        {
            return pixelService.Apply(cel, changes);
        }

        public SpriteToolboxOperationResult PaintSelectedCelResult(Vector2Int position, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            return pixelService.Paint(position, color, horizontalSymmetry, verticalSymmetry);
        }

        public SpriteToolboxOperationResult Paint(Vector2Int position, Color color)
        {
            return PaintSelectedCelResult(position, color, HorizontalSymmetry, VerticalSymmetry);
        }

        public SpriteToolboxOperationResult Erase(Vector2Int position)
        {
            return Paint(position, Color.clear);
        }

        public SpriteToolboxOperationResult BeginStroke(Vector2Int position)
        {
            return BeginStroke(position, PrimaryColor);
        }

        public SpriteToolboxOperationResult BeginStroke(Vector2Int position, Color color)
        {
            SpriteCel cel = GetUnlockedSelectedCel();
            return strokeService.Begin(document, cel, CreateStrokeSettings(color), position);
        }

        public SpriteToolboxOperationResult ContinueStroke(Vector2Int position)
        {
            return ContinueStroke(position, PrimaryColor);
        }

        public SpriteToolboxOperationResult ContinueStroke(Vector2Int position, Color color)
        {
            return strokeService.Continue(CreateStrokeSettings(color), position);
        }

        public SpriteToolboxOperationResult CommitStroke()
        {
            return strokeService.Commit(ExecutePixelChange);
        }

        public SpriteToolboxOperationResult CancelStroke()
        {
            return strokeService.Cancel();
        }

        public bool MoveSelectedLayer(int direction)
        {
            return documentService.MoveSelectedLayer(direction);
        }

        public bool AddLayer(string name)
        {
            return documentService.AddLayer(name);
        }

        public bool DuplicateLayer()
        {
            return documentService.DuplicateSelectedLayer();
        }

        public bool AddLayerWithPixels(string name, IReadOnlyList<Color32> pixels)
        {
            return documentService.AddLayerWithPixels(name, selectedFrameIndex, pixels);
        }

        public bool RemoveSelectedLayer()
        {
            return documentService.RemoveSelectedLayer();
        }

        public bool RemoveLayer(int layerIndex)
        {
            return documentService.RemoveLayer(layerIndex);
        }

        public bool RemoveLayer()
        {
            return RemoveSelectedLayer();
        }

        public bool MoveLayer(int direction)
        {
            return MoveSelectedLayer(direction);
        }

        public bool MoveLayerTo(int sourceIndex, int destinationIndex)
        {
            return documentService.MoveLayerTo(sourceIndex, destinationIndex);
        }

        public bool MergeSelectedLayerDown()
        {
            return documentService.MergeSelectedLayerDown();
        }

        public bool MergeLayerDown()
        {
            return MergeSelectedLayerDown();
        }

        public bool AddFrameAfterSelection(bool duplicateCurrentFrame)
        {
            return documentService.AddFrameAfterSelection(duplicateCurrentFrame);
        }

        public bool AddFrame(bool duplicateCurrentFrame)
        {
            return AddFrameAfterSelection(duplicateCurrentFrame);
        }

        public bool RemoveSelectedFrame()
        {
            return documentService.RemoveSelectedFrame();
        }

        public bool RemoveFrame()
        {
            return RemoveSelectedFrame();
        }

        public bool MoveSelectedFrame(int direction)
        {
            return documentService.MoveSelectedFrame(direction);
        }

        public bool MoveFrame(int direction)
        {
            return MoveSelectedFrame(direction);
        }

        public bool MoveFrameTo(int sourceIndex, int destinationIndex)
        {
            return documentService.MoveFrameTo(sourceIndex, destinationIndex);
        }

        public bool ClearSelectedCel()
        {
            return documentService.ClearSelectedCel();
        }

        public bool ResizeCanvas(int width, int height)
        {
            bool didResize = documentService.ResizeCanvas(width, height);
            if (didResize)
            {
                selectionService.Reset();
                ClampSelection();
            }

            return didResize;
        }

        public bool CropCanvasToSelection()
        {
            if (document == null || !selectionService.HasSelection)
            {
                return false;
            }

            RectInt selectionRect = selectionService.Rect;
            int sideLength = Mathf.Max(selectionRect.width, selectionRect.height);
            if (sideLength > document.Width || sideLength > document.Height)
            {
                return false;
            }

            int cropX = Mathf.Clamp(selectionRect.xMin, 0, document.Width - sideLength);
            int cropY = Mathf.Clamp(selectionRect.yMin, 0, document.Height - sideLength);
            bool didCrop = documentService.CropCanvas(new RectInt(cropX, cropY, sideLength, sideLength));
            if (didCrop)
            {
                selectionService.Reset();
                ClampSelection();
            }

            return didCrop;
        }

        public bool FlipCanvas(bool horizontally)
        {
            return documentService.FlipCanvas(horizontally);
        }

        public bool SetSelectedFrameDuration(float duration)
        {
            return documentService.SetSelectedFrameDuration(duration);
        }

        public bool SetFrameDuration(float duration)
        {
            return SetSelectedFrameDuration(duration);
        }

        public bool SetLayerVisibility(int layerIndex, bool isVisible)
        {
            return documentService.SetLayerVisibility(layerIndex, isVisible);
        }

        public bool SetLayerLock(int layerIndex, bool isLocked)
        {
            return documentService.SetLayerLock(layerIndex, isLocked);
        }

        public bool SetAllLayersVisibility(bool isVisible)
        {
            return documentService.SetAllLayersVisibility(isVisible);
        }

        public bool SetAllLayersLock(bool isLocked)
        {
            return documentService.SetAllLayersLock(isLocked);
        }

        public bool SetLayerName(int layerIndex, string name)
        {
            return documentService.SetLayerName(layerIndex, name);
        }

        public bool SetLayerOpacity(int layerIndex, float opacity)
        {
            return documentService.SetLayerOpacity(layerIndex, opacity);
        }

        public bool RecolorSelectedFrame(Color sourceColor, Color targetColor)
        {
            return paletteService.RecolorSelectedFrame(sourceColor, targetColor);
        }

        public IReadOnlyList<Color> GetCanvasColors()
        {
            return paletteService.GetCanvasColors();
        }

        public SpriteCanvasColorMap CreateCanvasColorMap()
        {
            return paletteService.CreateCanvasColorMap();
        }

        public Color32[] GetSelectedLayerPixels()
        {
            return paletteService.GetSelectedLayerPixels();
        }

        public IReadOnlyList<SpriteRecolorMapping> CreateColorReductionMappings(SpriteCanvasColorMap sourceMap, int targetColorCount)
        {
            return paletteService.CreateColorReductionMappings(sourceMap, targetColorCount);
        }

        public bool ApplyRecolorMappings(IReadOnlyList<SpriteRecolorMapping> mappings, SpriteCanvasColorMap sourceMap)
        {
            return paletteService.ApplyRecolorMappings(mappings, sourceMap);
        }

        public bool MapSelectedCelToPalette(IReadOnlyList<Color> palette, bool useDithering)
        {
            return paletteService.MapSelectedCelToPalette(palette, useDithering);
        }

        public bool FillSelectedCel(Vector2Int position, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            return FillSelectedCelResult(position, color, horizontalSymmetry, verticalSymmetry).DidChange;
        }

        public SpriteToolboxOperationResult FillSelectedCelResult(Vector2Int position, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            return pixelService.Fill(position, color, horizontalSymmetry, verticalSymmetry, FillTolerance);
        }

        public SpriteToolboxOperationResult Fill(Vector2Int position, Color color)
        {
            return FillSelectedCelResult(position, color, HorizontalSymmetry, VerticalSymmetry);
        }

        public bool DrawLineOnSelectedCel(Vector2Int start, Vector2Int end, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            return DrawLineOnSelectedCelResult(start, end, color, horizontalSymmetry, verticalSymmetry).DidChange;
        }

        public SpriteToolboxOperationResult DrawLineOnSelectedCelResult(Vector2Int start, Vector2Int end, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            return pixelService.DrawLine(start, end, color, horizontalSymmetry, verticalSymmetry);
        }

        public SpriteToolboxOperationResult DrawLine(Vector2Int start, Vector2Int end, Color color)
        {
            return DrawLineOnSelectedCelResult(start, end, color, HorizontalSymmetry, VerticalSymmetry);
        }

        public Color PickCompositeColor(Vector2Int position)
        {
            return pixelService.PickCompositeColor(position);
        }

        public Color PickColor(Vector2Int position)
        {
            return PickCompositeColor(position);
        }

        public bool CopyPixelSelection()
        {
            return selectionService.Copy(GetSelectedCel());
        }

        public bool CopySelection()
        {
            return CopyPixelSelection();
        }

        public bool CutPixelSelection()
        {
            return selectionService.Cut(GetUnlockedSelectedCel(), ApplyPixelChanges);
        }

        public bool CutSelection()
        {
            return CutPixelSelection();
        }

        public bool PastePixelSelection()
        {
            return selectionService.BeginPaste(document, GetUnlockedSelectedCel());
        }

        public bool PasteSelection()
        {
            return PastePixelSelection();
        }

        public void SetMagicPixelSelection(Vector2Int position)
        {
            SpriteCel cel = GetUnlockedSelectedCel();
            if (cel == null) return;
            selectionService.SetMagicPositions(SpritePixelOperations.CreateFloodFillPositions(cel, position, FillTolerance));
            selectionService.SetActive(selectionService.Positions.Count > 0);
        }

        public bool ModifyMagicPixelSelection(Vector2Int position, bool add)
        {
            SpriteCel cel = GetUnlockedSelectedCel();
            if (cel == null)
            {
                return false;
            }

            IReadOnlyList<Vector2Int> positions = SpritePixelOperations.CreateFloodFillPositions(cel, position, FillTolerance);
            return selectionService.CombineMagicPositions(positions, add);
        }

        public bool ModifyPixelSelectionRect(RectInt rect, bool add)
        {
            RectInt clampedRect = ClampSelectionRectToDocument(rect);
            return clampedRect.width > 0 && clampedRect.height > 0 && selectionService.CombineRect(clampedRect, add);
        }

        public bool FillPixelSelection(Color color)
        {
            return selectionService.Fill(GetUnlockedSelectedCel(), color, ApplyPixelChanges);
        }

        public bool ClearPixelSelectionPixels()
        {
            return FillPixelSelection(Color.clear);
        }

        public bool FlipPixelSelection(bool horizontally)
        {
            return selectionService.Flip(GetUnlockedSelectedCel(), horizontally, ApplyPixelChanges);
        }

        public bool InvertPixelSelection()
        {
            BeginSelectionUndo();
            return document != null && selectionService.Invert(document.Width, document.Height);
        }

        public bool BeginFloatingMoveSelection()
        {
            return selectionService.BeginMove(document, GetUnlockedSelectedCel());
        }

        public bool MoveFloatingSelection(Vector2Int requestedDelta)
        {
            return selectionService.MoveFloatingSelection(document, requestedDelta);
        }

        public bool NudgeFloatingSelection(Vector2Int delta)
        {
            SpriteToolboxFloatingSelection floatingSelection = selectionService.FloatingSelection;
            if (floatingSelection == null)
            {
                return false;
            }

            return selectionService.MoveFloatingSelection(document, floatingSelection.Translation + delta);
        }

        public bool SetFloatingSelectionRotation(float angleDegrees)
        {
            return selectionService.SetFloatingSelectionRotation(angleDegrees);
        }

        public bool SetFloatingSelectionRotationMode(bool value)
        {
            if (value)
            {
                selectionService.SetFloatingSelectionScaleMode(false);
                selectionService.SetFloatingSelectionTransformMode(false);
            }
            return selectionService.SetFloatingSelectionRotationMode(value);
        }

        public bool SetFloatingSelectionScale(Vector2 scale)
        {
            return selectionService.SetFloatingSelectionScale(scale);
        }

        public bool SetFloatingSelectionScaleMode(bool value)
        {
            if (value)
            {
                selectionService.SetFloatingSelectionRotationMode(false);
                selectionService.SetFloatingSelectionTransformMode(false);
            }
            return selectionService.SetFloatingSelectionScaleMode(value);
        }

        public bool SetFloatingSelectionTransform(RectInt rect)
        {
            return selectionService.SetFloatingSelectionTransform(rect);
        }

        public bool SetFloatingSelectionTransformMode(bool value)
        {
            if (value)
            {
                selectionService.SetFloatingSelectionRotationMode(false);
                selectionService.SetFloatingSelectionScaleMode(false);
            }

            return selectionService.SetFloatingSelectionTransformMode(value);
        }

        public bool CommitFloatingSelection()
        {
            return selectionService.CommitFloatingSelection(document, GetUnlockedSelectedCel(), ApplyPixelChanges);
        }

        public bool CancelFloatingSelection()
        {
            return selectionService.CancelFloatingSelection();
        }

        public bool SetPalette(IReadOnlyList<Color> colors)
        {
            return paletteService.SetPalette(colors);
        }

        public bool AddPaletteColor(Color color)
        {
            return paletteService.AddPaletteColor(color);
        }

        public bool SortPaletteByColorFamilies()
        {
            return paletteService.SortPaletteByColorFamilies();
        }

        public bool BuildPaletteFromCanvas()
        {
            return paletteService.BuildPaletteFromSelectedFrame();
        }

        public bool AddAnimationTag(string name, int fromFrame, int toFrame)
        {
            int tagIndex = document == null ? 0 : document.AnimationTags.Count;
            return AddAnimationTag(name, fromFrame, toFrame, SpriteAnimationDirection.Forward, SpriteToolboxTagColorPalette.GetColor(tagIndex));
        }

        public bool AddAnimationTag(string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color)
        {
            bool wasAdded = paletteService.AddAnimationTag(name, fromFrame, toFrame, direction, color);
            if (wasAdded)
            {
                SelectAnimationTag(document.AnimationTags.Count - 1);
            }

            return wasAdded;
        }

        public bool RemoveAnimationTag(int tagIndex)
        {
            return paletteService.RemoveAnimationTag(tagIndex);
        }

        public bool UpdateAnimationTag(int tagIndex, string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color)
        {
            return paletteService.UpdateAnimationTag(tagIndex, name, fromFrame, toFrame, direction, color);
        }

        private SpriteCel GetSelectedCel()
        {
            return document == null ? null : document.GetFrame(selectedFrameIndex).GetCel(selectedLayerIndex);
        }

        private SpriteCel GetUnlockedSelectedCel()
        {
            return document == null || document.GetLayerTrack(selectedLayerIndex).IsLocked ? null : GetSelectedCel();
        }

        private SpriteToolboxStrokeSettings CreateStrokeSettings(Color color)
        {
            return new SpriteToolboxStrokeSettings(ActiveTool, BrushSize, BrushShape, HorizontalSymmetry, VerticalSymmetry, TiledMode, PixelPerfectPencil, color);
        }

        public void Undo()
        {
            if (HasFloatingSelection)
            {
                CancelFloatingSelection();
                return;
            }

            if (!CommandHistory.CanUndo)
            {
                return;
            }

            CommandHistory.Undo();
            ClearSelectionHistory();
            NotifyDocumentChanged();
            DocumentEdited?.Invoke();
        }

        public void Redo()
        {
            if (HasFloatingSelection)
            {
                CancelFloatingSelection();
                return;
            }

            if (!CommandHistory.CanRedo)
            {
                return;
            }

            CommandHistory.Redo();
            ClearSelectionHistory();
            NotifyDocumentChanged();
            DocumentEdited?.Invoke();
        }

        public void NotifyDocumentChanged()
        {
            NotifyDocumentChanged(SpriteToolboxDocumentChange.Structure());
        }

        private void NotifyDocumentChanged(SpriteToolboxDocumentChange change)
        {
            ClampSelection();
            if (change.IsStructural)
            {
                InvalidateStateViews();
            }

            DocumentChangedDetailed?.Invoke(change);
            DocumentChanged?.Invoke();
        }

        private void ExecutePixelChange(ISpriteCommand command, SpriteToolboxOperationResult result)
        {
            CommandHistory.Execute(command);
            ClearSelectionHistory();
            NotifyDocumentChanged(SpriteToolboxDocumentChange.Pixels(selectedFrameIndex, result.InvalidatedBounds));
            DocumentEdited?.Invoke();
        }

        private SelectionSnapshot CaptureSelection()
        {
            IReadOnlyList<Vector2Int> positions = selectionService.IsMagicSelection ? selectionService.Positions : new Vector2Int[0];
            return new SelectionSnapshot(selectionService.Rect, selectionService.HasSelection, selectionService.IsMagicSelection, positions);
        }

        private void RestoreSelection(SelectionSnapshot snapshot)
        {
            if (snapshot.IsMagicSelection) selectionService.SetMagicPositions(snapshot.Positions);
            else selectionService.SetRect(snapshot.Rect);
            selectionService.SetActive(snapshot.IsActive);
        }

        private void ClearSelectionHistory()
        {
            selectionUndoHistory.Clear();
            selectionRedoHistory.Clear();
        }

        private RectInt ClampSelectionRectToDocument(RectInt rect)
        {
            if (document == null)
            {
                return new RectInt();
            }

            int xMin = Mathf.Clamp(rect.xMin, 0, document.Width);
            int yMin = Mathf.Clamp(rect.yMin, 0, document.Height);
            int xMax = Mathf.Clamp(rect.xMax, 0, document.Width);
            int yMax = Mathf.Clamp(rect.yMax, 0, document.Height);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private sealed class SelectionSnapshot
        {
            public RectInt Rect { get; }
            public bool IsActive { get; }
            public bool IsMagicSelection { get; }
            public IReadOnlyList<Vector2Int> Positions { get; }

            public SelectionSnapshot(RectInt rect, bool isActive, bool isMagicSelection, IReadOnlyList<Vector2Int> positions)
            {
                Rect = rect;
                IsActive = isActive;
                IsMagicSelection = isMagicSelection;
                Positions = new List<Vector2Int>(positions);
            }
        }

        private IReadOnlyList<SpriteToolboxLayerState> GetLayerStates()
        {
            if (document == null) return EmptyLayers;
            if (cachedLayers == null) cachedLayers = documentService.CreateLayerStates();
            return cachedLayers;
        }

        private IReadOnlyList<SpriteToolboxFrameState> GetFrameStates()
        {
            if (document == null) return EmptyFrames;
            if (cachedFrames == null) cachedFrames = documentService.CreateFrameStates();
            return cachedFrames;
        }

        private IReadOnlyList<SpriteToolboxAnimationTagState> GetAnimationTagStates()
        {
            if (document == null) return EmptyAnimationTags;
            if (cachedAnimationTags == null) cachedAnimationTags = paletteService.CreateAnimationTagStates();
            return cachedAnimationTags;
        }

        private void InvalidateStateViews()
        {
            cachedLayers = null;
            cachedFrames = null;
            cachedAnimationTags = null;
        }

        private void SetToolSetting(ref bool setting, bool value)
        {
            if (setting == value)
            {
                return;
            }

            setting = value;
            ToolSettingsChanged?.Invoke();
        }
    }

    public sealed class SpriteSelectionClipboard
    {
        private readonly Color32[] pixels;

        public int Width { get; }
        public int Height { get; }

        public SpriteSelectionClipboard(int width, int height, Color32[] pixels)
        {
            Width = Mathf.Max(1, width);
            Height = Mathf.Max(1, height);
            this.pixels = pixels == null ? throw new ArgumentNullException(nameof(pixels)) : (Color32[])pixels.Clone();
            if (this.pixels.Length != Width * Height)
            {
                throw new ArgumentException("The clipboard pixel count must match its dimensions.", nameof(pixels));
            }
        }

        public Color32 GetPixel(int x, int y)
        {
            return pixels[y * Width + x];
        }

        public Color32[] CopyPixels()
        {
            return (Color32[])pixels.Clone();
        }
    }
}
