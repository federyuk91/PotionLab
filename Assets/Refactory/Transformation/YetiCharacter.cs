using UnityEngine;
namespace CharacterSystem
{
    public class YetiCharacter : BaseCharacter
    {

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

        public override void ApplyFreezed(PotionScriptable ps)
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
