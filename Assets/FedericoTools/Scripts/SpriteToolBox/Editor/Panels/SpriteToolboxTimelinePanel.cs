using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public sealed class SpriteToolboxTimelinePanel : ISpriteToolboxPanel
    {
        private enum TimelineDragKind
        {
            None,
            Frame,
            Layer
        }

        private const float FlagWidth = 24f;
        private const float DefaultLayerWidth = 238f;
        private const float MinimumLayerWidth = 140f;
        private const float MaximumLayerWidth = 560f;
        private const float SplitterWidth = 6f;
        private const float FrameWidth = 30f;
        private const float RowHeight = 24f;
        private const float IconScale = 0.8f;
        private static readonly Color SelectedColor = new Color(0.57f, 0.78f, 0.88f);
        private static readonly Color PaletteSelectionColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color AddButtonColor = new Color(0.35f, 0.58f, 0.39f);
        private static readonly Color RemoveButtonColor = new Color(0.67f, 0.34f, 0.34f);
        private static readonly Rect EnlargedIconUv = new Rect(0.25f, 0.25f, 0.5f, 0.5f);

        private readonly ISpriteToolboxState state;
        private readonly ITimelineActions actions;
        private readonly IOnionSkinSettings onionSkinSettings;
        private readonly Action<ISpriteToolboxState, ITimelineActions, IOnionSkinSettings> testDraw;
        private Vector2 scrollPosition;
        private float layerWidth = DefaultLayerWidth;
        private float frameHeaderY;
        private float layerRowsStartY = RowHeight;
        private bool isResizingLayerWidth;
        private TimelineDragKind pendingDragKind;
        private TimelineDragKind activeDragKind;
        private int dragSourceIndex = -1;
        private int dragDestinationIndex = -1;
        private Vector2 dragStartPosition;
        private Rect dragTargetRect;
        private int renamingLayerIndex = -1;
        private string renamingLayerName = string.Empty;
        private bool hasPendingTagEditor;
        private int pendingTagEditorIndex;
        private Rect pendingTagEditorAnchorRect;
        private bool hasPendingFramePropertiesEditor;
        private int pendingFramePropertiesIndex;
        private Rect pendingFramePropertiesAnchorRect;
        private int resizingTagIndex = -1;
        private bool isResizingTagStart;
        private int resizingTagFromFrame;
        private int resizingTagToFrame;

        public int SheetColumns { get; private set; } = 4;

        public void RequestNewTagEditor()
        {
            float frameStartX = FlagWidth * 3f + layerWidth;
            Rect selectedFrameRect = new Rect(frameStartX + state.SelectedFrameIndex * FrameWidth, frameHeaderY, FrameWidth, RowHeight);
            ShowTagEditor(-1, selectedFrameRect);
        }

        public SpriteToolboxTimelinePanel(ISpriteToolboxTimelineContext context, IOnionSkinSettings onionSkinSettings)
        {
            this.state = context ?? throw new ArgumentNullException(nameof(context));
            this.actions = context;
            this.onionSkinSettings = onionSkinSettings ?? throw new ArgumentNullException(nameof(onionSkinSettings));
        }

        internal SpriteToolboxTimelinePanel(ISpriteToolboxState state, ITimelineActions actions, IOnionSkinSettings onionSkinSettings, Action<ISpriteToolboxState, ITimelineActions, IOnionSkinSettings> draw)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
            this.onionSkinSettings = onionSkinSettings ?? throw new ArgumentNullException(nameof(onionSkinSettings));
            testDraw = draw ?? throw new ArgumentNullException(nameof(draw));
        }

        public void Draw()
        {
            if (testDraw != null)
            {
                testDraw(state, actions, onionSkinSettings);
                return;
            }

            if (state.Frames.Count == 0 || state.Layers.Count == 0) return;
            DrawBody();
        }

        public void Draw(Rect panelRect)
        {
            if (testDraw != null)
            {
                testDraw(state, actions, onionSkinSettings);
                return;
            }

            if (state.Frames.Count == 0 || state.Layers.Count == 0) return;

            Rect contentRect = SpriteToolboxLayout.GetContentRect(panelRect, SpriteToolboxLayout.DefaultPanelContentPadding);
            GUILayout.BeginArea(contentRect);
            DrawBody();
            GUILayout.EndArea();
        }

        public void DrawBody()
        {
            if (state.Frames.Count == 0 || state.Layers.Count == 0) return;
            DrawPlaybackHeader();
            DrawVirtualizedGrid();
        }

        private void DrawPlaybackHeader()
        {
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("|◀", EditorStyles.miniButtonLeft, GUILayout.Width(30f))) actions.SelectFrame(0);
            if (GUILayout.Button("◀|", EditorStyles.miniButtonMid, GUILayout.Width(30f))) actions.SelectFrame(state.SelectedFrameIndex - 1);
            if (GUILayout.Button(state.IsPreviewPlaying ? "■" : "▶", EditorStyles.miniButtonMid, GUILayout.Width(30f))) actions.SetPreviewPlaying(!state.IsPreviewPlaying);
            if (GUILayout.Button("|▶", EditorStyles.miniButtonMid, GUILayout.Width(30f))) actions.SelectFrame(state.SelectedFrameIndex + 1);
            if (GUILayout.Button("▶|", EditorStyles.miniButtonRight, GUILayout.Width(30f))) actions.SelectFrame(state.Frames.Count - 1);
            GUILayout.FlexibleSpace();
            GUILayout.Label("Sheet columns", EditorStyles.miniLabel, GUILayout.Width(82f));
            SheetColumns = Mathf.Max(1, EditorGUILayout.IntField(SheetColumns, GUILayout.Width(42f)));
            EditorGUILayout.EndHorizontal();
        }

        private void DrawVirtualizedGrid()
        {
            Rect viewport = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            int layerCount = state.Layers.Count;
            int frameCount = state.Frames.Count;
            List<List<int>> tagLanes = BuildTagLanes();
            frameHeaderY = tagLanes.Count * RowHeight;
            layerRowsStartY = frameHeaderY + RowHeight;
            float frameStartX = FlagWidth * 3f + layerWidth;
            Rect content = new Rect(0f, 0f, Mathf.Max(viewport.width - 16f, frameStartX + (frameCount + 1) * FrameWidth), layerRowsStartY + (layerCount + 1) * RowHeight);
            scrollPosition = GUI.BeginScrollView(viewport, scrollPosition, content);

            HandleLayerWidthResize(frameStartX, content.height);
            frameStartX = FlagWidth * 3f + layerWidth;
            ShowPendingTagEditor();
            ShowPendingFramePropertiesEditor();
            UpdateTagRangeResize(frameStartX, frameCount);
            CommitLayerRenameWhenFocusLeaves();
            UpdateDragAndDrop(frameStartX, layerCount, frameCount);

            int firstFrame = Mathf.Clamp(Mathf.FloorToInt((scrollPosition.x - frameStartX) / FrameWidth), 0, frameCount - 1);
            int lastFrame = Mathf.Clamp(Mathf.CeilToInt((scrollPosition.x + viewport.width - frameStartX) / FrameWidth), 0, frameCount - 1);
            DrawHeaderCells(frameStartX, firstFrame, lastFrame, frameHeaderY);
            DrawTagLanes(tagLanes, frameStartX);
            int firstRow = Mathf.Clamp(Mathf.FloorToInt((scrollPosition.y - layerRowsStartY) / RowHeight), 0, layerCount - 1);
            int lastRow = Mathf.Clamp(Mathf.CeilToInt((scrollPosition.y + viewport.height - layerRowsStartY) / RowHeight), 0, layerCount - 1);
            for (int displayRow = firstRow; displayRow <= lastRow; displayRow++) DrawLayerRow(layerCount - 1 - displayRow, layerRowsStartY + displayRow * RowHeight, frameStartX, firstFrame, lastFrame);
            DrawNewLayerRow(layerRowsStartY + layerCount * RowHeight, frameStartX, firstFrame, lastFrame);
            DrawTagRangeBoundaries(tagLanes, frameStartX, content.height);
            DrawDragTarget(frameStartX, layerCount);
            GUI.EndScrollView();

            if (Event.current.rawType == EventType.MouseUp)
            {
                ClearPendingDrag();
            }
        }

        private void DrawHeaderCells(float frameStartX, int firstFrame, int lastFrame, float y)
        {
            bool allVisible = true;
            bool allLocked = true;
            for (int index = 0; index < state.Layers.Count; index++) { allVisible &= state.Layers[index].IsVisible; allLocked &= state.Layers[index].IsLocked; }
            bool nextVisible = DrawIconToggle(new Rect(0f, y, FlagWidth, RowHeight), allVisible, allVisible ? "IconEyeOpen" : "IconEyeClosed", "Show or hide all layers");
            if (nextVisible != allVisible) actions.SetAllLayersVisibility(nextVisible);
            bool nextLocked = DrawIconToggle(new Rect(FlagWidth, y, FlagWidth, RowHeight), allLocked, allLocked ? "IconLockClosed" : "IconLockOpen", "Lock or unlock all layers");
            if (nextLocked != allLocked) actions.SetAllLayersLock(nextLocked);
            float onionStartX = FlagWidth * 2f;
            bool showPrevious = DrawOnionToggle(new Rect(onionStartX, y, FlagWidth, RowHeight), onionSkinSettings.ShowPreviousOnionSkin, "IconOnionLeft", "Previous frame onion skin");
            if (showPrevious != onionSkinSettings.ShowPreviousOnionSkin) onionSkinSettings.ShowPreviousOnionSkin = showPrevious;
            bool showNext = DrawOnionToggle(new Rect(onionStartX + FlagWidth, y, FlagWidth, RowHeight), onionSkinSettings.ShowNextOnionSkin, "IconOnionRight", "Next frame onion skin");
            if (showNext != onionSkinSettings.ShowNextOnionSkin) onionSkinSettings.ShowNextOnionSkin = showNext;
            bool loopOnion = DrawOnionToggle(new Rect(onionStartX + FlagWidth * 2f, y, FlagWidth, RowHeight), onionSkinSettings.LoopOnionSkin, "IconOnionLoop", "Loop onion skin across first and last frames");
            if (loopOnion != onionSkinSettings.LoopOnionSkin) onionSkinSettings.LoopOnionSkin = loopOnion;
            for (int frameIndex = firstFrame; frameIndex <= lastFrame; frameIndex++)
            {
                Rect frameRect = new Rect(frameStartX + frameIndex * FrameWidth, y, FrameWidth, RowHeight);
                BeginPotentialDrag(TimelineDragKind.Frame, frameIndex, frameRect);
                int button = DrawFrameButton(frameRect, SpriteToolboxPresentationAdapter.GetFrameLabel(frameIndex), state.SelectedFrameIndex == frameIndex);
                if (button >= 0)
                {
                    actions.SelectFrame(frameIndex);
                    if (button == 1) CreateFrameMenu(frameRect).DropDown(GetContextMenuRect());
                    else if (button == 2) ShowFramePropertiesEditor(frameIndex, frameRect);
                }
            }
            if (DrawColoredButton(new Rect(frameStartX + state.Frames.Count * FrameWidth, y, FrameWidth, RowHeight), "+", AddButtonColor)) actions.AddFrame(false);
        }

        private void DrawLayerRow(int layerIndex, float y, float frameStartX, int firstFrame, int lastFrame)
        {
            SpriteToolboxLayerState layer = state.Layers[layerIndex];
            bool visible = DrawIconToggle(new Rect(0f, y, FlagWidth, RowHeight), layer.IsVisible, layer.IsVisible ? "IconEyeOpen" : "IconEyeClosed", "Layer visibility");
            if (visible != layer.IsVisible) actions.SetLayerVisibility(layerIndex, visible);
            bool locked = DrawIconToggle(new Rect(FlagWidth, y, FlagWidth, RowHeight), layer.IsLocked, layer.IsLocked ? "IconLockClosed" : "IconLockOpen", "Layer lock");
            if (locked != layer.IsLocked) actions.SetLayerLock(layerIndex, locked);
            Rect editRect = new Rect(FlagWidth * 2f, y, FlagWidth, RowHeight);
            if (DrawIconButton(editRect, "IconEdit", "Rename layer"))
            {
                actions.SelectLayer(layerIndex);
                BeginLayerRename(layerIndex, layer.Name);
            }

            Rect nameRect = new Rect(FlagWidth * 3f, y, layerWidth, RowHeight);
            if (renamingLayerIndex == layerIndex)
            {
                DrawLayerNameField(nameRect);
            }
            else
            {
                BeginPotentialDrag(TimelineDragKind.Layer, layerIndex, nameRect);
                int layerButton = DrawLayerButton(nameRect, layer.Name, state.SelectedLayerIndex == layerIndex);
                if (layerButton >= 0) { actions.SelectLayer(layerIndex); if (layerButton == 1) CreateLayerMenu().DropDown(GetContextMenuRect()); }
            }
            for (int frameIndex = firstFrame; frameIndex <= lastFrame; frameIndex++)
            {
                bool selected = state.SelectedLayerIndex == layerIndex && state.SelectedFrameIndex == frameIndex;
                Rect cellRect = new Rect(frameStartX + frameIndex * FrameWidth, y, FrameWidth, RowHeight);
                int cellButton = DrawCelButton(cellRect, state.CelHasVisiblePixels(frameIndex, layerIndex), selected);
                if (cellButton >= 0) { actions.SelectCell(frameIndex, layerIndex); if (cellButton == 1) CreateFrameMenu(cellRect).DropDown(GetContextMenuRect()); }
            }
            GUI.enabled = state.Layers.Count > 1 && !layer.IsLocked;
            if (DrawColoredButton(new Rect(frameStartX + state.Frames.Count * FrameWidth, y, FrameWidth, RowHeight), "-", RemoveButtonColor)) { actions.SelectLayer(layerIndex); actions.RemoveLayer(); }
            GUI.enabled = true;
        }

        private void DrawNewLayerRow(float y, float frameStartX, int firstFrame, int lastFrame)
        {
            if (DrawColoredButton(new Rect(FlagWidth * 3f, y, layerWidth, RowHeight), "New Layer", AddButtonColor)) actions.AddLayer($"Layer {state.Layers.Count + 1}");
            bool wasEnabled = GUI.enabled;
            GUI.enabled = state.Frames.Count > 1;
            for (int frameIndex = firstFrame; frameIndex <= lastFrame; frameIndex++) if (DrawColoredButton(new Rect(frameStartX + frameIndex * FrameWidth, y, FrameWidth, RowHeight), "-", RemoveButtonColor)) { actions.SelectFrame(frameIndex); actions.RemoveFrame(); }
            GUI.enabled = wasEnabled;
        }

        private GenericMenu CreateLayerMenu()
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New Layer"), false, () => actions.AddLayer($"Layer {state.Layers.Count + 1}"));
            menu.AddItem(new GUIContent("Duplicate Layer"), false, () => actions.DuplicateLayer());
            if (state.Layers.Count > 1 && !state.Layers[state.SelectedLayerIndex].IsLocked) menu.AddItem(new GUIContent("Delete Layer"), false, () => actions.RemoveLayer()); else menu.AddDisabledItem(new GUIContent("Delete Layer"));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Move Up"), false, () => actions.MoveLayer(1));
            menu.AddItem(new GUIContent("Move Down"), false, () => actions.MoveLayer(-1));
            if (state.SelectedLayerIndex > 0) menu.AddItem(new GUIContent("Merge Down"), false, () => actions.MergeLayerDown()); else menu.AddDisabledItem(new GUIContent("Merge Down"));
            return menu;
        }

        private GenericMenu CreateFrameMenu(Rect anchorRect)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("New Frame"), false, () => actions.AddFrame(false));
            menu.AddItem(new GUIContent("Duplicate Frame"), false, () => actions.AddFrame(true));
            if (state.Frames.Count > 1) menu.AddItem(new GUIContent("Delete Frame"), false, () => actions.RemoveFrame()); else menu.AddDisabledItem(new GUIContent("Delete Frame"));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Properties..."), false, () => ShowFramePropertiesEditor(state.SelectedFrameIndex, anchorRect));
            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("New Tag"), false, () => ShowTagEditor(-1, anchorRect));
            return menu;
        }

        private List<List<int>> BuildTagLanes()
        {
            List<List<int>> lanes = new List<List<int>>();
            List<int> sortedTagIndices = new List<int>(state.AnimationTags.Count);
            for (int tagIndex = 0; tagIndex < state.AnimationTags.Count; tagIndex++)
            {
                sortedTagIndices.Add(tagIndex);
            }

            sortedTagIndices.Sort((firstTagIndex, secondTagIndex) =>
            {
                SpriteToolboxAnimationTagState firstTag = state.AnimationTags[firstTagIndex];
                SpriteToolboxAnimationTagState secondTag = state.AnimationTags[secondTagIndex];
                int firstLength = firstTag.ToFrame - firstTag.FromFrame;
                int secondLength = secondTag.ToFrame - secondTag.FromFrame;
                int lengthComparison = secondLength.CompareTo(firstLength);
                return lengthComparison != 0 ? lengthComparison : firstTag.FromFrame.CompareTo(secondTag.FromFrame);
            });

            for (int sortedTagIndex = 0; sortedTagIndex < sortedTagIndices.Count; sortedTagIndex++)
            {
                int tagIndex = sortedTagIndices[sortedTagIndex];
                SpriteToolboxAnimationTagState tag = state.AnimationTags[tagIndex];
                bool wasPlaced = false;
                for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
                {
                    List<int> lane = lanes[laneIndex];
                    bool overlapsExistingTag = false;
                    for (int laneTagIndex = 0; laneTagIndex < lane.Count; laneTagIndex++)
                    {
                        SpriteToolboxAnimationTagState existingTag = state.AnimationTags[lane[laneTagIndex]];
                        if (tag.FromFrame <= existingTag.ToFrame && tag.ToFrame >= existingTag.FromFrame)
                        {
                            overlapsExistingTag = true;
                            break;
                        }
                    }

                    if (!overlapsExistingTag)
                    {
                        lane.Add(tagIndex);
                        wasPlaced = true;
                        break;
                    }
                }

                if (!wasPlaced)
                {
                    lanes.Add(new List<int> { tagIndex });
                }
            }

            for (int laneIndex = 0; laneIndex < lanes.Count; laneIndex++)
            {
                lanes[laneIndex].Sort((firstTagIndex, secondTagIndex) => state.AnimationTags[firstTagIndex].FromFrame.CompareTo(state.AnimationTags[secondTagIndex].FromFrame));
            }

            return lanes;
        }

        private void DrawTagLanes(IReadOnlyList<List<int>> tagLanes, float frameStartX)
        {
            for (int laneIndex = 0; laneIndex < tagLanes.Count; laneIndex++)
            {
                float y = laneIndex * RowHeight;
                List<int> lane = tagLanes[laneIndex];
                for (int laneTagIndex = 0; laneTagIndex < lane.Count; laneTagIndex++)
                {
                    int tagIndex = lane[laneTagIndex];
                    SpriteToolboxAnimationTagState tag = state.AnimationTags[tagIndex];
                    GetDisplayedTagRange(tagIndex, tag, out int fromFrame, out int toFrame);
                    Rect tagRangeRect = new Rect(frameStartX + fromFrame * FrameWidth, y, (toFrame - fromFrame + 1) * FrameWidth, RowHeight);
                    float labelWidth = CalculateTagLabelWidth(tag.Name);
                    Rect tagLabelRect = new Rect(tagRangeRect.x, y + 2f, labelWidth, RowHeight - 4f);
                    float lineStartX = Mathf.Min(tagLabelRect.xMax + 3f, tagRangeRect.xMax);
                    EditorGUI.DrawRect(new Rect(lineStartX, GetTagUnderlineY(laneIndex), tagRangeRect.xMax - lineStartX, 2f), tag.Color);
                    BeginTagRangeResize(tagIndex, tagRangeRect, laneIndex);
                    int button = DrawTagButton(tagLabelRect, tag.Name, state.ActiveAnimationTagIndex == tagIndex);
                    if (button == 0)
                    {
                        actions.SelectAnimationTag(tagIndex);
                    }
                    else if (button == 2)
                    {
                        actions.SelectAnimationTag(tagIndex);
                        ShowTagEditor(tagIndex, tagLabelRect);
                    }
                    else if (button == 1)
                    {
                        CreateTagMenu(tagIndex, tagLabelRect).DropDown(GetContextMenuRect());
                    }
                }
            }
        }

        private void DrawTagRangeBoundaries(IReadOnlyList<List<int>> tagLanes, float frameStartX, float contentHeight)
        {
            for (int laneIndex = 0; laneIndex < tagLanes.Count; laneIndex++)
            {
                List<int> lane = tagLanes[laneIndex];
                for (int laneTagIndex = 0; laneTagIndex < lane.Count; laneTagIndex++)
                {
                    int tagIndex = lane[laneTagIndex];
                    SpriteToolboxAnimationTagState tag = state.AnimationTags[tagIndex];
                    GetDisplayedTagRange(tagIndex, tag, out int fromFrame, out int toFrame);
                    float startX = frameStartX + fromFrame * FrameWidth;
                    float endX = frameStartX + (toFrame + 1) * FrameWidth;
                    float underlineY = GetTagUnderlineY(laneIndex);
                    DrawDashedVerticalLine(startX, underlineY, contentHeight, tag.Color);
                    DrawDashedVerticalLine(endX, underlineY, contentHeight, tag.Color);
                }
            }
        }

        private static void DrawDashedVerticalLine(float x, float startY, float endY, Color color)
        {
            const float DashHeight = 4f;
            const float GapHeight = 3f;
            Color lineColor = new Color(color.r, color.g, color.b, 0.7f);
            for (float y = startY; y < endY; y += DashHeight + GapHeight)
            {
                EditorGUI.DrawRect(new Rect(x, y, 1f, Mathf.Min(DashHeight, endY - y)), lineColor);
            }
        }

        private void BeginTagRangeResize(int tagIndex, Rect tagRangeRect, int laneIndex)
        {
            if (resizingTagIndex >= 0)
            {
                return;
            }

            float underlineY = GetTagUnderlineY(laneIndex);
            Rect startHandleRect = new Rect(tagRangeRect.x - 4f, underlineY - 5f, 8f, 10f);
            Rect endHandleRect = new Rect(tagRangeRect.xMax - 4f, underlineY - 5f, 8f, 10f);
            EditorGUIUtility.AddCursorRect(startHandleRect, MouseCursor.ResizeHorizontal);
            EditorGUIUtility.AddCursorRect(endHandleRect, MouseCursor.ResizeHorizontal);
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0)
            {
                return;
            }

            if (startHandleRect.Contains(current.mousePosition))
            {
                StartTagRangeResize(tagIndex, true);
                current.Use();
            }
            else if (endHandleRect.Contains(current.mousePosition))
            {
                StartTagRangeResize(tagIndex, false);
                current.Use();
            }
        }

        private void StartTagRangeResize(int tagIndex, bool resizeStart)
        {
            SpriteToolboxAnimationTagState tag = state.AnimationTags[tagIndex];
            resizingTagIndex = tagIndex;
            isResizingTagStart = resizeStart;
            resizingTagFromFrame = tag.FromFrame;
            resizingTagToFrame = tag.ToFrame;
            actions.SelectAnimationTag(tagIndex);
        }

        private void UpdateTagRangeResize(float frameStartX, int frameCount)
        {
            if (resizingTagIndex < 0)
            {
                return;
            }

            Event current = Event.current;
            if (current.type == EventType.MouseDrag)
            {
                float framePosition = (current.mousePosition.x - frameStartX) / FrameWidth;
                if (isResizingTagStart)
                {
                    resizingTagFromFrame = Mathf.Clamp(Mathf.FloorToInt(framePosition), 0, resizingTagToFrame);
                }
                else
                {
                    resizingTagToFrame = Mathf.Clamp(Mathf.CeilToInt(framePosition) - 1, resizingTagFromFrame, frameCount - 1);
                }

                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                CommitTagRangeResize();
                current.Use();
            }
        }

        private void CommitTagRangeResize()
        {
            if (resizingTagIndex >= 0 && resizingTagIndex < state.AnimationTags.Count)
            {
                SpriteToolboxAnimationTagState tag = state.AnimationTags[resizingTagIndex];
                if (tag.FromFrame != resizingTagFromFrame || tag.ToFrame != resizingTagToFrame)
                {
                    actions.UpdateAnimationTag(resizingTagIndex, tag.Name, resizingTagFromFrame, resizingTagToFrame, tag.Direction, tag.Color);
                }
            }

            resizingTagIndex = -1;
        }

        private void GetDisplayedTagRange(int tagIndex, SpriteToolboxAnimationTagState tag, out int fromFrame, out int toFrame)
        {
            if (tagIndex == resizingTagIndex)
            {
                fromFrame = resizingTagFromFrame;
                toFrame = resizingTagToFrame;
                return;
            }

            fromFrame = tag.FromFrame;
            toFrame = tag.ToFrame;
        }

        private static float GetTagUnderlineY(int laneIndex)
        {
            return laneIndex * RowHeight + RowHeight - 10f;
        }

        private GenericMenu CreateTagMenu(int tagIndex, Rect anchorRect)
        {
            GenericMenu menu = new GenericMenu();
            menu.AddItem(new GUIContent("Edit Tag"), false, () => ShowTagEditor(tagIndex, anchorRect));
            menu.AddItem(new GUIContent("Delete Tag"), false, () =>
            {
                if (actions.RemoveAnimationTag(tagIndex))
                {
                    actions.SelectAnimationTag(-1);
                }
            });
            return menu;
        }

        private void ShowTagEditor(int tagIndex, Rect anchorRect)
        {
            hasPendingTagEditor = true;
            pendingTagEditorIndex = tagIndex;
            pendingTagEditorAnchorRect = anchorRect;
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                focusedWindow.Repaint();
            }
        }

        private void ShowPendingTagEditor()
        {
            if (!hasPendingTagEditor || Event.current.type != EventType.Repaint)
            {
                return;
            }

            hasPendingTagEditor = false;
            AnimationTagEditorPopup popup = new AnimationTagEditorPopup(state, actions, pendingTagEditorIndex);
            Vector2 popupSize = popup.GetWindowSize();
            Rect popupAnchorRect = new Rect(pendingTagEditorAnchorRect.x, pendingTagEditorAnchorRect.y - popupSize.y, pendingTagEditorAnchorRect.width, 0f);
            PopupWindow.Show(popupAnchorRect, popup);
        }

        private void ShowFramePropertiesEditor(int frameIndex, Rect anchorRect)
        {
            if (frameIndex < 0 || frameIndex >= state.Frames.Count)
            {
                return;
            }

            hasPendingFramePropertiesEditor = true;
            pendingFramePropertiesIndex = frameIndex;
            pendingFramePropertiesAnchorRect = anchorRect;
            EditorWindow focusedWindow = EditorWindow.focusedWindow;
            if (focusedWindow != null)
            {
                focusedWindow.Repaint();
            }
        }

        private void ShowPendingFramePropertiesEditor()
        {
            if (!hasPendingFramePropertiesEditor || Event.current.type != EventType.Repaint)
            {
                return;
            }

            hasPendingFramePropertiesEditor = false;
            FramePropertiesPopup popup = new FramePropertiesPopup(state, actions, pendingFramePropertiesIndex);
            Vector2 popupSize = popup.GetWindowSize();
            Rect popupAnchorRect = new Rect(pendingFramePropertiesAnchorRect.x, pendingFramePropertiesAnchorRect.y - popupSize.y, pendingFramePropertiesAnchorRect.width, 0f);
            PopupWindow.Show(popupAnchorRect, popup);
        }

        private void BeginPotentialDrag(TimelineDragKind kind, int sourceIndex, Rect rect)
        {
            Event current = Event.current;
            if (current.type != EventType.MouseDown || current.button != 0 || !rect.Contains(current.mousePosition))
            {
                return;
            }

            pendingDragKind = kind;
            dragSourceIndex = sourceIndex;
            dragDestinationIndex = sourceIndex;
            dragStartPosition = current.mousePosition;
        }

        private void UpdateDragAndDrop(float frameStartX, int layerCount, int frameCount)
        {
            Event current = Event.current;
            if (activeDragKind != TimelineDragKind.None)
            {
                if (current.type == EventType.MouseDrag)
                {
                    dragDestinationIndex = GetDragDestination(current.mousePosition, frameStartX, layerCount, frameCount);
                    current.Use();
                }
                else if (current.type == EventType.MouseUp)
                {
                    int destinationIndex = GetDragDestination(current.mousePosition, frameStartX, layerCount, frameCount);
                    if (destinationIndex != dragSourceIndex)
                    {
                        if (activeDragKind == TimelineDragKind.Frame)
                        {
                            actions.MoveFrameTo(dragSourceIndex, destinationIndex);
                        }
                        else
                        {
                            actions.MoveLayerTo(dragSourceIndex, destinationIndex);
                        }
                    }

                    ClearPendingDrag();
                    current.Use();
                }

                return;
            }

            if (pendingDragKind == TimelineDragKind.None || current.type != EventType.MouseDrag || (current.mousePosition - dragStartPosition).sqrMagnitude < 16f)
            {
                return;
            }

            activeDragKind = pendingDragKind;
            dragDestinationIndex = GetDragDestination(current.mousePosition, frameStartX, layerCount, frameCount);
            current.Use();
        }

        private int GetDragDestination(Vector2 mousePosition, float frameStartX, int layerCount, int frameCount)
        {
            if (activeDragKind == TimelineDragKind.Frame)
            {
                int targetFrameIndex = Mathf.Clamp(Mathf.FloorToInt((mousePosition.x - frameStartX) / FrameWidth), 0, frameCount - 1);
                float targetX = frameStartX + targetFrameIndex * FrameWidth;
                bool insertAfter = mousePosition.x >= targetX + FrameWidth * 0.5f;
                dragTargetRect = new Rect(targetX + (insertAfter ? FrameWidth * 0.5f : 0f), frameHeaderY, FrameWidth * 0.5f, RowHeight);
                int insertionIndex = targetFrameIndex + (insertAfter ? 1 : 0);
                return GetDestinationIndexForInsertion(insertionIndex, frameCount);
            }

            int targetDisplayRow = Mathf.Clamp(Mathf.FloorToInt((mousePosition.y - layerRowsStartY) / RowHeight), 0, layerCount - 1);
            float targetY = layerRowsStartY + targetDisplayRow * RowHeight;
            bool insertBelow = mousePosition.y >= targetY + RowHeight * 0.5f;
            dragTargetRect = new Rect(FlagWidth * 3f, targetY + (insertBelow ? RowHeight * 0.5f : 0f), layerWidth, RowHeight * 0.5f);
            int visualInsertionIndex = targetDisplayRow + (insertBelow ? 1 : 0);
            int layerInsertionIndex = layerCount - visualInsertionIndex;
            return GetDestinationIndexForInsertion(layerInsertionIndex, layerCount);
        }

        private int GetDestinationIndexForInsertion(int insertionIndex, int itemCount)
        {
            int destinationIndex = insertionIndex > dragSourceIndex ? insertionIndex - 1 : insertionIndex;
            return Mathf.Clamp(destinationIndex, 0, itemCount - 1);
        }

        private void DrawDragTarget(float frameStartX, int layerCount)
        {
            if (activeDragKind == TimelineDragKind.None || dragDestinationIndex < 0)
            {
                return;
            }

            Color targetColor = new Color(SelectedColor.r, SelectedColor.g, SelectedColor.b, 0.72f);
            EditorGUI.DrawRect(dragTargetRect, targetColor);
        }

        private void ClearPendingDrag()
        {
            pendingDragKind = TimelineDragKind.None;
            activeDragKind = TimelineDragKind.None;
            dragSourceIndex = -1;
            dragDestinationIndex = -1;
            dragTargetRect = Rect.zero;
        }

        private void BeginLayerRename(int layerIndex, string name)
        {
            renamingLayerIndex = layerIndex;
            renamingLayerName = name ?? string.Empty;
            GUI.FocusControl("SpriteToolboxLayerName");
        }

        private void DrawLayerNameField(Rect rect)
        {
            Event current = Event.current;
            EventType rawType = current.rawType;
            KeyCode keyCode = current.keyCode;
            GUI.SetNextControlName("SpriteToolboxLayerName");
            renamingLayerName = EditorGUI.TextField(rect, renamingLayerName);
            if (rawType == EventType.KeyDown && (keyCode == KeyCode.Return || keyCode == KeyCode.KeypadEnter))
            {
                CommitLayerRename();
            }
            else if (rawType == EventType.KeyDown && keyCode == KeyCode.Escape)
            {
                renamingLayerIndex = -1;
                renamingLayerName = string.Empty;
            }
        }

        private void CommitLayerRenameWhenFocusLeaves()
        {
            if (renamingLayerIndex < 0 || Event.current.type != EventType.MouseDown)
            {
                return;
            }

            int displayRow = state.Layers.Count - 1 - renamingLayerIndex;
            Rect nameRect = new Rect(FlagWidth * 3f, layerRowsStartY + displayRow * RowHeight, layerWidth, RowHeight);
            if (!nameRect.Contains(Event.current.mousePosition))
            {
                CommitLayerRename();
            }
        }

        private void CommitLayerRename()
        {
            if (renamingLayerIndex >= 0)
            {
                actions.SetLayerName(renamingLayerIndex, renamingLayerName.Trim());
            }

            renamingLayerIndex = -1;
            renamingLayerName = string.Empty;
        }

        private static int DrawButton(Rect rect, string label, bool selected)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && (current.button == 0 || current.button == 1)) { int button = current.button; current.Use(); return button; }
            Color original = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = SelectedColor;
            GUI.Button(rect, label, GUI.skin.button);
            GUI.backgroundColor = original;
            return -1;
        }

        private static Rect GetContextMenuRect()
        {
            Vector2 mousePosition = Event.current.mousePosition;
            return new Rect(mousePosition.x, mousePosition.y, 0f, 0f);
        }

        private static float CalculateTagLabelWidth(string label)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel);
            float textWidth = style.CalcSize(new GUIContent(label ?? string.Empty)).x;
            return textWidth + 12f;
        }

        private static int DrawTagButton(Rect rect, string label, bool selected)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && (current.button == 0 || current.button == 1))
            {
                int button = current.button == 0 && current.clickCount >= 2 ? 2 : current.button;
                current.Use();
                return button;
            }

            DrawLabel(rect, label, selected ? PaletteSelectionColor : Color.white, TextAnchor.MiddleCenter);
            GUI.Label(rect, new GUIContent(string.Empty, "Select tag. Right-click for tag actions."), GUIStyle.none);
            return -1;
        }

        private static int DrawLayerButton(Rect rect, string label, bool selected)
        {
            return DrawTextButton(rect, label, selected, selected ? PaletteSelectionColor : Color.white, TextAnchor.MiddleLeft);
        }

        private static int DrawFrameButton(Rect rect, string label, bool selected)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && (current.button == 0 || current.button == 1))
            {
                int button = current.button == 0 && current.clickCount >= 2 ? 2 : current.button;
                current.Use();
                return button;
            }

            Color originalBackgroundColor = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = SelectedColor;
            GUI.Button(rect, GUIContent.none, GUI.skin.button);
            GUI.backgroundColor = originalBackgroundColor;
            DrawLabel(rect, label, selected ? PaletteSelectionColor : Color.white, TextAnchor.MiddleCenter);
            return -1;
        }

        private static int DrawTextButton(Rect rect, string label, bool selected, Color textColor, TextAnchor alignment)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && (current.button == 0 || current.button == 1))
            {
                int button = current.button;
                current.Use();
                return button;
            }

            Color originalBackgroundColor = GUI.backgroundColor;
            if (selected) GUI.backgroundColor = SelectedColor;
            GUI.Button(rect, GUIContent.none, GUI.skin.button);
            GUI.backgroundColor = originalBackgroundColor;
            Rect labelRect = alignment == TextAnchor.MiddleLeft ? new Rect(rect.x + 4f, rect.y, rect.width - 4f, rect.height) : rect;
            DrawLabel(labelRect, label, textColor, alignment);
            return -1;
        }

        private static bool DrawColoredButton(Rect rect, string label, Color color)
        {
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = color;
            bool wasPressed = GUI.Button(rect, label);
            GUI.backgroundColor = previousBackgroundColor;
            return wasPressed;
        }

        private static int DrawCelButton(Rect rect, bool hasVisiblePixels, bool selected)
        {
            Event current = Event.current;
            if (current.type == EventType.MouseDown && rect.Contains(current.mousePosition) && (current.button == 0 || current.button == 1))
            {
                int button = current.button;
                current.Use();
                return button;
            }

            Color previousBackgroundColor = GUI.backgroundColor;
            if (selected)
            {
                GUI.backgroundColor = SelectedColor;
            }

            GUI.Button(rect, GUIContent.none, GUI.skin.button);
            GUI.backgroundColor = previousBackgroundColor;
            DrawLabel(rect, hasVisiblePixels ? "●" : "○", selected ? PaletteSelectionColor : Color.white, TextAnchor.MiddleCenter);
            return -1;
        }

        private static void DrawLabel(Rect rect, string label, Color color, TextAnchor alignment)
        {
            GUIStyle style = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = alignment
            };
            style.normal.textColor = color;
            GUI.Label(rect, label, style);
        }

        private static bool DrawIconToggle(Rect rect, bool value, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            if (icon == null)
            {
                return GUI.Toggle(rect, value, new GUIContent(value ? "●" : "○", tooltip), GUI.skin.button);
            }

            bool result = GUI.Toggle(rect, value, GUIContent.none, GUI.skin.button);
            if (Event.current.type == EventType.Repaint)
            {
                float currentWidth = rect.width - 4f;
                float currentHeight = rect.height - 4f;
                float iconWidth = currentWidth * IconScale;
                float iconHeight = currentHeight * IconScale;
                Rect iconRect = new Rect(rect.center.x - iconWidth * 0.5f, rect.center.y - iconHeight * 0.5f, iconWidth, iconHeight);
                GUI.DrawTextureWithTexCoords(iconRect, icon, EnlargedIconUv, true);
            }

            GUI.Label(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            return result;
        }

        private void HandleLayerWidthResize(float frameStartX, float contentHeight)
        {
            Rect splitterRect = new Rect(frameStartX - SplitterWidth * 0.5f, 0f, SplitterWidth, contentHeight);
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);
            Event current = Event.current;
            if (current.type == EventType.MouseDown && current.button == 0 && splitterRect.Contains(current.mousePosition))
            {
                isResizingLayerWidth = true;
                current.Use();
                return;
            }

            if (!isResizingLayerWidth)
            {
                return;
            }

            if (current.type == EventType.MouseDrag)
            {
                layerWidth = Mathf.Clamp(current.mousePosition.x - FlagWidth * 3f, MinimumLayerWidth, MaximumLayerWidth);
                current.Use();
            }
            else if (current.type == EventType.MouseUp)
            {
                isResizingLayerWidth = false;
                current.Use();
            }
        }

        private static bool DrawIconButton(Rect rect, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            bool wasPressed = GUI.Button(rect, GUIContent.none, GUI.skin.button);
            if (icon != null && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(rect, icon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            return wasPressed;
        }

        private static bool DrawOnionToggle(bool value, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            GUIContent content = icon == null ? new GUIContent(value ? "●" : "○", tooltip) : new GUIContent(icon, tooltip);
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = value ? SelectedColor : Color.white;
            bool result = GUILayout.Toggle(value, content, EditorStyles.miniButton, GUILayout.Width(30f), GUILayout.Height(20f));
            GUI.backgroundColor = previousBackgroundColor;
            return result;
        }

        private static bool DrawOnionToggle(Rect rect, bool value, string iconName, string tooltip)
        {
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = value ? SelectedColor : Color.white;
            GUIContent content = icon == null ? new GUIContent(value ? "●" : "○", tooltip) : GUIContent.none;
            bool result = GUI.Toggle(rect, value, content, GUI.skin.button);
            GUI.backgroundColor = previousBackgroundColor;
            if (icon != null && Event.current.type == EventType.Repaint)
            {
                Rect iconRect = new Rect(rect.x + 2f, rect.y + 2f, rect.width - 4f, rect.height - 4f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(rect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            return result;
        }

        private sealed class AnimationTagEditorPopup : PopupWindowContent
        {
            private readonly ISpriteToolboxState state;
            private readonly ITimelineActions actions;
            private readonly int tagIndex;
            private string tagName;
            private int fromFrame;
            private int toFrame;
            private SpriteAnimationDirection direction;
            private Color color;

            public AnimationTagEditorPopup(ISpriteToolboxState state, ITimelineActions actions, int tagIndex)
            {
                this.state = state;
                this.actions = actions;
                this.tagIndex = tagIndex;
                if (tagIndex >= 0 && tagIndex < state.AnimationTags.Count)
                {
                    SpriteToolboxAnimationTagState tag = state.AnimationTags[tagIndex];
                    tagName = tag.Name;
                    fromFrame = tag.FromFrame;
                    toFrame = tag.ToFrame;
                    direction = tag.Direction;
                    color = tag.Color;
                }
                else
                {
                    tagName = "Tag";
                    fromFrame = state.SelectedFrameIndex;
                    toFrame = state.SelectedFrameIndex;
                    direction = SpriteAnimationDirection.Forward;
                    color = SpriteToolboxTagColorPalette.GetColor(state.AnimationTags.Count);
                }
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(260f, 176f);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Label(tagIndex < 0 ? "New Tag" : "Edit Tag", EditorStyles.boldLabel);
                tagName = EditorGUILayout.TextField("Name", tagName);
                int maximumFrameNumber = Mathf.Max(1, state.Frames.Count);
                float fromFrameNumber = Mathf.Clamp(fromFrame + 1, 1, maximumFrameNumber);
                float toFrameNumber = Mathf.Clamp(toFrame + 1, fromFrameNumber, maximumFrameNumber);
                EditorGUILayout.MinMaxSlider("Frame range", ref fromFrameNumber, ref toFrameNumber, 1f, maximumFrameNumber);
                fromFrame = Mathf.RoundToInt(fromFrameNumber) - 1;
                toFrame = Mathf.RoundToInt(toFrameNumber) - 1;
                EditorGUILayout.BeginHorizontal();
                GUILayout.Label($"From: {fromFrame + 1}", EditorStyles.miniLabel);
                GUILayout.FlexibleSpace();
                GUILayout.Label($"To: {toFrame + 1}", EditorStyles.miniLabel);
                EditorGUILayout.EndHorizontal();
                direction = (SpriteAnimationDirection)EditorGUILayout.EnumPopup("Direction", direction);
                color = EditorGUILayout.ColorField("Color", color);
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(72f)))
                {
                    editorWindow.Close();
                }

                GUI.enabled = !string.IsNullOrWhiteSpace(tagName);
                string confirmLabel = tagIndex < 0 ? "Create" : "Save";
                if (GUILayout.Button(confirmLabel, GUILayout.Width(72f)))
                {
                    bool wasSaved = tagIndex < 0
                        ? actions.AddAnimationTag(tagName.Trim(), fromFrame, toFrame, direction, color)
                        : actions.UpdateAnimationTag(tagIndex, tagName.Trim(), fromFrame, toFrame, direction, color);
                    if (wasSaved)
                    {
                        editorWindow.Close();
                    }
                }

                GUI.enabled = true;
                EditorGUILayout.EndHorizontal();
            }
        }

        private sealed class FramePropertiesPopup : PopupWindowContent
        {
            private const float MinimumDuration = 0.01f;
            private const float MaximumDuration = 1f;
            private readonly ISpriteToolboxState state;
            private readonly ITimelineActions actions;
            private readonly int frameIndex;
            private float duration;

            public FramePropertiesPopup(ISpriteToolboxState state, ITimelineActions actions, int frameIndex)
            {
                this.state = state;
                this.actions = actions;
                this.frameIndex = frameIndex;
                duration = frameIndex >= 0 && frameIndex < state.Frames.Count
                    ? state.Frames[frameIndex].Duration
                    : 0.1f;
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(240f, 126f);
            }

            public override void OnGUI(Rect rect)
            {
                GUILayout.Label($"Frame {frameIndex + 1} Properties", EditorStyles.boldLabel);
                duration = EditorGUILayout.FloatField("Duration", duration);
                duration = Mathf.Clamp(duration, MinimumDuration, MaximumDuration);
                GUILayout.Label("Duration slider", EditorStyles.miniLabel);
                duration = Mathf.Max(MinimumDuration, GUILayout.HorizontalSlider(duration, 0f, MaximumDuration));
                GUILayout.FlexibleSpace();
                EditorGUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Cancel", GUILayout.Width(72f)))
                {
                    editorWindow.Close();
                }

                if (GUILayout.Button("Save", GUILayout.Width(72f)))
                {
                    actions.SelectFrame(frameIndex);
                    if (actions.SetFrameDuration(duration))
                    {
                        editorWindow.Close();
                    }
                }

                EditorGUILayout.EndHorizontal();
            }
        }

    }
}
