using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class InsertLayerCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int layerIndex;
        private readonly string layerName;
        private SpriteLayerTrackSnapshot snapshot;

        public InsertLayerCommand(SpriteDocument document, int layerIndex, string layerName)
        {
            this.document = document;
            this.layerIndex = layerIndex;
            this.layerName = layerName;
        }

        public long EstimatedMemoryBytes => snapshot == null ? 0L : snapshot.EstimateMemoryBytes();

        public void Execute()
        {
            if (snapshot == null)
            {
                snapshot = document.CreateLayerTrackSnapshotForCommand(layerName);
            }

            document.InsertLayerTrackForCommand(layerIndex, snapshot);
        }

        public void Undo()
        {
            document.RemoveLayerTrackForCommand(layerIndex);
        }
    }

}
