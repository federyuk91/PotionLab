using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [Serializable]
    public sealed class SpriteFrame
    {
        [SerializeField] private List<SpriteCel> cels = new List<SpriteCel>();
        [SerializeField] private float duration = 0.1f;

        public IReadOnlyList<SpriteCel> Cels => cels;
        public float Duration { get => duration; set => duration = Mathf.Max(0.01f, value); }

        public SpriteFrame(int width, int height, int celCount)
        {
            for (int celIndex = 0; celIndex < celCount; celIndex++)
            {
                cels.Add(new SpriteCel(width, height));
            }
        }

        private SpriteFrame()
        {
        }

        public SpriteCel GetCel(int layerIndex)
        {
            return cels[layerIndex];
        }

        internal void AddCel(int width, int height)
        {
            cels.Add(new SpriteCel(width, height));
        }

        internal void RemoveCel(int layerIndex)
        {
            cels.RemoveAt(layerIndex);
        }

        internal void InsertCel(int layerIndex, SpriteCel cel)
        {
            cels.Insert(Mathf.Clamp(layerIndex, 0, cels.Count), cel);
        }

        internal void MoveCel(int layerIndex, int destinationIndex)
        {
            SpriteCel cel = cels[layerIndex];
            cels.RemoveAt(layerIndex);
            cels.Insert(destinationIndex, cel);
        }

        internal void ReplaceCel(int layerIndex, SpriteCel cel)
        {
            cels[layerIndex] = cel;
        }

        internal void EnsureCelCount(int requiredCount, int width, int height)
        {
            while (cels.Count < requiredCount)
            {
                cels.Add(new SpriteCel(width, height));
            }

            while (cels.Count > requiredCount)
            {
                cels.RemoveAt(cels.Count - 1);
            }
        }

        public SpriteFrame Clone()
        {
            SpriteFrame clone = new SpriteFrame { duration = duration };
            foreach (SpriteCel cel in cels)
            {
                clone.cels.Add(cel.Clone());
            }

            return clone;
        }
    }

}
