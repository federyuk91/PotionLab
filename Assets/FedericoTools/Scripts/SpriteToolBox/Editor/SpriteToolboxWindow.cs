using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using ActiveTool = FedericoTools.SpriteToolBox.SpriteToolboxTool;

namespace FedericoTools.SpriteToolBox.Editor
{
    public sealed class SpriteToolboxWindow : EditorWindow, ICanvasRenderSource
    {
        private const float SplitterSize = 5f;
        private const float ToolbarHeight = 22f;
        private const float MinimumLeftPanelWidth = 200f;
        private const float MinimumRightPanelWidth = 180f;
        private const float MinimumTimelineHeight = 130f;
        private const string CoreAssemblyDefinitionGuid = "e4025581d46741f4994c6917c74efd52";
        private const string LastOpenedFileDirectorySessionKey = "FedericoTools.SpriteToolbox.LastOpenedFileDirectory";
        private const string ThemePaletteResourcePath = "SpriteToolbox_EditorThemePalette";

        private enum ResizeTarget { None, LeftPanel, RightPanel, Timeline }

        private readonly struct PanelLayout
        {
            public Rect Left { get; }
            public Rect Canvas { get; }
            public Rect Tools { get; }
            public Rect Timeline { get; }

            public PanelLayout(Rect left, Rect canvas, Rect tools, Rect timeline)
            {
                Left = left;
                Canvas = canvas;
                Tools = tools;
                Timeline = timeline;
            }
        }

        [SerializeField] private SpriteDocumentAsset documentAsset;
        [SerializeField] private string documentDisplayName = "Sprite Toolbox";
        private SpriteDocument transientDocument;
        // Keep edits outside the asset until the user explicitly saves.
        private SpriteDocument workingDocument;
        [SerializeField] private SpriteToolboxPalettePanelSettings paletteSettings = new SpriteToolboxPalettePanelSettings();
        [SerializeField] private SpriteToolboxToolsPanelSettings toolsPanelSettings = new SpriteToolboxToolsPanelSettings();
        [SerializeField] private List<string> recentFilePaths = new List<string>();
        [SerializeField] private float leftPanelWidth = 220f;
        [SerializeField] private float rightPanelWidth = 180f;
        [SerializeField] private float timelineHeight = 190f;
        [SerializeField] private float canvasZoom = 18f;
        [SerializeField] private bool showPixelGrid = true;
        [SerializeField] private bool showPreviousOnionSkin = true;
        [SerializeField] private bool showNextOnionSkin = true;
        [SerializeField] private bool loopOnionSkin;
        [SerializeField, Range(0f, 1f)] private float onionSkinOpacity = 0.25f;
        [SerializeField] private SpriteToolboxLightPadSettings lightPadSettings = new SpriteToolboxLightPadSettings();
        [SerializeField] private SpriteToolboxLightPadEditorState lightPadEditorState = new SpriteToolboxLightPadEditorState();
        private SpriteToolboxSession session = new SpriteToolboxSession();
        private bool sessionEventsSubscribed;
        private bool hasUnsavedDocumentChanges;
        private bool closeRequestConfirmed;
        private ResizeTarget resizeTarget;
        private SpriteToolboxPalettePanel palettePanel;
        private SpritePalettePresetRepository palettePresetRepository;
        private SpriteToolboxCanvasPanel canvasPanel;
        private CanvasRenderCache canvasRenderCache;
        private SpriteToolboxLightPadController lightPadController;
        private ILightPadActions lightPadActions;
        private SpriteToolboxTool previousCanvasTool = SpriteToolboxTool.Pencil;
        private SpriteToolboxCanvasController canvasController;
        private SpriteToolboxTimelinePanel timelinePanel;
        private SpriteToolboxToolsPanel toolsPanel;
        private SpriteToolboxPlaybackCoordinator playbackCoordinator;
        private SpriteToolboxDocumentPersistenceService persistenceService;
        private SpriteToolboxImportExportService importExportService;
        private SpriteToolboxNotificationService notificationService;
        private SpriteToolboxEditorThemePalette themePalette;
        private int canvasDocumentWidth = -1;
        private int canvasDocumentHeight = -1;

        private SpriteDocument ActiveDocument => documentAsset != null ? workingDocument : transientDocument;
        private SpriteDocument Document => Session.Document;
        private static string PalettePresetFolder => GetToolRootFolder() + "/PalettePresets";
        private static string DefaultPalettePath => PalettePresetFolder + "/Nord16.asset";
        private SpriteToolboxSession Session
        {
            get
            {
                EnsureDocument();
                EnsureSession();
                return session;
            }
        }

        private static string GetToolRootFolder()
        {
            string coreAssemblyDefinitionPath = AssetDatabase.GUIDToAssetPath(CoreAssemblyDefinitionGuid);
            if (string.IsNullOrEmpty(coreAssemblyDefinitionPath))
            {
                throw new InvalidOperationException("Sprite Toolbox core assembly definition could not be located.");
            }

            return Path.GetDirectoryName(coreAssemblyDefinitionPath).Replace('\\', '/');
        }

        [MenuItem("Tools/Sprite Toolbox/Pixel Editor")]
        // Apre la finestra e garantisce un documento pronto per l editing.
        public static void Open()
        {
            SpriteToolboxWindow window = GetWindow<SpriteToolboxWindow>();
            if (window.documentAsset == null)
            {
                window.SetDocumentDisplayName("Sprite Toolbox");
            }
            window.minSize = new Vector2(800f, 560f);
            window.EnsureDocument();
        }

        // Inizializza servizi, pannelli e controller dopo un reload o l apertura della finestra.
        private void OnEnable()
        {
            wantsMouseMove = true;
            if (recentFilePaths == null) recentFilePaths = new List<string>();
            persistenceService = new SpriteToolboxDocumentPersistenceService(recentFilePaths);
            importExportService = new SpriteToolboxImportExportService();
            RestoreLastDocument();
            EnsureDocument();
            EnsureSession();
            if (paletteSettings == null)
            {
                paletteSettings = new SpriteToolboxPalettePanelSettings();
            }
            if (toolsPanelSettings == null)
            {
                toolsPanelSettings = new SpriteToolboxToolsPanelSettings();
            }
            if (lightPadSettings == null)
            {
                lightPadSettings = new SpriteToolboxLightPadSettings();
            }
            if (lightPadEditorState == null)
            {
                lightPadEditorState = new SpriteToolboxLightPadEditorState();
            }
            palettePresetRepository = new SpritePalettePresetRepository(PalettePresetFolder);
            notificationService = new SpriteToolboxNotificationService(this);
            themePalette = Resources.Load<SpriteToolboxEditorThemePalette>(ThemePaletteResourcePath);
            if (themePalette == null)
            {
                throw new InvalidOperationException($"Missing Sprite Toolbox editor theme palette at Resources/{ThemePaletteResourcePath}.");
            }
            palettePanel = new SpriteToolboxPalettePanel(Session, paletteSettings, palettePresetRepository, notificationService);
            canvasRenderCache = new CanvasRenderCache();
            lightPadController = new SpriteToolboxLightPadController(lightPadSettings, lightPadEditorState);
            lightPadActions = new LightPadActions(lightPadController, SetLightPadSelected, ImportLightPadReferenceAsLayer);
            canvasController = new SpriteToolboxCanvasController(
                Session,
                canvasRenderCache,
                lightPadController,
                lightPadEditorState,
                (color, useSecondaryColor) =>
                {
                    if (useSecondaryColor) Session.SetSecondaryColor(color);
                    else Session.SetPrimaryColor(color);
                    palettePanel.AddRecentColor(color);
                },
                Repaint);
            canvasPanel = new SpriteToolboxCanvasPanel(Session, this, canvasController, lightPadController, canvasZoom, showPixelGrid, showPreviousOnionSkin, showNextOnionSkin, loopOnionSkin, onionSkinOpacity);
            timelinePanel = new SpriteToolboxTimelinePanel(Session, canvasPanel);
            toolsPanel = new SpriteToolboxToolsPanel(
                Session,
                toolsPanelSettings,
                Repaint,
                RequestToolChange,
                lightPadController,
                lightPadActions);
            playbackCoordinator = new SpriteToolboxPlaybackCoordinator(Session, Repaint);
        }

