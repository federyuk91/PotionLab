using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

public class AudioSplitterWindow : EditorWindow
{
    private const float WaveformHeight = 260f;
    private const float TimelineHeight = 22f;
    private const float CutDiamondSize = 9f;
    private const float CutDragDelaySeconds = 0.18f;
    private const float MinimumClipLengthSeconds = 0.02f;
    private const float MinimumHorizontalZoom = 1f;
    private const float MaximumHorizontalZoom = 10f;
    private const float MouseWheelZoomStep = 1.12f;
    private const float MinimumSilenceThreshold = 0.00001f;
    private const float SilenceToleranceHighlightSeconds = 0.8f;
    private const string PreviewFolderName = "__PreviewCache";
    private const string PreviewFileName = "AudioSplitterPreview.wav";
    private static readonly string[] LowerPanelTabs = { "Clips", "Cuts" };
    private static string cachedToolRootPath;

    private AudioClip sourceClip;
    private AudioClip previousSourceClip;
    private AudioSplitterProject currentProject;
    private float[] sourceSamples = Array.Empty<float>();
    private readonly List<int> cutSamples = new List<int>();
    private readonly List<string> segmentNames = new List<string>();
    private Texture2D waveformTexture;
    private int cachedWaveformWidth;
    private float waveformVerticalScale = 1f;

    private Vector2 waveformScroll;
    private Vector2 clipListScroll;
    private Vector2 cutListScroll;
    private float horizontalZoom = 1f;
    private float autoCutMinimumSilenceSeconds = 0.2f;
    private float silenceTolerancePercent = 2f;
    private double silenceToleranceHighlightUntil;
    private int selectedSegmentIndex = -1;
    private int lowerPanelTabIndex;
    private int pendingDragCutIndex = -1;
    private int draggingCutIndex = -1;
    private double cutMouseDownTime;

    private AudioClip previewClip;
    private double previewStopTime;
    private double previewStartTime;
    private double nextPreviewRepaintTime;
    private int previewSegmentIndex = -1;
    private bool isPreviewing;
    private bool isProjectDirty;

    [MenuItem("Tools/Audio Splitter")]
    public static void Open()
    {
        AudioSplitterWindow window = GetWindow<AudioSplitterWindow>("Audio Splitter");
        window.minSize = new Vector2(720f, 460f);
        window.UpdateWindowTitle();
        window.Show();
    }

    private void OnEnable()
    {
        EditorApplication.update += UpdatePreview;
        UpdateWindowTitle();
    }

    private void OnDisable()
    {
        EditorApplication.update -= UpdatePreview;
        StopPreview(false);
        DestroyPreviewClip();
        DestroyWaveformTexture();
        ReleaseLoadedClipData();
    }

    private void OnDestroy()
    {
        PromptSaveUnsavedProject();
        StopPreview(false);
        DestroyPreviewClip();
        DestroyWaveformTexture();
        ReleaseLoadedClipData();
    }

    private void OnGUI()
    {
        HandleKeyboardShortcuts();
        DrawToolbar();

        if (sourceClip == null)
        {
            EditorGUILayout.HelpBox("Select an AudioClip imported in the project.", MessageType.Info);
            return;
        }

        if (sourceClip != previousSourceClip)
        {
            LoadClipData();
        }

        if (sourceSamples.Length == 0)
        {
            EditorGUILayout.HelpBox("The selected clip has no readable sample data. If this is a compressed clip, set Load Type to Decompress On Load and reimport it.", MessageType.Warning);
            return;
        }

        DrawClipInfo();
        DrawWaveformSection();
        DrawSegmentControls();
        DrawLowerPanel();
    }

