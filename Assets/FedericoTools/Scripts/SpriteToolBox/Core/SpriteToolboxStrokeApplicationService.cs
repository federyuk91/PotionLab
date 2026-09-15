using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    internal readonly struct SpriteToolboxStrokeSettings
    {
        public SpriteToolboxTool Tool { get; }
        public int BrushSize { get; }
        public SpriteBrushShape BrushShape { get; }
        public bool HorizontalSymmetry { get; }
        public bool VerticalSymmetry { get; }
        public bool TiledMode { get; }
        public bool PixelPerfect { get; }
        public Color Color { get; }

        public SpriteToolboxStrokeSettings(SpriteToolboxTool tool, int brushSize, SpriteBrushShape brushShape, bool horizontalSymmetry, bool verticalSymmetry, bool tiledMode, bool pixelPerfect, Color color)
        {
            Tool = tool;
            BrushSize = brushSize;
            BrushShape = brushShape;
            HorizontalSymmetry = horizontalSymmetry;
            VerticalSymmetry = verticalSymmetry;
            TiledMode = tiledMode;
            PixelPerfect = pixelPerfect;
            Color = color;
        }
    }

    internal sealed class SpriteToolboxStrokeApplicationService
    {
        private readonly BrushStrokeService stroke = new BrushStrokeService();
        private SpriteDocument document;
        private SpriteCel cel;
        private Vector2Int previousPosition;
        private Vector2Int penultimatePosition;
        private bool hasPreviousPosition;
        private bool hasPenultimatePosition;
        private bool isActive;

        public SpriteToolboxOperationResult Begin(SpriteDocument document, SpriteCel cel, SpriteToolboxStrokeSettings settings, Vector2Int position)
        {
            Cancel();
            if (document == null || cel == null || (settings.Tool != SpriteToolboxTool.Pencil && settings.Tool != SpriteToolboxTool.Eraser))
            {
                return SpriteToolboxOperationResult.NoChange("The selected layer is locked, unavailable, or the active tool cannot draw a stroke.");
            }
            this.document = document;
            this.cel = cel;
            stroke.Begin();
            isActive = true;
            return Continue(settings, position);
        }

        public SpriteToolboxOperationResult Continue(SpriteToolboxStrokeSettings settings, Vector2Int position)
        {
            if (!isActive || cel == null) return SpriteToolboxOperationResult.NoChange("No brush stroke is active.");
            Dictionary<Vector2Int, PixelChange> stepChanges = new Dictionary<Vector2Int, PixelChange>();
            if (ShouldRemovePixelPerfectCorner(settings, position)) RestorePixelPerfectCorner(stepChanges);
            int minimumOffset = -(settings.BrushSize - 1) / 2;
            int maximumOffset = minimumOffset + settings.BrushSize - 1;
            for (int y = minimumOffset; y <= maximumOffset; y++)
            {
                for (int x = minimumOffset; x <= maximumOffset; x++)
                {
                    if (SpriteBrushShapeUtility.ContainsOffset(x, y, settings.BrushSize, settings.BrushShape))
                    {
                        AddPixel(stepChanges, settings, position + new Vector2Int(x, y));
                    }
                }
            }
            penultimatePosition = previousPosition;
            hasPenultimatePosition = hasPreviousPosition;
            previousPosition = position;
            hasPreviousPosition = true;
            return SpriteToolboxOperationResult.FromPixelChanges(new List<PixelChange>(stepChanges.Values));
        }

        public SpriteToolboxOperationResult Commit(Action<ISpriteCommand, SpriteToolboxOperationResult> execute)
        {
            if (!isActive || cel == null) return SpriteToolboxOperationResult.NoChange("No brush stroke is active.");
            SpriteCel committedCel = cel;
            SpriteToolboxOperationResult result = stroke.Commit();
            Reset();
            if (result.DidChange) execute(new PixelChangesCommand(committedCel, result.PixelChanges), result);
            return result;
        }

        public SpriteToolboxOperationResult Cancel()
        {
            if (!isActive || cel == null)
            {
                stroke.Cancel();
                Reset();
                return SpriteToolboxOperationResult.NoChange("No brush stroke is active.");
            }
            SpriteToolboxOperationResult result = stroke.Commit();
            List<PixelChange> restoredChanges = new List<PixelChange>();
            foreach (PixelChange change in result.PixelChanges)
            {
                Color currentColor = cel.GetPixel(change.Position);
                cel.SetPixel(change.Position, change.PreviousColor);
                restoredChanges.Add(new PixelChange(change.Position, currentColor, change.PreviousColor));
            }
            Reset();
            return SpriteToolboxOperationResult.FromPixelChanges(restoredChanges);
        }

        private void AddPixel(Dictionary<Vector2Int, PixelChange> stepChanges, SpriteToolboxStrokeSettings settings, Vector2Int position)
        {
            Color color = settings.Tool == SpriteToolboxTool.Eraser ? Color.clear : settings.Color;
            ApplyColor(stepChanges, settings, position, color);
            if (settings.HorizontalSymmetry) ApplyColor(stepChanges, settings, new Vector2Int(document.Width - 1 - position.x, position.y), color);
            if (settings.VerticalSymmetry) ApplyColor(stepChanges, settings, new Vector2Int(position.x, document.Height - 1 - position.y), color);
            if (settings.HorizontalSymmetry && settings.VerticalSymmetry) ApplyColor(stepChanges, settings, new Vector2Int(document.Width - 1 - position.x, document.Height - 1 - position.y), color);
        }

        private void ApplyColor(Dictionary<Vector2Int, PixelChange> stepChanges, SpriteToolboxStrokeSettings settings, Vector2Int position, Color color)
        {
            if (settings.TiledMode) position = new Vector2Int(Modulo(position.x, document.Width), Modulo(position.y, document.Height));
            if (!cel.Contains(position) || SpriteColorMath.AreEqual(cel.GetPixel(position), color)) return;
            Color previousColor = cel.GetPixel(position);
            stroke.Record(cel, position, color);
            cel.SetPixel(position, color);
            AddStepChange(stepChanges, position, previousColor, color);
        }

        private bool ShouldRemovePixelPerfectCorner(SpriteToolboxStrokeSettings settings, Vector2Int currentPosition)
        {
            if (!settings.PixelPerfect || settings.Tool != SpriteToolboxTool.Pencil || settings.BrushSize != 1 || settings.TiledMode || settings.HorizontalSymmetry || settings.VerticalSymmetry || !hasPreviousPosition || !hasPenultimatePosition) return false;
            bool horizontalThenVertical = penultimatePosition.y == previousPosition.y && previousPosition.x == currentPosition.x;
            bool verticalThenHorizontal = penultimatePosition.x == previousPosition.x && previousPosition.y == currentPosition.y;
            return (horizontalThenVertical || verticalThenHorizontal) && penultimatePosition != currentPosition;
        }

        private void RestorePixelPerfectCorner(Dictionary<Vector2Int, PixelChange> stepChanges)
        {
            PixelChange previousChange;
            if (!stroke.TryGetChange(previousPosition, out previousChange)) return;
            Color currentColor = cel.GetPixel(previousPosition);
            cel.SetPixel(previousPosition, previousChange.PreviousColor);
            stroke.Remove(previousPosition);
            AddStepChange(stepChanges, previousPosition, currentColor, previousChange.PreviousColor);
        }

        private void Reset()
        {
            document = null;
            cel = null;
            isActive = false;
            hasPreviousPosition = false;
            hasPenultimatePosition = false;
        }

        private static void AddStepChange(Dictionary<Vector2Int, PixelChange> changes, Vector2Int position, Color previousColor, Color newColor)
        {
            PixelChange existingChange;
            if (changes.TryGetValue(position, out existingChange))
            {
                if (existingChange.PreviousColor == newColor) changes.Remove(position);
                else changes[position] = new PixelChange(position, existingChange.PreviousColor, newColor);
                return;
            }
            if (previousColor != newColor) changes.Add(position, new PixelChange(position, previousColor, newColor));
        }

        private static int Modulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }
    }
}
