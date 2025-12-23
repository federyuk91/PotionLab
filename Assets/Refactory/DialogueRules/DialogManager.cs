using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace CharacterSystem
{
    public class DialogManager : MonoBehaviour
    {
        private CharacterStatusController status;
        [SerializeField] private List<CharacterDialogRule> characterRules;

        private void Awake()
        {
            status = GetComponent<CharacterStatusController>();
        }



        public void OnPotionDrunk(PotionScriptable.EffectType FX, CharacterType character, CharacterStatusController statusController, float popUpDuration=1.5f)
        {
            //Trova le regole per il personaggio scelto
            var rule = characterRules.Find(r => r.character == character);

            //Se non la trova nessun popup di dialogo
            if (rule == null)
                return;

            //Cerca le regole riguardanti la pozione appena bevuta
            var entry = rule.potionDialogs
                .Find(p => p.potion == FX);
            
            //Se non le trova nessun popup di dialogo
            if (entry == null)
                return;

            //Ottiene gli stati attivi sul personaggio
            var currentStatuses = statusController.GetCurrentStatuses();

            // Ordina i casi: più status richiesti = più specifico
            entry.cases.Sort((a, b) =>
                b.requiredStatuses.Count.CompareTo(a.requiredStatuses.Count));

            foreach (var c in entry.cases)
            {

                if (!c.Matches(currentStatuses))
                    continue;
                //Ho trovato un caso che matcha, ma non ha linee di dialogo, voglio fermare la ricerca 
                if (c.lines.Count == 0)
                    return;

                string line = c.lines[Random.Range(0, c.lines.Count)];
                GameMan.Instance.PopDialog(line, popUpDuration);
                return;
            }
        }



    }
}

