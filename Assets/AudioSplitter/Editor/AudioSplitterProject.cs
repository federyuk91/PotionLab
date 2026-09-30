using System.Collections.Generic;
using UnityEngine;

public class AudioSplitterProject : ScriptableObject
{
    public AudioClip sourceClip;
    public List<int> cutSamples = new List<int>();
    public List<string> segmentNames = new List<string>();
    public List<int> selectedSegmentIndices = new List<int>();
    public int selectedSegmentIndex;
    public float horizontalZoom = 1f;
    public float autoCutMinimumSilenceSeconds = 0.2f;
    public float silenceTolerancePercent = 2f;
}
