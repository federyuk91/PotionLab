using UnityEngine;
namespace CharacterSystem
{
    public class TreeCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private GameObject treeShieldObject;
        [SerializeField] private GameObject overgrowthObject;

        private bool hasTreeShield;
        private bool hasOvergrowth;

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];

            switch (i)
            {
                case 0:
                    return CastGrassZone(spell);
                case 1:
                    return CastTreeBark(spell, powered);
                case 2:
                    return CastOvergrowth(spell, powered);
                default:
                    Debug.LogError($"{name} has no Tree spell behaviour for index {i}", this);
                    return false;
            }
        }

        private bool CastGrassZone(Spell spell)
        {
            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            transformationManager.lightController.ToggleLightField(LightFieldType.Grass);

            if (transformationManager.lightController.IsLightFieldActive(LightFieldType.Grass))
            {
                GameMan.Instance.PopDialog("Grass everywhere!", 2f);
            }
            else
            {
                GameMan.Instance.PopDialog("Goodbye grassss!", 2f);
            }

            return true;
        }

        private bool CastTreeBark(Spell spell, bool powered)
        {
            if (hasTreeShield)
            {
                GameMan.Instance.PopDialog("I already have shield", 3f);
                return false;
            }

            if (status.Has(Status.Burned))
            {
                GameMan.Instance.PopDialog("Barks can't form any shield with flames", 3f);
                AchievementManager.instance.Achive("Exotic Interaction");
                return false;
            }

            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            hasTreeShield = true;
            SetTreeShieldActive(true);

            if (powered)
            {
                stats.Heal(2);
            }

            return true;
        }

        private bool CastOvergrowth(Spell spell, bool powered)
        {
            if (hasOvergrowth)
            {
                GameMan.Instance.PopDialog("I already spawn my child...", 1.5f);
                return false;
            }

            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            hasOvergrowth = true;

            if (overgrowthObject == null)
            {
                Debug.LogWarning($"{name} has no overgrowth object assigned.", this);
                return true;
            }

            overgrowthObject.SetActive(true);

            if (powered && overgrowthObject.TryGetComponent(out FlowerScript flower))
            {
                flower.Grow();
                AchievementManager.instance.Achive("Sylvanus Blessing");
            }

            return true;
        }

        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            if (!stats.HasMana(spell.cost))
            {
                GameMan.Instance.PopDialog(notEnoughManaDialog, 3f);
                return false;
            }

            stats.LoseMana(spell.cost);
            return true;
        }

        private void BreakTreeShield(string dialog)
        {
            hasTreeShield = false;
            SetTreeShieldActive(false);
            GameMan.Instance.PopDialog(dialog, 3f);
        }

        private void SetTreeShieldActive(bool active)
        {
            if (treeShieldObject == null)
            {
                Debug.LogWarning($"{name} has no tree shield object assigned.", this);
                return;
            }

            treeShieldObject.SetActive(active);
        }

        public override void ApplyDark(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            if (hasTreeShield)
            {
                BreakTreeShield("Tree's bark burn away");
                return;
            }

            if(status.Has(Status.Grounded))
            {             
                status.TriggerImmunity();
                return;
            }
            status.Increase(Status.Burned);
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            if (hasTreeShield)
            {
                BreakTreeShield("Tree's bark freeze away");
                return;
            }

            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                stats.Heal(2);
                animator.SetTrigger("ReturnMage");
                return;
            }
            stats.TakeDamage(2);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            stats.AddMana(2);
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                status.Increase(Status.Grounded);
                return;
            }
            status.Increase(Status.Grounded);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            if (hasTreeShield)
            {
                BreakTreeShield("Tree's bark fade away");
                return;
            }

            if (status.Has(Status.Grounded))
            {
                status.Remove(Status.Grounded);
                return;
            }
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.AddMana(ps.baseValue);
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            if (status.Has(Status.Grounded))
            {
                status.TriggerImmunity();
                return;
            }
            stats.TakeDamage(1);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                animator.SetTrigger("ReturnMage");
                return;
            }
            if(status.Has(Status.Grounded))
            {
                stats.Heal(3);
                return;
            }
            stats.Heal(2);
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Tree;
        }



        #region TicksFX

        public override float GetFireTickDelay()
        {
            return 3f;
        }

        public override float GetGroundTickDelay()
        {
            return 5f;
        }

        public override void FireTick()
        {
            stats.TakeDamage(status.fireLevel);
            status.Increase(Status.Burned);
        }
        public override void GroundTick()
        {
            if(status.groundLevel == 3)
            {
                stats.Heal(1);
            }
        }

        public override void IceTick()
        {
            // Tree form is immune to ice tick effects ?
        }
        public override void PoisonTick()
        {
            // Tree form is immune to poison tick effects
        }
        #endregion
        public override void OnEnterTransformation()
        {
            Debug.Log("Transforming into Tree form");
        }

        public override void OnExitTransformation()
        {
            Debug.Log("Exiting Tree form. Returning to mage form");
        }

    }
}
