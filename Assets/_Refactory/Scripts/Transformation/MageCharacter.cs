
using UnityEngine;
namespace CharacterSystem
{
    public class MageCharacter : BaseCharacter
    {

        public int lightLevel = 0, darkLevel = 0;
        protected override bool CastSpell(int index, bool powered)
        {
            Spell spell = spellList[index];
            switch (index)
            {
                case 0:
                    return CastLight(spell, powered);
                case 1:
                    return CastHeal(spell, powered);
                case 2:
                    return CastCleanse(spell, powered);
                default:
                    Debug.LogError($"{name} has no Mage spell behaviour for index {index}", this);
                    return false;
            }
        }

        private bool CastLight(Spell spell, bool powered)
        {
            if (powered)
            {
                dialogManager.PopDialog("maximum lumen", 2f);
                return false;
            }

            if (!stats.HasMana(spell.cost))
            {
                dialogManager.PopDialog("I need more magic for this spell", 3f);
                return false;
            }

            stats.LoseMana(spell.cost);
            transformationManager.lightController.IncreaseLightLevel();
            return true;
        }

        private bool CastHeal(Spell spell, bool powered)
        {
            if (stats.HP >= stats.MaxHP)
            {
                dialogManager.PopDialog("No need for this now!", 2f);
                return false;
            }

            if (!stats.HasMana(spell.cost))
            {
                dialogManager.PopDialog("I need more magic for this spell", 3f);
                return false;
            }

            stats.LoseMana(spell.cost);

            if (powered)
            {
                stats.Heal(4);
                dialogManager.PopDialog("Amazing heal!", 2f);
            }
            else
            {
                stats.Heal(3);
            }

            return true;
        }

        private bool CastCleanse(Spell spell, bool powered)
        {
            if (!HasCleanseableStatus())
            {
                dialogManager.PopDialog("No base status to remove!", 2f);
                return false;
            }

            if (!stats.HasMana(spell.cost))
            {
                dialogManager.PopDialog("I need more magic for this spell", 3f);
                return false;
            }

            stats.LoseMana(spell.cost);

            status.Remove(Status.Burned);
            status.Remove(Status.Grass);
            status.Remove(Status.Wet);
            status.Remove(Status.Freezed);
            status.Remove(Status.Poisoned);
            status.Remove(Status.Grounded);

            if (powered)
            {
                dialogManager.PopDialog("A bit of health too!", 2f);
                stats.Heal(1);
            }

            return true;
        }

        private bool HasCleanseableStatus()
        {
            return status.Has(Status.Grass)
                || status.Has(Status.Wet)
                || status.Has(Status.Burned)
                || status.Has(Status.Freezed)
                || status.Has(Status.Poisoned)
                || status.Has(Status.Grounded);
        }


        public override void ApplyFire(PotionScriptable ps)
        {
            if (status.Has(Status.Grounded))
            {
                status.TriggerImmunity();
                return;
            }
            if (status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                status.Increase(Status.Burned);
                return;
            }
            if (status.Has(Status.Algae))
            {
                stats.AddMana(status.algaeLevel * 2);
                status.Remove(Status.Algae);
                return;
            }
            if (status.Has(Status.Poisoned))
            {
                stats.TakeDamage(2); //Explosion fx
                status.TriggerExplosion();
                animator.SetTrigger("poisonExplosion");//Rimuovere dall'animator e gestire con VFX altri personaggi potrebbero doverlo usare
                return;
            }
            //Se sono congelato, divento bagnato
            if (status.Has(Status.Freezed))
            {
                status.Remove(Status.Freezed);
                status.Add(Status.Wet);
                animator.SetTrigger("vaporing");
                return;
            }
            //se sono bagnato, rimuovo lo stato bagnato
            if (status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                animator.SetTrigger("smoking");
                return;
            }

            //Se sto bruciando, prendo danno extra
            if (status.Has(Status.Burned))
            {
                status.Increase(Status.Burned); //Aumento il livello di bruciatura
                stats.TakeDamage(1);
                return;
            }

            //Altrimenti inizio a bruciare
            status.Increase(Status.Burned);
        }
        /***** TRASFORMAZIONE ***** Il ghiaccio transforma in Yeti se si è grounded */
        public override void ApplyIce(PotionScriptable ps)
        {
            //Se sto bruciando, divento bagnato
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned); //Il ghiaccio spegne il fuoco direttamente, senza abbassare il livello
                status.Add(Status.Wet); //Il mago resta comunque bagnato
                animator.SetTrigger("vaporing");
                return;
            }

