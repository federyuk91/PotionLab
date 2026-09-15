using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class InsertLayerWithPixelsCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int layerIndex;
        private readonly int frameIndex;
        private readonly string layerName;
        private readonly Color32[] pixels;
        private SpriteLayerTrackSnapshot snapshot;

        public InsertLayerWithPixelsCommand(SpriteDocument document, int layerIndex, int frameIndex, string layerName, IReadOnlyList<Color32> pixels)
        {
            this.document = document ?? throw new ArgumentNullException(nameof(document));
            this.layerIndex = layerIndex;
            this.frameIndex = frameIndex;
            this.layerName = layerName;
            this.pixels = new Color32[pixels.Count];
            for (int pixelIndex = 0; pixelIndex < pixels.Count; pixelIndex++)
            {
                this.pixels[pixelIndex] = pixels[pixelIndex];
            }
        }

        public long EstimatedMemoryBytes => (snapshot == null ? 0L : snapshot.EstimateMemoryBytes()) + pixels.LongLength * 4L;

        public void Execute()
        {
            if (snapshot == null)
            {
                snapshot = document.CreateLayerTrackSnapshotForCommand(layerName);
                pixels.CopyTo(snapshot.Cels[frameIndex].Pixels, 0);
            }

            document.InsertLayerTrackForCommand(layerIndex, snapshot);
        }

        public void Undo()
        {
            document.RemoveLayerTrackForCommand(layerIndex);
        }
    }

}
