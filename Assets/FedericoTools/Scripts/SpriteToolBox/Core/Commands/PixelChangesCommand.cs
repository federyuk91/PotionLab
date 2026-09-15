using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class PixelChangesCommand : ISpriteCommand
    {
        private readonly SpriteCel cel;
        private readonly IReadOnlyList<PixelChange> changes;

        public PixelChangesCommand(SpriteCel cel, IReadOnlyList<PixelChange> changes)
        {
            this.cel = cel;
            this.changes = changes;
        }

        public long EstimatedMemoryBytes => changes.Count * 40L;

        public void Execute()
        {
            foreach (PixelChange change in changes)
            {
                cel.SetPixel(change.Position, change.NewColor);
            }
        }

        public void Undo()
        {
            foreach (PixelChange change in changes)
            {
                cel.SetPixel(change.Position, change.PreviousColor);
            }
        }
    }

}
