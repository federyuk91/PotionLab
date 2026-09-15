using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public sealed class SpriteToolboxToolsPanelSettings
    {
        public Color RecolorSource = Color.black;
        public Color RecolorTarget = Color.white;
    }

    public sealed class SpriteToolboxToolsPanel : ISpriteToolboxPanel
    {
        private const int AutomaticRecolorMapMaximumPixelCount = 512 * 512;
        private const int RecolorMapPixelsPerEditorUpdate = 32768;
        private static readonly Color ToolsAccentColor = new Color(0.94f, 0.76f, 0.38f);
        private static readonly Color BrushAccentColor = new Color(0.57f, 0.78f, 0.88f);
        private static readonly Color SelectionAccentColor = new Color(0.95f, 0.89f, 0.84f);
        private static readonly Color RecolorAccentColor = new Color(0.91f, 0.48f, 0.42f);
        private static readonly Color SecondaryColorAccent = new Color(111f / 255f, 158f / 255f, 196f / 255f);

        private readonly ISpriteToolboxState state;
        private readonly IToolActions actions;
        private readonly IRecolorActions recolorActions;
        private readonly IPaletteActions paletteActions;
        private readonly Action repaint;
        private readonly Action<SpriteToolboxTool> requestToolSelection;
        private readonly ILightPadState lightPadState;
        private readonly ILightPadActions lightPadActions;
        private readonly Action<ISpriteToolboxState, IToolActions> testDraw;
        private readonly List<SpriteRecolorMapping> recolorMappings = new List<SpriteRecolorMapping>();
        private SpriteCanvasColorMap recolorSourceMap;
        private Vector2 recolorScrollPosition;
        private SpriteToolboxTool previousTool;
        private int recolorFrameIndex = -1;
        private int recolorLayerIndex = -1;
        private int recolorPreviewVersion;
        private int recolorTargetColorCount;
        private bool recolorPreviewEnabled;
        private bool recolorUseDithering;
        private bool preserveRecolorSourcesOnNextInvalidation;
        private RecolorMapGeneration recolorMapGeneration;

        internal IReadOnlyList<SpriteRecolorMapping> RecolorMappings => recolorMappings;
        internal SpriteCanvasColorMap RecolorSourceMap => recolorSourceMap;
        internal int RecolorPreviewVersion => recolorPreviewVersion;
        internal bool IsRecolorPreviewEnabled => recolorPreviewEnabled;
        internal bool HasPendingRecolorChanges => HasRecolorChanges();

        public SpriteToolboxToolsPanel(
            ISpriteToolboxToolsContext context,
            SpriteToolboxToolsPanelSettings settings,
            Action repaint,
            Action<SpriteToolboxTool> requestToolSelection,
            ILightPadState lightPadState,
            ILightPadActions lightPadActions)
        {
            this.state = context ?? throw new ArgumentNullException(nameof(context));
            this.actions = context;
            this.recolorActions = context;
            this.paletteActions = context;
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
            this.requestToolSelection = requestToolSelection ?? throw new ArgumentNullException(nameof(requestToolSelection));
            this.lightPadState = lightPadState ?? throw new ArgumentNullException(nameof(lightPadState));
            this.lightPadActions = lightPadActions ?? throw new ArgumentNullException(nameof(lightPadActions));
        }

        internal SpriteToolboxToolsPanel(ISpriteToolboxState state, IToolActions actions, Action<ISpriteToolboxState, IToolActions> draw)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
            testDraw = draw ?? throw new ArgumentNullException(nameof(draw));
            lightPadState = null;
            lightPadActions = null;
        }

        public void Draw()
        {
            if (testDraw != null)
            {
                testDraw(state, actions);
                return;
            }

            EditorGUILayout.BeginVertical();
            DrawToolButtons();
            bool activeToolChanged = previousTool != state.ActiveTool;
            if (lightPadState.IsSelected)
            {
                GUILayout.FlexibleSpace();
                DrawToolSettingsSeparator();
                DrawLightPadSettings();
            }
            else if (state.ActiveTool == SpriteToolboxTool.Recolor)
            {
                EnsureRecolorMappings(activeToolChanged || recolorFrameIndex != state.SelectedFrameIndex || recolorLayerIndex != state.SelectedLayerIndex);
                DrawRecolorTool();
            }
            else
            {
                GUILayout.FlexibleSpace();
                DrawToolSettingsSeparator();
                DrawActiveToolOptions();
            }
            EditorGUILayout.EndVertical();
            previousTool = state.ActiveTool;
        }

        public void Draw(Rect panelRect)
        {
            Rect contentRect = SpriteToolboxLayout.GetContentRect(panelRect, SpriteToolboxLayout.DefaultPanelContentPadding);
            GUILayout.BeginArea(contentRect);
            Draw();
            GUILayout.EndArea();
        }

        private static void DrawToolSettingsSeparator()
        {
            Rect separatorRect = GUILayoutUtility.GetRect(1f, 1f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(separatorRect, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(4f);
            GUILayout.Label("Tool settings", EditorStyles.miniBoldLabel);
            GUILayout.Space(2f);
        }

        private void DrawToolButtons()
        {
            SpriteToolboxEditorGui.DrawAccentLabel("Tools", ToolsAccentColor, EditorStyles.boldLabel);
            SpriteToolboxTool[] tools = { SpriteToolboxTool.Pencil, SpriteToolboxTool.Eraser, SpriteToolboxTool.Fill, SpriteToolboxTool.Line, SpriteToolboxTool.Eyedropper, SpriteToolboxTool.Select };
            for (int toolIndex = 0; toolIndex < tools.Length; toolIndex += 2)
            {
                EditorGUILayout.BeginHorizontal();
                DrawToolButton(tools[toolIndex]);
                DrawToolButton(tools[toolIndex + 1]);
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.BeginHorizontal();
            DrawToolButton(SpriteToolboxTool.Recolor);
            DrawLightPadButton();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawToolButton(SpriteToolboxTool tool)
        {
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = state.ActiveTool == tool ? BrushAccentColor : Color.white;
            Texture2D icon = SpriteToolboxEditorIcons.Get(GetToolIconName(tool));
            GUIContent content = icon == null ? new GUIContent(tool.ToString()) : new GUIContent(icon, tool.ToString());
            bool requested = GUILayout.Toggle(state.ActiveTool == tool, content, "Button", GUILayout.Width(64f), GUILayout.Height(64f));
            GUI.backgroundColor = previousBackgroundColor;
            if (requested && (state.ActiveTool != tool || lightPadState.IsSelected))
            {
                requestToolSelection(tool);
            }
        }

        private void DrawLightPadButton()
        {
            bool selected = lightPadState.IsSelected;
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = selected ? BrushAccentColor : Color.white;
            Texture2D icon = SpriteToolboxEditorIcons.Get("IconLightPad");
            GUIContent content = icon == null ? new GUIContent("LightPad") : new GUIContent(icon, "LightPad");
            bool requested = GUILayout.Toggle(selected, content, "Button", GUILayout.Width(64f), GUILayout.Height(64f));
            GUI.backgroundColor = previousBackgroundColor;
            if (requested != selected)
            {
                lightPadActions.SetSelected(requested);
            }
        }

        private void DrawLightPadSettings()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("LightPad", EditorStyles.miniBoldLabel);
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(58f)))
            {
                lightPadActions.Refresh();
                repaint();
            }
            EditorGUILayout.EndHorizontal();

            bool requestedEnabled = EditorGUILayout.ToggleLeft("On", lightPadState.IsEnabled);
            if (requestedEnabled != lightPadState.IsEnabled)
            {
                lightPadActions.SetEnabled(requestedEnabled);
                repaint();
            }

            DrawLightPadField("Source", () =>
            {
                SpriteLightPadSourceType requestedSourceType = (SpriteLightPadSourceType)EditorGUILayout.EnumPopup(GUIContent.none, lightPadState.SourceType, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                if (requestedSourceType != lightPadState.SourceType)
                {
                    lightPadActions.SetSourceType(requestedSourceType);
                    repaint();
                }
            });

            if (lightPadState.SourceType == SpriteLightPadSourceType.Texture)
            {
                DrawLightPadField("Texture", () =>
                {
                    Texture requestedTexture = (Texture)EditorGUILayout.ObjectField(GUIContent.none, lightPadState.SourceTexture, typeof(Texture), false, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                    if (requestedTexture != lightPadState.SourceTexture)
                    {
                        lightPadActions.SetSourceTexture(requestedTexture);
                        repaint();
                    }
                });
            }
            else if (lightPadState.SourceType == SpriteLightPadSourceType.Camera)
            {
                DrawLightPadField("Camera", () =>
                {
                    Camera requestedCamera = (Camera)EditorGUILayout.ObjectField(GUIContent.none, lightPadState.SourceCamera, typeof(Camera), true, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                    if (requestedCamera != lightPadState.SourceCamera)
                    {
                        lightPadActions.SetSourceCamera(requestedCamera);
                        repaint();
                    }
                });
            }
            else
            {
                GUILayout.Label("Uses the active Scene View while this tool is selected.", EditorStyles.wordWrappedMiniLabel);
            }

            DrawLightPadField("Opacity", () =>
            {
                float requestedOpacity = EditorGUILayout.Slider(lightPadState.Opacity, 0f, 1f, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                if (!Mathf.Approximately(requestedOpacity, lightPadState.Opacity)) lightPadActions.SetOpacity(requestedOpacity);
            });
            DrawLightPadField("Align", () =>
            {
                SpriteLightPadAlignment requestedAlignment = (SpriteLightPadAlignment)EditorGUILayout.EnumPopup(GUIContent.none, lightPadState.Alignment, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                if (requestedAlignment != lightPadState.Alignment) lightPadActions.SetAlignment(requestedAlignment);
            });
            if (lightPadState.Alignment == SpriteLightPadAlignment.ZoomLevel)
            {
                DrawLightPadField("Ref. zoom", () =>
                {
                    float requestedZoom = EditorGUILayout.Slider(lightPadState.ReferenceZoom, 0.01f, 2f, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                    if (!Mathf.Approximately(requestedZoom, lightPadState.ReferenceZoom)) lightPadActions.SetReferenceZoom(requestedZoom);
                });
            }

            DrawLightPadField("Sampling", () =>
            {
                SpriteLightPadSamplingMode requestedSampling = (SpriteLightPadSamplingMode)EditorGUILayout.EnumPopup(GUIContent.none, lightPadState.SamplingMode, GUILayout.MinWidth(72f), GUILayout.ExpandWidth(true));
                if (requestedSampling != lightPadState.SamplingMode) lightPadActions.SetSamplingMode(requestedSampling);
            });

            bool requestedDithering = EditorGUILayout.ToggleLeft("Dithering", lightPadState.UseDithering);
            if (requestedDithering != lightPadState.UseDithering) lightPadActions.SetDithering(requestedDithering);

            if (lightPadState.SourceType != SpriteLightPadSourceType.SceneView)
            {
                GUILayout.Label("Drag on the canvas to position the reference.", EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Reset position", EditorStyles.miniButton))
                {
                    lightPadActions.ResetReferencePosition();
                    repaint();
                }
            }

            bool previousEnabled = GUI.enabled;
            GUI.enabled = lightPadState.HasReferenceCapture && state.Palette.Count > 0;
            if (GUILayout.Button("Import reference as layer", EditorStyles.miniButton))
            {
                lightPadActions.ImportReferenceAsLayer();
            }

            GUI.enabled = previousEnabled;
            if (state.Palette.Count == 0)
            {
                GUILayout.Label("Set an active palette before importing the reference.", EditorStyles.wordWrappedMiniLabel);
            }

            GUILayout.Label(lightPadState.Status, EditorStyles.wordWrappedMiniLabel);
        }

        private static void DrawLightPadField(string label, Action drawField)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(64f));
            drawField();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawActiveToolOptions()
        {
            switch (state.ActiveTool)
            {
                case SpriteToolboxTool.Pencil:
                    DrawBrushSettings(true);
                    DrawActiveColors();
                    break;
                case SpriteToolboxTool.Eraser:
                    DrawBrushSettings(false);
                    break;
                case SpriteToolboxTool.Fill:
                    DrawSymmetrySettings("Fill");
                    DrawTolerance();
                    DrawActiveColors();
                    break;
                case SpriteToolboxTool.Line:
                    DrawSymmetrySettings("Line");
                    DrawActiveColors();
                    break;
                case SpriteToolboxTool.Eyedropper:
                    DrawEyedropperHint();
                    DrawActiveColors();
                    break;
                case SpriteToolboxTool.Select:
                    DrawSelectionCommands();
                    break;
            }
        }

        private void DrawBrushSettings(bool showPixelPerfectOption)
        {
            GUILayout.Space(8f);
            SpriteToolboxEditorGui.DrawAccentLabel("Brush", BrushAccentColor, EditorStyles.boldLabel);
            DrawBrushSize();
            DrawBrushShape();
            DrawToggle("Horizontal symmetry", state.HorizontalSymmetry, actions.SetHorizontalSymmetry);
            DrawToggle("Vertical symmetry", state.VerticalSymmetry, actions.SetVerticalSymmetry);
            DrawToggle("Tiled mode", state.TiledMode, actions.SetTiledMode);
            if (showPixelPerfectOption)
            {
                DrawToggle("Pixel-perfect pencil", state.PixelPerfectPencil, actions.SetPixelPerfectPencil);
            }
        }

        private void DrawBrushSize()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Size", GUILayout.Width(30f));
            int requestedSize = EditorGUILayout.IntField(state.BrushSize, GUILayout.Width(34f));
            int clampedRequestedSize = Mathf.Clamp(requestedSize, 1, 8);
            float sliderValue = GUILayout.HorizontalSlider(state.BrushSize, 1f, 8f, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            int brushSize = requestedSize != state.BrushSize
                ? clampedRequestedSize
                : Mathf.RoundToInt(sliderValue);

            if (brushSize != state.BrushSize)
            {
                actions.SetBrushSize(brushSize);
            }
        }

        private void DrawBrushShape()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Shape", GUILayout.Width(38f));
            DrawBrushShapeButton(SpriteBrushShape.Square, "IconSquareBrush", "Square brush");
            DrawBrushShapeButton(SpriteBrushShape.Round, "IconRoundBrush", "Round brush");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawBrushShapeButton(SpriteBrushShape shape, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            GUIContent content = icon == null ? new GUIContent(shape.ToString(), tooltip) : new GUIContent(icon, tooltip);
            bool isSelected = state.BrushShape == shape;
            bool requested = GUILayout.Toggle(isSelected, content, "Button", GUILayout.Width(32f), GUILayout.Height(26f));
            if (requested && !isSelected)
            {
                actions.SetBrushShape(shape);
            }
        }

        private void DrawSymmetrySettings(string label)
        {
            GUILayout.Space(8f);
            SpriteToolboxEditorGui.DrawAccentLabel(label, ToolsAccentColor, EditorStyles.boldLabel);
            DrawToggle("Horizontal symmetry", state.HorizontalSymmetry, actions.SetHorizontalSymmetry);
            DrawToggle("Vertical symmetry", state.VerticalSymmetry, actions.SetVerticalSymmetry);
        }

        private void DrawEyedropperHint()
        {
            GUILayout.Space(8f);
            SpriteToolboxEditorGui.DrawAccentLabel("Eyedropper", ToolsAccentColor, EditorStyles.boldLabel);
            GUILayout.Label("Click a pixel on the canvas to pick its color.", EditorStyles.wordWrappedMiniLabel);
        }

        private void DrawActiveColors()
        {
            GUILayout.Space(8f);
            SpriteToolboxEditorGui.DrawAccentLabel("Colors", ToolsAccentColor, EditorStyles.boldLabel);
            Color primaryColor = SpriteToolboxEditorGui.DrawColorField("Primary", state.PrimaryColor, 58f, ToolsAccentColor);
            if (primaryColor != state.PrimaryColor)
            {
                paletteActions.SetPrimaryColor(primaryColor);
            }

            Color secondaryColor = SpriteToolboxEditorGui.DrawColorField("Secondary", state.SecondaryColor, 58f, SecondaryColorAccent);
            if (secondaryColor != state.SecondaryColor)
            {
                paletteActions.SetSecondaryColor(secondaryColor);
            }

            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("X:", GUILayout.Width(16f));
            if (GUILayout.Button("Swap", GUILayout.Width(42f)))
            {
                paletteActions.SwapColors();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionCommands()
        {
            SpriteToolboxEditorGui.DrawAccentLabel("Selection", SelectionAccentColor, EditorStyles.boldLabel);
            DrawSelectionMode();
            if (state.SelectionMode == SpriteSelectionMode.Magic) DrawTolerance();
            if (GUILayout.Button("Invert Selection")) actions.InvertPixelSelection();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = state.HasPixelSelection;
            if (GUILayout.Button("Crop")) actions.CropCanvasToSelection();
            GUI.enabled = previousEnabled;
            GUI.enabled = state.HasPixelSelection;
            if (GUILayout.Button("Copy")) actions.CopyPixelSelection();
            if (GUILayout.Button("Cut")) actions.CutPixelSelection();
            if (GUILayout.Button("Fill")) actions.FillPixelSelection(state.PrimaryColor);
            if (GUILayout.Button("Clear")) actions.ClearPixelSelectionPixels();
            if (GUILayout.Button("Flip H")) actions.FlipPixelSelection(true);
            if (GUILayout.Button("Flip V")) actions.FlipPixelSelection(false);
            GUI.enabled = state.HasSelectionClipboard;
            if (GUILayout.Button("Paste")) actions.PastePixelSelection();
            GUI.enabled = previousEnabled;
        }

        private void DrawTolerance()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Tolerance", GUILayout.Width(58f));
            int requestedTolerance = EditorGUILayout.IntField(state.FillTolerance, GUILayout.Width(34f));
            int clampedTolerance = Mathf.Clamp(requestedTolerance, 0, 255);
            float sliderTolerance = GUILayout.HorizontalSlider(state.FillTolerance, 0f, 255f, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            int tolerance = requestedTolerance != state.FillTolerance
                ? clampedTolerance
                : Mathf.RoundToInt(sliderTolerance);
            if (tolerance != state.FillTolerance) actions.SetFillTolerance(tolerance);
        }

        private void DrawSelectionMode()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Mode", GUILayout.Width(35f));
            DrawSelectionModeButton(SpriteSelectionMode.Rectangular, "IconSelection", "Rectangular selection");
            DrawSelectionModeButton(SpriteSelectionMode.Magic, "MagicSelectionIcon", "Magic selection");
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSelectionModeButton(SpriteSelectionMode mode, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            GUIContent content = icon == null ? new GUIContent(mode.ToString(), tooltip) : new GUIContent(icon, tooltip);
            bool selected = state.SelectionMode == mode;
            bool requested = GUILayout.Toggle(selected, content, "Button", GUILayout.Width(32f), GUILayout.Height(26f));
            if (requested && !selected) actions.SetSelectionMode(mode);
        }

        private void EnsureRecolorMappings(bool activeToolChanged)
        {
            if (!activeToolChanged)
            {
                return;
            }

            if (state.DocumentWidth * state.DocumentHeight <= AutomaticRecolorMapMaximumPixelCount)
            {
                CaptureRecolorSources();
            }
            else
            {
                ClearRecolorSources();
            }
            recolorPreviewEnabled = Mathf.Max(state.DocumentWidth, state.DocumentHeight) <= 512;
        }

        private void DrawRecolorTool()
        {
            GUILayout.Space(8f);
            SpriteToolboxEditorGui.DrawAccentLabel("Recolor", RecolorAccentColor, EditorStyles.boldLabel);
            if (recolorSourceMap == null)
            {
                DrawDeferredRecolorGeneration();
                return;
            }
            DrawColorReductionControls();
            recolorUseDithering = EditorGUILayout.ToggleLeft("Dithering", recolorUseDithering);
            EditorGUI.BeginDisabledGroup(state.Palette == null || state.Palette.Count == 0 || recolorSourceMap == null);
            if (GUILayout.Button("Map active cel to palette"))
            {
                if (recolorActions.MapSelectedCelToPalette(state.Palette, recolorUseDithering))
                {
                    InvalidateRecolorSources();
                    repaint();
                }
            }
            EditorGUI.EndDisabledGroup();
            bool requestedPreviewEnabled = EditorGUILayout.ToggleLeft("Preview", recolorPreviewEnabled);
            if (requestedPreviewEnabled != recolorPreviewEnabled)
            {
                recolorPreviewEnabled = requestedPreviewEnabled;
                recolorPreviewVersion++;
                repaint();
            }

            GUILayout.Label("Source → Destination", EditorStyles.miniLabel);

            recolorScrollPosition = EditorGUILayout.BeginScrollView(recolorScrollPosition, GUILayout.ExpandHeight(true));
            for (int mappingIndex = 0; mappingIndex < recolorMappings.Count; mappingIndex++)
            {
                SpriteRecolorMapping mapping = recolorMappings[mappingIndex];
                EditorGUILayout.BeginHorizontal();
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.ColorField(mapping.Source, GUILayout.Width(48f));
                EditorGUI.EndDisabledGroup();
                GUILayout.Label("→", GUILayout.Width(14f));
                Color target = EditorGUILayout.ColorField(mapping.Target, GUILayout.ExpandWidth(true));
                EditorGUILayout.EndHorizontal();

                if (target != mapping.Target)
                {
                    recolorMappings[mappingIndex] = new SpriteRecolorMapping(mapping.Source, target);
                    recolorPreviewVersion++;
                    repaint();
                }
            }

            EditorGUILayout.EndScrollView();
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = RecolorAccentColor;
            bool applyRequested = GUILayout.Button("Apply");
            GUI.backgroundColor = previousBackgroundColor;
            if (applyRequested)
            {
                ApplyPendingRecolor();
            }
        }

        private void DrawColorReductionControls()
        {
            int sourceColorCount = recolorSourceMap == null ? 0 : recolorSourceMap.Colors.Count;
            bool canReduce = SpriteColorReductionService.SupportsColorFamilyOperations(sourceColorCount);
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Colors", GUILayout.Width(38f));
            EditorGUI.BeginDisabledGroup(true);
            EditorGUILayout.IntField(sourceColorCount, GUILayout.Width(38f));
            EditorGUI.EndDisabledGroup();
            GUILayout.Space(8f);
            GUILayout.Label("Reduce to", GUILayout.Width(58f));
            EditorGUI.BeginDisabledGroup(!canReduce);
            int requestedTargetColorCount = EditorGUILayout.IntField(recolorTargetColorCount, GUILayout.Width(38f));
            EditorGUI.EndDisabledGroup();
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            if (!canReduce)
            {
                GUILayout.Label($"Color reduction is available up to {SpriteColorReductionService.MaximumColorFamilyOperationColorCount} source colors.", EditorStyles.miniLabel);
                return;
            }

            int clampedTargetColorCount = Mathf.Clamp(requestedTargetColorCount, 1, Mathf.Max(1, sourceColorCount));
            if (sourceColorCount > 0 && clampedTargetColorCount != recolorTargetColorCount)
            {
                recolorTargetColorCount = clampedTargetColorCount;
                ApplyColorReduction();
            }
        }

        private void ApplyColorReduction()
        {
            IReadOnlyList<SpriteRecolorMapping> reducedMappings = recolorActions.CreateColorReductionMappings(recolorSourceMap, recolorTargetColorCount);
            recolorMappings.Clear();
            recolorMappings.AddRange(reducedMappings);
            recolorPreviewVersion++;
            repaint();
        }

        internal bool ApplyPendingRecolor()
        {
            SpriteCanvasColorMap previousSourceMap = recolorSourceMap;
            List<SpriteRecolorMapping> appliedMappings = new List<SpriteRecolorMapping>(recolorMappings);
            preserveRecolorSourcesOnNextInvalidation = true;
            if (!HasRecolorChanges() || !recolorActions.ApplyRecolorMappings(recolorMappings, recolorSourceMap))
            {
                preserveRecolorSourcesOnNextInvalidation = false;
                return false;
            }

            recolorSourceMap = CreateRemappedSourceMap(previousSourceMap, appliedMappings);
            recolorMappings.Clear();
            foreach (Color sourceColor in recolorSourceMap.Colors)
            {
                recolorMappings.Add(new SpriteRecolorMapping(sourceColor, sourceColor));
            }

            recolorTargetColorCount = recolorMappings.Count;
            recolorPreviewVersion++;
            return true;
        }

        internal void DiscardRecolorPreview()
        {
            recolorMappings.Clear();
            recolorPreviewVersion++;
        }

        private void CaptureRecolorSources()
        {
            recolorMappings.Clear();
            recolorSourceMap = recolorActions.CreateCanvasColorMap();
            foreach (Color sourceColor in recolorSourceMap.Colors)
            {
                recolorMappings.Add(new SpriteRecolorMapping(sourceColor, sourceColor));
            }

            recolorPreviewVersion++;
            recolorFrameIndex = state.SelectedFrameIndex;
            recolorLayerIndex = state.SelectedLayerIndex;
            recolorTargetColorCount = recolorMappings.Count;
        }

        private void ClearRecolorSources()
        {
            recolorMappings.Clear();
            recolorSourceMap = null;
            recolorPreviewVersion++;
            recolorFrameIndex = state.SelectedFrameIndex;
            recolorLayerIndex = state.SelectedLayerIndex;
            recolorTargetColorCount = 0;
        }

        private void DrawDeferredRecolorGeneration()
        {
            GUILayout.Label("Recolor sources are not generated automatically for images larger than 512 × 512.", EditorStyles.wordWrappedMiniLabel);
            if (recolorMapGeneration != null)
            {
                GUILayout.Label("Generating recolor sources…", EditorStyles.miniLabel);
                return;
            }

            if (GUILayout.Button("Generate recolor sources"))
            {
                BeginRecolorMapGeneration();
            }
        }

        private void BeginRecolorMapGeneration()
        {
            Color32[] selectedLayerPixels = recolorActions.GetSelectedLayerPixels();
            recolorMapGeneration = new RecolorMapGeneration(selectedLayerPixels, state.DocumentWidth, state.DocumentHeight);
            EditorApplication.update += ProcessRecolorMapGeneration;
        }

        private void ProcessRecolorMapGeneration()
        {
            if (recolorMapGeneration == null)
            {
                EditorApplication.update -= ProcessRecolorMapGeneration;
                return;
            }

            float progress = recolorMapGeneration.Process(RecolorMapPixelsPerEditorUpdate);
            if (EditorUtility.DisplayCancelableProgressBar("Generate recolor sources", "Indexing the selected layer…", progress))
            {
                EndRecolorMapGeneration();
                repaint();
                return;
            }

            if (!recolorMapGeneration.IsComplete)
            {
                return;
            }

            recolorSourceMap = recolorMapGeneration.CreateMap();
            recolorMappings.Clear();
            foreach (Color sourceColor in recolorSourceMap.Colors)
            {
                recolorMappings.Add(new SpriteRecolorMapping(sourceColor, sourceColor));
            }

            recolorTargetColorCount = recolorMappings.Count;
            recolorPreviewVersion++;
            EndRecolorMapGeneration();
            repaint();
        }

        private void EndRecolorMapGeneration()
        {
            EditorApplication.update -= ProcessRecolorMapGeneration;
            recolorMapGeneration = null;
            EditorUtility.ClearProgressBar();
        }

        internal void InvalidateRecolorSources()
        {
            if (preserveRecolorSourcesOnNextInvalidation)
            {
                preserveRecolorSourcesOnNextInvalidation = false;
                return;
            }

            if (recolorMapGeneration != null)
            {
                EndRecolorMapGeneration();
            }
            recolorFrameIndex = -1;
            recolorLayerIndex = -1;
        }

        private static SpriteCanvasColorMap CreateRemappedSourceMap(SpriteCanvasColorMap sourceMap, IReadOnlyList<SpriteRecolorMapping> mappings)
        {
            Dictionary<Color32, Color32> targetsBySource = new Dictionary<Color32, Color32>();
            foreach (SpriteRecolorMapping mapping in mappings)
            {
                targetsBySource[(Color32)mapping.Source] = mapping.Target;
            }

            Dictionary<Color32, List<Vector2Int>> mutablePositionsByColor = new Dictionary<Color32, List<Vector2Int>>();
            List<Color> colors = new List<Color>();
            foreach (Color sourceColor in sourceMap.Colors)
            {
                Color32 source = sourceColor;
                Color32 target = targetsBySource.TryGetValue(source, out Color32 mappedTarget) ? mappedTarget : source;
                if (!mutablePositionsByColor.TryGetValue(target, out List<Vector2Int> positions))
                {
                    positions = new List<Vector2Int>();
                    mutablePositionsByColor.Add(target, positions);
                    colors.Add(target);
                }

                positions.AddRange(sourceMap.GetPositions(sourceColor));
            }

            Dictionary<Color32, IReadOnlyList<Vector2Int>> positionsByColor = new Dictionary<Color32, IReadOnlyList<Vector2Int>>();
            foreach (KeyValuePair<Color32, List<Vector2Int>> pair in mutablePositionsByColor)
            {
                positionsByColor.Add(pair.Key, pair.Value);
            }

            IReadOnlyList<Color> orderedColors = SpriteColorReductionService.OrderByColorFamilies(colors);
            return new SpriteCanvasColorMap(orderedColors, positionsByColor);
        }

        internal void Dispose()
        {
            if (recolorMapGeneration != null)
            {
                EndRecolorMapGeneration();
            }
        }

        private bool HasRecolorChanges()
        {
            foreach (SpriteRecolorMapping mapping in recolorMappings)
            {
                if (mapping.Source != mapping.Target)
                {
                    return true;
                }
            }

            return false;
        }

        private sealed class RecolorMapGeneration
        {
            private readonly Color32[] pixels;
            private readonly int width;
            private readonly Dictionary<Color32, List<Vector2Int>> positionsByColor = new Dictionary<Color32, List<Vector2Int>>();
            private readonly List<Color> colors = new List<Color>();
            private int nextPixelIndex;

            public bool IsComplete => nextPixelIndex >= pixels.Length;

            public RecolorMapGeneration(Color32[] pixels, int width, int height)
            {
                if (pixels == null) throw new ArgumentNullException(nameof(pixels));
                if (pixels.Length != width * height) throw new ArgumentException("The pixel buffer must match the supplied dimensions.", nameof(pixels));
                this.pixels = pixels;
                this.width = width;
            }

            public float Process(int pixelBudget)
            {
                int endIndex = Mathf.Min(pixels.Length, nextPixelIndex + pixelBudget);
                for (; nextPixelIndex < endIndex; nextPixelIndex++)
                {
                    Color32 color = pixels[nextPixelIndex];
                    if (color.a == 0)
                    {
                        continue;
                    }

                    if (!positionsByColor.TryGetValue(color, out List<Vector2Int> positions))
                    {
                        positions = new List<Vector2Int>();
                        positionsByColor.Add(color, positions);
                        colors.Add(color);
                    }

                    positions.Add(new Vector2Int(nextPixelIndex % width, nextPixelIndex / width));
                }

                return pixels.Length == 0 ? 1f : nextPixelIndex / (float)pixels.Length;
            }

            public SpriteCanvasColorMap CreateMap()
            {
                Dictionary<Color32, IReadOnlyList<Vector2Int>> readOnlyPositions = new Dictionary<Color32, IReadOnlyList<Vector2Int>>();
                foreach (KeyValuePair<Color32, List<Vector2Int>> pair in positionsByColor)
                {
                    readOnlyPositions.Add(pair.Key, pair.Value);
                }

                IReadOnlyList<Color> orderedColors = SpriteColorReductionService.OrderByColorFamilies(colors);
                return new SpriteCanvasColorMap(orderedColors, readOnlyPositions);
            }
        }

        private static void DrawToggle(string label, bool value, Action<bool> onChanged)
        {
            bool requestedValue = EditorGUILayout.ToggleLeft(label, value);
            if (requestedValue != value) onChanged(requestedValue);
        }

        private static string GetToolIconName(SpriteToolboxTool tool)
        {
            switch (tool)
            {
                case SpriteToolboxTool.Pencil: return "IconPencil";
                case SpriteToolboxTool.Eraser: return "Eraser";
                case SpriteToolboxTool.Fill: return "IconFill";
                case SpriteToolboxTool.Line: return "IconLine";
                case SpriteToolboxTool.Eyedropper: return "IconEyedropper";
                case SpriteToolboxTool.Select: return "IconSelection";
                case SpriteToolboxTool.Recolor: return "IconRecolorTool";
                default: return string.Empty;
            }
        }

    }
}
