using System.Collections;
using UnityEngine;
namespace CharacterSystem
{
    public class WitchCharacter : BaseCharacter
    {
        //[Header("Spell References")]

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];
            switch (i)
            {
                /*case 0:
                    return CastSummon(spell);
                case 1:
                    return CastDarkRay(spell);
                case 2:
                    return CastSecondChance(spell);*/
                default:
                    Debug.LogWarning($"{name} has no Witch spell behaviour for index {i}", this);
                    return false;
            }
        }


        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            int manaCost = Mathf.Min(stats.MP, spell.costo);
            int healthCost = spell.costo - manaCost;

            if (healthCost > 0 && stats.HP <= healthCost)
            {
                dialogManager.PopDialog(notEnoughManaDialog, 3f);
                return false;
            }

            if (manaCost > 0)
            {
                stats.LoseMana(manaCost);
            }

            if (healthCost > 0)
            {
                stats.TakeDamage(healthCost);
            }

            return true;
        }

        public override void ApplyDark(PotionScriptable ps)
        {
            stats.AddMana(ps.baseValue);
            Debug.Log("Una vecchia strega sa apprezzare oscurit� in bottiglia");
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            Debug.Log("Noi streghe non andiamo d'accordo con le fiamme");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            Debug.Log($"Un po' di ghiaccio � l'ideale per un succo sulla spiaggia");
            stats.Heal(ps.baseValue);
            return;
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            Debug.Log($"Eh eh, ingrediente segreto");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            Debug.Log($"Questo roviner� il mio intruglio D:");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            Debug.Log("Non mi fa' impazzire questa roba");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyLava(PotionScriptable ps)
        {

            Debug.Log($"Solo quel vecchio idiota pu� bere una cosa del genere");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyLight(PotionScriptable ps)
        {

            Debug.Log($"Dovrei tenerne un po' per quel pelato");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyPoison(PotionScriptable ps)
        {
            Debug.Log($"La mia favorit�");
            status.TriggerImmunity();
            return;
        }

        public override void ApplyWet(PotionScriptable ps)
        {

            Debug.Log($"mmm Annacquato");
            status.TriggerImmunity();
            return;
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Witch;
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
            status.Clear();
        }

        public override void OnExitTransformation()
        {
        }

    }
}

