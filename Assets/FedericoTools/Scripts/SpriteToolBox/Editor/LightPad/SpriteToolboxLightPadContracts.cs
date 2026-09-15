using System;
using UnityEditor;
using UnityEngine;

namespace FedericoTools.SpriteToolBox.Editor
{
    public interface ILightPadState
    {
        bool IsSelected { get; }
        bool IsEnabled { get; }
        bool HasReferenceCapture { get; }
        SpriteLightPadSourceType SourceType { get; }
        Texture SourceTexture { get; }
        Camera SourceCamera { get; }
        float Opacity { get; }
        SpriteLightPadAlignment Alignment { get; }
        float ReferenceZoom { get; }
        SpriteLightPadSamplingMode SamplingMode { get; }
        bool UseDithering { get; }
        string Status { get; }
    }

    public interface ILightPadActions
    {
        void SetSelected(bool value);
        void SetEnabled(bool value);
        void SetSourceType(SpriteLightPadSourceType value);
        void SetSourceTexture(Texture value);
        void SetSourceCamera(Camera value);
        void SetOpacity(float value);
        void SetAlignment(SpriteLightPadAlignment value);
        void SetReferenceZoom(float value);
        void SetSamplingMode(SpriteLightPadSamplingMode value);
        void SetDithering(bool value);
        void Refresh();
        void ResetReferencePosition();
        void ImportReferenceAsLayer();
    }

    public enum SpriteLightPadSourceType { Texture, Camera, SceneView }
    public enum SpriteLightPadAlignment { MatchHeight, MatchWidth, ZoomLevel }
    public enum SpriteLightPadSamplingMode { Nearest, Area }

    [Serializable]
    public sealed class SpriteToolboxLightPadSettings
    {
        public bool IsEnabled;
        public SpriteLightPadSourceType SourceType = SpriteLightPadSourceType.SceneView;
        public Texture SourceTexture;
        public Camera SourceCamera;
        [Range(0f, 1f)] public float Opacity = 0.35f;
        public SpriteLightPadAlignment Alignment = SpriteLightPadAlignment.MatchHeight;
        [Range(0.01f, 2f)] public float ReferenceZoom = 1f;
        public SpriteLightPadSamplingMode SamplingMode = SpriteLightPadSamplingMode.Nearest;
        public bool UseDithering;
        public Vector2 PositionOffset;
    }

    [Serializable]
    public sealed class SpriteToolboxLightPadEditorState
    {
        public bool IsSelected;
    }

}
