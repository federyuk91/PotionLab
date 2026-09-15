using System.Collections.Generic;
using System;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class SpriteToolboxImportExportService
    {
        private const int ImportPixelsPerEditorUpdate = 65536;
        private const int PaletteReductionPromptThreshold = 256;
        private const int ImportedPaletteReductionTarget = 64;
        private readonly Dictionary<string, TextureImportSettings> temporaryTextureImportSettings = new Dictionary<string, TextureImportSettings>();
        private ImportOperation activeImport;

        public bool TryImportPng(string path, out SpriteDocument document, out string displayName, out string recentPath)
        {
            document = null;
            displayName = Path.GetFileNameWithoutExtension(path);
            recentPath = path;
            Texture2D texture;
            bool destroyTextureAfterRead;
            if (!TryLoadTexture(path, out texture, out displayName, out recentPath, out destroyTextureAfterRead)) return false;

            try
            {
                document = ImportTexture(texture);
                return document != null;
            }
            finally
            {
                if (destroyTextureAfterRead) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        public bool BeginImportPng(string path, Action<SpriteDocument, string, string> completed, Action<string> failed)
        {
            if (activeImport != null)
            {
                failed?.Invoke("An image import is already in progress.");
                return false;
            }

            string displayName;
            string recentPath;
            Texture2D texture;
            bool destroyTextureAfterRead;
            if (!TryLoadTexture(path, out texture, out displayName, out recentPath, out destroyTextureAfterRead))
            {
                failed?.Invoke("The selected image could not be read.");
                return false;
            }

            try
            {
                Color32[] pixels = texture.GetPixels32();
                activeImport = new ImportOperation(pixels, texture.width, texture.height, displayName, recentPath, completed, failed);
                EditorApplication.update += ProcessActiveImport;
                return true;
            }
            catch (Exception exception)
            {
                failed?.Invoke($"Unable to import the selected image.\n{exception.Message}");
                return false;
            }
            finally
            {
                if (destroyTextureAfterRead) UnityEngine.Object.DestroyImmediate(texture);
            }
        }

        public void CancelActiveImport()
        {
            if (activeImport == null)
            {
                return;
            }

            EndActiveImport();
        }

        public void RestoreTemporaryImportSettings()
        {
            foreach (KeyValuePair<string, TextureImportSettings> entry in temporaryTextureImportSettings)
            {
                TextureImporter importer = AssetImporter.GetAtPath(entry.Key) as TextureImporter;
                TextureImportSettings originalSettings = entry.Value;
                if (importer != null && (importer.isReadable != originalSettings.IsReadable || importer.filterMode != originalSettings.FilterMode || importer.sRGBTexture != originalSettings.IsSrgbTexture || importer.textureCompression != originalSettings.TextureCompression))
                {
                    importer.isReadable = originalSettings.IsReadable;
                    importer.filterMode = originalSettings.FilterMode;
                    importer.sRGBTexture = originalSettings.IsSrgbTexture;
                    importer.textureCompression = originalSettings.TextureCompression;
                    importer.SaveAndReimport();
                }
            }
            temporaryTextureImportSettings.Clear();
        }

        public void ExportFrame(SpriteDocument document, int frameIndex)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Export frame PNG", $"frame_{frameIndex + 1}", "png", "Choose a Unity project folder.");
            if (!string.IsNullOrEmpty(assetPath)) WritePngAsset(SpriteDocumentRenderer.RenderFrame(document, frameIndex), assetPath, false);
        }

        public void ExportAllFrames(SpriteDocument document)
        {
            string absoluteFolderPath = EditorUtility.OpenFolderPanel("Export all frame PNG files", Application.dataPath, string.Empty);
            string assetFolderPath = SpriteToolboxDocumentPersistenceService.ToAssetPath(absoluteFolderPath);
            if (string.IsNullOrEmpty(assetFolderPath) || !assetFolderPath.StartsWith("Assets")) return;
            for (int frameIndex = 0; frameIndex < document.Frames.Count; frameIndex++)
            {
                string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{assetFolderPath}/frame_{frameIndex + 1:000}.png");
                WritePngAsset(SpriteDocumentRenderer.RenderFrame(document, frameIndex), assetPath, false);
            }
            AssetDatabase.Refresh();
        }

        public void ExportSpriteSheet(SpriteDocument document, int requestedColumns)
        {
            ExportSpriteSheet(document, requestedColumns, true);
        }

        public void ExportSpriteSheet(SpriteDocument document, int requestedColumns, bool exportAnimationClip)
        {
            string assetPath = EditorUtility.SaveFilePanelInProject("Export sprite sheet", "sprite_sheet", "png", "Choose a Unity project folder.");
            if (string.IsNullOrEmpty(assetPath)) return;

            bool isOverwritingSpriteSheet = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath) != null;
            int columns = Mathf.Clamp(requestedColumns, 1, document.Frames.Count);
            WritePngAsset(SpriteDocumentRenderer.RenderSpriteSheet(document, columns), assetPath, true);
            ConfigureSpriteSheetSlices(document, assetPath, columns);
            string basePath = Path.ChangeExtension(assetPath, null);
            SpriteSheetDescriptor descriptor = CreateOrUpdateSpriteSheetDescriptor(document, assetPath, columns, basePath, isOverwritingSpriteSheet);
            List<Sprite> sprites = new List<Sprite>();
            foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(assetPath))
            {
                if (asset is Sprite sprite) sprites.Add(sprite);
            }
            if (exportAnimationClip)
            {
                CreateOrUpdateAnimationClip(document, basePath, sprites.ToArray(), isOverwritingSpriteSheet);
            }
            AssetDatabase.SaveAssets();
            EditorGUIUtility.PingObject(descriptor);
        }

        private static SpriteSheetDescriptor CreateOrUpdateSpriteSheetDescriptor(SpriteDocument document, string spriteSheetPath, int columns, string basePath, bool isOverwritingSpriteSheet)
        {
            string descriptorPath = basePath + "_slices.asset";
            SpriteSheetDescriptor descriptor = isOverwritingSpriteSheet ? AssetDatabase.LoadAssetAtPath<SpriteSheetDescriptor>(descriptorPath) : null;
            if (descriptor == null)
            {
                descriptor = ScriptableObject.CreateInstance<SpriteSheetDescriptor>();
                AssetDatabase.CreateAsset(descriptor, AssetDatabase.GenerateUniqueAssetPath(descriptorPath));
            }

            descriptor.Initialize(AssetDatabase.LoadAssetAtPath<Texture2D>(spriteSheetPath), document, columns);
            EditorUtility.SetDirty(descriptor);
            return descriptor;
        }

        private static void CreateOrUpdateAnimationClip(SpriteDocument document, string basePath, Sprite[] sprites, bool isOverwritingSpriteSheet)
        {
            string clipPath = basePath + ".anim";
            AnimationClip existingClip = isOverwritingSpriteSheet ? AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath) : null;
            string outputPath = existingClip == null ? AssetDatabase.GenerateUniqueAssetPath(clipPath) : clipPath;
            CreateAnimationClip(document, outputPath, sprites, existingClip);
        }

        private SpriteDocument ImportTexture(Texture2D texture)
        {
            Texture2D readableTexture = EnsureTextureIsReadable(texture);
            if (readableTexture == null)
            {
                EditorUtility.DisplayDialog("Unable to read texture", "The selected texture could not be made readable.", "OK");
                return null;
            }
            SpriteDocument document = new SpriteDocument(readableTexture.width, readableTexture.height);
            SpriteCel cel = document.GetFrame(0).GetCel(0);
            Color32[] pixels = readableTexture.GetPixels32();
            cel.SetPixels(pixels);
            HashSet<Color32> uniqueColors = new HashSet<Color32>();
            List<Color> paletteColors = CreateDefaultPalette(uniqueColors);
            for (int pixelIndex = 0; pixelIndex < pixels.Length; pixelIndex++)
            {
                Color32 color = pixels[pixelIndex];
                if (color.a > 0 && uniqueColors.Add(color))
                {
                    paletteColors.Add(color);
                }
            }
            document.SetPalette(paletteColors);
            return document;
        }

        private bool TryLoadTexture(string path, out Texture2D texture, out string displayName, out string recentPath, out bool destroyTextureAfterRead)
        {
            displayName = Path.GetFileNameWithoutExtension(path);
            recentPath = path;
            destroyTextureAfterRead = false;
            string assetPath = SpriteToolboxDocumentPersistenceService.ToAssetPath(path);
            texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            if (texture != null)
            {
                Texture2D readableTexture = EnsureTextureIsReadable(texture);
                if (readableTexture == null)
                {
                    texture = null;
                    return false;
                }

                texture = readableTexture;
                displayName = texture.name;
                recentPath = assetPath;
                return true;
            }

            texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            destroyTextureAfterRead = true;
            try
            {
                bool imageLoaded = texture.LoadImage(File.ReadAllBytes(path));
                if (!imageLoaded)
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                    texture = null;
                    destroyTextureAfterRead = false;
                }

                return imageLoaded;
            }
            catch
            {
                UnityEngine.Object.DestroyImmediate(texture);
                texture = null;
                destroyTextureAfterRead = false;
                return false;
            }
        }

        private void ProcessActiveImport()
        {
            if (activeImport == null)
            {
                EditorApplication.update -= ProcessActiveImport;
                return;
            }

            float progress = activeImport.Process(ImportPixelsPerEditorUpdate);
            if (EditorUtility.DisplayCancelableProgressBar("Import PNG", "Building document palette…", progress))
            {
                EndActiveImport();
                return;
            }

            if (!activeImport.IsComplete)
            {
                return;
            }

            ImportOperation completedImport = activeImport;
            EndActiveImport();
            completedImport.Complete();
        }

        private void EndActiveImport()
        {
            EditorApplication.update -= ProcessActiveImport;
            activeImport = null;
            EditorUtility.ClearProgressBar();
        }

        private static List<Color> CreateDefaultPalette(HashSet<Color32> uniqueColors)
        {
            List<Color> paletteColors = new List<Color>();
            Color32 black = Color.black;
            Color32 white = Color.white;
            uniqueColors.Add(black);
            uniqueColors.Add(white);
            paletteColors.Add(black);
            paletteColors.Add(white);
            return paletteColors;
        }

        private sealed class ImportOperation
        {
            private readonly Color32[] pixels;
            private readonly SpriteDocument document;
            private readonly List<Color> paletteColors;
            private readonly HashSet<Color32> uniqueColors = new HashSet<Color32>();
            private readonly string displayName;
            private readonly string recentPath;
            private readonly Action<SpriteDocument, string, string> completed;
            private readonly Action<string> failed;
            private int nextPixelIndex;

            public bool IsComplete => nextPixelIndex >= pixels.Length;

            public ImportOperation(Color32[] pixels, int width, int height, string displayName, string recentPath, Action<SpriteDocument, string, string> completed, Action<string> failed)
            {
                this.pixels = pixels ?? throw new ArgumentNullException(nameof(pixels));
                document = new SpriteDocument(width, height);
                document.GetFrame(0).GetCel(0).SetPixels(pixels);
                paletteColors = CreateDefaultPalette(uniqueColors);
                this.displayName = displayName;
                this.recentPath = recentPath;
                this.completed = completed;
                this.failed = failed;
            }

            public float Process(int pixelBudget)
            {
                int endIndex = Mathf.Min(pixels.Length, nextPixelIndex + pixelBudget);
                for (; nextPixelIndex < endIndex; nextPixelIndex++)
                {
                    Color32 color = pixels[nextPixelIndex];
                    if (color.a > 0 && uniqueColors.Add(color))
                    {
                        paletteColors.Add(color);
                    }
                }

                return pixels.Length == 0 ? 1f : nextPixelIndex / (float)pixels.Length;
            }

            public void Complete()
            {
                try
                {
                    if (paletteColors.Count > PaletteReductionPromptThreshold)
                    {
                        int choice = EditorUtility.DisplayDialogComplex(
                            "Large palette detected",
                            $"This image contains {paletteColors.Count} palette colors. Reduce it to {ImportedPaletteReductionTarget} colors using the Sprite Toolbox color-family algorithm?",
                            $"Reduce palette to {ImportedPaletteReductionTarget}",
                            "Cancel",
                            string.Empty);
                        if (choice != 0)
                        {
                            return;
                        }

                        Color32[] reducedPixels = SpriteReferenceImageImportService.ReduceColors(pixels, document.Width, document.Height, ImportedPaletteReductionTarget);
                        document.GetFrame(0).GetCel(0).SetPixels(reducedPixels);
                        paletteColors.Clear();
                        uniqueColors.Clear();
                        paletteColors.AddRange(CreateDefaultPalette(uniqueColors));
                        foreach (Color32 color in reducedPixels)
                        {
                            if (color.a > 0 && uniqueColors.Add(color))
                            {
                                paletteColors.Add(color);
                            }
                        }
                    }

                    document.SetPalette(paletteColors);
                    completed?.Invoke(document, displayName, recentPath);
                }
                catch (Exception exception)
                {
                    failed?.Invoke($"Unable to complete the image import.\n{exception.Message}");
                }
            }
        }

        private Texture2D EnsureTextureIsReadable(Texture2D texture)
        {
            string assetPath = AssetDatabase.GetAssetPath(texture);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return texture.isReadable ? texture : null;
            if (texture.isReadable && importer.filterMode == FilterMode.Point && importer.sRGBTexture && importer.textureCompression == TextureImporterCompression.Uncompressed)
            {
                return texture;
            }
            if (!temporaryTextureImportSettings.ContainsKey(assetPath))
            {
                temporaryTextureImportSettings.Add(assetPath, new TextureImportSettings(importer.isReadable, importer.filterMode, importer.sRGBTexture, importer.textureCompression));
            }

            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.sRGBTexture = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        }

        private readonly struct TextureImportSettings
        {
            public bool IsReadable { get; }
            public FilterMode FilterMode { get; }
            public bool IsSrgbTexture { get; }
            public TextureImporterCompression TextureCompression { get; }

            public TextureImportSettings(bool isReadable, FilterMode filterMode, bool isSrgbTexture, TextureImporterCompression textureCompression)
            {
                IsReadable = isReadable;
                FilterMode = filterMode;
                IsSrgbTexture = isSrgbTexture;
                TextureCompression = textureCompression;
            }
        }

        private static void WritePngAsset(Texture2D texture, string assetPath, bool multipleSprites)
        {
            string projectPath = Directory.GetParent(Application.dataPath).FullName;
            File.WriteAllBytes(Path.Combine(projectPath, assetPath), texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = multipleSprites ? SpriteImportMode.Multiple : SpriteImportMode.Single;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }

        private static void ConfigureSpriteSheetSlices(SpriteDocument document, string assetPath, int columns)
        {
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;
            int rows = Mathf.CeilToInt(document.Frames.Count / (float)columns);
            SpriteRect[] sliceData = new SpriteRect[document.Frames.Count];
            string baseName = Path.GetFileNameWithoutExtension(assetPath);
            for (int frameIndex = 0; frameIndex < document.Frames.Count; frameIndex++)
            {
                int x = frameIndex % columns;
                int y = rows - 1 - frameIndex / columns;
                sliceData[frameIndex] = new SpriteRect
                {
                    name = $"{baseName}_{frameIndex + 1}",
                    rect = new Rect(x * document.Width, y * document.Height, document.Width, document.Height),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    spriteID = GUID.Generate()
                };
            }
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            if (provider == null) return;
            provider.InitSpriteEditorDataProvider();
            provider.SetSpriteRects(sliceData);
            provider.Apply();
            importer.SaveAndReimport();
        }

        private static void CreateAnimationClip(SpriteDocument document, string assetPath, Sprite[] sprites, AnimationClip existingClip)
        {
            if (sprites.Length != document.Frames.Count)
            {
                Debug.LogWarning("The sprite sheet does not contain one Sprite for each Sprite Toolbox frame. Animation clip export was skipped.");
                return;
            }
            List<ObjectReferenceKeyframe> keyframes = new List<ObjectReferenceKeyframe>();
            float time = 0f;
            for (int frameIndex = 0; frameIndex < sprites.Length; frameIndex++)
            {
                keyframes.Add(new ObjectReferenceKeyframe { time = time, value = sprites[frameIndex] });
                time += document.GetFrame(frameIndex).Duration;
            }
            AnimationClip clip = existingClip ?? new AnimationClip();
            clip.frameRate = 60f;
            EditorCurveBinding binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keyframes.ToArray());
            if (existingClip == null)
            {
                AssetDatabase.CreateAsset(clip, assetPath);
            }
            else
            {
                EditorUtility.SetDirty(clip);
            }
        }
    }
}
