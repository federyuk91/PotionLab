using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal static class SpriteToolboxLayout
    {
        public const float DefaultPanelContentPadding = 20f;

        public static Rect GetContentRect(Rect panelRect, float padding)
        {
            float contentWidth = Mathf.Max(1f, panelRect.width - padding * 2f);
            float contentHeight = Mathf.Max(1f, panelRect.height - padding * 2f);
            return new Rect(panelRect.x + padding, panelRect.y + padding, contentWidth, contentHeight);
        }
    }
}
