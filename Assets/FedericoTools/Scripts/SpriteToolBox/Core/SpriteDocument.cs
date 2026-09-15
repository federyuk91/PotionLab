using System;
using System.Collections.Generic;
using UnityEngine;

namespace FedericoTools.SpriteToolBox
{
    [Serializable]
    public sealed class SpriteDocument
    {
        [SerializeField] private int width;
        [SerializeField] private int height;
        [SerializeField] private List<Color> palette = new List<Color>();
        [SerializeField] private List<SpriteLayerTrack> layerTracks = new List<SpriteLayerTrack>();
        [SerializeField] private List<SpriteFrame> frames = new List<SpriteFrame>();
        [SerializeField] private List<SpriteAnimationTag> animationTags = new List<SpriteAnimationTag>();

        public int Width => width;
        public int Height => height;
        public IReadOnlyList<Color> Palette => palette;
        public IReadOnlyList<SpriteLayerTrack> LayerTracks
        {
            get
            {
                EnsureStructure();
                return layerTracks;
            }
        }

        public IReadOnlyList<SpriteFrame> Frames
        {
            get
            {
                EnsureStructure();
                return frames;
            }
        }
        public IReadOnlyList<SpriteAnimationTag> AnimationTags
        {
            get
            {
                EnsureStructure();
                return animationTags;
            }
        }

        public SpriteAnimationTag AddAnimationTag(string name, int fromFrame, int toFrame)
        {
            EnsureStructure();
            SpriteAnimationTag tag = new SpriteAnimationTag(name, fromFrame, toFrame);
            tag.ClampToFrameCount(frames.Count);
            animationTags.Add(tag);
            return tag;
        }

        public bool RemoveAnimationTag(int tagIndex)
        {
            EnsureStructure();
            if (tagIndex < 0 || tagIndex >= animationTags.Count)
            {
                return false;
            }

            animationTags.RemoveAt(tagIndex);
            return true;
        }

        public bool UpdateAnimationTag(int tagIndex, string name, int fromFrame, int toFrame, SpriteAnimationDirection direction, Color color)
        {
            EnsureStructure();
            if (tagIndex < 0 || tagIndex >= animationTags.Count)
            {
                return false;
            }

            SpriteAnimationTag tag = animationTags[tagIndex];
            tag.Name = name;
            tag.SetFrameRange(fromFrame, toFrame, frames.Count);
            tag.Direction = direction;
            tag.Color = color;
            return true;
        }