            //Se sono bagnato o congelato, prendo danno e divento congelato
            if (status.Has(Status.Wet) || status.Has(Status.Freezed))
            {
                status.Remove(Status.Wet);
                stats.TakeDamage(3);
                status.Add(Status.Freezed);
                return;
            }

            //Se sono grounded mi trasformo in yeti
            if (status.Has(Status.Grounded))
            {
                status.Remove(Status.Grounded);
                transformationManager.SwitchTo(CharacterType.Yeti);
                return;
            }
            if (status.Has(Status.Algae))
            {
                status.Remove(Status.Algae);
                status.Add(Status.Freezed);
                return;
            }

            status.Add(Status.Freezed);

        }

        public override void ApplyGrass(PotionScriptable ps)
        {
            if (status.Has(Status.Freezed) || status.Has(Status.Algae) || status.Has(Status.Poisoned))
            {
                //Il mago è immune all'erba in questi stati
                status.TriggerImmunity();
                return;
            }

            if (status.Has(Status.Burned))
            {
                status.Increase(Status.Burned); //L'erba sul fuoco aumenta il livello delle fiamme
                return;
            }

            if (status.Has(Status.Wet))
            {
                status.Remove(Status.Wet); //Questo dovrebbe rimuovere il VFX bagnato
                status.Increase(Status.Algae); //Questo dovrebbe aggiungere il VFX alghe lv 1
                return;
            }

            if (status.Has(Status.Grounded))
            {
                stats.Heal(2);
                return;
            }
            status.Add(Status.Grass); //Questo dovrebbe aggiungere il VFX erba

        }

        public override void ApplyGround(PotionScriptable ps)
        {
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                status.Increase(Status.Grounded);
                return;
            }
            if (status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                status.Increase(Status.Grounded);
                return;
            }
            if (status.Has(Status.Algae))
            {
                status.Remove(Status.Algae);
                status.Increase(Status.Grounded);
                return;
            }
            if (status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                status.Increase(Status.Grounded);
                return;
            }
            if (status.Has(Status.Freezed))
            {
                status.TriggerImmunity();
                return;
            }
            status.Increase(Status.Grounded);
        }

        public override void ApplyHeal(PotionScriptable ps)
        {
            stats.Heal(ps.baseValue);
        }

        /***** TRASFORMAZIONE ***** Il fuoco transforma in Balrog se si è burned */
        public override void ApplyLava(PotionScriptable ps)
        {
            animator.SetTrigger("lavaDrunked");
            // Se sto bruciando, mi trasformo in Balrog
            if (status.Has(Status.Burned))
            {
                transformationManager.SwitchTo(CharacterType.Balrog);
                return;
            }
            //Se sono congelato, rimuovo lo stato congelato e non prendo danno
            if (status.Has(Status.Freezed))
            {
                status.Remove(Status.Freezed);
                return;
            }
            if (status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                stats.TakeDamage(ps.baseValue - 1);
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
            if (stats.MP == stats.MaxMP)
            {
                lightLevel++;
                //Se lightLevel raggiunge 3, il mago si trasforma in WhiteMage
            }
            else
            {
                lightLevel = 0;
            }
        }


        /***** TRASFORMAZIONE ***** Il veleno transforma in Pesce se si è bagnati */
        public override void ApplyPoison(PotionScriptable ps)
        {
            //L'erba neutralizza il veleno e viene rimossa
            if (status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                return;
            }
            if (status.Has(Status.Algae))
            {
                status.Remove(Status.Algae);
                return;
            }
            if (status.Has(Status.Burned))
            {
                stats.TakeDamage(2);
                status.TriggerExplosion();
                animator.SetTrigger("poisonExplosion");//Rimuovere dall'animator e gestire con VFX
                return;
            }
            if (status.Has(Status.Wet))
            {
                status.Remove(Status.Wet);
                transformationManager.SwitchTo(CharacterType.PupperFish);
                return;
            }
            if ((status.Has(Status.Freezed) || status.Has(Status.Grounded)) && status.Has(Status.Poisoned))
            {
                status.TriggerImmunity();
                return;
            }

            status.Increase(Status.Poisoned);

        }

        /***** TRASFORMAZIONE ***** L'acqua transforma in albero se sta crescendo l'erba */
        public override void ApplyWet(PotionScriptable ps)
        {
            if (status.Has(Status.Freezed))
            {
                stats.TakeDamage(2);
                return;
            }

            if (status.Has(Status.Poisoned))
            {
                status.Remove(Status.Poisoned);
                if (status.Has(Status.Grounded))
                {
                    status.Decrease(Status.Grounded);
                }
                else
                {
                    status.Add(Status.Wet);
                }
                return;
            }

            if (status.Has(Status.Grounded))
            {
                status.Decrease(Status.Grounded);

                return;
            }
            if (status.Has(Status.Burned))
            {
                status.Remove(Status.Burned);
                animator.SetTrigger("smoking");
                return;
            }

            if (status.Has(Status.Algae))
            {
                status.Increase(Status.Algae);
                return;
            }
            if (status.Has(Status.Grass))
            {
                status.Remove(Status.Grass);
                transformationManager.SwitchTo(CharacterType.Tree);
                return;
            }

            status.Add(Status.Wet);

        }

        public override void ApplyDark(PotionScriptable ps)
        {
            Debug.Log("Dark potion?");

            if (stats.MP == 0)
            {
                stats.TakeDamage(2);
                Debug.Log("Mage darkLevel up");
                darkLevel++;
                //Rimuovere gli stati di luce se presenti?
                //Se darkLevel raggiunge 3, il mago si trasforma in Lictch
                if (darkLevel > 2)
                {
                    transformationManager.SwitchTo(CharacterType.Litch);
                }
            }
            else
            {
                Debug.Log("Mage darkLevel reset");
                darkLevel = 0;
            }
            stats.LoseMana(ps.baseValue);

        }

        public override CharacterType GetCharacterForm()
        {
            return CharacterType.Mage;
        }


        public override void OnEnterTransformation()
        {
            //Diventare Mago non ha effetti particolari
            switch (transformationManager.previousForm)
            {
                case CharacterType.Balrog:
                    break;
                case CharacterType.Tree:
                    animator.SetTrigger("smoking");
                    break;
                case CharacterType.PupperFish:
                    break;
                case CharacterType.Yeti:
                    break;
                case CharacterType.Litch:
                    break;
                case CharacterType.WhiteMage:
                    break;
            }
        }
        public override void OnExitTransformation()
        {
            //Uscire dalla forma Mago non ha effetti particolari
        }

        public override void FireTick()
        {
            animator.SetTrigger("isDamaged");
            stats.TakeDamage(status.fireLevel);
        }

        public override void PoisonTick()
        {
            animator.SetTrigger("isDamaged");
            if (status.Has(Status.Grounded))
                stats.TakeDamage(1); //Se è interrato prende meno danni da veleno, ma non diminuisce il livello
            else
            {
                stats.TakeDamage(status.poisonLevel);
                status.Decrease(Status.Poisoned);
            }
        }

        public override void GroundTick()
        {
            animator.SetTrigger("isDamaged");
            stats.TakeDamage(2);
        }

        public override void IceTick()
        {
            Debug.Log("Ice Tick Mage has no effect");
        }

        public override float GetFireTickDelay()
        {
            return 2.0f;
        }

        public override float GetPoisonTickDelay()
        {
            if (status.Has(Status.Freezed))
                return Mathf.Infinity; //Se è congelato o interrato non prende danni da veleno

            return 4.0f;
        }

        public override float GetGroundTickDelay()
        {
            if (status.groundLevel < 3)
                return Mathf.Infinity; //Se è interrato a livello 0,1 o 2 non prende danni da terra
            return 5f;
        }

        /*public override float GetIceTickDelay()
        {
            return Mathf.Infinity; //Mage non subisce danni da ghiaccio
        }*/
    }
}

