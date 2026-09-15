using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public enum SpriteToolboxColorChannelFormat { Rgb255, Rgb01, Hsv }

    public sealed class SpritePaletteImportResult
    {
        public SpritePaletteAsset Asset { get; }
        public IReadOnlyList<Color> Colors { get; }

        public SpritePaletteImportResult(SpritePaletteAsset asset, IReadOnlyList<Color> colors)
        {
            Asset = asset;
            Colors = colors ?? throw new ArgumentNullException(nameof(colors));
        }
    }

    [Serializable]
    public sealed class SpriteToolboxPalettePanelSettings
    {
        public SpritePaletteAsset PresetPalette;
        public PaletteHarmonyMode Harmony = PaletteHarmonyMode.Analogous;
        public int GeneratedColorCount = 8;
        public int PlanetarySeed = 1234;
        public float PlanetaryBaseHue = 0.58f;
        public float PlanetaryHueVariation = 0.08f;
        public Vector2 PlanetarySaturationRange = new Vector2(0.2f, 0.9f);
        public Vector2 PlanetaryValueRange = new Vector2(0.5f, 1f);
        public List<Color> RecentColors = new List<Color>();
        public SpriteToolboxColorChannelFormat ColorChannelFormat = SpriteToolboxColorChannelFormat.Rgb255;
        public bool ShowPicker = true;
        public bool ShowPaletteGenerator;
        public bool HasInitializedPickerVisibility;
    }

    public sealed class SpritePalettePresetRepository : IDisposable
    {
        private readonly string presetFolder;
        private List<SpritePaletteAsset> cachedPresets;
        private bool isCacheDirty = true;

        public SpritePalettePresetRepository(string presetFolder)
        {
            this.presetFolder = presetFolder;
            EditorApplication.projectChanged += Invalidate;
        }

        public IReadOnlyList<SpritePaletteAsset> GetPresets()
        {
            if (!isCacheDirty && cachedPresets != null) return cachedPresets;

            string[] assetGuids = AssetDatabase.FindAssets("t:SpritePaletteAsset", new[] { presetFolder });
            List<SpritePaletteAsset> presets = new List<SpritePaletteAsset>(assetGuids.Length);
            foreach (string assetGuid in assetGuids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(assetGuid);
                SpritePaletteAsset preset = AssetDatabase.LoadAssetAtPath<SpritePaletteAsset>(assetPath);
                if (preset != null) presets.Add(preset);
            }

            presets.Sort((first, second) => string.CompareOrdinal(first.name, second.name));
            cachedPresets = presets;
            isCacheDirty = false;
            return cachedPresets;
        }

        public SpritePaletteImportResult Import()
        {
            string absolutePath = EditorUtility.OpenFilePanelWithFilters(
                "Import palette",
                Application.dataPath,
                new[]
                {
                    "All supported formats", "asset,gpl",
                    "Sprite Toolbox palette asset", "asset",
                    "GIMP palette", "gpl"
                });
            if (string.IsNullOrEmpty(absolutePath)) return null;

            string extension = Path.GetExtension(absolutePath);
            if (string.Equals(extension, ".gpl", StringComparison.OrdinalIgnoreCase))
            {
                List<Color> colors;
                try
                {
                    colors = ParseGimpPalette(absolutePath);
                }
                catch (IOException exception)
                {
                    EditorUtility.DisplayDialog("Import palette", "Unable to read the selected GPL file.\n\n" + exception.Message, "OK");
                    return null;
                }
                catch (UnauthorizedAccessException exception)
                {
                    EditorUtility.DisplayDialog("Import palette", "Unable to access the selected GPL file.\n\n" + exception.Message, "OK");
                    return null;
                }

                if (colors.Count == 0)
                {
                    EditorUtility.DisplayDialog("Import palette", "The selected GPL file contains no valid RGB colors.", "OK");
                    return null;
                }

                return new SpritePaletteImportResult(null, colors);
            }

            string assetPath = FileUtil.GetProjectRelativePath(absolutePath);
            SpritePaletteAsset importedPalette = AssetDatabase.LoadAssetAtPath<SpritePaletteAsset>(assetPath);
            if (importedPalette == null) EditorUtility.DisplayDialog("Import palette", "Select a SpritePaletteAsset located inside this Unity project.", "OK");
            return importedPalette == null ? null : new SpritePaletteImportResult(importedPalette, importedPalette.Colors);
        }

        internal static List<Color> ParseGimpPalette(string absolutePath)
        {
            List<Color> colors = new List<Color>();
            string[] lines = File.ReadAllLines(absolutePath);
            foreach (string line in lines)
            {
                string trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine) || trimmedLine.StartsWith("#", StringComparison.Ordinal) || trimmedLine.StartsWith("GIMP Palette", StringComparison.Ordinal) || trimmedLine.StartsWith("Name:", StringComparison.Ordinal) || trimmedLine.StartsWith("Columns:", StringComparison.Ordinal))
                {
                    continue;
                }

                string[] fields = trimmedLine.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (fields.Length < 3
                    || !byte.TryParse(fields[0], out byte red)
                    || !byte.TryParse(fields[1], out byte green)
                    || !byte.TryParse(fields[2], out byte blue))
                {
                    continue;
                }

                colors.Add(new Color32(red, green, blue, byte.MaxValue));
            }

            return colors;
        }

        public SpritePaletteAsset Save(IReadOnlyList<Color> colors)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Save palette", "NewSpritePalette", "asset", "Choose a Unity project folder.");
            if (string.IsNullOrEmpty(assetPath)) return null;

            SpritePaletteAsset savedPalette = AssetDatabase.LoadAssetAtPath<SpritePaletteAsset>(assetPath);
            if (savedPalette == null)
            {
                savedPalette = ScriptableObject.CreateInstance<SpritePaletteAsset>();
                AssetDatabase.CreateAsset(savedPalette, assetPath);
            }

            savedPalette.SetColors(colors);
            EditorUtility.SetDirty(savedPalette);
            AssetDatabase.SaveAssets();
            Invalidate();
            return savedPalette;
        }

        public void Invalidate()
        {
            isCacheDirty = true;
        }

        public void Dispose()
        {
            EditorApplication.projectChanged -= Invalidate;
        }
    }

    public sealed class SpriteToolboxPalettePanel : ISpriteToolboxPanel
    {
        private const float FieldLabelWidth = 72f;
        private const float NumericFieldWidth = 42f;
        private const float SwatchMinimumWidth = 26f;
        private const float ScrollbarPadding = 16f;
        private const int ColorPickerTextureSize = 128;
        private const float ColorPickerSize = 160f;
        private static readonly Color AccentColor = new Color(0.95f, 0.89f, 0.84f);
        private static readonly Color GenerateButtonColor = new Color(1f, 0.9f, 0.2f);
        private static readonly Color SecondaryColorOutline = new Color(111f / 255f, 158f / 255f, 196f / 255f);

        private readonly ISpriteToolboxState state;
        private readonly IPaletteActions actions;
        private readonly SpriteToolboxPalettePanelSettings settings;
        private readonly SpritePalettePresetRepository repository;
        private readonly ISpriteToolboxNotifications notifications;
        private readonly Action<ISpriteToolboxState, IPaletteActions> testDraw;
        private Texture2D hueWheelTexture;
        private Texture2D saturationValueTexture;
        private Texture2D circleTexture;
        private Color pickerColor;
        private Color observedPrimaryColor;
        private float saturationValueHue = -1f;
        private bool isPickerInitialized;
        private bool pickerUsesSecondaryColor;
        private Vector2 scrollPosition;

        public SpriteToolboxPalettePanel(ISpriteToolboxPaletteContext context, SpriteToolboxPalettePanelSettings settings, SpritePalettePresetRepository repository, ISpriteToolboxNotifications notifications)
        {
            this.state = context ?? throw new ArgumentNullException(nameof(context));
            this.actions = context;
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
            this.notifications = notifications ?? throw new ArgumentNullException(nameof(notifications));
            EnsureDefaultPickerVisibility();
            RemoveDuplicateRecentColors();
        }

        public SpriteToolboxPalettePanel(ISpriteToolboxState state, IPaletteActions actions, Action<ISpriteToolboxState, IPaletteActions> draw)
        {
            this.state = state ?? throw new ArgumentNullException(nameof(state));
            this.actions = actions ?? throw new ArgumentNullException(nameof(actions));
            testDraw = draw ?? throw new ArgumentNullException(nameof(draw));
        }

        public void Draw(float contentWidth)
        {
            if (testDraw != null)
            {
                testDraw(state, actions);
                return;
            }

            DrawPalette(contentWidth);
        }

        public void Draw()
        {
            if (testDraw != null)
            {
                testDraw(state, actions);
                return;
            }

            Draw(EditorGUIUtility.currentViewWidth);
        }

        public void Draw(Rect panelRect)
        {
            if (testDraw != null)
            {
                testDraw(state, actions);
                return;
            }

            Rect contentRect = SpriteToolboxLayout.GetContentRect(panelRect, SpriteToolboxLayout.DefaultPanelContentPadding);
            GUILayout.BeginArea(contentRect);
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            DrawPalette(contentRect.width);
            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        public void AddRecentColor(Color color)
        {
            for (int colorIndex = settings.RecentColors.Count - 1; colorIndex >= 0; colorIndex--)
            {
                if (settings.RecentColors[colorIndex] == color)
                {
                    settings.RecentColors.RemoveAt(colorIndex);
                }
            }

            settings.RecentColors.Insert(0, color);
            if (settings.RecentColors.Count > 12) settings.RecentColors.RemoveAt(settings.RecentColors.Count - 1);
        }

        private void DrawPalette(float contentWidth)
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));
            EditorGUILayout.BeginHorizontal();
            SpriteToolboxEditorGui.DrawAccentLabel("Palette:", AccentColor, EditorStyles.boldLabel);
            DrawPresetSelector();
            GUILayout.FlexibleSpace();
            if (DrawIconButton("IconSavePalette", "Save palette", 24f, 20f))
            {
                SpritePaletteAsset savedPalette = repository.Save(state.Palette);
                if (savedPalette != null) notifications.Show("Palette saved.");
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool canSortPalette = SpriteColorReductionService.SupportsColorFamilyOperations(state.Palette.Count);
            EditorGUI.BeginDisabledGroup(!canSortPalette);
            if (GUILayout.Button(new GUIContent("Sort colors", "Order the current palette by related color families."), GUILayout.Width(84f)))
            {
                actions.SortPaletteByColorFamilies();
            }
            EditorGUI.EndDisabledGroup();
            EditorGUILayout.EndHorizontal();
            if (!canSortPalette)
            {
                GUILayout.Label($"Sorting is available up to {SpriteColorReductionService.MaximumColorFamilyOperationColorCount} colors.", EditorStyles.miniLabel);
            }

            DrawColorSwatches(state.Palette, true, contentWidth);
            if (settings.RecentColors.Count > 0)
            {
                GUILayout.Label("Recent", EditorStyles.miniBoldLabel);
                DrawColorSwatches(settings.RecentColors, false, contentWidth);
            }

            if (DrawSectionButton(settings.ShowPaletteGenerator ? "Palette Generator △" : "Palette Generator ▽"))
            {
                settings.ShowPaletteGenerator = !settings.ShowPaletteGenerator;
            }

            if (settings.ShowPaletteGenerator)
            {
                DrawPaletteGenerator();
            }

            GUILayout.FlexibleSpace();
            if (settings.ShowPicker)
            {
                DrawPersistentColorPicker(contentWidth);
            }
            GUILayout.Space(4f);
            if (DrawSectionButton(settings.ShowPicker ? "Picker ▽:" : "Picker △:"))
            {
                settings.ShowPicker = !settings.ShowPicker;
            }
            GUILayout.Space(4f);
            EditorGUILayout.EndVertical();
        }

        private void EnsureDefaultPickerVisibility()
        {
            if (settings.HasInitializedPickerVisibility) return;
            settings.ShowPicker = true;
            settings.HasInitializedPickerVisibility = true;
        }

        private bool DrawSectionButton(string label)
        {
            return GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(true));
        }

        private void DrawPaletteGenerator()
        {
            settings.Harmony = DrawHarmonyField("Harmony", settings.Harmony);
            settings.GeneratedColorCount = DrawIntSlider("Colors", settings.GeneratedColorCount, 2, 32);
            if (settings.Harmony == PaletteHarmonyMode.Planetary)
            {
                settings.PlanetarySeed = DrawIntField("Seed", settings.PlanetarySeed);
                settings.PlanetaryBaseHue = DrawSlider("Base hue", settings.PlanetaryBaseHue, 0f, 1f);
                settings.PlanetaryHueVariation = DrawSlider("Hue step", settings.PlanetaryHueVariation, 0f, 0.5f);
                DrawRangeSlider("Saturation", ref settings.PlanetarySaturationRange);
                DrawRangeSlider("Value", ref settings.PlanetaryValueRange);
            }

            Color previousBackgroundColor = GUI.backgroundColor;
            GUI.backgroundColor = GenerateButtonColor;
            bool generateRequested = GUILayout.Button("Generate palette", GUILayout.ExpandWidth(true));
            GUI.backgroundColor = previousBackgroundColor;
            if (!generateRequested) return;
            List<Color> generatedColors = settings.Harmony == PaletteHarmonyMode.Planetary
                ? SpritePaletteGenerator.GeneratePlanetary(settings.GeneratedColorCount, settings.PlanetarySeed, settings.PlanetaryBaseHue, settings.PlanetaryHueVariation, settings.PlanetarySaturationRange, settings.PlanetaryValueRange)
                : SpritePaletteGenerator.Generate(state.PrimaryColor, settings.GeneratedColorCount, settings.Harmony);
            actions.SetPalette(generatedColors);
        }

        private void DrawPersistentColorPicker(float contentWidth)
        {
            SynchronizePickerColor();
            GUILayout.Space(4f);
            float pickerSize = Mathf.Min(ColorPickerSize, Mathf.Max(80f, contentWidth));
            Rect pickerRect = GUILayoutUtility.GetRect(pickerSize, pickerSize, GUILayout.Width(pickerSize), GUILayout.Height(pickerSize));
            Rect hueRect = new Rect(pickerRect.x, pickerRect.y, pickerSize, pickerSize);
            float innerSize = pickerSize * 0.62f;
            Rect saturationValueRect = new Rect(
                pickerRect.center.x - innerSize * 0.5f,
                pickerRect.center.y - innerSize * 0.5f,
                innerSize,
                innerSize);

            Color.RGBToHSV(pickerColor, out float hue, out float saturation, out float value);
            DrawColorPickerTextures(hueRect, saturationValueRect, hue);
            DrawColorPickerHandles(hueRect, saturationValueRect, hue, saturation, value);
            HandleColorPickerInput(hueRect, saturationValueRect, hue, saturation, value);
            DrawColorPickerFields();
        }

        private void SynchronizePickerColor()
        {
            if (!isPickerInitialized)
            {
                pickerColor = state.PrimaryColor;
                observedPrimaryColor = state.PrimaryColor;
                isPickerInitialized = true;
                return;
            }

            if (!pickerUsesSecondaryColor && observedPrimaryColor != state.PrimaryColor)
            {
                pickerColor = state.PrimaryColor;
            }

            observedPrimaryColor = state.PrimaryColor;
        }

        private void DrawColorPickerTextures(Rect hueRect, Rect saturationValueRect, float hue)
        {
            if (hueWheelTexture == null) hueWheelTexture = CreateHueWheelTexture();
            if (saturationValueTexture == null || !Mathf.Approximately(saturationValueHue, hue))
            {
                UpdateSaturationValueTexture(hue);
            }

            GUI.DrawTexture(hueRect, hueWheelTexture, ScaleMode.StretchToFill, true);
            GUI.DrawTexture(saturationValueRect, saturationValueTexture, ScaleMode.StretchToFill, false);
        }

        private void DrawColorPickerHandles(Rect hueRect, Rect saturationValueRect, float hue, float saturation, float value)
        {
            float angle = hue * Mathf.PI * 2f - Mathf.PI * 0.5f;
            float radius = hueRect.width * 0.43f;
            Vector2 huePosition = hueRect.center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            DrawPickerHandle(huePosition, 8f, Color.HSVToRGB(hue, 1f, 1f));
            Vector2 saturationValuePosition = new Vector2(
                Mathf.Lerp(saturationValueRect.xMin, saturationValueRect.xMax, saturation),
                Mathf.Lerp(saturationValueRect.yMax, saturationValueRect.yMin, value));
            DrawPickerHandle(saturationValuePosition, 6f, pickerColor);
        }

        private void DrawPickerHandle(Vector2 position, float radius, Color fillColor)
        {
            if (circleTexture == null) circleTexture = CreateCircleTexture();
            DrawCircle(position, radius, Color.black);
            DrawCircle(position, radius - 1f, Color.white);
            DrawCircle(position, radius - 3f, fillColor);
        }

        private void DrawCircle(Vector2 position, float radius, Color color)
        {
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(position.x - radius, position.y - radius, radius * 2f, radius * 2f), circleTexture, ScaleMode.StretchToFill, true);
            GUI.color = previousColor;
        }

        private void HandleColorPickerInput(Rect hueRect, Rect saturationValueRect, float hue, float saturation, float value)
        {
            Event currentEvent = Event.current;
            bool isPointerInHueWheel = IsPointerInHueWheel(currentEvent.mousePosition, hueRect);
            bool isPointerInSaturationValue = saturationValueRect.Contains(currentEvent.mousePosition);
            if ((currentEvent.type != EventType.MouseDown && currentEvent.type != EventType.MouseDrag) || (!isPointerInHueWheel && !isPointerInSaturationValue)) return;

            if (isPointerInHueWheel)
            {
                Vector2 offset = currentEvent.mousePosition - hueRect.center;
                hue = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) / (Mathf.PI * 2f) + 0.25f, 1f);
            }
            else
            {
                saturation = Mathf.InverseLerp(saturationValueRect.xMin, saturationValueRect.xMax, currentEvent.mousePosition.x);
                value = Mathf.InverseLerp(saturationValueRect.yMax, saturationValueRect.yMin, currentEvent.mousePosition.y);
            }

            Color requestedColor = Color.HSVToRGB(hue, saturation, value);
            requestedColor.a = pickerColor.a;
            pickerUsesSecondaryColor = currentEvent.button == 1;
            SetPickerColor(requestedColor);
            currentEvent.Use();
        }

        private static bool IsPointerInHueWheel(Vector2 position, Rect rect)
        {
            float distance = Vector2.Distance(position, rect.center);
            float outerRadius = rect.width * 0.5f;
            float innerRadius = rect.width * 0.31f;
            return distance <= outerRadius && distance >= innerRadius;
        }

        private void DrawColorPickerFields()
        {
            Color32 color = pickerColor;
            DrawPickerSeparator();
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            string[] channelFormats = { "RGB 0-255", "RGB 0-1", "HSV" };
            settings.ColorChannelFormat = (SpriteToolboxColorChannelFormat)EditorGUILayout.Popup((int)settings.ColorChannelFormat, channelFormats, GUILayout.Width(92f));
            EditorGUILayout.EndHorizontal();

            Color requestedColor = settings.ColorChannelFormat == SpriteToolboxColorChannelFormat.Hsv
                ? DrawHsvColorChannels(pickerColor)
                : DrawRgbColorChannels(color, settings.ColorChannelFormat == SpriteToolboxColorChannelFormat.Rgb01);
            if (requestedColor != pickerColor)
            {
                pickerUsesSecondaryColor = false;
                SetPickerColor(requestedColor);
            }

            DrawPickerSeparator();
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Hex:", GUILayout.Width(28f));
            string hexadecimal = EditorGUILayout.TextField(ColorUtility.ToHtmlStringRGBA(pickerColor), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            if (ColorUtility.TryParseHtmlString("#" + hexadecimal.TrimStart('#'), out Color hexadecimalColor) && hexadecimalColor != pickerColor)
            {
                pickerUsesSecondaryColor = false;
                SetPickerColor(hexadecimalColor);
            }
        }

        private Color DrawRgbColorChannels(Color32 color, bool useNormalizedValues)
        {
            Color minimumRed = new Color(0f, color.g / 255f, color.b / 255f, color.a / 255f);
            Color maximumRed = new Color(1f, color.g / 255f, color.b / 255f, color.a / 255f);
            Color minimumGreen = new Color(color.r / 255f, 0f, color.b / 255f, color.a / 255f);
            Color maximumGreen = new Color(color.r / 255f, 1f, color.b / 255f, color.a / 255f);
            Color minimumBlue = new Color(color.r / 255f, color.g / 255f, 0f, color.a / 255f);
            Color maximumBlue = new Color(color.r / 255f, color.g / 255f, 1f, color.a / 255f);
            Color minimumAlpha = new Color(color.r / 255f, color.g / 255f, color.b / 255f, 0f);
            Color maximumAlpha = new Color(color.r / 255f, color.g / 255f, color.b / 255f, 1f);

            if (useNormalizedValues)
            {
                float red = DrawFloatColorChannel("R:", color.r / 255f, 1f, minimumRed, maximumRed, false);
                float green = DrawFloatColorChannel("G:", color.g / 255f, 1f, minimumGreen, maximumGreen, false);
                float blue = DrawFloatColorChannel("B:", color.b / 255f, 1f, minimumBlue, maximumBlue, false);
                float alpha = DrawFloatColorChannel("A:", color.a / 255f, 1f, minimumAlpha, maximumAlpha, true);
                return new Color(red, green, blue, alpha);
            }

            int byteRed = DrawColorChannel("R:", color.r, minimumRed, maximumRed, false);
            int byteGreen = DrawColorChannel("G:", color.g, minimumGreen, maximumGreen, false);
            int byteBlue = DrawColorChannel("B:", color.b, minimumBlue, maximumBlue, false);
            int byteAlpha = DrawColorChannel("A:", color.a, minimumAlpha, maximumAlpha, true);
            return new Color32((byte)byteRed, (byte)byteGreen, (byte)byteBlue, (byte)byteAlpha);
        }

        private Color DrawHsvColorChannels(Color color)
        {
            Color.RGBToHSV(color, out float hue, out float saturation, out float value);
            float degrees = DrawHueChannel("H:", hue * 360f);
            float requestedSaturation = DrawFloatColorChannel("S:", saturation, 1f, Color.HSVToRGB(hue, 0f, value), Color.HSVToRGB(hue, 1f, value), false);
            float requestedValue = DrawFloatColorChannel("V:", value, 1f, Color.black, Color.HSVToRGB(hue, saturation, 1f), false);
            float requestedAlpha = DrawFloatColorChannel("A:", color.a, 1f, new Color(color.r, color.g, color.b, 0f), color, true);
            Color requestedColor = Color.HSVToRGB(degrees / 360f, requestedSaturation, requestedValue);
            requestedColor.a = requestedAlpha;
            return requestedColor;
        }

        private static int DrawColorChannel(string label, int value, Color minimumColor, Color maximumColor, bool showCheckerboard)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(14f));
            Rect sliderRect = GUILayoutUtility.GetRect(32f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            DrawChannelGradient(sliderRect, minimumColor, maximumColor, showCheckerboard);
            int requestedValue = value;
            Event currentEvent = Event.current;
            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && sliderRect.Contains(currentEvent.mousePosition))
            {
                requestedValue = Mathf.RoundToInt(Mathf.InverseLerp(sliderRect.xMin, sliderRect.xMax, currentEvent.mousePosition.x) * 255f);
                currentEvent.Use();
            }

            float handleX = Mathf.Lerp(sliderRect.xMin, sliderRect.xMax, requestedValue / 255f);
            EditorGUI.DrawRect(new Rect(handleX - 1f, sliderRect.y + 2f, 2f, sliderRect.height - 4f), Color.white);
            requestedValue = EditorGUILayout.IntField(requestedValue, GUILayout.Width(38f));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
            return Mathf.Clamp(requestedValue, 0, 255);
        }

        private static float DrawFloatColorChannel(string label, float value, float maximumValue, Color minimumColor, Color maximumColor, bool showCheckerboard)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(14f));
            Rect sliderRect = GUILayoutUtility.GetRect(32f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            DrawChannelGradient(sliderRect, minimumColor, maximumColor, showCheckerboard);
            float requestedValue = value;
            Event currentEvent = Event.current;
            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && sliderRect.Contains(currentEvent.mousePosition))
            {
                requestedValue = Mathf.InverseLerp(sliderRect.xMin, sliderRect.xMax, currentEvent.mousePosition.x) * maximumValue;
                currentEvent.Use();
            }

            float handleX = Mathf.Lerp(sliderRect.xMin, sliderRect.xMax, requestedValue / maximumValue);
            EditorGUI.DrawRect(new Rect(handleX - 1f, sliderRect.y + 2f, 2f, sliderRect.height - 4f), Color.white);
            requestedValue = EditorGUILayout.FloatField(requestedValue, GUILayout.Width(38f));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
            return Mathf.Clamp(requestedValue, 0f, maximumValue);
        }

        private static float DrawHueChannel(string label, float value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(14f));
            Rect sliderRect = GUILayoutUtility.GetRect(32f, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(true));
            DrawHueGradient(sliderRect);
            float requestedValue = value;
            Event currentEvent = Event.current;
            if ((currentEvent.type == EventType.MouseDown || currentEvent.type == EventType.MouseDrag) && sliderRect.Contains(currentEvent.mousePosition))
            {
                requestedValue = Mathf.InverseLerp(sliderRect.xMin, sliderRect.xMax, currentEvent.mousePosition.x) * 360f;
                currentEvent.Use();
            }

            float handleX = Mathf.Lerp(sliderRect.xMin, sliderRect.xMax, requestedValue / 360f);
            EditorGUI.DrawRect(new Rect(handleX - 1f, sliderRect.y + 2f, 2f, sliderRect.height - 4f), Color.white);
            requestedValue = EditorGUILayout.FloatField(requestedValue, GUILayout.Width(38f));
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(2f);
            return Mathf.Repeat(requestedValue, 360f);
        }

        private static void DrawPickerSeparator()
        {
            GUILayout.Space(4f);
            Rect separatorRect = GUILayoutUtility.GetRect(1f, 2f, GUILayout.ExpandWidth(true));
            if (Event.current.type == EventType.Repaint) EditorGUI.DrawRect(separatorRect, new Color(0f, 0f, 0f, 0.35f));
            GUILayout.Space(4f);
        }

        private static void DrawChannelGradient(Rect rect, Color minimumColor, Color maximumColor, bool showCheckerboard)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (showCheckerboard)
            {
                const float checkerSize = 4f;
                for (float y = rect.y; y < rect.yMax; y += checkerSize)
                {
                    for (float x = rect.x; x < rect.xMax; x += checkerSize)
                    {
                        bool useLightColor = ((int)((x - rect.x) / checkerSize) + (int)((y - rect.y) / checkerSize)) % 2 == 0;
                        EditorGUI.DrawRect(new Rect(x, y, checkerSize, checkerSize), useLightColor ? new Color(0.78f, 0.78f, 0.78f) : new Color(0.48f, 0.48f, 0.48f));
                    }
                }
            }

            const int gradientSteps = 64;
            float stepWidth = rect.width / gradientSteps;
            for (int step = 0; step < gradientSteps; step++)
            {
                float interpolation = step / (float)(gradientSteps - 1);
                EditorGUI.DrawRect(new Rect(rect.x + step * stepWidth, rect.y, stepWidth + 1f, rect.height), Color.Lerp(minimumColor, maximumColor, interpolation));
            }
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), Color.black);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.black);
        }

        private static void DrawHueGradient(Rect rect)
        {
            if (Event.current.type != EventType.Repaint) return;
            const int gradientSteps = 64;
            float stepWidth = rect.width / gradientSteps;
            for (int step = 0; step < gradientSteps; step++)
            {
                float hue = step / (float)(gradientSteps - 1);
                EditorGUI.DrawRect(new Rect(rect.x + step * stepWidth, rect.y, stepWidth + 1f, rect.height), Color.HSVToRGB(hue, 1f, 1f));
            }
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, 1f), Color.black);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), Color.black);
        }

        private void SetPickerColor(Color color)
        {
            if (pickerColor == color) return;
            pickerColor = color;
            if (pickerUsesSecondaryColor) actions.SetSecondaryColor(color);
            else actions.SetPrimaryColor(color);
        }

        private static Texture2D CreateHueWheelTexture()
        {
            Texture2D texture = new Texture2D(ColorPickerTextureSize, ColorPickerTextureSize, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[ColorPickerTextureSize * ColorPickerTextureSize];
            float outerRadius = ColorPickerTextureSize * 0.5f;
            float innerRadius = ColorPickerTextureSize * 0.31f;
            Vector2 center = new Vector2((ColorPickerTextureSize - 1) * 0.5f, (ColorPickerTextureSize - 1) * 0.5f);
            for (int y = 0; y < ColorPickerTextureSize; y++)
            {
                for (int x = 0; x < ColorPickerTextureSize; x++)
                {
                    Vector2 offset = new Vector2(x, y) - center;
                    float distance = offset.magnitude;
                    if (distance < innerRadius || distance > outerRadius)
                    {
                        pixels[y * ColorPickerTextureSize + x] = Color.clear;
                        continue;
                    }

                    float hue = Mathf.Repeat(Mathf.Atan2(offset.y, offset.x) / (Mathf.PI * 2f) + 0.25f, 1f);
                    pixels[y * ColorPickerTextureSize + x] = Color.HSVToRGB(hue, 1f, 1f);
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D CreateCircleTexture()
        {
            const int size = 32;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                hideFlags = HideFlags.HideAndDontSave,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Color32[] pixels = new Color32[size * size];
            Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
            float radius = size * 0.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Vector2.Distance(new Vector2(x, y), center);
                    pixels[y * size + x] = distance <= radius ? Color.white : Color.clear;
                }
            }
            texture.SetPixels32(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private void UpdateSaturationValueTexture(float hue)
        {
            if (saturationValueTexture == null)
            {
                saturationValueTexture = new Texture2D(ColorPickerTextureSize, ColorPickerTextureSize, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
            }

            Color32[] pixels = new Color32[ColorPickerTextureSize * ColorPickerTextureSize];
            for (int y = 0; y < ColorPickerTextureSize; y++)
            {
                float value = y / (float)(ColorPickerTextureSize - 1);
                for (int x = 0; x < ColorPickerTextureSize; x++)
                {
                    float saturation = x / (float)(ColorPickerTextureSize - 1);
                    pixels[y * ColorPickerTextureSize + x] = Color.HSVToRGB(hue, saturation, value);
                }
            }
            saturationValueTexture.SetPixels32(pixels);
            saturationValueTexture.Apply(false, false);
            saturationValueHue = hue;
        }

        private void DrawPresetSelector()
        {
            IReadOnlyList<SpritePaletteAsset> presets = repository.GetPresets();
            if (presets.Count == 0)
            {
                GUILayout.Label("No presets", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                return;
            }

            string[] presetNames = new string[presets.Count + 3];
            int selectedIndex = 0;
            presetNames[0] = "Current palette";
            for (int index = 0; index < presets.Count; index++)
            {
                presetNames[index + 1] = presets[index].name;
                if (presets[index] == settings.PresetPalette) selectedIndex = index + 1;
            }

            int fromCanvasIndex = presetNames.Length - 2;
            presetNames[fromCanvasIndex] = "From Canvas";
            int importIndex = presetNames.Length - 1;
            presetNames[importIndex] = "Import palette…";
            int requestedIndex = EditorGUILayout.Popup(selectedIndex, presetNames, GUILayout.ExpandWidth(true));
            if (requestedIndex == importIndex)
            {
                SpritePaletteImportResult importedPalette = repository.Import();
                if (importedPalette != null)
                {
                    settings.PresetPalette = importedPalette.Asset;
                    actions.SetPalette(importedPalette.Colors);
                    notifications.Show("Palette imported.");
                }

                return;
            }

            if (requestedIndex == fromCanvasIndex)
            {
                settings.PresetPalette = null;
                actions.BuildPaletteFromCanvas();
                return;
            }

            if (requestedIndex > 0 && requestedIndex != selectedIndex)
            {
                SpritePaletteAsset preset = presets[requestedIndex - 1];
                settings.PresetPalette = preset;
                actions.SetPalette(preset.Colors);
            }
        }

        private void DrawColorSwatches(IReadOnlyList<Color> colors, bool allowPrimaryColorAdd, float contentWidth)
        {
            float availableWidth = Mathf.Max(SwatchMinimumWidth, contentWidth - ScrollbarPadding);
            int columns = Mathf.Max(1, Mathf.FloorToInt(availableWidth / SwatchMinimumWidth));
            float swatchWidth = availableWidth / columns;
            bool canAddPrimaryColor = allowPrimaryColorAdd && !ContainsColor(colors, state.PrimaryColor);
            int swatchCount = colors.Count + (canAddPrimaryColor ? 1 : 0);
            for (int colorIndex = 0; colorIndex < swatchCount; colorIndex += columns)
            {
                EditorGUILayout.BeginHorizontal();
                int rowEnd = Mathf.Min(colorIndex + columns, swatchCount);
                for (int currentColorIndex = colorIndex; currentColorIndex < rowEnd; currentColorIndex++)
                {
                    bool isAddButton = canAddPrimaryColor && currentColorIndex == colors.Count;
                    Color color = isAddButton ? state.PrimaryColor : colors[currentColorIndex];
                    Rect swatchRect = GUILayoutUtility.GetRect(swatchWidth, SwatchMinimumWidth, GUILayout.Width(swatchWidth), GUILayout.Height(SwatchMinimumWidth));
                    if (GUI.Button(swatchRect, GUIContent.none, GUIStyle.none))
                    {
                        if (isAddButton)
                        {
                            if (actions.AddPaletteColor(state.PrimaryColor)) AddRecentColor(state.PrimaryColor);
                        }
                        else if (Event.current.button == 1)
                        {
                            actions.SetSecondaryColor(color);
                            AddRecentColor(color);
                        }
                        else
                        {
                            actions.SetPrimaryColor(color);
                            AddRecentColor(color);
                        }
                    }

                    if (Event.current.type == EventType.Repaint)
                    {
                        bool isPrimarySelected = !isAddButton && color == state.PrimaryColor;
                        bool isSecondarySelected = !isAddButton && color == state.SecondaryColor;
                        DrawSwatch(swatchRect, color, isAddButton, isPrimarySelected, isSecondarySelected);
                    }
                }

                GUILayout.Space(ScrollbarPadding);
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void RemoveDuplicateRecentColors()
        {
            HashSet<Color32> colors = new HashSet<Color32>();
            int colorIndex = 0;
            while (colorIndex < settings.RecentColors.Count)
            {
                Color32 color = settings.RecentColors[colorIndex];
                if (!colors.Add(color))
                {
                    settings.RecentColors.RemoveAt(colorIndex);
                    continue;
                }

                colorIndex++;
            }
        }

        private static void DrawSwatch(Rect rect, Color color, bool isAddButton, bool isPrimarySelected, bool isSecondarySelected)
        {
            bool isHovered = rect.Contains(Event.current.mousePosition);
            bool isSelected = isPrimarySelected || isSecondarySelected;
            int outlineThickness = isSelected ? 2 : 1;
            Color outlineColor = isPrimarySelected ? GenerateButtonColor : (isSecondarySelected ? SecondaryColorOutline : (isHovered ? Color.white : Color.black));
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, rect.width, outlineThickness), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.yMax - outlineThickness, rect.width, outlineThickness), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.x, rect.y, outlineThickness, rect.height), outlineColor);
            EditorGUI.DrawRect(new Rect(rect.xMax - outlineThickness, rect.y, outlineThickness, rect.height), outlineColor);
            Rect contentRect = new Rect(rect.x + outlineThickness, rect.y + outlineThickness, rect.width - outlineThickness * 2f, rect.height - outlineThickness * 2f);
            EditorGUI.DrawRect(contentRect, isAddButton ? new Color(color.r, color.g, color.b, 0.25f) : color);
            if (!isAddButton) return;

            Texture2D icon = SpriteToolboxEditorIcons.Get("IconNewColor");
            if (icon == null) return;
            Color previousColor = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(contentRect.x + 2f, contentRect.y + 2f, contentRect.width - 4f, contentRect.height - 4f), icon, ScaleMode.ScaleToFit, true);
            GUI.color = previousColor;
        }

        private static PaletteHarmonyMode DrawHarmonyField(string label, PaletteHarmonyMode harmony)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(FieldLabelWidth));
            PaletteHarmonyMode result = (PaletteHarmonyMode)EditorGUILayout.EnumPopup(harmony, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private static int DrawIntField(string label, int value)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(FieldLabelWidth));
            int result = EditorGUILayout.IntField(value, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private static int DrawIntSlider(string label, int value, int minimum, int maximum)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(FieldLabelWidth));
            int result = EditorGUILayout.IntSlider(value, minimum, maximum, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();
            return result;
        }

        private static float DrawSlider(string label, float value, float minimum, float maximum)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(FieldLabelWidth));
            float result = GUILayout.HorizontalSlider(value, minimum, maximum, GUILayout.ExpandWidth(true));
            result = EditorGUILayout.FloatField(result, GUILayout.Width(NumericFieldWidth));
            EditorGUILayout.EndHorizontal();
            return Mathf.Clamp(result, minimum, maximum);
        }

        private static void DrawRangeSlider(string label, ref Vector2 range)
        {
            float minimum = Mathf.Clamp01(Mathf.Min(range.x, range.y));
            float maximum = Mathf.Clamp01(Mathf.Max(range.x, range.y));
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(FieldLabelWidth));
            minimum = EditorGUILayout.FloatField(minimum, GUILayout.Width(NumericFieldWidth));
            EditorGUILayout.MinMaxSlider(ref minimum, ref maximum, 0f, 1f, GUILayout.ExpandWidth(true));
            maximum = EditorGUILayout.FloatField(maximum, GUILayout.Width(NumericFieldWidth));
            EditorGUILayout.EndHorizontal();
            minimum = Mathf.Clamp01(minimum);
            maximum = Mathf.Clamp(maximum, minimum, 1f);
            range = new Vector2(minimum, maximum);
        }

        private static bool ContainsColor(IReadOnlyList<Color> colors, Color color)
        {
            foreach (Color item in colors)
            {
                if (item == color) return true;
            }

            return false;
        }

        private static bool DrawIconButton(string iconName, string tooltip, float width, float height)
        {
            Rect buttonRect = GUILayoutUtility.GetRect(width, height, GUILayout.Width(width), GUILayout.Height(height));
            Texture2D icon = SpriteToolboxEditorIcons.Get(iconName);
            Event current = Event.current;
            bool wasPressed = current.type == EventType.MouseDown && current.button == 0 && buttonRect.Contains(current.mousePosition);
            if (wasPressed)
            {
                current.Use();
            }

            if (icon != null && Event.current.type == EventType.Repaint)
            {
                GUI.DrawTexture(buttonRect, icon, ScaleMode.ScaleToFit, true);
            }

            GUI.Label(buttonRect, new GUIContent(string.Empty, tooltip), GUIStyle.none);
            return wasPressed;
        }

    }
}
