using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class MoveLayerCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int sourceIndex;
        private readonly int destinationIndex;

        public MoveLayerCommand(SpriteDocument document, int sourceIndex, int destinationIndex)
        {
            this.document = document;
            this.sourceIndex = sourceIndex;
            this.destinationIndex = destinationIndex;
        }

        public long EstimatedMemoryBytes => 128L;

        public void Execute() => document.MoveLayerTrack(sourceIndex, destinationIndex);
        public void Undo() => document.MoveLayerTrack(destinationIndex, sourceIndex);
    }

}
