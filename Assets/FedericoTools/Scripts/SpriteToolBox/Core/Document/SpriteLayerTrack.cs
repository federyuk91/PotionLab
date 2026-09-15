using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    internal sealed class SpriteLayerTrackSnapshot
    {
        public SpriteLayerTrack Track { get; }
        public IReadOnlyList<SpriteCel> Cels { get; }

        public SpriteLayerTrackSnapshot(SpriteLayerTrack track, IReadOnlyList<SpriteCel> cels)
        {
            Track = track;
            Cels = cels;
        }

        public long EstimateMemoryBytes()
        {
            long pixelCount = 0L;
            foreach (SpriteCel cel in Cels)
            {
                pixelCount += cel.Pixels.LongLength;
            }

            return pixelCount * 4L + 128L;
        }
    }

    [Serializable]
    public sealed class SpriteLayerTrack
    {
        [SerializeField] private string name;
        [SerializeField] private bool isVisible = true;
        [SerializeField] private bool isLocked;
        [SerializeField] private float opacity = 1f;

        public string Name { get => name; set => name = value; }
        public bool IsVisible { get => isVisible; set => isVisible = value; }
        public bool IsLocked { get => isLocked; set => isLocked = value; }
        public float Opacity { get => opacity; set => opacity = Mathf.Clamp01(value); }

        public SpriteLayerTrack(string name, bool isVisible = true, bool isLocked = false, float opacity = 1f)
        {
            this.name = name;
            this.isVisible = isVisible;
            this.isLocked = isLocked;
            this.opacity = opacity;
        }

        public SpriteLayerTrack Clone()
        {
            return new SpriteLayerTrack(name, isVisible, isLocked, opacity);
        }
    }

}
