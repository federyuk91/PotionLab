using System.Collections;
using UnityEngine;

namespace CharacterSystem
{
    public abstract class BaseCharacter : MonoBehaviour, ICharacter
    {
        [Header("References")]
        [SerializeField] public Animator animator;
        [SerializeField] protected CharacterStats stats;
        [SerializeField] protected CharacterStatusController status;
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

        private void OnEnable()
        {
            OnEnterTransformation();
        }
        private void OnDisable()
        {
            OnExitTransformation();
        }

        public abstract CharacterType GetCharacterForm();

        public void Drunk(PotionScript potion)
        {
            dialogManager.OnPotionDrunk(potion.potion.effectType, GetCharacterForm(), status);
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
                case PotionScriptable.EffectType.magicUP:
                    ApplyLight(potion.potion);
                    break;
                case PotionScriptable.EffectType.magicDown:
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

        public abstract void OnEnterTransformation();
        public abstract void OnExitTransformation();

        public void ReturnMage()
        {
            transformationManager.SwitchTo(CharacterType.Mage);
        }
    }
}
