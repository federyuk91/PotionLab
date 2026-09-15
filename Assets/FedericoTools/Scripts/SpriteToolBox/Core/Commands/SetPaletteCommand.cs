using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SetPaletteCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly List<Color> before;
        private readonly List<Color> after;

        public SetPaletteCommand(SpriteDocument document, IReadOnlyList<Color> colors)
        {
            this.document = document;
            before = new List<Color>(document.Palette);
            after = new List<Color>(colors);
        }

        public long EstimatedMemoryBytes => (before.Count + after.Count) * sizeof(float) * 4L;

        public void Execute() => document.SetPalette(after);
        public void Undo() => document.SetPalette(before);
    }

}
