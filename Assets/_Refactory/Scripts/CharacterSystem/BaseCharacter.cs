
using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

namespace CharacterSystem
{
    public abstract class BaseCharacter : MonoBehaviour, ICharacter
    {
        private static readonly int Spell1Trigger = Animator.StringToHash("Spell1");
        private static readonly int Spell2Trigger = Animator.StringToHash("Spell2");
        private static readonly int Spell3Trigger = Animator.StringToHash("Spell3");

        [Header("References")]
        public List<Spell> spellList;
        [SerializeField] public Animator animator;
        [SerializeField] public CharacterStats stats;
        [SerializeField] public CharacterStatusController status;
        [SerializeField] protected TransformationManager transformationManager;
        [SerializeField] protected DialogManager dialogManager;

        [Header("Character Light")]
        [SerializeField] private Color transformationLightColor = Color.white;

        public event Action<BaseCharacter, PotionScriptable, IReadOnlyCollection<Status>> PotionEffectResolving;
        public event Action<BaseCharacter, PotionScriptable> PotionEffectResolved;
        public event Action<BaseCharacter, int, Spell, bool> SpellCastSucceeded;

        public Color TransformationLightColor => transformationLightColor;
        public bool IsReturnMagePending { get; private set; }


        private void Awake()
        {
            if (animator == null)
            {
                Debug.LogWarning($"{name}: Animator reference is missing in Inspector. Using local fallback; assign it explicitly before production.", this);
                animator = GetComponent<Animator>();
            }

            if (stats == null)
            {
                Debug.LogWarning($"{name}: CharacterStats reference is missing in Inspector. Using parent fallback; assign it explicitly before production.", this);
                stats = GetComponentInParent<CharacterStats>();
            }

            if (status == null)
            {
                Debug.LogWarning($"{name}: CharacterStatusController reference is missing in Inspector. Using parent fallback; assign it explicitly before production.", this);
                status = GetComponentInParent<CharacterStatusController>();
            }

            if (transformationManager == null)
            {
                Debug.LogWarning($"{name}: TransformationManager reference is missing in Inspector. Using parent fallback; assign it explicitly before production.", this);
                transformationManager = GetComponentInParent<TransformationManager>();
            }

            if (dialogManager == null)
            {
                Debug.LogWarning($"{name}: DialogManager reference is missing in Inspector. Using parent fallback; assign it explicitly before production.", this);
                dialogManager = GetComponentInParent<DialogManager>();
            }

            ValidateRequiredReferences();
        }

        private void ValidateRequiredReferences()
        {
            if (animator == null)
            {
                Debug.LogError($"{name}: Animator reference is required.", this);
            }

            if (stats == null)
            {
                Debug.LogError($"{name}: CharacterStats reference is required.", this);
            }

            if (status == null)
            {
                Debug.LogError($"{name}: CharacterStatusController reference is required.", this);
            }

            if (transformationManager == null)
            {
                Debug.LogError($"{name}: TransformationManager reference is required.", this);
            }

            if (dialogManager == null)
            {
                Debug.LogError($"{name}: DialogManager reference is required.", this);
            }
        }

        public virtual void OnEnable()
        {
        }
        public virtual void OnDisable()
        {
        }

        public abstract CharacterType GetCharacterForm();

        public void Drunk(PotionScript potion)
        {
            if (potion == null)
            {
                return;
            }

            Drunk(potion.potion);
        }

        public void Drunk(PotionScriptable potion)
        {
            if (potion == null)
            {
                return;
            }

            dialogManager.OnPotionDrunk(potion, GetCharacterForm(), status);
            StartCoroutine(DrunkRoutine(potion));
        }

        private IEnumerator DrunkRoutine(PotionScriptable potion)
        {
            animator.SetTrigger("Drunk");
            List<Status> previousStatuses = new List<Status>(status.GetCurrentStatuses());
            yield return new WaitForSeconds(1f);

            PotionEffectResolving?.Invoke(this, potion, previousStatuses);

            switch (potion.effectType)
            {
                case PotionScriptable.EffectType.healing:
                    ApplyHeal(potion);
                    break;
                case PotionScriptable.EffectType.fire:
                    ApplyFire(potion);
                    break;
                case PotionScriptable.EffectType.lava:
                    ApplyLava(potion);
                    break;
                case PotionScriptable.EffectType.ice:
                    ApplyIce(potion);
                    break;
                case PotionScriptable.EffectType.water:
                    ApplyWet(potion);
                    break;
                case PotionScriptable.EffectType.grass:
                    ApplyGrass(potion);
                    break;
                case PotionScriptable.EffectType.light:
                    ApplyLight(potion);
                    break;
                case PotionScriptable.EffectType.dark:
                    ApplyDark(potion);
                    break;
                case PotionScriptable.EffectType.poisoned:
                    ApplyPoison(potion);
                    break;
                case PotionScriptable.EffectType.grounded:
                    ApplyGround(potion);
                    break;

                    default:
                    Debug.LogWarning("Potion effect not handled in DrunkRoutine: "+ potion.effectType.ToString());
                    break;
            }

            PotionEffectResolved?.Invoke(this, potion);
        }

        // 🔴 OBBLIGATORI: se mancano, NON COMPILA

        public abstract void FireTick();
        public abstract void PoisonTick();
        public abstract void GroundTick();
        public abstract void IceTick();

        public virtual float GetFireTickDelay() { 
            return Mathf.Infinity;
        }
        public virtual float GetPoisonTickDelay()
        {
            return Mathf.Infinity;
        }
        public virtual float GetGroundTickDelay()
        {
            return Mathf.Infinity;
        }
        public virtual float GetIceTickDelay()
        {
            return Mathf.Infinity;
        }

        public abstract void ApplyHeal(PotionScriptable ps);
        public abstract void ApplyFire(PotionScriptable ps);
        public abstract void ApplyLava(PotionScriptable ps);
        public abstract void ApplyIce(PotionScriptable ps);
        public abstract void ApplyWet(PotionScriptable ps);
        public abstract void ApplyGrass(PotionScriptable ps);
        public abstract void ApplyLight(PotionScriptable ps);
        public abstract void ApplyDark(PotionScriptable ps);
        public abstract void ApplyPoison(PotionScriptable ps);
        public abstract void ApplyGround(PotionScriptable ps);

        public void Cast(int index, bool powered)
        {
            if (spellList == null || index < 0 || index >= spellList.Count)
            {
                Debug.LogError($"{name} has no spell at index {index}", this);
                return;
            }

            if (CastSpell(index, powered))
            {
                SpellCastSucceeded?.Invoke(this, index, spellList[index], powered);
                TriggerSpellAnimation(index);
            }
        }

        private void TriggerSpellAnimation(int index)
        {
            switch (index)
            {
                case 0:
                    animator.SetTrigger(Spell1Trigger);
                    break;
                case 1:
                    animator.SetTrigger(Spell2Trigger);
                    break;
                case 2:
                    animator.SetTrigger(Spell3Trigger);
                    break;
            }
        }

        protected abstract bool CastSpell(int index, bool powered);


        public abstract void OnEnterTransformation();
        public abstract void OnExitTransformation();

        public void ReturnMage()
        {
            IsReturnMagePending = false;
            transformationManager.SwitchTo(CharacterType.Mage);
        }

        protected void TriggerReturnMageAnimation()
        {
            IsReturnMagePending = true;
            animator.SetTrigger("ReturnMage");
        }
    }
}
