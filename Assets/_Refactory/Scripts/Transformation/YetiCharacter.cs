using System;
using UnityEngine;
using ProgressSystem;
namespace CharacterSystem
{
    public class YetiCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private GameObject punchObject;
        private int punchPotionHitCount;

        public event Action StatsBalanced;

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];

            switch (i)
            {
                case 0:
                    return CastIceZone(spell);
                case 1:
                    return CastConvert(spell, powered);
                case 2:
                    return CastPunch(spell, powered);
                default:
                    Debug.LogError($"{name} has no Yeti spell behaviour for index {i}", this);
                    return false;
            }
        }

        private bool CastIceZone(Spell spell)
        {
            if (!TrySpendMana(spell, "eh?"))
            {
                return false;
            }

            transformationManager.lightController.ToggleLightField(LightFieldType.Ice);

            if (transformationManager.lightController.IsLightFieldActive(LightFieldType.Ice))
            {
                dialogManager.PopDialog("Dance Move!", 2f);
            }
            else
            {
                dialogManager.PopDialog("nooooooo", 2f);
            }

            return true;
        }

        private bool CastConvert(Spell spell, bool powered)
        {
            if (stats.HP >= stats.MaxHP)
            {
                RequestAchievement(AchievementId.SmartButFart);

                dialogManager.PopDialog("FULL", 1f);
                return false;
            }

            if (!HasEnoughMana(spell, "eh?"))
            {
                return false;
            }

            int healing = powered ? 4 : 3;
            stats.ModifyHPAndMP(healing, -spell.costo);
            return true;
        }

        private bool CastPunch(Spell spell, bool powered)
        {
            if (!HasEnoughMana(spell, "eh?"))
            {
                return false;
            }

            int selfDamage = powered ? 1 : 2;
            stats.ModifyHPAndMP(-selfDamage, -spell.costo);
            punchPotionHitCount = 0;

            if (punchObject == null)
            {
                Debug.LogWarning($"{name} has no punch object assigned.", this);
                return true;
            }

            punchObject.SetActive(false);
            punchObject.SetActive(true);
            return true;
        }

        public void RegisterPunchPotionHit()
        {
            punchPotionHitCount++;
            if (punchPotionHitCount >= 4)
            {
                RequestAchievement(AchievementId.FalconPunch);
            }
        }

        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            if (!HasEnoughMana(spell, notEnoughManaDialog))
            {
                return false;
            }

            stats.LoseMana(spell.costo);
            return true;
        }

        private bool HasEnoughMana(Spell spell, string notEnoughManaDialog)
        {
            if (stats.HasMana(spell.costo))
            {
                return true;
            }

            dialogManager.PopDialog(notEnoughManaDialog, 1f);
            return false;
        }

        public override void OnEnable()
        {
            base.OnEnable();
            Debug.Log("Yeti on enable");
            stats.HPChanged += CheckMutation;
            stats.MPChanged += CheckMutation;
        }
        public override void OnDisable()
        {
            base.OnDisable();
            Debug.Log("Yeti on disable");
            stats.HPChanged -= CheckMutation;
            stats.MPChanged -= CheckMutation;
        }

        private void CheckMutation(int currentValue, int maximumValue)
        {
            if (!isActiveAndEnabled || IsReturnMagePending || stats == null || stats.HP <= 0)
            {
                return;
            }

            if (stats.HP != stats.MP)
            {
                return;
            }

            StatsBalanced?.Invoke();
            TriggerReturnMageAnimation();
        }
        public override void ApplyDark(PotionScriptable ps)
        {
            if (stats.MP > 0)
            {
                stats.LoseMana(1);
                return;
            }
            animator.SetTrigger("isDamaged");
            stats.TakeDamage(2);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            status.Increase(Status.Grounded);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            if(status.Has(Status.Grounded))
            {
                status.Remove(Status.Grounded);
                return;
            }
            animator.SetTrigger("isDamaged");
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.ModifyHPAndMP(-ps.baseValue, ps.baseValue);
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            status.Increase(Status.Poisoned);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if (status.Has(Status.Poisoned))
            {
                status.Remove(Status.Poisoned);
                return;
            }
            status.TriggerImmunity();
        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Yeti;
        }

        #region TicksFX
        public override void FireTick()
        {
        }

        public override float GetGroundTickDelay()
        {
            return 5f;
        }
        public override void GroundTick()
        {
            if (status.groundLevel == 3)
            {
                animator.SetTrigger("isDamaged");
                stats.TakeDamage(2);
            }
        }


        public override float GetPoisonTickDelay()
        {
            if(status.Has(Status.Grounded))
            {
                return 5f;
            }
            return 4f;
        }
        public override void PoisonTick()
        {
            animator.SetTrigger("isDamaged");
            if (status.Has(Status.Grounded))
                stats.TakeDamage(1); //Se è interrato prende 1 danno da veleno, utile per annullare la trasformazione ma non ridusce il poisonLevel
            else
            {
                stats.TakeDamage(1);
                status.Decrease(Status.Poisoned);
            }
        }

        public override void IceTick()
        {
        }
        #endregion

        public override void OnEnterTransformation()
        {
            Debug.Log("Transformed into Yeti!");
        }

        public override void OnExitTransformation()
        {
            Debug.Log("Exiting Yeti form! Returning to mage form");
        }
        

    }
}

