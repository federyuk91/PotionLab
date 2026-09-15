using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public sealed class SpriteToolboxEditorThemePalette : ScriptableObject
    {
        [SerializeField] private Color palettePanelBackground;
        [SerializeField] private Color canvasPanelBackground;
        [SerializeField] private Color toolsPanelBackground;
        [SerializeField] private Color timelinePanelBackground;
        [SerializeField] private Color toolbarBackground;
        [SerializeField] private Color canvasAccent;
        [SerializeField] private Color toolsAccent;
        [SerializeField] private Color paletteAccent;
        [SerializeField] private Color timelineAccent;
        [SerializeField, Range(0f, 1f)] private float panelAccentBorderOpacity = 0.45f;

        public Color PalettePanelBackground => palettePanelBackground;
        public Color CanvasPanelBackground => canvasPanelBackground;
        public Color ToolsPanelBackground => toolsPanelBackground;
        public Color TimelinePanelBackground => timelinePanelBackground;
        public Color ToolbarBackground => toolbarBackground;
        public Color CanvasAccent => canvasAccent;
        public Color ToolsAccent => toolsAccent;
        public Color PaletteAccent => paletteAccent;
        public Color TimelineAccent => timelineAccent;
        public float PanelAccentBorderOpacity => panelAccentBorderOpacity;
    }
}
