using System.Collections.Generic;
using UnityEngine;

namespace CharacterSystem
{
    public class DialogManager : MonoBehaviour
    {
        private CharacterStatusController status;
        [SerializeField] private List<DialogRule> rules;

        private void Awake()
        {
            status = GetComponent<CharacterStatusController>();
        }
        public void Evaluate(DialogContext ctx)
        {
            DialogRule bestRule = null;

            foreach (var rule in rules)
            {
                if (!rule.Matches(ctx))
                    continue;

                if (bestRule == null || rule.priority > bestRule.priority)
                    bestRule = rule;
            }

            if (bestRule == null || bestRule.lines.Length == 0)
                return;

            string line = bestRule.lines[Random.Range(0, bestRule.lines.Length)];

            Debug.Log($"[DialogManager] {ctx.character} says: {line}");
            GameMan.Instance.PopDialog(line, 1.5f);
        }




        public void OnPotionDrunk(PotionScriptable potion, CharacterType form)
        {
            Evaluate(new DialogContext
            {
                character = form,
                potion = potion.effectType,
                trigger = DialogTrigger.PotionDrink,
                statuses = status.GetAllStatuses()
            });
        }



    }
}

