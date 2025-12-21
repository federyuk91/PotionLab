using System.Collections;
using UnityEngine;

namespace CharacterSystem
{
    public abstract class BaseCharacter : MonoBehaviour, ICharacter
    {
        [Header("References")]
        [SerializeField] protected Animator animator;
        [SerializeField] protected CharacterStats stats;
        [SerializeField] protected CharacterStatusController status;

        private void Awake()
        {
            animator = GetComponent<Animator>();
            stats = GetComponentInParent<CharacterStats>();
            status = GetComponentInParent<CharacterStatusController>();
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
                case PotionScriptable.EffectType.damage:
                    ApplyDamage(potion.potion);
                    break;
                case PotionScriptable.EffectType.burned:
                    ApplyFire(potion.potion);
                    break;
                case PotionScriptable.EffectType.lava:
                    ApplyLava(potion.potion);
                    break;
                case PotionScriptable.EffectType.freezed:
                    ApplyFreezed(potion.potion);
                    break;
                case PotionScriptable.EffectType.wet:
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
                case PotionScriptable.EffectType.none:
                    ApplyNone(potion.potion);
                    break;
            }
        }

        // 🔴 OBBLIGATORI: se mancano, NON COMPILA

        public abstract void FireTickFX();
        public abstract void PoisonTick();
        public abstract void GroundTick();
        public abstract void IceTick();

        public abstract float GetFireTickDelay();
        public abstract float GetPoisonTickDelay();
        public abstract float GetGroundTickDelay();
        public abstract float GetIceTickDelay();

        public abstract void ApplyHeal(PotionScriptable ps);
        public abstract void ApplyDamage(PotionScriptable ps);
        public abstract void ApplyFire(PotionScriptable ps);
        public abstract void ApplyLava(PotionScriptable ps);
        public abstract void ApplyFreezed(PotionScriptable ps);
        public abstract void ApplyWet(PotionScriptable ps);
        public abstract void ApplyGrass(PotionScriptable ps);
        public abstract void ApplyLight(PotionScriptable ps);
        public abstract void ApplyDark(PotionScriptable ps);
        public abstract void ApplyPoison(PotionScriptable ps);
        public abstract void ApplyGround(PotionScriptable ps);
        public abstract void ApplyNone(PotionScriptable ps);

        public abstract void OnEnterTransformation();
        public abstract void OnExitTransformation();
    }
}
