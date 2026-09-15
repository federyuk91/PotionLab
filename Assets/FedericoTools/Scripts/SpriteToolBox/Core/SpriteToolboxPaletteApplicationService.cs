using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    /// <summary>Executes palette, recolor and animation-tag use cases.</summary>
    internal sealed class SpriteToolboxPaletteApplicationService
    {
        private readonly Func<SpriteDocument> getDocument;
        private readonly Func<int> getSelectedFrameIndex;
        private readonly Func<int> getSelectedLayerIndex;
        private readonly Action<ISpriteCommand> execute;

        public SpriteToolboxPaletteApplicationService(Func<SpriteDocument> getDocument, Func<int> getSelectedFrameIndex, Func<int> getSelectedLayerIndex, Action<ISpriteCommand> execute)
        {
            this.getDocument = getDocument ?? throw new ArgumentNullException(nameof(getDocument));
            this.getSelectedFrameIndex = getSelectedFrameIndex ?? throw new ArgumentNullException(nameof(getSelectedFrameIndex));
            this.getSelectedLayerIndex = getSelectedLayerIndex ?? throw new ArgumentNullException(nameof(getSelectedLayerIndex));
            this.execute = execute ?? throw new ArgumentNullException(nameof(execute));
        }

        public IReadOnlyList<SpriteToolboxAnimationTagState> CreateAnimationTagStates()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new SpriteToolboxAnimationTagState[0];
            }

            List<SpriteToolboxAnimationTagState> states = new List<SpriteToolboxAnimationTagState>(document.AnimationTags.Count);
            foreach (SpriteAnimationTag tag in document.AnimationTags)
            {
                states.Add(new SpriteToolboxAnimationTagState(tag.Name, tag.FromFrame, tag.ToFrame, tag.Direction, tag.Color));
            }

            return states;
        }

        public bool RecolorSelectedFrame(Color sourceColor, Color targetColor)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            SpriteFrame frame = document.GetFrame(getSelectedFrameIndex());
            List<ISpriteCommand> commands = new List<ISpriteCommand>();
            for (int layerIndex = 0; layerIndex < document.LayerTracks.Count; layerIndex++)
            {
                if (document.GetLayerTrack(layerIndex).IsLocked)
                {
                    continue;
                }

                SpriteCel cel = frame.GetCel(layerIndex);
                List<PixelChange> changes = new List<PixelChange>();
                for (int y = 0; y < document.Height; y++)
                {
                    for (int x = 0; x < document.Width; x++)
                    {
                        Vector2Int position = new Vector2Int(x, y);
                        if (SpriteColorMath.AreEqual(cel.GetPixel(position), sourceColor))
                        {
                            changes.Add(new PixelChange(position, sourceColor, targetColor));
                        }
                    }
                }

                if (changes.Count > 0)
                {
                    commands.Add(new PixelChangesCommand(cel, changes));
                }
            }

            if (commands.Count == 0)
            {
                return false;
            }

            List<Color> palette = new List<Color>(document.Palette);
            if (!palette.Contains(targetColor))
            {
                palette.Add(targetColor);
                commands.Add(new SetPaletteCommand(document, palette));
            }

            execute(new SpriteCommandGroup(commands));
            return true;
        }

        public bool SetPalette(IReadOnlyList<Color> colors)
        {
            SpriteDocument document = getDocument();
            if (document == null || colors == null)
            {
                return false;
            }

            execute(new SetPaletteCommand(document, colors));
            return true;
        }

        public bool AddPaletteColor(Color color)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            List<Color> colors = new List<Color>(document.Palette);
            if (colors.Contains(color))
            {
                return false;
            }

            colors.Add(color);
            return SetPalette(colors);
        }

        public bool SortPaletteByColorFamilies()
        {
            SpriteDocument document = getDocument();
            if (document == null || document.Palette.Count < 2)
            {
                return false;
            }

            IReadOnlyList<Color> sortedColors = SpriteColorReductionService.OrderByColorFamilies(document.Palette);
            if (ArePalettesEqual(document.Palette, sortedColors))
            {
                return false;
            }

            return SetPalette(sortedColors);
        }

        public bool BuildPaletteFromSelectedFrame()
        {
            IReadOnlyList<Color> colors = GetVisibleCompositeColors();
            return colors.Count > 0 && SetPalette(colors);
        }

        public IReadOnlyList<Color> GetCanvasColors()
        {
            return CreateCanvasColorMap().Colors;
        }

        public Color32[] GetSelectedLayerPixels()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new Color32[0];
            }

            return document.GetFrame(getSelectedFrameIndex()).GetCel(getSelectedLayerIndex()).Pixels;
        }

        private IReadOnlyList<Color> GetVisibleCompositeColors()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new Color[0];
            }

            Color32[] compositePixels = new Color32[document.Width * document.Height];
            SpriteDocumentRenderer.RenderFrameIntoBuffer(document, getSelectedFrameIndex(), compositePixels);
            HashSet<Color32> uniqueColors = new HashSet<Color32>();
            List<Color> colors = new List<Color>();
            for (int pixelIndex = 0; pixelIndex < compositePixels.Length; pixelIndex++)
            {
                Color32 color = compositePixels[pixelIndex];
                if (color.a > 0 && uniqueColors.Add(color))
                {
                    colors.Add(color);
                    if (!SpriteColorReductionService.SupportsColorFamilyOperations(colors.Count))
                    {
                        return new Color[0];
                    }
                }
            }

            return SpriteColorReductionService.OrderByColorFamilies(colors);
        }

        public SpriteCanvasColorMap CreateCanvasColorMap()
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return new SpriteCanvasColorMap(new Color[0], new Dictionary<Color32, IReadOnlyList<Vector2Int>>());
            }

            SpriteCel selectedCel = document.GetFrame(getSelectedFrameIndex()).GetCel(getSelectedLayerIndex());
            Color32[] layerPixels = selectedCel.Pixels;

            HashSet<Color32> uniqueColors = new HashSet<Color32>();
            List<Color> colors = new List<Color>();
            Dictionary<Color32, List<Vector2Int>> mutablePositionsByColor = new Dictionary<Color32, List<Vector2Int>>();
            for (int pixelIndex = 0; pixelIndex < layerPixels.Length; pixelIndex++)
            {
                Color32 color = layerPixels[pixelIndex];
                if (color.a == 0 || !uniqueColors.Add(color))
                {
                    if (color.a == 0)
                    {
                        continue;
                    }
                }

                if (!mutablePositionsByColor.TryGetValue(color, out List<Vector2Int> positions))
                {
                    positions = new List<Vector2Int>();
                    mutablePositionsByColor.Add(color, positions);
                    colors.Add(color);
                }

                positions.Add(new Vector2Int(pixelIndex % document.Width, pixelIndex / document.Width));
            }

            Dictionary<Color32, IReadOnlyList<Vector2Int>> positionsByColor = new Dictionary<Color32, IReadOnlyList<Vector2Int>>();
            foreach (KeyValuePair<Color32, List<Vector2Int>> pair in mutablePositionsByColor)
            {
                positionsByColor.Add(pair.Key, pair.Value);
            }

            IReadOnlyList<Color> orderedColors = SpriteColorReductionService.OrderByColorFamilies(colors);
            return new SpriteCanvasColorMap(orderedColors, positionsByColor);
        }

        public IReadOnlyList<SpriteRecolorMapping> CreateColorReductionMappings(SpriteCanvasColorMap sourceMap, int targetColorCount)
        {
            return SpriteColorReductionService.CreateMappings(sourceMap, targetColorCount);
        }

        public bool ApplyRecolorMappings(IReadOnlyList<SpriteRecolorMapping> mappings, SpriteCanvasColorMap sourceMap)
        {
            SpriteDocument document = getDocument();
            if (document == null || mappings == null || mappings.Count == 0 || sourceMap == null)
            {
                return false;
            }

            Dictionary<Color32, Color32> targetsBySource = new Dictionary<Color32, Color32>();
            foreach (SpriteRecolorMapping mapping in mappings)
            {
                Color32 source = mapping.Source;
                Color32 target = mapping.Target;
                if (!source.Equals(target))
                {
                    targetsBySource[source] = target;
                }
            }

            if (targetsBySource.Count == 0)
            {
                return false;
            }

            int selectedLayerIndex = getSelectedLayerIndex();
            SpriteLayerTrack selectedTrack = document.GetLayerTrack(selectedLayerIndex);
            if (selectedTrack.IsLocked)
            {
                return false;
            }

            SpriteCel selectedCel = document.GetFrame(getSelectedFrameIndex()).GetCel(selectedLayerIndex);
            List<Color> palette = new List<Color>(document.Palette);
            List<PixelChange> changes = CreateRecolorChanges(selectedCel, targetsBySource, sourceMap);
            if (changes.Count == 0)
            {
                return false;
            }

            foreach (Color32 target in targetsBySource.Values)
            {
                Color color = target;
                if (!palette.Contains(color))
                {
                    palette.Add(color);
                }
            }

            List<ISpriteCommand> commands = new List<ISpriteCommand>
            {
                new PixelChangesCommand(selectedCel, changes),
                new SetPaletteCommand(document, palette)
            };
            execute(new SpriteCommandGroup(commands));
            return true;
        }

        public bool MapSelectedCelToPalette(IReadOnlyList<Color> palette, bool useDithering)
        {
            if (palette == null || palette.Count == 0)
            {
                return false;
            }

            if (useDithering)
            {
                return DitherSelectedCelToPalette(palette);
            }

            SpriteCanvasColorMap sourceMap = CreateCanvasColorMap();
            List<SpriteRecolorMapping> mappings = new List<SpriteRecolorMapping>(sourceMap.Colors.Count);
            foreach (Color sourceColor in sourceMap.Colors)
            {
                Color32 source = sourceColor;
                Color32 target = SpriteColorReductionService.GetNearestPaletteColor(source, palette);
                mappings.Add(new SpriteRecolorMapping(source, target));
            }

            return ApplyRecolorMappings(mappings, sourceMap);
        }

        private bool DitherSelectedCelToPalette(IReadOnlyList<Color> palette)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            int selectedLayerIndex = getSelectedLayerIndex();
            if (document.GetLayerTrack(selectedLayerIndex).IsLocked)
            {
                return false;
            }

            SpriteCel selectedCel = document.GetFrame(getSelectedFrameIndex()).GetCel(selectedLayerIndex);
            Color32[] ditheredPixels = SpriteReferenceImageImportService.DitherToColors(selectedCel.Pixels, document.Width, document.Height, palette);
            List<PixelChange> changes = new List<PixelChange>();
            for (int pixelIndex = 0; pixelIndex < ditheredPixels.Length; pixelIndex++)
            {
                Color32 source = selectedCel.Pixels[pixelIndex];
                Color32 target = ditheredPixels[pixelIndex];
                if (!source.Equals(target))
                {
                    changes.Add(new PixelChange(new Vector2Int(pixelIndex % document.Width, pixelIndex / document.Width), source, target));
                }
            }

            if (changes.Count == 0)
            {
                return false;
            }

            execute(new PixelChangesCommand(selectedCel, changes));
            return true;
        }

        public bool AddAnimationTag(string name, int fromFrame, int toFrame)
        {
            SpriteDocument document = getDocument();
            int tagIndex = document == null ? 0 : document.AnimationTags.Count;
            return AddAnimationTag(name, fromFrame, toFrame, SpriteAnimationDirection.Forward, SpriteToolboxTagColorPalette.GetColor(tagIndex));
        }

        public bool AddAnimationTag(string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color)
        {
            SpriteDocument document = getDocument();
            if (document == null)
            {
                return false;
            }

            List<SpriteAnimationTag> tags = CloneAnimationTags(document.AnimationTags);
            SpriteAnimationTag tag = new SpriteAnimationTag(name, fromFrame, toFrame);
            tag.ClampToFrameCount(document.Frames.Count);
            tag.Direction = direction;
            tag.Color = color;
            tags.Add(tag);
            execute(new SetAnimationTagsCommand(document, tags));
            return true;
        }

        public bool RemoveAnimationTag(int tagIndex)
        {
            SpriteDocument document = getDocument();
            if (document == null || tagIndex < 0 || tagIndex >= document.AnimationTags.Count)
            {
                return false;
            }

            List<SpriteAnimationTag> tags = CloneAnimationTags(document.AnimationTags);
            tags.RemoveAt(tagIndex);
            execute(new SetAnimationTagsCommand(document, tags));
            return true;
        }

        public bool UpdateAnimationTag(int tagIndex, string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color)
        {
            SpriteDocument document = getDocument();
            if (document == null || tagIndex < 0 || tagIndex >= document.AnimationTags.Count)
            {
                return false;
            }

            List<SpriteAnimationTag> tags = CloneAnimationTags(document.AnimationTags);
            SpriteAnimationTag tag = tags[tagIndex];
            tag.Name = name;
            tag.SetFrameRange(fromFrame, toFrame, document.Frames.Count);
            tag.Direction = direction;
            tag.Color = color;
            execute(new SetAnimationTagsCommand(document, tags));
            return true;
        }

        private static List<SpriteAnimationTag> CloneAnimationTags(IReadOnlyList<SpriteAnimationTag> source)
        {
            List<SpriteAnimationTag> tags = new List<SpriteAnimationTag>(source.Count);
            foreach (SpriteAnimationTag tag in source)
            {
                tags.Add(tag.Clone());
            }

            return tags;
        }

        private static bool ArePalettesEqual(IReadOnlyList<Color> first, IReadOnlyList<Color> second)
        {
            if (first.Count != second.Count)
            {
                return false;
            }

            for (int colorIndex = 0; colorIndex < first.Count; colorIndex++)
            {
                if (!SpriteColorMath.AreEqual(first[colorIndex], second[colorIndex]))
                {
                    return false;
                }
            }

            return true;
        }

        private static List<PixelChange> CreateRecolorChanges(
            SpriteCel cel,
            IReadOnlyDictionary<Color32, Color32> targetsBySource,
            SpriteCanvasColorMap sourceMap)
        {
            List<PixelChange> changes = new List<PixelChange>();
            foreach (KeyValuePair<Color32, Color32> pair in targetsBySource)
            {
                IReadOnlyList<Vector2Int> positions = sourceMap.GetPositions((Color)pair.Key);
                foreach (Vector2Int position in positions)
                {
                    Color32 source = cel.GetPixel32(position);
                    if (source.Equals(pair.Key))
                    {
                        changes.Add(new PixelChange(position, source, pair.Value));
                    }
                }
            }

            return changes;
        }
    }

}
