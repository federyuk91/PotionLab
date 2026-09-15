using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class SpriteCommandHistory
    {
        private readonly LinkedList<ISpriteCommand> undoStack = new LinkedList<ISpriteCommand>();
        private readonly LinkedList<ISpriteCommand> redoStack = new LinkedList<ISpriteCommand>();
        private long retainedMemoryBytes;

        public SpriteCommandHistory(long memoryBudgetBytes = 64L * 1024L * 1024L)
        {
            MemoryBudgetBytes = System.Math.Max(0L, memoryBudgetBytes);
        }

        public bool CanUndo => undoStack.Count > 0;
        public bool CanRedo => redoStack.Count > 0;
        public long RetainedMemoryBytes => retainedMemoryBytes;
        public long MemoryBudgetBytes { get; private set; }

        public void SetMemoryBudget(long memoryBudgetBytes)
        {
            MemoryBudgetBytes = System.Math.Max(0L, memoryBudgetBytes);
            EnforceMemoryBudget();
        }

        public void Execute(ISpriteCommand command)
        {
            command.Execute();
            ClearRedoStack();
            undoStack.AddLast(command);
            retainedMemoryBytes += command.EstimatedMemoryBytes;
            EnforceMemoryBudget();
        }

        public void Undo()
        {
            if (!CanUndo)
            {
                return;
            }

            ISpriteCommand command = undoStack.Last.Value;
            undoStack.RemoveLast();
            command.Undo();
            redoStack.AddLast(command);
        }

        public void Redo()
        {
            if (!CanRedo)
            {
                return;
            }

            ISpriteCommand command = redoStack.Last.Value;
            redoStack.RemoveLast();
            command.Execute();
            undoStack.AddLast(command);
        }

        public void Clear()
        {
            undoStack.Clear();
            redoStack.Clear();
            retainedMemoryBytes = 0L;
        }

        private void ClearRedoStack()
        {
            foreach (ISpriteCommand command in redoStack)
            {
                retainedMemoryBytes -= command.EstimatedMemoryBytes;
            }

            redoStack.Clear();
        }

        private void EnforceMemoryBudget()
        {
            while (retainedMemoryBytes > MemoryBudgetBytes && undoStack.Count > 0)
            {
                ISpriteCommand discardedCommand = undoStack.First.Value;
                retainedMemoryBytes -= discardedCommand.EstimatedMemoryBytes;
                undoStack.RemoveFirst();
            }

            while (retainedMemoryBytes > MemoryBudgetBytes && redoStack.Count > 0)
            {
                ISpriteCommand discardedCommand = redoStack.First.Value;
                retainedMemoryBytes -= discardedCommand.EstimatedMemoryBytes;
                redoStack.RemoveFirst();
            }
        }
    }

    public interface ISpriteCommand
    {
        long EstimatedMemoryBytes { get; }
        void Execute();
        void Undo();
    }

}