        // Gestisce l operazione GetCheckerboardTexture nel workflow della finestra Sprite Toolbox.
        Texture2D ICanvasRenderSource.GetCheckerboardTexture()
        {
            return canvasRenderCache.GetCheckerboardTexture();
        }

        // Gestisce l operazione GetCompositeTexture nel workflow della finestra Sprite Toolbox.
        Texture2D ICanvasRenderSource.GetCompositeTexture()
        {
            bool isRecolorPreviewActive = Session.ActiveTool == SpriteToolboxTool.Recolor && toolsPanel.IsRecolorPreviewEnabled;
            IReadOnlyList<SpriteRecolorMapping> recolorMappings = isRecolorPreviewActive
                ? toolsPanel.RecolorMappings
                : null;
            int recolorPreviewVersion = isRecolorPreviewActive
                ? toolsPanel.RecolorPreviewVersion
                : -1;
            SpriteCanvasColorMap recolorSourceMap = isRecolorPreviewActive ? toolsPanel.RecolorSourceMap : null;
            return canvasRenderCache.GetCompositeTexture(Document, Session.SelectedFrameIndex, recolorSourceMap, recolorMappings, recolorPreviewVersion);
        }

        // Gestisce l operazione DrawOnionSkin nel workflow della finestra Sprite Toolbox.
        void ICanvasRenderSource.DrawOnionSkin(Rect canvasRect, bool showPrevious, bool showNext, bool loop, float opacity)
        {
            canvasRenderCache.DrawOnionSkin(canvasRect, Document, Session.SelectedFrameIndex, showPrevious, showNext, loop, opacity);
        }

        // Gestisce l operazione DrawLightPadBackground nel workflow della finestra Sprite Toolbox.
        void ICanvasRenderSource.DrawLightPadBackground(Rect canvasRect, Texture referenceTexture, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom)
        {
            canvasRenderCache.DrawLightPadBackground(canvasRect, referenceTexture, alignment, referenceZoom, positionOffset, canvasZoom);
        }

        // Gestisce l operazione DrawLightPadCanvasOverlay nel workflow della finestra Sprite Toolbox.
        void ICanvasRenderSource.DrawLightPadCanvasOverlay(Rect canvasRect, Texture referenceTexture, float opacity, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom)
        {
            canvasRenderCache.DrawLightPadCanvasOverlay(canvasRect, referenceTexture, opacity, alignment, referenceZoom, positionOffset, canvasZoom);
        }

        // Rilascia risorse temporanee e annulla tutte le subscription editor-only.
        private void OnDisable()
        {
            playbackCoordinator?.Dispose();
            palettePresetRepository?.Dispose();
            UnsubscribeFromSession();
            toolsPanel?.Dispose();
            importExportService?.CancelActiveImport();
            importExportService?.RestoreTemporaryImportSettings();
            canvasController?.Dispose();
            lightPadController?.Dispose();
            canvasRenderCache?.Dispose();
        }

        // Protegge le modifiche non salvate durante la distruzione della finestra.
        private void OnDestroy()
        {
            if (closeRequestConfirmed || !hasUnsavedDocumentChanges)
            {
                return;
            }

            TryResolveUnsavedChanges();
        }

        // Gestisce l operazione RequestCloseWindow nel workflow della finestra Sprite Toolbox.
        private void RequestCloseWindow()
        {
            if (!TryResolveUnsavedChanges())
            {
                return;
            }

            closeRequestConfirmed = true;
            Close();
        }

        // Mostra il dialogo di salvataggio e restituisce false soltanto se il salvataggio esplicitamente richiesto non riesce.
        private bool TryResolveUnsavedChanges()
        {
            if (!hasUnsavedDocumentChanges)
            {
                return true;
            }

            bool saveRequested = EditorUtility.DisplayDialog(
                "Close Sprite Toolbox",
                "Save unsaved changes before closing?",
                "Save",
                "Don't Save");

            return !saveRequested || TrySaveCurrentDocument();
        }

        // Gestisce l operazione RestoreLastDocument nel workflow della finestra Sprite Toolbox.
        private void RestoreLastDocument()
        {
            SpriteDocumentAsset lastDocument = persistenceService.RestoreLastDocument();
            if (lastDocument == null) return;
            SetDocumentAsset(lastDocument);
            SetDocumentDisplayName(lastDocument.name);
            SetUnsavedChanges(false);
        }

        // Gestisce l operazione SetDocumentDisplayName nel workflow della finestra Sprite Toolbox.
        private void SetDocumentDisplayName(string value)
        {
            documentDisplayName = string.IsNullOrEmpty(value) ? "Sprite Toolbox" : value;
            UpdateWindowTitle();
        }

        // Gestisce l operazione SetUnsavedChanges nel workflow della finestra Sprite Toolbox.
        private void SetUnsavedChanges(bool value)
        {
            hasUnsavedDocumentChanges = value;
            UpdateWindowTitle();
        }

        // Gestisce l operazione UpdateWindowTitle nel workflow della finestra Sprite Toolbox.
        private void UpdateWindowTitle()
        {
            string suffix = hasUnsavedDocumentChanges ? " *" : string.Empty;
            titleContent = new GUIContent("Sprite ToolBox: "+documentDisplayName + suffix, SpriteToolboxEditorIcons.Get("Logo"));
        }

        // Disegna l interfaccia corrente e coordina input, layout e repaint IMGUI.
        private void OnGUI()
        {
            PrepareGuiFrame();
            DrawToolbar();
            PanelLayout layout = CalculatePanelLayout();

            DrawPanel(layout.Left, palettePanel, themePalette.PalettePanelBackground, themePalette.PaletteAccent);
            DrawPanel(layout.Canvas, canvasPanel, themePalette.CanvasPanelBackground, themePalette.CanvasAccent);
            PersistCanvasPanelPreferences();
            DrawPanel(layout.Tools, toolsPanel, themePalette.ToolsPanelBackground, themePalette.ToolsAccent);
            DrawPanel(layout.Timeline, timelinePanel, themePalette.TimelinePanelBackground, themePalette.TimelineAccent);
        }

        private void PrepareGuiFrame()
        {
            EnsureDocument();
            EnsureSession();
            ClampSelection();
            ClearTextFieldFocusOnMouseDown();
            HandleShortcuts();
        }

        private PanelLayout CalculatePanelLayout()
        {
            Rect workspaceRect = new Rect(0f, ToolbarHeight, position.width, position.height - ToolbarHeight);
            UpdatePanelResizing(workspaceRect);
            float canvasHeight = Mathf.Max(40f, workspaceRect.height - timelineHeight - SplitterSize);
            Rect leftRect = new Rect(workspaceRect.x, workspaceRect.y, leftPanelWidth, canvasHeight);
            Rect canvasRect = new Rect(leftRect.xMax + SplitterSize, workspaceRect.y, workspaceRect.width - leftPanelWidth - rightPanelWidth - SplitterSize * 2f, canvasHeight);
            Rect toolsRect = new Rect(canvasRect.xMax + SplitterSize, workspaceRect.y, rightPanelWidth, canvasHeight);
            Rect timelineRect = new Rect(workspaceRect.x, canvasRect.yMax + SplitterSize, workspaceRect.width, workspaceRect.height - canvasHeight - SplitterSize);
            return new PanelLayout(leftRect, canvasRect, toolsRect, timelineRect);
        }


        // Gestisce l operazione DrawToolbar nel workflow della finestra Sprite Toolbox.
        private void DrawToolbar()
        {
            Rect toolbarRect = new Rect(0f, 0f, position.width, ToolbarHeight);
            EditorGUI.DrawRect(toolbarRect, themePalette.ToolbarBackground);
            EditorGUI.DrawRect(new Rect(toolbarRect.x, toolbarRect.yMax - 2f, toolbarRect.width, 2f), themePalette.CanvasAccent);
            GUILayout.BeginArea(toolbarRect);
            float menuX = 2f;
            DrawMenuButton("File", ref menuX, ShowFileMenu);
            DrawMenuButton("Edit", ref menuX, ShowEditMenu);
            DrawMenuButton("Sprite", ref menuX, ShowSpriteMenu);
            DrawMenuButton("Layer", ref menuX, ShowLayerMenu);
            DrawMenuButton("Frame", ref menuX, ShowFrameMenu);
            DrawMenuButton("Select", ref menuX, ShowSelectMenu);
            DrawMenuButton("View", ref menuX, ShowViewMenu);
            DrawMenuButton("Help", ref menuX, ShowHelpMenu);
            GUI.Label(new Rect(menuX + 4f, 2f, 120f, ToolbarHeight - 2f), $"{Document.Width} × {Document.Height}", EditorStyles.miniLabel);
            GUILayout.EndArea();
        }

