using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal interface ICanvasRenderSource
    {
        Texture2D GetCheckerboardTexture();
        Texture2D GetCompositeTexture();
        void DrawLightPadBackground(Rect canvasRect, Texture referenceTexture, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom);
        void DrawLightPadCanvasOverlay(Rect canvasRect, Texture referenceTexture, float opacity, SpriteLightPadAlignment alignment, float referenceZoom, Vector2 positionOffset, float canvasZoom);
        void DrawOnionSkin(Rect canvasRect, bool showPrevious, bool showNext, bool loop, float opacity);
    }

    internal interface ICanvasInteractionHandler
    {
        bool IsMovingSelection { get; }
        bool UsesNativeCursor { get; }
        void DrawPreviewsAndHandleInput(Rect canvasRect, float zoom, bool isPointerInViewport);
    }

    public interface IOnionSkinSettings
    {
        bool ShowPreviousOnionSkin { get; set; }
        bool ShowNextOnionSkin { get; set; }
        bool LoopOnionSkin { get; set; }
    }

    public sealed class SpriteToolboxCanvasPanel : IOnionSkinSettings, ISpriteToolboxPanel
    {
        private const float PanPadding = 4096f;
        private const int MaximumGridPixelCount = 4096;
        private static readonly int PanControlHint = "SpriteToolboxCanvasPan".GetHashCode();
        private readonly ISpriteToolboxState state;
        private readonly IToolActions toolActions;
        private readonly ICanvasRenderSource renderSource;
        private readonly ICanvasInteractionHandler interactionHandler;
        private readonly SpriteToolboxLightPadController lightPadController;
        private readonly Action<ISpriteToolboxState, IToolActions> testDraw;
        private Vector2 scrollPosition;
        private bool isPanning;
        private Texture2D activeCursorTexture;
        private Vector2 activeCursorHotspot;
        private Vector2 panStartMouseScreenPosition;
        private Vector2 panStartScrollPosition;
        private bool centerViewportPending;
        private bool fitToViewportPending = true;

        public Vector2 ScrollPosition { get => scrollPosition; set => scrollPosition = value; }
        public float Zoom { get; private set; }
        public bool ShowPixelGrid { get; set; }
        public bool ShowPreviousOnionSkin { get; set; }
        public bool ShowNextOnionSkin { get; set; }
        public bool LoopOnionSkin { get; set; }
        public float OnionSkinOpacity { get; set; }

        internal SpriteToolboxCanvasPanel(ISpriteToolboxCanvasContext context, ICanvasRenderSource renderSource, ICanvasInteractionHandler interactionHandler, SpriteToolboxLightPadController lightPadController, float zoom, bool showPixelGrid, bool showPreviousOnionSkin, bool showNextOnionSkin, bool loopOnionSkin, float onionSkinOpacity)
        {
            this.state = context ?? throw new ArgumentNullException(nameof(context));
            this.toolActions = context;
            this.renderSource = renderSource ?? throw new ArgumentNullException(nameof(renderSource));
            this.interactionHandler = interactionHandler ?? throw new ArgumentNullException(nameof(interactionHandler));
            this.lightPadController = lightPadController ?? throw new ArgumentNullException(nameof(lightPadController));
            Zoom = Mathf.Clamp(zoom, 1f, 48f);
            ShowPixelGrid = showPixelGrid;
            ShowPreviousOnionSkin = showPreviousOnionSkin;
            ShowNextOnionSkin = showNextOnionSkin;
            LoopOnionSkin = loopOnionSkin;
            OnionSkinOpacity = Mathf.Clamp01(onionSkinOpacity);
            scrollPosition = new Vector2(PanPadding, PanPadding);
        }

        internal SpriteToolboxCanvasPanel(ISpriteToolboxState state, IToolActions toolActions, Action<ISpriteToolboxState, IToolActions> draw)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.toolActions = toolActions ?? throw new ArgumentNullException(nameof(toolActions));
            testDraw = draw ?? throw new ArgumentNullException(nameof(draw));
            lightPadController = null;
            Zoom = 1f;
        }

        public void Draw()
        {
            if (testDraw != null)
            {
                testDraw(state, toolActions);
                return;
            }

            DrawToolbar();
            Event currentEvent = Event.current;
            Rect viewportRect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (fitToViewportPending && currentEvent.type == EventType.Repaint)
            {
                FitToViewport(viewportRect);
                fitToViewportPending = false;
            }
            else if (centerViewportPending && currentEvent.type == EventType.Repaint)
            {
                CenterInViewport(viewportRect);
                centerViewportPending = false;
            }
            bool isPointerInViewport = viewportRect.Contains(currentEvent.mousePosition);
            int panControlId = GUIUtility.GetControlID(PanControlHint, FocusType.Passive, viewportRect);
            HandleNavigation(currentEvent, viewportRect, isPointerInViewport, panControlId);

            float canvasWidth = state.DocumentWidth * Zoom;
            float canvasHeight = state.DocumentHeight * Zoom;
            Rect contentRect = new Rect(0f, 0f, canvasWidth + PanPadding * 2f, canvasHeight + PanPadding * 2f);
            scrollPosition = GUI.BeginScrollView(viewportRect, scrollPosition, contentRect);
            Rect canvasRect = new Rect(contentRect.x + PanPadding, contentRect.y + PanPadding, canvasWidth, canvasHeight);
            if (lightPadController.Settings.IsEnabled && lightPadController.ReferenceTexture != null)
            {
                renderSource.DrawLightPadBackground(canvasRect, lightPadController.ReferenceTexture, lightPadController.Settings.Alignment, lightPadController.Settings.ReferenceZoom, lightPadController.Settings.PositionOffset, Zoom);
            }
            GUI.DrawTextureWithTexCoords(canvasRect, renderSource.GetCheckerboardTexture(), new Rect(0f, 0f, state.DocumentWidth * 0.5f, state.DocumentHeight * 0.5f), true);
            if (lightPadController.Settings.IsEnabled && lightPadController.ReferenceTexture != null)
            {
                renderSource.DrawLightPadCanvasOverlay(canvasRect, lightPadController.ReferenceTexture, lightPadController.Settings.Opacity, lightPadController.Settings.Alignment, lightPadController.Settings.ReferenceZoom, lightPadController.Settings.PositionOffset, Zoom);
            }
            renderSource.DrawOnionSkin(canvasRect, ShowPreviousOnionSkin, ShowNextOnionSkin, LoopOnionSkin, OnionSkinOpacity);
            GUI.DrawTexture(canvasRect, renderSource.GetCompositeTexture(), ScaleMode.StretchToFill, true);
            if (ShowPixelGrid && Zoom >= 4f && state.DocumentWidth * state.DocumentHeight <= MaximumGridPixelCount) DrawPixelGrid(canvasRect);
            interactionHandler.DrawPreviewsAndHandleInput(canvasRect, Zoom, isPointerInViewport);
            GUI.EndScrollView();
            UpdateCanvasCursor(viewportRect, isPointerInViewport);
        }

        public void Draw(Rect panelRect)
        {
            GUILayout.BeginArea(panelRect);
            Draw();
            GUILayout.EndArea();
        }

        internal bool TryCaptureLightPadReference(int documentWidth, int documentHeight, out Color32[] pixels, out int captureWidth, out int captureHeight, out string error)
        {
            pixels = null;
            captureWidth = 0;
            captureHeight = 0;
            error = null;
            if (lightPadController == null)
            {
                error = "The LightPad controller is unavailable.";
                return false;
            }

            return lightPadController.TryCaptureReferenceForDocument(documentWidth, documentHeight, out pixels, out captureWidth, out captureHeight, out error);
        }

        public void ResetViewport()
        {
            scrollPosition = new Vector2(PanPadding, PanPadding);
            Zoom = 1f;
        }

        public void RequestFitToViewport()
        {
            fitToViewportPending = true;
        }

        public void RequestCenterInViewport()
        {
            centerViewportPending = true;
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            ShowPixelGrid = GUILayout.Toggle(ShowPixelGrid, "Grid", EditorStyles.toolbarButton, GUILayout.Width(40f));
            if (GUILayout.Button("1:1", EditorStyles.toolbarButton, GUILayout.Width(32f))) Zoom = 1f;
            if (GUILayout.Button("Fit", EditorStyles.toolbarButton, GUILayout.Width(28f))) RequestFitToViewport();
            Zoom = GUILayout.HorizontalSlider(Zoom, 1f, 48f, GUILayout.Width(110f));
            GUILayout.Label($"{Zoom:0.##}x", EditorStyles.toolbarButton, GUILayout.Width(42f));
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void FitToViewport(Rect viewportRect)
        {
            if (state.DocumentWidth < 1 || state.DocumentHeight < 1 || viewportRect.width <= 0f || viewportRect.height <= 0f) return;
            const float ScrollbarAllowance = 16f;
            float horizontalZoom = (viewportRect.width - ScrollbarAllowance) / state.DocumentWidth;
            float verticalZoom = (viewportRect.height - ScrollbarAllowance) / state.DocumentHeight;
            Zoom = Mathf.Clamp(Mathf.Floor(Mathf.Min(horizontalZoom, verticalZoom)), 1f, 48f);
            float canvasWidth = state.DocumentWidth * Zoom;
            float canvasHeight = state.DocumentHeight * Zoom;
            scrollPosition = new Vector2(
                PanPadding + canvasWidth * 0.5f - viewportRect.width * 0.5f,
                PanPadding + canvasHeight * 0.5f - viewportRect.height * 0.5f);
        }

        private void CenterInViewport(Rect viewportRect)
        {
            if (viewportRect.width <= 0f || viewportRect.height <= 0f)
            {
                return;
            }

            float canvasWidth = state.DocumentWidth * Zoom;
            float canvasHeight = state.DocumentHeight * Zoom;
            scrollPosition = new Vector2(
                PanPadding + canvasWidth * 0.5f - viewportRect.width * 0.5f,
                PanPadding + canvasHeight * 0.5f - viewportRect.height * 0.5f);
        }

        private void UpdateCanvasCursor(Rect viewportRect, bool isPointerInViewport)
        {
            bool isInteractionActive = isPanning || interactionHandler.IsMovingSelection;
            bool shouldUseCustomCursor = isPointerInViewport || isInteractionActive;
            Texture2D requestedCursor = null;
            Vector2 hotspot = Vector2.zero;
            if (shouldUseCustomCursor && !interactionHandler.UsesNativeCursor)
            {
                string cursorName = isPanning ? "Pan" : (interactionHandler.IsMovingSelection ? "Move" : "ToolboxDefaultCursor");
                requestedCursor = SpriteToolboxEditorIcons.Get(cursorName);
                if (requestedCursor != null && (cursorName == "ToolboxDefaultCursor" || cursorName == "Move"))
                {
                    hotspot = new Vector2(requestedCursor.width * 0.5f, requestedCursor.height * 0.5f);
                }
            }

            if (Event.current.type != EventType.Layout)
            {
                if (requestedCursor != activeCursorTexture || hotspot != activeCursorHotspot)
                {
                    activeCursorTexture = requestedCursor;
                    activeCursorHotspot = hotspot;
                }

                Cursor.SetCursor(activeCursorTexture, activeCursorHotspot, CursorMode.Auto);
            }

            if (shouldUseCustomCursor && requestedCursor != null && Event.current.type == EventType.Repaint)
            {
                EditorGUIUtility.AddCursorRect(viewportRect, MouseCursor.CustomCursor);
            }
        }

        private void HandleNavigation(Event currentEvent, Rect viewportRect, bool isPointerInViewport, int panControlId)
        {
            if (currentEvent.type == EventType.ScrollWheel && currentEvent.control && isPointerInViewport)
            {
                int direction = currentEvent.delta.y > 0f ? -1 : 1;
                toolActions.SetBrushSize(state.BrushSize + direction);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.ScrollWheel && !currentEvent.control && isPointerInViewport)
            {
                float previousZoom = Zoom;
                float requestedZoom = Mathf.Clamp(Zoom - currentEvent.delta.y, 1f, 48f);
                Vector2 mouseInViewport = currentEvent.mousePosition - viewportRect.position;
                Vector2 contentUnderMouse = scrollPosition + mouseInViewport;
                Vector2 pixelPosition = (contentUnderMouse - new Vector2(PanPadding, PanPadding)) / previousZoom;
                Zoom = requestedZoom;
                scrollPosition = new Vector2(PanPadding, PanPadding) + pixelPosition * requestedZoom - mouseInViewport;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDown && currentEvent.button == 2 && isPointerInViewport)
            {
                isPanning = true;
                GUIUtility.hotControl = panControlId;
                panStartMouseScreenPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                panStartScrollPosition = scrollPosition;
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 2 && isPanning && GUIUtility.hotControl == panControlId)
            {
                Vector2 currentMouseScreenPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                scrollPosition = CalculatePannedScrollPosition(panStartScrollPosition, panStartMouseScreenPosition, currentMouseScreenPosition);
                currentEvent.Use();
            }
            else if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 2 && isPanning)
            {
                isPanning = false;
                if (GUIUtility.hotControl == panControlId) GUIUtility.hotControl = 0;
                currentEvent.Use();
            }
        }

        internal static Vector2 CalculatePannedScrollPosition(Vector2 startScrollPosition, Vector2 startMouseScreenPosition, Vector2 currentMouseScreenPosition)
        {
            return startScrollPosition - (currentMouseScreenPosition - startMouseScreenPosition);
        }

        private void DrawPixelGrid(Rect canvasRect)
        {
            Color gridColor = new Color(0f, 0f, 0f, 0.3f);
            for (int x = 1; x < state.DocumentWidth; x++) EditorGUI.DrawRect(new Rect(canvasRect.x + x * Zoom - 1f, canvasRect.y, 1f, canvasRect.height), gridColor);
            for (int y = 1; y < state.DocumentHeight; y++) EditorGUI.DrawRect(new Rect(canvasRect.x, canvasRect.y + y * Zoom - 1f, canvasRect.width, 1f), gridColor);
        }
    }

}
