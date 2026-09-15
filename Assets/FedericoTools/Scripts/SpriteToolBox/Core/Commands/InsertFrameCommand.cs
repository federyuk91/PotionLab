using System.Collections.Generic;
using System;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class InsertFrameCommand : ISpriteCommand
    {
        private readonly SpriteDocument document;
        private readonly int frameIndex;
        private readonly bool duplicatePreviousFrame;
        private readonly float duration;
        private SpriteFrame frame;
        private List<SpriteAnimationTag> animationTagsBefore;
        private List<SpriteAnimationTag> animationTagsAfter;

        public InsertFrameCommand(SpriteDocument document, int frameIndex, bool duplicatePreviousFrame, float duration)
        {
            this.document = document;
            this.frameIndex = frameIndex;
            this.duplicatePreviousFrame = duplicatePreviousFrame;
            this.duration = duration;
        }

        public long EstimatedMemoryBytes => frame == null ? 0L : frame.Cels.Count * frame.GetCel(0).Pixels.LongLength * 4L;

        public void Execute()
        {
            if (animationTagsBefore == null)
            {
                animationTagsBefore = document.CloneAnimationTagsForCommand();
            }

            if (frame == null)
            {
                frame = document.CreateFrameForCommand(frameIndex, duplicatePreviousFrame, duration);
            }

            document.InsertFrameForCommand(frameIndex, frame);
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
            document.RemoveFrameForCommand(frameIndex);
            document.RestoreAnimationTagsForCommand(animationTagsBefore);
        }
    }

}
