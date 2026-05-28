using UnityEngine;
namespace CharacterSystem
{
    public class WhiteMageCharacter : BaseCharacter
    {
        public override void Cast(Spell spell, bool powered)
        {
            if (stats.HasMana(spell.cost))
            {
                stats.LoseMana(spell.cost);
                animator.SetTrigger(spell.spellName);
                switch (spell.spellName)
                {
                    case "Light":
                        break;
                    case "Heal":
                        break;
                    case "Cleanse":
                        break;

                }
            }
        }
        public override void ApplyDark(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            throw new System.NotImplementedException();
        }

        public override void ApplyIce(PotionScriptable ps)
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
            return CharacterType.WhiteMage;
        }

        public override void PoisonTick()
        {
            throw new System.NotImplementedException();
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

        public override void OnEnterTransformation()
        {
            throw new System.NotImplementedException();
        }

        public override void OnExitTransformation()
        {
            throw new System.NotImplementedException();
        }

    }
}
