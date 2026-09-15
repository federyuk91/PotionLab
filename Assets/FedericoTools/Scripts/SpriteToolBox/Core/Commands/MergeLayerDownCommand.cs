using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class MergeLayerDownCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int layerIndex;
        private SpriteLayerTrackSnapshot removedLayer;
        private List<SpriteCel> lowerLayerBeforeMerge;

        public MergeLayerDownCommand(SpriteDocument document, int layerIndex)
        {
            this.document = document;
            this.layerIndex = layerIndex;
        }

        public long EstimatedMemoryBytes
        {
            get
            {
                if (removedLayer == null || lowerLayerBeforeMerge == null)
                {
                    return 0L;
                }

                long lowerLayerBytes = 0L;
                foreach (SpriteCel cel in lowerLayerBeforeMerge)
                {
                    lowerLayerBytes += cel.Pixels.LongLength * 4L;
                }

                return removedLayer.EstimateMemoryBytes() + lowerLayerBytes;
            }
        }

        public void Execute()
        {
            if (removedLayer == null)
            {
                List<SpriteCel> removedCels = new List<SpriteCel>(document.Frames.Count);
                lowerLayerBeforeMerge = new List<SpriteCel>(document.Frames.Count);
                foreach (SpriteFrame frame in document.Frames)
                {
                    removedCels.Add(frame.GetCel(layerIndex));
                    lowerLayerBeforeMerge.Add(frame.GetCel(layerIndex - 1).Clone());
                }

                removedLayer = new SpriteLayerTrackSnapshot(document.GetLayerTrack(layerIndex), removedCels);
            }

            document.MergeLayerDown(layerIndex);
        }

        public void Undo()
        {
            document.InsertLayerTrackForCommand(layerIndex, removedLayer);
            for (int frameIndex = 0; frameIndex < lowerLayerBeforeMerge.Count; frameIndex++)
            {
                document.ReplaceCelForCommand(frameIndex, layerIndex - 1, lowerLayerBeforeMerge[frameIndex].Clone());
            }
        }
    }

}
