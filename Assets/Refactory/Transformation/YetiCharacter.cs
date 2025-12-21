using UnityEngine;
namespace CharacterSystem
{
    public class YetiCharacter : BaseCharacter
    {

        public override void ApplyDark(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
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
            return CharacterType.Yeti;
        }

        #region TicksFX
        public override void FireTick()
        {
        }

        public override void GroundTick()
        {
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
