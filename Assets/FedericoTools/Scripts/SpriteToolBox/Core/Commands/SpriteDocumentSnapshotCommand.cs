using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SpriteDocumentSnapshotCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly SpriteDocument before;
        private readonly SpriteDocument after;

        public SpriteDocumentSnapshotCommand(SpriteDocument document, SpriteDocument before, SpriteDocument after)
        {
            this.document = document;
            this.before = before;
            this.after = after;
        }

        public long EstimatedMemoryBytes => before.EstimateMemoryBytes() + after.EstimateMemoryBytes();

        public void Execute()
        {
            document.RestoreFrom(after);
        }

        public void Undo()
        {
            document.RestoreFrom(before);
        }
    }

}
