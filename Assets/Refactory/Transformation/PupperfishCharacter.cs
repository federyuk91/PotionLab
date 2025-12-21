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
            throw new System.NotImplementedException();
        }

        public override void ApplyFreezed(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyPoison(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.PupperFish;
        }

        public override void FireTick()
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
        public override void PoisonTick()
        {
            throw new System.NotImplementedException();
        }

        public override void OnEnterTransformation()
        {

        }

        public override void OnExitTransformation()
        {

        }

    }
}
