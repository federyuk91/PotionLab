using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class ClearCelCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int frameIndex;
        private readonly int layerIndex;
        private SpriteCel previousCel;
        private SpriteCel clearedCel;

        public ClearCelCommand(SpriteDocument document, int frameIndex, int layerIndex)
        {
            this.document = document;
            this.frameIndex = frameIndex;
            this.layerIndex = layerIndex;
        }

        public long EstimatedMemoryBytes => previousCel == null ? 0L : previousCel.Pixels.LongLength * 4L;

        public void Execute()
        {
            if (clearedCel == null)
            {
                clearedCel = new SpriteCel(document.Width, document.Height);
            }

            previousCel = document.ReplaceCelForCommand(frameIndex, layerIndex, clearedCel);
        }

        public void Undo()
        {
            document.ReplaceCelForCommand(frameIndex, layerIndex, previousCel);
        }
    }

}
