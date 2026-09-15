using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [Serializable]
    public struct SpriteFrameSlice
    {
        public int FrameIndex;
        public Rect Rect;
        public float Duration;
    }

    [CreateAssetMenu(menuName = "Federico Tools/Sprite Toolbox/Sprite Sheet Descriptor", fileName = "NewSpriteSheetDescriptor")]
    public sealed class SpriteSheetDescriptor : ScriptableObject
    {
        [SerializeField] private Texture2D spriteSheet;
        [SerializeField] private List<SpriteFrameSlice> slices = new List<SpriteFrameSlice>();

        public Texture2D SpriteSheet => spriteSheet;
        public IReadOnlyList<SpriteFrameSlice> Slices => slices;

        public void Initialize(Texture2D texture, SpriteDocument document, int columns)
        {
            spriteSheet = texture;
            slices.Clear();
            int safeColumns = Mathf.Clamp(columns, 1, document.Frames.Count);
            int rows = Mathf.CeilToInt(document.Frames.Count / (float)safeColumns);
            for (int frameIndex = 0; frameIndex < document.Frames.Count; frameIndex++)
            {
                int x = frameIndex % safeColumns;
                int y = rows - 1 - frameIndex / safeColumns;
                SpriteFrameSlice slice = new SpriteFrameSlice
                {
                    FrameIndex = frameIndex,
                    Rect = new Rect(x * document.Width, y * document.Height, document.Width, document.Height),
                    Duration = document.GetFrame(frameIndex).Duration
                };
                slices.Add(slice);
            }
        }
    }
}