    private void HandleKeyboardShortcuts()
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.KeyDown || currentEvent.keyCode != KeyCode.Space)
        {
            return;
        }

        if (EditorGUIUtility.editingTextField || sourceClip == null || selectedSegmentIndex < 0)
        {
            return;
        }

        TogglePreview(selectedSegmentIndex);
        currentEvent.Use();
    }

    private void DrawToolbar()
    {
        using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
        {
            AudioClip selectedClip = (AudioClip)EditorGUILayout.ObjectField(sourceClip, typeof(AudioClip), false, GUILayout.MinWidth(220f));

            if (selectedClip != sourceClip)
            {
                SetSourceClip(selectedClip);
            }

            if (GUILayout.Button("Browse", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                BrowseForClip();
            }

            if (GUILayout.Button("Refresh", EditorStyles.toolbarButton, GUILayout.Width(64f)))
            {
                LoadClipData();
            }

            if (GUILayout.Button("Clear Cuts", EditorStyles.toolbarButton, GUILayout.Width(78f)))
            {
                cutSamples.Clear();
                selectedSegmentIndex = GetSegmentCount() > 0 ? 0 : -1;
                EnsureSegmentNames();
                MarkProjectDirty();
                Repaint();
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Open Project", EditorStyles.toolbarButton, GUILayout.Width(92f)))
            {
                OpenProject();
            }

            using (new EditorGUI.DisabledScope(sourceClip == null))
            {
                if (GUILayout.Button("Save Project", EditorStyles.toolbarButton, GUILayout.Width(88f)))
                {
                    SaveProject();
                }

                if (GUILayout.Button("Save Project As...", EditorStyles.toolbarButton, GUILayout.Width(112f)))
                {
                    SaveProjectAs();
                }
            }
        }
    }

    private void BrowseForClip()
    {
        string absolutePath = EditorUtility.OpenFilePanel("Select AudioClip", Application.dataPath, "wav,mp3,ogg,aif,aiff");

        if (string.IsNullOrEmpty(absolutePath))
        {
            return;
        }

        string projectPath = ToProjectPath(absolutePath);

        if (string.IsNullOrEmpty(projectPath))
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Select an audio file already inside this Unity project's Assets folder.", "OK");
            return;
        }

        AudioClip loadedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(projectPath);

        if (loadedClip == null)
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Unity could not load the selected file as an AudioClip. Check the import settings.", "OK");
            return;
        }

        SetSourceClip(loadedClip);
    }

    private void OpenProject()
    {
        string absolutePath = EditorUtility.OpenFilePanel("Open Audio Splitter Project", Application.dataPath, "asset");

        if (string.IsNullOrEmpty(absolutePath))
        {
            return;
        }

        string projectPath = ToProjectPath(absolutePath);

        if (string.IsNullOrEmpty(projectPath))
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Select an Audio Splitter project asset inside this Unity project's Assets folder.", "OK");
            return;
        }

        AudioSplitterProject project = AssetDatabase.LoadAssetAtPath<AudioSplitterProject>(projectPath);

        if (project == null)
        {
            EditorUtility.DisplayDialog("Audio Splitter", "The selected asset is not an Audio Splitter project.", "OK");
            return;
        }

        LoadProject(project);
    }

    private void SaveProject()
    {
        if (currentProject == null)
        {
            SaveProjectAs();
            return;
        }

        WriteStateToProject(currentProject);
        EditorUtility.SetDirty(currentProject);
        AssetDatabase.SaveAssets();
        SetProjectDirty(false);
        ShowNotification(new GUIContent($"Saved {currentProject.name}"));
    }

    private void SaveProjectAs()
    {
        if (sourceClip == null)
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Select an AudioClip before saving the project.", "OK");
            return;
        }

        string defaultName = currentProject == null ? $"{sourceClip.name}_AudioSplitterProject" : currentProject.name;
        string projectPath = EditorUtility.SaveFilePanelInProject("Save Audio Splitter Project", defaultName, "asset", "Choose where to save this Audio Splitter project.");

        if (string.IsNullOrEmpty(projectPath))
        {
            return;
        }

        AudioSplitterProject project = AssetDatabase.LoadAssetAtPath<AudioSplitterProject>(projectPath);

        if (project == null)
        {
            UnityEngine.Object existingAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(projectPath);

            if (existingAsset != null)
            {
                EditorUtility.DisplayDialog("Audio Splitter", "The selected asset already exists and is not an Audio Splitter project.", "OK");
                return;
            }

            project = CreateInstance<AudioSplitterProject>();
            AssetDatabase.CreateAsset(project, projectPath);
        }

        currentProject = project;
        WriteStateToProject(currentProject);
        EditorUtility.SetDirty(currentProject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetProjectDirty(false);
        ShowNotification(new GUIContent($"Saved {Path.GetFileName(projectPath)}"));
    }

    private void LoadProject(AudioSplitterProject project)
    {
        StopPreview(false);
        DestroyPreviewClip();
        DestroyWaveformTexture();

        currentProject = project;
        sourceClip = project.sourceClip;
        previousSourceClip = null;
        sourceSamples = Array.Empty<float>();
        cutSamples.Clear();
        segmentNames.Clear();

        if (sourceClip == null)
        {
            selectedSegmentIndex = -1;
            SetProjectDirty(false);
            Repaint();
            return;
        }

        LoadClipData();

        cutSamples.Clear();
        cutSamples.AddRange(project.cutSamples);
        cutSamples.Sort();
        RemoveInvalidCuts();

        segmentNames.Clear();
        segmentNames.AddRange(project.segmentNames);
        selectedSegmentIndex = Mathf.Clamp(project.selectedSegmentIndex, 0, GetSegmentCount() - 1);
        horizontalZoom = Mathf.Clamp(project.horizontalZoom, MinimumHorizontalZoom, MaximumHorizontalZoom);
        autoCutMinimumSilenceSeconds = Mathf.Max(0.01f, project.autoCutMinimumSilenceSeconds);
        silenceTolerancePercent = Mathf.Clamp(project.silenceTolerancePercent, 0f, 100f);
        waveformScroll = Vector2.zero;
        clipListScroll = Vector2.zero;
        cutListScroll = Vector2.zero;
        EnsureSegmentNames();
        SetProjectDirty(false);
        Repaint();
    }

    private void WriteStateToProject(AudioSplitterProject project)
    {
        EnsureSegmentNames();

        project.sourceClip = sourceClip;
        project.cutSamples.Clear();
        project.cutSamples.AddRange(cutSamples);
        project.segmentNames.Clear();
        project.segmentNames.AddRange(segmentNames);
        project.selectedSegmentIndex = selectedSegmentIndex;
        project.horizontalZoom = horizontalZoom;
        project.autoCutMinimumSilenceSeconds = autoCutMinimumSilenceSeconds;
        project.silenceTolerancePercent = silenceTolerancePercent;
    }

    private void PromptSaveUnsavedProject()
    {
        if (!isProjectDirty || EditorApplication.isCompiling || EditorApplication.isUpdating)
        {
            return;
        }

        int choice = EditorUtility.DisplayDialogComplex(
            "Unsaved Audio Splitter Project",
            "The Audio Splitter project has unsaved changes. Do you want to save them before closing?",
            "Save",
            "Don't Save",
            "Cancel");

        if (choice == 0)
        {
            SaveProject();
        }
        else if (choice == 2)
        {
            AudioSplitterWindow window = GetWindow<AudioSplitterWindow>("Audio Splitter");
            CopyStateTo(window);
            window.SetProjectDirty(true);
            window.Show();
        }
    }

    private void CopyStateTo(AudioSplitterWindow window)
    {
        window.currentProject = currentProject;
        window.sourceClip = sourceClip;
        window.previousSourceClip = null;
        window.sourceSamples = sourceSamples;
        window.cutSamples.Clear();
        window.cutSamples.AddRange(cutSamples);
        window.segmentNames.Clear();
        window.segmentNames.AddRange(segmentNames);
        window.waveformScroll = waveformScroll;
        window.clipListScroll = clipListScroll;
        window.cutListScroll = cutListScroll;
        window.horizontalZoom = horizontalZoom;
        window.selectedSegmentIndex = selectedSegmentIndex;
        window.lowerPanelTabIndex = lowerPanelTabIndex;
    }

    private void SetProjectDirty(bool dirty)
    {
        isProjectDirty = dirty;
        UpdateWindowTitle();
    }

    private void MarkProjectDirty()
    {
        SetProjectDirty(true);
    }

    private void UpdateWindowTitle()
    {
        titleContent = new GUIContent(isProjectDirty ? "Audio Splitter*" : "Audio Splitter");
    }

    private void DrawClipInfo()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField(sourceClip.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField("Project", currentProject == null ? "Unsaved" : AssetDatabase.GetAssetPath(currentProject));
            EditorGUILayout.LabelField("Length", FormatTime(sourceClip.length));
            EditorGUILayout.LabelField("Samples", $"{sourceClip.samples} @ {sourceClip.frequency} Hz, {sourceClip.channels} channel(s)");
            EditorGUILayout.LabelField("Horizontal Zoom", $"{horizontalZoom:0.00}x / {MaximumHorizontalZoom:0}x");
            DrawAutoCutControls();
        }
    }

    private void DrawAutoCutControls()
    {
        EditorGUILayout.Space(4f);

        using (new EditorGUILayout.HorizontalScope())
        {
            EditorGUILayout.LabelField("Auto-cut", EditorStyles.boldLabel, GUILayout.Width(58f));

            EditorGUILayout.LabelField("Min Silence (s)", GUILayout.Width(94f));
            EditorGUI.BeginChangeCheck();
            autoCutMinimumSilenceSeconds = EditorGUILayout.FloatField(Mathf.Max(0.01f, autoCutMinimumSilenceSeconds), GUILayout.Width(52f));
            if (EditorGUI.EndChangeCheck())
            {
                autoCutMinimumSilenceSeconds = Mathf.Max(0.01f, autoCutMinimumSilenceSeconds);
                MarkProjectDirty();
            }

            GUILayout.Space(10f);
            EditorGUILayout.LabelField("Noise Tolerance", GUILayout.Width(96f));
            EditorGUI.BeginChangeCheck();
            silenceTolerancePercent = GUILayout.HorizontalSlider(silenceTolerancePercent, 0f, 100f, GUILayout.Width(180f));
            silenceTolerancePercent = EditorGUILayout.FloatField(silenceTolerancePercent, GUILayout.Width(44f));
            EditorGUILayout.LabelField("%", GUILayout.Width(16f));

            if (EditorGUI.EndChangeCheck())
            {
                silenceTolerancePercent = Mathf.Clamp(silenceTolerancePercent, 0f, 100f);
                silenceToleranceHighlightUntil = EditorApplication.timeSinceStartup + SilenceToleranceHighlightSeconds;
                MarkProjectDirty();
                Repaint();
            }

            GUILayout.Space(10f);

            using (new EditorGUI.DisabledScope(sourceSamples.Length == 0))
            {
                if (GUILayout.Button("Auto-cut", GUILayout.Width(76f)))
                {
                    AutoCut();
                }
            }

            GUILayout.FlexibleSpace();
        }
    }

    private void DrawWaveformSection()
    {
        float viewportWidth = Mathf.Max(1f, position.width - 18f);
        ClampHorizontalZoom(viewportWidth);
        float contentWidth = Mathf.Max(viewportWidth, viewportWidth * horizontalZoom);
        Rect waveformRect = GUILayoutUtility.GetRect(viewportWidth, WaveformHeight + TimelineHeight);

        HandleWaveformZoom(waveformRect, viewportWidth);
        ClampHorizontalZoom(viewportWidth);
        contentWidth = Mathf.Max(viewportWidth, viewportWidth * horizontalZoom);
        waveformScroll.x = Mathf.Clamp(waveformScroll.x, 0f, Mathf.Max(0f, contentWidth - viewportWidth));

        Rect viewRect = new Rect(0f, 0f, contentWidth, WaveformHeight + TimelineHeight);

        waveformScroll = GUI.BeginScrollView(waveformRect, waveformScroll, viewRect, false, false);

        Rect timelineRect = new Rect(0f, 0f, contentWidth, TimelineHeight);
        Rect graphRect = new Rect(0f, TimelineHeight, contentWidth, WaveformHeight);
        AudioWaveformRenderer.DrawTimeline(timelineRect, sourceClip.length);
        DrawCachedWaveform(graphRect);
        DrawSilenceThreshold(graphRect);
        DrawSegments(graphRect);
        DrawCutLines(graphRect);
        DrawPlayhead(graphRect);
        HandleWaveformInput(graphRect);

        GUI.EndScrollView();
    }

    private void HandleWaveformZoom(Rect waveformRect, float viewportWidth)
    {
        Event currentEvent = Event.current;

        if (currentEvent.type != EventType.ScrollWheel || !waveformRect.Contains(currentEvent.mousePosition))
        {
            return;
        }

        float oldZoom = horizontalZoom;
        float oldContentWidth = Mathf.Max(viewportWidth, viewportWidth * oldZoom);
        float mouseXInViewport = currentEvent.mousePosition.x - waveformRect.x;
        float normalizedMousePosition = Mathf.Clamp01((waveformScroll.x + mouseXInViewport) / oldContentWidth);
        float zoomFactor = Mathf.Pow(MouseWheelZoomStep, -currentEvent.delta.y);
        float maximumAllowedZoom = GetMaximumAllowedHorizontalZoom(viewportWidth);

        horizontalZoom = Mathf.Clamp(horizontalZoom * zoomFactor, MinimumHorizontalZoom, maximumAllowedZoom);

        if (!Mathf.Approximately(oldZoom, horizontalZoom))
        {
            float newContentWidth = Mathf.Max(viewportWidth, viewportWidth * horizontalZoom);
            waveformScroll.x = Mathf.Clamp(normalizedMousePosition * newContentWidth - mouseXInViewport, 0f, Mathf.Max(0f, newContentWidth - viewportWidth));
            DestroyWaveformTexture();
            MarkProjectDirty();
            Repaint();
        }

        currentEvent.Use();
    }

    private void ClampHorizontalZoom(float viewportWidth)
    {
        float clampedZoom = Mathf.Clamp(horizontalZoom, MinimumHorizontalZoom, GetMaximumAllowedHorizontalZoom(viewportWidth));

        if (!Mathf.Approximately(horizontalZoom, clampedZoom))
        {
            horizontalZoom = clampedZoom;
            DestroyWaveformTexture();
        }
    }

    private void DrawSilenceThreshold(Rect graphRect)
    {
        float threshold = GetSilenceThreshold();
        float centerY = graphRect.y + graphRect.height * 0.5f;
        float offset = threshold * waveformVerticalScale * graphRect.height * 0.48f;
        offset = Mathf.Min(offset, graphRect.height * 0.5f);
        float upperY = centerY - offset;
        float lowerY = centerY + offset;
        bool isHighlighted = EditorApplication.timeSinceStartup < silenceToleranceHighlightUntil;

        if (isHighlighted)
        {
            EditorGUI.DrawRect(new Rect(graphRect.x, upperY, graphRect.width, Mathf.Max(1f, lowerY - upperY)), new Color(1f, 0.25f, 0.55f, 0.18f));
        }

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = isHighlighted ? new Color(1f, 0.55f, 0f, 0.95f) : new Color(1f, 0.55f, 0f, 0.28f);
        Handles.DrawLine(new Vector3(graphRect.x, upperY), new Vector3(graphRect.xMax, upperY));
        Handles.DrawLine(new Vector3(graphRect.x, lowerY), new Vector3(graphRect.xMax, lowerY));
        Handles.color = previousColor;
        Handles.EndGUI();
    }

    private float GetMaximumAllowedHorizontalZoom(float viewportWidth)
    {
        float maximumTextureZoom = Mathf.Max(MinimumHorizontalZoom, (SystemInfo.maxTextureSize - 1f) / Mathf.Max(1f, viewportWidth));
        return Mathf.Min(MaximumHorizontalZoom, maximumTextureZoom);
    }

    private void DrawCachedWaveform(Rect graphRect)
    {
        int maximumTextureWidth = Mathf.Max(1, SystemInfo.maxTextureSize - 1);
        int targetWidth = Mathf.Clamp(Mathf.RoundToInt(graphRect.width), 1, maximumTextureWidth);

        if (waveformTexture == null || cachedWaveformWidth != targetWidth)
        {
            DestroyWaveformTexture();
            waveformTexture = AudioWaveformRenderer.BuildWaveformTexture(targetWidth, Mathf.RoundToInt(graphRect.height), sourceSamples, sourceClip.channels, out waveformVerticalScale);
            cachedWaveformWidth = targetWidth;
        }

        if (waveformTexture != null)
        {
            GUI.DrawTexture(graphRect, waveformTexture, ScaleMode.StretchToFill);
        }
    }

    private void DrawSegments(Rect graphRect)
    {
        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegmentSamples(i, out int startSample, out int endSample);

            float startX = SampleToX(startSample, graphRect);
            float endX = SampleToX(endSample, graphRect);
            Rect segmentRect = new Rect(startX, graphRect.y, Mathf.Max(1f, endX - startX), graphRect.height);

            if (i == selectedSegmentIndex)
            {
                EditorGUI.DrawRect(segmentRect, new Color(0.25f, 0.47f, 0.87f, 0.22f));
            }
            else if (IsMouseInside(segmentRect))
            {
                EditorGUI.DrawRect(segmentRect, new Color(0.7f, 0.7f, 0.7f, 0.08f));
            }
        }
    }

    private void DrawCutLines(Rect graphRect)
    {
        Handles.BeginGUI();

        Color previousColor = Handles.color;
        Handles.color = Color.yellow;

        foreach (int cutSample in cutSamples)
        {
            float x = SampleToX(cutSample, graphRect);
            Handles.DrawLine(new Vector3(x, graphRect.y), new Vector3(x, graphRect.yMax));
            DrawCutDiamond(x, graphRect.y);
        }

        Handles.color = previousColor;
        Handles.EndGUI();
    }

    private static void DrawCutDiamond(float x, float y)
    {
        Vector3 top = new Vector3(x, y - CutDiamondSize);
        Vector3 right = new Vector3(x + CutDiamondSize, y);
        Vector3 bottom = new Vector3(x, y + CutDiamondSize);
        Vector3 left = new Vector3(x - CutDiamondSize, y);
        Handles.DrawAAConvexPolygon(top, right, bottom, left);
    }

    private void DrawPlayhead(Rect graphRect)
    {
        if (!isPreviewing || previewSegmentIndex < 0)
        {
            return;
        }

        GetSegmentSamples(previewSegmentIndex, out int startSample, out int endSample);
        int currentSample = Mathf.Clamp(startSample + Mathf.RoundToInt(GetPreviewElapsedSeconds() * sourceClip.frequency), startSample, endSample);
        float x = SampleToX(currentSample, graphRect);

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = Color.red;
        Handles.DrawLine(new Vector3(x, graphRect.y), new Vector3(x, graphRect.yMax));
        Handles.color = previousColor;
        Handles.EndGUI();
    }

    private void HandleWaveformInput(Rect graphRect)
    {
        Event currentEvent = Event.current;

        Rect interactionRect = new Rect(graphRect.x, graphRect.y - CutDiamondSize - 2f, graphRect.width, graphRect.height + CutDiamondSize + 2f);

        if (!interactionRect.Contains(currentEvent.mousePosition) && pendingDragCutIndex < 0 && draggingCutIndex < 0)
        {
            return;
        }

        int cutIndex = FindCutDiamondAtMouse(currentEvent.mousePosition, graphRect);

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 1 && cutIndex >= 0)
        {
            RemoveCut(cutIndex);
            currentEvent.Use();
            Repaint();
            return;
        }

        if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0)
        {
            if (cutIndex >= 0)
            {
                pendingDragCutIndex = cutIndex;
                draggingCutIndex = -1;
                cutMouseDownTime = EditorApplication.timeSinceStartup;
                currentEvent.Use();
                return;
            }

            if (!graphRect.Contains(currentEvent.mousePosition))
            {
                return;
            }

            int clickedSample = XToSample(currentEvent.mousePosition.x, graphRect);

            if (currentEvent.clickCount >= 2)
            {
                AddCut(clickedSample);
            }
            else
            {
                SelectSegment(FindSegmentIndex(clickedSample), true);
            }

            EnsureSegmentNames();
            GUI.FocusControl(null);
            currentEvent.Use();
            Repaint();
            return;
        }

        if (currentEvent.type == EventType.MouseDrag && currentEvent.button == 0 && pendingDragCutIndex >= 0)
        {
            if (EditorApplication.timeSinceStartup - cutMouseDownTime >= CutDragDelaySeconds)
            {
                draggingCutIndex = pendingDragCutIndex;
            }

            if (draggingCutIndex >= 0)
            {
                int draggedSample = XToSample(currentEvent.mousePosition.x, graphRect);
                MoveCut(draggingCutIndex, draggedSample);
                selectedSegmentIndex = Mathf.Clamp(draggingCutIndex, 0, GetSegmentCount() - 1);
                currentEvent.Use();
                Repaint();
            }

            return;
        }

        if (currentEvent.type == EventType.MouseUp && currentEvent.button == 0 && (pendingDragCutIndex >= 0 || draggingCutIndex >= 0))
        {
            pendingDragCutIndex = -1;
            draggingCutIndex = -1;
            currentEvent.Use();
            Repaint();
        }
    }

    private int FindCutDiamondAtMouse(Vector2 mousePosition, Rect graphRect)
    {
        for (int i = 0; i < cutSamples.Count; i++)
        {
            float cutX = SampleToX(cutSamples[i], graphRect);
            Rect hitRect = new Rect(cutX - CutDiamondSize - 2f, graphRect.y - CutDiamondSize - 2f, (CutDiamondSize + 2f) * 2f, (CutDiamondSize + 2f) * 2f);

            if (hitRect.Contains(mousePosition))
            {
                return i;
            }
        }

        return -1;
    }

    private void RemoveCut(int cutIndex)
    {
        cutSamples.RemoveAt(cutIndex);

        if (segmentNames.Count > cutIndex + 1)
        {
            segmentNames.RemoveAt(cutIndex + 1);
        }

        selectedSegmentIndex = Mathf.Clamp(selectedSegmentIndex, 0, GetSegmentCount() - 1);
        EnsureSegmentNames();
        MarkProjectDirty();
    }

    private void MoveCut(int cutIndex, int sample)
    {
        int minimumDistanceSamples = GetMinimumDistanceSamples();
        int previousLimit = cutIndex == 0 ? minimumDistanceSamples : cutSamples[cutIndex - 1] + minimumDistanceSamples;
        int nextLimit = cutIndex == cutSamples.Count - 1 ? sourceClip.samples - minimumDistanceSamples : cutSamples[cutIndex + 1] - minimumDistanceSamples;
        int clampedSample = Mathf.Clamp(sample, previousLimit, nextLimit);

        if (cutSamples[cutIndex] == clampedSample)
        {
            return;
        }

        cutSamples[cutIndex] = clampedSample;
        MarkProjectDirty();
    }

    private void AutoCut()
    {
        if (sourceClip == null || sourceSamples.Length == 0)
        {
            return;
        }

        List<int> detectedCuts = DetectSilentIntervalCuts();
        cutSamples.Clear();
        cutSamples.AddRange(detectedCuts);
        cutSamples.Sort();
        RemoveInvalidCuts();

        segmentNames.Clear();
        EnsureSegmentNames();
        selectedSegmentIndex = GetSegmentCount() > 0 ? 0 : -1;
        lowerPanelTabIndex = 0;
        MarkProjectDirty();
        Repaint();
        ShowNotification(new GUIContent($"Auto-cut created {cutSamples.Count} cut(s)."));
    }

    private List<int> DetectSilentIntervalCuts()
    {
        List<int> detectedCuts = new List<int>();
        float threshold = GetSilenceThreshold();
        int minimumSilenceSamples = Mathf.Max(1, Mathf.RoundToInt(autoCutMinimumSilenceSeconds * sourceClip.frequency));
        int silenceStartSample = -1;

        for (int sample = 0; sample < sourceClip.samples; sample++)
        {
            bool isSilent = GetFramePeak(sample) <= threshold;

            if (isSilent && silenceStartSample < 0)
            {
                silenceStartSample = sample;
            }
            else if (!isSilent && silenceStartSample >= 0)
            {
                AddDetectedCutIfSilenceIsLongEnough(detectedCuts, silenceStartSample, sample, minimumSilenceSamples);
                silenceStartSample = -1;
            }
        }

        if (silenceStartSample >= 0)
        {
            AddDetectedCutIfSilenceIsLongEnough(detectedCuts, silenceStartSample, sourceClip.samples, minimumSilenceSamples);
        }

        return detectedCuts;
    }

    private void AddDetectedCutIfSilenceIsLongEnough(List<int> detectedCuts, int startSample, int endSample, int minimumSilenceSamples)
    {
        int silenceLength = endSample - startSample;

        if (silenceLength < minimumSilenceSamples)
        {
            return;
        }

        int cutSample = startSample + silenceLength / 2;
        int minimumDistanceSamples = GetMinimumDistanceSamples();

        if (cutSample < minimumDistanceSamples || cutSample > sourceClip.samples - minimumDistanceSamples)
        {
            return;
        }

        if (detectedCuts.Count > 0 && cutSample - detectedCuts[detectedCuts.Count - 1] < minimumDistanceSamples)
        {
            return;
        }

        detectedCuts.Add(cutSample);
    }

    private float GetFramePeak(int sample)
    {
        int sampleOffset = sample * sourceClip.channels;
        float peak = 0f;

        for (int channel = 0; channel < sourceClip.channels; channel++)
        {
            peak = Mathf.Max(peak, Mathf.Abs(sourceSamples[sampleOffset + channel]));
        }

        return peak;
    }

    private float GetSilenceThreshold()
    {
        if (silenceTolerancePercent >= 100f)
        {
            return 1.01f;
        }

        float normalizedTolerance = Mathf.Clamp01(silenceTolerancePercent / 100f);
        return Mathf.Lerp(MinimumSilenceThreshold, 1f, normalizedTolerance);
    }

    private void DrawSegmentControls()
    {
        int segmentCount = GetSegmentCount();
        EnsureSegmentNames();

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            EditorGUILayout.LabelField("Segments", EditorStyles.boldLabel);

            if (segmentCount == 0)
            {
                EditorGUILayout.HelpBox("No segments available.", MessageType.Info);
                return;
            }

            selectedSegmentIndex = Mathf.Clamp(selectedSegmentIndex, 0, segmentCount - 1);
            GetSegmentSamples(selectedSegmentIndex, out int startSample, out int endSample);
            bool selectedClipIsPlaying = isPreviewing && previewSegmentIndex == selectedSegmentIndex;

            EditorGUILayout.LabelField("Selected", $"Clip {selectedSegmentIndex}: {FormatSampleTime(startSample)} - {FormatSampleTime(endSample)}");
            EditorGUI.BeginChangeCheck();
            string clipName = EditorGUILayout.TextField("Clip Name", segmentNames[selectedSegmentIndex]);

            if (EditorGUI.EndChangeCheck())
            {
                segmentNames[selectedSegmentIndex] = clipName;
                MarkProjectDirty();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button(selectedClipIsPlaying ? "Stop" : "Play", GUILayout.Width(72f)))
                {
                    TogglePreview(selectedSegmentIndex);
                }

                if (selectedClipIsPlaying)
                {
                    EditorGUILayout.LabelField($"{FormatPlaybackTime(GetPreviewElapsedSeconds())} / {FormatPlaybackTime(GetSegmentSeconds(startSample, endSample))}", GUILayout.Width(110f));
                }
                else
                {
                    GUILayout.Space(110f);
                }

                if (GUILayout.Button("Save As...", GUILayout.Width(100f)))
                {
                    SaveSelectedSegmentAs();
                }

                if (GUILayout.Button("Split All", GUILayout.Width(90f)))
                {
                    SplitAllSegments();
                }
            }
        }
    }

    private void DrawLowerPanel()
    {
        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            lowerPanelTabIndex = GUILayout.Toolbar(lowerPanelTabIndex, LowerPanelTabs, EditorStyles.toolbarButton);

            if (lowerPanelTabIndex == 0)
            {
                DrawSegmentList();
            }
            else
            {
                DrawCutList();
            }
        }
    }

    private void DrawSegmentList()
    {
        int segmentCount = GetSegmentCount();
        EnsureSegmentNames();

        clipListScroll = EditorGUILayout.BeginScrollView(clipListScroll, GUILayout.MinHeight(110f), GUILayout.MaxHeight(220f));

        if (segmentCount == 0)
        {
            EditorGUILayout.LabelField("No clips available.");
            EditorGUILayout.EndScrollView();
            return;
        }

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegmentSamples(i, out int startSample, out int endSample);
            bool rowIsPlaying = isPreviewing && previewSegmentIndex == i;

            Rect rowRect = EditorGUILayout.BeginHorizontal();

            if (Event.current.type == EventType.Repaint && i == selectedSegmentIndex)
            {
                EditorGUI.DrawRect(rowRect, new Color(0.25f, 0.47f, 0.87f, 0.28f));
            }

            if (GUILayout.Button($"Clip {i}", GUILayout.Width(64f)))
            {
                SelectSegment(i, false);
                Repaint();
            }

            EditorGUILayout.LabelField(segmentNames[i], GUILayout.Width(180f));
            EditorGUILayout.LabelField($"{FormatSampleTime(startSample)} - {FormatSampleTime(endSample)}", GUILayout.Width(160f));
            EditorGUILayout.LabelField($"Duration {FormatSegmentDuration(startSample, endSample)}", GUILayout.Width(110f));

            if (rowIsPlaying)
            {
                EditorGUILayout.LabelField($"{FormatPlaybackTime(GetPreviewElapsedSeconds())} / {FormatPlaybackTime(GetSegmentSeconds(startSample, endSample))}", GUILayout.Width(110f));
            }
            else
            {
                GUILayout.Space(110f);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawCutList()
    {
        cutListScroll = EditorGUILayout.BeginScrollView(cutListScroll, GUILayout.MinHeight(110f), GUILayout.MaxHeight(220f));

        if (cutSamples.Count == 0)
        {
            EditorGUILayout.LabelField("Double-click the waveform to add yellow cut lines.");
            EditorGUILayout.EndScrollView();
            return;
        }

        for (int i = 0; i < cutSamples.Count; i++)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"Cut {i}", FormatSampleTime(cutSamples[i]));

                if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                {
                    RemoveCut(i);
                    GUIUtility.ExitGUI();
                }
            }
        }

        EditorGUILayout.EndScrollView();
    }

    private void SetSourceClip(AudioClip clip)
    {
        StopPreview(false);
        DestroyPreviewClip();
        DestroyWaveformTexture();

        currentProject = null;
        sourceClip = clip;
        previousSourceClip = null;
        sourceSamples = Array.Empty<float>();
        cutSamples.Clear();
        segmentNames.Clear();
        selectedSegmentIndex = -1;
        SetProjectDirty(clip != null);

        Repaint();
    }

    private void LoadClipData()
    {
        previousSourceClip = sourceClip;
        sourceSamples = Array.Empty<float>();
        StopPreview(false);
        DestroyPreviewClip();
        DestroyWaveformTexture();

        if (sourceClip == null)
        {
            return;
        }

        float[] samples = new float[sourceClip.samples * sourceClip.channels];

        try
        {
            if (!sourceClip.GetData(samples, 0))
            {
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning($"Audio Splitter could not read '{sourceClip.name}'. Set Load Type to Decompress On Load. {exception.Message}");
            return;
        }

        sourceSamples = samples;
        selectedSegmentIndex = GetSegmentCount() > 0 ? 0 : -1;
        EnsureSegmentNames();
    }

    private void AddCut(int sample)
    {
        int clampedSample = Mathf.Clamp(sample, 1, sourceClip.samples - 1);

        if (IsTooCloseToExistingCut(clampedSample))
        {
            return;
        }

        int insertIndex = cutSamples.BinarySearch(clampedSample);

        if (insertIndex < 0)
        {
            insertIndex = ~insertIndex;
        }

        cutSamples.Add(clampedSample);
        cutSamples.Sort();

        EnsureSegmentNames();
        segmentNames.Insert(insertIndex + 1, GetDefaultSegmentName(insertIndex + 1));
        EnsureSegmentNames();
        MarkProjectDirty();
    }

    private bool IsTooCloseToExistingCut(int sample)
    {
        int minimumDistanceSamples = GetMinimumDistanceSamples();

        foreach (int cutSample in cutSamples)
        {
            if (Mathf.Abs(cutSample - sample) < minimumDistanceSamples)
            {
                return true;
            }
        }

        return sample < minimumDistanceSamples || sourceClip.samples - sample < minimumDistanceSamples;
    }

    private void RemoveInvalidCuts()
    {
        if (sourceClip == null)
        {
            cutSamples.Clear();
            return;
        }

        int minimumDistanceSamples = GetMinimumDistanceSamples();

        for (int i = cutSamples.Count - 1; i >= 0; i--)
        {
            int cutSample = cutSamples[i];
            bool isOutOfRange = cutSample < minimumDistanceSamples || cutSample > sourceClip.samples - minimumDistanceSamples;
            bool overlapsPrevious = i > 0 && cutSample - cutSamples[i - 1] < minimumDistanceSamples;

            if (isOutOfRange || overlapsPrevious)
            {
                cutSamples.RemoveAt(i);
            }
        }
    }

    private int GetMinimumDistanceSamples()
    {
        return Mathf.Max(1, Mathf.RoundToInt(MinimumClipLengthSeconds * sourceClip.frequency));
    }

    private int FindSegmentIndex(int sample)
    {
        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegmentSamples(i, out int startSample, out int endSample);

            if (sample >= startSample && sample <= endSample)
            {
                return i;
            }
        }

        return segmentCount > 0 ? segmentCount - 1 : -1;
    }

    private void SelectSegment(int segmentIndex, bool revealInClipList)
    {
        int segmentCount = GetSegmentCount();
        int clampedSegmentIndex = Mathf.Clamp(segmentIndex, 0, segmentCount - 1);

        if (selectedSegmentIndex != clampedSegmentIndex)
        {
            selectedSegmentIndex = clampedSegmentIndex;
            MarkProjectDirty();
        }
        else
        {
            selectedSegmentIndex = clampedSegmentIndex;
        }

        if (!revealInClipList)
        {
            return;
        }

        lowerPanelTabIndex = 0;
        clipListScroll.y = Mathf.Max(0f, selectedSegmentIndex * EditorGUIUtility.singleLineHeight - EditorGUIUtility.singleLineHeight);
    }

    private int GetSegmentCount()
    {
        return sourceClip == null ? 0 : cutSamples.Count + 1;
    }

    private void GetSegmentSamples(int segmentIndex, out int startSample, out int endSample)
    {
        startSample = segmentIndex == 0 ? 0 : cutSamples[segmentIndex - 1];
        endSample = segmentIndex >= cutSamples.Count ? sourceClip.samples : cutSamples[segmentIndex];
    }

    private float SampleToX(int sample, Rect graphRect)
    {
        return graphRect.x + graphRect.width * sample / Mathf.Max(1f, sourceClip.samples);
    }

    private int XToSample(float x, Rect graphRect)
    {
        float normalizedX = Mathf.InverseLerp(graphRect.x, graphRect.xMax, x);
        return Mathf.Clamp(Mathf.RoundToInt(normalizedX * sourceClip.samples), 0, sourceClip.samples);
    }

    private void TogglePreview(int segmentIndex)
    {
        if (isPreviewing && previewSegmentIndex == segmentIndex)
        {
            StopPreview();
            return;
        }

        GetSegmentSamples(segmentIndex, out int startSample, out int endSample);
        PlayPreview(segmentIndex, startSample, endSample);
    }

    private void PlayPreview(int segmentIndex, int startSample, int endSample)
    {
        StopPreview(false);

        int segmentSamples = Mathf.Max(1, endSample - startSample);
        float[] segmentData = new float[segmentSamples * sourceClip.channels];
        Array.Copy(sourceSamples, startSample * sourceClip.channels, segmentData, 0, segmentData.Length);

        EnsurePreviewFolder();
        string previewAssetPath = GetPreviewAssetPath();
        WavWriter.Write(previewAssetPath, segmentData, sourceClip.channels, sourceClip.frequency);
        AssetDatabase.ImportAsset(previewAssetPath, ImportAssetOptions.ForceUpdate);
        previewClip = AssetDatabase.LoadAssetAtPath<AudioClip>(previewAssetPath);

        if (previewClip == null)
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Unity could not import the temporary preview clip.", "OK");
            DestroyPreviewClip();
            return;
        }

        if (!AudioPreviewUtility.Play(previewClip))
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Unity editor audio preview is not available in this editor session.", "OK");
            DestroyPreviewClip();
            return;
        }

        isPreviewing = true;
        previewSegmentIndex = segmentIndex;
        previewStartTime = EditorApplication.timeSinceStartup;
        previewStopTime = EditorApplication.timeSinceStartup + (double)segmentSamples / sourceClip.frequency;
        nextPreviewRepaintTime = 0d;
        Repaint();
    }

    private void StopPreview(bool repaint = true)
    {
        AudioPreviewUtility.StopAll();
        isPreviewing = false;
        previewStopTime = 0d;
        previewStartTime = 0d;
        previewSegmentIndex = -1;
        DestroyPreviewClip();

        if (repaint)
        {
            Repaint();
        }
    }

    private void UpdatePreview()
    {
        if (!isPreviewing)
        {
            if (EditorApplication.timeSinceStartup < silenceToleranceHighlightUntil && EditorApplication.timeSinceStartup >= nextPreviewRepaintTime)
            {
                nextPreviewRepaintTime = EditorApplication.timeSinceStartup + 0.1d;
                Repaint();
            }

            return;
        }

        AudioPreviewUtility.UpdateAudio();

        if (EditorApplication.timeSinceStartup >= previewStopTime)
        {
            StopPreview();
            return;
        }

        if (EditorApplication.timeSinceStartup >= nextPreviewRepaintTime)
        {
            nextPreviewRepaintTime = EditorApplication.timeSinceStartup + 0.1d;
            Repaint();
        }
    }

    private void DestroyPreviewClip()
    {
        previewClip = null;
        DeletePreviewAsset();
    }

    private static void EnsurePreviewFolder()
    {
        string previewFolderPath = GetPreviewFolderPath();

        if (AssetDatabase.IsValidFolder(previewFolderPath))
        {
            return;
        }

        AssetDatabase.CreateFolder(GetToolRootPath(), PreviewFolderName);
    }

    private static void DeletePreviewAsset()
    {
        string previewAssetPath = GetPreviewAssetPath();
        string previewFolderPath = GetPreviewFolderPath();
        AssetDatabase.DeleteAsset(previewAssetPath);

        string absolutePreviewPath = Path.GetFullPath(previewAssetPath);
        string absolutePreviewMetaPath = absolutePreviewPath + ".meta";

        if (File.Exists(absolutePreviewPath))
        {
            File.Delete(absolutePreviewPath);
        }

        if (File.Exists(absolutePreviewMetaPath))
        {
            File.Delete(absolutePreviewMetaPath);
        }

        if (AssetDatabase.IsValidFolder(previewFolderPath))
        {
            string absolutePreviewFolder = Path.GetFullPath(previewFolderPath);
            string[] childFiles = Directory.Exists(absolutePreviewFolder) ? Directory.GetFiles(absolutePreviewFolder) : Array.Empty<string>();
            string[] childDirectories = Directory.Exists(absolutePreviewFolder) ? Directory.GetDirectories(absolutePreviewFolder) : Array.Empty<string>();

            if (childFiles.Length == 0 && childDirectories.Length == 0)
            {
                AssetDatabase.DeleteAsset(previewFolderPath);
            }
        }
    }

    private static string GetPreviewAssetPath()
    {
        return $"{GetPreviewFolderPath()}/{PreviewFileName}";
    }

    private static string GetPreviewFolderPath()
    {
        return $"{GetToolRootPath()}/{PreviewFolderName}";
    }

    private static string GetToolRootPath()
    {
        if (!string.IsNullOrEmpty(cachedToolRootPath))
        {
            return cachedToolRootPath;
        }

        string[] scriptGuids = AssetDatabase.FindAssets($"{nameof(AudioSplitterWindow)} t:MonoScript");

        foreach (string scriptGuid in scriptGuids)
        {
            string scriptPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
            MonoScript script = AssetDatabase.LoadAssetAtPath<MonoScript>(scriptPath);

            if (script != null && script.GetClass() == typeof(AudioSplitterWindow))
            {
                cachedToolRootPath = Path.GetDirectoryName(scriptPath).Replace('\\', '/');
                return cachedToolRootPath;
            }
        }

        return "Assets";
    }

    private void DestroyWaveformTexture()
    {
        if (waveformTexture == null)
        {
            return;
        }

        DestroyImmediate(waveformTexture);
        waveformTexture = null;
        cachedWaveformWidth = 0;
        waveformVerticalScale = 1f;
    }

    private void ReleaseLoadedClipData()
    {
        sourceSamples = Array.Empty<float>();
        previousSourceClip = null;
    }

    private void SaveSelectedSegmentAs()
    {
        if (sourceClip == null || selectedSegmentIndex < 0)
        {
            return;
        }

        GetSegmentSamples(selectedSegmentIndex, out int startSample, out int endSample);
        string defaultName = GetSafeSegmentName(selectedSegmentIndex);
        string absolutePath = EditorUtility.SaveFilePanelInProject("Save audio clip as WAV", defaultName, "wav", "Choose where to save the selected audio clip.");

        if (string.IsNullOrEmpty(absolutePath))
        {
            return;
        }

        SaveSegment(startSample, endSample, absolutePath);
    }

    private void SplitAllSegments()
    {
        if (sourceClip == null)
        {
            return;
        }

        string folder = EditorUtility.OpenFolderPanel("Choose output folder inside Assets", Application.dataPath, string.Empty);

        if (string.IsNullOrEmpty(folder))
        {
            return;
        }

        string projectFolder = ToProjectPath(folder);

        if (string.IsNullOrEmpty(projectFolder))
        {
            EditorUtility.DisplayDialog("Audio Splitter", "Choose a folder inside this Unity project's Assets folder.", "OK");
            return;
        }

        int segmentCount = GetSegmentCount();

        for (int i = 0; i < segmentCount; i++)
        {
            GetSegmentSamples(i, out int startSample, out int endSample);
            string outputPath = AssetDatabase.GenerateUniqueAssetPath($"{projectFolder}/{GetSafeSegmentName(i)}.wav");
            SaveSegment(startSample, endSample, outputPath);
        }

        AssetDatabase.Refresh();
        ShowNotification(new GUIContent($"Saved {segmentCount} audio clips."));
    }

    private void SaveSegment(int startSample, int endSample, string projectPath)
    {
        int segmentSamples = Mathf.Max(1, endSample - startSample);
        float[] segmentData = new float[segmentSamples * sourceClip.channels];
        Array.Copy(sourceSamples, startSample * sourceClip.channels, segmentData, 0, segmentData.Length);

        WavWriter.Write(projectPath, segmentData, sourceClip.channels, sourceClip.frequency);
        AssetDatabase.ImportAsset(projectPath, ImportAssetOptions.ForceUpdate);
        ShowNotification(new GUIContent($"Saved {Path.GetFileName(projectPath)}"));
    }

    private string FormatSampleTime(int sample)
    {
        return FormatTime((float)sample / sourceClip.frequency);
    }

    private void EnsureSegmentNames()
    {
        int segmentCount = GetSegmentCount();

        while (segmentNames.Count < segmentCount)
        {
            segmentNames.Add(GetDefaultSegmentName(segmentNames.Count));
        }

        while (segmentNames.Count > segmentCount)
        {
            segmentNames.RemoveAt(segmentNames.Count - 1);
        }

        if (selectedSegmentIndex >= segmentCount)
        {
            selectedSegmentIndex = Mathf.Clamp(selectedSegmentIndex, 0, segmentCount - 1);
        }
    }

    private string GetDefaultSegmentName(int segmentIndex)
    {
        return sourceClip == null ? $"clip_{segmentIndex}" : $"{sourceClip.name}_clip_{segmentIndex}";
    }

    private string GetSafeSegmentName(int segmentIndex)
    {
        EnsureSegmentNames();
        string segmentName = segmentNames[segmentIndex];

        if (string.IsNullOrWhiteSpace(segmentName))
        {
            segmentName = GetDefaultSegmentName(segmentIndex);
        }

        foreach (char invalidChar in Path.GetInvalidFileNameChars())
        {
            segmentName = segmentName.Replace(invalidChar, '_');
        }

        return segmentName;
    }

    private string FormatSegmentDuration(int startSample, int endSample)
    {
        return FormatPlaybackTime(GetSegmentSeconds(startSample, endSample));
    }

    private float GetSegmentSeconds(int startSample, int endSample)
    {
        return Mathf.Max(0f, (float)(endSample - startSample) / sourceClip.frequency);
    }

    private float GetPreviewElapsedSeconds()
    {
        if (!isPreviewing)
        {
            return 0f;
        }

        return Mathf.Max(0f, (float)(EditorApplication.timeSinceStartup - previewStartTime));
    }

    private static string FormatTime(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalMinutes:00}:{timeSpan.Seconds:00}.{timeSpan.Milliseconds:000}";
    }

    private static string FormatPlaybackTime(float seconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(seconds);
        return $"{(int)timeSpan.TotalMinutes:00}:{timeSpan.Seconds:00}";
    }

    private static bool IsMouseInside(Rect rect)
    {
        return rect.Contains(Event.current.mousePosition);
    }

    private static string ToProjectPath(string absolutePath)
    {
        string normalizedPath = absolutePath.Replace('\\', '/');
        string normalizedDataPath = Application.dataPath.Replace('\\', '/');

        if (!normalizedPath.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
        {
            return string.Empty;
        }

        return "Assets" + normalizedPath.Substring(normalizedDataPath.Length);
    }

    private static class AudioPreviewUtility
    {
        private static readonly Type AudioUtilType = typeof(AudioImporter).Assembly.GetType("UnityEditor.AudioUtil");
        private static readonly MethodInfo StopAllPreviewClipsMethod = GetMethod("StopAllPreviewClips");
        private static readonly MethodInfo StopAllClipsMethod = GetMethod("StopAllClips");
        private static readonly MethodInfo UpdateAudioMethod = GetMethod("UpdateAudio");
        private static readonly MethodInfo PlayPreviewClipMethod = GetAudioClipMethod("PlayPreviewClip");
        private static readonly MethodInfo PlayClipMethod = GetAudioClipMethod("PlayClip");

        public static bool Play(AudioClip clip)
        {
            if (AudioUtilType == null)
            {
                return false;
            }

            if (PlayPreviewClipMethod != null)
            {
                return TryInvoke(PlayPreviewClipMethod, clip);
            }

            if (PlayClipMethod != null)
            {
                return TryInvoke(PlayClipMethod, clip);
            }

            return false;
        }

        public static void StopAll()
        {
            if (StopAllPreviewClipsMethod != null)
            {
                StopAllPreviewClipsMethod.Invoke(null, null);
                return;
            }

            if (StopAllClipsMethod != null)
            {
                StopAllClipsMethod.Invoke(null, null);
            }
        }

        public static void UpdateAudio()
        {
            if (UpdateAudioMethod != null)
            {
                UpdateAudioMethod.Invoke(null, null);
            }
        }

        private static MethodInfo GetMethod(string methodName, params Type[] parameterTypes)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            if (parameterTypes == null || parameterTypes.Length == 0)
            {
                return AudioUtilType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            }

            return AudioUtilType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, null, parameterTypes, null);
        }

        private static MethodInfo GetAudioClipMethod(string methodName)
        {
            if (AudioUtilType == null)
            {
                return null;
            }

            MethodInfo[] methods = AudioUtilType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

            foreach (MethodInfo method in methods)
            {
                if (method.Name != methodName)
                {
                    continue;
                }

                ParameterInfo[] parameters = method.GetParameters();

                if (parameters.Length > 0 && parameters[0].ParameterType == typeof(AudioClip))
                {
                    return method;
                }
            }

            return null;
        }

        private static bool TryInvoke(MethodInfo method, AudioClip clip)
        {
            try
            {
                ParameterInfo[] parameters = method.GetParameters();
                object[] values = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    Type parameterType = parameters[i].ParameterType;

                    if (parameterType == typeof(AudioClip))
                    {
                        values[i] = clip;
                    }
                    else if (parameterType == typeof(int))
                    {
                        values[i] = 0;
                    }
                    else if (parameterType == typeof(bool))
                    {
                        values[i] = false;
                    }
                    else
                    {
                        values[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
                    }
                }

                method.Invoke(null, values);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
