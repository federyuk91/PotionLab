using UnityEngine;
namespace CharacterSystem
{
    public class YetiCharacter : BaseCharacter
    {
        private void OnEnable()
        {
            Debug.Log("Yeti on enable");
            stats.OnHealtUp += CheckMutation;
            stats.OnHealtDown += CheckMutation;
            stats.OnManaDown += CheckMutation;
            stats.OnManaUp += CheckMutation;
        }
        private void OnDisable()
        {
            Debug.Log("Yeti on disable");
            stats.OnHealtUp -= CheckMutation;
            stats.OnHealtDown -= CheckMutation;
            stats.OnManaDown -= CheckMutation;
            stats.OnManaUp -= CheckMutation;
        }

        public void CheckMutation()
        {
            if (stats.HP == stats.MP)
                transformationManager.SwitchTo(CharacterType.Mage);
        }
        public override void ApplyDark(PotionScriptable ps)
        {
            if (stats.MP > 0)
            {
                stats.LoseMana(1);
                return;
            }
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
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
            stats.AddMana(ps.baseValue);
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            status.Add(Status.Poisoned);
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
                status.poisonLevel--;
                if (status.poisonLevel <= 0)
                {
                    status.Remove(Status.Poisoned);
                }
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