        public SpriteDocument(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), "A sprite document must be at least one pixel wide and high.");
            }

            this.width = width;
            this.height = height;
            palette.Add(Color.black);
            palette.Add(Color.white);
            layerTracks.Add(new SpriteLayerTrack("Layer 1"));
            frames.Add(new SpriteFrame(width, height, 1));
        }

        public SpriteFrame GetFrame(int frameIndex)
        {
            EnsureStructure();
            return frames[frameIndex];
        }

        public SpriteLayerTrack GetLayerTrack(int layerIndex)
        {
            EnsureStructure();
            return layerTracks[layerIndex];
        }

        public SpriteFrame AddFrame(float duration = 0.1f)
        {
            EnsureStructure();
            SpriteFrame frame = frames.Count == 0
                ? new SpriteFrame(width, height, layerTracks.Count)
                : frames[frames.Count - 1].Clone();
            frame.Duration = duration;
            frames.Add(frame);
            return frame;
        }

        public SpriteFrame InsertFrame(int frameIndex, bool duplicatePreviousFrame, float duration = 0.1f)
        {
            EnsureStructure();
            int insertionIndex = Mathf.Clamp(frameIndex, 0, frames.Count);
            SpriteFrame frame = duplicatePreviousFrame && frames.Count > 0
                ? frames[Mathf.Clamp(insertionIndex - 1, 0, frames.Count - 1)].Clone()
                : new SpriteFrame(width, height, layerTracks.Count);
            frame.Duration = duration;
            frames.Insert(insertionIndex, frame);
            ShiftAnimationTagsForInsertedFrame(insertionIndex);
            return frame;
        }

        public void RemoveFrame(int frameIndex)
        {
            EnsureStructure();
            if (frames.Count > 1)
            {
                frames.RemoveAt(frameIndex);
                ShiftAnimationTagsForRemovedFrame(frameIndex);
            }
        }

        public void MoveFrame(int frameIndex, int destinationIndex)
        {
            EnsureStructure();
            if (frameIndex == destinationIndex || frameIndex < 0 || destinationIndex < 0 || frameIndex >= frames.Count || destinationIndex >= frames.Count)
            {
                return;
            }

            SpriteFrame frame = frames[frameIndex];
            frames.RemoveAt(frameIndex);
            frames.Insert(destinationIndex, frame);
        }

        public void ClearCel(int frameIndex, int layerIndex)
        {
            EnsureStructure();
            frames[frameIndex].ReplaceCel(layerIndex, new SpriteCel(width, height));
        }

        internal SpriteFrame CreateFrameForCommand(int insertionIndex, bool duplicatePreviousFrame, float duration)
        {
            int safeIndex = Mathf.Clamp(insertionIndex, 0, frames.Count);
            SpriteFrame frame = duplicatePreviousFrame && frames.Count > 0
                ? frames[Mathf.Clamp(safeIndex - 1, 0, frames.Count - 1)].Clone()
                : new SpriteFrame(width, height, layerTracks.Count);
            frame.Duration = duration;
            return frame;
        }

        internal void InsertFrameForCommand(int frameIndex, SpriteFrame frame)
        {
            int insertionIndex = Mathf.Clamp(frameIndex, 0, frames.Count);
            frames.Insert(insertionIndex, frame);
            ShiftAnimationTagsForInsertedFrame(insertionIndex);
        }

        internal SpriteFrame RemoveFrameForCommand(int frameIndex)
        {
            SpriteFrame frame = frames[frameIndex];
            frames.RemoveAt(frameIndex);
            ShiftAnimationTagsForRemovedFrame(frameIndex);
            return frame;
        }

        internal List<SpriteAnimationTag> CloneAnimationTagsForCommand()
        {
            List<SpriteAnimationTag> clonedTags = new List<SpriteAnimationTag>(animationTags.Count);
            foreach (SpriteAnimationTag tag in animationTags)
            {
                clonedTags.Add(tag.Clone());
            }

            return clonedTags;
        }

        internal void RestoreAnimationTagsForCommand(IReadOnlyList<SpriteAnimationTag> sourceTags)
        {
            animationTags = new List<SpriteAnimationTag>(sourceTags.Count);
            foreach (SpriteAnimationTag tag in sourceTags)
            {
                animationTags.Add(tag.Clone());
            }

            ClampAnimationTagsToFrameCount();
        }

        internal SpriteCel ReplaceCelForCommand(int frameIndex, int layerIndex, SpriteCel replacement)
        {
            SpriteCel previous = frames[frameIndex].GetCel(layerIndex);
            frames[frameIndex].ReplaceCel(layerIndex, replacement);
            return previous;
        }

        public void AddLayerTrack(string name)
        {
            EnsureStructure();
            layerTracks.Add(new SpriteLayerTrack(name));
            foreach (SpriteFrame frame in frames)
            {
                frame.AddCel(width, height);
            }
        }

        internal SpriteLayerTrackSnapshot CreateLayerTrackSnapshotForCommand(string name)
        {
            List<SpriteCel> cels = new List<SpriteCel>(frames.Count);
            foreach (SpriteFrame frame in frames)
            {
                cels.Add(new SpriteCel(width, height));
            }

            return new SpriteLayerTrackSnapshot(new SpriteLayerTrack(name), cels);
        }

        internal SpriteLayerTrackSnapshot CreateDuplicateLayerTrackSnapshotForCommand(int layerIndex, string duplicateName)
        {
            SpriteLayerTrack sourceTrack = layerTracks[layerIndex];
            SpriteLayerTrack duplicateTrack = sourceTrack.Clone();
            duplicateTrack.Name = duplicateName;
            List<SpriteCel> duplicateCels = new List<SpriteCel>(frames.Count);
            foreach (SpriteFrame frame in frames)
            {
                duplicateCels.Add(frame.GetCel(layerIndex).Clone());
            }

            return new SpriteLayerTrackSnapshot(duplicateTrack, duplicateCels);
        }

        internal SpriteLayerTrackSnapshot RemoveLayerTrackForCommand(int layerIndex)
        {
            SpriteLayerTrack track = layerTracks[layerIndex];
            List<SpriteCel> cels = new List<SpriteCel>(frames.Count);
            foreach (SpriteFrame frame in frames)
            {
                cels.Add(frame.GetCel(layerIndex));
                frame.RemoveCel(layerIndex);
            }

            layerTracks.RemoveAt(layerIndex);
            return new SpriteLayerTrackSnapshot(track, cels);
        }

        internal void InsertLayerTrackForCommand(int layerIndex, SpriteLayerTrackSnapshot snapshot)
        {
            layerTracks.Insert(Mathf.Clamp(layerIndex, 0, layerTracks.Count), snapshot.Track);
            for (int frameIndex = 0; frameIndex < frames.Count; frameIndex++)
            {
                frames[frameIndex].InsertCel(layerIndex, snapshot.Cels[frameIndex]);
            }
        }

        public void RemoveLayerTrack(int layerIndex)
        {
            EnsureStructure();
            if (layerTracks.Count <= 1)
            {
                return;
            }

            layerTracks.RemoveAt(layerIndex);
            foreach (SpriteFrame frame in frames)
            {
                frame.RemoveCel(layerIndex);
            }
        }

        public void MoveLayerTrack(int layerIndex, int destinationIndex)
        {
            EnsureStructure();
            if (layerIndex == destinationIndex || layerIndex < 0 || destinationIndex < 0 || layerIndex >= layerTracks.Count || destinationIndex >= layerTracks.Count)
            {
                return;
            }

            SpriteLayerTrack track = layerTracks[layerIndex];
            layerTracks.RemoveAt(layerIndex);
            layerTracks.Insert(destinationIndex, track);
            foreach (SpriteFrame frame in frames)
            {
                frame.MoveCel(layerIndex, destinationIndex);
            }
        }

        public void MergeLayerDown(int layerIndex)
        {
            EnsureStructure();
            if (layerIndex <= 0 || layerIndex >= layerTracks.Count)
            {
                return;
            }

            SpriteLayerTrack topTrack = layerTracks[layerIndex];
            foreach (SpriteFrame frame in frames)
            {
                frame.GetCel(layerIndex - 1).Composite(frame.GetCel(layerIndex), topTrack.Opacity);
                frame.RemoveCel(layerIndex);
            }

            layerTracks.RemoveAt(layerIndex);
        }

        /// <summary>Resizes every cel, preserving pixels from the top-left document origin.</summary>
        public void ResizeCanvas(int newWidth, int newHeight)
        {
            EnsureStructure();
            if (newWidth < 1 || newHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(newWidth), "Canvas dimensions must be at least one pixel.");
            }

            if (width == newWidth && height == newHeight)
            {
                return;
            }

            TransformCanvas(newWidth, newHeight, false, false);
        }

        public void CropCanvas(RectInt cropRect)
        {
            EnsureStructure();
            int xMin = Mathf.Clamp(cropRect.xMin, 0, width);
            int yMin = Mathf.Clamp(cropRect.yMin, 0, height);
            int xMax = Mathf.Clamp(cropRect.xMax, xMin, width);
            int yMax = Mathf.Clamp(cropRect.yMax, yMin, height);
            int croppedWidth = xMax - xMin;
            int croppedHeight = yMax - yMin;
            if (croppedWidth < 1 || croppedHeight < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(cropRect), "The crop rectangle must include at least one canvas pixel.");
            }

            foreach (SpriteFrame frame in frames)
            {
                for (int layerIndex = 0; layerIndex < layerTracks.Count; layerIndex++)
                {
                    Color32[] sourcePixels = frame.GetCel(layerIndex).Pixels;
                    Color32[] croppedPixels = new Color32[croppedWidth * croppedHeight];
                    for (int y = 0; y < croppedHeight; y++)
                    {
                        System.Array.Copy(sourcePixels, (yMin + y) * width + xMin, croppedPixels, y * croppedWidth, croppedWidth);
                    }

                    SpriteCel croppedCel = new SpriteCel(croppedWidth, croppedHeight);
                    croppedCel.SetPixels(croppedPixels);
                    frame.ReplaceCel(layerIndex, croppedCel);
                }
            }

            width = croppedWidth;
            height = croppedHeight;
        }

        public void FlipCanvas(bool horizontally)
        {
            EnsureStructure();
            TransformCanvas(width, height, horizontally, !horizontally);
        }

        public void AddPaletteColor(Color color)
        {
            if (!palette.Contains(color))
            {
                palette.Add(color);
            }
        }

        public void SetPalette(IReadOnlyList<Color> colors)
        {
            palette.Clear();
            HashSet<Color> uniqueColors = new HashSet<Color>();
            foreach (Color color in colors)
            {
                if (uniqueColors.Add(color))
                {
                    palette.Add(color);
                }
            }
        }

        public SpriteDocument Clone()
        {
            EnsureStructure();
            SpriteDocument clone = new SpriteDocument(width, height);
            clone.palette = new List<Color>(palette);
            clone.layerTracks = new List<SpriteLayerTrack>();
            clone.frames = new List<SpriteFrame>();
            clone.animationTags = new List<SpriteAnimationTag>();
            foreach (SpriteLayerTrack track in layerTracks)
            {
                clone.layerTracks.Add(track.Clone());
            }

            foreach (SpriteFrame frame in frames)
            {
                clone.frames.Add(frame.Clone());
            }

            foreach (SpriteAnimationTag tag in animationTags)
            {
                clone.animationTags.Add(tag.Clone());
            }

            return clone;
        }

        public long EstimateMemoryBytes()
        {
            EnsureStructure();
            long pixelCount = 0L;
            foreach (SpriteFrame frame in frames)
            {
                foreach (SpriteCel cel in frame.Cels)
                {
                    pixelCount += cel.Pixels.LongLength;
                }
            }

            return pixelCount * 4L + palette.Count * sizeof(float) * 4L + layerTracks.Count * 128L;
        }

        public void RestoreFrom(SpriteDocument source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            SpriteDocument copy = source.Clone();
            width = copy.width;
            height = copy.height;
            palette = copy.palette;
            layerTracks = copy.layerTracks;
            frames = copy.frames;
            animationTags = copy.animationTags;
        }

        public void EnsureStructure()
        {
            // Unity can deserialize missing lists as null after a domain reload or an asset migration.
            palette ??= new List<Color>();
            layerTracks ??= new List<SpriteLayerTrack>();
            frames ??= new List<SpriteFrame>();
            animationTags ??= new List<SpriteAnimationTag>();

            if (width < 1 || height < 1)
            {
                width = 32;
                height = 32;
            }

            if (frames.Count == 0)
            {
                frames.Add(new SpriteFrame(width, height, 0));
            }

            if (layerTracks.Count == 0)
            {
                layerTracks.Add(new SpriteLayerTrack("Layer 1"));
            }

            foreach (SpriteFrame frame in frames)
            {
                frame.EnsureCelCount(layerTracks.Count, width, height);
            }

            ClampAnimationTagsToFrameCount();
        }

        private void ShiftAnimationTagsForInsertedFrame(int frameIndex)
        {
            foreach (SpriteAnimationTag tag in animationTags)
            {
                if (frameIndex <= tag.FromFrame)
                {
                    tag.SetFrameRange(tag.FromFrame + 1, tag.ToFrame + 1, frames.Count);
                }
                else if (frameIndex <= tag.ToFrame)
                {
                    tag.SetFrameRange(tag.FromFrame, tag.ToFrame + 1, frames.Count);
                }
            }
        }

        private void TransformCanvas(int newWidth, int newHeight, bool flipHorizontally, bool flipVertically)
        {
            foreach (SpriteFrame frame in frames)
            {
                for (int layerIndex = 0; layerIndex < layerTracks.Count; layerIndex++)
                {
                    SpriteCel sourceCel = frame.GetCel(layerIndex);
                    SpriteCel destinationCel = new SpriteCel(newWidth, newHeight);
                    int copyWidth = Mathf.Min(width, newWidth);
                    int copyHeight = Mathf.Min(height, newHeight);
                    for (int y = 0; y < copyHeight; y++)
                    {
                        for (int x = 0; x < copyWidth; x++)
                        {
                            int sourceX = flipHorizontally ? width - 1 - x : x;
                            int sourceY = flipVertically ? height - 1 - y : y;
                            destinationCel.SetPixel(new Vector2Int(x, y), sourceCel.GetPixel32(new Vector2Int(sourceX, sourceY)));
                        }
                    }

                    frame.ReplaceCel(layerIndex, destinationCel);
                }
            }

            width = newWidth;
            height = newHeight;
        }

        private void ShiftAnimationTagsForRemovedFrame(int frameIndex)
        {
            foreach (SpriteAnimationTag tag in animationTags)
            {
                if (frameIndex < tag.FromFrame)
                {
                    tag.SetFrameRange(tag.FromFrame - 1, tag.ToFrame - 1, frames.Count);
                }
                else if (frameIndex <= tag.ToFrame)
                {
                    int newToFrame = tag.ToFrame - 1;
                    if (newToFrame < tag.FromFrame)
                    {
                        int nearestFrame = Mathf.Clamp(frameIndex, 0, frames.Count - 1);
                        tag.SetFrameRange(nearestFrame, nearestFrame, frames.Count);
                    }
                    else
                    {
                        tag.SetFrameRange(tag.FromFrame, newToFrame, frames.Count);
                    }
                }
            }
        }

        private void ClampAnimationTagsToFrameCount()
        {
            foreach (SpriteAnimationTag tag in animationTags)
            {
                tag.ClampToFrameCount(frames.Count);
            }
        }

    }
}
