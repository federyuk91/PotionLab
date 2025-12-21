using UnityEngine;
namespace CharacterSystem
{
    public class BalrogCharacter : BaseCharacter
    {

        public override void ApplyDark(PotionScriptable ps)
        {
            stats.AddMana(2);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.Increase(Status.Burned);
        }

        public override void ApplyFreezed(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                return;
            }

            stats.TakeDamage(4);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                return;
            }

            stats.TakeDamage(2);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            animator.SetTrigger("ReturnMage");
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.TriggerExplosion();
                stats.TakeDamage(2);
                return;
            }
            stats.TakeDamage(1);

        }

        public override void ApplyWet(PotionScriptable ps)
        {
            status.TriggerImmunity();
            status.Remove(Status.Burned);
        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Balrog;
        }

        #region TicksFX
        public override float GetFireTickDelay()
        {
            return 7f - status.fireLevel;
        }

        public override void FireTick()
        {
            stats.Heal(1);
        }
        public override void GroundTick()
        {
            // Balrog is immune to ground tick effects.
        }

        public override void IceTick()
        {
            // Balrog is immune to ice tick effects.
        }

        public override void PoisonTick()
        {
            // Balrog is immune to poison tick effects.
        }
        #endregion


        public override void OnEnterTransformation()
        {
            status.Remove(Status.Burned);
        }

        public override void OnExitTransformation()
        {
            stats.TakeDamage(stats.HP-1);
            stats.SetMP(10);
        }
    }
}
