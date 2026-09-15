using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    /// <summary>Builds and executes atomic pixel-editing use cases.</summary>
    internal sealed class SpriteToolboxPixelApplicationService
    {
        private readonly Func<SpriteDocument> getDocument;
        private readonly Func<int> getSelectedFrameIndex;
        private readonly Func<SpriteCel> getUnlockedSelectedCel;
        private readonly Action<ISpriteCommand, SpriteToolboxOperationResult> execute;

        public SpriteToolboxPixelApplicationService(
            Func<SpriteDocument> getDocument,
            Func<int> getSelectedFrameIndex,
            Func<SpriteCel> getUnlockedSelectedCel,
            Action<ISpriteCommand, SpriteToolboxOperationResult> execute)
        {
            this.getDocument = getDocument ?? throw new ArgumentNullException(nameof(getDocument));
            this.getSelectedFrameIndex = getSelectedFrameIndex ?? throw new ArgumentNullException(nameof(getSelectedFrameIndex));
            this.getUnlockedSelectedCel = getUnlockedSelectedCel ?? throw new ArgumentNullException(nameof(getUnlockedSelectedCel));
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public SpriteToolboxOperationResult Apply(SpriteCel cel, IReadOnlyList<PixelChange> changes)
        {
            if (cel == null || changes == null || changes.Count == 0)
            {
                return SpriteToolboxOperationResult.NoChange("No pixel changes to apply.");
            }

            SpriteToolboxOperationResult result = SpriteToolboxOperationResult.FromPixelChanges(changes);
            execute(new PixelChangesCommand(cel, changes), result);
            return result;
        }

        public SpriteToolboxOperationResult Paint(Vector2Int position, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            SpriteCel cel = getUnlockedSelectedCel();
            if (cel == null)
            {
                return SpriteToolboxOperationResult.NoChange("The selected layer is locked or unavailable.");
            }

            List<PixelChange> changes = SpritePixelOperations.CreatePixelChange(cel, position, color);
            return Apply(cel, CreateSymmetricChanges(cel, changes, horizontalSymmetry, verticalSymmetry));
        }

        public SpriteToolboxOperationResult Fill(Vector2Int position, Color color, bool horizontalSymmetry, bool verticalSymmetry, int tolerance)
        {
            SpriteCel cel = getUnlockedSelectedCel();
            if (cel == null)
            {
                return SpriteToolboxOperationResult.NoChange("The selected layer is locked or unavailable.");
            }

            List<PixelChange> changes = SpritePixelOperations.CreateFloodFillChanges(cel, position, color, tolerance);
            return Apply(cel, CreateSymmetricChanges(cel, changes, horizontalSymmetry, verticalSymmetry));
        }

        public SpriteToolboxOperationResult DrawLine(Vector2Int start, Vector2Int end, Color color, bool horizontalSymmetry, bool verticalSymmetry)
        {
            SpriteCel cel = getUnlockedSelectedCel();
            if (cel == null)
            {
                return SpriteToolboxOperationResult.NoChange("The selected layer is locked or unavailable.");
            }

            List<PixelChange> changes = SpritePixelOperations.CreateLineChanges(cel, start, end, color);
            return Apply(cel, CreateSymmetricChanges(cel, changes, horizontalSymmetry, verticalSymmetry));
        }

        public Color PickCompositeColor(Vector2Int position)
        {
            SpriteDocument document = getDocument();
            return document == null
                ? Color.clear
                : SpriteDocumentRenderer.GetCompositePixel(document, getSelectedFrameIndex(), position);
        }

        private List<PixelChange> CreateSymmetricChanges(SpriteCel cel, IReadOnlyList<PixelChange> sourceChanges, bool horizontalSymmetry, bool verticalSymmetry)
        {
            if (!horizontalSymmetry && !verticalSymmetry)
            {
                return new List<PixelChange>(sourceChanges);
            }

            SpriteDocument document = getDocument();
            Dictionary<Vector2Int, PixelChange> changesByPosition = new Dictionary<Vector2Int, PixelChange>();
            foreach (PixelChange change in sourceChanges)
            {
                AddOrUpdateChange(changesByPosition, cel, change.Position, change.NewColor);
                if (horizontalSymmetry)
                {
                    AddOrUpdateChange(changesByPosition, cel, new Vector2Int(document.Width - 1 - change.Position.x, change.Position.y), change.NewColor);
                }

                if (verticalSymmetry)
                {
                    AddOrUpdateChange(changesByPosition, cel, new Vector2Int(change.Position.x, document.Height - 1 - change.Position.y), change.NewColor);
                }

                if (horizontalSymmetry && verticalSymmetry)
                {
                    AddOrUpdateChange(changesByPosition, cel, new Vector2Int(document.Width - 1 - change.Position.x, document.Height - 1 - change.Position.y), change.NewColor);
                }
            }

            return new List<PixelChange>(changesByPosition.Values);
        }

        private static void AddOrUpdateChange(Dictionary<Vector2Int, PixelChange> changesByPosition, SpriteCel cel, Vector2Int position, Color newColor)
        {
            if (!cel.Contains(position))
            {
                return;
            }

            if (changesByPosition.TryGetValue(position, out PixelChange existingChange))
            {
                changesByPosition[position] = new PixelChange(position, existingChange.PreviousColor, newColor);
            }
            else if (!SpriteColorMath.AreEqual(cel.GetPixel(position), newColor))
            {
                changesByPosition.Add(position, new PixelChange(position, cel.GetPixel(position), newColor));
            }
        }
    }
}
