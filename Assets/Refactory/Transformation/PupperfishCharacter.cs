using UnityEngine;
namespace CharacterSystem
{
    public class PupperFishCharacter : BaseCharacter
    {


        public override void ApplyDark(PotionScriptable ps)
        {
            OnExitTransformation();
            transformationManager.SwitchTo(CharacterType.Mage);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyFreezed(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            if(status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                status.Increase(Status.Algae);
                return;
            }
            status.Add(Status.Grass);
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.AddMana(ps.baseValue);
        }

        public override void ApplyPoison(PotionScriptable ps)
        {
            stats.AddMana(1);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if(status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                status.Increase(Status.Algae);
                return;
            }

            status.Add(Status.Wet);
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.PupperFish;
        }

        public override void FireTick()
        {
        }

        public override void GroundTick()
        {
        }

        public override void IceTick()
        {
        }
        public override void PoisonTick()
        {
        }

        public override void OnEnterTransformation()
        {

        }

        public override void OnExitTransformation()
        {

        }

    }
}
