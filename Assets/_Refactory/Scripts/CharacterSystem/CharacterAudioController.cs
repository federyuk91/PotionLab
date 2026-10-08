using System;
using System.Collections;
using System.Collections.Generic;
using InspectorValidation;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterAudioController : MonoBehaviour
    {
        private enum DrinkReactionType
        {
            Positive,
            Negative,
            Neutral
        }

        [Serializable]
        private sealed class DrinkAudioProfile
        {
            [SerializeField] private CharacterType characterType;
            [SerializeField] private AudioClip[] drinkClips = Array.Empty<AudioClip>();
            [SerializeField] private AudioClip[] positiveReactionClips = Array.Empty<AudioClip>();
            [SerializeField] private AudioClip[] negativeReactionClips = Array.Empty<AudioClip>();
            [SerializeField] private AudioClip[] neutralReactionClips = Array.Empty<AudioClip>();

            private int lastDrinkClipIndex = -1;
            private int lastPositiveReactionClipIndex = -1;
            private int lastNegativeReactionClipIndex = -1;
            private int lastNeutralReactionClipIndex = -1;

            public CharacterType CharacterType => characterType;

            public AudioClip GetRandomDrinkClip()
            {
                return GetRandomClip(drinkClips, ref lastDrinkClipIndex);
            }

            public AudioClip GetRandomReactionClip(DrinkReactionType reactionType)
            {
                switch (reactionType)
                {
                    case DrinkReactionType.Positive:
                        return GetRandomClip(positiveReactionClips, ref lastPositiveReactionClipIndex);
                    case DrinkReactionType.Negative:
                        return GetRandomClip(negativeReactionClips, ref lastNegativeReactionClipIndex);
                    default:
                        return GetRandomClip(neutralReactionClips, ref lastNeutralReactionClipIndex);
                }
            }

            private static AudioClip GetRandomClip(AudioClip[] clips, ref int lastPlayedClipIndex)
            {
                if (clips == null || clips.Length == 0)
                {
                    return null;
                }

                int validClipCount = 0;
                for (int index = 0; index < clips.Length; index++)
                {
                    if (clips[index] != null)
                    {
                        validClipCount++;
                    }
                }

                if (validClipCount == 0)
                {
                    return null;
                }

                int selectedValidClip = UnityEngine.Random.Range(0, validClipCount);
                int selectedClipIndex = GetClipIndexByValidPosition(clips, selectedValidClip);
                if (validClipCount > 1 && selectedClipIndex == lastPlayedClipIndex)
                {
                    selectedValidClip = (selectedValidClip + 1) % validClipCount;
                    selectedClipIndex = GetClipIndexByValidPosition(clips, selectedValidClip);
                }

                lastPlayedClipIndex = selectedClipIndex;
                return clips[selectedClipIndex];
            }

            private static int GetClipIndexByValidPosition(AudioClip[] clips, int validPosition)
            {
                int currentValidPosition = 0;
                for (int index = 0; index < clips.Length; index++)
                {
                    if (clips[index] == null)
                    {
                        continue;
                    }

                    if (currentValidPosition == validPosition)
                    {
                        return index;
                    }

                    currentValidPosition++;
                }

                return 0;
            }
        }

        [Header("References")]
        [SerializeField] private CharacterStats stats;
        [SerializeField] private CharacterStatusController statusController;
        [SerializeField] private TransformationManager transformationManager;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource feedbackSource;
        [SerializeField] private AudioSource spellSource;
        [SerializeField] private AudioSource potionSource;
        [SerializeField, RequiredInspectorReference] private AudioSource drinkSource;

        [Header("Drink Voices")]
        [SerializeField] private DrinkAudioProfile[] drinkAudioProfiles = Array.Empty<DrinkAudioProfile>();

        [Header("Status Effect Audio")]
        [SerializeField, RequiredInspectorReference] private AudioSource[] statusEffectSources = Array.Empty<AudioSource>();

        [Header("Stats")]
        [SerializeField] private AudioClip damageClip;
        [SerializeField] private AudioClip healClip;
        [SerializeField] private AudioClip manaUpClip;
        [SerializeField] private AudioClip manaDownClip;
        [SerializeField] private AudioClip deathClip;

        [Header("Reactions")]
        [SerializeField] private AudioClip immunityClip;
        [SerializeField] private AudioClip explosionClip;

        private BaseCharacter currentCharacter;
        private Coroutine pendingDrinkReaction;
        private bool characterDead;

        private void Awake()
        {
            if (stats == null)
                stats = GetComponent<CharacterStats>();

            if (statusController == null)
                statusController = GetComponent<CharacterStatusController>();

            if (transformationManager == null)
                transformationManager = GetComponent<TransformationManager>();

            if (feedbackSource == null)
                feedbackSource = GetComponent<AudioSource>();

            if (spellSource == null)
                spellSource = feedbackSource;

            if (potionSource == null)
                potionSource = feedbackSource;

            SubscribeSharedEvents();
        }

        private void Start()
        {
            SubscribeCurrentCharacter();
        }

        private void OnDestroy()
        {
            UnsubscribeSharedEvents();
            UnsubscribeCurrentCharacter();
        }

        private void SubscribeSharedEvents()
        {
            if (stats != null)
            {
                stats.OnHealtDown += PlayDamage;
                stats.OnHealtUp += PlayHeal;
                stats.OnManaUp += PlayManaUp;
                stats.OnManaDown += PlayManaDown;
                stats.HPChanged += HandleHPChanged;
                stats.OnDeath += PlayDeath;
            }

            if (statusController != null)
            {
                statusController.OnImmunity += PlayImmunity;
                statusController.OnExplosion += PlayExplosion;
            }

            if (transformationManager != null)
            {
                transformationManager.OnTransformation += OnTransformation;
            }
        }

        private void UnsubscribeSharedEvents()
        {
            if (stats != null)
            {
                stats.OnHealtDown -= PlayDamage;
                stats.OnHealtUp -= PlayHeal;
                stats.OnManaUp -= PlayManaUp;
                stats.OnManaDown -= PlayManaDown;
                stats.HPChanged -= HandleHPChanged;
                stats.OnDeath -= PlayDeath;
            }

            if (statusController != null)
            {
                statusController.OnImmunity -= PlayImmunity;
                statusController.OnExplosion -= PlayExplosion;
            }

            if (transformationManager != null)
            {
                transformationManager.OnTransformation -= OnTransformation;
            }
        }

        private void OnTransformation(CharacterType fromType, CharacterType toType)
        {
            UnsubscribeCurrentCharacter();
            SubscribeCurrentCharacter();
        }

        private void SubscribeCurrentCharacter()
        {
            if (transformationManager == null || transformationManager.Current == null)
                return;

            if (currentCharacter == transformationManager.Current)
                return;

            currentCharacter = transformationManager.Current;
            currentCharacter.PotionEffectResolving += OnPotionEffectResolving;
            currentCharacter.SpellCastSucceeded += OnSpellCastSucceeded;
        }

        private void UnsubscribeCurrentCharacter()
        {
            if (currentCharacter == null)
                return;

            currentCharacter.PotionEffectResolving -= OnPotionEffectResolving;
            currentCharacter.SpellCastSucceeded -= OnSpellCastSucceeded;
            currentCharacter = null;
        }

        private void OnPotionEffectResolving(BaseCharacter character, PotionScriptable potion, IReadOnlyCollection<Status> previousStatuses)
        {
            if (characterDead)
            {
                return;
            }

            if (potion == null)
            {
                Debug.LogWarning("AUDIO: Cannot play potion audio because the potion reference is missing.", this);
                return;
            }

            CharacterType characterType = character.GetCharacterForm();
            int previousHP = stats != null ? stats.HP : 0;
            int previousMP = stats != null ? stats.MP : 0;
            AudioClip drinkClip = PlayDrinkAudio(characterType);
            AudioClip potionClip = GetPotionClip(characterType, potion, previousStatuses);
            PlayOneShot(potionSource, potionClip, $"potion '{potion._name}'");

            if (drinkClip != null)
            {
                if (pendingDrinkReaction != null)
                {
                    StopCoroutine(pendingDrinkReaction);
                }

                pendingDrinkReaction = StartCoroutine(PlayReactionAfterDrink(
                    characterType,
                    drinkClip,
                    previousHP,
                    previousMP));
            }
        }

        private AudioClip PlayDrinkAudio(CharacterType characterType)
        {
            DrinkAudioProfile profile = FindDrinkAudioProfile(characterType);
            if (profile == null)
            {
                return null;
            }

            AudioClip clip = profile.GetRandomDrinkClip();
            if (clip == null)
            {
                return null;
            }

            StopAudioSource(drinkSource);
            PlayOneShot(drinkSource, clip, $"{characterType} drink voice");
            return clip;
        }

        private IEnumerator PlayReactionAfterDrink(
            CharacterType characterType,
            AudioClip drinkClip,
            int previousHP,
            int previousMP)
        {
            float playbackStartTime = Time.realtimeSinceStartup;
            yield return null;

            DrinkReactionType reactionType = GetDrinkReactionType(previousHP, previousMP);
            float pitch = drinkSource != null ? Mathf.Abs(drinkSource.pitch) : 1f;
            float drinkDuration = pitch > 0f ? drinkClip.length / pitch : drinkClip.length;
            float remainingDuration = drinkDuration - (Time.realtimeSinceStartup - playbackStartTime);
            if (remainingDuration > 0f)
            {
                yield return new WaitForSecondsRealtime(remainingDuration);
            }

            pendingDrinkReaction = null;
            if (characterDead)
            {
                yield break;
            }

            DrinkAudioProfile profile = FindDrinkAudioProfile(characterType);
            AudioClip reactionClip = profile != null ? profile.GetRandomReactionClip(reactionType) : null;
            if (reactionClip != null)
            {
                PlayOneShot(drinkSource, reactionClip, $"{characterType} {reactionType} drink reaction");
            }
        }

        private DrinkReactionType GetDrinkReactionType(int previousHP, int previousMP)
        {
            if (stats == null)
            {
                return DrinkReactionType.Neutral;
            }

            int hpDelta = stats.HP - previousHP;
            int mpDelta = stats.MP - previousMP;
            if (hpDelta < 0 || mpDelta < 0)
            {
                return DrinkReactionType.Negative;
            }

            return hpDelta > 0 || mpDelta > 0
                ? DrinkReactionType.Positive
                : DrinkReactionType.Neutral;
        }

        private DrinkAudioProfile FindDrinkAudioProfile(CharacterType characterType)
        {
            if (drinkAudioProfiles == null)
            {
                return null;
            }

            foreach (DrinkAudioProfile profile in drinkAudioProfiles)
            {
                if (profile != null && profile.CharacterType == characterType)
                {
                    return profile;
                }
            }

            return null;
        }

        private void OnSpellCastSucceeded(BaseCharacter character, int index, Spell spell, bool powered)
        {
            if (characterDead)
            {
                return;
            }

            if (spell == null)
            {
                Debug.LogWarning($"AUDIO: Cannot play spell audio for {character.name} spell index {index} because the spell reference is missing.", this);
                return;
            }

            PlayOneShot(spellSource, spell.GetRandomAudioClip(), $"spell '{spell.nome}' on {character.name}");
        }

        private AudioClip GetPotionClip(CharacterType characterType, PotionScriptable potion, IReadOnlyCollection<Status> previousStatuses)
        {
            switch (characterType)
            {
                case CharacterType.Balrog:
                    return potion.balrog_audio != null ? potion.balrog_audio : potion.none;
                case CharacterType.Tree:
                    return potion.tree_audio != null ? potion.tree_audio : potion.none;
                case CharacterType.PupperFish:
                    return potion.pupperFish_audio != null ? potion.pupperFish_audio : potion.none;
            }

            if (HasPreviousStatus(previousStatuses, Status.Burned))
                return potion.burned_audio != null ? potion.burned_audio : potion.none;

            if (HasPreviousStatus(previousStatuses, Status.Freezed))
                return potion.freezed_audio != null ? potion.freezed_audio : potion.none;

            if (HasPreviousStatus(previousStatuses, Status.Wet))
                return potion.wet_audio != null ? potion.wet_audio : potion.none;

            if (HasPreviousStatus(previousStatuses, Status.Grass))
                return potion.grass_audio != null ? potion.grass_audio : potion.none;

            switch (potion.effectType)
            {
                case PotionScriptable.EffectType.fire:
                    return potion.burned_audio != null ? potion.burned_audio : potion.none;
            }

            return potion.none;
        }

        private bool HasPreviousStatus(IReadOnlyCollection<Status> statuses, Status status)
        {
            if (statuses == null)
                return false;

            foreach (Status currentStatus in statuses)
            {
                if (currentStatus == status)
                    return true;
            }

            return false;
        }

        private void PlayDamage()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, damageClip, "damage feedback");
        }

        private void PlayHeal()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, healClip, "heal feedback");
        }

        private void PlayManaUp()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, manaUpClip, "mana up feedback");
        }

        private void PlayManaDown()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, manaDownClip, "mana down feedback");
        }

        private void PlayDeath()
        {
            characterDead = true;
            if (pendingDrinkReaction != null)
            {
                StopCoroutine(pendingDrinkReaction);
                pendingDrinkReaction = null;
            }

            StopActiveAudio();
            PlayOneShot(feedbackSource, deathClip, "death feedback");
        }

        private void PlayImmunity()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, immunityClip, "immunity reaction");
        }

        private void PlayExplosion()
        {
            if (characterDead)
            {
                return;
            }

            PlayOneShot(feedbackSource, explosionClip, "explosion reaction");
        }

        private void HandleHPChanged(int currentHP, int maximumHP)
        {
            if (currentHP > 0)
            {
                characterDead = false;
            }
        }

        private void StopActiveAudio()
        {
            StopAudioSource(feedbackSource);

            if (spellSource != feedbackSource)
            {
                StopAudioSource(spellSource);
            }

            if (potionSource != feedbackSource && potionSource != spellSource)
            {
                StopAudioSource(potionSource);
            }

            if (drinkSource != feedbackSource
                && drinkSource != spellSource
                && drinkSource != potionSource)
            {
                StopAudioSource(drinkSource);
            }

            if (statusEffectSources == null)
            {
                return;
            }

            foreach (AudioSource statusEffectSource in statusEffectSources)
            {
                StopAudioSource(statusEffectSource);
            }
        }

        private static void StopAudioSource(AudioSource source)
        {
            if (source != null)
            {
                source.Stop();
            }
        }

        private void PlayOneShot(AudioSource source, AudioClip clip, string context)
        {
            if (source == null)
            {
                Debug.LogWarning($"AUDIO: Cannot play {context} because the AudioSource reference is missing.", this);
                return;
            }

            if (clip == null)
            {
                Debug.LogWarning($"AUDIO: Cannot play {context} because the AudioClip reference is missing.", this);
                return;
            }

            source.PlayOneShot(clip);
        }
    }
}

