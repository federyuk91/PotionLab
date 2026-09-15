using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    internal sealed class SpriteToolboxPlaybackCoordinator : IDisposable
    {
        private readonly SpriteToolboxSession session;
        private readonly Action repaint;

        public SpriteToolboxPlaybackCoordinator(SpriteToolboxSession session, Action repaint)
        {
            this.session = session ?? throw new ArgumentNullException(nameof(session));
            this.repaint = repaint ?? throw new ArgumentNullException(nameof(repaint));
            EditorApplication.update += Update;
        }

        public void Dispose()
        {
            EditorApplication.update -= Update;
        }

        private void Update()
        {
            SpriteDocument document = session.Document;
            if (document == null) return;
            SpriteAnimationTag activeTag = session.ActiveAnimationTagIndex >= 0 && session.ActiveAnimationTagIndex < document.AnimationTags.Count
                ? document.AnimationTags[session.ActiveAnimationTagIndex]
                : null;
            int frameIndex = session.SelectedFrameIndex;
            if (!session.Playback.TryAdvance(document, ref frameIndex, EditorApplication.timeSinceStartup, activeTag)) return;
            session.SelectCell(frameIndex, Mathf.Clamp(session.SelectedLayerIndex, 0, document.LayerTracks.Count - 1));
            repaint();
        }
    }
}
