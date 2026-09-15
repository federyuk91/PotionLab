using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class RemoveFrameCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int frameIndex;
        private SpriteFrame frame;
        private List<SpriteAnimationTag> animationTagsBefore;
        private List<SpriteAnimationTag> animationTagsAfter;

        public RemoveFrameCommand(SpriteDocument document, int frameIndex)
        {
            this.document = document;
            this.frameIndex = frameIndex;
        }

        public long EstimatedMemoryBytes => frame == null ? 0L : frame.Cels.Count * frame.GetCel(0).Pixels.LongLength * 4L;

        public void Execute()
        {
            if (animationTagsBefore == null)
            {
                animationTagsBefore = document.CloneAnimationTagsForCommand();
            }

            frame = document.RemoveFrameForCommand(frameIndex);
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
            document.InsertFrameForCommand(frameIndex, frame);
            document.RestoreAnimationTagsForCommand(animationTagsBefore);
        }
    }

}
