using System.Collections;
using InspectorValidation;
using UnityEngine;
namespace CharacterSystem
{
    public class TreeCharacter : BaseCharacter
    {
        private static readonly int BurnTreeShieldTrigger = Animator.StringToHash("Burn");
        private static readonly int FreezeTreeShieldTrigger = Animator.StringToHash("Freeze");

        [Header("Spell References")]
        [SerializeField, RequiredInspectorReference] private GameObject treeShieldObject;
        [SerializeField, RequiredInspectorReference] private Animator treeShieldAnimator;
        [SerializeField, Min(0f)] private float treeShieldBreakDuration = 0.75f;
        [SerializeField, RequiredInspectorReference] private GameObject overgrowthObject;

        private bool hasTreeShield;
        private bool hasOvergrowth;
        private Flower overgrowthFlower;
        private Coroutine treeShieldBreakCoroutine;

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
                dialogManager.PopDialog("Grass everywhere!", 2f);
            }
            else
            {
                dialogManager.PopDialog("Goodbye grassss!", 2f);
            }

            return true;
        }

        private bool CastTreeBark(Spell spell, bool powered)
        {
            if (hasTreeShield)
            {
                dialogManager.PopDialog("I already have shield", 3f);
                return false;
            }

            if (status.Has(Status.Burned))
            {
                dialogManager.PopDialog("Barks can't form any shield with flames", 3f);
                if (AchievementManager.instance != null)
                {
                    AchievementManager.instance.Achive("Exotic Interaction");
                }

                return false;
            }

            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            hasTreeShield = true;
            ActivateTreeShield();

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
                dialogManager.PopDialog("I already spawn my child...", 1.5f);
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

            if (!overgrowthObject.TryGetComponent(out overgrowthFlower))
            {
                Debug.LogWarning($"{name} overgrowth object has no Flower assigned.", this);
                overgrowthObject.SetActive(true);
                return true;
            }

            overgrowthFlower.Destroyed -= OnOvergrowthDestroyed;
            overgrowthFlower.Destroyed += OnOvergrowthDestroyed;
            overgrowthFlower.ResetFlower();
            overgrowthObject.SetActive(true);

            if (powered)
            {
                overgrowthFlower.Grow();
                if (AchievementManager.instance != null)
                {
                    AchievementManager.instance.Achive("Sylvanus Blessing");
                }
            }

            return true;
        }

        private void OnOvergrowthDestroyed(Flower flower)
        {
            if (flower != overgrowthFlower)
            {
                return;
            }

            hasOvergrowth = false;
            overgrowthFlower.Destroyed -= OnOvergrowthDestroyed;
            overgrowthFlower = null;
        }

        private bool TrySpendMana(Spell spell, string notEnoughManaDialog)
        {
            if (!stats.HasMana(spell.cost))
            {
                dialogManager.PopDialog(notEnoughManaDialog, 3f);
                return false;
            }

            stats.LoseMana(spell.cost);
            return true;
        }

        private void BreakTreeShield(string dialog, int animationTrigger)
        {
            hasTreeShield = false;

            if (treeShieldAnimator == null)
            {
                Debug.LogError($"{name} cannot play the Bark break animation: assign Tree Shield Animator in the Inspector.", this);
                SetTreeShieldActive(false);
            }
            else
            {
                treeShieldAnimator.ResetTrigger(BurnTreeShieldTrigger);
                treeShieldAnimator.ResetTrigger(FreezeTreeShieldTrigger);
                treeShieldAnimator.SetTrigger(animationTrigger);

                StopTreeShieldBreakCoroutine();
                treeShieldBreakCoroutine = StartCoroutine(DisableTreeShieldAfterBreak());
            }

            dialogManager.PopDialog(dialog, 3f);
        }

        private void ActivateTreeShield()
        {
            StopTreeShieldBreakCoroutine();
            SetTreeShieldActive(true);

            if (treeShieldAnimator == null)
            {
                Debug.LogError($"{name} cannot reset the Bark animation: assign Tree Shield Animator in the Inspector.", this);
                return;
            }

            treeShieldAnimator.ResetTrigger(BurnTreeShieldTrigger);
            treeShieldAnimator.ResetTrigger(FreezeTreeShieldTrigger);
            treeShieldAnimator.Rebind();
            treeShieldAnimator.Update(0f);
        }

        private IEnumerator DisableTreeShieldAfterBreak()
        {
            yield return new WaitForSeconds(treeShieldBreakDuration);
            SetTreeShieldActive(false);
            treeShieldBreakCoroutine = null;
        }

        private void StopTreeShieldBreakCoroutine()
        {
            if (treeShieldBreakCoroutine == null)
            {
                return;
            }

            StopCoroutine(treeShieldBreakCoroutine);
            treeShieldBreakCoroutine = null;
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
                BreakTreeShield("Tree's bark burn away", BurnTreeShieldTrigger);
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
                BreakTreeShield("Tree's bark freeze away", FreezeTreeShieldTrigger);
                return;
            }

            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                stats.Heal(2);
                TriggerReturnMageAnimation();
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
                BreakTreeShield("Tree's bark fade away", BurnTreeShieldTrigger);
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
                TriggerReturnMageAnimation();
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

        private void OnDestroy()
        {
            StopTreeShieldBreakCoroutine();

            if (overgrowthFlower != null)
            {
                overgrowthFlower.Destroyed -= OnOvergrowthDestroyed;
            }
        }

    }
}
