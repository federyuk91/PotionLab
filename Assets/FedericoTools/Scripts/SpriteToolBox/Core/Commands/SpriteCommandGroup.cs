using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SpriteCommandGroup : ISpriteCommand
    {
        private readonly IReadOnlyList<ISpriteCommand> commands;

        public SpriteCommandGroup(IReadOnlyList<ISpriteCommand> commands)
        {
            this.commands = commands;
        }

        public long EstimatedMemoryBytes
        {
            get
            {
                long total = 0L;
                foreach (ISpriteCommand command in commands)
                {
                    total += command.EstimatedMemoryBytes;
                }

                return total;
            }
        }

        public void Execute()
        {
            foreach (ISpriteCommand command in commands)
            {
                command.Execute();
            }
        }

        public void Undo()
        {
            for (int commandIndex = commands.Count - 1; commandIndex >= 0; commandIndex--)
            {
                commands[commandIndex].Undo();
            }
        }
    }

}
