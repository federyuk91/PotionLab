using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    /// <summary>Executes document, timeline, palette and tag use cases without owning editor state.</summary>
    internal sealed class SpriteToolboxDocumentApplicationService
    {
        private readonly Func<SpriteDocument> getDocument;
        private readonly Func<int> getSelectedFrameIndex;
        private readonly Func<int> getSelectedLayerIndex;
        private readonly Action<int> selectFrame;
        private readonly Action<int> selectLayer;
        private readonly Action<int, int> selectCell;
        private readonly Action<ISpriteCommand> execute;

        public SpriteToolboxDocumentApplicationService(
            Func<SpriteDocument> getDocument,
            Func<int> getSelectedFrameIndex,
            Func<int> getSelectedLayerIndex,
            Action<int> selectFrame,
            Action<int> selectLayer,
            Action<int, int> selectCell,
            Action<ISpriteCommand> execute)
        {
            this.getDocument = getDocument ?? throw new ArgumentNullException(nameof(getDocument));
            this.getSelectedFrameIndex = getSelectedFrameIndex ?? throw new ArgumentNullException(nameof(getSelectedFrameIndex));
            this.getSelectedLayerIndex = getSelectedLayerIndex ?? throw new ArgumentNullException(nameof(getSelectedLayerIndex));
            this.selectFrame = selectFrame ?? throw new ArgumentNullException(nameof(selectFrame));
            this.selectLayer = selectLayer ?? throw new ArgumentNullException(nameof(selectLayer));
            this.selectCell = selectCell ?? throw new ArgumentNullException(nameof(selectCell));
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public IReadOnlyList<SpriteToolboxLayerState> CreateLayerStates()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new SpriteToolboxLayerState[0];
            }

            List<SpriteToolboxLayerState> states = new List<SpriteToolboxLayerState>(document.LayerTracks.Count);
            foreach (SpriteLayerTrack layer in document.LayerTracks)
            {
                states.Add(new SpriteToolboxLayerState(layer.Name, layer.IsVisible, layer.IsLocked, layer.Opacity));
            }

            return states;
        }

        public IReadOnlyList<SpriteToolboxFrameState> CreateFrameStates()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new SpriteToolboxFrameState[0];
            }

            List<SpriteToolboxFrameState> states = new List<SpriteToolboxFrameState>(document.Frames.Count);
            foreach (SpriteFrame frame in document.Frames)
            {
                states.Add(new SpriteToolboxFrameState(frame.Duration));
            }

            return states;
        }

        public bool MoveSelectedLayer(int direction)
        {
            int selectedLayerIndex = getSelectedLayerIndex();
            SpriteDocument document = getDocument();
            int destinationIndex = document == null ? selectedLayerIndex : Mathf.Clamp(selectedLayerIndex + direction, 0, document.LayerTracks.Count - 1);
            return MoveLayerTo(selectedLayerIndex, destinationIndex);
        }

        public bool MoveLayerTo(int sourceIndex, int destinationIndex)
        {
            SpriteDocument document = getDocument();
            if (document == null || sourceIndex < 0 || destinationIndex < 0 || sourceIndex >= document.LayerTracks.Count || destinationIndex >= document.LayerTracks.Count || sourceIndex == destinationIndex)
            {
                return false;
            }

            execute(new MoveLayerCommand(document, sourceIndex, destinationIndex));
            selectLayer(destinationIndex);
            return true;
        }

        public bool AddLayer(string name)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            execute(new InsertLayerCommand(document, document.LayerTracks.Count, name));
            selectLayer(document.LayerTracks.Count - 1);
            return true;
        }

        public bool DuplicateSelectedLayer()
        {
            SpriteDocument document = getDocument();
            int selectedLayerIndex = getSelectedLayerIndex();
            if (document == null || selectedLayerIndex < 0 || selectedLayerIndex >= document.LayerTracks.Count)
            {
                return false;
            }

            string duplicateName = GetDuplicateLayerName(document, document.GetLayerTrack(selectedLayerIndex).Name);
            execute(new DuplicateLayerCommand(document, selectedLayerIndex, duplicateName));
            selectLayer(selectedLayerIndex + 1);
            return true;
        }

        private static string GetDuplicateLayerName(SpriteDocument document, string sourceName)
        {
            string baseName = string.IsNullOrWhiteSpace(sourceName) ? "Layer" : sourceName;
            string duplicateName = $"{baseName} Copy";
            int duplicateNumber = 2;
            while (HasLayerNamed(document, duplicateName))
            {
                duplicateName = $"{baseName} Copy {duplicateNumber}";
                duplicateNumber++;
            }

            return duplicateName;
        }

        private static bool HasLayerNamed(SpriteDocument document, string name)
        {
            foreach (SpriteLayerTrack layerTrack in document.LayerTracks)
            {
                if (string.Equals(layerTrack.Name, name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        public bool AddLayerWithPixels(string name, int frameIndex, IReadOnlyList<Color32> pixels)
        {
            SpriteDocument document = getDocument();
            if (document == null
                || frameIndex < 0
                || frameIndex >= document.Frames.Count
                || pixels == null
                || pixels.Count != document.Width * document.Height)
            {
                return false;
            }

            execute(new InsertLayerWithPixelsCommand(document, document.LayerTracks.Count, frameIndex, name, pixels));
            selectCell(frameIndex, document.LayerTracks.Count - 1);
            return true;
        }

        public bool RemoveSelectedLayer()
        {
            SpriteDocument document = getDocument();
            int selectedLayerIndex = getSelectedLayerIndex();
            if (document == null || document.LayerTracks.Count <= 1 || document.GetLayerTrack(selectedLayerIndex).IsLocked)
            {
                return false;
            }

            execute(new RemoveLayerCommand(document, selectedLayerIndex));
            selectLayer(Mathf.Clamp(selectedLayerIndex - 1, 0, document.LayerTracks.Count - 1));
            return true;
        }

        public bool RemoveLayer(int layerIndex)
        {
            SpriteDocument document = getDocument();
            if (document == null || layerIndex < 0 || layerIndex >= document.LayerTracks.Count)
            {
                return false;
            }

            selectLayer(layerIndex);
            return RemoveSelectedLayer();
        }

        public bool MergeSelectedLayerDown()
        {
            SpriteDocument document = getDocument();
            int selectedLayerIndex = getSelectedLayerIndex();
            if (document == null || selectedLayerIndex <= 0)
            {
                return false;
            }

            execute(new MergeLayerDownCommand(document, selectedLayerIndex));
            selectLayer(selectedLayerIndex - 1);
            return true;
        }

        public bool AddFrameAfterSelection(bool duplicateCurrentFrame)
        {
            SpriteDocument document = getDocument();
            int selectedFrameIndex = getSelectedFrameIndex();
            if (document == null)
            {
                return false;
            }

            int insertionIndex = selectedFrameIndex + 1;
            float duration = document.GetFrame(selectedFrameIndex).Duration;
            execute(new InsertFrameCommand(document, insertionIndex, duplicateCurrentFrame, duration));
            selectCell(insertionIndex, 0);
            return true;
        }

        public bool RemoveSelectedFrame()
        {
            SpriteDocument document = getDocument();
            int selectedFrameIndex = getSelectedFrameIndex();
            if (document == null || document.Frames.Count <= 1)
            {
                return false;
            }

            execute(new RemoveFrameCommand(document, selectedFrameIndex));
            selectCell(Mathf.Clamp(selectedFrameIndex - 1, 0, document.Frames.Count - 1), 0);
            return true;
        }

        public bool MoveSelectedFrame(int direction)
        {
            int selectedFrameIndex = getSelectedFrameIndex();
            SpriteDocument document = getDocument();
            int destinationIndex = document == null ? selectedFrameIndex : Mathf.Clamp(selectedFrameIndex + direction, 0, document.Frames.Count - 1);
            return MoveFrameTo(selectedFrameIndex, destinationIndex);
        }

        public bool MoveFrameTo(int sourceIndex, int destinationIndex)
        {
            SpriteDocument document = getDocument();
            if (document == null || sourceIndex < 0 || destinationIndex < 0 || sourceIndex >= document.Frames.Count || destinationIndex >= document.Frames.Count || sourceIndex == destinationIndex)
            {
                return false;
            }

            execute(new MoveFrameCommand(document, sourceIndex, destinationIndex));
            selectFrame(destinationIndex);
            return true;
        }

        public bool ClearSelectedCel()
        {
            SpriteDocument document = getDocument();
            int selectedLayerIndex = getSelectedLayerIndex();
            if (document == null || document.GetLayerTrack(selectedLayerIndex).IsLocked)
            {
                return false;
            }

            execute(new ClearCelCommand(document, getSelectedFrameIndex(), selectedLayerIndex));
            return true;
        }

        public bool ResizeCanvas(int width, int height)
        {
            SpriteDocument document = getDocument();
            if (document == null || width < 1 || height < 1 || (document.Width == width && document.Height == height))
            {
                return false;
            }

            SpriteDocument before = document.Clone();
            SpriteDocument after = document.Clone();
            after.ResizeCanvas(width, height);
            execute(new SpriteDocumentSnapshotCommand(document, before, after));
            return true;
        }

        public bool CropCanvas(RectInt cropRect)
        {
            SpriteDocument document = getDocument();
            if (document == null || cropRect.width < 1 || cropRect.height < 1)
            {
                return false;
            }

            RectInt boundedCropRect = new RectInt(
                Mathf.Clamp(cropRect.xMin, 0, document.Width),
                Mathf.Clamp(cropRect.yMin, 0, document.Height),
                Mathf.Clamp(cropRect.xMax, 0, document.Width) - Mathf.Clamp(cropRect.xMin, 0, document.Width),
                Mathf.Clamp(cropRect.yMax, 0, document.Height) - Mathf.Clamp(cropRect.yMin, 0, document.Height));
            if (boundedCropRect.width < 1 || boundedCropRect.height < 1 || (boundedCropRect.width == document.Width && boundedCropRect.height == document.Height))
            {
                return false;
            }

            SpriteDocument before = document.Clone();
            SpriteDocument after = document.Clone();
            after.CropCanvas(boundedCropRect);
            execute(new SpriteDocumentSnapshotCommand(document, before, after));
            return true;
        }

        public bool FlipCanvas(bool horizontally)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            SpriteDocument before = document.Clone();
            SpriteDocument after = document.Clone();
            after.FlipCanvas(horizontally);
            execute(new SpriteDocumentSnapshotCommand(document, before, after));
            return true;
        }

        public bool SetSelectedFrameDuration(float duration)
        {
            SpriteDocument document = getDocument();
            int selectedFrameIndex = getSelectedFrameIndex();
            if (document == null)
            {
                return false;
            }

            SpriteFrame frame = document.GetFrame(selectedFrameIndex);
            if (Mathf.Approximately(frame.Duration, duration))
            {
                return false;
            }

            execute(new ValueChangeCommand<float>(value => document.GetFrame(selectedFrameIndex).Duration = value, frame.Duration, duration));
            return true;
        }

        public bool SetLayerVisibility(int layerIndex, bool isVisible)
        {
            SpriteDocument document = getDocument();
            if (!IsValidLayerIndex(document, layerIndex))
            {
                return false;
            }

            SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
            if (layer.IsVisible == isVisible)
            {
                return false;
            }

            execute(new ValueChangeCommand<bool>(value => document.GetLayerTrack(layerIndex).IsVisible = value, layer.IsVisible, isVisible));
            return true;
        }

        public bool SetLayerLock(int layerIndex, bool isLocked)
        {
            SpriteDocument document = getDocument();
            if (!IsValidLayerIndex(document, layerIndex))
            {
                return false;
            }

            SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
            if (layer.IsLocked == isLocked)
            {
                return false;
            }

            execute(new ValueChangeCommand<bool>(value => document.GetLayerTrack(layerIndex).IsLocked = value, layer.IsLocked, isLocked));
            return true;
        }

        public bool SetAllLayersVisibility(bool isVisible)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            List<ISpriteCommand> commands = new List<ISpriteCommand>();
            for (int layerIndex = 0; layerIndex < document.LayerTracks.Count; layerIndex++)
            {
                SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
                if (layer.IsVisible != isVisible)
                {
                    int changedLayerIndex = layerIndex;
                    commands.Add(new ValueChangeCommand<bool>(value => document.GetLayerTrack(changedLayerIndex).IsVisible = value, layer.IsVisible, isVisible));
                }
            }

            return ExecuteGroup(commands);
        }

        public bool SetAllLayersLock(bool isLocked)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            List<ISpriteCommand> commands = new List<ISpriteCommand>();
            for (int layerIndex = 0; layerIndex < document.LayerTracks.Count; layerIndex++)
            {
                SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
                if (layer.IsLocked != isLocked)
                {
                    int changedLayerIndex = layerIndex;
                    commands.Add(new ValueChangeCommand<bool>(value => document.GetLayerTrack(changedLayerIndex).IsLocked = value, layer.IsLocked, isLocked));
                }
            }

            return ExecuteGroup(commands);
        }

        public bool SetLayerName(int layerIndex, string name)
        {
            SpriteDocument document = getDocument();
            if (!IsValidLayerIndex(document, layerIndex))
            {
                return false;
            }

            SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
            string safeName = name ?? string.Empty;
            if (layer.Name == safeName)
            {
                return false;
            }

            execute(new ValueChangeCommand<string>(value => document.GetLayerTrack(layerIndex).Name = value, layer.Name, safeName));
            return true;
        }

        public bool SetLayerOpacity(int layerIndex, float opacity)
        {
            SpriteDocument document = getDocument();
            if (!IsValidLayerIndex(document, layerIndex))
            {
                return false;
            }

            SpriteLayerTrack layer = document.GetLayerTrack(layerIndex);
            float clampedOpacity = Mathf.Clamp01(opacity);
            if (Mathf.Approximately(layer.Opacity, clampedOpacity))
            {
                return false;
            }

            execute(new ValueChangeCommand<float>(value => document.GetLayerTrack(layerIndex).Opacity = value, layer.Opacity, clampedOpacity));
            return true;
        }

        private bool ExecuteGroup(IReadOnlyList<ISpriteCommand> commands)
        {
            if (commands == null || commands.Count == 0)
            {
                return false;
            }

            execute(new SpriteCommandGroup(commands));
            return true;
        }

        private static bool IsValidLayerIndex(SpriteDocument document, int layerIndex)
        {
            return document != null && layerIndex >= 0 && layerIndex < document.LayerTracks.Count;
        }

    }
}
