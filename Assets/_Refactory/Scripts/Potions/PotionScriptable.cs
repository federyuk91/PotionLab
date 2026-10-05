using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PotionData", menuName = "TheGoodNightPotion/Potions/Potion Data", order = 1)]
public class PotionScriptable : ScriptableObject
{
    public enum PotionId
    {
        None,
        Health,
        Lava,
        Fire,
        Seed,
        Water,
        Ground,
        Poison,
        Ice,
        Light,
        Dark,
        EmptyBottle
    }

    // Medium is zero so existing PotionScript components retain their previous default behavior.
    public enum PotionSize
    {
        Medium = 0,
        Small = 1,
        Large = 2
    }

    public enum EffectType
    {
        healing,
        damage,
        fire,
        lava,
        ice,
        water,
        grass,
        light,
        dark,
        poisoned,
        grounded,
        Any
    }

    [Serializable]
    public sealed class PotionVariant
    {
        [SerializeField] private PotionSize size = PotionSize.Medium;
        [SerializeField] private int effectValue = 1;

        [Header("Compendium")]
        [SerializeField] private bool visibleInCompendium = true;
        [SerializeField, TextArea(3, 8)] private string narrativeDescription;
        [SerializeField] private Sprite icon;
        [SerializeField] private Sprite[] animationFrames;
        [SerializeField, Min(1f)] private float animationFrameRate = 12f;

        [Header("Effect Description")]
        [SerializeField] private bool useCustomEffectDescription;
        [SerializeField, TextArea(2, 5)] private string customEffectDescription;

        [Header("Optional Dialog Override")]
        [SerializeField] private List<string> dialogOverrides = new List<string>();

        public PotionSize Size => size;
        public int EffectValue => effectValue;
        public bool VisibleInCompendium => visibleInCompendium;
        public string NarrativeDescription => narrativeDescription;
        public Sprite Icon => icon;
        public Sprite[] AnimationFrames => animationFrames;
        public float AnimationFrameRate => animationFrameRate;
        public bool UseCustomEffectDescription => useCustomEffectDescription;
        public string CustomEffectDescription => customEffectDescription;
        public IReadOnlyList<string> DialogOverrides => dialogOverrides;
        public bool HasDialogOverride => dialogOverrides != null && dialogOverrides.Count > 0;
    }

    [Header("Identity")]
    [SerializeField] private PotionId potionId;
    public string _name;
    public Color _potColor;

    [Header("Random popped on drunk without previous status")]
    public List<string> dialogs;

    [Header("Gameplay")]
    public EffectType effectType;
    [SerializeField] private PotionSize defaultSize = PotionSize.Medium;
    [SerializeField] private List<PotionVariant> variants = new List<PotionVariant>();

    [Header("Popped if have previous status")]
    public string burned;
    public string freezed;
    public string wet;
    public string grass;
    public string tree;
    public string balrog;
    public string poisoned;
    public string pupperFish;

    [Header("Play based on previous status")]
    public AudioClip none;
    public AudioClip burned_audio;
    public AudioClip freezed_audio;
    public AudioClip wet_audio;
    public AudioClip grass_audio;
    public AudioClip tree_audio;
    public AudioClip balrog_audio;
    public AudioClip pupperFish_audio;

    public PotionId Id => potionId;
    public PotionSize DefaultSize => defaultSize;
    public IReadOnlyList<PotionVariant> Variants => variants;

    // Compatibility for systems that operate on a definition without a physical potion instance.
    public int baseValue => GetEffectValue(defaultSize);

    public bool TryGetVariant(PotionSize size, out PotionVariant variant)
    {
        if (variants != null)
        {
            foreach (PotionVariant candidate in variants)
            {
                if (candidate != null && candidate.Size == size)
                {
                    variant = candidate;
                    return true;
                }
            }
        }

        variant = null;
        return false;
    }

    public PotionVariant GetDefaultVariant()
    {
        if (TryGetVariant(defaultSize, out PotionVariant variant))
        {
            return variant;
        }

        return variants != null && variants.Count > 0 ? variants[0] : null;
    }

    public int GetEffectValue(PotionSize size)
    {
        if (TryGetVariant(size, out PotionVariant variant))
        {
            return variant.EffectValue;
        }

        PotionVariant defaultVariant = GetDefaultVariant();
        return defaultVariant != null ? defaultVariant.EffectValue : 0;
    }

    public IReadOnlyList<string> GetDialogs(PotionSize size)
    {
        if (TryGetVariant(size, out PotionVariant variant) && variant.HasDialogOverride)
        {
            return variant.DialogOverrides;
        }

        return dialogs;
    }

    public string GetDisplayName(PotionVariant variant)
    {
        return variant != null ? $"{variant.Size} {potionId}" : potionId.ToString();
    }

    public string GetCompendiumDescription(PotionVariant variant)
    {
        if (variant == null)
        {
            return string.Empty;
        }

        string effectDescription = variant.UseCustomEffectDescription
            ? variant.CustomEffectDescription
            : BuildGeneratedEffectDescription(variant.EffectValue);

        if (string.IsNullOrWhiteSpace(variant.NarrativeDescription))
        {
            return effectDescription;
        }

        if (string.IsNullOrWhiteSpace(effectDescription))
        {
            return variant.NarrativeDescription;
        }

        return string.Concat(variant.NarrativeDescription, "\n\n<b>Effect:</b>\n", effectDescription);
    }

    public string BuildGeneratedEffectDescription(int effectValue)
    {
        switch (effectType)
        {
            case EffectType.healing:
                return $"Restores {effectValue} HP.";
            case EffectType.damage:
            case EffectType.lava:
                return $"Deals {effectValue} damage.";
            case EffectType.fire:
                return "Causes Burn.";
            case EffectType.ice:
                return "Causes Freeze.";
            case EffectType.water:
                return "Causes Wet.";
            case EffectType.grass:
                return "Causes Grass.";
            case EffectType.light:
                return $"Restores {effectValue} MP.";
            case EffectType.dark:
                return $"Drains {effectValue} MP.";
            case EffectType.poisoned:
                return "Causes Poison.";
            case EffectType.grounded:
                return "Causes Grounded.";
            case EffectType.Any:
                return "No gameplay effect.";
            default:
                return string.Empty;
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        HashSet<PotionSize> knownSizes = new HashSet<PotionSize>();
        if (potionId == PotionId.None)
        {
            Debug.LogWarning($"{name}: assign a stable PotionId.", this);
        }

        if (variants == null)
        {
            Debug.LogWarning($"{name}: configure at least one potion size variant.", this);
            return;
        }

        if (variants.Count == 0)
        {
            Debug.LogWarning($"{name}: configure at least one potion size variant.", this);
        }

        foreach (PotionVariant variant in variants)
        {
            if (variant != null && !knownSizes.Add(variant.Size))
            {
                Debug.LogWarning($"{name}: potion size '{variant.Size}' is configured more than once.", this);
            }
        }

        if (!TryGetVariant(defaultSize, out PotionVariant unusedVariant))
        {
            Debug.LogWarning($"{name}: default size '{defaultSize}' has no configured variant.", this);
        }
    }
#endif
}
