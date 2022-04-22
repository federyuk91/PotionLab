using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/Potion", order = 1)]
public class PotionScriptable : ScriptableObject
{
    public string _name;
    public Color _potColor;
    [Header("Random popped on drunk without previous status")]
    public List<string> dialogs;

    public EffectType effectType;

    [Header("Popped if have previous status")]
    public string burned;
    public string freezed, wet, grass, tree, balrog;

    [Header("Play based on previous status")]
    public AudioClip none;
    public AudioClip burned_audio, freezed_audio, wet_audio, grass_audio, tree_audio, balrog_audio;
    public enum EffectType
    {
        healing,
        damage,
        burned,//damageOverTime,
        lava,
        freezed,
        wet,
        grass,
        none,
    }



    public int baseValue = 1; //Quanti danni fa o quanto cura
}
