using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class SpriteToolboxCanvasController : ICanvasInteractionHandler, IDisposable
    {
        private enum TransformHandle { None, Left, Right, Top, Bottom, TopLeft, TopRight, BottomLeft, BottomRight }
        private enum MoveAxis { Free, Horizontal, Vertical }
        private static readonly Color PreviewOutlineColor = new Color(0.94f, 0.76f, 0.38f);
        private const float RotationRingRadius = 72f;
        private readonly SpriteToolboxSession session;
        private readonly CanvasRenderCache renderCache;
        private readonly Action<Color, bool> colorPicked;
        private readonly Action repaint;
        private readonly LightPadSceneViewNavigationController sceneViewNavigationController;
        private readonly SpriteToolboxLightPadController lightPadController;
        private readonly SpriteToolboxLightPadEditorState lightPadEditorState;
        private Vector2Int lineStart;
        private Vector2Int selectionStart;
        private Vector2Int selectionMoveStart;
        private Vector2Int floatingSelectionDragStartDelta;
        private float floatingSelectionRotationStartAngle;
        private float floatingSelectionRotationStartMouseAngle;
        private Texture2D movingSelectionTexture;
        private bool isDrawingStroke;
        private bool isLinePreviewing;
        private bool isSelecting;
        private bool isModifyingSelection;
        private bool isRepositioningFloatingSelection;
        private bool isRotatingFloatingSelection;
        private bool isScalingFloatingSelection;
        private bool isScalingHorizontally;
        private bool isScalingProportionally;
        private bool isTransformingFloatingSelection;
        private TransformHandle activeTransformHandle;
        private RectInt floatingSelectionTransformStartRect;
        private Vector2 floatingSelectionScaleStart;
        private Vector2 floatingSelectionScalePivot;
        private Vector2 floatingSelectionScaleMouseStart;
        private MoveAxis activeMoveAxis;
        private int interactionMouseButton;
        private Color interactionColor;
        private bool selectionModifierAdds;
        private bool isDraggingLightPadReference;
        private Vector2 previousLightPadDragScreenPosition;
        private float zoom;

        public bool IsMovingSelection => isRepositioningFloatingSelection;
        public bool UsesNativeCursor => session.HasFloatingSelection;

        public SpriteToolboxCanvasController(SpriteToolboxSession session, CanvasRenderCache renderCache, SpriteToolboxLightPadController lightPadController, SpriteToolboxLightPadEditorState lightPadEditorState, Action<Color, bool> colorPicked, Action repaint)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.renderCache = renderCache ?? throw new ArgumentNullException(nameof(renderCache));
            this.colorPicked = colorPicked ?? throw new ArgumentNullException(nameof(colorPicked));
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
            this.lightPadEditorState = lightPadEditorState ?? throw new ArgumentNullException(nameof(lightPadEditorState));
            this.lightPadController = lightPadController ?? throw new ArgumentNullException(nameof(lightPadController));
            sceneViewNavigationController = new LightPadSceneViewNavigationController(lightPadController, repaint);
        }

        public void DrawPreviewsAndHandleInput(Rect canvasRect, float zoom, bool isPointerInViewport)
        {
            this.zoom = zoom;
            if (lightPadEditorState.IsSelected)
            {
                HandleLightPadInput(isPointerInViewport);
                return;
            }

            sceneViewNavigationController.CancelNavigation();

            DrawPencilPreview(canvasRect);
            DrawMovingSelectionPreview(canvasRect);
            DrawSelectionPreview(canvasRect);
            UpdateFloatingSelectionCursor(canvasRect);
            DrawSelectionModificationPreview(canvasRect);
            DrawLinePreview(canvasRect);
            HandleInput(canvasRect, isPointerInViewport);
        }

        public void Dispose()
        {
            if (isDrawingStroke) session.CancelStroke();
            isDrawingStroke = false;
            movingSelectionTexture = null;
            renderCache.ReleaseMovingSelectionPreview();
            sceneViewNavigationController.Dispose();
            Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
        }

        private void HandleLightPadInput(bool isPointerInViewport)
        {
            Event currentEvent = Event.current;
            if (lightPadController.Settings.SourceType == SpriteLightPadSourceType.SceneView)
            {
                sceneViewNavigationController.HandleInput(currentEvent, isPointerInViewport);
                return;
            }

            sceneViewNavigationController.CancelNavigation();
            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && isPointerInViewport)
            {
                isDraggingLightPadReference = true;
                previousLightPadDragScreenPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && isDraggingLightPadReference)
            {
                Vector2 currentScreenPosition = GUIUtility.GUIToScreenPoint(currentEvent.mousePosition);
                float safeZoom = Mathf.Max(zoom, 0.0001f);
                lightPadController.MoveReference((currentScreenPosition - previousLightPadDragScreenPosition) / safeZoom);
                previousLightPadDragScreenPosition = currentScreenPosition;
                currentEvent.Use();
                repaint();
                return;
            }

            if (currentEvent.rawType == EventType.MouseUp && currentEvent.button == 0 && isDraggingLightPadReference)
            {
                isDraggingLightPadReference = false;
                currentEvent.Use();
            }
        }

        private void HandleInput(Rect canvasRect, bool isPointerInViewport)
        {
            SpriteDocument document = session.Document;
            if (document == null) return;
            SpriteCel cel = document.GetFrame(session.SelectedFrameIndex).GetCel(session.SelectedLayerIndex);
            Event currentEvent = Event.current;

            if (session.HasFloatingSelection)
            {
                HandleFloatingSelectionInput(canvasRect, isPointerInViewport, currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isDrawingStroke)
            {
                CommitStroke();
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isLinePreviewing)
            {
                Vector2Int releasePosition;
                if (TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out releasePosition)) FinishPendingInteraction(releasePosition);
                else isLinePreviewing = false;
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isSelecting)
            {
                Vector2Int releasePosition;
                if (!TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out releasePosition)) TryGetClampedPosition(currentEvent.mousePosition, canvasRect, out releasePosition);
                FinishPendingInteraction(releasePosition);
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isLinePreviewing)
            {
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isSelecting)
            {
                Vector2Int dragPosition;
                if (TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out dragPosition))
                {
                    ContinueInteraction(dragPosition);
                    ConsumeAndRepaint(currentEvent);
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDown && IsSupportedMouseButton(session.ActiveTool, currentEvent.button) && session.ActiveTool == SpriteToolboxTool.Line)
            {
                Vector2Int startPosition;
                if (isPointerInViewport && !session.Layers[session.SelectedLayerIndex].IsLocked && TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out startPosition))
                {
                    lineStart = startPosition;
                    interactionMouseButton = currentEvent.button;
                    interactionColor = GetInteractionColor(currentEvent.button);
                    isLinePreviewing = true;
                    currentEvent.Use();
                }
                return;
            }

            bool canStartRectangularSelectionOutsideCanvas = currentEvent.type == EventType.MouseDown
                && session.ActiveTool == SpriteToolboxTool.Select
                && session.SelectionMode == SpriteSelectionMode.Rectangular
                && isPointerInViewport;
            Vector2Int position;
            bool hasPosition = canStartRectangularSelectionOutsideCanvas
                ? TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out position)
                : TryGetPosition(currentEvent.mousePosition, canvasRect, out position);
            if (!hasPosition || session.Layers[session.SelectedLayerIndex].IsLocked) return;
            if (currentEvent.type == EventType.MouseMove && (session.ActiveTool == SpriteToolboxTool.Pencil || session.ActiveTool == SpriteToolboxTool.Eraser) && !isDrawingStroke)
            {
                repaint();
                return;
            }

            bool isSelectionModifierClick = session.ActiveTool == SpriteToolboxTool.Select
                && currentEvent.control
                && (currentEvent.button == 0 || currentEvent.button == 1);
            if (currentEvent.type == EventType.MouseDown && (IsSupportedMouseButton(session.ActiveTool, currentEvent.button) || isSelectionModifierClick))
            {
                interactionMouseButton = currentEvent.button;
                interactionColor = GetInteractionColor(currentEvent.button);
                BeginInteraction(cel, position, currentEvent.button == 1, currentEvent.control);
                currentEvent.Use();
            }
            else if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton)
            {
                ContinueInteraction(position);
                ConsumeAndRepaint(currentEvent);
            }
        }

        private void BeginInteraction(SpriteCel cel, Vector2Int position, bool useSecondaryColor, bool modifySelection)
        {
            if (session.ActiveTool == SpriteToolboxTool.Eyedropper)
            {
                colorPicked(session.PickCompositeColor(position), useSecondaryColor);
            }
            else if (session.ActiveTool == SpriteToolboxTool.Select)
            {
                if (session.SelectionMode == SpriteSelectionMode.Magic)
                {
                    ReleaseMovingSelectionPreview();
                    session.BeginSelectionUndo();
                    if (modifySelection)
                    {
                        session.ModifyMagicPixelSelection(position, !useSecondaryColor);
                    }
                    else
                    {
                        session.SetMagicPixelSelection(position);
                    }
                    return;
                }

                if (!modifySelection && session.HasPixelSelection && session.SelectionRect.Contains(position))
                {
                    if (!session.BeginFloatingMoveSelection())
                    {
                        return;
                    }

                    isRepositioningFloatingSelection = true;
                    selectionMoveStart = position;
                    floatingSelectionDragStartDelta = Vector2Int.zero;
                    SetFloatingSelectionPreviewTexture();
                }
                else
                {
                    ReleaseMovingSelectionPreview();
                    session.BeginSelectionUndo();
                    selectionStart = position;
                    isModifyingSelection = modifySelection;
                    selectionModifierAdds = !useSecondaryColor;
                    if (!isModifyingSelection)
                    {
                        session.SetPixelSelectionRect(new RectInt(position.x, position.y, 1, 1));
                        session.SetPixelSelectionActive(true);
                    }
                    isSelecting = true;
                }
            }
            else if (session.ActiveTool == SpriteToolboxTool.Fill)
            {
                session.Fill(position, interactionColor);
            }
            else
            {
                BeginStroke(position, interactionColor);
            }
        }

        private void ContinueInteraction(Vector2Int position)
        {
            if (isDrawingStroke) ContinueStroke(position);
            else if (isSelecting && !isModifyingSelection)
            {
                session.SetPixelSelectionRect(CreateSelectionRect(selectionStart, position));
                session.SetPixelSelectionActive(true);
            }
            else if (isRepositioningFloatingSelection) MoveFloatingSelection(position);
        }

        private void FinishPendingInteraction(Vector2Int position)
        {
            if (isLinePreviewing)
            {
                session.DrawLine(lineStart, position, interactionColor);
                isLinePreviewing = false;
            }

            if (isSelecting)
            {
                RectInt selectionRect = CreateSelectionRect(selectionStart, position);
                if (isModifyingSelection)
                {
                    session.ModifyPixelSelectionRect(selectionRect, selectionModifierAdds);
                }
                else
                {
                    session.SetPixelSelectionRect(selectionRect);
                    session.SetPixelSelectionActive(true);
                }

                isSelecting = false;
                isModifyingSelection = false;
            }

        }

        private void HandleFloatingSelectionInput(Rect canvasRect, bool isPointerInViewport, Event currentEvent)
        {
            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isRepositioningFloatingSelection)
            {
                isRepositioningFloatingSelection = false;
                activeMoveAxis = MoveAxis.Free;
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isRotatingFloatingSelection)
            {
                isRotatingFloatingSelection = false;
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isScalingFloatingSelection)
            {
                isScalingFloatingSelection = false;
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseUp && currentEvent.button == interactionMouseButton && isTransformingFloatingSelection)
            {
                isTransformingFloatingSelection = false;
                activeTransformHandle = TransformHandle.None;
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isTransformingFloatingSelection)
            {
                TransformFloatingSelection(currentEvent.mousePosition, canvasRect);
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isScalingFloatingSelection)
            {
                ScaleFloatingSelection(currentEvent.mousePosition, canvasRect);
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isRotatingFloatingSelection)
            {
                RotateFloatingSelection(currentEvent.mousePosition, canvasRect);
                ConsumeAndRepaint(currentEvent);
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && session.FloatingSelection.IsRotationMode && TryBeginFloatingSelectionRotation(currentEvent.mousePosition, canvasRect))
            {
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && session.FloatingSelection.IsScaleMode && TryBeginFloatingSelectionScale(currentEvent.mousePosition, canvasRect))
            {
                currentEvent.Use();
                return;
            }

            if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && session.FloatingSelection.IsTransformMode && TryBeginFloatingSelectionTransform(currentEvent.mousePosition, canvasRect))
            {
                currentEvent.Use();
                return;
            }

            if (session.FloatingSelection.IsRotationMode || session.FloatingSelection.IsScaleMode || session.FloatingSelection.IsTransformMode)
            {
                Vector2Int rotationModePosition;
                if (currentEvent.type == EventType.MouseDown
                    && currentEvent.button == 0
                    && isPointerInViewport
                    && (!TryGetPosition(currentEvent.mousePosition, canvasRect, out rotationModePosition) || !session.FloatingSelection.Rect.Contains(rotationModePosition)))
                {
                    CommitFloatingSelection();
                    ConsumeAndRepaint(currentEvent);
                }

                return;
            }

            if (currentEvent.type == EventType.MouseDrag && currentEvent.button == interactionMouseButton && isRepositioningFloatingSelection)
            {
                Vector2Int position;
                if (TryGetUnboundedPosition(currentEvent.mousePosition, canvasRect, out position))
                {
                    MoveFloatingSelection(position);
                    ConsumeAndRepaint(currentEvent);
                }

                return;
            }

            if (currentEvent.type != EventType.MouseDown || currentEvent.button != 0 || !isPointerInViewport)
            {
                return;
            }

            if (TryBeginAxisMove(currentEvent.mousePosition, canvasRect))
            {
                currentEvent.Use();
                return;
            }

            Vector2Int clickedPosition;
            if (!TryGetPosition(currentEvent.mousePosition, canvasRect, out clickedPosition) || !session.FloatingSelection.Rect.Contains(clickedPosition))
            {
                CommitFloatingSelection();
                ConsumeAndRepaint(currentEvent);
                return;
            }

            interactionMouseButton = currentEvent.button;
            selectionMoveStart = clickedPosition;
            floatingSelectionDragStartDelta = session.FloatingSelection.Translation;
            activeMoveAxis = MoveAxis.Free;
            isRepositioningFloatingSelection = true;
            currentEvent.Use();
        }

        private void MoveFloatingSelection(Vector2Int position)
        {
            SpriteToolboxFloatingSelection floatingSelection = session.FloatingSelection;
            if (floatingSelection == null)
            {
                return;
            }

            Vector2Int delta = position - selectionMoveStart;
            if (activeMoveAxis == MoveAxis.Horizontal) delta.y = 0;
            else if (activeMoveAxis == MoveAxis.Vertical) delta.x = 0;
            session.MoveFloatingSelection(floatingSelectionDragStartDelta + delta);
        }

        private bool TryBeginAxisMove(Vector2 mousePosition, Rect canvasRect)
        {
            Vector2 pivot = GetSelectionGuiRect(canvasRect, session.FloatingSelection.Rect).center;
            if (GetScaleHandleRect(pivot + Vector2.right * 48f).Contains(mousePosition)) activeMoveAxis = MoveAxis.Horizontal;
            else if (GetScaleHandleRect(pivot + Vector2.down * 48f).Contains(mousePosition)) activeMoveAxis = MoveAxis.Vertical;
            else return false;
            Vector2Int startPosition;
            if (!TryGetUnboundedPosition(mousePosition, canvasRect, out startPosition)) return false;
            interactionMouseButton = 0;
            selectionMoveStart = startPosition;
            floatingSelectionDragStartDelta = session.FloatingSelection.Translation;
            isRepositioningFloatingSelection = true;
            return true;
        }

        private bool TryBeginFloatingSelectionRotation(Vector2 mousePosition, Rect canvasRect)
        {
            Rect selectionRect = GetSelectionGuiRect(canvasRect, session.FloatingSelection.Rect);
            if (!IsPointerOnRotationRing(mousePosition, selectionRect))
            {
                return false;
            }

            Vector2 pivot = selectionRect.center;
            floatingSelectionRotationStartMouseAngle = Mathf.Atan2(mousePosition.y - pivot.y, mousePosition.x - pivot.x) * Mathf.Rad2Deg;
            floatingSelectionRotationStartAngle = session.FloatingSelection.RotationDegrees;
            interactionMouseButton = 0;
            isRotatingFloatingSelection = true;
            return true;
        }

        private static bool IsPointerOnRotationRing(Vector2 mousePosition, Rect selectionRect)
        {
            const float RingHitThickness = 15f;
            float distance = Vector2.Distance(mousePosition, selectionRect.center);
            return Mathf.Abs(distance - GetRotationRingRadius(selectionRect)) <= RingHitThickness;
        }

        private void RotateFloatingSelection(Vector2 mousePosition, Rect canvasRect)
        {
            Rect selectionRect = GetSelectionGuiRect(canvasRect, session.FloatingSelection.Rect);
            Vector2 pivot = selectionRect.center;
            float mouseAngle = Mathf.Atan2(mousePosition.y - pivot.y, mousePosition.x - pivot.x) * Mathf.Rad2Deg;
            float angle = floatingSelectionRotationStartAngle - Mathf.DeltaAngle(floatingSelectionRotationStartMouseAngle, mouseAngle);
            session.SetFloatingSelectionRotation(angle);
            SetFloatingSelectionPreviewTexture();
        }

        private bool TryBeginFloatingSelectionScale(Vector2 mousePosition, Rect canvasRect)
        {
            Rect selectionRect = GetSelectionGuiRect(canvasRect, session.FloatingSelection.Rect);
            Vector2 pivot = selectionRect.center;
            Rect horizontalHandle = GetScaleHandleRect(pivot + Vector2.right * 48f);
            Rect verticalHandle = GetScaleHandleRect(pivot + Vector2.down * 48f);
            Rect proportionalHandle = GetScaleHandleRect(pivot);
            if (!horizontalHandle.Contains(mousePosition) && !verticalHandle.Contains(mousePosition) && !proportionalHandle.Contains(mousePosition)) return false;
            isScalingProportionally = proportionalHandle.Contains(mousePosition);
            isScalingHorizontally = !isScalingProportionally && horizontalHandle.Contains(mousePosition);
            floatingSelectionScaleStart = session.FloatingSelection.Scale;
            floatingSelectionScalePivot = pivot;
            floatingSelectionScaleMouseStart = mousePosition;
            interactionMouseButton = 0;
            isScalingFloatingSelection = true;
            return true;
        }

        private void ScaleFloatingSelection(Vector2 mousePosition, Rect canvasRect)
        {
            Vector2 scale = floatingSelectionScaleStart;
            if (isScalingProportionally)
            {
                float factor = 1f + ((mousePosition.x - floatingSelectionScaleMouseStart.x) + (floatingSelectionScaleMouseStart.y - mousePosition.y)) / 96f;
                scale *= Mathf.Max(0.1f, factor);
            }
            else if (isScalingHorizontally) scale.x *= Mathf.Abs(mousePosition.x - floatingSelectionScalePivot.x) / 48f;
            else scale.y *= Mathf.Abs(mousePosition.y - floatingSelectionScalePivot.y) / 48f;
            session.SetFloatingSelectionScale(scale);
            SetFloatingSelectionPreviewTexture();
        }

        private bool TryBeginFloatingSelectionTransform(Vector2 mousePosition, Rect canvasRect)
        {
            activeTransformHandle = GetTransformHandle(mousePosition, GetSelectionGuiRect(canvasRect, session.FloatingSelection.Rect));
            if (activeTransformHandle == TransformHandle.None) return false;
            floatingSelectionTransformStartRect = session.FloatingSelection.Rect;
            interactionMouseButton = 0;
            isTransformingFloatingSelection = true;
            return true;
        }

        private void TransformFloatingSelection(Vector2 mousePosition, Rect canvasRect)
        {
            Vector2Int position;
            if (!TryGetUnboundedPosition(mousePosition, canvasRect, out position)) return;
            int xMin = floatingSelectionTransformStartRect.xMin;
            int xMax = floatingSelectionTransformStartRect.xMax;
            int yMin = floatingSelectionTransformStartRect.yMin;
            int yMax = floatingSelectionTransformStartRect.yMax;
            if (activeTransformHandle == TransformHandle.Left || activeTransformHandle == TransformHandle.TopLeft || activeTransformHandle == TransformHandle.BottomLeft) xMin = Mathf.Min(position.x, xMax - 1);
            if (activeTransformHandle == TransformHandle.Right || activeTransformHandle == TransformHandle.TopRight || activeTransformHandle == TransformHandle.BottomRight) xMax = Mathf.Max(position.x + 1, xMin + 1);
            if (activeTransformHandle == TransformHandle.Bottom || activeTransformHandle == TransformHandle.BottomLeft || activeTransformHandle == TransformHandle.BottomRight) yMin = Mathf.Min(position.y, yMax - 1);
            if (activeTransformHandle == TransformHandle.Top || activeTransformHandle == TransformHandle.TopLeft || activeTransformHandle == TransformHandle.TopRight) yMax = Mathf.Max(position.y + 1, yMin + 1);
            session.SetFloatingSelectionTransform(new RectInt(xMin, yMin, xMax - xMin, yMax - yMin));
            SetFloatingSelectionPreviewTexture();
        }

        private void CommitFloatingSelection()
        {
            session.CommitFloatingSelection();
            isRepositioningFloatingSelection = false;
            ReleaseMovingSelectionPreview();
        }

        private void SetFloatingSelectionPreviewTexture()
        {
            SpriteToolboxFloatingSelection floatingSelection = session.FloatingSelection;
            if (floatingSelection == null)
            {
                return;
            }

            movingSelectionTexture = renderCache.SetMovingSelectionPreview(floatingSelection.CopyPixels(), floatingSelection.Rect.width, floatingSelection.Rect.height);
        }

        private void BeginStroke(Vector2Int position, Color color)
        {
            isDrawingStroke = true;
            ApplyStrokeResultToRenderCache(session.BeginStroke(position, color));
            renderCache.ApplyPendingUpload();
        }

        private void ContinueStroke(Vector2Int position)
        {
            ApplyStrokeResultToRenderCache(session.ContinueStroke(position, interactionColor));
        }

        private void CommitStroke()
        {
            isDrawingStroke = false;
            session.CommitStroke();
        }

        private void ApplyStrokeResultToRenderCache(SpriteToolboxOperationResult result)
        {
            foreach (PixelChange change in result.PixelChanges)
            {
                Color color = SpriteDocumentRenderer.GetCompositePixel(session.Document, session.SelectedFrameIndex, change.Position);
                renderCache.UpdateCompositePixel(session.Document, session.SelectedFrameIndex, change.Position, color);
            }
        }

        private void DrawSelectionPreview(Rect canvasRect)
        {
            if (session.HasFloatingSelection)
            {
                DrawFloatingSelectionOutline(canvasRect, session.FloatingSelection);
                return;
            }

            if (!session.HasPixelSelection) return;
            Color overlayColor = new Color(0.3f, 0.8f, 1f, 0.25f);
            Color outlineColor = new Color(0.3f, 0.8f, 1f, 0.9f);
            if (session.IsMagicPixelSelection)
            {
                foreach (Vector2Int position in session.PixelSelectionPositions)
                {
                    Rect pixelRect = GetSelectionGuiRect(canvasRect, new RectInt(position.x, position.y, 1, 1));
                    EditorGUI.DrawRect(pixelRect, overlayColor);
                }

                HashSet<Vector2Int> selectedPositions = new HashSet<Vector2Int>(session.PixelSelectionPositions);
                foreach (Vector2Int position in session.PixelSelectionPositions)
                {
                    Rect pixelRect = GetSelectionGuiRect(canvasRect, new RectInt(position.x, position.y, 1, 1));
                    DrawSelectionOutlineEdge(pixelRect, !selectedPositions.Contains(position + Vector2Int.up), !selectedPositions.Contains(position + Vector2Int.down), !selectedPositions.Contains(position + Vector2Int.left), !selectedPositions.Contains(position + Vector2Int.right), outlineColor);
                }
                return;
            }

            Rect rect = GetSelectionGuiRect(canvasRect, session.SelectionRect);
            EditorGUI.DrawRect(rect, overlayColor);
            DrawSelectionOutlineEdge(rect, true, true, true, true, outlineColor);
        }

        private void DrawFloatingSelectionOutline(Rect canvasRect, SpriteToolboxFloatingSelection floatingSelection)
        {
            if (floatingSelection == null)
            {
                return;
            }

            Color outlineColor = new Color(0.3f, 0.8f, 1f, 0.9f);
            if (!floatingSelection.IsMagicSelection)
            {
                DrawSelectionOutlineEdge(GetSelectionGuiRect(canvasRect, floatingSelection.Rect), true, true, true, true, outlineColor);
                if (floatingSelection.IsRotationMode) DrawRotationGuide(GetSelectionGuiRect(canvasRect, floatingSelection.Rect), floatingSelection.RotationDegrees);
                if (floatingSelection.IsScaleMode) DrawScaleHandles(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
                if (floatingSelection.IsTransformMode) DrawTransformHandles(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
                if (!floatingSelection.IsRotationMode && !floatingSelection.IsScaleMode && !floatingSelection.IsTransformMode) DrawMoveAxes(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
                return;
            }

            IReadOnlyList<Vector2Int> positions = floatingSelection.CurrentPositions;
            HashSet<Vector2Int> selectedPositions = new HashSet<Vector2Int>(positions);
            foreach (Vector2Int position in positions)
            {
                Rect pixelRect = GetSelectionGuiRect(canvasRect, new RectInt(position.x, position.y, 1, 1));
                DrawSelectionOutlineEdge(pixelRect, !selectedPositions.Contains(position + Vector2Int.up), !selectedPositions.Contains(position + Vector2Int.down), !selectedPositions.Contains(position + Vector2Int.left), !selectedPositions.Contains(position + Vector2Int.right), outlineColor);
            }

            if (floatingSelection.IsRotationMode) DrawRotationGuide(GetSelectionGuiRect(canvasRect, floatingSelection.Rect), floatingSelection.RotationDegrees);
            if (floatingSelection.IsScaleMode) DrawScaleHandles(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
            if (floatingSelection.IsTransformMode) DrawTransformHandles(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
            if (!floatingSelection.IsRotationMode && !floatingSelection.IsScaleMode && !floatingSelection.IsTransformMode) DrawMoveAxes(GetSelectionGuiRect(canvasRect, floatingSelection.Rect));
        }

        private void UpdateFloatingSelectionCursor(Rect canvasRect)
        {
            SpriteToolboxFloatingSelection floatingSelection = session.FloatingSelection;
            if (floatingSelection == null)
            {
                return;
            }

            Rect selectionRect = GetSelectionGuiRect(canvasRect, floatingSelection.Rect);
            if (!floatingSelection.IsRotationMode && !floatingSelection.IsScaleMode && !floatingSelection.IsTransformMode)
            {
                Vector2 pivot = selectionRect.center;
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(pivot + Vector2.right * 48f), MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(pivot + Vector2.down * 48f), MouseCursor.ResizeVertical);
                EditorGUIUtility.AddCursorRect(selectionRect, MouseCursor.MoveArrow);
                return;
            }
            if (floatingSelection.IsTransformMode)
            {
                AddTransformHandleCursors(selectionRect);
                return;
            }
            if (floatingSelection.IsScaleMode)
            {
                Vector2 pivot = selectionRect.center;
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(pivot + Vector2.right * 48f), MouseCursor.ResizeHorizontal);
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(pivot + Vector2.down * 48f), MouseCursor.ResizeVertical);
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(pivot), MouseCursor.ResizeUpLeft);
                return;
            }
            if (!floatingSelection.IsRotationMode)
            {
                EditorGUIUtility.AddCursorRect(selectionRect, MouseCursor.MoveArrow);
                return;
            }

            const int RingCursorSegments = 96;
            const float RingCursorHitSize = 30f;
            float radius = GetRotationRingRadius(selectionRect);
            for (int segmentIndex = 0; segmentIndex < RingCursorSegments; segmentIndex++)
            {
                float angle = segmentIndex * Mathf.PI * 2f / RingCursorSegments;
                Vector2 center = selectionRect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Rect cursorRect = new Rect(center.x - RingCursorHitSize * 0.5f, center.y - RingCursorHitSize * 0.5f, RingCursorHitSize, RingCursorHitSize);
                EditorGUIUtility.AddCursorRect(cursorRect, MouseCursor.RotateArrow);
            }
        }

        private static void DrawRotationGuide(Rect selectionRect, float angleDegrees)
        {
            float radius = GetRotationRingRadius(selectionRect);
            Vector3 pivot = new Vector3(selectionRect.center.x, selectionRect.center.y, 0f);
            Color previousColor = Handles.color;
            Handles.BeginGUI();
            Handles.color = new Color(0.95f, 0.82f, 0.2f, 0.16f);
            Handles.DrawSolidArc(pivot, Vector3.forward, Vector3.right, -angleDegrees, radius);
            Handles.color = PreviewOutlineColor;
            Handles.DrawWireDisc(pivot, Vector3.forward, radius);
            Handles.DrawLine(pivot, pivot + Vector3.right * radius);
            float angleRadians = -angleDegrees * Mathf.Deg2Rad;
            Vector3 rotatedDirection = new Vector3(Mathf.Cos(angleRadians), Mathf.Sin(angleRadians), 0f);
            Handles.DrawLine(pivot, pivot + rotatedDirection * radius);
            Handles.EndGUI();
            Handles.color = previousColor;
        }

        private static void DrawScaleHandles(Rect selectionRect)
        {
            Vector2 pivot = selectionRect.center;
            Vector2 horizontalHandle = pivot + Vector2.right * 48f;
            Vector2 verticalHandle = pivot + Vector2.down * 48f;
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawLine(pivot, horizontalHandle);
            Handles.color = Color.green;
            Handles.DrawLine(pivot, verticalHandle);
            Handles.EndGUI();
            EditorGUI.DrawRect(GetScaleHandleRect(horizontalHandle), Color.red);
            EditorGUI.DrawRect(GetScaleHandleRect(verticalHandle), Color.green);
            EditorGUI.DrawRect(GetScaleHandleRect(pivot), new Color(0.38f, 0.65f, 1f));
        }

        private static void DrawMoveAxes(Rect selectionRect)
        {
            Vector2 pivot = selectionRect.center;
            Vector2 horizontalHandle = pivot + Vector2.right * 48f;
            Vector2 verticalHandle = pivot + Vector2.down * 48f;
            Handles.BeginGUI();
            Handles.color = Color.red;
            Handles.DrawLine(pivot, horizontalHandle);
            Handles.color = Color.green;
            Handles.DrawLine(pivot, verticalHandle);
            Handles.color = Color.red;
            Handles.DrawAAConvexPolygon(new Vector3[]
            {
                horizontalHandle + Vector2.right * 8f,
                horizontalHandle + new Vector2(-5f, -6f),
                horizontalHandle + new Vector2(-5f, 6f)
            });
            Handles.color = Color.green;
            Handles.DrawAAConvexPolygon(new Vector3[]
            {
                verticalHandle + Vector2.down * 8f,
                verticalHandle + new Vector2(-6f, -5f),
                verticalHandle + new Vector2(6f, -5f)
            });
            Handles.EndGUI();
        }

        private static Rect GetScaleHandleRect(Vector2 center)
        {
            return new Rect(center.x - 8f, center.y - 8f, 16f, 16f);
        }

        private static void DrawTransformHandles(Rect selectionRect)
        {
            foreach (Vector2 handleCenter in GetTransformHandleCenters(selectionRect))
            {
                EditorGUI.DrawRect(GetScaleHandleRect(handleCenter), new Color(0.38f, 0.65f, 1f));
            }
        }

        private static void AddTransformHandleCursors(Rect selectionRect)
        {
            foreach (Vector2 handleCenter in GetTransformHandleCenters(selectionRect))
            {
                EditorGUIUtility.AddCursorRect(GetScaleHandleRect(handleCenter), MouseCursor.ResizeUpLeft);
            }
        }

        private static TransformHandle GetTransformHandle(Vector2 mousePosition, Rect selectionRect)
        {
            TransformHandle[] handles = { TransformHandle.TopLeft, TransformHandle.Top, TransformHandle.TopRight, TransformHandle.Right, TransformHandle.BottomRight, TransformHandle.Bottom, TransformHandle.BottomLeft, TransformHandle.Left };
            Vector2[] centers = GetTransformHandleCenters(selectionRect);
            for (int index = 0; index < centers.Length; index++)
            {
                if (GetScaleHandleRect(centers[index]).Contains(mousePosition)) return handles[index];
            }

            return TransformHandle.None;
        }

        private static Vector2[] GetTransformHandleCenters(Rect selectionRect)
        {
            return new[]
            {
                new Vector2(selectionRect.xMin, selectionRect.yMin), new Vector2(selectionRect.center.x, selectionRect.yMin), new Vector2(selectionRect.xMax, selectionRect.yMin), new Vector2(selectionRect.xMax, selectionRect.center.y),
                new Vector2(selectionRect.xMax, selectionRect.yMax), new Vector2(selectionRect.center.x, selectionRect.yMax), new Vector2(selectionRect.xMin, selectionRect.yMax), new Vector2(selectionRect.xMin, selectionRect.center.y)
            };
        }

        private static float GetRotationRingRadius(Rect selectionRect)
        {
            return RotationRingRadius;
        }

        private void DrawSelectionModificationPreview(Rect canvasRect)
        {
            if (!isSelecting || !isModifyingSelection)
            {
                return;
            }

            Vector2Int currentPosition;
            if (!TryGetUnboundedPosition(Event.current.mousePosition, canvasRect, out currentPosition))
            {
                return;
            }

            RectInt previewSelectionRect = ClampSelectionRectToCanvas(CreateSelectionRect(selectionStart, currentPosition));
            if (previewSelectionRect.width < 1 || previewSelectionRect.height < 1)
            {
                return;
            }

            Rect previewRect = GetSelectionGuiRect(canvasRect, previewSelectionRect);
            Color previewColor = selectionModifierAdds
                ? new Color(0.3f, 0.8f, 1f, 0.25f)
                : new Color(0.91f, 0.48f, 0.42f, 0.25f);
            Color outlineColor = selectionModifierAdds
                ? new Color(0.3f, 0.8f, 1f, 0.9f)
                : new Color(0.91f, 0.48f, 0.42f, 0.9f);
            EditorGUI.DrawRect(previewRect, previewColor);
            DrawSelectionOutlineEdge(previewRect, true, true, true, true, outlineColor);
        }

        private static void DrawSelectionOutlineEdge(Rect rect, bool top, bool bottom, bool left, bool right, Color color)
        {
            float thickness = Mathf.Min(2f, Mathf.Min(rect.width, rect.height));
            if (top) EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, thickness), color);
            if (bottom) EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - thickness, rect.width, thickness), color);
            if (left) EditorGUI.DrawRect(new Rect(rect.x, rect.y, thickness, rect.height), color);
            if (right) EditorGUI.DrawRect(new Rect(rect.xMax - thickness, rect.y, thickness, rect.height), color);
        }

        private void DrawPencilPreview(Rect canvasRect)
        {
            if ((session.ActiveTool != SpriteToolboxTool.Pencil && session.ActiveTool != SpriteToolboxTool.Eraser) || isDrawingStroke || session.Layers[session.SelectedLayerIndex].IsLocked) return;
            Vector2Int position;
            if (!TryGetPosition(Event.current.mousePosition, canvasRect, out position)) return;
            Color color = session.ActiveTool == SpriteToolboxTool.Eraser ? Color.clear : session.PrimaryColor;
            color.a = session.PrimaryColor.a > 0f ? 0.55f : 0.15f;
            int minimumOffset = -(session.BrushSize - 1) / 2;
            int maximumOffset = minimumOffset + session.BrushSize - 1;
            for (int y = minimumOffset; y <= maximumOffset; y++)
            {
                for (int x = minimumOffset; x <= maximumOffset; x++)
                {
                    if (SpriteBrushShapeUtility.ContainsOffset(x, y, session.BrushSize, session.BrushShape))
                    {
                        DrawBrushPreviewPixel(canvasRect, position + new Vector2Int(x, y), color);
                    }
                }
            }
        }

        private void DrawBrushPreviewPixel(Rect canvasRect, Vector2Int position, Color color)
        {
            DrawBrushPreviewPixelAt(canvasRect, position, color);
            SpriteDocument document = session.Document;
            if (session.HorizontalSymmetry) DrawBrushPreviewPixelAt(canvasRect, new Vector2Int(document.Width - 1 - position.x, position.y), color);
            if (session.VerticalSymmetry) DrawBrushPreviewPixelAt(canvasRect, new Vector2Int(position.x, document.Height - 1 - position.y), color);
            if (session.HorizontalSymmetry && session.VerticalSymmetry) DrawBrushPreviewPixelAt(canvasRect, new Vector2Int(document.Width - 1 - position.x, document.Height - 1 - position.y), color);
        }

        private void DrawBrushPreviewPixelAt(Rect canvasRect, Vector2Int position, Color color)
        {
            SpriteDocument document = session.Document;
            if (session.TiledMode) position = new Vector2Int(Modulo(position.x, document.Width), Modulo(position.y, document.Height));
            SpriteCel cel = document.GetFrame(session.SelectedFrameIndex).GetCel(session.SelectedLayerIndex);
            if (!cel.Contains(position)) return;
            Rect rect = GetPixelRect(canvasRect, position);
            EditorGUI.DrawRect(rect, color);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), PreviewOutlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), PreviewOutlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, 1f, rect.height), PreviewOutlineColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), PreviewOutlineColor);
        }

        private void DrawLinePreview(Rect canvasRect)
        {
            if (!isLinePreviewing) return;
            Vector2Int endPosition;
            if (!TryGetUnboundedPosition(Event.current.mousePosition, canvasRect, out endPosition)) return;
            SpriteCel cel = session.Document.GetFrame(session.SelectedFrameIndex).GetCel(session.SelectedLayerIndex);
            List<PixelChange> changes = CreateSymmetricPreviewChanges(cel, SpritePixelOperations.CreateLineChanges(cel, lineStart, endPosition, interactionColor));
            foreach (PixelChange change in changes) EditorGUI.DrawRect(GetPixelRect(canvasRect, change.Position), new Color(interactionColor.r, interactionColor.g, interactionColor.b, 0.65f));
        }

        private List<PixelChange> CreateSymmetricPreviewChanges(SpriteCel cel, List<PixelChange> sourceChanges)
        {
            if (!session.HorizontalSymmetry && !session.VerticalSymmetry) return sourceChanges;
            SpriteDocument document = session.Document;
            Dictionary<Vector2Int, PixelChange> changesByPosition = new Dictionary<Vector2Int, PixelChange>();
            foreach (PixelChange change in sourceChanges)
            {
                AddPreviewChange(changesByPosition, cel, change.Position, change.NewColor);
                if (session.HorizontalSymmetry) AddPreviewChange(changesByPosition, cel, new Vector2Int(document.Width - 1 - change.Position.x, change.Position.y), change.NewColor);
                if (session.VerticalSymmetry) AddPreviewChange(changesByPosition, cel, new Vector2Int(change.Position.x, document.Height - 1 - change.Position.y), change.NewColor);
                if (session.HorizontalSymmetry && session.VerticalSymmetry) AddPreviewChange(changesByPosition, cel, new Vector2Int(document.Width - 1 - change.Position.x, document.Height - 1 - change.Position.y), change.NewColor);
            }

            return new List<PixelChange>(changesByPosition.Values);
        }

        private static void AddPreviewChange(Dictionary<Vector2Int, PixelChange> changesByPosition, SpriteCel cel, Vector2Int position, Color color)
        {
            if (!cel.Contains(position) || cel.GetPixel(position) == color) return;
            PixelChange existingChange;
            Color previousColor = changesByPosition.TryGetValue(position, out existingChange) ? existingChange.PreviousColor : cel.GetPixel(position);
            changesByPosition[position] = new PixelChange(position, previousColor, color);
        }

        private void DrawMovingSelectionPreview(Rect canvasRect)
        {
            SpriteToolboxFloatingSelection floatingSelection = session.FloatingSelection;
            if (floatingSelection == null)
            {
                if (movingSelectionTexture != null) ReleaseMovingSelectionPreview();
                return;
            }

            if (movingSelectionTexture == null) SetFloatingSelectionPreviewTexture();
            if (movingSelectionTexture == null) return;
            foreach (Vector2Int sourcePosition in floatingSelection.SourcePositions)
            {
                GUI.DrawTextureWithTexCoords(GetPixelRect(canvasRect, sourcePosition), renderCache.GetCheckerboardTexture(), new Rect(sourcePosition.x * 0.5f, sourcePosition.y * 0.5f, 0.5f, 0.5f), true);
            }
            Color previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(GetSelectionGuiRect(canvasRect, floatingSelection.Rect), movingSelectionTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private bool TryGetPosition(Vector2 mousePosition, Rect canvasRect, out Vector2Int position)
        {
            return SpriteToolboxPresentationAdapter.TryGetCanvasPixel(session, mousePosition, canvasRect, zoom, out position);
        }

        private bool TryGetUnboundedPosition(Vector2 mousePosition, Rect canvasRect, out Vector2Int position)
        {
            return SpriteToolboxPresentationAdapter.TryGetCanvasPixelUnbounded(session, mousePosition, canvasRect, zoom, out position);
        }

        private bool TryGetClampedPosition(Vector2 mousePosition, Rect canvasRect, out Vector2Int position)
        {
            Vector2 clamped = new Vector2(Mathf.Clamp(mousePosition.x, canvasRect.xMin, canvasRect.xMax - 0.01f), Mathf.Clamp(mousePosition.y, canvasRect.yMin, canvasRect.yMax - 0.01f));
            return TryGetPosition(clamped, canvasRect, out position);
        }

        private Rect GetPixelRect(Rect canvasRect, Vector2Int position)
        {
            return new Rect(canvasRect.x + position.x * zoom, canvasRect.y + (session.Document.Height - 1 - position.y) * zoom, zoom, zoom);
        }

        private Rect GetSelectionGuiRect(Rect canvasRect, RectInt rect)
        {
            Rect topLeft = GetPixelRect(canvasRect, new Vector2Int(rect.xMin, rect.yMax - 1));
            return new Rect(topLeft.x, topLeft.y, rect.width * zoom, rect.height * zoom);
        }

        private static RectInt CreateSelectionRect(Vector2Int start, Vector2Int end)
        {
            return new RectInt(Mathf.Min(start.x, end.x), Mathf.Min(start.y, end.y), Mathf.Abs(end.x - start.x) + 1, Mathf.Abs(end.y - start.y) + 1);
        }

        private RectInt ClampSelectionRectToCanvas(RectInt rect)
        {
            SpriteDocument document = session.Document;
            int xMin = Mathf.Clamp(rect.xMin, 0, document.Width);
            int yMin = Mathf.Clamp(rect.yMin, 0, document.Height);
            int xMax = Mathf.Clamp(rect.xMax, 0, document.Width);
            int yMax = Mathf.Clamp(rect.yMax, 0, document.Height);
            return new RectInt(xMin, yMin, xMax - xMin, yMax - yMin);
        }

        private void ReleaseMovingSelectionPreview()
        {
            movingSelectionTexture = null;
            renderCache.ReleaseMovingSelectionPreview();
        }

        private void ConsumeAndRepaint(Event currentEvent)
        {
            currentEvent.Use();
            repaint();
        }

        private static int Modulo(int value, int modulus)
        {
            int remainder = value % modulus;
            return remainder < 0 ? remainder + modulus : remainder;
        }

        private Color GetInteractionColor(int mouseButton)
        {
            return mouseButton == 1 ? session.SecondaryColor : session.PrimaryColor;
        }

        private static bool IsSupportedMouseButton(SpriteToolboxTool tool, int mouseButton)
        {
            if (tool == SpriteToolboxTool.Recolor)
            {
                return false;
            }

            if (mouseButton == 0) return true;
            if (mouseButton != 1) return false;
            return tool == SpriteToolboxTool.Pencil || tool == SpriteToolboxTool.Fill || tool == SpriteToolboxTool.Line || tool == SpriteToolboxTool.Eyedropper;
        }
    }
}
