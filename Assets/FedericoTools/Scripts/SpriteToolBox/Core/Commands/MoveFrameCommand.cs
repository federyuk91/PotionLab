using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class MoveFrameCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int sourceIndex;
        private readonly int destinationIndex;
        private List<SpriteAnimationTag> animationTagsBefore;
        private List<SpriteAnimationTag> animationTagsAfter;

        public MoveFrameCommand(SpriteDocument document, int sourceIndex, int destinationIndex)
        {
            this.document = document;
            this.sourceIndex = sourceIndex;
            this.destinationIndex = destinationIndex;
        }

        public long EstimatedMemoryBytes => 128L;

        public void Execute()
        {
            if (animationTagsBefore == null)
            {
                animationTagsBefore = document.CloneAnimationTagsForCommand();
            }

            document.MoveFrame(sourceIndex, destinationIndex);
            if (animationTagsAfter == null)
            {
                animationTagsAfter = document.CloneAnimationTagsForCommand();
            }
            else
            {
                document.RestoreAnimationTagsForCommand(animationTagsAfter);
            }
        }

        public void Undo()
        {
            document.MoveFrame(destinationIndex, sourceIndex);
            document.RestoreAnimationTagsForCommand(animationTagsBefore);
        }
    }

}
