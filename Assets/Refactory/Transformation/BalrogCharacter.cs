using UnityEngine;
namespace CharacterSystem
{
    public class BalrogCharacter : BaseCharacter
    {
        public override void ApplyDamage(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyDark(PotionScriptable ps)
        {
            stats.AddMana(2);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.Add(Status.Burned);

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
            TransformationManager.Instance.SwitchTo(CharacterType.Mage);
        }

        public override void ApplyNone(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
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

        public override void FireTickFX()
        {
            stats.Heal(1);
        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Balrog;
        }

        public override float GetFireTickDelay()
        {
            return 7f - status.fireLevel;
        }

        public override float GetGroundTickDelay()
        {
            throw new System.NotImplementedException();
        }

        public override float GetIceTickDelay()
        {
            throw new System.NotImplementedException();
        }

        public override float GetPoisonTickDelay()
        {
            throw new System.NotImplementedException();
        }

        public override void GroundTick()
        {
            throw new System.NotImplementedException();
        }

        public override void IceTick()
        {
            throw new System.NotImplementedException();
        }

        public override void OnEnterTransformation()
        {
            status.Remove(Status.Burned);
        }

        public override void OnExitTransformation()
        {
            stats.SetHP(1);
            stats.SetMP(10);
        }

        public override void PoisonTick()
        {
            throw new System.NotImplementedException();
        }
    }
}
