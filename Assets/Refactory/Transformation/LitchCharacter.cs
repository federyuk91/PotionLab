using UnityEngine;
namespace CharacterSystem
{
    public class LitchCharacter : BaseCharacter
    {

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];
            Debug.LogWarning("Not implemented yet: " + spell.spellName);
            switch (i)
            {
                case 0:

                    return true;
                case 1:
                    return true;
                case 2:
                    return true;
                default:
                    Debug.LogError($"{name} has no Balrog spell behaviour for index {i}", this);
                    return false;
            }
        }

        public override void ApplyDark(PotionScriptable ps)
        {
            if(stats.HP < stats.MaxHP)
                stats.Heal(ps.baseValue);
            else
                stats.AddMana(ps.baseValue);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            if (status.Has(Status.Freezed))
            {
                if (status.groundLevel > 0)
                {
                    status.TriggerImmunity();
                    return;
                }
                stats.TakeDamage(ps.baseValue);
                return;
            }

            if (status.Has(Status.Burned))
            {
                status.TriggerImmunity();
                return;
            }

            status.Increase(Status.Burned);
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            if (status.Has(Status.Freezed))
            {
                status.TriggerImmunity();
                return;
            }

            if (status.Has(Status.Burned))
            {
                if(status.groundLevel > 0)
                {
                    status.TriggerImmunity();
                    return;
                }
                stats.TakeDamage(ps.baseValue);
                return;
            }

            status.Add(Status.Freezed);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            status.Increase(Status.Grounded);

            if (status.groundLevel >= 3)
            {
                status.Remove(Status.Burned);
                status.Remove(Status.Freezed);
                transformationManager.SwitchTo(CharacterType.Mage);
            }
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            if (status.groundLevel > 0)
            {
                status.Remove(Status.Grounded);
                return;
            }

            if (status.Has(Status.Burned))
            {
                status.TriggerImmunity();
                return;
            }

            stats.TakeDamage(ps.baseValue);
            if(status.Has(Status.Freezed))
            {
                stats.TakeDamage(1);
            }
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyPoison(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if(status.groundLevel>0)
            {
                status.Decrease(Status.Grounded);
                return;
            }

            if (status.Has(Status.Burned))
            {
                stats.TakeDamage(ps.baseValue);
                return;
            }

            status.TriggerImmunity();
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Litch;
        }

        public override void PoisonTick()
        {
            // Litch is immune to poison effects.
        }
        public override void FireTick()
        {
            // Fire has no periodic effect on Litch in the current rule table.
        }
        public override void GroundTick()
        {
            // Ground has no periodic effect on Litch in the current rule table.
        }

        public override void IceTick()
        {
            // Ice has no periodic effect on Litch in the current rule table.
        }

        public override void OnEnterTransformation()
        {
            // Litch has no enter side effects yet.
        }

        public override void OnExitTransformation()
        {
            // Litch has no exit side effects yet.
        }

    }
}
