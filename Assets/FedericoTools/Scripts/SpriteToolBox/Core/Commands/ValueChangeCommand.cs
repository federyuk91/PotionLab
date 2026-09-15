using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class ValueChangeCommand<T> : ISpriteCommand
    {
        private readonly Action<T> setter;
        private readonly T before;
        private readonly T after;

        public ValueChangeCommand(Action<T> setter, T before, T after)
        {
            this.setter = setter;
            this.before = before;
            this.after = after;
        }

        public long EstimatedMemoryBytes => 128L;

        public void Execute() => setter(after);
        public void Undo() => setter(before);
    }
}
