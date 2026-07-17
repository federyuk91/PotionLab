using UnityEngine;
namespace CharacterSystem
{
    public class PupperFishCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private GameObject pufferFishProjectionObject;


        protected override void CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];

            switch (i)
            {
                case 0:
                    CastWaterZone(spell);
                    break;
                case 1:
                    CastEatAlgae(spell, powered);
                    break;
                case 2:
                    CastPufferFish(spell, powered);
                    break;
                default:
                    Debug.LogError($"{name} has no PupperFish spell behaviour for index {i}", this);
                    break;
            }
        }

        private void CastWaterZone(Spell spell)
        {
            if (!TrySpendMana(spell, "Ohoh, bloblob!"))
            {
                return;
            }

            transformationManager.lightController.ToggleLightField(LightFieldType.Water);

            if (transformationManager.lightController.IsLightFieldActive(LightFieldType.Water))
            {
                GameMan.Instance.PopDialog("Water BLO BLOB!", 2f);
            }
            else
            {
                GameMan.Instance.PopDialog("Goodbye water blob blob!", 2f);
            }
        }

        private void CastEatAlgae(Spell spell, bool powered)
        {
            if (!status.Has(Status.Algae))
            {
                GameMan.Instance.PopDialog("I need some alghe to eat", 3f);
                return;
            }

            if (!TrySpendMana(spell, "Ohoh, bloblob!"))
            {
                return;
            }

            stats.AddMana(status.algaeLevel * 2);
            status.Remove(Status.Algae);

            if (powered)
            {
                stats.Heal(2);
            }
        }

        private void CastPufferFish(Spell spell, bool powered)
        {
            if (!TrySpendMana(spell, "Ohoh, bloblob!"))
            {
                return;
            }

            if (pufferFishProjectionObject != null)
            {
                pufferFishProjectionObject.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"{name} has no puffer fish projection object assigned.", this);
            }

            if (powered)
            {
                stats.AddMana(3);
                AchievementManager.instance.Achive("BLOB!");
            }
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
        public override void ApplyDark(PotionScriptable ps)
        {
            OnExitTransformation();
            transformationManager.SwitchTo(CharacterType.Mage);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            if(status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                status.Increase(Status.Algae);
                return;
            }
            if (status.Has(Status.Algae)) {
                status.TriggerImmunity();
                return;
            }
            status.Add(Status.Grass);
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            stats.TakeDamage(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            stats.AddMana(ps.baseValue);
        }

        public override void ApplyPoison(PotionScriptable ps)
        {
            stats.AddMana(1);
        }

        public override void ApplyWet(PotionScriptable ps)
        {
            if(status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                status.Increase(Status.Algae);
                return;
            }
            if (status.Has(Status.Algae))
            {
                status.Increase(Status.Algae);
                return;
            }

            status.Add(Status.Wet);
        }


        public override CharacterType GetCharacterForm()
        {
            return CharacterType.PupperFish;
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
        public override void PoisonTick()
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
