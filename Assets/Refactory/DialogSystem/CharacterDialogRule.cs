using System.Collections.Generic;
using CharacterSystem;
using UnityEngine;

    [CreateAssetMenu(menuName = "Dialog/Character Dialog Rule")]
    public class CharacterDialogRule : ScriptableObject
    {
        public CharacterType character;
        public List<PotionDialogEntry> potionDialogs;
    }

    [System.Serializable]
    public class PotionDialogEntry
    {
        public PotionScriptable.EffectType potion;
        public List<StatusDialogCase> cases;
    }
    [System.Serializable]
    public class StatusDialogCase
    {
        [Tooltip("Tutti questi status DEVONO essere presenti. Vuoto = nessuno status richiesto")]
        public List<Status> requiredStatuses;

        [TextArea]
        public List<string> lines;

        public bool Matches(HashSet<Status> currentStatuses)
        {
            if (requiredStatuses == null || requiredStatuses.Count == 0)
                return currentStatuses.Count == 0;

            foreach (var s in requiredStatuses)
            {
                if (!currentStatuses.Contains(s))
                    return false;
            }
            return true;
        }
    }
