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
            var rule = characterRules.Find(r => r.character == character);
            if (rule == null)
                return;

            var entry = rule.potionDialogs
                .Find(p => p.potion == FX);

            if (entry == null)
                return;

            var currentStatuses = statusController.GetAllStatuses();

            // Ordina i casi: più status richiesti = più specifico
            entry.cases.Sort((a, b) =>
                b.requiredStatuses.Count.CompareTo(a.requiredStatuses.Count));

            foreach (var c in entry.cases)
            {
                if (!c.Matches(currentStatuses))
                    continue;

                if (c.lines.Count == 0)
                    return;

                string line = c.lines[Random.Range(0, c.lines.Count)];
                GameMan.Instance.PopDialog(line, popUpDuration);
                return;
            }
        }



    }
}

