using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class RemoveLayerCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int layerIndex;
        private SpriteLayerTrackSnapshot snapshot;

        public RemoveLayerCommand(SpriteDocument document, int layerIndex)
        {
            this.document = document;
            this.layerIndex = layerIndex;
        }

        public long EstimatedMemoryBytes => snapshot == null ? 0L : snapshot.EstimateMemoryBytes();

        public void Execute()
        {
            snapshot = document.RemoveLayerTrackForCommand(layerIndex);
        }

        public void Undo()
        {
            document.InsertLayerTrackForCommand(layerIndex, snapshot);
        }
    }

}
