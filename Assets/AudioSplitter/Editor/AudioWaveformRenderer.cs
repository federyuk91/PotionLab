using UnityEditor;
using UnityEngine;

public static class AudioWaveformRenderer
{
    private static readonly Color BackgroundColor = new Color(0.16f, 0.16f, 0.16f, 1f);
    private static readonly Color CenterLineColor = new Color(0.42f, 0.42f, 0.42f, 1f);
    private static readonly Color WaveformColor = new Color(0.18f, 0.72f, 0.92f, 1f);
    private static readonly Color GridColor = new Color(1f, 1f, 1f, 0.08f);
    private const float TargetPeakHeightUsage = 0.8f;

    public static void DrawTimeline(Rect rect, float durationSeconds)
    {
        EditorGUI.DrawRect(rect, new Color(0.12f, 0.12f, 0.12f, 1f));

        if (durationSeconds <= 0f)
        {
            return;
        }

        Handles.BeginGUI();
        Color previousColor = Handles.color;
        Handles.color = GridColor;

        int markerCount = Mathf.Clamp(Mathf.CeilToInt(rect.width / 100f), 2, 100);

        for (int i = 0; i <= markerCount; i++)
        {
            float normalized = (float)i / markerCount;
            float x = Mathf.Lerp(rect.x, rect.xMax, normalized);
            Handles.DrawLine(new Vector3(x, rect.y), new Vector3(x, rect.yMax));

            if (i < markerCount)
            {
                float time = durationSeconds * normalized;
                GUI.Label(new Rect(x + 3f, rect.y + 2f, 80f, rect.height - 2f), FormatTime(time), EditorStyles.miniLabel);
            }
        }

        Handles.color = previousColor;
        Handles.EndGUI();
    }

    public static Texture2D BuildWaveformTexture(int width, int height, float[] samples, int channels, out float verticalScale)
    {
        verticalScale = 1f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            hideFlags = HideFlags.HideAndDontSave,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        Color[] pixels = new Color[width * height];

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = BackgroundColor;
        }

        if (samples == null || samples.Length == 0 || channels <= 0)
        {
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        int centerY = height / 2;
        DrawHorizontalLine(pixels, width, height, centerY, CenterLineColor);

        int sampleFrames = samples.Length / channels;
        verticalScale = CalculateVerticalScale(samples, channels);

        for (int x = 0; x < width; x++)
        {
            int startFrame = Mathf.FloorToInt((float)x / width * sampleFrames);
            int endFrame = Mathf.FloorToInt((float)(x + 1) / width * sampleFrames);
            endFrame = Mathf.Clamp(endFrame, startFrame + 1, sampleFrames);

            float minimum = 0f;
            float maximum = 0f;

            for (int frame = startFrame; frame < endFrame; frame++)
            {
                float mixedSample = 0f;
                int sampleOffset = frame * channels;

                for (int channel = 0; channel < channels; channel++)
                {
                    mixedSample += samples[sampleOffset + channel];
                }

                mixedSample /= channels;
                minimum = Mathf.Min(minimum, mixedSample);
                maximum = Mathf.Max(maximum, mixedSample);
            }

            int minY = Mathf.RoundToInt(centerY - minimum * verticalScale * height * 0.48f);
            int maxY = Mathf.RoundToInt(centerY - maximum * verticalScale * height * 0.48f);
            DrawVerticalLine(pixels, width, height, x, Mathf.Min(minY, maxY), Mathf.Max(minY, maxY), WaveformColor);
        }

        texture.SetPixels(pixels);
        texture.Apply(false, true);
        return texture;
    }

    private static float CalculateVerticalScale(float[] samples, int channels)
    {
        float peak = 0f;
        int sampleFrames = samples.Length / channels;

        for (int frame = 0; frame < sampleFrames; frame++)
        {
            float mixedSample = 0f;
            int sampleOffset = frame * channels;

            for (int channel = 0; channel < channels; channel++)
            {
                mixedSample += samples[sampleOffset + channel];
            }

            mixedSample /= channels;
            peak = Mathf.Max(peak, Mathf.Abs(mixedSample));
        }

        if (peak <= 0f)
        {
            return 1f;
        }

        return TargetPeakHeightUsage / peak;
    }

    private static void DrawHorizontalLine(Color[] pixels, int width, int height, int y, Color color)
    {
        int clampedY = Mathf.Clamp(y, 0, height - 1);

        for (int x = 0; x < width; x++)
        {
            pixels[clampedY * width + x] = color;
        }
    }

    private static void DrawVerticalLine(Color[] pixels, int width, int height, int x, int startY, int endY, Color color)
    {
        int clampedStartY = Mathf.Clamp(startY, 0, height - 1);
        int clampedEndY = Mathf.Clamp(endY, 0, height - 1);

        for (int y = clampedStartY; y <= clampedEndY; y++)
        {
            pixels[y * width + x] = color;
        }
    }

    private static string FormatTime(float seconds)
    {
        int minutes = Mathf.FloorToInt(seconds / 60f);
        int remainingSeconds = Mathf.FloorToInt(seconds % 60f);
        return $"{minutes:00}:{remainingSeconds:00}";
    }
}
