using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    /// <summary>Provides the default Pastel20 sequence used for new animation tags.</summary>
    public static class SpriteToolboxTagColorPalette
    {
        private static readonly Color[] Colors =
        {
            new Color32(251, 223, 228, 255), new Color32(144, 183, 220, 255), new Color32(136, 189, 171, 255), new Color32(159, 180, 146, 255),
            new Color32(247, 163, 178, 255), new Color32(141, 188, 234, 255), new Color32(139, 202, 178, 255), new Color32(189, 202, 179, 255),
            new Color32(242, 133, 131, 255), new Color32(156, 151, 210, 255), new Color32(161, 218, 203, 255), new Color32(230, 238, 146, 255),
            new Color32(253, 163, 132, 255), new Color32(193, 157, 199, 255), new Color32(168, 220, 152, 255), new Color32(243, 234, 137, 255),
            new Color32(245, 148, 126, 255), new Color32(203, 176, 215, 255), new Color32(221, 229, 138, 255), new Color32(249, 244, 173, 255)
        };

        public static Color GetColor(int tagIndex)
        {
            int colorIndex = Mathf.Abs(tagIndex) % Colors.Length;
            return Colors[colorIndex];
        }
    }
}
