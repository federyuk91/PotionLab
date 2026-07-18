
using System.Collections;
using System.Collections.Generic;
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


        private void Awake()
        {
            animator = GetComponent<Animator>();
            stats = GetComponentInParent<CharacterStats>();
            status = GetComponentInParent<CharacterStatusController>();
            transformationManager = GetComponentInParent<TransformationManager>();
            dialogManager = GetComponentInParent<DialogManager>();
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
            dialogManager.OnPotionDrunk(potion.potion, GetCharacterForm(), status);
            StartCoroutine(DrunkRoutine(potion));
        }

        private IEnumerator DrunkRoutine(PotionScript potion)
        {
            animator.SetTrigger("Drunk");
            yield return new WaitForSeconds(1f);

            switch (potion.potion.effectType)
            {
                case PotionScriptable.EffectType.healing:
                    ApplyHeal(potion.potion);
                    break;
                case PotionScriptable.EffectType.fire:
                    ApplyFire(potion.potion);
                    break;
                case PotionScriptable.EffectType.lava:
                    ApplyLava(potion.potion);
                    break;
                case PotionScriptable.EffectType.ice:
                    ApplyIce(potion.potion);
                    break;
                case PotionScriptable.EffectType.water:
                    ApplyWet(potion.potion);
                    break;
                case PotionScriptable.EffectType.grass:
                    ApplyGrass(potion.potion);
                    break;
                case PotionScriptable.EffectType.light:
                    ApplyLight(potion.potion);
                    break;
                case PotionScriptable.EffectType.dark:
                    ApplyDark(potion.potion);
                    break;
                case PotionScriptable.EffectType.poisoned:
                    ApplyPoison(potion.potion);
                    break;
                case PotionScriptable.EffectType.grounded:
                    ApplyGround(potion.potion);
                    break;

                    default:
                    Debug.LogWarning("Potion effect not handled in DrunkRoutine: "+ potion.potion.effectType.ToString());
                    break;
            }
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
            transformationManager.SwitchTo(CharacterType.Mage);
        }
    }
}
