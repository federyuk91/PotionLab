using System.IO;
using System.Text;
using UnityEngine;

public static class WavWriter
{
    private const int BitsPerSample = 16;
    private const short AudioFormatPcm = 1;

    public static void Write(string projectPath, float[] samples, int channels, int sampleRate)
    {
        string absolutePath = Path.GetFullPath(projectPath);
        string directory = Path.GetDirectoryName(absolutePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using (FileStream fileStream = new FileStream(absolutePath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter writer = new BinaryWriter(fileStream))
        {
            int byteRate = sampleRate * channels * BitsPerSample / 8;
            short blockAlign = (short)(channels * BitsPerSample / 8);
            int dataSize = samples.Length * BitsPerSample / 8;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write(AudioFormatPcm);
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)BitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            for (int i = 0; i < samples.Length; i++)
            {
                float clampedSample = Mathf.Clamp(samples[i], -1f, 1f);
                short pcmSample = (short)Mathf.RoundToInt(clampedSample * short.MaxValue);
                writer.Write(pcmSample);
            }
        }
    }
}
