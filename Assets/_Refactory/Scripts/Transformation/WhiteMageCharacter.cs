
using UnityEngine;
namespace CharacterSystem
{
    public class WhiteMageCharacter : BaseCharacter
    {
        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];
            int manaCost = GetEffectiveSpellManaCost(spell);
            if (stats.HasMana(manaCost))
            {
                stats.LoseMana(manaCost);
                animator.SetTrigger(spell.nome);
                status.TriggerImmunity();
            }

            return false;
        }
        public override void ApplyDark(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyFire(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyIce(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGrass(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGround(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyHeal(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLava(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLight(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyPoison(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }

        public override void ApplyWet(PotionScriptable ps, int effectValue)
        {
            status.TriggerImmunity();
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.WhiteMage;
        }

        public override void PoisonTick()
        {
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

        public override void OnEnterTransformation()
        {
        }

        public override void OnExitTransformation()
        {
        }

    }
}

