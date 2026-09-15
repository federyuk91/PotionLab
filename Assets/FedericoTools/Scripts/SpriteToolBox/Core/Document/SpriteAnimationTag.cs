using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    public enum SpriteAnimationDirection { Forward, Reverse, PingPong, PingPongReverse }

    [Serializable]
    public sealed class SpriteAnimationTag
    {
        [SerializeField] private string name;
        [SerializeField] private int fromFrame;
        [SerializeField] private int toFrame;
        [SerializeField] private SpriteAnimationDirection direction;
        [SerializeField] private Color color = Color.white;

        public string Name { get => name; set => name = value ?? string.Empty; }
        public int FromFrame => fromFrame;
        public int ToFrame => toFrame;
        public SpriteAnimationDirection Direction { get => direction; set => direction = value; }
        public Color Color { get => color; set => color = value; }

        public SpriteAnimationTag(string name, int fromFrame, int toFrame)
        {
            this.name = name ?? string.Empty;
            this.fromFrame = Mathf.Max(0, fromFrame);
            this.toFrame = Mathf.Max(this.fromFrame, toFrame);
        }

        public void SetFrameRange(int fromFrame, int toFrame, int frameCount)
        {
            int safeFrameCount = Mathf.Max(1, frameCount);
            int clampedFromFrame = Mathf.Clamp(fromFrame, 0, safeFrameCount - 1);
            int clampedToFrame = Mathf.Clamp(toFrame, clampedFromFrame, safeFrameCount - 1);
            this.fromFrame = clampedFromFrame;
            this.toFrame = clampedToFrame;
        }

        public void ClampToFrameCount(int frameCount)
        {
            SetFrameRange(fromFrame, toFrame, frameCount);
        }

        public SpriteAnimationTag Clone()
        {
            SpriteAnimationTag clone = new SpriteAnimationTag(name, fromFrame, toFrame)
            {
                Direction = direction,
                Color = color
            };
            return clone;
        }
    }

}
