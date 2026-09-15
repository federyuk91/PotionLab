using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public sealed class AnimationPlaybackService
    {
        private double nextFrameTime;
        private int pingPongDirection = 1;
        private int rangeStart = -1;
        private int rangeEnd = -1;
        private SpriteAnimationDirection rangeDirection;
        public bool IsPlaying { get; set; }
        public void ResetSchedule()
        {
            nextFrameTime = 0d;
            rangeStart = -1;
            rangeEnd = -1;
        }
        public bool TryAdvance(SpriteDocument document, ref int frameIndex, double time, SpriteAnimationTag tag)
        {
            if (!IsPlaying || document == null || document.Frames.Count == 0) return false;
            if (nextFrameTime > time) return false;
            int first = tag == null ? 0 : tag.FromFrame;
            int last = tag == null ? document.Frames.Count - 1 : tag.ToFrame;
            SpriteAnimationDirection direction = tag == null ? SpriteAnimationDirection.Forward : tag.Direction;
            if (rangeStart != first || rangeEnd != last || rangeDirection != direction)
            {
                rangeStart = first;
                rangeEnd = last;
                rangeDirection = direction;
                pingPongDirection = direction == SpriteAnimationDirection.PingPongReverse ? -1 : 1;
            }

            frameIndex = Mathf.Clamp(frameIndex, first, last);
            frameIndex = GetNextFrame(frameIndex, first, last, direction);
            nextFrameTime = time + document.GetFrame(frameIndex).Duration;
            return true;
        }

        private int GetNextFrame(int currentFrame, int firstFrame, int lastFrame, SpriteAnimationDirection direction)
        {
            if (firstFrame >= lastFrame)
            {
                return firstFrame;
            }

            if (direction == SpriteAnimationDirection.Forward)
            {
                return currentFrame >= lastFrame ? firstFrame : currentFrame + 1;
            }

            if (direction == SpriteAnimationDirection.Reverse)
            {
                return currentFrame <= firstFrame ? lastFrame : currentFrame - 1;
            }

            if (currentFrame >= lastFrame)
            {
                pingPongDirection = -1;
            }
            else if (currentFrame <= firstFrame)
            {
                pingPongDirection = 1;
            }

            return currentFrame + pingPongDirection;
        }
    }
}