        // Gestisce l operazione DrawPanelBackground nel workflow della finestra Sprite Toolbox.
        private void DrawPanelBackground(Rect rect, Color backgroundColor, Color accentColor)
        {
            EditorGUI.DrawRect(rect, backgroundColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 2f), accentColor);
            Color accentBorderColor = accentColor;
            accentBorderColor.a = themePalette.PanelAccentBorderOpacity;
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), accentBorderColor);
        }

        // Gestisce l operazione DrawPanel nel workflow della finestra Sprite Toolbox.
        private void DrawPanel(Rect rect, ISpriteToolboxPanel panel, Color background, Color accent)
        {
            DrawPanelBackground(rect, background, accent);
            panel.Draw(rect);
        }

        // Gestisce l operazione DrawAccentLabel nel workflow della finestra Sprite Toolbox.
        private static void DrawAccentLabel(string label, Color color, GUIStyle style)
        {
            Color previousContentColor = GUI.contentColor;
            GUI.contentColor = color;
            GUILayout.Label(label, style);
            GUI.contentColor = previousContentColor;
        }

        // Gestisce l operazione DrawMenuButton nel workflow della finestra Sprite Toolbox.
        private void DrawMenuButton(string label, ref float x, System.Action<Rect> menuAction)
        {
            GUIContent content = new GUIContent($"{label} ▾");
            float buttonWidth = EditorStyles.toolbarButton.CalcSize(content).x + 4f;
            Rect buttonRect = new Rect(x, 1f, buttonWidth, ToolbarHeight - 2f);
            if (EditorGUI.DropdownButton(buttonRect, content, FocusType.Keyboard, EditorStyles.toolbarButton))
            {
                menuAction(buttonRect);
            }

            x += buttonWidth;
        }

        // Gestisce l operazione ShowFileMenu nel workflow della finestra Sprite Toolbox.
        private void ShowFileMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New…\tCtrl+N"), false, ShowNewDocumentDialog);
            menu.AddItem(new GUIContent("Open…\tCtrl+O"), false, OpenFile);
            AddRecentFilesMenu(menu);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Save Project"), false, SaveDocument);
            menu.AddItem(new GUIContent("Save Project As…"), false, SaveDocumentAs);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Close\tCtrl+W"), false, RequestCloseWindow);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Export…"), false, ShowExportDialog);
            menu.AddItem(new GUIContent("Quick Export/Frame PNG"), false, ExportFramePng);
            menu.AddItem(new GUIContent("Quick Export/All Frames PNG"), false, ExportAllFramesPng);
            menu.AddItem(new GUIContent("Quick Export/Sprite Sheet"), false, ExportSpriteSheet);
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Exit\tCtrl+Q"), false, RequestCloseWindow);
            menu.DropDown(anchor);
        }

        // Gestisce l operazione AddRecentFilesMenu nel workflow della finestra Sprite Toolbox.
        private void AddRecentFilesMenu(GenericMenu menu)
        {
            if (recentFilePaths.Count == 0)
            {
                menu.AddDisabledItem(new GUIContent("Open Recent/No recent files"));
            }
            else
            {
                foreach (string recentFilePath in recentFilePaths)
                {
                    string path = recentFilePath;
                    menu.AddItem(new GUIContent($"Open Recent/{Path.GetFileName(path)}"), false, () => OpenPath(path));
                }
            }

            menu.AddSeparator("Open Recent/");
            menu.AddItem(new GUIContent("Open Recent/Clear Recent Files"), false, persistenceService.ClearRecent);
        }

        // Gestisce l operazione ShowEditMenu nel workflow della finestra Sprite Toolbox.
        private void ShowEditMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Undo"), false, Undo);
            menu.AddItem(new GUIContent("Redo"), false, Redo);
            menu.AddSeparator(string.Empty);
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Copy"), false, () => Session.CopySelection());
            else menu.AddDisabledItem(new GUIContent("Copy"));
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Cut"), false, () => Session.CutSelection());
            else menu.AddDisabledItem(new GUIContent("Cut"));
            if (Session.HasSelectionClipboard) menu.AddItem(new GUIContent("Paste"), false, () => Session.PasteSelection());
            else menu.AddDisabledItem(new GUIContent("Paste"));
            menu.DropDown(anchor);
        }

        // Gestisce l operazione ShowSpriteMenu nel workflow della finestra Sprite Toolbox.
        private void ShowSpriteMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Resize Canvas…"), false, ShowResizeCanvasDialog);
            menu.AddItem(new GUIContent("Flip Horizontal"), false, () => FlipCanvas(true));
            menu.AddItem(new GUIContent("Flip Vertical"), false, () => FlipCanvas(false));
            menu.DropDown(anchor);
        }

        // Gestisce l operazione ShowLayerMenu nel workflow della finestra Sprite Toolbox.
        private void ShowLayerMenu(Rect anchor)
        {
            CreateLayerMenu().DropDown(anchor);
        }

        // Gestisce l operazione CreateLayerMenu nel workflow della finestra Sprite Toolbox.
        private GenericMenu CreateLayerMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New Layer"), false, AddLayer);
            if (Document.LayerTracks.Count > 1 && !Document.GetLayerTrack(Session.SelectedLayerIndex).IsLocked) menu.AddItem(new GUIContent("Delete Layer"), false, DeleteSelectedLayer);
            else menu.AddDisabledItem(new GUIContent("Delete Layer"));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Move Up"), false, () => MoveSelectedLayer(1));
            menu.AddItem(new GUIContent("Move Down"), false, () => MoveSelectedLayer(-1));
            if (Session.SelectedLayerIndex > 0) menu.AddItem(new GUIContent("Merge Down"), false, MergeSelectedLayerDown);
            else menu.AddDisabledItem(new GUIContent("Merge Down"));
            return menu;
        }

        // Gestisce l operazione ShowFrameMenu nel workflow della finestra Sprite Toolbox.
        private void ShowFrameMenu(Rect anchor)
        {
            CreateFrameMenu().DropDown(anchor);
        }

        // Gestisce l operazione CreateFrameMenu nel workflow della finestra Sprite Toolbox.
        private GenericMenu CreateFrameMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New Frame"), false, () => AddFrame(false));
            menu.AddItem(new GUIContent("Duplicate Frame"), false, () => AddFrame(true));
            if (Document.Frames.Count > 1) menu.AddItem(new GUIContent("Delete Frame"), false, DeleteSelectedFrame);
            else menu.AddDisabledItem(new GUIContent("Delete Frame"));
            return menu;
        }

        // Gestisce l operazione ShowSelectMenu nel workflow della finestra Sprite Toolbox.
        private void ShowSelectMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Select All"), false, () => { Session.BeginSelectionUndo(); Session.SetPixelSelectionRect(new RectInt(0, 0, Document.Width, Document.Height)); Session.SetPixelSelectionActive(true); });
            menu.AddItem(new GUIContent("Invert Selection\tCtrl+I"), false, () => Session.InvertPixelSelection());
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Fill Selection"), false, () => Session.FillPixelSelection(Session.PrimaryColor));
            else menu.AddDisabledItem(new GUIContent("Fill Selection"));
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Flip Selection/Horizontal"), false, () => Session.FlipPixelSelection(true));
            else menu.AddDisabledItem(new GUIContent("Flip Selection/Horizontal"));
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Flip Selection/Vertical"), false, () => Session.FlipPixelSelection(false));
            else menu.AddDisabledItem(new GUIContent("Flip Selection/Vertical"));
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Delete Selected Area"), false, () => Session.ClearPixelSelectionPixels());
            else menu.AddDisabledItem(new GUIContent("Delete Selected Area"));
            if (Session.HasPixelSelection) menu.AddItem(new GUIContent("Clear Selection"), false, Session.ClearPixelSelection);
            else menu.AddDisabledItem(new GUIContent("Clear Selection"));
            menu.DropDown(anchor);
        }

        // Gestisce l operazione ShowViewMenu nel workflow della finestra Sprite Toolbox.
        private void ShowViewMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Pixel Grid"), canvasPanel.ShowPixelGrid, () => canvasPanel.ShowPixelGrid = !canvasPanel.ShowPixelGrid);
            menu.AddItem(new GUIContent("Onion Skin/Previous Frame"), canvasPanel.ShowPreviousOnionSkin, () => canvasPanel.ShowPreviousOnionSkin = !canvasPanel.ShowPreviousOnionSkin);
            menu.AddItem(new GUIContent("Onion Skin/Next Frame"), canvasPanel.ShowNextOnionSkin, () => canvasPanel.ShowNextOnionSkin = !canvasPanel.ShowNextOnionSkin);
            menu.AddItem(new GUIContent("Onion Skin/Loop"), canvasPanel.LoopOnionSkin, () => canvasPanel.LoopOnionSkin = !canvasPanel.LoopOnionSkin);
            menu.AddItem(new GUIContent("Zoom/Fit Canvas"), false, canvasPanel.RequestFitToViewport);
            menu.AddItem(new GUIContent("Zoom/Reset 1:1"), false, canvasPanel.ResetViewport);
            menu.DropDown(anchor);
        }

        // Gestisce l operazione ShowHelpMenu nel workflow della finestra Sprite Toolbox.
        private void ShowHelpMenu(Rect anchor)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Shortcuts"), false, () => EditorUtility.DisplayDialog("Sprite Toolbox shortcuts", "Ctrl+N New document\nCtrl+O Open document or PNG\nCtrl+T New tag\nCtrl+W Close Sprite Toolbox\nCtrl+Q Exit Sprite Toolbox\n\nB Pencil tool\nE Eraser tool / rotate active selection\nI Eyedropper tool\nL Line tool\nX Swap primary/secondary colors\nEsc Clear selection\nRight click Use secondary color\n\nCtrl+A Select all pixels and activate Select\nCtrl+I Invert selection\nCtrl+Z Undo\nCtrl+Y Redo\nCtrl+C Copy selection\nCtrl+X Cut selection\nCtrl+V Paste selection", "OK"));
            menu.DropDown(anchor);
        }

        private void PersistCanvasPanelPreferences()
        {
            // Persist canvas-owned preferences because they are serialized by the window, not by the panel instance.
            canvasZoom = canvasPanel.Zoom;
            showPixelGrid = canvasPanel.ShowPixelGrid;
            showPreviousOnionSkin = canvasPanel.ShowPreviousOnionSkin;
            showNextOnionSkin = canvasPanel.ShowNextOnionSkin;
            loopOnionSkin = canvasPanel.LoopOnionSkin;
            onionSkinOpacity = canvasPanel.OnionSkinOpacity;
        }
        // Gestisce l operazione UpdatePanelResizing nel workflow della finestra Sprite Toolbox.
        private void UpdatePanelResizing(Rect workspaceRect)
        {
            float minimumCanvasWidth = 160f;
            float maximumLeftWidth = workspaceRect.width - rightPanelWidth - minimumCanvasWidth - SplitterSize * 2f;
            float maximumRightWidth = workspaceRect.width - leftPanelWidth - minimumCanvasWidth - SplitterSize * 2f;
            float maximumTimelineHeight = workspaceRect.height - 120f;
            leftPanelWidth = Mathf.Clamp(leftPanelWidth, MinimumLeftPanelWidth, Mathf.Max(MinimumLeftPanelWidth, maximumLeftWidth));
            rightPanelWidth = Mathf.Clamp(rightPanelWidth, MinimumRightPanelWidth, Mathf.Max(MinimumRightPanelWidth, maximumRightWidth));
            timelineHeight = Mathf.Clamp(timelineHeight, MinimumTimelineHeight, Mathf.Max(MinimumTimelineHeight, maximumTimelineHeight));

            Rect leftSplitter = new Rect(workspaceRect.x + leftPanelWidth, workspaceRect.y, SplitterSize, workspaceRect.height - timelineHeight - SplitterSize);
            Rect rightSplitter = new Rect(workspaceRect.xMax - rightPanelWidth - SplitterSize, workspaceRect.y, SplitterSize, workspaceRect.height - timelineHeight - SplitterSize);
            Rect timelineSplitter = new Rect(workspaceRect.x, workspaceRect.yMax - timelineHeight - SplitterSize, workspaceRect.width, SplitterSize);
            EditorGUIUtility.AddCursorRect(leftSplitter, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(rightSplitter, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(timelineSplitter, MouseCursor.ResizeVertical);
            Event currentEvent = Event.current;

            if (currentEvent.type == EventType.MouseDown)
            {
                if (leftSplitter.Contains(currentEvent.mousePosition)) resizeTarget = ResizeTarget.LeftPanel;
                else if (rightSplitter.Contains(currentEvent.mousePosition)) resizeTarget = ResizeTarget.RightPanel;
                else if (timelineSplitter.Contains(currentEvent.mousePosition)) resizeTarget = ResizeTarget.Timeline;
            }

            if (currentEvent.type == EventType.MouseDrag)
            {
                if (resizeTarget == ResizeTarget.LeftPanel) leftPanelWidth = currentEvent.mousePosition.x - workspaceRect.x;
                else if (resizeTarget == ResizeTarget.RightPanel) rightPanelWidth = workspaceRect.xMax - currentEvent.mousePosition.x;
                else if (resizeTarget == ResizeTarget.Timeline) timelineHeight = workspaceRect.yMax - currentEvent.mousePosition.y;
                else return;

                currentEvent.Use();
                Repaint();
            }

            if (currentEvent.type == EventType.MouseUp)
            {
                resizeTarget = ResizeTarget.None;
            }
        }

        // Gestisce l operazione Undo nel workflow della finestra Sprite Toolbox.
        private void Undo()
        {
            if (Session.UndoSelection())
            {
                return;
            }

            if (!Session.CommandHistory.CanUndo)
            {
                return;
            }

            Session.Undo();
            Repaint();
        }

        // Gestisce l operazione Redo nel workflow della finestra Sprite Toolbox.
        private void Redo()
        {
            if (!Session.CommandHistory.CanRedo)
            {
                return;
            }

            Session.Redo();
            Repaint();
        }

        // Gestisce l operazione MoveSelectedLayer nel workflow della finestra Sprite Toolbox.
        private void MoveSelectedLayer(int direction)
        {
            Session.MoveSelectedLayer(direction);
        }

        // Gestisce l operazione AddLayer nel workflow della finestra Sprite Toolbox.
        private void AddLayer()
        {
            Session.AddLayer($"Layer {Document.LayerTracks.Count + 1}");
        }

        // Gestisce l operazione DeleteSelectedLayer nel workflow della finestra Sprite Toolbox.
        private void DeleteSelectedLayer()
        {
            DeleteLayerAt(Session.SelectedLayerIndex);
        }

        // Gestisce l operazione DeleteLayerAt nel workflow della finestra Sprite Toolbox.
        private void DeleteLayerAt(int layerIndex)
        {
            Session.RemoveLayer(layerIndex);
        }

        // Gestisce l operazione MergeSelectedLayerDown nel workflow della finestra Sprite Toolbox.
        private void MergeSelectedLayerDown()
        {
            Session.MergeSelectedLayerDown();
        }

        // Gestisce l operazione AddFrame nel workflow della finestra Sprite Toolbox.
        private void AddFrame(bool duplicateCurrentFrame)
        {
            Session.AddFrameAfterSelection(duplicateCurrentFrame);
        }

        // Gestisce l operazione DeleteSelectedFrame nel workflow della finestra Sprite Toolbox.
        private void DeleteSelectedFrame()
        {
            DeleteFrameAt(Session.SelectedFrameIndex);
        }

        // Gestisce l operazione DeleteFrameAt nel workflow della finestra Sprite Toolbox.
        private void DeleteFrameAt(int frameIndex)
        {
            if (frameIndex < 0 || frameIndex >= Document.Frames.Count)
            {
                return;
            }

            Session.SelectFrame(frameIndex);
            Session.RemoveSelectedFrame();
        }

        // Gestisce l operazione SaveDocument nel workflow della finestra Sprite Toolbox.
        private void SaveDocument()
        {
            if (documentAsset == null) { TrySaveDocumentAs(); return; }
            documentAsset.ReplaceDocument(Document.Clone());
            persistenceService.Save(documentAsset);
            SetUnsavedChanges(false);
        }

        // Gestisce l operazione TrySaveCurrentDocument nel workflow della finestra Sprite Toolbox.
        private bool TrySaveCurrentDocument()
        {
            if (documentAsset == null)
            {
                return TrySaveDocumentAs();
            }

            SaveDocument();
            return true;
        }

        // Gestisce l operazione SaveDocumentAs nel workflow della finestra Sprite Toolbox.
        private void SaveDocumentAs()
        {
            TrySaveDocumentAs();
        }

        // Gestisce l operazione TrySaveDocumentAs nel workflow della finestra Sprite Toolbox.
        private bool TrySaveDocumentAs()
        {
            SpriteDocumentAsset newAsset = persistenceService.SaveAs(Document);
            if (newAsset == null) return false;
            workingDocument = Document;
            documentAsset = newAsset;
            transientDocument = null;
            SetDocumentDisplayName(newAsset.name);
            SetUnsavedChanges(false);
            return true;
        }

        // Gestisce l operazione ShowNewDocumentDialog nel workflow della finestra Sprite Toolbox.
        private void ShowNewDocumentDialog()
        {
            NewDocumentDialog.Show(this);
        }

        // Gestisce l operazione ShowResizeCanvasDialog nel workflow della finestra Sprite Toolbox.
        private void ShowResizeCanvasDialog()
        {
            ResizeCanvasDialog.Show(this, Document.Width, Document.Height);
        }

        // Gestisce l operazione ResizeCanvas nel workflow della finestra Sprite Toolbox.
        private void ResizeCanvas(int width, int height)
        {
            Session.ResizeCanvas(width, height);
        }

        // Gestisce l operazione FlipCanvas nel workflow della finestra Sprite Toolbox.
        private void FlipCanvas(bool horizontally)
        {
            Session.FlipCanvas(horizontally);
        }

        // Gestisce l operazione CreateNewDocument nel workflow della finestra Sprite Toolbox.
        private void CreateNewDocument(int width, int height)
        {
            importExportService.RestoreTemporaryImportSettings();
            CreateTransientDocument(width, height);
            SetDocumentDisplayName("Untitled Sprite");
            SetUnsavedChanges(false);
            Repaint();
        }

        // Gestisce l operazione OpenFile nel workflow della finestra Sprite Toolbox.
        private void OpenFile()
        {
            string path = EditorUtility.OpenFilePanelWithFilters(
                "Open Sprite Toolbox file",
                GetLastOpenedFileDirectory(),
                new[] { "All supported formats", "asset,png", "Sprite Toolbox Document", "asset", "PNG image", "png" });
            if (!string.IsNullOrEmpty(path))
            {
                OpenPath(path);
            }
        }

        // Gestisce l operazione GetLastOpenedFileDirectory nel workflow della finestra Sprite Toolbox.
        private string GetLastOpenedFileDirectory()
        {
            string lastOpenedFileDirectory = SessionState.GetString(LastOpenedFileDirectorySessionKey, string.Empty);
            return !string.IsNullOrEmpty(lastOpenedFileDirectory) && Directory.Exists(lastOpenedFileDirectory)
                ? lastOpenedFileDirectory
                : Application.dataPath;
        }

        // Gestisce l operazione RememberOpenedFileDirectory nel workflow della finestra Sprite Toolbox.
        private static void RememberOpenedFileDirectory(string path)
        {
            string absolutePath = ToAbsolutePath(path);
            string directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                SessionState.SetString(LastOpenedFileDirectorySessionKey, directory);
            }
        }

        // Gestisce l operazione ToAbsolutePath nel workflow della finestra Sprite Toolbox.
        private static string ToAbsolutePath(string path)
        {
            if (Path.IsPathRooted(path))
            {
                return path;
            }

            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectPath, path);
        }

        // Gestisce l operazione OpenPath nel workflow della finestra Sprite Toolbox.
        private void OpenPath(string path)
        {
            if (!File.Exists(path) && !path.StartsWith("Assets/"))
            {
                RemoveRecentFile(path);
                return;
            }

            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (extension == ".png")
            {
                OpenPng(path);
                return;
            }

            if (extension == ".asset")
            {
                string assetPath;
                SpriteDocumentAsset asset = persistenceService.LoadAsset(path, out assetPath);
                if (asset != null)
                {
                    RememberOpenedFileDirectory(path);
                    SetDocumentAsset(asset);
                    EnsureDocument();
                    SetDocumentDisplayName(asset.name);
                    SetUnsavedChanges(false);
                    Repaint();
                    return;
                }
            }

            EditorUtility.DisplayDialog("Unsupported file", "Open supports Sprite Toolbox document assets and PNG images.", "OK");
        }

        // Gestisce l operazione OpenPng nel workflow della finestra Sprite Toolbox.
        private void OpenPng(string path)
        {
            importExportService.BeginImportPng(path, CompletePngImport, ShowPngImportError);
        }

        // Gestisce l operazione CompletePngImport nel workflow della finestra Sprite Toolbox.
        private void CompletePngImport(SpriteDocument importedDocument, string displayName, string recentPath)
        {
            RememberOpenedFileDirectory(recentPath);
            documentAsset = null;
            workingDocument = null;
            transientDocument = importedDocument;
            EnsureSession();
            Session.SelectCell(0, 0);
            persistenceService.AddRecent(recentPath);
            SetDocumentDisplayName(displayName);
            SetUnsavedChanges(false);
            Repaint();
        }

        // Gestisce l operazione ShowPngImportError nel workflow della finestra Sprite Toolbox.
        private static void ShowPngImportError(string message)
        {
            EditorUtility.DisplayDialog("Unable to import PNG", message, "OK");
        }

        // Gestisce l operazione RemoveRecentFile nel workflow della finestra Sprite Toolbox.
        private void RemoveRecentFile(string path)
        {
            persistenceService.RemoveRecent(path);
        }

        // Gestisce l operazione ExportFramePng nel workflow della finestra Sprite Toolbox.
        private void ExportFramePng()
        {
            importExportService.ExportFrame(Document, Session.SelectedFrameIndex);
        }

        // Gestisce l operazione ExportAllFramesPng nel workflow della finestra Sprite Toolbox.
        private void ExportAllFramesPng()
        {
            importExportService.ExportAllFrames(Document);
        }

        // Gestisce l operazione ExportSpriteSheet nel workflow della finestra Sprite Toolbox.
        private void ExportSpriteSheet()
        {
            ExportSpriteSheet(true);
        }

        // Gestisce l operazione ExportSpriteSheet nel workflow della finestra Sprite Toolbox.
        private void ExportSpriteSheet(bool exportAnimationClip)
        {
            int columns = timelinePanel == null ? 4 : timelinePanel.SheetColumns;
            importExportService.ExportSpriteSheet(Document, columns, exportAnimationClip);
        }

        // Gestisce l operazione ShowExportDialog nel workflow della finestra Sprite Toolbox.
        private void ShowExportDialog()
        {
            ExportDialog.Show(this);
        }

        // Gestisce l operazione ExportSelectedOutputs nel workflow della finestra Sprite Toolbox.
        private void ExportSelectedOutputs(bool exportProject, bool exportFrame, bool exportAllFrames, bool exportSpriteSheet, bool exportAnimationClip)
        {
            if (exportProject)
            {
                persistenceService.ExportCopy(Document);
            }

            if (exportFrame)
            {
                ExportFramePng();
            }

            if (exportAllFrames)
            {
                ExportAllFramesPng();
            }

            if (exportSpriteSheet)
            {
                ExportSpriteSheet(exportAnimationClip);
            }
        }

        // Interpreta le shortcut globali senza interferire con i campi di testo.
        private void HandleShortcuts()
        {
            Event currentEvent = Event.current;
            if (currentEvent.type != EventType.KeyDown) return;
            if (focusedWindow != this) return;
            if (EditorGUIUtility.editingTextField) return;
            if (IsLightPadSceneViewNavigationKey(currentEvent)) return;
            if (TryHandleUndoRedoShortcut(currentEvent)) return;
            if (Session.HasFloatingSelection)
            {
                if (currentEvent.keyCode == KeyCode.Return || currentEvent.keyCode == KeyCode.KeypadEnter)
                {
                    Session.CommitFloatingSelection();
                    Repaint();
                    currentEvent.Use();
                }
                else if (currentEvent.keyCode == KeyCode.Escape)
                {
                    Session.CancelFloatingSelection();
                    Repaint();
                    currentEvent.Use();
                }
                else
                {
                    Vector2Int nudge;
                    if (currentEvent.keyCode == KeyCode.E && Session.ActiveTool == SpriteToolboxTool.Select)
                    {
                        Session.SetFloatingSelectionRotationMode(true);
                        Repaint();
                        currentEvent.Use();
                    }
                    else if (currentEvent.keyCode == KeyCode.W)
                    {
                        Session.SetFloatingSelectionRotationMode(false);
                        Session.SetFloatingSelectionScaleMode(false);
                        Session.SetFloatingSelectionTransformMode(false);
                        Repaint();
                        currentEvent.Use();
                    }
                    else if (currentEvent.keyCode == KeyCode.R)
                    {
                        Session.SetFloatingSelectionScaleMode(true);
                        Repaint();
                        currentEvent.Use();
                    }
                    else if (currentEvent.keyCode == KeyCode.T)
                    {
                        Session.SetFloatingSelectionTransformMode(true);
                        Repaint();
                        currentEvent.Use();
                    }
                    else if (TryGetFloatingSelectionNudge(currentEvent.keyCode, out nudge) && Session.NudgeFloatingSelection(nudge))
                    {
                        Repaint();
                        currentEvent.Use();
                    }
                }

                return;
            }
            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.W && Session.HasPixelSelection)
            {
                if (!Session.HasFloatingSelection) Session.BeginFloatingMoveSelection();
                Session.SetFloatingSelectionRotationMode(false);
                Session.SetFloatingSelectionScaleMode(false);
                Session.SetFloatingSelectionTransformMode(false);
                Repaint();
                currentEvent.Use();
                return;
            }
            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt
                && currentEvent.keyCode == KeyCode.E
                && Session.ActiveTool == SpriteToolboxTool.Select
                && Session.HasPixelSelection)
            {
                if (!Session.HasFloatingSelection) Session.BeginFloatingMoveSelection();
                Session.SetFloatingSelectionRotationMode(true);
                Repaint();
                currentEvent.Use();
                return;
            }
            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.R && Session.HasPixelSelection)
            {
                if (!Session.HasFloatingSelection) Session.BeginFloatingMoveSelection();
                Session.SetFloatingSelectionScaleMode(true);
                Repaint();
                currentEvent.Use();
                return;
            }
            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.T && Session.HasPixelSelection)
            {
                if (!Session.HasFloatingSelection) Session.BeginFloatingMoveSelection();
                Session.SetFloatingSelectionTransformMode(true);
                Repaint();
                currentEvent.Use();
                return;
            }
            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.B)
            {
                RequestToolChange(SpriteToolboxTool.Pencil);
                currentEvent.Use();
                return;
            }

            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.E)
            {
                RequestToolChange(SpriteToolboxTool.Eraser);
                currentEvent.Use();
                return;
            }

            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.G)
            {
                RequestToolChange(SpriteToolboxTool.Fill);
                currentEvent.Use();
                return;
            }

            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.I)
            {
                RequestToolChange(SpriteToolboxTool.Eyedropper);
                currentEvent.Use();
                return;
            }

            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.L)
            {
                RequestToolChange(SpriteToolboxTool.Line);
                currentEvent.Use();
                return;
            }

            if (!currentEvent.control && !currentEvent.command && !currentEvent.alt && currentEvent.keyCode == KeyCode.X)
            {
                Session.SwapColors();
                currentEvent.Use();
                return;
            }

            if (currentEvent.control || currentEvent.command)
            {
                if (currentEvent.keyCode == KeyCode.N) { ShowNewDocumentDialog(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.O) { OpenFile(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.W && !currentEvent.shift) { RequestCloseWindow(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.Q) { RequestCloseWindow(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.T && timelinePanel != null) { timelinePanel.RequestNewTagEditor(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.A) { RequestToolChange(SpriteToolboxTool.Select); Session.BeginSelectionUndo(); Session.SetPixelSelectionRect(new RectInt(0, 0, Document.Width, Document.Height)); Session.SetPixelSelectionActive(true); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.I) { RequestToolChange(SpriteToolboxTool.Select); Session.InvertPixelSelection(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.C) { Session.CopySelection(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.X && currentEvent.shift) { Session.CropCanvasToSelection(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.X) { Session.CutSelection(); currentEvent.Use(); }
                else if (currentEvent.keyCode == KeyCode.V) { Session.PasteSelection(); currentEvent.Use(); }
                return;
            }

            if (currentEvent.alt) return;
            if (currentEvent.keyCode == KeyCode.Escape)
            {
                Session.ClearPixelSelection();
                currentEvent.Use();
            }
        }

        // Gestisce l operazione TryHandleUndoRedoShortcut nel workflow della finestra Sprite Toolbox.
        private bool TryHandleUndoRedoShortcut(Event currentEvent)
        {
            if ((!currentEvent.control && !currentEvent.command) || currentEvent.alt)
            {
                return false;
            }

            if (currentEvent.keyCode == KeyCode.Z)
            {
                if (Session.CanUndoSelection || Session.CommandHistory.CanUndo) Undo();
                else notificationService.Show("Sprite Toolbox: nothing to undo.");
                currentEvent.Use();
                return true;
            }

            if (currentEvent.keyCode == KeyCode.Y)
            {
                if (Session.CommandHistory.CanRedo) Redo();
                else notificationService.Show("Sprite Toolbox: nothing to redo.");
                currentEvent.Use();
                return true;
            }

            return false;
        }

        // Gestisce l operazione TryGetFloatingSelectionNudge nel workflow della finestra Sprite Toolbox.
        private static bool TryGetFloatingSelectionNudge(KeyCode keyCode, out Vector2Int nudge)
        {
            nudge = Vector2Int.zero;
            if (keyCode == KeyCode.LeftArrow) nudge = Vector2Int.left;
            else if (keyCode == KeyCode.RightArrow) nudge = Vector2Int.right;
            else if (keyCode == KeyCode.UpArrow) nudge = Vector2Int.up;
            else if (keyCode == KeyCode.DownArrow) nudge = Vector2Int.down;
            return nudge != Vector2Int.zero;
        }

        // Gestisce l operazione IsLightPadSceneViewNavigationKey nel workflow della finestra Sprite Toolbox.
        private bool IsLightPadSceneViewNavigationKey(Event currentEvent)
        {
            if (!lightPadEditorState.IsSelected
                || lightPadController == null
                || lightPadController.Settings.SourceType != SpriteLightPadSourceType.SceneView
                || currentEvent.control
                || currentEvent.command
                || currentEvent.alt)
            {
                return false;
            }

            return currentEvent.keyCode == KeyCode.W
                || currentEvent.keyCode == KeyCode.A
                || currentEvent.keyCode == KeyCode.S
                || currentEvent.keyCode == KeyCode.D
                || currentEvent.keyCode == KeyCode.Q
                || currentEvent.keyCode == KeyCode.E;
        }

        // Gestisce l operazione ClearTextFieldFocusOnMouseDown nel workflow della finestra Sprite Toolbox.
        private static void ClearTextFieldFocusOnMouseDown()
        {
            if (Event.current.type != EventType.MouseDown || !EditorGUIUtility.editingTextField)
            {
                return;
            }

            GUI.FocusControl(null);
            EditorGUIUtility.editingTextField = false;
        }

        // Gestisce l operazione CreateTransientDocument nel workflow della finestra Sprite Toolbox.
        private void CreateTransientDocument(int width, int height)
        {
            documentAsset = null;
            workingDocument = null;
            transientDocument = new SpriteDocument(width, height);
            ApplyDefaultPalette(transientDocument);
            EnsureSession();
            Session.SelectCell(0, 0);
        }

        // Garantisce che la finestra disponga sempre di un documento attivo valido.
        private void EnsureDocument()
        {
            if (documentAsset != null)
            {
                if (documentAsset.Document == null)
                {
                    documentAsset.Initialize(32, 32);
                    ApplyDefaultPalette(documentAsset.Document);
                    EditorUtility.SetDirty(documentAsset);
                }

                if (workingDocument == null)
                {
                    workingDocument = documentAsset.Document.Clone();
                }

                return;
            }

            if (transientDocument == null)
            {
                transientDocument = new SpriteDocument(32, 32);
                ApplyDefaultPalette(transientDocument);
            }
        }

        // Sincronizza la sessione di editing con il documento attivo.
        private void EnsureSession()
        {
            if (session == null)
            {
                session = new SpriteToolboxSession();
            }

            SubscribeToSession();
            if (!object.ReferenceEquals(session.Document, ActiveDocument))
            {
                session.SetDocument(ActiveDocument);
                canvasDocumentWidth = session.Document.Width;
                canvasDocumentHeight = session.Document.Height;
                canvasPanel?.RequestFitToViewport();
            }
        }

        // Gestisce l operazione SubscribeToSession nel workflow della finestra Sprite Toolbox.
        private void SubscribeToSession()
        {
            if (sessionEventsSubscribed)
            {
                return;
            }

            session.DocumentChangedDetailed += OnSessionDocumentChanged;
            session.DocumentEdited += OnSessionDocumentEdited;
            session.SelectionChanged += Repaint;
            session.ToolSettingsChanged += Repaint;
            sessionEventsSubscribed = true;
        }

        // Gestisce l operazione UnsubscribeFromSession nel workflow della finestra Sprite Toolbox.
        private void UnsubscribeFromSession()
        {
            if (session == null || !sessionEventsSubscribed)
            {
                return;
            }

            session.DocumentChangedDetailed -= OnSessionDocumentChanged;
            session.DocumentEdited -= OnSessionDocumentEdited;
            session.SelectionChanged -= Repaint;
            session.ToolSettingsChanged -= Repaint;
            sessionEventsSubscribed = false;
        }

        // Aggiorna cache e visualizzazione in risposta a una modifica del documento.
        private void OnSessionDocumentChanged(SpriteToolboxDocumentChange change)
        {
            bool canvasDimensionsChanged = Document.Width != canvasDocumentWidth || Document.Height != canvasDocumentHeight;
            canvasDocumentWidth = Document.Width;
            canvasDocumentHeight = Document.Height;
            if (canvasDimensionsChanged)
            {
                canvasPanel?.RequestCenterInViewport();
            }

            if (Session.ActiveTool == SpriteToolboxTool.Recolor)
            {
                toolsPanel?.InvalidateRecolorSources();
            }

            if (change.Kind == SpriteToolboxDocumentChangeKind.Pixels)
            {
                if (change.FrameIndex == Session.SelectedFrameIndex)
                {
                    canvasRenderCache?.UpdateCompositeRegion(Document, change.FrameIndex, change.InvalidatedBounds);
                }

                canvasRenderCache?.InvalidateOnionSkinFrame(Document, change.FrameIndex);
            }
            else
            {
                canvasRenderCache?.InvalidateComposite();
                canvasRenderCache?.InvalidateOnionSkin();
            }

            Repaint();
        }

        // Cambia strumento preservando correttamente eventuali anteprime pendenti.
        private void RequestToolChange(SpriteToolboxTool requestedTool)
        {
            lightPadEditorState.IsSelected = false;
            if (requestedTool == Session.ActiveTool)
            {
                return;
            }

            if (Session.ActiveTool == SpriteToolboxTool.Recolor && toolsPanel != null && toolsPanel.HasPendingRecolorChanges)
            {
                bool applyRecolor = EditorUtility.DisplayDialog(
                    "Apply recolor?",
                    "Apply the pending recolor changes before switching tools?",
                    "Yes",
                    "No");
                if (applyRecolor)
                {
                    toolsPanel.ApplyPendingRecolor();
                }
                else
                {
                    toolsPanel.DiscardRecolorPreview();
                }
            }

            previousCanvasTool = requestedTool;
            Session.SetActiveTool(requestedTool);
        }

        // Gestisce l operazione ImportLightPadReferenceAsLayer nel workflow della finestra Sprite Toolbox.
        private void ImportLightPadReferenceAsLayer()
        {
            try
            {
                EditorUtility.DisplayProgressBar("Import LightPad Reference", "Capturing the reference inside the drawing area…", 0.1f);
                Color32[] capturedPixels = null;
                int captureWidth = 0;
                int captureHeight = 0;
                string captureError = null;
                if (canvasPanel == null
                    || !canvasPanel.TryCaptureLightPadReference(Document.Width, Document.Height, out capturedPixels, out captureWidth, out captureHeight, out captureError))
                {
                    notificationService.Show(captureError ?? "Unable to capture the LightPad reference.");
                    return;
                }

                EditorUtility.DisplayProgressBar("Import LightPad Reference", "Reducing the reference colors…", 0.35f);
                Color32[] reducedPixels = SpriteReferenceImageImportService.ReduceColors(capturedPixels, captureWidth, captureHeight, Session.Palette.Count);
                IReadOnlyList<Color> reducedColors = SpriteReferenceImageImportService.GetOpaqueColors(reducedPixels);

                EditorUtility.DisplayProgressBar("Import LightPad Reference", "Resizing for the drawing canvas…", 0.82f);
                Color32[] layerPixels = SpriteReferenceImageImportService.DownscaleToCanvas(reducedPixels, captureWidth, captureHeight, Document.Width, Document.Height);

                if (lightPadController.Settings.UseDithering && reducedColors.Count > 0)
                {
                    EditorUtility.DisplayProgressBar("Import LightPad Reference", "Applying dithering…", 0.9f);
                    layerPixels = SpriteReferenceImageImportService.DitherToColors(layerPixels, Document.Width, Document.Height, reducedColors);
                }

                EditorUtility.DisplayProgressBar("Import LightPad Reference", "Creating the reference layer…", 0.95f);
                if (Session.AddLayerWithPixels("LightPad Reference", layerPixels))
                {
                    notificationService.Show("Reference imported as a new layer.");
                }
                else
                {
                    notificationService.Show("Unable to create the reference layer.");
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                notificationService.Show("Reference import failed. See the Console for details.");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        // Gestisce l operazione SetLightPadSelected nel workflow della finestra Sprite Toolbox.
        private void SetLightPadSelected(bool isSelected)
        {
            if (isSelected)
            {
                if (Session.ActiveTool != SpriteToolboxTool.EditorOnly)
                {
                    previousCanvasTool = Session.ActiveTool;
                }

                lightPadEditorState.IsSelected = true;
                Session.SetActiveTool(SpriteToolboxTool.EditorOnly);
                lightPadController.Enable();
            }
            else
            {
                lightPadEditorState.IsSelected = false;
                if (Session.ActiveTool == SpriteToolboxTool.EditorOnly)
                {
                    Session.SetActiveTool(previousCanvasTool);
                }
            }

            Repaint();
        }

        // Gestisce l operazione OnSessionDocumentEdited nel workflow della finestra Sprite Toolbox.
        private void OnSessionDocumentEdited()
        {
            SetUnsavedChanges(true);
        }

        // Gestisce l operazione SetDocumentAsset nel workflow della finestra Sprite Toolbox.
        private void SetDocumentAsset(SpriteDocumentAsset asset)
        {
            documentAsset = asset;
            transientDocument = null;
            workingDocument = asset != null && asset.Document != null ? asset.Document.Clone() : null;
        }

        // Gestisce l operazione ApplyDefaultPalette nel workflow della finestra Sprite Toolbox.
        private void ApplyDefaultPalette(SpriteDocument targetDocument)
        {
            SpritePaletteAsset defaultPalette = AssetDatabase.LoadAssetAtPath<SpritePaletteAsset>(DefaultPalettePath);
            if (defaultPalette == null || defaultPalette.Colors.Count == 0)
            {
                return;
            }

            paletteSettings.PresetPalette = defaultPalette;
            targetDocument.SetPalette(defaultPalette.Colors);
        }

        // Gestisce l operazione ClampSelection nel workflow della finestra Sprite Toolbox.
        private void ClampSelection()
        {
            Session.ClampSelection();
        }

        private sealed class ExportDialog : EditorWindow
        {
            private SpriteToolboxWindow owner;
            private bool exportProject;
            private bool exportFrame;
            private bool exportAllFrames;
            private bool exportSpriteSheet;
            private bool exportAnimationClip;

            // Gestisce l operazione Show nel workflow della finestra Sprite Toolbox.
            public static void Show(SpriteToolboxWindow owner)
            {
                ExportDialog dialog = CreateInstance<ExportDialog>();
                dialog.owner = owner;
                dialog.titleContent = new GUIContent("Export");
                dialog.minSize = new Vector2(300f, 248f);
                dialog.maxSize = dialog.minSize;
                dialog.ShowModalUtility();
            }

            // Disegna l interfaccia corrente e coordina input, layout e repaint IMGUI.
            private void OnGUI()
            {
                GUILayout.Space(8f);
                GUILayout.Label("Choose the outputs to export", EditorStyles.boldLabel);
                GUILayout.Space(4f);
                exportProject = EditorGUILayout.ToggleLeft("Project (.asset)", exportProject);
                exportFrame = EditorGUILayout.ToggleLeft("Frame PNG", exportFrame);
                exportAllFrames = EditorGUILayout.ToggleLeft("All Frames PNG", exportAllFrames);
                exportSpriteSheet = EditorGUILayout.ToggleLeft("Sprite Sheet PNG", exportSpriteSheet);

                bool wasEnabled = GUI.enabled;
                GUI.enabled = exportSpriteSheet;
                exportAnimationClip = EditorGUILayout.ToggleLeft("Animation Clip (.anim)", exportAnimationClip);
                GUI.enabled = wasEnabled;
                if (!exportSpriteSheet)
                {
                    exportAnimationClip = false;
                }

                GUILayout.Label("Animation Clip requires Sprite Sheet PNG.", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(75f)))
                {
                    Close();
                }

                bool hasSelectedOutput = exportProject || exportFrame || exportAllFrames || exportSpriteSheet;
                GUI.enabled = hasSelectedOutput;
                if (GUILayout.Button("Export", GUILayout.Width(75f)))
                {
                    if (owner != null)
                    {
                        owner.ExportSelectedOutputs(exportProject, exportFrame, exportAllFrames, exportSpriteSheet, exportAnimationClip);
                    }

                    Close();
                }

                GUI.enabled = wasEnabled;
                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }

        private sealed class NewDocumentDialog : EditorWindow
        {
            private SpriteToolboxWindow owner;
            private int width = 32;
            private int height = 32;

            // Gestisce l operazione Show nel workflow della finestra Sprite Toolbox.
            public static void Show(SpriteToolboxWindow owner)
            {
                NewDocumentDialog dialog = CreateInstance<NewDocumentDialog>();
                dialog.owner = owner;
                dialog.titleContent = new GUIContent("New Sprite Document");
                dialog.minSize = new Vector2(250f, 120f);
                dialog.maxSize = dialog.minSize;
                dialog.ShowUtility();
            }

            // Disegna l interfaccia corrente e coordina input, layout e repaint IMGUI.
            private void OnGUI()
            {
                GUILayout.Space(8f);
                width = EditorGUILayout.IntField("Width", Mathf.Clamp(width, 1, 4096));
                height = EditorGUILayout.IntField("Height", Mathf.Clamp(height, 1, 4096));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(75f)))
                {
                    Close();
                }

                if (GUILayout.Button("Create", GUILayout.Width(75f)))
                {
                    if (owner != null)
                    {
                        owner.CreateNewDocument(Mathf.Clamp(width, 1, 4096), Mathf.Clamp(height, 1, 4096));
                    }

                    Close();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }

        private sealed class LightPadActions : ILightPadActions
        {
            private readonly SpriteToolboxLightPadController controller;
            private readonly Action<bool> setSelected;
            private readonly Action importReferenceAsLayer;

            // Gestisce l operazione LightPadActions nel workflow della finestra Sprite Toolbox.
            public LightPadActions(SpriteToolboxLightPadController controller, Action<bool> setSelected, Action importReferenceAsLayer)
            {
                this.controller = controller ?? throw new ArgumentNullException(nameof(controller));
                this.setSelected = setSelected ?? throw new ArgumentNullException(nameof(setSelected));
                this.importReferenceAsLayer = importReferenceAsLayer ?? throw new ArgumentNullException(nameof(importReferenceAsLayer));
            }

            // Gestisce l operazione SetSelected nel workflow della finestra Sprite Toolbox.
            public void SetSelected(bool value) { setSelected(value); }
            // Gestisce l operazione SetEnabled nel workflow della finestra Sprite Toolbox.
            public void SetEnabled(bool value) { controller.SetEnabled(value); }
            // Gestisce l operazione SetSourceType nel workflow della finestra Sprite Toolbox.
            public void SetSourceType(SpriteLightPadSourceType value) { controller.SetSourceType(value); }
            // Gestisce l operazione SetSourceTexture nel workflow della finestra Sprite Toolbox.
            public void SetSourceTexture(Texture value) { controller.SetSourceTexture(value); }
            // Gestisce l operazione SetSourceCamera nel workflow della finestra Sprite Toolbox.
            public void SetSourceCamera(Camera value) { controller.SetSourceCamera(value); }
            // Gestisce l operazione SetOpacity nel workflow della finestra Sprite Toolbox.
            public void SetOpacity(float value) { controller.SetOpacity(value); }
            // Gestisce l operazione SetAlignment nel workflow della finestra Sprite Toolbox.
            public void SetAlignment(SpriteLightPadAlignment value) { controller.SetAlignment(value); }
            // Gestisce l operazione SetReferenceZoom nel workflow della finestra Sprite Toolbox.
            public void SetReferenceZoom(float value) { controller.SetReferenceZoom(value); }
            // Gestisce l operazione SetSamplingMode nel workflow della finestra Sprite Toolbox.
            public void SetSamplingMode(SpriteLightPadSamplingMode value) { controller.SetSamplingMode(value); }
            // Gestisce l operazione SetDithering nel workflow della finestra Sprite Toolbox.
            public void SetDithering(bool value) { controller.SetDithering(value); }
            // Gestisce l operazione Refresh nel workflow della finestra Sprite Toolbox.
            public void Refresh() { controller.Refresh(); }
            // Gestisce l operazione ResetReferencePosition nel workflow della finestra Sprite Toolbox.
            public void ResetReferencePosition() { controller.ResetReferencePosition(); }
            // Gestisce l operazione ImportReferenceAsLayer nel workflow della finestra Sprite Toolbox.
            public void ImportReferenceAsLayer() { importReferenceAsLayer(); }
        }

        private sealed class ResizeCanvasDialog : EditorWindow
        {
            private SpriteToolboxWindow owner;
            private int width;
            private int height;

            // Gestisce l operazione Show nel workflow della finestra Sprite Toolbox.
            public static void Show(SpriteToolboxWindow owner, int width, int height)
            {
                ResizeCanvasDialog dialog = CreateInstance<ResizeCanvasDialog>();
                dialog.owner = owner;
                dialog.width = width;
                dialog.height = height;
                dialog.titleContent = new GUIContent("Resize Canvas");
                dialog.minSize = new Vector2(250f, 120f);
                dialog.maxSize = dialog.minSize;
                dialog.ShowUtility();
            }

            // Disegna l interfaccia corrente e coordina input, layout e repaint IMGUI.
            private void OnGUI()
            {
                GUILayout.Space(8f);
                GUILayout.Label("Pixels outside the new bounds are cropped. New pixels are transparent.", EditorStyles.wordWrappedLabel);
                width = EditorGUILayout.IntField("Width", width);
                height = EditorGUILayout.IntField("Height", height);
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(75f)))
                {
                    Close();
                }

                if (GUILayout.Button("Resize", GUILayout.Width(75f)))
                {
                    if (owner != null)
                    {
                        owner.ResizeCanvas(Mathf.Clamp(width, 1, 4096), Mathf.Clamp(height, 1, 4096));
                    }

                    Close();
                }

                EditorGUILayout.EndHorizontal();
                GUILayout.Space(8f);
            }
        }
    }
}
