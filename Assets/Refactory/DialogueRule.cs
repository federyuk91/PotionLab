using System.Linq;
using UnityEngine;

namespace CharacterSystem
{
    [CreateAssetMenu(menuName = "Dialog/Dialog Rule")]
    public class DialogRule : ScriptableObject
    {
        public CharacterType character;
        public DialogTrigger trigger;
        public PotionScriptable.EffectType potion;

        public Status[] requiredStatuses;
        public Status[] forbiddenStatuses;

        [TextArea]
        public string[] lines;

        public int priority = 0;
        public bool Matches(DialogContext ctx)
        {
            if (trigger != ctx.trigger)
                return false;

            if (character != CharacterType.Any && character != ctx.character)
                return false;

            if (potion != PotionScriptable.EffectType.Any && potion != ctx.potion)
                return false;

            foreach (var s in requiredStatuses)
                if (!ctx.statuses.Contains(s))
                    return false;

            foreach (var s in forbiddenStatuses)
                if (ctx.statuses.Contains(s))
                    return false;

            return true;
        }
    }
}
