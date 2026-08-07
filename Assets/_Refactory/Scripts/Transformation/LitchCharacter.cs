using System.Collections;
using UnityEngine;
namespace CharacterSystem
{
    public class LitchCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private LitchSummonGroup summonGroup;
        [SerializeField] private DarkRaySpellEffect darkRaySpellEffect;

        [Header("Second Chance")]
        [SerializeField] private float secondChanceReturnDelay = 1.35f;

        private Coroutine secondChanceCoroutine;

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];
            switch (i)
            {
                case 0:
                    return CastSummon(spell);
                case 1:
                    return CastDarkRay(spell);
                case 2:
                    return CastSecondChance(spell);
                default:
                    Debug.LogError($"{name} has no Litch spell behaviour for index {i}", this);
                    return false;
            }
        }

        private bool CastSummon(Spell spell)
        {
            if (summonGroup == null)
            {
                summonGroup = GetComponentInChildren<LitchSummonGroup>(true);
            }

            if (summonGroup == null)
            {
                LitchSummonPotionDestroyer summon = GetComponentInChildren<LitchSummonPotionDestroyer>(true);
                if (summon != null)
                {
                    summonGroup = summon.GetComponentInParent<LitchSummonGroup>();
                }
            }

            if (summonGroup == null)
            {
                Debug.LogWarning($"{name} cannot cast Summon: summon group is missing.", this);
                return false;
            }

            if (summonGroup.HasActiveSkeletons())
            {
                dialogManager.PopDialog("They are still working", 2f);
                return false;
            }

            if (!TrySpendMana(spell, "I need more life or magic for this spell"))
            {
                return false;
            }

            summonGroup.ActivateSkeletons();
            return true;
        }

        private bool CastDarkRay(Spell spell)
        {
            if (darkRaySpellEffect == null)
            {
                darkRaySpellEffect = GetComponentInChildren<DarkRaySpellEffect>(true);
            }

            if (darkRaySpellEffect == null)
            {
                Debug.LogWarning($"{name} cannot cast Dark-Ray: DarkRaySpellEffect reference is missing.", this);
                return false;
            }

            if (!TrySpendMana(spell, "I need more life or magic for this spell"))
            {
                return false;
            }

            darkRaySpellEffect.Cast();
            return true;
        }

        private bool CastSecondChance(Spell spell)
        {
            if (secondChanceCoroutine != null)
            {
                return false;
            }

            if (!TrySpendMana(spell, "I need more life or magic for this spell"))
            {
                return false;
            }

            status.Clear();
            secondChanceCoroutine = StartCoroutine(ReturnMageAfterSecondChance());
            return true;
        }

        private IEnumerator ReturnMageAfterSecondChance()
        {
            yield return new WaitForSeconds(secondChanceReturnDelay);

            secondChanceCoroutine = null;
            ReturnMage();
        }

        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            int manaCost = Mathf.Min(stats.MP, spell.cost);
            int healthCost = spell.cost - manaCost;

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
                    status.Decrease(Status.Grounded);
                    status.Remove(Status.Freezed);
                    return;
                }
                stats.TakeDamage(ps.baseValue);
                status.Remove(Status.Freezed);
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
                    status.Decrease(Status.Grounded);
                    status.Remove(Status.Burned);
                    return;
                }
                stats.TakeDamage(ps.baseValue);
                status.Remove(Status.Burned);
                return;
            }

            status.Add(Status.Freezed);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            int damage = ps.baseValue + status.groundLevel;
            stats.TakeDamage(damage);
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
                status.Decrease(Status.Grounded);
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
            status.Clear();
        }

        public override void OnExitTransformation()
        {
            secondChanceCoroutine = null;
        }

    }
}
