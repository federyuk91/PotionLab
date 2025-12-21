using UnityEngine;
namespace CharacterSystem
{
    public class TreeCharacter : BaseCharacter
    {
        public override void ApplyDark(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            if(status.Has(Status.Grounded))
            {             
                status.TriggerImmunity();
                return;
            }
            status.Increase(Status.Burned);
        }

        public override void ApplyFreezed(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                stats.Heal(2);
                animator.SetTrigger("ReturnMage");
                return;
            }
            stats.TakeDamage(2);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            stats.AddMana(2);
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                status.Increase(Status.Grounded);
                return;
            }
            status.Increase(Status.Grounded);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            if (status.Has(Status.Grounded))
            {
                status.Remove(Status.Grounded);
                return;
            }
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.AddMana(ps.baseValue);
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            if (status.Has(Status.Grounded))
            {
                status.TriggerImmunity();
                return;
            }
            stats.TakeDamage(1);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                animator.SetTrigger("ReturnMage");
                return;
            }
            if(status.Has(Status.Grounded))
            {
                stats.Heal(3);
                return;
            }
            stats.Heal(2);
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Tree;
        }



        #region TicksFX

        public override float GetFireTickDelay()
        {
            return 3f;
        }

        public override float GetGroundTickDelay()
        {
            return 5f;
        }

        public override void FireTick()
        {
            stats.TakeDamage(status.fireLevel);
            status.Increase(Status.Burned);
        }
        public override void GroundTick()
        {
            if(status.groundLevel == 3)
            {
                stats.Heal(1);
            }
        }

        public override void IceTick()
        {
            // Tree form is immune to ice tick effects ?
        }
        public override void PoisonTick()
        {
            // Tree form is immune to poison tick effects
        }
        #endregion
        public override void OnEnterTransformation()
        {
            Debug.Log("Transforming into Tree form");
        }

        public override void OnExitTransformation()
        {
            Debug.Log("Exiting Tree form. Returning to mage form");
        }
    }
}
