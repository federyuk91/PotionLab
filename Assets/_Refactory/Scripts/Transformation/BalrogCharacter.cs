using UnityEngine;
namespace CharacterSystem
{
    public class BalrogCharacter : BaseCharacter
    {
        [Header("Spell References")]
        [SerializeField] private GameObject calderoneObject;
        [SerializeField] private GameObject poweredCalderoneObject;

        protected override bool CastSpell(int i, bool powered)
        {
            Spell spell = spellList[i];

            switch (i)
            {
                case 0:
                    return CastFireZone(spell);
                case 1:
                    return CastBalance(spell, powered);
                case 2:
                    return CastCalderone(spell, powered);
                default:
                    Debug.LogError($"{name} has no Balrog spell behaviour for index {i}", this);
                    return false;
            }
        }

        private bool CastFireZone(Spell spell)
        {
            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            transformationManager.lightController.ToggleLightField(LightFieldType.Fire);

            if (transformationManager.lightController.IsLightFieldActive(LightFieldType.Fire))
            {
                dialogManager.PopDialog("Burn baby burn! Disco INFERNO!", 2f);
            }
            else
            {
                dialogManager.PopDialog("Goodbye heat ):", 2f);
            }

            return true;
        }

        private bool CastBalance(Spell spell, bool powered)
        {
            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            stats.TakeDamage(3);
            stats.AddMana(powered ? 2 : 1);
            return true;
        }

        private bool CastCalderone(Spell spell, bool powered)
        {
            if (!TrySpendMana(spell, "Ohoh, not enough magic!"))
            {
                return false;
            }

            GameObject selectedCalderone = powered && poweredCalderoneObject != null ? poweredCalderoneObject : calderoneObject;

            if (selectedCalderone == null)
            {
                Debug.LogWarning($"{name} has no calderone object assigned.", this);
                return true;
            }

            SetCalderoneActive(calderoneObject, selectedCalderone == calderoneObject);
            SetCalderoneActive(poweredCalderoneObject, selectedCalderone == poweredCalderoneObject);
            RestartCalderoneAnimations(selectedCalderone);
            RestartCalderoneSpriteAnimations(selectedCalderone);

            if (powered)
            {
                AchievementManager.instance.Achive("Cooking Mama!");
            }

            return true;
        }

        private void SetCalderoneActive(GameObject calderone, bool active)
        {
            if (calderone != null)
            {
                calderone.SetActive(active);
            }
        }

        private void RestartCalderoneAnimations(GameObject calderone)
        {
            Animation[] calderoneAnimations = calderone.GetComponentsInChildren<Animation>(true);

            if (calderoneAnimations.Length == 0)
            {
                Debug.LogWarning($"{calderone.name} has no legacy Animation component assigned.", calderone);
                return;
            }

            foreach (Animation calderoneAnimation in calderoneAnimations)
            {
                if (calderoneAnimation.GetComponent<global::PotionDestroyTrigger>() != null)
                {
                    continue;
                }

                calderoneAnimation.enabled = true;
                calderoneAnimation.Stop();
                calderoneAnimation.Rewind();
                calderoneAnimation.Play();
            }
        }

        private void RestartCalderoneSpriteAnimations(GameObject calderone)
        {
            global::PotionDestroyTrigger[] potionDestroyTriggers = calderone.GetComponentsInChildren<global::PotionDestroyTrigger>(true);
            foreach (global::PotionDestroyTrigger potionDestroyTrigger in potionDestroyTriggers)
            {
                potionDestroyTrigger.RestartSpriteAnimation();
            }
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
        public override void ApplyDark(PotionScriptable ps)
        {
            stats.AddMana(2);
        }

        public override void ApplyFire(PotionScriptable ps)
        {
            status.Increase(Status.Burned);
        }

        public override void ApplyIce(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                return;
            }

            stats.TakeDamage(4);
        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyGround(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                return;
            }

            stats.TakeDamage(2);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            status.TriggerImmunity();
        }

        public override void ApplyLava(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        public override void ApplyLight(PotionScriptable ps)
        {
            //Alla fine dell'animazione di trasformazione, il balrog ritorna alla forma del mago con un animation event.
            animator.SetTrigger("ReturnMage");
        }


        public override void ApplyPoison(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.TriggerExplosion();
                stats.TakeDamage(2);
                return;
            }
            stats.TakeDamage(1);

        }

        public override void ApplyWet(PotionScriptable ps)
        {
            status.Remove(Status.Burned);
        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Balrog;
        }

        #region TicksFX
        public override float GetFireTickDelay()
        {
            return 7f - status.fireLevel;
        }

        public override void FireTick()
        {
            stats.Heal(1);
        }
        public override void GroundTick()
        {
            // Balrog is immune to ground tick effects.
        }

        public override void IceTick()
        {
            // Balrog is immune to ice tick effects.
        }

        public override void PoisonTick()
        {
            // Balrog is immune to poison tick effects.
        }
        #endregion


        public override void OnEnterTransformation()
        {
            status.Remove(Status.Burned);
        }

        public override void OnExitTransformation()
        {
            stats.TakeDamage(stats.HP-1);
            stats.SetMP(10);
        }
    }
}
