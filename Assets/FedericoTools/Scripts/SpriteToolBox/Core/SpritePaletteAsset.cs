using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [CreateAssetMenu(menuName = "Federico Tools/Sprite Toolbox/Palette", fileName = "NewSpritePalette")]
    public sealed class SpritePaletteAsset : ScriptableObject
    {
        [SerializeField] private List<Color> colors = new List<Color>();

        public IReadOnlyList<Color> Colors => colors;

        public void SetColors(IReadOnlyList<Color> sourceColors)
        {
            colors.Clear();
            foreach (Color color in sourceColors)
            {
                if (!colors.Contains(color))
                {
                    colors.Add(color);
                }
            }
        }
    }
}
