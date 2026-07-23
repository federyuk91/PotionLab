using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class CharacterAudioController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private CharacterStats stats;
        [SerializeField] private CharacterStatusController statusController;
        [SerializeField] private TransformationManager transformationManager;

        [Header("Audio Sources")]
        [SerializeField] private AudioSource feedbackSource;
        [SerializeField] private AudioSource spellSource;
        [SerializeField] private AudioSource potionSource;

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
            if (potion == null)
            {
                Debug.LogWarning("AUDIO: Cannot play potion audio because the potion reference is missing.", this);
                return;
            }

            AudioClip clip = GetPotionClip(character.GetCharacterForm(), potion, previousStatuses);
            PlayOneShot(potionSource, clip, $"potion '{potion._name}'");
        }

        private void OnSpellCastSucceeded(BaseCharacter character, int index, Spell spell, bool powered)
        {
            if (spell == null)
            {
                Debug.LogWarning($"AUDIO: Cannot play spell audio for {character.name} spell index {index} because the spell reference is missing.", this);
                return;
            }

            PlayOneShot(spellSource, spell.castAudio, $"spell '{spell.spellName}' on {character.name}");
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
            PlayOneShot(feedbackSource, damageClip, "damage feedback");
        }

        private void PlayHeal()
        {
            PlayOneShot(feedbackSource, healClip, "heal feedback");
        }

        private void PlayManaUp()
        {
            PlayOneShot(feedbackSource, manaUpClip, "mana up feedback");
        }

        private void PlayManaDown()
        {
            PlayOneShot(feedbackSource, manaDownClip, "mana down feedback");
        }

        private void PlayDeath()
        {
            PlayOneShot(feedbackSource, deathClip, "death feedback");
        }

        private void PlayImmunity()
        {
            PlayOneShot(feedbackSource, immunityClip, "immunity reaction");
        }

        private void PlayExplosion()
        {
            PlayOneShot(feedbackSource, explosionClip, "explosion reaction");
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
