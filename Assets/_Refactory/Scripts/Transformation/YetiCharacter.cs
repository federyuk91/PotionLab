using UnityEngine;
namespace CharacterSystem
{
    public class YetiCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private GameObject punchObject;

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];

            switch (i)
            {
                case 0:
                    return CastIceZone(spell);
                case 1:
                    return CastConvert(spell, powered);
                case 2:
                    return CastPunch(spell, powered);
                default:
                    Debug.LogError($"{name} has no Yeti spell behaviour for index {i}", this);
                    return false;
            }
        }

        private bool CastIceZone(Spell spell)
        {
            if (!TrySpendMana(spell, "eh?"))
            {
                return false;
            }

            transformationManager.lightController.ToggleLightField(LightFieldType.Ice);

            if (transformationManager.lightController.IsLightFieldActive(LightFieldType.Ice))
            {
                dialogManager.PopDialog("Dance Move!", 2f);
            }
            else
            {
                dialogManager.PopDialog("nooooooo", 2f);
            }

            return true;
        }

        private bool CastConvert(Spell spell, bool powered)
        {
            if (stats.HP >= stats.MaxHP)
            {
                AchievementManager.instance.Achive("Smart but fart!");
                dialogManager.PopDialog("FULL", 1f);
                return false;
            }

            if (!TrySpendMana(spell, "eh?"))
            {
                return false;
            }

            stats.Heal(powered ? 4 : 3);
            return true;
        }

        private bool CastPunch(Spell spell, bool powered)
        {
            if (!TrySpendMana(spell, "eh?"))
            {
                return false;
            }

            stats.TakeDamage(powered ? 1 : 2);

            if (punchObject == null)
            {
                Debug.LogWarning($"{name} has no punch object assigned.", this);
                return true;
            }

            punchObject.SetActive(false);
            punchObject.SetActive(true);
            return true;
        }

        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            if (!stats.HasMana(spell.cost))
            {
                dialogManager.PopDialog(notEnoughManaDialog, 1f);
                return false;
            }

            stats.LoseMana(spell.cost);
            return true;
        }
        public override void OnEnable()
        {
            base.OnEnable();
            Debug.Log("Yeti on enable");
            stats.OnHealtUp += CheckMutation;
            stats.OnHealtDown += CheckMutation;
            stats.OnManaDown += CheckMutation;
            stats.OnManaUp += CheckMutation;
        }
        public override void OnDisable()
        {
            base.OnDisable();
            Debug.Log("Yeti on disable");
            stats.OnHealtUp -= CheckMutation;
            stats.OnHealtDown -= CheckMutation;
            stats.OnManaDown -= CheckMutation;
            stats.OnManaUp -= CheckMutation;
        }

        public void CheckMutation()
        {
            if (stats.HP == stats.MP)
                transformationManager.SwitchTo(CharacterType.Mage);
        }
        public override void ApplyDark(PotionScriptable ps)
        {
            if (stats.MP > 0)
            {
                stats.LoseMana(1);
                return;
            }
            stats.TakeDamage(2);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            status.Increase(Status.Grounded);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            if(status.Has(Status.Grounded))
            {
                status.Remove(Status.Grounded);
                return;
            }
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
            stats.AddMana(ps.baseValue);
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            status.Increase(Status.Poisoned);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if (status.Has(Status.Poisoned))
            {
                status.Remove(Status.Poisoned);
                return;
            }
            status.TriggerImmunity();
        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Yeti;
        }

        #region TicksFX
        public override void FireTick()
        {
        }

        public override float GetGroundTickDelay()
        {
            return 5f;
        }
        public override void GroundTick()
        {
            if (status.groundLevel == 3)
            {
                stats.TakeDamage(2);
            }
        }


        public override float GetPoisonTickDelay()
        {
            if(status.Has(Status.Grounded))
            {
                return 5f;
            }
            return 4f;
        }
        public override void PoisonTick()
        {
            animator.SetTrigger("isDamaged");
            if (status.Has(Status.Grounded))
                stats.TakeDamage(1); //Se è interrato prende 1 danno da veleno, utile per annullare la trasformazione ma non ridusce il poisonLevel
            else
            {
                stats.TakeDamage(1);
                status.Decrease(Status.Poisoned);
            }
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
